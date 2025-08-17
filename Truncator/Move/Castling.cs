using System.Diagnostics;

public unsafe struct Castling
{

    public static ReadOnlySpan<byte> Masks => [
        0b0001,
        0b0010,
        0b0100,
        0b1000,
    ];

    public static ReadOnlySpan<int> KingDestinations => [
        (int)Square.G1,
        (int)Square.C1,
        (int)Square.G8,
        (int)Square.C8,
    ];

    public static ReadOnlySpan<int> RookDestinations => [
        (int)Square.F1,
        (int)Square.D1,
        (int)Square.F8,
        (int)Square.D8,
    ];

    public static int GetCastlingIdx(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return ((int)c * 2) + (kingside ? 0 : 1);
    }

    public static byte GetCastlingRightMask(Color c, bool kingside) => Masks[GetCastlingIdx(c, kingside)];



    /* Non Static Stuff */

    public unsafe fixed byte modifier[64];
    public unsafe fixed int kingTargets[4];
    public unsafe fixed ulong paths[4] ;
    public unsafe fixed int kingStart[2];

    public unsafe Castling()
    {
        for (int i = 0; i < 64; i++)
        {
            modifier[i] = 0xFF;
        }
    }

    public unsafe void UpdateNewPosition(ref Pos p)
    {
        for (int f = 0; f < 8; f++)
        {
            modifier[f] = 0xF;
            modifier[56 + f] = 0xF;
        }

        kingStart[(int)Color.White] = p.KingSquares[(int)Color.White];
        kingStart[(int)Color.Black] = p.KingSquares[(int)Color.Black];

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            foreach (bool kingside in new bool[] { true, false })
            {
                // skip missing castling rights

                if (!p.HasCastlingRight(c, kingside))
                {
                    continue;
                }

                int idx = GetCastlingIdx(c, kingside);

                // castling is encoded as King takes Rook
                // so make the rook the target for the move-representation

                ulong rooks = p.GetPieces(c, PieceType.Rook) & (c == Color.White ? 0xFFul : 0xFF00_0000_0000_0000ul);
                int target = kingside ? Utils.msb(rooks) : Utils.lsb(rooks);

                kingTargets[idx] = target;

                // when a move is made, modify the castling rights according to the modifiers
                // on the from or to square
                // moving a king or rook or capturing a rook removes the castling right

                modifier[target] &= (byte)~Masks[idx];
                modifier[p.KingSquares[(int)c]] &= (byte)~Masks[idx];

                // save the paths the rooks and kings need to move along
                // compute them independently, because it might get messy for (d)frc positions

                paths[idx] = Utils.GetRay(p.KingSquares[(int)c], KingDestinations[idx])
                    | Utils.GetRay(target, RookDestinations[idx])
                    | (1ul << KingDestinations[idx])
                    | (1ul << RookDestinations[idx]);
            }
        }
    }

    /// <summary>
    /// returns true if the from and to squares match those of a castling move
    /// differentiate between normal chess and (d)frc mode
    /// </summary>
    public unsafe bool IsUCICastlingMove(int from, int to, ref Pos p)
    {
        Debug.Assert(p.HasCastlingRight(p.Us, from < to));

        int idx = GetCastlingIdx(p.Us, from < to);
        int KingEnd = !UCI.IsChess960 ? KingDestinations[idx] : kingTargets[idx];
        int KingStart = kingStart[(int)p.Us];
        return KingStart == from && KingEnd == to;
    }
}