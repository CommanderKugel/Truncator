
using static Settings;
using static Weights;

public static class NNUE
{

    public static unsafe int Evaluate(ref Pos p)
    {

        Accumulator acc = new();
        acc.Accumulate(ref p);

        // Perspective: if its blacks turn, swap whites and blacks accumulator

        bool wtm = p.Us == Color.White;
        float* WhiteAcc = wtm ? acc.WhiteAcc : acc.BlackAcc;
        float* BlackAcc = wtm ? acc.BlackAcc : acc.WhiteAcc;

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
