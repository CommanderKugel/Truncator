
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ButterflyHistory : IDisposable
{

    public const int SIZE = 2 * 64 * 64 * 4;
    private unsafe HistVal* table_ = null;


    public unsafe ButterflyHistory()
    {
        table_ = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[ulong threats, Color c, Move m]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(m.NotNull);
            Debug.Assert(m.ButterflyMask == (m.to * 64 + m.from));
            return ref table_[
                (int)c * 64 * 64 * 4
                + m.ButterflyMask * 4
                + (int)((threats >> m.to) & 1) * 2
                + (int)((threats >> m.from) & 1)
            ];
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
