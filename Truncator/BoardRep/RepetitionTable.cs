
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct RepetitionTable : IDisposable
{
    private volatile int idx;
    public unsafe ulong* table_ = null;

    private const int SIZE = 256;

    public unsafe RepetitionTable()
    {
        idx = 0;
        table_ = (ulong*)NativeMemory.Alloc(sizeof(ulong) * SIZE);
    }

    public unsafe void CopyFrom(ref RepetitionTable src)
    {
        Debug.Assert(table_ != null, "this repetition tabe is not initializted!");
        Debug.Assert(src.table_ is not null, "UCI is not initialized");
        NativeMemory.Copy(src.table_, this.table_, sizeof(ulong) * SIZE);
        this.idx = src.idx;
    }

    public unsafe void MoveKeysToFront(int idx_)
    {
        Debug.Assert(idx_ >= 0 && idx_ < SIZE, "index out of range!");
        NativeMemory.Copy(table_ + idx_, table_, sizeof(ulong) * (nuint)(SIZE - idx_));
        NativeMemory.Clear(table_ + idx_, sizeof(ulong) * (nuint)idx_);
    }

    public unsafe void Push(ulong key)
    {
        Debug.Assert(idx >= 0 && idx < SIZE, "rep-table key will be out of bounds!");
        table_[idx++] = key;
    }

    public unsafe void Pop()
    {
        Debug.Assert(idx >= 1 && idx < SIZE, "rep-table key will be out of bounds!");
        table_[idx--] = 0;
    }

    public unsafe void Clear()
    {
        NativeMemory.Clear(table_, sizeof(ulong) * SIZE);
        idx = 0;
    }

    public unsafe bool IsTwofoldRepetition(ref Pos p)
    {
        for (int i = idx - 5; i >= 0; i -= 2)
        {
            if (p.ZobristKey == table_[i])
            {
                return true;
            }
        }
        return false;
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
