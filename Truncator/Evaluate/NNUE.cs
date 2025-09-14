
using static Settings;
using static Weights;

public static class NNUE
{

    public static unsafe int Evaluate(ref Pos p)
    {

        // accumulate feature transformer

        var WhiteAcc = new float[L2_SIZE];
        var BlackAcc = new float[L2_SIZE];

        // copy bias

        for (int node = 0; node < L2_SIZE; node++)
        {
            WhiteAcc[node] = l1_bias[node];
            BlackAcc[node] = l1_bias[node];
        }

        // apply weight for every piece on the board

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);
                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    int widx = (int)c * 384 + (int)pt * 64 + sq;
                    int bidx = (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ 56);

                    for (int node = 0; node < L2_SIZE; node++)
                    {
                        WhiteAcc[node] += l1_weight[widx * L2_SIZE + node];
                        BlackAcc[node] += l1_weight[bidx * L2_SIZE + node];
                    }
                }
            }
        }

        // Perspective: if its blacks turn, swap whites and blacks accumulator

        if (p.Us == Color.Black)
        {
            (WhiteAcc, BlackAcc) = (BlackAcc, WhiteAcc);
        }

        // activate the accumulated values
        // weigh them with L2 weigts
        // sum the accumulated & weighted values

        float output = l2_bias;

        for (int node = 0; node < L2_SIZE; node++)
        {
            float wact = (float)Math.Pow(Math.Clamp(WhiteAcc[node], 0, 1), 2);
            float bact = (float)Math.Pow(Math.Clamp(BlackAcc[node], 0, 1), 2);

            output += wact * l2_weight[node] + bact * l2_weight[node + L2_SIZE];
        }

        // scale the output to approximately centipawns

        int outInt = (int)(output * EVAL_SCALE);
        return Math.Clamp(outInt, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

}
