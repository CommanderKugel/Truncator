
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PawnHistory : IDisposable
{

    public const int SIZE = 512;

    private unsafe PieceToHistory* table = null;

    public unsafe PawnHistory()
    {
        table = (PieceToHistory*)NativeMemory.Alloc((nuint)sizeof(PieceToHistory) * SIZE);

        for (int i = 0; i < SIZE; i++)
        {
            table[i] = new();
        }
    }

    public unsafe ref HistVal this[Color c, PieceType pt, int sq, ulong pawnKey]
    {
        get
        {
            Debug.Assert(c != Color.NONE);
            Debug.Assert(pt != PieceType.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            PieceToHistory* hist = &table[pawnKey % SIZE];
            return ref (*hist)[c, pt, sq];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table != null, "cant clear an empty array!");
        for (int i = 0; i < SIZE; i++)
        {
            table[i].Clear();
        }
    }

    public unsafe void Dispose()
    {
        if (table != null)
        {
            for (int i = 0; i < SIZE; i++)
            {
                table[i].Dispose();
            }

            NativeMemory.Free(table);
            table = null;
        }
    }

}
