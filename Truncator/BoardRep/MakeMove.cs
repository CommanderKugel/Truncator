
using System.Diagnostics;

public unsafe partial struct Pos
{

    public bool IsPseudoLegal(Move m)
    {
        PieceType pt = PieceTypeOn(m.from);

        // catch obviously illegal cases
        // we have to move our own piece
        // cant capture our own pieces 
        // except castling, which is encoded as king x rook
        // and only pawns are able to promote

        if (m.IsNull
            || pt == PieceType.NONE
            || ColorOn(m.from) != Us
            || ColorOn(m.to) == Us && !m.IsCastling
            || m.IsPromotion && pt != PieceType.Pawn)
        {
            return false;
        }

        // castling moves two pieces and follows its rules on its own...
        // also, (d)frc castling is kinda fucked, luckily its logic 
        // is mostly computed by IsLegal() and MakeMove()

        if (m.IsCastling)
        {
            // make sure we have the castling rights and 
            // are about to capture the correct rook

            return pt == PieceType.King
                && HasCastlingRight(Us, m.from < m.to)
                && m.from == Castling.GetKingStart(Us)
                && m.to == Castling.GetKingCastlingTarget(Us, m.from < m.to)
                && PieceTypeOn(m.to) == PieceType.Rook
                && ColorOn(m.to) == Us;
        }

        Debug.Assert(!m.IsCastling);

        // pawns are special so lets handle them on their own
        // luckily, promotions are really easy here

        if (pt == PieceType.Pawn)
        {
            if (m.IsEnPassant)
            {
                int target = m.to - (Us == Color.White ? 8 : -8);
                return EnPassantSquare == target;
            }

            if (Utils.FileOf(m.from) != Utils.FileOf(m.to)) // capture
            {
                return ((1ul << m.to) & Attacks.PawnAttacks(Us, m.from)) != 0
                    && IsCapture(m);
            }

            else // pawn pushes
            {
                ulong bb = Up(Us, 1ul << m.from) & ~blocker;

                // double pushes
                if (Utils.RankOf(m.from) == (Us == Color.White ? 1 : 6))
                {
                    bb |= Up(Us, bb) & ~blocker;
                }

                return ((1ul << m.to) & bb) != 0;

                static ulong Up(Color c, ulong bb) => c == Color.White ? bb << 8 : bb >> 8;
            }

        }

        // test if the destination is accessible to the Piece
        // now Knight, Bishop, Rook, Queen and King remain

        Debug.Assert(pt != PieceType.Pawn);
        Debug.Assert(!m.IsCastling);
        Debug.Assert(!m.IsEnPassant);
        Debug.Assert(m.NotNull);

        return (Attacks.PieceAttacks(pt, m.from, blocker) & (1ul << m.to)) != 0;
    }


    public bool IsLegal(Move m)
    {

        // assumes positive IsPseudoLegal check
        
        Debug.Assert(m.NotNull);

        int from = m.from;
        int to   = m.to;
        
        PieceType pt = PieceTypeOn(from);
        int ksq = pt == PieceType.King ? to : KingSquares[(int)Us];

        ulong block = (blocker ^ (1ul << from)) | (1ul << to);

        // Castling is legal if:
        // 1. we are not in check and we do not move through/into check
        // 2. we have the corresponding castling rights
        // 3. the kings and rooks path is not blocked by other pieces
        
        if (m.IsCastling)
        {
            block ^= 1ul << to;

            // check if we have the castling rights, the path is not blocked
            // and if we are currently in check

            if (!HasCastlingRight(Us, from < to)
                || (block & Castling.GetCastlingBlocker(Us, from < to)) != 0)
            {
                return false;
            }

            var (kingEndSq, rookEndSq) = Castling.GetCastlingSquares(Us, from < to);
            int target = Castling.GetKingDestination(Us, from < to);

            block ^= (1ul << kingEndSq) | (1ul << rookEndSq);
            int dir = from < to ? 1 : -1;

            // check if we move through check

            for (int sq = from; sq != target; sq += dir)
            {
                if ((AttackerTo(sq, block) & ColorBB[(int)Them]) != 0)
                {
                    return false;
                }
            }

            // check if we would end in check

            return (AttackerTo(target, block) & ColorBB[(int)Them]) == 0;
        }

        if (m.IsEnPassant)
        {
            // for en-passant, simply always compute the king-attackers with the
            // blocker configuratio after the move

            block ^= 1ul << EnPassantSquare;
            ulong victim = 1ul << (Us == Color.White ? to - 8 : to + 8);
            return (AttackerTo(ksq, block) & ColorBB[(int)Them] & ~victim) == 0;
        }

        // pseudolegal moves (normal and promotions) are legal, if we dont leave the king 
        // in check after making the move. Because Movegeneration makes use of check-masks,
        // only moves are generated that block slider checks or capture the checking piece,
        // if needed. Thus, only potential pinned pieces can leave the king in check.
        // If the king and moving Piece are aligned, a legality check is needed.

        return (pt != PieceType.King && !IsInKingsSliderVision(from))
            || (AttackerTo(ksq, block) & ColorBB[(int)Them] & ~(1ul << to)) == 0;
    }

