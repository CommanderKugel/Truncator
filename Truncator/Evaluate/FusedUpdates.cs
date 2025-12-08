
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static Settings;
using static Weights;

public partial struct Accumulator
{

    public unsafe void AddSub(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(parent[accol] != null);
        Debug.Assert(this[accol] != null);
        Debug.Assert(c1 != Color.NONE);
        Debug.Assert(c2 != Color.NONE);
        Debug.Assert(pt1 != PieceType.NONE);
        Debug.Assert(pt2 != PieceType.NONE);
        Debug.Assert(sq1 >= 0 && sq1 < 64);
        Debug.Assert(sq2 >= 0 && sq2 < 64);

        if (Avx2.IsSupported)
            AddSubAvx2(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2);
        else
            AddSubFallback(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2); ;
    }


    private unsafe void AddSubAvx2(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add = GetFeatureIdx(c1, pt1, sq1, accol);
        var sub = GetFeatureIdx(c2, pt2, sq2, accol);

        for (int node = 0; node < L1_SIZE; node += 16)
        {
            var accval = Avx.LoadAlignedVector256(parent_acc + node);
            var add_val = Avx.LoadAlignedVector256(l0_weight + add * L1_SIZE + node);
            var sub_val = Avx.LoadAlignedVector256(l0_weight + sub * L1_SIZE + node);

            accval = Avx2.Add(accval, add_val);
            accval = Avx2.Subtract(accval, sub_val);
            Avx.StoreAligned(child_acc + node, accval);
        }
    }

    private unsafe void AddSubFallback(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add = GetFeatureIdx(c1, pt1, sq1, accol);
        var sub = GetFeatureIdx(c2, pt2, sq2, accol);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Vector.Load(parent_acc + node);
            var add_val = Vector.Load(l0_weight + add * L1_SIZE + node);
            var sub_val = Vector.Load(l0_weight + sub * L1_SIZE + node);

            accval = Vector.Add(accval, add_val);
            accval = Vector.Subtract(accval, sub_val);
            Vector.Store(accval, child_acc + node);
        }
    }


    public unsafe void AddSubSub(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(parent[accol] != null);
        Debug.Assert(this[accol] != null);
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
            AddSubSubAvx2(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3);
        else
            AddSubSubFallback(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3);
    }


    private unsafe void AddSubSubAvx2(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add = GetFeatureIdx(c1, pt1, sq1, accol);
        var sub1 = GetFeatureIdx(c2, pt2, sq2, accol);
        var sub2 = GetFeatureIdx(c3, pt3, sq3, accol);

        int vecSize = Vector256<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Avx.LoadAlignedVector256(parent_acc + node);
            var add_val = Avx.LoadAlignedVector256(l0_weight + add * L1_SIZE + node);
            var sub_val1 = Avx.LoadAlignedVector256(l0_weight + sub1 * L1_SIZE + node);
            var sub_val2 = Avx.LoadAlignedVector256(l0_weight + sub2 * L1_SIZE + node);

            accval = Avx2.Add(accval, add_val);
            accval = Avx2.Subtract(accval, sub_val1);
            accval = Avx2.Subtract(accval, sub_val2);
            Avx.StoreAligned(child_acc + node, accval);
        }
    }

    private unsafe void AddSubSubFallback(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add = GetFeatureIdx(c1, pt1, sq1, accol);
        var sub1 = GetFeatureIdx(c2, pt2, sq2, accol);
        var sub2 = GetFeatureIdx(c3, pt3, sq3, accol);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Vector.Load(parent_acc + node);
            var add_val = Vector.Load(l0_weight + add * L1_SIZE + node);
            var sub_val1 = Vector.Load(l0_weight + sub1 * L1_SIZE + node);
            var sub_val2 = Vector.Load(l0_weight + sub2 * L1_SIZE + node);

            accval = Vector.Add(accval, add_val);
            accval = Vector.Subtract(accval, sub_val1);
            accval = Vector.Subtract(accval, sub_val2);
            Vector.Store(accval, child_acc + node);
        }
    }
    

    public unsafe void AddAddSubSub(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(parent[accol] != null);
        Debug.Assert(this[accol] != null);
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
            AddAddSubSubAvx2(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3, c4, pt4, sq4);
        else
            AddAddSubSubFallback(accol, ref parent, c1, pt1, sq1, c2, pt2, sq2, c3, pt3, sq3, c4, pt4, sq4);
    }


    private unsafe void AddAddSubSubAvx2(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add1 = GetFeatureIdx(c1, pt1, sq1, accol);
        var add2 = GetFeatureIdx(c2, pt2, sq2, accol);
        var sub1 = GetFeatureIdx(c3, pt3, sq3, accol);
        var sub2 = GetFeatureIdx(c4, pt4, sq4, accol);

        int vecSize = Vector256<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Avx.LoadAlignedVector256(parent_acc + node);
            var add_val1 = Avx.LoadAlignedVector256(l0_weight + add1 * L1_SIZE + node);
            var add_val2 = Avx.LoadAlignedVector256(l0_weight + add2 * L1_SIZE + node);
            var sub_val1 = Avx.LoadAlignedVector256(l0_weight + sub1 * L1_SIZE + node);
            var sub_val2 = Avx.LoadAlignedVector256(l0_weight + sub2 * L1_SIZE + node);

            accval = Avx2.Add(accval, add_val1);
            accval = Avx2.Add(accval, add_val2);
            accval = Avx2.Subtract(accval, sub_val1);
            accval = Avx2.Subtract(accval, sub_val2);
            Avx.StoreAligned(child_acc + node, accval);
        }
    }

    private unsafe void AddAddSubSubFallback(
        Color accol,
        ref Accumulator parent,
        Color c1, PieceType pt1, int sq1,
        Color c2, PieceType pt2, int sq2,
        Color c3, PieceType pt3, int sq3,
        Color c4, PieceType pt4, int sq4)
    {
        var parent_acc = parent[accol];
        var child_acc = this[accol];

        var add1 = GetFeatureIdx(c1, pt1, sq1, accol);
        var add2 = GetFeatureIdx(c2, pt2, sq2, accol);
        var sub1 = GetFeatureIdx(c3, pt3, sq3, accol);
        var sub2 = GetFeatureIdx(c4, pt4, sq4, accol);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Vector.Load(parent_acc + node);
            var add_val1 = Vector.Load(l0_weight + add1 * L1_SIZE + node);
            var add_val2 = Vector.Load(l0_weight + add2 * L1_SIZE + node);
            var sub_val1 = Vector.Load(l0_weight + sub1 * L1_SIZE + node);
            var sub_val2 = Vector.Load(l0_weight + sub2 * L1_SIZE + node);

            accval = Vector.Add(accval, add_val1);
            accval = Vector.Add(accval, add_val2);
            accval = Vector.Subtract(accval, sub_val1);
            accval = Vector.Subtract(accval, sub_val2);
            Vector.Store(accval, child_acc + node);
        }
    }

}
