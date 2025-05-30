
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct RepetitionTable : IDisposable
{
    private volatile int idx;
    public unsafe ulong* table = null;

    private const int SIZE = 256;

    public unsafe RepetitionTable()
    {
        idx = 0;
        table = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * SIZE, sizeof(ulong) * SIZE);
    }

    public unsafe void CopyFrom(ref RepetitionTable src)
    {
        Debug.Assert(table is not null, "this repetition tabe is not initializted!");
        Debug.Assert(src.table is not null, "UCI is not initialized");
        NativeMemory.Copy(src.table, this.table, sizeof(ulong) * SIZE);
        this.idx = src.idx;
    }

    public unsafe void MoveKeysToFront(int idx_)
    {
        Debug.Assert(idx_ >= 0 && idx_ < SIZE, "index out of range!");
        NativeMemory.Copy(table + idx_, table, sizeof(ulong) * (nuint)(SIZE - idx_));
        NativeMemory.Clear(table + idx_, sizeof(ulong) * (nuint)idx_);
    }

    public unsafe void Push(ulong key)
    {
        Debug.Assert(idx >= 0 && idx < SIZE, "rep-table key will be out of bounds!");
        table[idx++] = key;
    }

    public unsafe void Pop()
    {
        Debug.Assert(idx >= 1 && idx < SIZE, "rep-table key will be out of bounds!");
        table[idx--] = 0;
    }

    public unsafe void Clear()
    {
        NativeMemory.Clear(table, sizeof(ulong) * SIZE);
        idx = 0;
    }

    public unsafe bool IsTwofoldRepetition(ref Pos p)
    {
        for (int i = idx - 5; i >= 0; i -= 2)
        {
            if (p.ZobristKey == table[i])
            {
                return true;
            }
        }
        return false;
    }

    public unsafe void Dispose()
    {
        if (table is not null)
        {
            NativeMemory.Free(table);
            table = null;
        }
    }
}
