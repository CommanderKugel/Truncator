
using static Weights;
using static Params;

public static class NNUE
{

    public static unsafe int Evaluate(ref Pos p, Node* n)
    {
        bool wtm = p.Us == Color.White;

        var WhiteAcc = wtm ? n->acc.WhiteAcc : n->acc.BlackAcc;
        var BlackAcc = wtm ? n->acc.BlackAcc : n->acc.WhiteAcc;

        // compute hidden layer(s)
        // in this case just one output neuron

        int output = 0;

        for (int i = 0; i < L1_SIZE; i++)
        {
            // activation function SCReLU

            int whiteTemp = (int)Math.Clamp(WhiteAcc[i], (short)0, QA);
            int blackTemp = (int)Math.Clamp(BlackAcc[i], (short)0, QA);
            whiteTemp *= whiteTemp;
            blackTemp *= blackTemp;

            // weighted sum

            output += whiteTemp * l2_weight[i];
            output += blackTemp * l2_weight[i + L1_SIZE];
        }

        // add bias
        // scale 
        // dequantize

        output = (output / QA + l2_bias) * SCALE / QAB;

        //Console.WriteLine($"eval: {output}");

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

}
