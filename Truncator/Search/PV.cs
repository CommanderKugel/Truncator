using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PV : IDisposable
{
    public const int SIZE = 128;

    private unsafe Move* pv = null;


    public unsafe PV()
    {
        pv = (Move*)NativeMemory.Alloc(sizeof(ushort) * SIZE * SIZE);
    }

    public unsafe Move this[int ply1, int ply2]
    {
        get
        {
            Debug.Assert(ply1 >= 0 && ply1 < SIZE);
            Debug.Assert(ply2 >= 0 && ply2 < SIZE);
            return pv[SIZE * ply1 + ply2];
        }
        set
        {
            Debug.Assert(ply1 >= 0 && ply1 < SIZE);
            Debug.Assert(ply2 >= 0 && ply2 < SIZE);
            pv[SIZE * ply1 + ply2] = value;
        }
    }

    public unsafe void Clear() => NativeMemory.Clear(pv, sizeof(ushort) * SIZE * SIZE);

    public unsafe void Push(Move m, int ply)
    {
        Debug.Assert(ply >= 0 && ply < SIZE);
        this[ply, ply] = m;
    }

    public unsafe string GetPV()
    {
        string pv = "";
        for (int i = 0; i < SIZE && this[0, i].NotNull; i++)
        {
            pv += this[0, i].ToString() + " ";
        }
        return pv;
    }

    public unsafe Move BestMove => pv[0];

    public unsafe void Dispose()
    {
        if (pv is not null)
        {
            NativeMemory.Free(pv);
            pv = null;
        }
    }

}