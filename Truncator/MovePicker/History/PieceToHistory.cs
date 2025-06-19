
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PieceToHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 64;

    private unsafe HistVal* table = null;

    public unsafe PieceToHistory()
    {
        table = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[Color c, PieceType pt, int sq]
    {
        get
        {
            Debug.Assert(table != null);
            Debug.Assert(c != Color.NONE);
            Debug.Assert(pt != PieceType.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            return ref table[(int)c * 6 * 64 + (int)pt * 64 + sq];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table != null, "cant clean an empty array!");
        NativeMemory.Clear(table, (nuint)sizeof(HistVal) * SIZE);
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
