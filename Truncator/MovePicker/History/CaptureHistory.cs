
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CaptureHistory : IDisposable
{

    public const int SIZE = 2 * 6 * 6 * 64 * 64;

    private unsafe HistVal* table = null;

    public unsafe CaptureHistory()
    {
        table = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE);
    }

    public unsafe ref HistVal this[Color c, PieceType victim, PieceType attacker, Move m]
    {
        get
        {
            Debug.Assert(table != null);
            Debug.Assert(c != Color.NONE);
            Debug.Assert(victim != PieceType.NONE);
            Debug.Assert(attacker != PieceType.NONE);
            Debug.Assert(m.NotNull);
            return ref table[(int)c * 6 * 6 * 64 * 64 + (int)victim * 6 * 64 * 64 + (int)attacker * 64 * 64 + m.ButterflyMask];
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(table != null);
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
