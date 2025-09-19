
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

using static Settings;
using static Weights;

public struct Accumulator : IDisposable
{

    public unsafe short* WhiteAcc = null;
    public unsafe short* BlackAcc = null;

    public unsafe Accumulator()
    {
        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);
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


    /// <summary>
    /// accumulate a newly activated feaure in the accumulator
    /// ~a piece has been placed on the board somewhere
    /// </summary>
    public unsafe void Activate(Color c, PieceType pt, int sq)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + sq;
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ 56);

        var wWeightPrt = l1_weight + widx * L2_SIZE;
        var bWeightPrt = l1_weight + bidx * L2_SIZE;

        int vecSize = Vector<short>.Count;

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Vector.LoadAligned(WhiteAcc + node);
            var bacc = Vector.LoadAligned(BlackAcc + node);

            var wWeight = Vector.LoadAligned(wWeightPrt + node);
            var bWeight = Vector.LoadAligned(bWeightPrt + node);

            Vector.Store(Vector.Add(wacc, wWeight), WhiteAcc + node);
            Vector.Store(Vector.Add(bacc, bWeight), BlackAcc + node);
        }
    }

    /// <summary>
    /// remove the accumulation of a formerly activated feaure from the accumulator
    /// ~a piece has been removed from the board somewhere
    /// </summary>
    public unsafe void Deactivate(Color c, PieceType pt, int sq)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        int widx = (int)c * 384 + (int)pt * 64 + sq;
        int bidx = ((int)c ^ 1) * 384 + (int)pt * 64 + (sq ^ 56);

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
