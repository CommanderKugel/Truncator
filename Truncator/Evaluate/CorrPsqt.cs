
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
    /// update the correction-psqt-square of a pieces destination
    /// </summary>
    public unsafe void UpdateSingle(Color c, PieceType pt, int sq, int score, int eval, int depth)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);
        Debug.Assert(!Search.IsTerminal(score));
        
        int delta = Math.Clamp((score - eval) * depth / 8, -HistVal.HIST_VAL_MAX / 4, HistVal.HIST_VAL_MAX / 4);
        table[(int)c * 6 * 64 + (int)pt * 64 + (sq ^ ((int)c * 56))].Update(c == Color.White ? delta : -delta);
    }

    public unsafe void UpdateMultiple(ref Pos p, ref Span<Move> quiets, int quietcount, Move bestmove, int score, int eval, int depth)
    {
        int delta = Math.Clamp((score - eval) * depth / 8, -HistVal.HIST_VAL_MAX / 4, HistVal.HIST_VAL_MAX / 4);
        delta *= p.Us == Color.White ? 1 : -1;

        for (int i = 0; i < quietcount; i++)
        {
            ref Move m = ref quiets[i];
            int sq = m.to ^ ((int)p.Us * 56);
            PieceType pt = p.PieceTypeOn(m.from);

            table[(int)p.Us * 6 * 64 + (int)pt * 64 + sq].Update(m == bestmove ? delta : -delta);
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
