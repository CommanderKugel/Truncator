
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
    public unsafe fixed int buck[2];
    public unsafe fixed bool needsUpdate[2];
    public unsafe fixed bool needsRefresh[2];


    public unsafe Accumulator()
    {
        WhiteAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);
        BlackAcc = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);

        flip[(int)Color.White] = flip[(int)Color.Black] = 0;
        buck[(int)Color.White] = buck[(int)Color.Black] = 0;

        needsUpdate[(int)Color.White] = needsUpdate[(int)Color.Black] = true;
        needsRefresh[(int)Color.White] = needsRefresh[(int)Color.Black] = true;
    }

    public unsafe void Kill()
    {
        for (Color c = Color.White; c <= Color.Black; c++)
        {
            flip[(int)c] = 0;
            buck[(int)c] = 0;
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
        buck[(int)accol] = GetBucket(p.KingSquares[(int)accol], accol);

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
        Debug.Assert(p.ZobristKey == (parent + 1)->p.ZobristKey);
        Debug.Assert(accol != Color.NONE);

        Debug.Assert(!parent->acc.needsUpdate[(int)accol]);
        Debug.Assert(!parent->acc.needsRefresh[(int)accol]);
        Debug.Assert(!needsRefresh[(int)accol]);
        Debug.Assert(needsUpdate[(int)accol]);

        Debug.Assert(this[accol] != null);
        Debug.Assert(parent->acc[accol] != null);

        Debug.Assert(parent->acc.flip[(int)accol] == GetFlip(parent->p.KingSquares[(int)accol]), $"ksq: {(Square)parent->p.KingSquares[(int)accol]}");
        Debug.Assert(parent->acc.buck[(int)accol] == GetBucket(parent->p.KingSquares[(int)accol], accol), $"ksq: {(Square)parent->p.KingSquares[(int)accol]}");

        Color Us = p.Them;
        Color Them = p.Us;

        PieceType pt = parent->MovedPieceType;
        PieceType vict = parent->CapturedPieceType;

        Move m = parent->move;
        int from = m.from;
        int to = m.to;

        // assume the king stays in its bucket -> merged efficient updates (UE)

        if (m.IsNull)
        {
            parent->acc.CopyTo(ref this, accol);
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
                n->acc.Accumulate(ref n->p, c);
                continue;
            }
            
            // if current accumulator does not need updates, skip all this

            if (!n->acc.needsUpdate[(int)c])
            {
                continue;
            }

            // find last nodes accumulator does not need an update/refresh

            int ply = 0;
            Node* m = n - 1;
            while (m->acc.needsUpdate[(int)c] && !m->acc.needsRefresh[(int)c])
            {
                m--;
                ply++;
            }

            // refresh the earliest acc that does not need updates if needed

            if (m->acc.needsRefresh[(int)c])
            {
                m->acc.Accumulate(ref m->p, c);
            }

            // incremental updates until current accumulator is up-to-date

            while (m != n)
            {
                m++;
                m->acc.Update(m - 1, ref m->p, c);               
            }
        }
    }


    public static int GetFlip(int ksq)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        return Utils.FileOf(ksq) > 3 ? 7 : 0;
    }

    public static int GetBucket(int ksq, Color c)
    {
        Debug.Assert(ksq >= 0 && ksq < 64);
        Debug.Assert(c != Color.NONE);
        return KingBuckets[ksq ^ ((int)c * 56)];
    }

    public unsafe int GetFeatureIdx(Color c, PieceType pt, int sq, Color accol)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);
        Debug.Assert(accol != Color.NONE);

        if (accol == Color.White)
        {
            return buck[(int)Color.White] * 768 + (int)c * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.White]);
        }
        else
        {
            return buck[(int)Color.Black] * 768 + (int)(1 - c) * 384 + (int)pt * 64 + (sq ^ flip[(int)Color.Black] ^ 56);
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
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(this[accol] != null);
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
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(this[accol] != null);
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
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(this[accol] != null);
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
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(this[accol] != null);
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
    public void CopyTo(ref Accumulator child)
    {
        CopyTo(ref child, Color.White);
        CopyTo(ref child, Color.Black);
    } 


    public unsafe void CopyTo(ref Accumulator child, Color accol)
    {
        Debug.Assert(accol != Color.NONE);
        Debug.Assert(this[accol] != null);
        Debug.Assert(child[accol] != null);

        NativeMemory.Copy(this[accol], child[accol], (nuint)sizeof(short) * L1_SIZE);

        child.flip[(int)accol] = flip[(int)accol];
        child.buck[(int)accol] = buck[(int)accol];
        child.needsRefresh[(int)accol] = needsRefresh[(int)accol];
        child.needsUpdate[(int)accol] = needsUpdate[(int)accol];
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


    public unsafe bool EqualContents(ref Accumulator other, Color c)
    {
        if (other.flip[(int)c] != flip[(int)c])
        {
            throw new Exception($"flip[{c}]: other={other.flip[(int)c]} vs this={flip[(int)c]}");
        }

        if (other.needsRefresh[(int)c] != needsRefresh[(int)c])
        {
            throw new Exception($"needsRefresh[{c}]: other={other.needsRefresh[(int)c]} vs this={needsRefresh[(int)c]}");
        }

        if (other.needsUpdate[(int)c] != needsUpdate[(int)c])
        {
            throw new Exception($"needsUpdate[{c}]: other={other.needsUpdate[(int)c]} vs this={needsUpdate[(int)c]}");
        }

        for (int i = 0; i < L1_SIZE; i++)
        {
            if (this[c][i] != other[c][i])
            {
                return false;
            }
        }

        return true;
    }
}
