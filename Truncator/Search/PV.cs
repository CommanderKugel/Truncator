using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

public struct PV : IDisposable
{
    /// <summary>
    /// maximum number of moves a variation can store
    /// </summary>
    public const int SIZE = 256;

    private unsafe Move* table_ = null;
    private unsafe Move* lastVariation = null;

    private unsafe int* scores = null;
    public object lockObject;

    public unsafe Move BestMove => table_[0].NotNull ? table_[0] : lastVariation[0];
    public unsafe Move PonderMove => table_[1].NotNull ? table_[1] : lastVariation[1];


    public unsafe PV()
    {
        table_ = (Move*)NativeMemory.Alloc((nuint)sizeof(Move) * SIZE * SIZE);
        lastVariation = (Move*)NativeMemory.Alloc((nuint)sizeof(Move) * SIZE);

        scores = (int*)NativeMemory.Alloc(sizeof(int) * SIZE);
        lockObject = new object();
    }

    public unsafe Move this[int ply1, int ply2]
    {
        get
        {
            Debug.Assert(ply1 >= 0 && ply1 < SIZE);
            Debug.Assert(ply2 >= 0 && ply2 < SIZE);
            return table_[SIZE * ply1 + ply2];
        }
        set
        {
            Debug.Assert(ply1 >= 0 && ply1 < SIZE);
            Debug.Assert(ply2 >= 0 && ply2 < SIZE);
            table_[SIZE * ply1 + ply2] = value;
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
        Debug.Assert(table_ != null && lastVariation != null && scores != null, "cant clear a disposed or uninitialized pv!");
        NativeMemory.Clear(table_, (nuint)sizeof(Move) * SIZE * SIZE);
        NativeMemory.Clear(lastVariation, (nuint)sizeof(Move) * SIZE);
        NativeMemory.Clear(scores, sizeof(int) * SIZE);
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
        var ptr = table_[0].NotNull ? table_ : lastVariation;

        string pv = "";
        for (int i = 0; i < SIZE && (ptr + i)->NotNull; i++)
        {
            pv += (ptr + i)->ToString() + " ";
        }
        return pv;
    }

    public unsafe void SaveLastLine()
    {
        Debug.Assert(table_ != null && lastVariation != null);
        NativeMemory.Copy(table_, lastVariation, (nuint)sizeof(Move) * SIZE);
    }

    public unsafe void Dispose()
    {
        if (table_ != null)
        {
            NativeMemory.Free(table_);
            NativeMemory.Free(lastVariation);
            NativeMemory.Free(scores);
            table_ = lastVariation = null;
            scores = null;
        }
    }

}