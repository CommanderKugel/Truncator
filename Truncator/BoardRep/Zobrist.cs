
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

        var rng = new Random(3398300);

        for (int i = 0; i < 2 * 6 * 64; i++)
        {
            PieceKeys[i] = NextRandomUlong(rng);
        }

        for (int i = 0; i < 16; i++)
        {
            CastlingKeys[i] = NextRandomUlong(rng);
        }

        for (int f = 0; f < 8; f++)
        {
            EpKeys[f] = NextRandomUlong(rng);
        }

        unsafe ulong NextRandomUlong(Random rng)
        {
            var buffer = new byte[sizeof(ulong)];
            rng.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer) & ~1ul;
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

    public static unsafe ulong ComputeFromZero(ref Pos p)
    {
        ulong key = p.Us == Color.White ? 0 : stmKey;

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);
                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    key ^= GetSinglePieceKey(c, pt, sq);
                }
            }
        }

        key ^= GetCastlingKey(p.CastlingRights);

        if (p.EnPassantSquare != (int)Square.NONE)
        {
            key ^= GetEpKEy(p.EnPassantSquare);
        }

        return key;
    }

    public static unsafe ulong GetPieceKey(PieceType pt, ref Pos p)
    {
        ulong key = 0;
        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (ulong pieces = p.GetPieces(c, pt); pieces != 0; )
            {
                int sq = Utils.popLsb(ref pieces);
                key ^= GetSinglePieceKey(c, pt, sq);
            }
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
