
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using static Settings;
using static Weights;

public partial struct Accumulator : IDisposable
{

    private unsafe short* WhiteAcc = null;
    private unsafe short* BlackAcc = null;

    public readonly unsafe short* this[Color c]
    {
        get
        {
            Debug.Assert(c != Color.NONE);
            return c == Color.White ? WhiteAcc : BlackAcc;
        }
    }

    public unsafe fixed int flip[2];
    public unsafe fixed bool needsUpdate[2];
    public unsafe fixed bool needsRefresh[2];


    public unsafe Accumulator()
    {
        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);

        flip[(int)Color.White] = 0;
        flip[(int)Color.Black] = 0;

        needsUpdate[(int)Color.White] = true;
        needsUpdate[(int)Color.Black] = true;

        needsRefresh[(int)Color.White] = true;
        needsRefresh[(int)Color.Black] = true;
    }

    public unsafe void Kill()
    {
        for (Color c = Color.White; c <= Color.Black; c++)
        {
            flip[(int)c] = 0;
            needsUpdate[(int)c] = false;
            needsRefresh[(int)c] = false;
            NativeMemory.AlignedFree(this[c]);
        }

        WhiteAcc = BlackAcc = null;
    }


    /// <summary>
    /// accumulate all active features of the given position
    /// features consist of every piece on the board
    /// there are 768 features: 2xColor, 6xPieceType, 64xSquare
    /// every feature can only be activated once
    /// </summary>
    public void Accumulate(ref Pos p)
    {
        Accumulate(ref p, Color.White);
        Accumulate(ref p, Color.Black);   
    } 

    public unsafe void Accumulate(ref Pos p, Color accol)
    {
        Debug.Assert(this[accol] != null);

        flip[(int)accol] = GetFlip(p.KingSquares[(int)accol]);

        // copy bias
        // implicitly clears accumulator

        NativeMemory.Copy(l0_bias, this[accol], sizeof(short) * L1_SIZE);

        // accumulate weights for every piece on the board

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);
                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    Activate(c, pt, sq, accol);
                }
            }
        }

        needsUpdate[(int)accol] = false;        
        needsRefresh[(int)accol] = false;
    }


    /// <summary>
    /// ~makes a move
    /// add and subtract features UE style to/from this accumulator
    /// </summary>
    public unsafe void Update(Node* parent, ref Pos p, Color accol)
    {
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(!parent->acc.needsUpdate[(int)accol]);
        Debug.Assert(!parent->acc.needsRefresh[(int)accol]);
        Debug.Assert(parent->acc[Color.White] != null);
        Debug.Assert(parent->acc[Color.Black] != null);

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

        if (m.IsNull)
        {
            parent->acc.CopyTo(ref this);
        }
        else if (m.IsCastling)
        {
            int idx = Castling.GetCastlingIdx(Us, from < to);
            AddAddSubSub(
                accol,
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
                accol,
                ref parent->acc,
                Us, m.IsPromotion ? m.PromoType : pt, to,
                Us, pt, from,
                Them, vict, !m.IsEnPassant ? to : (Us == Color.White ? to - 8 : to + 8)
            );
        }
        else
        {
            AddSub(
                accol,
                ref parent->acc,
                Us, m.IsPromotion ? m.PromoType : pt, to,
                Us, pt, from
            );
        }

        needsUpdate[(int)accol] = false;
        needsRefresh[(int)accol] = false;
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

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            if (n->acc.needsRefresh[(int)c])
            {
                n->acc.Accumulate(ref n->p);
                return;
            }
            
            // if current accumulator does not need updates, skip all this

            if (!n->acc.needsUpdate[(int)c])
            {
                return;            
            }

            // find last nodes accumulator does not need an update/refresh

            Node* m = n - 1;
            while (m->acc.needsUpdate[(int)c] && !m->acc.needsRefresh[(int)c])
            {
                m--;
            }

            if (m->acc.needsRefresh[(int)c])
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
                    m->acc.Update(m - 1, ref m->p, c);
                }
            }
        }
    }


    public static int GetFlip(int ksq)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        return Utils.FileOf(ksq) > 3 ? 7 : 0;
    }

    public unsafe int GetFeatureIdx(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        if (accol == Color.White)
        {
            return (int)c * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.White]);
        }
        else
        {
            return (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.Black] ^ 56);
        }
    }


    /// <summary>
    /// accumulate a newly activated feaure in the accumulator
    /// ~a piece has been placed on the board somewhere
    /// </summary>
    public void Activate(Color c, PieceType pt, int sq, Color accol)
    {
        if (Avx2.IsSupported)
            ActivateAvx2(c, pt, sq, accol);
        else
            ActivateFallback(c, pt, sq, accol);
    }

    private unsafe void ActivateFallback(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var acc = this[accol];
        var idx = GetFeatureIdx(c, pt, sq, accol);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Vector.Load(acc + node);
            var weight = Vector.Load(l0_weight + idx * L1_SIZE + node);
            Vector.Store(Vector.Add(accval, weight), acc + node);
        }
    }

    private unsafe void ActivateAvx2(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var acc = this[accol];
        var idx = GetFeatureIdx(c, pt, sq, accol);

        for (int node = 0; node < L1_SIZE; node += 16)
        {
            var accval = Avx.LoadAlignedVector256(acc + node);
            var weight = Avx.LoadAlignedVector256(l0_weight + idx * L1_SIZE + node);
            Avx.StoreAligned(acc + node, Avx2.Add(accval, weight));
        }
    }


    /// <summary>
    /// remove the accumulation of a formerly activated feaure from the accumulator
    /// ~a piece has been removed from the board somewhere
    /// </summary>
    public void Deactivate(Color c, PieceType pt, int sq, Color accol)
    {
        if (Avx2.IsSupported)
            DeactivateAvx2(c, pt, sq, accol);
        else
            DeactivateFallback(c, pt, sq, accol);
    }

    private unsafe void DeactivateFallback(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var acc = this[accol];
        var idx = GetFeatureIdx(c, pt, sq, accol);

        int vecSize = Vector<short>.Count;
        Debug.Assert(L1_SIZE % vecSize == 0);

        for (int node = 0; node < L1_SIZE; node += vecSize)
        {
            var accval = Vector.Load(acc + node);
            var weight = Vector.Load(l0_weight + idx * L1_SIZE + node);
            Vector.Store(Vector.Subtract(accval, weight), acc + node);
        }
    }

    private unsafe void DeactivateAvx2(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(Avx2.IsSupported);
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);

        var acc = this[accol];
        var idx = GetFeatureIdx(c, pt, sq, accol);

        for (int node = 0; node < L1_SIZE; node += 16)
        {
            var accval = Avx.LoadAlignedVector256(acc + node);
            var weight = Avx.LoadAlignedVector256(l0_weight + idx * L1_SIZE + node);
            Avx.StoreAligned(acc + node, Avx2.Subtract(accval, weight));
        }
    }

    /// <summary>
    /// copy accumulated values to childs White- & BlackAcc
    /// </summary>
    public unsafe void CopyTo(ref Accumulator child)
    {
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);
        Debug.Assert(child[Color.White] != null);
        Debug.Assert(child[Color.Black] != null);

        NativeMemory.Copy(this[Color.White], child[Color.White], (nuint)sizeof(short) * L1_SIZE);
        NativeMemory.Copy(this[Color.Black], child[Color.Black], (nuint)sizeof(short) * L1_SIZE);

        child.flip[(int)Color.White] = flip[(int)Color.White];
        child.flip[(int)Color.Black] = flip[(int)Color.Black];
    }

    /// <summary>
    /// fill accumulator with zeros
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(this[Color.White] != null);
        Debug.Assert(this[Color.Black] != null);

        NativeMemory.Clear(this[Color.White], sizeof(short) * L1_SIZE);
        NativeMemory.Clear(this[Color.Black], sizeof(short) * L1_SIZE);

        flip[(int)Color.White] = 0;
        flip[(int)Color.Black] = 0;
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
        if (other.flip[(int)Color.White] != flip[(int)Color.White] 
            || other.flip[(int)Color.Black] != flip[(int)Color.Black])
        {
            return false;
        }

        for (int i = 0; i < L1_SIZE; i++)
        {
            if (this[Color.White][i] != other[Color.White][i] 
                || this[Color.White][i] != other[Color.Black][i])
            {
                return false;
            }
        }

        return true;
    }
}
