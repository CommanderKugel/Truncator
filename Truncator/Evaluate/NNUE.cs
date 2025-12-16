
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

    public const int STEP_F32 = 8;
    public const int STEP_I32 = 8;
    public const int STEP_I16 = 16;
    public const int STEP_I8 = 32;

    public const int BYTE_PER_INT = 4;


    public static unsafe int Evaluate(ref Pos p, Accumulator acc)
    {
        var bucket = GetOutputBucket(ref p);
        var wacc = p.Us == Color.White ? acc.WhiteAcc : acc.BlackAcc;
        var bacc = p.Us == Color.White ? acc.BlackAcc : acc.WhiteAcc;

        Span<byte> l1 = stackalloc byte[L1_SIZE];
        Span<float> l2 = stackalloc float[L2_SIZE];

        fixed (byte* l1ptr = l1)
        fixed (float* l2ptr = l2)
        {
            ActivatePairwiseCrelu(l1ptr, wacc, bacc);
            ComputeL2(l2ptr, l1ptr, bucket);
            ComputeL3(l2ptr, bucket, out int eval);
            
            return Math.Clamp(eval , -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
        }
    }


    public static unsafe void ActivatePairwiseCrelu(byte* l1, short* wacc, short* bacc)
    {
        for (Color c = Color.White; c <= Color.Black; c++)
        {
            var acc = c == Color.White ? wacc : bacc;

            for (int i = 0; i < L1_SIZE / 2; i += STEP_I16)
            {
                int j = i + L1_SIZE / 2;

                var vec_1 = Avx.LoadAlignedVector256(&acc[i]);
                var vec_2 = Avx.LoadAlignedVector256(&acc[j]);

                var clamp_1 = Vector256.Clamp(vec_1, Vector256<short>.Zero, Vector256.Create(QA));
                var clamp_2 = Vector256.Clamp(vec_2, Vector256<short>.Zero, Vector256.Create(QA));

                // we want to compute ((clamp_a * clamp_b) >> SHIFT) but that will probably overflows
                // as 255 * 255 does not fit into a short
                // we need to multiply high which only takes the upper 16 bits of the resulting integer
                // which is effectifely a right shift by 16
                // so we counteract that by  (16 - SHIFT) and get the resullt we want

                var mul = Avx2.MultiplyHigh(Avx2.ShiftLeftLogical(clamp_1, 16 - SHIFT), clamp_2);
                var packed = Sse2.PackUnsignedSaturate(mul.GetLower(), mul.GetUpper());

                Sse2.Store(&l1[i + (int)c * L1_SIZE / 2], packed);

                // find all 4-byte blocks that contain at least one non-zero (nnz) values

                //var nnz_mask = Avx.MoveMask(Avx2.CompareGreaterThan(mul.AsInt32(), Vector256<int>.Zero).AsSingle());
            }
        }
    }


    public static unsafe void ComputeL2(float* l2, byte* l1, int bucket)
    {
        // weigh

        var weight_ptr = &l1_weight[bucket * L1_SIZE * L2_SIZE];
        var acc = stackalloc Vector256<int>[L2_SIZE / STEP_F32];

        for (int l1node = 0; l1node < L1_SIZE / BYTE_PER_INT; l1node++)
        {
            var l1_scalar = ((int*)l1)[l1node];
            var l1vec = Vector256.Create(l1_scalar).AsByte();

            for (int i = 0; i < L2_SIZE * BYTE_PER_INT; i += STEP_I8)
            {
                // weigh accumulated and activated values from from l1
                // compute for chunks of 4 l1-values, so 4xi8 is converted to 1xi32
                // and all fits perfectly into the avx lanes
                // also helps with nnz stuff and float-masks later
                // can be done in one instruction (dpbusd) when using avx512

                var weights_i8 = Avx.LoadAlignedVector256(&weight_ptr[l1node * L2_SIZE * 4 + i]);
                var muladd_i16 = Avx2.MultiplyAddAdjacent(l1vec, weights_i8);
                var muladd_i32 = Avx2.MultiplyAddAdjacent(muladd_i16, Vector256<short>.One);

                var idx = i / (BYTE_PER_INT * STEP_F32);
                acc[idx] = Avx2.Add(muladd_i32, acc[idx]);
            }
        }

        // convert from i32 to f32
        // normalize
        // bias
        // screlu
        
        var normVec = Vector256.Create(L1_NORM);

        for (int i = 0; i < L2_SIZE / STEP_F32; i++)
        {
            var l2Vec = Avx.ConvertToVector256Single(acc[i]); 
            var biasVec = Vector256.LoadAligned(&l1_bias[bucket * L2_SIZE + i * STEP_F32]);
            var fma = Fma.MultiplyAdd(l2Vec, normVec, biasVec);

            var clamped = Vector256.Clamp(fma, Vector256<float>.Zero, Vector256<float>.One);
            var squared = Avx.Multiply(clamped, clamped);

            Avx.Store(&l2[i * STEP_F32], squared);
        }
    }

    public static unsafe void ComputeL3(float* l2, int bucket, out int output)
    {
        var weightPtr = &l2_weight[bucket * L2_SIZE * L3_SIZE];
        var acc = stackalloc Vector256<float>[L3_SIZE / STEP_F32];

        // load bias

        for (int i = 0; i < L3_SIZE / STEP_F32; i++)
        {
            acc[i] = Avx.LoadAlignedVector256(&l2_bias[bucket * L3_SIZE + i * STEP_F32]);
        }

        // sum weights

        for (int l2node = 0; l2node < L2_SIZE; l2node++)
        {
            var l2Vec = Vector256.Create(l2[l2node]);

            for (int l3node = 0; l3node < L3_SIZE / STEP_F32; l3node++)
            {
                var weight = Avx.LoadAlignedVector256(&weightPtr[l2node * L3_SIZE + l3node * STEP_F32]);
                acc[l3node] = Fma.MultiplyAdd(weight, l2Vec, acc[l3node]);
            }
        }

        // screlu
        // out weights

        var outAcc = Vector256<float>.Zero;

        for (int i = 0; i < L3_SIZE / STEP_F32; i++)
        {
            var clamp = Vector256.Clamp(acc[i], Vector256<float>.Zero, Vector256<float>.One);
            var square = Avx.Multiply(clamp, clamp);

            var weight = Vector256.LoadAligned(&l3_weight[bucket * L3_SIZE + i * STEP_F32]);
            outAcc = Fma.MultiplyAdd(square, weight, outAcc);
        }

        // sum 
        // bias
        // scale
        // f32 to i32

        output = (int)((Vector256.Sum(outAcc) + l3_bias[bucket]) * EVAL_SCALE);
    }
    

    public static int GetOutputBucket(ref Pos p)
    {
        const int DIV = (32 + 1) / OUT_BUCKETS;
        return (Utils.popcnt(p.blocker) - 2) / DIV;
    }

}
