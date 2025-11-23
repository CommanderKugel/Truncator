
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static Settings;
using static Weights;

public partial struct Accumulator
{

    public unsafe void AddSub(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {
        Debug.Assert(parent.WhiteAcc != null);
        Debug.Assert(parent.BlackAcc != null);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c1 != Color.NONE);
        Debug.Assert(c2 != Color.NONE);
        Debug.Assert(pt1 != PieceType.NONE);
        Debug.Assert(pt2 != PieceType.NONE);
        Debug.Assert(sq1 >= 0 && sq1 < 64);
        Debug.Assert(sq2 >= 0 && sq2 < 64);
        Debug.Assert(parent.bigNet == bigNet);

        if (Avx2.IsSupported)
            AddSubAvx2(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2);
        else
            AddSubFallback(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2); ;
    }


    private unsafe void AddSubAvx2(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {

        var (wadd, badd) = GetFeatureIdx(c1, pt1, sq1);
        var (wsub, bsub) = GetFeatureIdx(c2, pt2, sq2);
        int vecSize = Vector256<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Avx2.Add(Avx.LoadAlignedVector256(parent.WhiteAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + wadd * weights.l1_size + node));
            var bacc = Avx2.Add(Avx.LoadAlignedVector256(parent.BlackAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + badd * weights.l1_size + node));

            wacc = Avx2.Subtract(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wsub * weights.l1_size + node));
            bacc = Avx2.Subtract(bacc, Avx.LoadAlignedVector256(weights.l1_weight + bsub * weights.l1_size + node));

            Avx.StoreAligned(WhiteAcc + node, wacc);
            Avx.StoreAligned(BlackAcc + node, bacc);
        }
    }

    private unsafe void AddSubFallback(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {

        var (wadd, badd) = GetFeatureIdx(c1, pt1, sq1);
        var (wsub, bsub) = GetFeatureIdx(c2, pt2, sq2);
        int vecSize = Vector<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            // add -> sub -> store
            // ToDo: load from parent-accumulator & avoid previous parent.CopyTo(this)

            var wacc = Vector.Add(Vector.Load(parent.WhiteAcc + node), Vector.Load(weights.l1_weight + wadd * weights.l1_size + node));
            var bacc = Vector.Add(Vector.Load(parent.BlackAcc + node), Vector.Load(weights.l1_weight + badd * weights.l1_size + node));

            wacc = Vector.Subtract(wacc, Vector.Load(weights.l1_weight + wsub * weights.l1_size + node));
            bacc = Vector.Subtract(bacc, Vector.Load(weights.l1_weight + bsub * weights.l1_size + node));

            Vector.Store(wacc, WhiteAcc + node);
            Vector.Store(bacc, BlackAcc + node);
        }
    }


    public unsafe void AddSubSub(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {
        Debug.Assert(parent.bigNet == bigNet);
        Debug.Assert(parent.WhiteAcc != null);
        Debug.Assert(parent.BlackAcc != null);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c1 != Color.NONE);
        Debug.Assert(c2 != Color.NONE);
        Debug.Assert(c3 != Color.NONE);
        Debug.Assert(pt1 != PieceType.NONE);
        Debug.Assert(pt2 != PieceType.NONE);
        Debug.Assert(pt3 != PieceType.NONE);
        Debug.Assert(sq1 >= 0 && sq1 < 64);
        Debug.Assert(sq2 >= 0 && sq2 < 64);
        Debug.Assert(sq3 >= 0 && sq3 < 64);

        if (Avx2.IsSupported)
            AddSubSubAvx2(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3);
        else
            AddSubSubFallback(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3);
    }


    private unsafe void AddSubSubAvx2(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {

        var (wadd, badd) = GetFeatureIdx(c1, pt1, sq1);
        var (wsub1, bsub1) = GetFeatureIdx(c2, pt2, sq2);
        var (wsub2, bsub2) = GetFeatureIdx(c3, pt3, sq3);
        int vecSize = Vector256<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Avx2.Add(Avx.LoadAlignedVector256(parent.WhiteAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + wadd * weights.l1_size + node));
            var bacc = Avx2.Add(Avx.LoadAlignedVector256(parent.BlackAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + badd * weights.l1_size + node));

            wacc = Avx2.Subtract(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wsub1 * weights.l1_size + node));
            bacc = Avx2.Subtract(bacc, Avx.LoadAlignedVector256(weights.l1_weight + bsub1 * weights.l1_size + node));

            wacc = Avx2.Subtract(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wsub2 * weights.l1_size + node));
            bacc = Avx2.Subtract(bacc, Avx.LoadAlignedVector256(weights.l1_weight + bsub2 * weights.l1_size + node));

            Avx.StoreAligned(WhiteAcc + node, wacc);
            Avx.StoreAligned(BlackAcc + node, bacc);
        }
    }

    private unsafe void AddSubSubFallback(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {

        var (wadd, badd) = GetFeatureIdx(c1, pt1, sq1);
        var (wsub1, bsub1) = GetFeatureIdx(c2, pt2, sq2);
        var (wsub2, bsub2) = GetFeatureIdx(c3, pt3, sq3);
        int vecSize = Vector<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Vector.Add(Vector.Load(parent.WhiteAcc + node), Vector.Load(weights.l1_weight + wadd * weights.l1_size + node));
            var bacc = Vector.Add(Vector.Load(parent.BlackAcc + node), Vector.Load(weights.l1_weight + badd * weights.l1_size + node));

            wacc = Vector.Subtract(wacc, Vector.Load(weights.l1_weight + wsub1 * weights.l1_size + node));
            bacc = Vector.Subtract(bacc, Vector.Load(weights.l1_weight + bsub1 * weights.l1_size + node));

            wacc = Vector.Subtract(wacc, Vector.Load(weights.l1_weight + wsub2 * weights.l1_size + node));
            bacc = Vector.Subtract(bacc, Vector.Load(weights.l1_weight + bsub2 * weights.l1_size + node));

            Vector.Store(wacc, WhiteAcc + node);
            Vector.Store(bacc, BlackAcc + node);
        }
    }
    

    public unsafe void AddAddSubSub(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {
        Debug.Assert(parent.bigNet == bigNet);
        Debug.Assert(parent.WhiteAcc != null);
        Debug.Assert(parent.BlackAcc != null);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c1 != Color.NONE);
        Debug.Assert(c2 != Color.NONE);
        Debug.Assert(c3 != Color.NONE);
        Debug.Assert(pt1 != PieceType.NONE);
        Debug.Assert(pt2 != PieceType.NONE);
        Debug.Assert(pt3 != PieceType.NONE);
        Debug.Assert(sq1 >= 0 && sq1 < 64);
        Debug.Assert(sq2 >= 0 && sq2 < 64);
        Debug.Assert(sq3 >= 0 && sq3 < 64);

        if (Avx2.IsSupported)
            AddAddSubSubAvx2(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3, c4, pt4, sq4);
        else
            AddAddSubSubFallback(ref parent, ref weights, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3, c4, pt4, sq4);
    }


    private unsafe void AddAddSubSubAvx2(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {

        var (wadd1, badd1) = GetFeatureIdx(c1, pt1, sq1);
        var (wadd2, badd2) = GetFeatureIdx(c2, pt2, sq2);
        var (wsub1, bsub1) = GetFeatureIdx(c3, pt3, sq3);
        var (wsub2, bsub2) = GetFeatureIdx(c4, pt4, sq4);
        int vecSize = Vector256<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Avx2.Add(Avx.LoadAlignedVector256(parent.WhiteAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + wadd1 * weights.l1_size + node));
            var bacc = Avx2.Add(Avx.LoadAlignedVector256(parent.BlackAcc + node), Avx.LoadAlignedVector256(weights.l1_weight + badd1 * weights.l1_size + node));

            wacc = Avx2.Add(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wadd2 * weights.l1_size + node));
            bacc = Avx2.Add(bacc, Avx.LoadAlignedVector256(weights.l1_weight + badd2 * weights.l1_size + node));

            wacc = Avx2.Subtract(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wsub1 * weights.l1_size + node));
            bacc = Avx2.Subtract(bacc, Avx.LoadAlignedVector256(weights.l1_weight + bsub1 * weights.l1_size + node));

            wacc = Avx2.Subtract(wacc, Avx.LoadAlignedVector256(weights.l1_weight + wsub2 * weights.l1_size + node));
            bacc = Avx2.Subtract(bacc, Avx.LoadAlignedVector256(weights.l1_weight + bsub2 * weights.l1_size + node));

            Avx.StoreAligned(WhiteAcc + node, wacc);
            Avx.StoreAligned(BlackAcc + node, bacc); 
        }
    }

    private unsafe void AddAddSubSubFallback(
        ref Accumulator parent,
        ref Weights weights,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {

        var (wadd1, badd1) = GetFeatureIdx(c1, pt1, sq1);
        var (wadd2, badd2) = GetFeatureIdx(c2, pt2, sq2);
        var (wsub1, bsub1) = GetFeatureIdx(c3, pt3, sq3);
        var (wsub2, bsub2) = GetFeatureIdx(c4, pt4, sq4);
        int vecSize = Vector<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Vector.Add(Vector.Load(parent.WhiteAcc + node), Vector.Load(weights.l1_weight + wadd1 * weights.l1_size + node));
            var bacc = Vector.Add(Vector.Load(parent.BlackAcc + node), Vector.Load(weights.l1_weight + badd1 * weights.l1_size + node));

            wacc = Vector.Add(wacc, Vector.Load(weights.l1_weight + wadd2 * weights.l1_size + node));
            bacc = Vector.Add(bacc, Vector.Load(weights.l1_weight + badd2 * weights.l1_size + node));

            wacc = Vector.Subtract(wacc, Vector.Load(weights.l1_weight + wsub1 * weights.l1_size + node));
            bacc = Vector.Subtract(bacc, Vector.Load(weights.l1_weight + bsub1 * weights.l1_size + node));

            wacc = Vector.Subtract(wacc, Vector.Load(weights.l1_weight + wsub2 * weights.l1_size + node));
            bacc = Vector.Subtract(bacc, Vector.Load(weights.l1_weight + bsub2 * weights.l1_size + node));

            Vector.Store(wacc, WhiteAcc + node);
            Vector.Store(bacc, BlackAcc + node);
        }
    }

}
