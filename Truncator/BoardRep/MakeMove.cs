


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

        if (m.IsCastling)
        {
            if (!HasCastlingRight(Us, from < to) ||
                (blocker & Castling.GetCastlingBlocker(Us, from < to)) != 0 ||
                 GetCheckers() != 0)
            {
                return false;
            }

            var (rookStart, rookEnd) = Castling.GetRookCastlingSquares(Us, from < to);
            block = (block ^ (1ul << rookStart)) | (1ul << rookEnd);

            int dir = from < to ? 1 : -1;

            for (int sq=from+dir; sq!=to; sq+=dir)
            {
                if ((AttackerTo(sq, block) & ColorBB[(int)Them]) != 0)
                {
                    //Console.WriteLine($"{(Square)sq} is attacked! ({m})");
                    return false;
                }
            }

            return (AttackerTo(to, block) & ColorBB[(int)Them]) == 0;
        }

        if (m.IsEnPassant)
        {
            block ^= 1ul << EnPassantSquare;
            ulong victim = 1ul << (Us == Color.White ? to - 8 : to + 8);
            return (AttackerTo(ksq, block) & ColorBB[(int)Them] & ~victim) == 0;
        }

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

        ColorBB[(int)Us] ^= FromToBB;
        PieceBB[(int)movingPt] ^= FromToBB;

        FiftyMoveRule++;

        if (victimPt != PieceType.NONE)
        {
            ColorBB[(int)Them] ^= toBB;
            PieceBB[(int)victimPt] ^= toBB;

            FiftyMoveRule = 0;
        }

        if (EnPassantSquare != (int)Square.NONE)
        {
            EnPassantSquare = (int)Square.NONE;
        }

        if (movingPt == PieceType.Pawn)
        {
            FiftyMoveRule = 0;

            if (Math.Abs(from - to) == 16)
            {
                EnPassantSquare = to;
            }

            if (m.IsPromotion)
            {
                PieceType promoPt = m.PromoType;
                PieceBB[(int)PieceType.Pawn] ^= toBB;
                PieceBB[(int)promoPt] ^= toBB;
            }

            if (m.IsEnPassant)
            {
                int victim = Us == Color.White ? to - 8 : to + 8;
                PieceBB[(int)PieceType.Pawn] ^= 1ul << victim;
                ColorBB[(int)Them] ^= 1ul << victim;
            }
        }

        if (m.IsCastling)
        {
            var (rookStart, rookEnd) = Castling.GetRookCastlingSquares(Us, from < to);
            ulong rookBB = (1ul << rookStart) | (1ul << rookEnd);
            PieceBB[(int)PieceType.Rook] ^= rookBB;
            ColorBB[(int)Us] ^= rookBB;
        }

        CastlingRights &= Castling.modifier[from];
        CastlingRights &= Castling.modifier[to];

        if (movingPt == PieceType.King)
        {
            KingSquares[(int)Us] = to;
        }

        Us = Them;
    }

}
