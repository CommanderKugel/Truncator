using static Attacks;

using System.Diagnostics;


public unsafe partial struct Pos
{
    public fixed ulong PieceBB[6];
    public fixed ulong ColorBB[2];

    public fixed int KingSquares[2];

    public byte CastlingRights;
    public int EnPassantSquare;
    public int FiftyMoveRule;

    public Color Us;
    public readonly Color Them => 1 - Us;

    public ulong ZobristKey;
    //public fixed ulong PieceKeys[6];
    //public fixed ulong ColorKeys[2];

    public readonly ulong blocker => ColorBB[0] | ColorBB[1];


    public ulong GetPieces(Color c, PieceType pt)
    {
        Debug.Assert(c < Color.NONE);
        Debug.Assert(pt < PieceType.NONE);
        return ColorBB[(int)c] & PieceBB[(int)pt];
    }

    public ulong GetPieces(Color c, PieceType pt1, PieceType pt2)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt1 != PieceType.NONE && pt2 != PieceType.NONE);
        return ColorBB[(int)c] & (PieceBB[(int)pt1] | PieceBB[(int)pt2]);
    }


    public PieceType PieceTypeOn(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);

        ulong bb = 1ul << sq;
        for (int pt = (int)PieceType.Pawn; pt <= (int)PieceType.King; pt++)
        {
            if ((bb & PieceBB[pt]) != 0)
            {
                return (PieceType)pt;
            }
        }
        return PieceType.NONE;
    }

    public Color ColorOn(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64, $"illegal square {sq}!");

        if (((1ul << sq) & ColorBB[(int)Color.White]) != 0)
        {
            return Color.White;
        }
        if (((1ul << sq) & ColorBB[(int)Color.Black]) != 0)
        {
            return Color.Black;
        }
        return Color.NONE;
    }

    public bool HasCastlingRight(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return (CastlingRights & Castling.GetCastlingRightMask(c, kingside)) != 0;
    }

    public ulong AttackerTo(int sq, ulong block)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        return PawnAttacks(Color.White, sq) & GetPieces(Color.Black, PieceType.Pawn) |
               PawnAttacks(Color.Black, sq) & GetPieces(Color.White, PieceType.Pawn) |
               PieceAttacks(PieceType.Knight, sq, 0) & PieceBB[(int)PieceType.Knight] |
               PieceAttacks(PieceType.Bishop, sq, block) & (PieceBB[(int)PieceType.Bishop] | PieceBB[(int)PieceType.Queen]) |
               PieceAttacks(PieceType.Rook, sq, block) & (PieceBB[(int)PieceType.Rook] | PieceBB[(int)PieceType.Queen]) |
               PieceAttacks(PieceType.King, sq, 0) & PieceBB[(int)PieceType.King];
    }

    public ulong GetCheckers()
        => AttackerTo(KingSquares[(int)Us], blocker) & ColorBB[(int)Them];

    public bool IsInKingsSliderVision(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        int ksq = KingSquares[(int)Us];
        ulong ray = Utils.GetRay(ksq, sq) & ~(1ul << ksq | 1ul << sq);
        return (ray & blocker) == 0;
    }
}
