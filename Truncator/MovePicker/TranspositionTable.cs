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
        size = entryCount;
    }

    public unsafe void Resize(int sizemb)
    {
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        int sizeByte = sizemb * 1024 * 1024;
        nuint entryCount = (nuint)((sizeByte / sizeof(TTEntry)) & ~0b11); // round to multiples of 4

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

    public unsafe TTEntry Probe(ulong key)
    {
        Debug.Assert(tt != null);

        TTEntry* ptr = &tt[(key % size) & ~0b11ul];
        bool hit = false;

        for (int i = 0; i < 4 && !hit; i++)
        {
            if ((ptr + i)->Key == key)
            {
                ptr += i;
                hit = true;
            }
        }

        return hit ? *ptr : new();
    }

    public unsafe void Write(ulong key, int score, Move move, int depth, int flag, bool pv, SearchThread thread)
    {
        Debug.Assert(tt != null);
        
        TTEntry* ptr = &tt[(key % size) & ~0b11ul];
        TTEntry* target = ptr;

        for (int i = 0; i < 4; i++)
        {
            // always replace old or empty entries

            if ((ptr + i)->Key == key
                || (ptr + i)->Flag == Search.NONE_BOUND)
            {
                target = ptr + i;
                break;
            }
        }

        target->Key = key;
        target->Score = TTEntry.ConvertToSavescore(score, thread.ply);
        target->MoveValue = move.value;
        target->Depth = (byte)depth;
        target->PackPVAgeFlag(pv, 0, flag);
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