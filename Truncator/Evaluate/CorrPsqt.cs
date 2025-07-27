
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CorrPsqt : IDisposable
{
    public const int SIZE = 2 * 6 * 64;

    private unsafe HistVal* table = null;


    public unsafe CorrPsqt()
    {
        table = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }


    public unsafe int this[int idx]
    {
        get
        {
            Debug.Assert(idx >= 0 && idx < SIZE);
            return table[idx];
        }
        set
        {
            Debug.Assert(idx >= 0 && idx < SIZE);
            table[idx] = value;
        }
    }


    /// <summary>
    /// Update the Correction PSQT on the current position and search result
    /// </summary>
    public unsafe void Update(ref Pos p, int eval, int score, int depth)
    {
        int delta = Math.Clamp((eval - score) * depth / 8, -HistVal.HIST_VAL_MAX / 4, HistVal.HIST_VAL_MAX / 4);

        if (p.Us == Color.Black)
        {
            delta = -delta;
        }

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);

                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    int idx = (int)c * 6 * 64 + (int)pt * 64 + (sq ^ ((int)c * 56));
                    table[idx].Update(c == Color.White ? delta : -delta);
                }
            }
        }
    }

    /// <summary>
    /// Fill the table with Zeroes
    /// </summary>
    public unsafe void Clear()
    {
        for (int i = 0; i < SIZE; i++)
        {
            table[i] = 0;
        }
    }

    public unsafe void Dispose()
    {
        if (table != null)
        {
            NativeMemory.Free(table);
            table = null;
        }
    }

}
