using System.Diagnostics;
using System.Runtime.InteropServices;

public class TranspositionTable : IDisposable
{
    // ToDo: Bucketing, ageing, advanced replacement policy

    public const int MIN_SIZE = 1;
    public const int MAX_SIZE = 32 * 1024;
    public const int DEFAULT_SIZE = 64;

    private unsafe TTBucket* tt = null;
    private ulong size = 0;


    public unsafe TranspositionTable(int sizemb = DEFAULT_SIZE)
    {
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);
        nuint bucketCount = sizeByte / (nuint)sizeof(TTBucket);

        tt = (TTBucket*)NativeMemory.Alloc((nuint)sizeof(TTBucket) * bucketCount);
        this.size = bucketCount;
    }

    /// <summary>
    /// reallocates the tt-size 
    /// accepts sizes in mb
    /// </summary>
    public unsafe void Resize(int sizemb)
    {
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);
        nuint bucketCount = sizeByte / (nuint)sizeof(TTBucket);

        if (tt != null)
        {
            NativeMemory.Free(tt);
        }

        tt = (TTBucket*)NativeMemory.Alloc((nuint)sizeof(TTBucket) * bucketCount);
        size = bucketCount;

        Console.WriteLine($"hash set to {sizemb} mb");
    }

    /// <summary>
    /// sets the contents of the tt to zero
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(tt != null);
        NativeMemory.Clear(tt, (nuint)sizeof(TTBucket) * (nuint)size);
    }

    /// <summary>
    /// probe the tt for a transposition
    /// returns the entry contained if there is one
    /// retuens an empty entry if there is none
    /// </summary>
    public unsafe TTEntry Probe(ulong key)
    {
        Debug.Assert(tt != null);

        ref TTBucket buck = ref tt[key % size];

        for (int i = 0; i < TTBucket.SIZE; i++)
        {
            if (buck[i].Key == key || buck[i].Flag == Search.NONE_BOUND)
            {
                return buck[i];
            }
        }

        return new();
    }

    /// <summary>
    /// writes data from the current node to the tt
    /// </summary>
    public unsafe void Write(ulong key, int score, Move move, int depth, int flag, bool pv, SearchThread thread)
    {
        Debug.Assert(tt != null);

        // current policy: always replace
        ref var buck = ref tt[key % size];
        int replace = -1;

        // look for an entry that is empty or matches the key
        // if there is one, replace it with the new data

        for (int i = 0; i < TTBucket.SIZE; i++)
        {
            if (buck[i].Key == key || buck[i].Flag == Search.NONE_BOUND)
            {
                replace = i;
                break;
            }
        }

        // if no entry is suitable for replacement
        // replace a random one pseudorandomize by taking 
        // the last 2 bits from the threads node count
        // psudorandomization allows deterministic benches and uci <go nodes> commands

        if (replace == -1)
        {
            replace = (int)thread.nodeCount % TTBucket.SIZE;
        }

        // replace the entry

        TTEntry newEntry = new(
            key,
            score,
            move,
            depth,
            flag,
            pv,
            thread
        );

        Debug.Assert(replace >= 0 && replace < TTBucket.SIZE);
        buck[replace] = newEntry;
    }

    /// <summary>
    /// returns the how full the tt is in permille
    /// </summary>
    public unsafe int GetHashfull()
    {
        Debug.Assert(tt != null);
        Debug.Assert(size >= MIN_SIZE);

        int hashfull = 0;
        for (int i = 0; i < 1000 / TTBucket.SIZE; i++)
        {
            for (int j = 0; j < TTBucket.SIZE; j++)
            {
                if (tt[i][j].Flag != Search.NONE_BOUND)
                {
                    hashfull++;
                }
            }
        }

        return hashfull;
    }

    /// <summary>
    /// frees the allocated memory by the tt
    /// </summary>
    public unsafe void Dispose()
    {
        if (tt != null)
        {
            NativeMemory.Free(tt);
            tt = null;
        }
    }

}