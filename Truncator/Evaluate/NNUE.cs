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

        int output = 0;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            // load accumulator into vectors

            Vector<short> wact = Vector.Load(WhiteAcc + node);
            Vector<short> bact = Vector.Load(BlackAcc + node);

            // clamp 

            wact = Vector.Min(new Vector<short>(QA), wact);
            wact = Vector.Max(Vector<short>.Zero, wact);
            
            bact = Vector.Min(new Vector<short>(QA), bact);
            bact = Vector.Max(Vector<short>.Zero, bact);

            // weigh
            // 255 * 64 fits into int16, while 255 * 255 does not

            var wWeighted = Vector.Multiply(wact, Vector.Load(l2_weight + outputBucket * L2_SIZE * 2 + node));
            var bWeighted = Vector.Multiply(bact, Vector.Load(l2_weight + outputBucket * L2_SIZE * 2 + node + L2_SIZE));

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

            output += Vector.Sum(Vector.Add(
                Vector.Add(wSquaredLow, wSquaredHigh),
                Vector.Add(bSquaredLow, bSquaredHigh))
            );
        }

        // scale and dequantize

        output = (output / QA + l2_bias[outputBucket]) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUTPUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
