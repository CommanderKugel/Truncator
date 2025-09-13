
using static Params;
using static Weights;

public struct Accumulator
{

    public unsafe fixed short WhiteAcc[L1_SIZE];
    public unsafe fixed short BlackAcc[L1_SIZE];


    public unsafe Accumulator(ref Pos p)
    {
        AccumulateFromZero(ref p);
    }

    public unsafe void AccumulateFromZero(ref Pos p)
    {
        // copy bias
        // also deletes previous data

        for (int i = 0; i < L1_SIZE; i++)
        {
            WhiteAcc[i] = l1_bias[i];
            BlackAcc[i] = l1_bias[i];
        }

        // activate for all existing pieces

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);
                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);

                    int widx = (int)c * 384 + (int)pt * 64 + sq;
                    int bidx = (1 - (int)c) * 384 + (int)pt * 64 + (sq ^ 56);

                    for (int i = 0; i < L1_SIZE; i++)
                    {
                        WhiteAcc[i] += l1_weight[widx, i];
                        BlackAcc[i] += l1_weight[bidx, i];
                    }
                }
            }
        }
    }


    public unsafe void Activate(Color c, PieceType pt, int sq)
    {
        int widx = (int)c * 384 + (int)pt * 64 + sq;
        int bidx = (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ 56);

        for (int i = 0; i < L1_SIZE; i++)
        {
            WhiteAcc[i] += l1_weight[widx, i];
            BlackAcc[i] += l1_weight[bidx, i];
        }
    }


    public unsafe void Deactivate(Color c, PieceType pt, int sq)
    {
        int widx = (int)c * 384 + (int)pt * 64 + sq;
        int bidx = (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ 56);

        for (int i = 0; i < L1_SIZE; i++)
        {
            WhiteAcc[i] -= l1_weight[widx, i];
            BlackAcc[i] -= l1_weight[bidx, i];
        }
    }


    public unsafe void Clear()
    {
        for (int i = 0; i < L1_SIZE; i++)
        {
            WhiteAcc[i] = 0;
            BlackAcc[i] = 0;
        }
    }

    public unsafe bool Equals(ref Accumulator other)
    {
        for (int i = 0; i < L1_SIZE; i++)
        {
            if (WhiteAcc[i] != other.WhiteAcc[i] || BlackAcc[i] != other.BlackAcc[i])
            {
                return false;
            }
        }
        return true;
    }
}
