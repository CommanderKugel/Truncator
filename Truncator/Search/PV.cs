using System.Diagnostics;
using System.Runtime.InteropServices;

public struct PV : IDisposable
{
    /// <summary>
    /// maximum number of moves a variation can store
    /// </summary>
    public const int SIZE = 256;

    private unsafe Move* table_ = null;
    public unsafe Move* bestmoves = null;
    public unsafe int* scores = null;

    public unsafe Move BestMove;
    public unsafe Move PonderMove;


    public unsafe PV()
    {
        table_ = (Move*)NativeMemory.Alloc((nuint)sizeof(Move) * SIZE * SIZE);
        bestmoves = (Move*)NativeMemory.Alloc((nuint)sizeof(Move) * SIZE);
        scores = (int*)NativeMemory.Alloc(sizeof(int) * SIZE);
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

    public unsafe void Clear()
    {
        Debug.Assert(table_ != null && scores != null, "cant clear a disposed or uninitialized pv!");
        NativeMemory.Clear(table_, (nuint)sizeof(Move) * SIZE * SIZE);
        NativeMemory.Clear(bestmoves, (nuint)sizeof(Move) * SIZE);
        NativeMemory.Clear(scores, sizeof(int) * SIZE);

        BestMove = Move.NullMove;
        PonderMove = Move.NullMove;
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

    public override unsafe string ToString()
    {
        string pv = "";
        for (int i = 0; i < SIZE && this[0, i].NotNull; i++)
        {
            pv += this[0, i].ToString() + " ";
        }
        return pv;
    }

    public unsafe void Dispose()
    {
        if (table_ != null)
        {
            NativeMemory.Free(table_);
            NativeMemory.Free(scores);
            NativeMemory.Free(bestmoves);
            table_ = null;
            scores = null;
        }
    }

}