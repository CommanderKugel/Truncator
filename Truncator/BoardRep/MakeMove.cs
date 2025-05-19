


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
            // check if we have the castling rights, the path is not blocked
            // and if we are currently in check
            if (!HasCastlingRight(Us, from < to) ||
                (blocker & Castling.GetCastlingBlocker(Us, from < to)) != 0 ||
                 GetCheckers() != 0)
            {
                return false;
            }

            var (rookStart, rookEnd) = Castling.GetRookCastlingSquares(Us, from < to);
            block = (block ^ (1ul << rookStart)) | (1ul << rookEnd);
            int dir = from < to ? 1 : -1;

            // check if we move through check
            for (int sq = from + dir; sq != to; sq += dir)
            {
                if ((AttackerTo(sq, block) & ColorBB[(int)Them]) != 0)
                {
                    return false;
                }
            }

            // check if we would end in check
            return (AttackerTo(to, block) & ColorBB[(int)Them]) == 0;
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

    public void MakeMove(Move m)
    {
        int from = m.from;
        int to = m.to;

        ulong fromBB = 1ul << from;
        ulong toBB = 1ul << to;
        ulong FromToBB = fromBB | toBB;

        PieceType movingPt = PieceTypeOn(from);
        PieceType victimPt = PieceTypeOn(to);
        Debug.Assert(movingPt != PieceType.NONE);

        // make the quiet part of the move
        ColorBB[(int)Us] ^= FromToBB;
        PieceBB[(int)movingPt] ^= FromToBB;

        FiftyMoveRule++;

        // if capture: remove the victim
        if (victimPt != PieceType.NONE)
        {
            ColorBB[(int)Them] ^= toBB;
            PieceBB[(int)victimPt] ^= toBB;

            FiftyMoveRule = 0;
        }

        // reset the ep-square
        if (EnPassantSquare != (int)Square.NONE)
        {
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
            }

            // swap the pawn with the promoted piece
            if (m.IsPromotion)
            {
                PieceType promoPt = m.PromoType;
                PieceBB[(int)PieceType.Pawn] ^= toBB;
                PieceBB[(int)promoPt] ^= toBB;
            }

            // capture the ep-victim, as it is not done like normal captures
            if (m.IsEnPassant)
            {
                int victim = Us == Color.White ? to - 8 : to + 8;
                PieceBB[(int)PieceType.Pawn] ^= 1ul << victim;
                ColorBB[(int)Them] ^= 1ul << victim;
            }
        }

        // move the rooks when castling
        if (m.IsCastling)
        {
            var (rookStart, rookEnd) = Castling.GetRookCastlingSquares(Us, from < to);
            ulong rookBB = (1ul << rookStart) | (1ul << rookEnd);
            PieceBB[(int)PieceType.Rook] ^= rookBB;
            ColorBB[(int)Us] ^= rookBB;
        }

        // update castling rights
        CastlingRights &= Castling.modifier[from];
        CastlingRights &= Castling.modifier[to];

        // update the kingsquare
        if (movingPt == PieceType.King)
        {
            KingSquares[(int)Us] = to;
        }

        // swap the side-to-move
        Us = Them;
    }

}
