
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ButterflyHistory : IDisposable
{

    public readonly nuint size;
    private unsafe HistVal* table_ = null;


    public unsafe ButterflyHistory()
    {
        size = 2 * 64 * 64;
        table_ = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * size);
    }

    public unsafe void Update(short delta, Color c, Move m)
    {
        ref HistVal val = ref this[c, m];
        val <<= delta;
    }

    public unsafe ref HistVal this[Color c, int from, int to]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(from >= 0 && from < 64);
            Debug.Assert(to >= 0 && to < 64);
            Debug.Assert(from != to);
            return ref table_[(int)c * 64 * 64 + to * 64 + from];
        }
    }

    public unsafe ref HistVal this[Color c, Move m]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(m.NotNull);
            Debug.Assert(m.ButterflyMask == (m.to * 64 + m.from));
            return ref table_[(int)c * 64 * 64 + m.ButterflyMask];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table_ != null, "cant clear an empty array!");
        NativeMemory.Clear(table_, sizeof(short) * size);
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
