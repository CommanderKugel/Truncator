
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CaptureHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 6 * 64 * 64;
    private unsafe HistVal* table_ = null;

    public unsafe CaptureHistory()
    {
        table_ = (HistVal*)NativeMemory.Alloc((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[Color c, PieceType att, PieceType vict, Move m]
    {
        get
        {
            Debug.Assert(c == Color.White || c == Color.Black);
            Debug.Assert(att != PieceType.NONE);
            Debug.Assert(vict != PieceType.NONE);
            Debug.Assert(m.NotNull);
            return ref table_[
                (int)c * 6 * 6 * 64 * 64
                + (int)att * 6 * 64 * 64
                + (int)vict * 64 * 64
                + m.ButterflyMask
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
