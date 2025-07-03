using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PV : IDisposable
{
    public const int SIZE = 128;

    private unsafe Move* pv = null;

    private Move lastBestMove, lastPonderMove;
    private string lastPv;

    private unsafe int* scores = null;
    public object lockObject;

    public unsafe PV()
    {
        pv = (Move*)NativeMemory.Alloc(sizeof(ushort) * SIZE * SIZE);
        scores = (int*)NativeMemory.Alloc(sizeof(int) * SIZE);
        lockObject = new object();

        lastBestMove = Move.NullMove;
        lastPonderMove = Move.NullMove;
        lastPv = "";
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

    public unsafe int this[int iteration]
    {
        get
        {
            Debug.Assert(iteration >= 0 && iteration <= SIZE);
            return scores[iteration];
        }
        set
        {
            Debug.Assert(iteration >= 0 && iteration <= SIZE);
            scores[iteration] = value;
        }
    }

    public unsafe void Clear()
    {
        Debug.Assert(pv != null && scores != null, "cant clear a disposed or uninitialized pv!");
        NativeMemory.Clear(pv, sizeof(ushort) * SIZE * SIZE);
        NativeMemory.Clear(scores, sizeof(int) * SIZE);
    }

    public unsafe void PrepareNewIteration()
    {
        lastBestMove = pv[0];
        lastPonderMove = pv[1];
        lastPv = GetPV();
    }

    public unsafe void Push(Move m, int ply)
    {
        Debug.Assert(ply >= 0 && ply < SIZE);
        this[ply, ply] = m;

        for (int i = ply + 1; i < SIZE; i++)
        {
            this[ply, i] = this[ply + 1, i];
        }
    }

    public unsafe string GetPV()
    {
        if (this[0, 0].IsNull)
        {
            return lastPv;
        }

        string pv = "";
        for (int i = 0; i < SIZE && this[0, i].NotNull; i++)
        {
            pv += this[0, i].ToString() + " ";
        }
        return pv;
    }

    public unsafe Move BestMove => pv[0].NotNull ? pv[0] : lastBestMove;
    public unsafe Move PonderMove => pv[1].NotNull ? pv[1] : lastPonderMove;

    public unsafe void Dispose()
    {
        if (pv != null)
        {
            NativeMemory.Free(pv);
            NativeMemory.Free(scores);
            pv = null;
            scores = null;
        }
    }

}