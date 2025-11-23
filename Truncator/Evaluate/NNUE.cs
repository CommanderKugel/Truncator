using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Settings;
using static Weights;

public static class NNUE
{
    private static ReadOnlySpan<int> SimpleEvalMaterial => [
        100, 300, 300, 500, 900, 0
    ];


    public static unsafe int Evaluate<node>(ref Pos p, SearchThread thread, bool ignoreSmalNet = false)
        where node : NodeType
    {      
        int simpleEval = GetSimpleEval(ref p);

        bool useSmallnet = Math.Abs(simpleEval) > 900 
            && typeof(node) == typeof(NonPVNode)
            && !ignoreSmalNet;

        Node* n = &thread.nodeStack[thread.ply];

        // accumulate non root nodes

        if (typeof(node) != typeof(RootNode))
        {
            Accumulator.DoLazyUpdates(n, !useSmallnet);
        }

        // now accumulate

        int eval = Evaluate(ref p, useSmallnet ? n->smolAcc : n->bigAcc);

        // re-evaluate bignet sometimes?
        // material-/50mr scaling?

        return eval;
    }


    private static int GetSimpleEval(ref Pos p)
    {
        int simpleEval = SimpleEvalMaterial[(int)PieceType.Pawn] * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Pawn)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Pawn)))
            + SimpleEvalMaterial[(int)PieceType.Knight] * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Knight)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Knight)))
            + SimpleEvalMaterial[(int)PieceType.Bishop] * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Bishop)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Bishop)))
            + SimpleEvalMaterial[(int)PieceType.Rook] * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Rook)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Rook)))
            + SimpleEvalMaterial[(int)PieceType.Queen] * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Queen)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Queen)));
        return p.Us == Color.White ? simpleEval : -simpleEval;
    }

    private static int Evaluate(ref Pos p, Accumulator acc)
    {
        ref var weights = ref acc.bigNet ? ref BigNetWeights : ref SmallNetWeights;
        return Avx2.IsSupported ? EvaluateAvx2(ref p, acc, ref weights) : EvaluateFallback(ref p, acc, ref weights);
    }

    private static unsafe int EvaluateFallback(ref Pos p, Accumulator acc, ref Weights weights)
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

        var wWeightPtr = weights.l2_weight + outputBucket * weights.l1_size * 2;
        var bWeightPrt = weights.l2_weight + outputBucket * weights.l1_size * 2 + weights.l1_size;

        for (int node = 0; node < weights.l1_size; node += vecSize)
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

        int output = (Vector.Sum(OutputAccumulator) / QA + weights.l2_bias[outputBucket]) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

    private static unsafe int EvaluateAvx2(ref Pos p, Accumulator acc, ref Weights weights)
    {
        Debug.Assert(Avx2.IsSupported);

        // Perspective: if its blacks turn, swap whites and blacks accumulator

        short* WhiteAcc = p.Us == Color.White ? acc.WhiteAcc : acc.BlackAcc;
        short* BlackAcc = p.Us == Color.White ? acc.BlackAcc : acc.WhiteAcc;

        const int VEC_SIZE = 16; // avx2 uses 256 bit registers
        
        var QAVector = Vector256.Create((short)QA);
        var ZeroVector = Vector256<short>.Zero;

        int outputBucket = GetOutputBucket(ref p);
        var OutputAccumulator = Vector256<int>.Zero;
        
        var wWeightPtr = weights.l2_weight + outputBucket * weights.l1_size * 2;
        var bWeightPrt = weights.l2_weight + outputBucket * weights.l1_size * 2 + weights.l1_size;

        // main accumulation loop

        for (int node = 0; node < weights.l1_size; node += VEC_SIZE)
        {
            // load accumulator into vectors

            var wact = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bact = Avx.LoadAlignedVector256(BlackAcc + node);

            // clamp 

            wact = Avx2.Max(ZeroVector, Avx2.Min(QAVector, Avx.LoadAlignedVector256(WhiteAcc + node)));
            bact = Avx2.Max(ZeroVector, Avx2.Min(QAVector, Avx.LoadAlignedVector256(BlackAcc + node)));

            // weigh
            // 255 * 64 fits into int16, while 255 * 255 does not

            var wWeighted = Avx2.MultiplyLow(wact, Avx.LoadAlignedVector256(wWeightPtr + node));
            var bWeighted = Avx2.MultiplyLow(bact, Avx.LoadAlignedVector256(bWeightPrt + node));

            // now square (255 * 64 fits into short, 255 * 255 * 64 needs to be int)
            // so widen vector into ints
            // after that sum it all up
            // Square -> widen -> sum (2x)
            // can be simplified into
            // MultiplyAddAdjacend -> sum (1x)

            var wSquared = Avx2.MultiplyAddAdjacent(wWeighted, wact);
            var bSquared = Avx2.MultiplyAddAdjacent(bWeighted, bact);

            // save into the evaluation

            OutputAccumulator = Avx2.Add(wSquared, OutputAccumulator);
            OutputAccumulator = Avx2.Add(bSquared, OutputAccumulator);
        }

        // scale and dequantize

        int output = (Vector256.Sum(OutputAccumulator) / QA + weights.l2_bias[outputBucket]) * EVAL_SCALE / (QA * QB);

        return Math.Clamp(output, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUTPUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
