
using System.Runtime.InteropServices;

public unsafe struct Magic : IDisposable
{
    ulong magicNumber;
    ulong mask;
    int shift;

    nuint size;
    ulong* lookup;

    public Magic(ulong magicNumber, ulong mask, int bits, int sq, Func<int, ulong, ulong> att)
    {
        this.magicNumber = magicNumber;
        this.mask = mask;
        this.shift = 64 - bits;

        this.size = (nuint)(1 << bits) * sizeof(ulong);
        this.lookup = (ulong*)NativeMemory.Alloc(size);

        fill(sq, att);
    }

    public ulong GetAttack(ulong block)
    {
        block &= mask;
        block *= magicNumber;
        block >>= shift;
        return lookup[block];
    }

    public void fill(int sq, Func<int, ulong, ulong> att)
    {
        int bits = Utils.popcnt(mask);

        for (int idx=0; idx < (1 << bits); idx++)
        {
            ulong block = IndexToBlocker(idx, mask);

            ulong key = (block * magicNumber) >> shift;
            lookup[key] = att(sq, block);
        }
    }

    private static ulong IndexToBlocker(int idx, ulong mask)
    {
        ulong res = 0;
        int bits = Utils.popcnt(mask);

        for (int i=0; i<bits; i++)
        {
            int lsb = Utils.popLsb(ref mask);
            if ((idx & (1 << i)) != 0)
            {
                res |= 1ul << lsb;
            }
        }
        return res;
    }

    public void Dispose()
    {
        if (size != 0) // dont dispose twice
        {
            NativeMemory.Free(lookup);
            size = 0;
        }
    }
}