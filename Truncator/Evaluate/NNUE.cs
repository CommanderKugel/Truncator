using System.Numerics;
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

        int vecSize = Vector<short>.Count;

        // store weighted feature transformer values in a vector
        // and only sum them once in the end

        var outputAcc = Vector<int>.Zero;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            // load accumulator into vectors

            Vector<short> wact = Vector.Load(WhiteAcc + node);
            Vector<short> bact = Vector.Load(BlackAcc + node);

            // clamp (first part of the activation function) 

            wact = Vector.Min(new Vector<short>(QA), wact);
            wact = Vector.Max(Vector<short>.Zero, wact);

            bact = Vector.Min(new Vector<short>(QA), bact);
            bact = Vector.Max(Vector<short>.Zero, bact);

            // weigh
            // 255 * 64 fits into int16, while 255 * 255 does not

            var wWeighted = Vector.Multiply(wact, Vector.Load(l2_weight + node));
            var bWeighted = Vector.Multiply(bact, Vector.Load(l2_weight + node + L2_SIZE));

            // split into int-vectors to avoid overflows

            Vector.Widen(wWeighted, out Vector<int> wWeightedLow, out Vector<int> wWeightedHigh);
            Vector.Widen(bWeighted, out Vector<int> bWeightedLow, out Vector<int> bWeightedHigh);

            Vector.Widen(wact, out Vector<int> wactLow, out Vector<int> wactHigh);
            Vector.Widen(bact, out Vector<int> bactLow, out Vector<int> bactHigh);

            // square the not int-vectos (activation function again)

            var wSquaredLow = Vector.Multiply(wWeightedLow, wactLow);
            var wSquaredHigh = Vector.Multiply(wWeightedHigh, wactHigh);
            var bSquaredLow = Vector.Multiply(bWeightedLow, bactLow);
            var bSquaredHigh = Vector.Multiply(bWeightedHigh, bactHigh);

            // store in output accumulator

            var wSquaredSum = Vector.Add(wSquaredHigh, wSquaredLow);
            var bSquaredSum = Vector.Add(bSquaredHigh, bSquaredLow);

            outputAcc = Vector.Add(outputAcc, wSquaredSum);
            outputAcc = Vector.Add(outputAcc, bSquaredSum);
        }

        // scale and dequantize

        int output = (Vector.Sum(outputAcc) / QA + l2_bias) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

}
