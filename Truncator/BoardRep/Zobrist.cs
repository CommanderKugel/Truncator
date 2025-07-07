
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class Zobrist
{
    public static unsafe ulong stmKey = 1;
    private static unsafe ulong* PieceKeys;
    private static unsafe ulong* CastlingKeys;
    private static unsafe ulong* EpKeys;


    static unsafe Zobrist()
    {
        PieceKeys = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * 2 * 6 * 64, sizeof(ulong) * 64);
        CastlingKeys = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * 16, sizeof(ulong) * 16);
        EpKeys = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * 8, sizeof(ulong) * 8);

        for (int i = 0; i < 2 * 6 * 64; i++)
        {
            PieceKeys[i] = Rng.XoShiRoNext();
        }

        for (int i = 0; i < 16; i++)
        {
            CastlingKeys[i] = Rng.XoShiRoNext();
        }

        for (int f = 0; f < 8; f++)
        {
            EpKeys[f] = Rng.XoShiRoNext();
        }
    }

    public static unsafe ulong GetSinglePieceKey(Color c, PieceType pt, int sq)
    {
        Debug.Assert(c == Color.White || c == Color.Black);
        Debug.Assert(pt <= PieceType.King);
        Debug.Assert(sq >= 0 && sq < 64);
        return PieceKeys[(int)c * 384 + (int)pt * 64 + sq];
    }

    public static unsafe ulong GetCastlingKey(byte cr)
    {
        Debug.Assert(cr < 16);
        return CastlingKeys[cr];
    }

    public static unsafe ulong GetEpKEy(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        return EpKeys[Utils.FileOf(sq)];
    }

    /// <summary>
    /// Computes and replaces all types of zobrist keys of the given position
    /// </summary>
    public static unsafe void ComputeFromZero(ref Pos p)
    {
        p.ZobristKey = p.Us == Color.White ? 0 : stmKey;
        p.NonPawnKeys[(int)Color.White] = p.NonPawnKeys[(int)Color.Black] = 0;
        // piece keys will simply be overwritten

        for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
        {
            ulong wkey = GetColoredPieceKey(Color.White, pt, ref p);
            ulong bkey = GetColoredPieceKey(Color.Black, pt, ref p);

            p.ZobristKey ^= wkey ^ bkey;
            p.PieceKeys[(int)pt] = wkey ^ bkey;

            if (pt != PieceType.Pawn)
            {
                p.NonPawnKeys[(int)Color.White] ^= wkey;
                p.NonPawnKeys[(int)Color.Black] ^= bkey;
            }
        }

        p.ZobristKey ^= GetCastlingKey(p.CastlingRights);

        if (p.EnPassantSquare != (int)Square.NONE)
        {
            p.ZobristKey ^= GetEpKEy(p.EnPassantSquare);
        }
    }

    public static unsafe ulong GetColoredPieceKey(Color c, PieceType pt, ref Pos p)
    {
        ulong key = 0;
        for (ulong pieces = p.GetPieces(c, pt); pieces != 0; )
        {
            int sq = Utils.popLsb(ref pieces);
            key ^= GetSinglePieceKey(c, pt, sq);
        }
        return key;
    }

    public static unsafe ulong GetNonPawnKey(Color c, ref Pos p)
    {
        ulong key = 0;
        for (PieceType pt = PieceType.Knight; pt <= PieceType.King; pt++)
        {
            key ^= GetColoredPieceKey(c, pt, ref p);
        }
        return key;
    }

    public static unsafe void Dispose()
    {
        if (PieceKeys != null)
        {
            NativeMemory.Free(PieceKeys);
            NativeMemory.Free(CastlingKeys);
            NativeMemory.Free(EpKeys);
            PieceKeys = CastlingKeys = EpKeys = null;
        }
    }

}
