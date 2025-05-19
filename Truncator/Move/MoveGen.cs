using System.Diagnostics;
using static Utils;

public static class MoveGen
{

    public static int GenerateLegaMoves(ref Span<Move> moves, ref Pos p)
    {
        int moveCount = GeneratePseudolegalMoves(ref moves, ref p);

        // remove all illegal moves
        for (int i = 0; i < moveCount;)
        {
            if (!p.IsLegal(moves[i]))
            {
                (moves[i], moves[moveCount - 1]) = (moves[moveCount - 1], moves[i]);
                moveCount--;
                continue;
            }
            i++;
        }

        moves = moves.Slice(0, moveCount);
        return moveCount;
    }

    public static unsafe int GeneratePseudolegalMoves(ref Span<Move> moves, ref Pos p)
    {
        int moveCount = 0;
        ulong checkMask = GenerateCheckMask(ref p);

        GeneratePieceMoves(ref moves, ref moveCount, ref p, checkMask, PieceType.Knight);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, checkMask, PieceType.Bishop);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, checkMask, PieceType.Rook);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, checkMask, PieceType.Queen);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, ulong.MaxValue, PieceType.King);

        GeneratePawnPushes(ref moves, ref moveCount, ref p, checkMask);
        GeneratePawnCaptures(ref moves, ref moveCount, ref p, checkMask);
        GenerateEnPassantCapture(ref moves, ref moveCount, ref p);

        if (checkMask == ulong.MaxValue)
        {
            GenerateCastling(ref moves, ref moveCount, ref p);
        }

        return moveCount;       
    }


    public static unsafe ulong GenerateCheckMask(ref Pos p)
    {
        ulong checker = p.GetCheckers();

        if (checker == 0) // no checkers
        {
            return ulong.MaxValue;
        }
        else if (!MoreThanOne(checker)) // only one checker
        {
            int ksq = p.KingSquares[(int)p.Us];
            int csq = lsb(checker);
            ulong mask = GetRay(ksq, csq) | (1ul << csq);
            return mask;
        }
        else // double check
        {
            return 0;
        }
    }


    private static unsafe void GeneratePieceMoves(ref Span<Move> moves, ref int moveCount, ref Pos p, ulong mask, PieceType pt)
    {
        ulong pieces = p.GetPieces(p.Us, pt);
        ulong enemyOrEmpty = ~p.ColorBB[(int)p.Us] & mask;
        while (pieces != 0)
        {
            int from = popLsb(ref pieces);
            ulong attack = Attacks.PieceAttacks(pt, from, p.blocker) & enemyOrEmpty;

            while (attack != 0)
            {
                int to = popLsb(ref attack);
                moves[moveCount++] = new Move(from, to);
            }
        }
    }


    private static void GeneratePawnPushes(ref Span<Move> moves, ref int moveCount, ref Pos p, ulong mask)
    {
        ulong pawns = p.GetPieces(p.Us, PieceType.Pawn);
        ulong empty = ~p.blocker;
        ulong thirdRank = p.Us == Color.White ? 0x0000_0000_00FF_0000ul : 0x0000_FF00_0000_0000ul;

        int dir = p.Us == Color.White ? 8 : -8;

        ulong singlePush = Up(p.Us, pawns) & empty;
        ulong doublePush = Up(p.Us, singlePush & thirdRank) & empty;

        singlePush &= mask;
        doublePush &= mask;

        ulong promos = singlePush & 0xFF00_0000_0000_00FFul;
        singlePush &= ~promos;

        extractPawnMoves(ref moves, ref moveCount, dir,     singlePush);        
        extractPawnMoves(ref moves, ref moveCount, dir+dir, doublePush);
        extractPawnPromotions(ref moves, ref moveCount, dir, promos);
    }

    private static unsafe void GeneratePawnCaptures(ref Span<Move> moves, ref int moveCount, ref Pos p, ulong mask)
    {
        ulong pawns = p.GetPieces(p.Us, PieceType.Pawn);
        ulong enemy = p.ColorBB[(int)p.Them];
        int dirRight = p.Us == Color.White ? 9 : -7;
        int dirLeft  = p.Us == Color.White ? 7 : -9;

        ulong right = Attacks.RightPawnMassAttacks(p.Us, pawns) & enemy;
        ulong left  = Attacks.LeftPawnMassAttacks (p.Us, pawns) & enemy;

        right &= mask;
        left  &= mask;

        ulong rPromo = right & 0xFF00_0000_0000_00FFul;
        ulong lPromo = left  & 0xFF00_0000_0000_00FFul;
        right ^= rPromo;
        left  ^= lPromo;

        extractPawnMoves(ref moves, ref moveCount, dirRight, right);
        extractPawnMoves(ref moves, ref moveCount, dirLeft,  left );
        extractPawnPromotions(ref moves, ref moveCount, dirRight, rPromo);
        extractPawnPromotions(ref moves, ref moveCount, dirLeft,  lPromo);
    }

    private static void extractPawnMoves(ref Span<Move> moves, ref int moveCount, int dir, ulong pawns)
    {
        while (pawns != 0)
        {
            int to = popLsb(ref pawns);
            moves[moveCount++] = new Move(to-dir, to);
        }
    }

    private static void extractPawnPromotions(ref Span<Move> moves, ref int moveCount, int dir, ulong pawns)
    {
        while (pawns != 0)
        {
            int to = popLsb(ref pawns);
            moves[moveCount++] = new Move(to-dir, to, MoveFlag.KnightPromo);
            moves[moveCount++] = new Move(to-dir, to, MoveFlag.BishopPromo);
            moves[moveCount++] = new Move(to-dir, to, MoveFlag.RookPromo  );
            moves[moveCount++] = new Move(to-dir, to, MoveFlag.QueenPromo );
        }
    }

    private static void GenerateEnPassantCapture(ref Span<Move> moves, ref int moveCount, ref Pos p)
    {
        if (p.EnPassantSquare == (int)Square.NONE)
        {
            return;
        }

        int victimSquare = p.Us == Color.White ? p.EnPassantSquare + 8 : p.EnPassantSquare - 8;
        ulong attacker = Attacks.PawnAttacks(p.Them, victimSquare) & p.GetPieces(p.Us, PieceType.Pawn);

        while (attacker != 0)
        {
            int from = popLsb(ref attacker);
            moves[moveCount++] = new Move(from, victimSquare, MoveFlag.EnPassant);
        }
    }

    private static void GenerateCastling(ref Span<Move> moves, ref int moveCount, ref Pos p)
    {
        // kingside castling
        if (p.HasCastlingRight(p.Us, true))
        {
            var (from, to) = Castling.GetKingCastlingSquares(p.Us, true);
            moves[moveCount++] = new Move(from, to, MoveFlag.Castling);
        }

        // queenside castling
        if (true || p.HasCastlingRight(p.Us, false))
        {
            var (from, to) = Castling.GetKingCastlingSquares(p.Us, false);
            moves[moveCount++] = new Move(from, to, MoveFlag.Castling);
        }
    }

    private static ulong Up(Color c, ulong bb) 
    {
        Debug.Assert(c != Color.NONE);
        return c == Color.White ? bb << 8 : bb >> 8;
    }
}
