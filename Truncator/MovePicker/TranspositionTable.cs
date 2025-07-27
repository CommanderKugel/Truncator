using System.Diagnostics;
using System.Runtime.InteropServices;

public class TranspositionTable : IDisposable
{
    // ToDo: Bucketing, advanced replacement policy

    public const int MIN_SIZE = 1;
    public const int MAX_SIZE = 32 * 1024;
    public const int DEFAULT_SIZE = 64;

    // the table
    private unsafe TTEntry* tt = null;
    private ulong size = 0;

    // ageing
    // store the current cycle in the newly written ttEntries 
    // to keep track of how old they are
    private int Cycle = 0;
    /// <summary>
    /// Increment the TT Cycle every uci-go-call
    /// </summary>
    public void Age() => Cycle = ++Cycle & 0b0001_1111;

    public unsafe TranspositionTable(int sizemb = DEFAULT_SIZE)
    {
        Debug.Assert(sizemb > 0, "unallowed hash size!");
        sizemb = Math.Clamp(sizemb, MIN_SIZE, MAX_SIZE);

        nuint sizeByte = (nuint)sizemb * 1024 * 1024;
        nuint entryCount = sizeByte / (nuint)sizeof(TTEntry);

        tt = (TTEntry*)NativeMemory.Alloc((nuint)sizeof(TTEntry) * entryCount);
        this.size = entryCount;
    }

    /// <summary>
    /// Reallocate the table with the corrected size
    /// table will be empty afterwards
    /// </summary>
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

        Clear();
        Console.WriteLine($"hash set to {sizemb} mb");
    }

    /// <summary>
    /// sets the tables contents and current cycle to zero
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(tt != null);
        NativeMemory.Clear(tt, (nuint)sizeof(TTEntry) * (nuint)size);
        Cycle = 0;
    }

    /// <summary>
    /// checks the ttentry corresponding to the given key.
    /// if the stored position does not match the key, an empty entry is outputtet.
    /// returns true if the stored key equals the probed key
    /// </summary>
    public unsafe bool Probe(ulong key, out TTEntry entry)
    {
        Debug.Assert(tt != null);
        ref var e = ref tt[key % size];

        if (e.Key == key)
        {
            entry = e;
            return true;
        }
        else
        {
            entry = new();
            return false;
        }
    }

    /// <summary>
    /// stores the inputtet data in the transposition table
    /// </summary>
    public unsafe void Write(ulong key, int score, Move move, int depth, int flag, bool pv, SearchThread thread)
    {
        Debug.Assert(tt != null);
        ref var entry = ref tt[key % size];

        // if we have a recent and much better entry
        //  dont replace it

        if (entry.Key == key
            && entry.Age == Cycle
            && entry.Depth - 4 > depth)
        {
            return;
        }

        entry.Key = key;
        entry.Score = TTEntry.ConvertToSavescore(score, thread.ply);
        entry.MoveValue = move.value;
        entry.Depth = (byte)depth;
        entry.PackPVAgeFlag(pv, Cycle, flag);
    }

    /// <summary>
    /// returns the approximated usage of the tt in permille
    /// </summary>
    public unsafe int GetHashfull()
    {
        Debug.Assert(tt != null);
        Debug.Assert(size >= MIN_SIZE && size >= 1000);

        int hashfull = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (tt[i].Age == Cycle)
            {
                hashfull++;
            }
        }
        return hashfull;
    }

    /// <summary>
    /// frees all allocated memory
    /// </summary>
    public unsafe void Dispose()
    {
        if (tt is not null)
        {
            NativeMemory.Free(tt);
            tt = null;
            size = 0;
            Cycle = 0;
        }
    }

}