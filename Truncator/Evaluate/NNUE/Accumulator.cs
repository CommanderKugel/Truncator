
using static Params;
using static Weights;

public struct Accumulator
{

    public unsafe short[] WhiteAcc = new short[L1_SIZE];
    public unsafe short[] BlackAcc = new short[L1_SIZE];


    public unsafe Accumulator(ref Pos p)
    {
        WhiteAcc = new short[L1_SIZE];
        BlackAcc = new short[L1_SIZE];

        AccumulateFromZero(ref p);
    }

    public unsafe void AccumulateFromZero(ref Pos p)
    {
        // copy bias
        // also deletes previous data

        Array.Copy(l1_bias, WhiteAcc, L1_SIZE);
        Array.Copy(l1_bias, BlackAcc, L1_SIZE);

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


    public void Clear()
    {
        Array.Clear(WhiteAcc);
        Array.Clear(BlackAcc);
    }
}
