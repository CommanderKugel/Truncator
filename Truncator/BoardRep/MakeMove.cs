
using System.Diagnostics;

public unsafe partial struct Pos
{

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
            if (!HasCastlingRight(Us, from < to) ||
                (block & Castling.GetCastlingBlocker(Us, from < to)) != 0)
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
        return (pt != PieceType.King && !IsInKingsSliderVision(from)) ||
               (AttackerTo(ksq, block) & ColorBB[(int)Them] & ~(1ul << to)) == 0;
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
        ZobristKey ^= Zobrist.GetPieceKey(Us, movingPt, from)
                   ^ Zobrist.GetPieceKey(Us, movingPt, to);

        FiftyMoveRule++;

        // if capture: remove the victim
        if (victimPt != PieceType.NONE && !m.IsCastling)
        {
            ColorBB[(int)Them] ^= toBB;
            PieceBB[(int)victimPt] ^= toBB;
            ZobristKey ^= Zobrist.GetPieceKey(Them, victimPt, to);

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
                ZobristKey ^= Zobrist.GetPieceKey(Us, promoPt, to)
                           ^ Zobrist.GetPieceKey(Us, PieceType.Pawn, to);
            }

            // capture the ep-victim, as it is not done like normal captures
            if (m.IsEnPassant)
            {
                int victim = Us == Color.White ? to - 8 : to + 8;
                PieceBB[(int)PieceType.Pawn] ^= 1ul << victim;
                ColorBB[(int)Them] ^= 1ul << victim;
                ZobristKey ^= Zobrist.GetPieceKey(Them, PieceType.Pawn, victim);
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

            ZobristKey ^= Zobrist.GetPieceKey(Us, PieceType.King, kingEnd)
                        ^ Zobrist.GetPieceKey(Us, PieceType.King, to)
                        ^ Zobrist.GetPieceKey(Us, PieceType.Rook, to)
                        ^ Zobrist.GetPieceKey(Us, PieceType.Rook, rookEnd);

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

        // update the search-stack
        thread.nodeStack[thread.ply].MovedPieceType = movingPt;
        thread.nodeStack[thread.ply].CapturedPieceType = victimPt;
        thread.nodeStack[thread.ply].move = m;
        thread.nodeStack[thread.ply].ContHist = thread.history.ContHist[Them, movingPt, to];

        // update the thread-data
        thread.nodeCount++;
        thread.ply++;
        // push to rep-table only in search, because qsearch cant realistically cause repetitions
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

        Debug.Assert(this.ZobristKey == Zobrist.ComputeFromZero(ref this));

        // update the search-stack
        thread.nodeStack[thread.ply].MovedPieceType = PieceType.NONE;
        thread.nodeStack[thread.ply].CapturedPieceType = PieceType.NONE;
        thread.nodeStack[thread.ply].move = Move.NullMove;
        thread.nodeStack[thread.ply].ContHist = thread.history.ContHist.NullHist;

        // update the thread-data
        thread.nodeCount++;
        thread.ply++;
    }

}
