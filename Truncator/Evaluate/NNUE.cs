
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Settings;
using static Weights;

public static class NNUE
{
    private const int SHIFT = 9;
    private const float L1_NORM = (float)(1 << SHIFT) / (float)(QA * QA * QB);

    private static readonly Vector256<short> VectorZero = Vector256<short>.Zero;
    private static readonly Vector256<short> VectorQA = Vector256.Create(QA);


    public static unsafe int Evaluate(ref Pos p, Accumulator acc)
    {
        var bucket = GetOutputBucket(ref p);
        var wacc = p.Us == Color.White ? acc.WhiteAcc : acc.BlackAcc;
        var bacc = p.Us == Color.White ? acc.BlackAcc : acc.WhiteAcc;

        Span<short> l1 = stackalloc short[L1_SIZE];
        Span<float> l2 = stackalloc float[L2_SIZE];
        Span<float> l3 = stackalloc float[L3_SIZE];

        fixed (short* l1ptr = l1)
        fixed (float* l2ptr = l2)
        fixed (float* l3ptr = l3)
        {
            ActivatePairwiseCrelu(l1ptr, wacc, bacc);
            ComputeL2(l2ptr, l1ptr, bucket);
            var eval = ComputeL3(l3ptr, l2ptr, bucket);
            
            return Math.Clamp((int)eval , -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
        }
    }


    public static unsafe void ActivatePairwiseCrelu(short* l1, short* wacc, short* bacc)
    {
        var step = Vector256<short>.Count;

        for (int i = 0; i < L1_SIZE / 2; i += step)
        {
            int j = i + L1_SIZE / 2;

            var w_vec_1 = Avx.LoadAlignedVector256(&wacc[i]);
            var w_vec_2 = Avx.LoadAlignedVector256(&wacc[j]);

            var b_vec_1 = Avx.LoadAlignedVector256(&bacc[i]);
            var b_vec_2 = Avx.LoadAlignedVector256(&bacc[j]);

            var w_clamp_1 = Vector256.Clamp(w_vec_1, Vector256<short>.Zero, Vector256.Create(QA));
            var w_clamp_2 = Vector256.Clamp(w_vec_2, Vector256<short>.Zero, Vector256.Create(QA));

            var b_clamp_1 = Vector256.Clamp(b_vec_1, Vector256<short>.Zero, Vector256.Create(QA));
            var b_clamp_2 = Vector256.Clamp(b_vec_2, Vector256<short>.Zero, Vector256.Create(QA));

            var w_mul = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical(w_clamp_1, 16 - SHIFT), w_clamp_2);
            var b_mul = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical(b_clamp_1, 16 - SHIFT), b_clamp_2);

            Avx.Store(&l1[i], w_mul);
            Avx.Store(&l1[j], b_mul);
        }
    }


    public static unsafe void ComputeL2(float* l2, short* l1, int bucket)
    {
        var step = Vector256<float>.Count;

        // weights

        for (int l2node = 0; l2node < L2_SIZE; l2node++)
        {
            for (int l1node = 0; l1node < L1_SIZE; l1node++)
            {
                l2[l2node] += l1[l1node] * l1_weight[bucket * L1_SIZE * L2_SIZE + l1node * L2_SIZE + l2node];
            }
        }

        // normalize
        // bias
        // screlu

        for (int i = 0; i < L2_SIZE; i += step)
        {
            var l2Vec = Vector256.Load(&l2[i]);
            var normVec = Vector256.Create(L1_NORM);
            var biasVec = Vector256.LoadAligned(&l1_bias[bucket * L2_SIZE + i]);

            var fma = Fma.MultiplyAdd(l2Vec, normVec, biasVec);
            var clamped = Vector256.Clamp(fma, Vector256<float>.Zero, Vector256<float>.One);
            var squared = Avx.Multiply(clamped, clamped);

            Avx.Store(&l2[i], squared);
        }
    }

    public static unsafe float ComputeL3(float* l3, float* l2, int bucket)
    {
        var step = Vector256<float>.Count;

        // weights

        var weightPtr = &l2_weight[bucket * L2_SIZE * L3_SIZE];

        for (int l3node = 0; l3node < L3_SIZE; l3node++)
        {
            var l3acc = Vector256<float>.Zero;

            for (int l2node = 0; l2node < L2_SIZE; l2node += step)
            {
                var weight = Avx.LoadAlignedVector256(&weightPtr[l3node * L2_SIZE + l2node]);
                var l2Vec = Avx.LoadVector256(&l2[l2node]);
                
                l3acc = Fma.MultiplyAdd(weight, l2Vec, l3acc);
            }

            l3[l3node] = Vector256.Sum(l3acc);
        }

        // bias
        // screlu
        // out weights

        var outAcc = Vector256<float>.Zero;

        for (int i = 0; i < L3_SIZE; i += step)
        {
            var biasVec = Vector256.LoadAligned(&l2_bias[bucket * L3_SIZE + i]);
            var l3Vec = Vector256.Load(&l3[i]);
            var l3Weight = Vector256.LoadAligned(&l3_weight[bucket * L3_SIZE + i]);

            var sum = Avx.Add(biasVec, l3Vec);
            var clamp = Vector256.Clamp(sum, Vector256<float>.Zero, Vector256<float>.One);
            var square = Avx.Multiply(clamp, clamp);
            
            outAcc = Fma.MultiplyAdd(square, l3Weight, outAcc);
        }

        var output = Vector256.Sum(outAcc) + l3_bias[bucket];

        return output * EVAL_SCALE;
    }
    

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
