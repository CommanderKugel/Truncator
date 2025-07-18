
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ContinuationHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 64;

    private unsafe PieceToHistory* table_ = null;

    public unsafe ContinuationHistory()
    {
        table_ = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);

        for (int i = 0; i < SIZE; i++)
        {
            *(table_ + i) = new PieceToHistory();
        }
    }

    public unsafe PieceToHistory* this[Color c, PieceType pt, int sq]
    {
        get
        {
            Debug.Assert(table_ != null);
            Debug.Assert(c != Color.NONE);
            Debug.Assert(pt != PieceType.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            return &table_[(int)c * 6 * 64 + (int)pt * 64 + sq];
        }
    }

    public unsafe PieceToHistory* NullHist => &table_[0];

    public unsafe void Clear()
    {
        for (int i = 0; i < SIZE; i++)
        {
            table_[i].Clear();
        }
    }

    public unsafe void Dispose()
    {
        if (table_ != null)
        {
            for (int i = 0; i < SIZE; i++)
            {
                table_[i].Dispose();
            }

            NativeMemory.Free(table_);
            table_ = null;
        }
    }

}
