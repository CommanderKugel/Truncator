using System.Diagnostics;
using static Utils;

public static class MoveGen
{

    public static int GenerateLegaMoves(ref Span<Move> moves, ref Pos p)
    {
        int moveCount = 0;
        if ((p.Threats & p.GetPieces(p.Us, PieceType.King)) == 0)
        {
            GeneratePseudolegalMoves<Captures>(ref moves, ref moveCount, ref p);
            GeneratePseudolegalMoves<Quiets>(ref moves, ref moveCount, ref p);
        }
        else
        {
            GeneratePseudolegalMoves<CaptureEvasions>(ref moves, ref moveCount, ref p);
            GeneratePseudolegalMoves<QuietEvasions>(ref moves, ref moveCount, ref p);
        }

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

    public static unsafe void GeneratePseudolegalMoves<Type>(ref Span<Move> moves, ref int moveCount, ref Pos p)
        where Type : GenType
    {

        bool captures = typeof(Type) == typeof(Captures) || typeof(Type) == typeof(CaptureEvasions);
        bool evasions = typeof(Type) == typeof(QuietEvasions) || typeof(Type) == typeof(CaptureEvasions);

        // double checks only allow king moves

        if (evasions && MoreThanOne(p.Checkers))
        {
            GeneratePieceMoves(ref moves, ref moveCount, ref p, ~p.Threats & (captures ? p.ColorBB[(int)p.Them] : ~p.blocker), PieceType.King);
            return;
        }

        ulong mask = captures && !evasions ? p.ColorBB[(int)p.Them]
            : !captures && !evasions ? ~p.blocker
            : captures && evasions ? p.Checkers
            : !captures && evasions ? GetRay(p.KingSquares[(int)p.Us], lsb(p.Checkers))
            : throw new Exception("Unexpected Gentype");

        GeneratePieceMoves(ref moves, ref moveCount, ref p, mask, PieceType.Knight);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, mask, PieceType.Bishop);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, mask, PieceType.Rook);
        GeneratePieceMoves(ref moves, ref moveCount, ref p, mask, PieceType.Queen);

        // king moves dont need to block checks

        ulong kingMask = captures ? p.ColorBB[(int)p.Them] : ~p.blocker;
        GeneratePieceMoves(ref moves, ref moveCount, ref p, kingMask, PieceType.King);

        if (captures)
        {
            GeneratePawnCaptures(ref moves, ref moveCount, ref p, mask);
            GenerateEnPassantCapture(ref moves, ref moveCount, ref p);
        }

        if (!captures)
        {
            GeneratePawnPushes(ref moves, ref moveCount, ref p, mask);
        }
        
        if (!captures && !evasions)
        {
            GenerateCastling(ref moves, ref moveCount, ref p);
        }
    }


    private static unsafe void GeneratePieceMoves(ref Span<Move> moves, ref int moveCount, ref Pos p, ulong mask, PieceType pt)
    {
        ulong pieces = p.GetPieces(p.Us, pt);
        while (pieces != 0)
        {
            int from = popLsb(ref pieces);
            ulong attack = Attacks.PieceAttacks(pt, from, p.blocker) & mask;

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

        extractPawnMoves(ref moves, ref moveCount, dir, singlePush);
        extractPawnMoves(ref moves, ref moveCount, dir + dir, doublePush);
        extractPawnPromotions(ref moves, ref moveCount, dir, promos);
    }

    private static unsafe void GeneratePawnCaptures(ref Span<Move> moves, ref int moveCount, ref Pos p, ulong mask)
    {
        ulong pawns = p.GetPieces(p.Us, PieceType.Pawn);
        ulong enemy = p.ColorBB[(int)p.Them];
        int dirRight = p.Us == Color.White ? 9 : -7;
        int dirLeft = p.Us == Color.White ? 7 : -9;

        ulong right = Attacks.RightPawnMassAttacks(p.Us, pawns) & enemy;
        ulong left = Attacks.LeftPawnMassAttacks(p.Us, pawns) & enemy;

        right &= mask;
        left &= mask;

        ulong rPromo = right & 0xFF00_0000_0000_00FFul;
        ulong lPromo = left & 0xFF00_0000_0000_00FFul;
        right ^= rPromo;
        left ^= lPromo;

        extractPawnMoves(ref moves, ref moveCount, dirRight, right);
        extractPawnMoves(ref moves, ref moveCount, dirLeft, left);
        extractPawnPromotions(ref moves, ref moveCount, dirRight, rPromo);
        extractPawnPromotions(ref moves, ref moveCount, dirLeft, lPromo);
    }

    private static void extractPawnMoves(ref Span<Move> moves, ref int moveCount, int dir, ulong pawns)
    {
        while (pawns != 0)
        {
            int to = popLsb(ref pawns);
            moves[moveCount++] = new Move(to - dir, to);
        }
    }

    private static void extractPawnPromotions(ref Span<Move> moves, ref int moveCount, int dir, ulong pawns)
    {
        while (pawns != 0)
        {
            int to = popLsb(ref pawns);
            moves[moveCount++] = new Move(to - dir, to, MoveFlag.KnightPromo);
            moves[moveCount++] = new Move(to - dir, to, MoveFlag.BishopPromo);
            moves[moveCount++] = new Move(to - dir, to, MoveFlag.RookPromo);
            moves[moveCount++] = new Move(to - dir, to, MoveFlag.QueenPromo);
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

    private static unsafe void GenerateCastling(ref Span<Move> moves, ref int moveCount, ref Pos p)
    {
        // kingside castling
        if (p.HasCastlingRight(p.Us, true))
        {
            moves[moveCount++] = new Move(
                p.KingSquares[(int)p.Us],
                Castling.GetKingCastlingTarget(p.Us, true),
                MoveFlag.Castling
            );
        }

        // queenside castling
        if (p.HasCastlingRight(p.Us, false))
        {
            moves[moveCount++] = new Move(
                p.KingSquares[(int)p.Us],
                Castling.GetKingCastlingTarget(p.Us, false),
                MoveFlag.Castling
            );
        }
    }

    private static ulong Up(Color c, ulong bb) 
    {
        Debug.Assert(c != Color.NONE);
        return c == Color.White ? bb << 8 : bb >> 8;
    }
}
