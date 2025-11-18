
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

        Span<sbyte> l1 = stackalloc sbyte[L1_SIZE];
        Span<float> l2 = stackalloc float[L2_SIZE];
        Span<float> l3 = stackalloc float[L3_SIZE];

        fixed (sbyte* l1ptr = l1)
        fixed (float* l2ptr = l2)
        fixed (float* l3ptr = l3)
        {
            ActivatePairwiseCrelu(l1ptr, wacc, bacc);
            ComputeL2(l2ptr, l1ptr, bucket);
            var eval = ComputeL3(l3ptr, l2ptr, bucket);
            
            return Math.Clamp((int)eval , -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
        }
    }


    public static unsafe void ActivatePairwiseCrelu(sbyte* l1, short* wacc, short* bacc)
    {
        for (int i = 0; i < L1_SIZE / 2; i++)
        {
            int j = i + L1_SIZE / 2;

            var w_clamp_1 = Math.Clamp(wacc[i], (short)0, QA);
            var w_clamp_2 = Math.Clamp(wacc[j], (short)0, QA);

            var b_clamp_1 = Math.Clamp(bacc[i], (short)0, QA);
            var b_clamp_2 = Math.Clamp(bacc[j], (short)0, QA);

            l1[i] = (sbyte)((w_clamp_1 * w_clamp_2) >> SHIFT);
            l1[j] = (sbyte)((b_clamp_1 * b_clamp_2) >> SHIFT);
        }
    }


    public static unsafe void ComputeL2(float* l2, sbyte* l1, int bucket)
    {
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

        for (int i = 0; i < L2_SIZE; i++)
        {
            l2[i] = l2[i] * L1_NORM + l1_bias[bucket * L2_SIZE + i];
            l2[i] = Math.Clamp(l2[i], 0, 1);
            l2[i] *= l2[i];
        }
    }

    public static unsafe float ComputeL3(float* l3, float* l2, int bucket)
    {
        // weights

        for (int l3node = 0; l3node < L3_SIZE; l3node++)
        {
            for (int l2node = 0; l2node < L2_SIZE; l2node++)
            {
                l3[l3node] += l2[l2node] * l2_weight[bucket * L2_SIZE * L3_SIZE + l2node * L3_SIZE + l3node];
            }
        }

        float output = l3_bias[bucket];

        // bias
        // screlu
        // out weights

        for (int i = 0; i < L3_SIZE; i++)
        {
            l3[i] += l2_bias[bucket * L3_SIZE + i];
            l3[i] = Math.Clamp(l3[i], 0, 1);
            output += l3[i] * l3[i] * l3_weight[bucket * L3_SIZE + i];
        }

        return output * EVAL_SCALE;
    }
    

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
