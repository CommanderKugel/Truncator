
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Settings;
using static Weights;

public partial struct Accumulator : IDisposable
{

    public unsafe short* WhiteAcc = null;
    public unsafe short* BlackAcc = null;

    public int wflip;
    public int bflip;

    public bool needsUpdate;
    public bool needsRefresh;

    public bool bigNet;


    public unsafe Accumulator(bool bigNet)
    {
        this.bigNet = bigNet;

        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * (nuint)(bigNet ? L1_SIZE : L1_SIZE_SMOL), 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * (nuint)(bigNet ? L1_SIZE : L1_SIZE_SMOL), 256);

        wflip = 0;
        bflip = 0;

        needsUpdate = true;
        needsRefresh = true;
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

        ref var weights = ref bigNet ? ref BigNetWeights : ref SmallNetWeights;

        wflip = GetFlip(p.KingSquares[(int)Color.White]);
        bflip = GetFlip(p.KingSquares[(int)Color.Black]);

        // copy bias
        // implicitly clears accumulator

        NativeMemory.Copy(weights.l1_bias, WhiteAcc, sizeof(short) * (nuint)weights.l1_size);
        NativeMemory.Copy(weights.l1_bias, BlackAcc, sizeof(short) * (nuint)weights.l1_size);

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

        needsUpdate = false;
        needsRefresh = false;
    }


    /// <summary>
    /// ~makes a move
    /// add and subtract features UE style to/from this accumulator
    /// </summary>
    public unsafe void Update(Node* parent, ref Pos p, bool bigNet)
    {
        var parentAcc = bigNet ? parent->bigAcc : parent->smolAcc;
        ref var weights = ref (bigNet ? ref BigNetWeights : ref SmallNetWeights);

        Debug.Assert(!parentAcc.needsUpdate);
        Debug.Assert(!parentAcc.needsRefresh);
        Debug.Assert(parentAcc.WhiteAcc != null);
        Debug.Assert(parentAcc.BlackAcc != null);

        Color Us = p.Them;
        Color Them = p.Us;

        PieceType pt = parent->MovedPieceType;
        PieceType vict = parent->CapturedPieceType;

        Move m = parent->move;
        int from = m.from;
        int to = m.to;

        // assume the king stays in its bucket -> merged efficient updates (UE)

        wflip = parentAcc.wflip;
        bflip = parentAcc.bflip;

        if (m.IsNull)
        {
            parentAcc.CopyTo(ref this);
        }
        else if (m.IsCastling)
        {
            int idx = Castling.GetCastlingIdx(Us, from < to);
            AddAddSubSub(
                ref parentAcc,
                ref weights,
                Us, PieceType.King, Castling.KingDestinations[idx],
                Us, PieceType.Rook, Castling.RookDestinations[idx],
                Us, PieceType.King, from,
                Us, PieceType.Rook, to
            );
        }
        else if (vict != PieceType.NONE)
        {
            AddSubSub(
                ref parentAcc,
                ref weights,
                Us, m.IsPromotion ? m.PromoType : pt, to,
                Us, pt, from,
                Them, vict, !m.IsEnPassant ? to : (Us == Color.White ? to - 8 : to + 8)
            );
        }
        else
        {
            AddSub(
                ref parentAcc,
                ref weights,
                Us, m.IsPromotion ? m.PromoType : pt, to,
                Us, pt, from
            );
        }

        needsUpdate = false;
        needsRefresh = false;
    }


    /// <summary>
    /// We waited to make incremental updates to the accumulator for as long as possible
    /// Backtrack to the last usefull accumulator and propagate the updates
    /// upwards until the current accumulator is up-to-date
    /// After fighting with this piece of code for hours, i looked for some help in Lizards's code.
    /// Thanks Liam amd Ciekce!
    /// https://github.com/liamt19/Lizard/blob/9ee703cf9e6befd8d0395f86856afc06472fb082/Logic/NN/Bucketed768.cs#L735
    /// </summary>
    public static unsafe void DoLazyUpdates(Node* n, bool bigNet)
    {
        var currAcc = bigNet ? n->bigAcc : n->smolAcc;

        // if current accumulator needs a full refresh
        // refresh it and skip all this

        if (currAcc.needsRefresh)
        {
            currAcc.Accumulate(ref n->p);
            return;
        }
        
        // if current accumulator does not need updates, skip all this

        if (!currAcc.needsUpdate)
        {
            return;            
        }

        // find last nodes accumulator does not need an update/refresh

        if (bigNet)
        {
            Node* m = n - 1;
            while (m->bigAcc.needsUpdate && !m->bigAcc.needsRefresh)
            {
                m--;
            }

            if (m->bigAcc.needsRefresh)
            {
                n->bigAcc.Accumulate(ref n->p);
            }
            else
            {
                while (m != n)
                {
                    m++;
                    m->bigAcc.Update(m - 1, ref m->p, true);
                }
            }
        }
        else // smolnet
        {
            Node* m = n - 1;
            while (m->smolAcc.needsUpdate && !m->smolAcc.needsRefresh)
            {
                m--;
            }

            if (m->smolAcc.needsRefresh)
            {
                n->smolAcc.Accumulate(ref n->p);
            }
            else
            {
                while (m != n)
                {
                    m++;
                    m->smolAcc.Update(m - 1, ref m->p, false);
                }
            }
        }
    }


    public static int GetFlip(int ksq)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        return Utils.FileOf(ksq) > 3 ? 7 : 0;
    }

    public (int, int) GetFeatureIdx(Color c, PieceType pt, int sq)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);
        int w = (int)c * 384 + (int)pt * 64 + (sq ^ wflip);
        int b = (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ bflip ^ 56);
        return (w, b);
    }


    /// <summary>
    /// accumulate a newly activated feaure in the accumulator
    /// ~a piece has been placed on the board somewhere
    /// </summary>
    public void Activate(Color c, PieceType pt, int sq)
    {
        ref var weights = ref (bigNet ? ref BigNetWeights : ref SmallNetWeights);

        if (Avx2.IsSupported)
            ActivateAvx2(c, pt, sq, ref weights);
        else
            ActivateFallback(c, pt, sq, ref weights);
    }

    private unsafe void ActivateFallback(Color c, PieceType pt, int sq, ref Weights weights)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);
        int vecSize = Vector<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(weights.l1_weight + widx * weights.l1_size + node);
            var bWeight = Vector.Load(weights.l1_weight + bidx * weights.l1_size + node);

            Vector.Store(Vector.Add(wacc, wWeight), WhiteAcc + node);
            Vector.Store(Vector.Add(bacc, bWeight), BlackAcc + node);
        }
    }

    private unsafe void ActivateAvx2(Color c, PieceType pt, int sq, ref Weights weights)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);
        int vecSize = Vector256<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(weights.l1_weight + widx * weights.l1_size + node);
            var bWeight = Avx.LoadAlignedVector256(weights.l1_weight + bidx * weights.l1_size + node);

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
        ref var weights = ref (bigNet ? ref BigNetWeights : ref SmallNetWeights);

        if (Avx2.IsSupported)
            DeactivateAvx2(c, pt, sq, ref weights);
        else
            DeactivateFallback(c, pt, sq, ref weights);
    }

    private unsafe void DeactivateFallback(Color c, PieceType pt, int sq, ref Weights weights)
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);
        int vecSize = Vector<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(weights.l1_weight + widx * weights.l1_size + node);
            var bWeight = Vector.Load(weights.l1_weight + bidx * weights.l1_size + node);

            Vector.Store(Vector.Subtract(wacc, wWeight), WhiteAcc + node);
            Vector.Store(Vector.Subtract(bacc, bWeight), BlackAcc + node);
        }
    }

    private unsafe void DeactivateAvx2(Color c, PieceType pt, int sq, ref Weights weights)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);
        int vecSize = Vector256<short>.Count;

        for (int node = 0; node < weights.l1_size; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(weights.l1_weight + widx * weights.l1_size + node);
            var bWeight = Avx.LoadAlignedVector256(weights.l1_weight + bidx * weights.l1_size + node);

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
        Debug.Assert(child.bigNet == bigNet);

        var size = (nuint)(bigNet ? L1_SIZE : L1_SIZE_SMOL);

        NativeMemory.Copy(WhiteAcc, child.WhiteAcc, (nuint)sizeof(short) * size);
        NativeMemory.Copy(BlackAcc, child.BlackAcc, (nuint)sizeof(short) * size);

        child.wflip = wflip;
        child.bflip = bflip;
    }

    /// <summary>
    /// fill accumulator with zeros
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);

        var size = (nuint)(bigNet ? L1_SIZE : L1_SIZE_SMOL);

        NativeMemory.Clear(WhiteAcc, sizeof(short) * size);
        NativeMemory.Clear(BlackAcc, sizeof(short) * size);

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
        if (bigNet != other.bigNet)
        {
            return false;
        }

        if (other.wflip != wflip || other.bflip != bflip)
        {
            return false;
        }

        for (int i = 0; i < (bigNet ? L1_SIZE : L1_SIZE_SMOL); i++)
        {
            if (WhiteAcc[i] != other.WhiteAcc[i] || BlackAcc[i] != other.BlackAcc[i])
            {
                return false;
            }
        }

        return true;
    }
}
