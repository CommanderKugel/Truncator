using System.Diagnostics;
using System.Runtime.InteropServices;

public class TranspositionTable : IDisposable
{
    // ToDo: Bucketing, ageing, advanced replacement policy

    public const int MIN_SIZE = 1;
    public const int MAX_SIZE = 32 * 1024;
    public const int DEFAULT_SIZE = 64;

    private unsafe TTEntry* tt = null;
    private ulong size = 0;

    public unsafe TranspositionTable(int sizemb = DEFAULT_SIZE)
    {
        Debug.Assert(sizemb >= MIN_SIZE && sizemb <= MAX_SIZE, "unallowed tt size!");
        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);

        tt = (TTEntry*)NativeMemory.AlignedAlloc((nuint)sizeof(TTEntry) * entryCount, (nuint)sizeof(TTEntry) * 4);
        this.size = entryCount;
    }

    public unsafe void Resize(int sizemb)
    {
        Debug.Assert(sizemb >= MIN_SIZE && sizemb <= MAX_SIZE, "unallowed tt size!");
        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);

        NativeMemory.AlignedRealloc(tt, (nuint)sizeof(TTEntry) * entryCount, (nuint)sizeof(TTEntry) * 4);
        this.size = entryCount;
    }

    public unsafe void Clear()
    {
        NativeMemory.Clear(tt, (nuint)sizeof(TTEntry) * (nuint)size);
    }

    public unsafe TTEntry Probe(ulong key) => tt[key % size];

    public unsafe void Write(ulong key, int score, Move move, int depth, int flag, bool pv, SearchThread thread)
    {
        // current policy: always replace
        ref var entry = ref tt[key % size];

        entry.Key = key;
        entry.Score = TTEntry.ConvertToSavescore(score, thread.ply);
        entry.MoveValue = move.value;
        entry.Depth = (byte)depth;
        entry.PackPVAgeFlag(pv, 0, flag);
    }

    public unsafe void Dispose()
    {
        if (tt is not null)
        {
            NativeMemory.Free(tt);
            tt = null;
        }
    }

}