
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
        ref short val = ref this[c, m];
        val = (short)Math.Clamp(val + delta, -16_000, 16_000);
    }

    public unsafe ref short this[Color c, int from, int to]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(from >= 0 && from < 64);
            Debug.Assert(to >= 0 && to < 64);
            return ref table_[from * 64 + to];
        }
    }

    public unsafe ref short this[Color c, Move m]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(m.NotNull);
            return ref table_[m.value & 0x0FFF];
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
