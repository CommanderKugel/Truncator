
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PieceToHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 64;
    private unsafe HistVal* table_ = null;

    public unsafe PieceToHistory()
    {
        table_ = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[Color c, PieceType pt, int sq]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(pt != PieceType.NONE);
            Debug.Assert(sq >= 0 && sq < 64);
            return ref table_[(int)c * 6 * 64 + (int)pt * 64 + sq];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table_ != null, "cant clear an empty array!");
        NativeMemory.Clear(table_, sizeof(short) * SIZE);
    }

    public unsafe void Dispose()
    {
        if (table_ != null)
        {
            NativeMemory.Free(table_);
            table_ = null;
        }
    }

}
