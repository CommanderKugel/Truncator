
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

    public unsafe fixed int flip[2];
    public unsafe fixed int bucket[2];

    public bool needsUpdate;
    public bool needsRefresh;


    public unsafe Accumulator()
    {
        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);

        flip[0] = 0;
        flip[1] = 0;

        bucket[0] = 0;
        bucket[1] = 0;

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

        flip[(int)Color.White] = GetFlip(p.KingSquares[(int)Color.White]);
        flip[(int)Color.Black] = GetFlip(p.KingSquares[(int)Color.Black]);

        bucket[(int)Color.White] = GetBucket(Color.White, p.KingSquares[(int)Color.White], flip[(int)Color.White]);
        bucket[(int)Color.Black] = GetBucket(Color.Black, p.KingSquares[(int)Color.Black], flip[(int)Color.Black]);

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

        needsUpdate = false;
        needsRefresh = false;
    }


    /// <summary>
    /// ~makes a move
    /// add and subtract features UE style to/from this accumulator
    /// </summary>
    public unsafe void Update(Node* parent, ref Pos p)
    {
        Debug.Assert(!parent->acc.needsUpdate);
        Debug.Assert(!parent->acc.needsRefresh);
        Debug.Assert(parent->acc.WhiteAcc != null);
        Debug.Assert(parent->acc.BlackAcc != null);

        Color Us = p.Them;
        Color Them = p.Us;

        PieceType pt = parent->MovedPieceType;
        PieceType vict = parent->CapturedPieceType;

        Move m = parent->move;
        int from = m.from;
        int to = m.to;

        // assume the king stays in its bucket -> merged efficient updates (UE)

        flip[(int)Color.White] = parent->acc.flip[(int)Color.White];
        flip[(int)Color.Black] = parent->acc.flip[(int)Color.Black];

        bucket[(int)Color.White] = parent->acc.bucket[(int)Color.White];
        bucket[(int)Color.Black] = parent->acc.bucket[(int)Color.Black];

        Debug.Assert(flip[(int)Color.White] == GetFlip(p.KingSquares[(int)Color.White]));
        Debug.Assert(flip[(int)Color.Black] == GetFlip(p.KingSquares[(int)Color.Black]));
        Debug.Assert(bucket[(int)Color.White] == GetBucket(Color.White, p.KingSquares[(int)Color.White], flip[(int)Color.White]));
        Debug.Assert(bucket[(int)Color.Black] == GetBucket(Color.Black, p.KingSquares[(int)Color.Black], flip[(int)Color.Black]));

        if (m.IsNull)
        {
            parent->acc.CopyTo(ref this);
        }
        else if (m.IsCastling)
        {
            int idx = Castling.GetCastlingIdx(Us, from < to);
            AddAddSubSub(
                ref parent->acc,
                Us, PieceType.King, Castling.KingDestinations[idx],
                Us, PieceType.Rook, Castling.RookDestinations[idx],
                Us, PieceType.King, from,
                Us, PieceType.Rook, to
            );
        }
        else if (vict != PieceType.NONE)
        {
            AddSubSub(
                ref parent->acc,
                Us, m.IsPromotion ? m.PromoType : pt, to,
                Us, pt, from,
                Them, vict, !m.IsEnPassant ? to : (Us == Color.White ? to - 8 : to + 8)
            );
        }
        else
        {
            AddSub(
                ref parent->acc,
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
    public static unsafe void DoLazyUpdates(Node* n)
    {
        // if current accumulator needs a full refresh
        // refresh it and skip all this

        if (n->acc.needsRefresh)
        {
            n->acc.Accumulate(ref n->p);
            return;
        }
        
        // if current accumulator does not need updates, skip all this

        if (!n->acc.needsUpdate)
        {
            return;            
        }

        // find last nodes accumulator does not need an update/refresh

        Node* m = n - 1;
        while (m->acc.needsUpdate && !m->acc.needsRefresh)
        {
            m--;
        }

        if (m->acc.needsRefresh)
        {
            // if last useful accumulator needs a full refresh, 
            // just refresh the current one instead

            n->acc.Accumulate(ref n->p);
        }
        else
        {
            // incremental updates until current accumulator is up-to-date

            while (m != n)
            {
                m++;
                m->acc.Update(m - 1, ref m->p);
            }
        }
    }


    public static int GetFlip(int ksq)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        return Utils.FileOf(ksq) > 3 ? 7 : 0;
    }

    public static int GetBucket(Color c, int ksq, int flip)
    {
        Debug.Assert(c != Color.NONE);
        return KingBucketsLayout[ksq ^ flip ^ ((int)c * 56)];
    }


    public unsafe (int, int) GetFeatureIdx(Color c, PieceType pt, int sq)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);
        Debug.Assert(bucket[(int)Color.White] >= 0 && bucket[(int)Color.White] < INPUT_BUCKETS);
        Debug.Assert(bucket[(int)Color.Black] >= 0 && bucket[(int)Color.Black] < INPUT_BUCKETS);

        int w = bucket[(int)Color.White] * 768 + (int)c * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.White]);
        int b = bucket[(int)Color.Black] * 768 + (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.Black] ^ 56);
        return (w, b);
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

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L2_SIZE % vecSize == 0);

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(l1_weight + widx * L2_SIZE + node);
            var bWeight = Vector.Load(l1_weight + bidx * L2_SIZE + node);

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

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);

        int vecSize = Vector256<short>.Count;
        Debug.Assert(L2_SIZE % vecSize == 0);

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(l1_weight + widx * L2_SIZE + node);
            var bWeight = Avx.LoadAlignedVector256(l1_weight + bidx * L2_SIZE + node);

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

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L2_SIZE % vecSize == 0);

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Vector.Load(WhiteAcc + node);
            var bacc = Vector.Load(BlackAcc + node);

            var wWeight = Vector.Load(l1_weight + widx * L2_SIZE + node);
            var bWeight = Vector.Load(l1_weight + bidx * L2_SIZE + node);

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

        var (widx, bidx) = GetFeatureIdx(c, pt, sq);

        int vecSize = Vector256<short>.Count;
        Debug.Assert(L2_SIZE % vecSize == 0);

        for (int node = 0; node < L2_SIZE; node += vecSize)
        {
            var wacc = Avx.LoadAlignedVector256(WhiteAcc + node);
            var bacc = Avx.LoadAlignedVector256(BlackAcc + node);

            var wWeight = Avx.LoadAlignedVector256(l1_weight + widx * L2_SIZE + node);
            var bWeight = Avx.LoadAlignedVector256(l1_weight + bidx * L2_SIZE + node);

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

        child.flip[(int)Color.White] = flip[(int)Color.White];
        child.flip[(int)Color.Black] = flip[(int)Color.Black];

        child.bucket[(int)Color.White] = bucket[(int)Color.White];
        child.bucket[(int)Color.Black] = bucket[(int)Color.Black];
    }

    /// <summary>
    /// fill accumulator with zeros
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(WhiteAcc != null);
        Debug.Assert(BlackAcc != null);

        NativeMemory.Clear(WhiteAcc, sizeof(short) * L2_SIZE);
        NativeMemory.Clear(BlackAcc, sizeof(short) * L2_SIZE);

        flip[(int)Color.White] = 0;
        flip[(int)Color.Black] = 0;

        bucket[(int)Color.White] = 0;
        bucket[(int)Color.Black] = 0;
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
        if (other.flip[(int)Color.White] != flip[(int)Color.White])
            throw new Exception($"wflip {other.flip[(int)Color.White]} != {flip[(int)Color.White]}");

        if (other.flip[(int)Color.Black] != flip[(int)Color.Black])
            throw new Exception($"bflip {other.flip[(int)Color.Black]} != {flip[(int)Color.Black]}");;

        if (other.bucket[(int)Color.White] != bucket[(int)Color.White])
            throw new Exception($"wbuck {other.bucket[(int)Color.White]} != {bucket[(int)Color.White]}");;

        if (other.bucket[(int)Color.Black] != bucket[(int)Color.Black])
            throw new Exception($"bbuck {other.bucket[(int)Color.Black]} != {bucket[(int)Color.Black]}");;

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
