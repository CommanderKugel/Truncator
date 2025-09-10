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
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);

        tt = (TTEntry*)NativeMemory.Alloc((nuint)sizeof(TTEntry) * entryCount);
        this.size = entryCount;
    }

    public unsafe void Resize(int sizemb)
    {
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);

        if (tt != null)
        {
            NativeMemory.Free(tt);
        }
        tt = (TTEntry*)NativeMemory.Alloc((nuint)sizeof(TTEntry) * entryCount);
        size = entryCount;

        Console.WriteLine($"hash set to {sizemb} mb");
    }

    public unsafe void Clear()
    {
        Debug.Assert(tt != null);
        NativeMemory.Clear(tt, (nuint)sizeof(TTEntry) * (nuint)size);
    }

    public unsafe bool Probe(ulong key, out TTEntry entry, int ply)
    {
        Debug.Assert(tt != null);
        var copy = tt[key % size];

        if (copy.Key == key)
        {
            entry = copy;
            entry.Score = TTEntry.ConvertToSearchscore(entry.Score, ply);
            return true;
        }

        entry = new();
        return false;
    }

    public unsafe void Write(ulong key, int score, Move move, int depth, int flag, bool pv, SearchThread thread)
    {
        Debug.Assert(tt != null);
        
        // current policy: always replace
        ref var entry = ref tt[key % size];

        entry.Key = key;
        entry.Score = TTEntry.ConvertToSavescore(score, thread.ply);
        entry.MoveValue = move.value;
        entry.Depth = (byte)depth;
        entry.PackPVAgeFlag(pv, 0, flag);
    }

    public unsafe int GetHashfull()
    {
        Debug.Assert(tt != null);
        Debug.Assert(size >= MIN_SIZE);

        int hashfull = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (tt[i].Key != 0)
            {
                hashfull++;
            }
        }
        return hashfull;
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