    public void MakeMove(Move m, SearchThread thread)
    {
        Debug.Assert(m.NotNull, "can not make null-moves the normal way!");
        int from = m.from;
        int to = m.to;

        ulong fromBB = 1ul << from;
        ulong toBB = 1ul << to;
        ulong FromToBB = fromBB | toBB;

        PieceType movingPt = PieceTypeOn(from);
        PieceType victimPt = PieceTypeOn(to);
        Debug.Assert(movingPt != PieceType.NONE, "there is no piece to move!");

        // make the quiet part of the move

        ColorBB[(int)Us] ^= FromToBB;
        PieceBB[(int)movingPt] ^= FromToBB;

        // update Zobrist keys regarding the moves piece

        ulong temp = Zobrist.GetSinglePieceKey(Us, movingPt, from) ^ Zobrist.GetSinglePieceKey(Us, movingPt, to);
        ZobristKey ^= temp;
        PieceKeys[(int)movingPt] ^= temp;
        if (movingPt != PieceType.Pawn)
        {
            NonPawnKeys[(int)Us] ^= temp;
        }

        FiftyMoveRule++;

        // if capture: remove the victim

        if (victimPt != PieceType.NONE && !m.IsCastling)
        {
            ColorBB[(int)Them] ^= toBB;
            PieceBB[(int)victimPt] ^= toBB;

            // update zobrist keys regarding the captured piece

            temp = Zobrist.GetSinglePieceKey(Them, victimPt, to);
            ZobristKey ^= temp;
            PieceKeys[(int)victimPt] ^= temp;
            if (victimPt != PieceType.Pawn)
            {
                NonPawnKeys[(int)Them] ^= temp;
            }

            FiftyMoveRule = 0;
        }

        // reset the ep-square

        if (EnPassantSquare != (int)Square.NONE)
        {
            ZobristKey ^= Zobrist.GetEpKEy(EnPassantSquare);
            EnPassantSquare = (int)Square.NONE;
        }

        if (movingPt == PieceType.Pawn)
        {
            // fifty move rule resets, because pawn-moves are not reversible

            FiftyMoveRule = 0;

            // set ep square after double pawn pushes

            if (Math.Abs(from - to) == 16)
            {
                EnPassantSquare = to;
                ZobristKey ^= Zobrist.GetEpKEy(to);
            }

            // swap the pawn with the promoted piece

            if (m.IsPromotion)
            {
                PieceType promoPt = m.PromoType;
                PieceBB[(int)PieceType.Pawn] ^= toBB;
                PieceBB[(int)promoPt] ^= toBB;

                // update zobrist keys regarding the promoted pawn and piece 
                
                ulong promoKey = Zobrist.GetSinglePieceKey(Us, promoPt, to);
                ulong pawnKey = Zobrist.GetSinglePieceKey(Us, PieceType.Pawn, to);
                ZobristKey ^= promoKey ^ pawnKey;
                NonPawnKeys[(int)Us] ^= promoKey;
                PieceKeys[(int)promoPt] ^= promoKey;
                PieceKeys[(int)PieceType.Pawn] ^= pawnKey;
            }

            // capture the ep-victim, as it is not done like normal captures

            if (m.IsEnPassant)
            {
                int victim = Us == Color.White ? to - 8 : to + 8;
                PieceBB[(int)PieceType.Pawn] ^= 1ul << victim;
                ColorBB[(int)Them] ^= 1ul << victim;

                // update zobrist keys regarding the captured pawn

                temp = Zobrist.GetSinglePieceKey(Them, PieceType.Pawn, victim);
                ZobristKey ^= temp;
                PieceKeys[(int)PieceType.Pawn] ^= temp;
                victimPt = PieceType.Pawn;
            }
        }

        // move the rooks when castling

        if (m.IsCastling)
        {
            var (kingEnd, rookEnd) = Castling.GetCastlingSquares(Us, from < to);

            // first, move the king to the correct square.
            // for (d)frc, castling is encoded as capturing the rook.

            PieceBB[(int)PieceType.King] ^= 1ul << to;
            PieceBB[(int)PieceType.King] |= 1ul << kingEnd;

            // now move the rook to its destination.

            PieceBB[(int)PieceType.Rook] ^= 1ul << to;
            PieceBB[(int)PieceType.Rook] |= 1ul << rookEnd;

            // lastly, update out colors occupancy.
            // the rook was already 'captured', only place down both pieces.

            ColorBB[(int)Us] ^= 1ul << kingEnd | 1ul << rookEnd;

            // correctly update kingsquare

            KingSquares[(int)Us] = kingEnd;

            // update zobrist keys regarding the king and rook

            ulong kingKey = Zobrist.GetSinglePieceKey(Us, PieceType.King, kingEnd) ^ Zobrist.GetSinglePieceKey(Us, PieceType.King, to);
            ulong rookKey = Zobrist.GetSinglePieceKey(Us, PieceType.Rook, to) ^ Zobrist.GetSinglePieceKey(Us, PieceType.Rook, rookEnd);
            ZobristKey ^= kingKey ^ rookKey;
            NonPawnKeys[(int)Us] ^= kingKey ^ rookKey;
            PieceKeys[(int)PieceType.King] ^= kingKey;
            PieceKeys[(int)PieceType.Rook] ^= rookKey;

            victimPt = PieceType.NONE;
        }
        else if (movingPt == PieceType.King)
        {
            KingSquares[(int)Us] = to;
        }

        // update castling rights
        
        byte oldCastling = CastlingRights;
        CastlingRights &= Castling.modifier[from];
        CastlingRights &= Castling.modifier[to];

        if (CastlingRights != oldCastling)
        {
            ZobristKey ^= Zobrist.GetCastlingKey(oldCastling)
                       ^ Zobrist.GetCastlingKey(CastlingRights);
        }

        // swap the side-to-move

        Us = Them;
        ZobristKey ^= Zobrist.stmKey;

        // miscellaneous

        FullMoveCounter += (int)Us;
        Threats = ComputeThreats();

        // update thread and search-stack data

        thread.nodeStack[thread.ply].MovedPieceType = movingPt;
        thread.nodeStack[thread.ply].CapturedPieceType = victimPt;
        thread.nodeStack[thread.ply].move = m;
        thread.nodeStack[thread.ply].ContHist = thread.history.ContHist[Them, movingPt, m.to];
        thread.nodeCount++;
        thread.ply++;

        // push to rep-table only in search, because qsearch cant realistically cause repetitions

        Debug.Assert(NonPawnKeys[(int)Color.White] == Zobrist.GetNonPawnKey(Color.White, ref this));
        Debug.Assert(NonPawnKeys[(int)Color.Black] == Zobrist.GetNonPawnKey(Color.Black, ref this));
        Debug.Assert(Utils.MoreThanOne(PieceBB[(int)PieceType.King]));
    }

    public void MakeNullMove(SearchThread thread)
    {
        // swap the side-to-move

        Us = Them;
        ZobristKey ^= Zobrist.stmKey;

        // reset the ep-square

        if (EnPassantSquare != (int)Square.NONE)
        {
            ZobristKey ^= Zobrist.GetEpKEy(EnPassantSquare);
            EnPassantSquare = (int)Square.NONE;
        }

        // miscellaneous stuff

        FullMoveCounter += (int)Us;
        Threats = ComputeThreats();

        // update thread and search-stack data

        thread.nodeStack[thread.ply].MovedPieceType = PieceType.NONE;
        thread.nodeStack[thread.ply].CapturedPieceType = PieceType.NONE;
        thread.nodeStack[thread.ply].move = Move.NullMove;
        thread.nodeStack[thread.ply].ContHist = thread.history.ContHist.NullHist;
        thread.nodeCount++;
        thread.ply++;
    }

}
