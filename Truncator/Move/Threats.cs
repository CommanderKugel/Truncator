using System.Diagnostics;

public struct Threats
{

    /// <summary>
    /// Threats per Color per PieceType
    /// </summary>
    private unsafe fixed ulong table[2 * 6];

    /// <summary>
    /// total Threats per Color
    /// </summary>
    private unsafe fixed ulong maps[2];

    /// <summary>
    /// Access the threat per Color per PieceType
    /// </summary>
    public unsafe ulong this[Color c, PieceType pt]
    {
        get
        {
            Debug.Assert(c != Color.NONE);
            Debug.Assert(pt != PieceType.NONE);
            return table[(int)c * 6 + (int)pt];
        }
        set
        {
            Debug.Assert(c != Color.NONE);
            Debug.Assert(pt != PieceType.NONE);
            table[(int)c * 6 + (int)pt] = value;
        }
    }

    /// <summary>
    /// Access the threat per Color per PieceType
    /// </summary>
    public unsafe ulong this[Color c]
    {
        get
        {
            Debug.Assert(c != Color.NONE);
            return maps[(int)c];
        }
        set
        {
            Debug.Assert(c != Color.NONE);
            maps[(int)c] = value;
        }
    }


    public unsafe Threats(ref Pos p)
    {
        ComputeFromZero(ref p, Color.White);
        ComputeFromZero(ref p, Color.Black);
    }

    private unsafe void ComputeFromZero(ref Pos p, Color c)
    {
        ulong pawns = p.GetPieces(c, PieceType.Pawn);
        this[c, PieceType.Pawn] = Attacks.LeftPawnMassAttacks(c, pawns) | Attacks.RightPawnMassAttacks(c, pawns);

        for (PieceType pt = PieceType.Knight; pt <= PieceType.King; pt++)
        {
            ulong pieces = p.GetPieces(c, pt);
            while (pieces != 0)
            {
                int sq = Utils.popLsb(ref pieces);
                ulong attack = Attacks.PieceAttacks(pt, sq, p.blocker);
                this[c, pt] |= attack;
            }
        }
    }

    public unsafe void Update(ref Pos p, Move m, PieceType movedPt, PieceType capturedPt)
    {
        ulong mask = (1ul << m.from) | (1ul << m.to);
        if (m.IsPromotion)
        {
            movedPt = m.PromoType;
        }

        if (m.IsEnPassant)
        {
            mask |= 1ul << ((p.Us == Color.White) ? m.to - 8 : m.to + 8);
        }

        if (m.IsCastling)
        {
            mask = Castling.GetCastlingBlocker(p.Us, m.from < m.to);
        }

        // if the moved piece was a leaping piece (pawn, knight, king)
        // update that types threat map. 

        if (movedPt == PieceType.Pawn || m.IsPromotion)
        {
            ulong pawns = p.GetPieces(p.Us, PieceType.Pawn);
            this[p.Us, PieceType.Pawn] = Attacks.LeftPawnMassAttacks(p.Us, pawns) | Attacks.RightPawnMassAttacks(p.Us, pawns);
        }

        else if (movedPt == PieceType.Pawn)
        {
            UpdatePieceThreats(ref p, p.Us, PieceType.Knight);
        }

        else if (movedPt == PieceType.Pawn)
        {
            UpdatePieceThreats(ref p, p.Us, PieceType.King);
        }

        // do the same for the captured piecetype
        // the captured piece type may be PieceType.NONE

        if (movedPt == PieceType.Pawn)
        {
            ulong pawns = p.GetPieces(p.Them, PieceType.Pawn);
            this[p.Them, PieceType.Pawn] = Attacks.LeftPawnMassAttacks(p.Them, pawns) | Attacks.RightPawnMassAttacks(p.Them, pawns);
        }

        else if (movedPt == PieceType.Knight)
        {
            UpdatePieceThreats(ref p, p.Them, PieceType.Knight);
        }

        else if (movedPt == PieceType.King)
        {
            UpdatePieceThreats(ref p, p.Them, PieceType.King);
        }

        // if the piece was a sliding piece (bishopr, rook, queen)
        // or its movement interacted with other sliders line of sight
        // update that threat map because sliders may now or no
        // longer be blocked

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Bishop; pt <= PieceType.Queen; pt++)
            {
                if ((this[c, pt] & mask) != 0
                    || movedPt == pt && p.Us == c
                    || capturedPt == pt && p.Them == c)
                {
                    UpdatePieceThreats(ref p, c, pt);
                }
            }
        }

        // update the total siede Attack maps

        this[Color.White] = this[Color.White, PieceType.Pawn]
            | this[Color.White, PieceType.Knight]
            | this[Color.White, PieceType.Bishop]
            | this[Color.White, PieceType.Rook]
            | this[Color.White, PieceType.Queen]
            | this[Color.White, PieceType.King];

        this[Color.Black] = this[Color.Black, PieceType.Pawn]
            | this[Color.Black, PieceType.Knight]
            | this[Color.Black, PieceType.Bishop]
            | this[Color.Black, PieceType.Rook]
            | this[Color.Black, PieceType.Queen]
            | this[Color.Black, PieceType.King];
    }

    /// <summary>
    /// Update a single Threat map
    /// for a PieceType and Color
    /// Cannot be used for Pawns
    /// </summary>
    private unsafe void UpdatePieceThreats(ref Pos p, Color c, PieceType pt)
    {
        Debug.Assert(pt != PieceType.Pawn);

        ulong pieces = p.GetPieces(c, pt);
        while (pieces != 0)
        {
            int sq = Utils.popLsb(ref pieces);
            ulong attack = Attacks.PieceAttacks(pt, sq, p.blocker);
            this[c, pt] |= attack;
        }
    }

}