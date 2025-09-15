
using static Settings;
using static Weights;

public static class NNUE
{

    public static unsafe int Evaluate(ref Pos p, Accumulator acc)
    {
        // Perspective: if its blacks turn, swap whites and blacks accumulator

        bool wtm = p.Us == Color.White;
        short* WhiteAcc = wtm ? acc.WhiteAcc : acc.BlackAcc;
        short* BlackAcc = wtm ? acc.BlackAcc : acc.WhiteAcc;

        // activate the accumulated values
        // weigh them with L2 weigts
        // sum the accumulated & weighted values

        int output = 0;

        for (int node = 0; node < L2_SIZE; node++)
        {
            int wact = Math.Clamp((int)WhiteAcc[node], 0, QA);
            int bact = Math.Clamp((int)BlackAcc[node], 0, QA);
            wact *= wact;
            bact *= bact;

            output += wact * l2_weight[node] + bact * l2_weight[node + L2_SIZE];
        }

        // scale and dequantize

        output = (output / QA + l2_bias) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

}
