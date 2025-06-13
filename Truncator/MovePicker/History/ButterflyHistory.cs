
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ButterflyHistory : IDisposable
{

    public readonly nuint size;
    private unsafe short* table_ = null;


    public unsafe ButterflyHistory()
    {
        this.size = 2 * 64 * 64;
        table_ = (short*)NativeMemory.Alloc((nuint)sizeof(short) * this.size);
    }

    public unsafe void Update(short delta, Color c, Move m)
    {
        Debug.Assert(c == Color.White || c == Color.Black);
        Debug.Assert(m.NotNull);
        this[c, m] = (short)Math.Clamp(this[c, m] + delta, -16_000, 16_000);
    }

    public unsafe ref short this[Color c, int from, int to]
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

    public unsafe ref short this[Color c, Move m]
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
