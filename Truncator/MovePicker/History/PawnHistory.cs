
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PawnHistory : IDisposable
{

    public const int SIZE = 1024;

    private unsafe PieceToHistory* table_ = null;

    public unsafe PawnHistory()
    {
        table_ = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);

        for (int i = 0; i < SIZE; i++)
        {
            *(table_ + i) = new PieceToHistory();
        }
    }

    public unsafe PieceToHistory* this[ulong key]
    {
        get
        {
            Debug.Assert(table_ != null);
            return &table_[key % SIZE];
        }
    }

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
