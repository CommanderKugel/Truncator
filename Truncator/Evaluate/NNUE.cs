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
        // sum the accumulated & weighted values

        int outputBucket = GetOutputBucket(ref p);
        int vecSize = Vector<short>.Count;

        var OutputAccumulator = Vector<int>.Zero;
        var QAVector = new Vector<short>(QA);
        var ZeroVector = Vector<short>.Zero;

        var wWeightPtr = l2_weight + outputBucket * L2_SIZE * 2;
        var bWeightPrt = l2_weight + outputBucket * L2_SIZE * 2 + L2_SIZE;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            // load accumulator into vectors

            Vector<short> wact = Vector.LoadAligned(WhiteAcc + node);
            Vector<short> bact = Vector.LoadAligned(BlackAcc + node);

            // clamp 

            wact = Vector.Min(QAVector, Vector.Max(ZeroVector, wact));
            bact = Vector.Min(QAVector, Vector.Max(ZeroVector, bact));

            // weigh
            // 255 * 64 fits into int16, while 255 * 255 does not

            var wWeighted = Vector.Multiply(wact, Vector.LoadAligned(wWeightPtr + node));
            var bWeighted = Vector.Multiply(bact, Vector.LoadAligned(bWeightPrt + node));

            // split into int-vectors to avoid overflows

            Vector.Widen(wWeighted, out Vector<int> wWeightedLow, out Vector<int> wWeightedHigh);
            Vector.Widen(bWeighted, out Vector<int> bWeightedLow, out Vector<int> bWeightedHigh);

            Vector.Widen(wact, out Vector<int> wactLow, out Vector<int> wactHigh);
            Vector.Widen(bact, out Vector<int> bactLow, out Vector<int> bactHigh);


            // square (activation function again)

            var wSquaredLow = Vector.Multiply(wWeightedLow, wactLow);
            var wSquaredHigh = Vector.Multiply(wWeightedHigh, wactHigh);
            var bSquaredLow = Vector.Multiply(bWeightedLow, bactLow);
            var bSquaredHigh = Vector.Multiply(bWeightedHigh, bactHigh);

            // sum it up

            var wSum = Vector.Add(wSquaredHigh, wSquaredLow);
            var bSum = Vector.Add(bSquaredHigh, bSquaredLow);
            var sum = Vector.Add(wSum, bSum);
            OutputAccumulator = Vector.Add(sum, OutputAccumulator);
        }

        // scale and dequantize

        int output = (Vector.Sum(OutputAccumulator) / QA + l2_bias[outputBucket]) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUTPUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
