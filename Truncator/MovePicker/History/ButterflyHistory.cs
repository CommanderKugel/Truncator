
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct ButterflyHistory : IDisposable
{

    public const int SIZE = 2 * 2 * 2 * 64 * 64;
    private unsafe HistVal* table_ = null;


    public unsafe ButterflyHistory()
    {
        table_ = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[Color c, Move m, ulong threats]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(m.NotNull);
            Debug.Assert(m.ButterflyMask == (m.to * 64 + m.from));
            int threatenedFrom = ((1ul << m.from) & threats) != 0 ? 2 * 64 * 64 : 0;
            int threatenedTo = ((1ul << m.from) & threats) != 0 ? 2 * 2 * 64 * 64 : 0;
            return ref table_[(int)c * 64 * 64 + m.ButterflyMask + threatenedFrom + threatenedTo];
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
