
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ContinuationHistory : IDisposable
{

    public const int SIZE = 6 * 64;

    private unsafe PieceToHistory* table = null;

    /// <summary>
    /// this table will always contain zeroes, to account for null- or rootmoves 
    /// that dont have a valid previous move.
    /// (i know root might have a predecessor, but that is irrelevant for now)
    /// </summary>
    public unsafe PieceToHistory* NullHist;

    /// <summary>
    /// Update this table instead of the NullHist, so that NullHist is always empty
    /// </summary>
    public unsafe PieceToHistory* RubbishHist;

    public unsafe ContinuationHistory()
    {
        table = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);
        NullHist = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory));
        RubbishHist = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory));

        for (int i = 0; i < SIZE; i++)
        {
            table[i] = new();
        }

        *NullHist = new();
        *RubbishHist = new();
    }

    public unsafe PieceToHistory* this[PieceType pt, int sq]
    {
        get
        {
            Debug.Assert(table != null);
            Debug.Assert(pt != PieceType.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            return &table[(int)pt * 64 + sq];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table != null);

        for (int i = 0; i < SIZE; i++)
        {
            table[i].Clear();
        }

        (*NullHist).Clear();
        (*RubbishHist).Clear();
    }

    public unsafe void Dispose()
    {
        if (table != null)
        {
            for (int i = 0; i < SIZE; i++)
            {
                table[i].Dispose();
            }

            (*NullHist).Dispose();
            (*RubbishHist).Dispose();

            NativeMemory.Free(table);
            NativeMemory.Free(NullHist);
            NativeMemory.Free(RubbishHist);
            table = NullHist = RubbishHist = null;
        }
    }

}
