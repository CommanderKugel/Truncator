
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using static Settings;
using static Weights;

public struct Accumulator : IDisposable
{

    public unsafe short* WhiteAcc = null;
    public unsafe short* BlackAcc = null;

    public int wflip;
    public int bflip;


    public unsafe Accumulator()
    {
        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);

        wflip = 0;
        bflip = 0;
    }


    /// <summary>
    /// accumulate all active features of the given position
    /// features consist of every piece on the board
    /// there are 768 features: 2xColor, 6xPieceType, 64xSquare
    /// every feature can only be activated once
    /// </summary>
    public unsafe void Accumulate(ref Pos p)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);

        Debug.Assert(Utils.popcnt(p.GetPieces(Color.White, PieceType.King)) == 1);
        Debug.Assert(Utils.popcnt(p.GetPieces(Color.Black, PieceType.King)) == 1);
        Debug.Assert(Utils.lsb(p.GetPieces(Color.White, PieceType.King)) == p.KingSquares[(int)Color.White]);
        Debug.Assert(Utils.lsb(p.GetPieces(Color.Black, PieceType.King)) == p.KingSquares[(int)Color.Black]);

        this.wflip = GetFlip(p.KingSquares[(int)Color.White]);
        this.bflip = GetFlip(p.KingSquares[(int)Color.Black]);

        // copy bias
        // implicitly clears accumulator

        NativeMemory.Copy(l1_bias, WhiteAcc, sizeof(short) * L2_SIZE);
        NativeMemory.Copy(l1_bias, BlackAcc, sizeof(short) * L2_SIZE);

        // accumulate weights for every piece on the board

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);
                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    Activate(c, pt, sq);
                }
            }
        }
    }


    public static int GetFlip(int ksq)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        return Utils.FileOf(ksq) > 3 ? 7 : 0;
    }


    /// <summary>
    /// accumulate a newly activated feaure in the accumulator
    /// ~a piece has been placed on the board somewhere
    /// </summary>
    public void Activate(Color c, PieceType pt, int sq)
    {
        if (Avx2.IsSupported)
            ActivateAvx2(c, pt, sq);
        else
            ActivateFallback(c, pt, sq);
    }

    private unsafe void ActivateFallback(Color c, PieceType pt, int sq)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + (sq ^ wflip);
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ bflip ^ 56);

        var wWeightPrt = l1_weight + widx * L2_SIZE;
        var bWeightPrt = l1_weight + bidx * L2_SIZE;

        int vecSize = Vector<short>.Count;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(wWeightPrt + node);
            var bWeight = Vector.Load(bWeightPrt + node);

            Vector.Store(Vector.Add(wacc, wWeight), WhiteAcc + node);
            Vector.Store(Vector.Add(bacc, bWeight), BlackAcc + node);
        }
    }

    private unsafe void ActivateAvx2(Color c, PieceType pt, int sq)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + (sq ^ wflip);
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ bflip ^ 56);

        var wWeightPrt = l1_weight + widx * L2_SIZE;
        var bWeightPrt = l1_weight + bidx * L2_SIZE;

        int vecSize = Vector<short>.Count;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(wWeightPrt + node);
            var bWeight = Avx.LoadAlignedVector256(bWeightPrt + node);

            Avx.StoreAligned(WhiteAcc + node, Avx2.Add(wacc, wWeight));
            Avx.StoreAligned(BlackAcc + node, Avx2.Add(bacc, bWeight));
        }
    }


    /// <summary>
    /// remove the accumulation of a formerly activated feaure from the accumulator
    /// ~a piece has been removed from the board somewhere
    /// </summary>
    public void Deactivate(Color c, PieceType pt, int sq)
    {
        if (Avx2.IsSupported)
            DeactivateAvx2(c, pt, sq);
        else
            DeactivateFallback(c, pt, sq);
    }

    private unsafe void DeactivateFallback(Color c, PieceType pt, int sq)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + (sq ^ wflip);
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ bflip ^ 56);

        var wWeightPrt = l1_weight + widx * L2_SIZE;
        var bWeightPrt = l1_weight + bidx * L2_SIZE;

        int vecSize = Vector<short>.Count;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(wWeightPrt + node);
            var bWeight = Vector.Load(bWeightPrt + node);

            Vector.Store(Vector.Subtract(wacc, wWeight), WhiteAcc + node);
            Vector.Store(Vector.Subtract(bacc, bWeight), BlackAcc + node);
        }
    }

    private unsafe void DeactivateAvx2(Color c, PieceType pt, int sq)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + (sq ^ wflip);
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ bflip ^ 56);

        var wWeightPrt = l1_weight + widx * L2_SIZE;
        var bWeightPrt = l1_weight + bidx * L2_SIZE;

        int vecSize = Vector<short>.Count;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(wWeightPrt + node);
            var bWeight = Avx.LoadAlignedVector256(bWeightPrt + node);

            Avx.StoreAligned(WhiteAcc + node, Avx2.Subtract(wacc, wWeight));
            Avx.StoreAligned(BlackAcc + node, Avx2.Subtract(bacc, bWeight));
        }
    }

    /// <summary>
    /// copy accumulated values to childs White- & BlackAcc
    /// </summary>
    public unsafe void CopyTo(ref Accumulator child)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(child.WhiteAcc != null);
        Debug.Assert(child.BlackAcc != null);

        NativeMemory.Copy(WhiteAcc, child.WhiteAcc, (nuint)sizeof(short) * L2_SIZE);
        NativeMemory.Copy(BlackAcc, child.BlackAcc, (nuint)sizeof(short) * L2_SIZE);

        child.wflip = this.wflip;
        child.bflip = this.bflip;
    }

    /// <summary>
    /// fill accumulator with zeros
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);

        NativeMemory.Clear(WhiteAcc, sizeof(float) * L2_SIZE);
        NativeMemory.Clear(BlackAcc, sizeof(float) * L2_SIZE);

        wflip = 0;
        bflip = 0;
    }

    /// <summary>
    /// free allocated memory
    /// </summary>
    public unsafe void Dispose()
    {
        if (WhiteAcc != null)
        {
            NativeMemory.AlignedFree(WhiteAcc);
            NativeMemory.AlignedFree(BlackAcc);
            WhiteAcc = null;
            BlackAcc = null;
        }
    }


    public unsafe bool EqualContents(ref Accumulator other)
    {
        if (other.wflip != wflip || other.bflip != bflip)
        {
            return false;
        }

        for (int i = 0; i < L2_SIZE; i++)
            {
                if (WhiteAcc[i] != other.WhiteAcc[i] || BlackAcc[i] != other.BlackAcc[i])
                {
                    return false;
                }
            }

        return true;
    }
}
