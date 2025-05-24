
using System.Diagnostics;

public struct Move
{

    public readonly ushort value = 0;

    public Move(ushort value_)
    {
        value = value_;
    }

    public Move(int from, int to)
    {
        value = (ushort)(from | (to << 6));
    }

    public Move(int from, int to, MoveFlag flag)
    {
        value = (ushort)(from | (to << 6) | (ushort)flag);
    }

    public Move(Square from, Square to, MoveFlag flag = MoveFlag.Normal)
    {
        value = (ushort)((int)from | ((int)to << 6) | (ushort)flag);
    }


    public static bool operator ==(Move lhs, Move rhs) => lhs.value == rhs.value;
    public static bool operator !=(Move lhs, Move rhs) => lhs.value != rhs.value;

    public readonly bool IsNull => value == 0;
    public readonly bool NotNull => value != 0;

    public readonly int from => value & 0b0011_1111;
    public readonly int to => (value >> 6) & 0b0011_1111;
    public readonly MoveFlag flag => (MoveFlag)(value & 0b1100_0000_0000_0000);

    public readonly bool IsCastling => flag == MoveFlag.Castling;
    public readonly bool IsEnPassant => flag == MoveFlag.EnPassant;
    public readonly bool IsPromotion => flag == MoveFlag.Promotion;

    public readonly PieceType PromoType => (PieceType)(((value >> 12) & 0b11) + 1);

    public static readonly Move NullMove = new((ushort)0);

    public override string ToString()
    {
        int target = IsCastling && !UCI.IsChess960 ? CastlingDestination() : to;
        var val = Utils.SquareToString(from) + Utils.SquareToString(target);
        return IsPromotion ? val + ".nbrq."[(int)PromoType] : val;
    }

    private readonly int CastlingDestination() => (Utils.RankOf(from) == 7 ? 56 : 0) ^ (from < to ? (int)Square.G1 : (int)Square.C1);


    public unsafe Move(string movestr, ref Pos p)
    {
        Debug.Assert(movestr.Length == 4 || movestr.Length == 5 && "nbrq".Contains(movestr[4]), "invalid movestring!");
        int from = Utils.LetterToFile(movestr[0]) + 8 * Utils.NumberToRank(movestr[1]);
        int to = Utils.LetterToFile(movestr[2]) + 8 * Utils.NumberToRank(movestr[3]);

        PieceType pt = p.PieceTypeOn(from);
        Debug.Assert(pt != PieceType.NONE);

        MoveFlag myFlag = MoveFlag.Normal;

        if (movestr.Length == 5)
        {
            myFlag = movestr[4] switch
            {
                'n' => MoveFlag.KnightPromo,
                'b' => MoveFlag.BishopPromo,
                'r' => MoveFlag.RookPromo,
                'q' => MoveFlag.QueenPromo,
                _ => throw new Exception("invalid movestring!"),
            };
        }

        else if (pt == PieceType.King &&
                (UCI.IsChess960 ?
                    (to == Castling.kingTargets[2 * (int)p.Us] || to == Castling.kingTargets[1 + 2 * (int)p.Us]) :
                    (to == ((int)Square.G1 ^ 56 * (int)p.Us) || to == ((int)Square.C1 ^ 56 * (int)p.Us))
                ) && (
                   from == Castling.kingTargets[4 + (int)p.Us]
                ))
        {
            myFlag = MoveFlag.Castling;
            to = Castling.GetKingCastlingTarget(p.Us, from < to);
        }

        else if (pt == PieceType.Pawn && p.EnPassantSquare != (int)Square.NONE && to == (p.EnPassantSquare + (p.Us == Color.White ? 8 : -8)))
        {
            myFlag = MoveFlag.EnPassant;
        }

        this.value = (ushort)(from | (to << 6) | (ushort)myFlag);
    }
    
}

public enum MoveFlag : ushort
{
    Normal = 0b0000_0000_0000_0000,
    Castling = 0b0100_0000_0000_0000,
    EnPassant = 0b1000_0000_0000_0000,
    Promotion = 0b1100_0000_0000_0000,

    KnightPromo = 0b1100_0000_0000_0000,
    BishopPromo = 0b1101_0000_0000_0000,
    RookPromo = 0b1110_0000_0000_0000,
    QueenPromo = 0b1111_0000_0000_0000,
}
