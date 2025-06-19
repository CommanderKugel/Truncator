
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ContinuationHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 64;

    private unsafe PieceToHistory* table = null;

    /// <summary>
    /// this table will always contain zeroes, to account for null- or rootmoves 
    /// that dont have a valid previous move.
    /// (i know root might have a predecessor, but that is irrelevant for now)
    /// </summary>
    public unsafe PieceToHistory* NullHist;

    public unsafe ContinuationHistory()
    {
        table = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);
        NullHist = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);

        for (int i = 0; i < SIZE; i++)
        {
            table[i] = new();
        }

        NullHist[0] = new();
    }

    public unsafe PieceToHistory* this[Color c, PieceType pt, int sq]
    {
        get
        {
            Debug.Assert(table != null);
            Debug.Assert(c != Color.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            return pt == PieceType.NONE ? NullHist
                : &table[(int)c * 6 * 64 + (int)pt * 64 + sq];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table != null);

        for (int i = 0; i < SIZE; i++)
        {
            table[i].Clear();
        }
        NullHist[0].Clear();
    }

    public unsafe void Dispose()
    {
        if (table != null)
        {
            for (int i = 0; i < SIZE; i++)
            {
                table[i].Dispose();
            }

            NullHist[0].Dispose();

            NativeMemory.Free(table);
            NativeMemory.Free(NullHist);
            table = null;
            NullHist = null;
        }
    }

}
