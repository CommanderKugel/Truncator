
using System.Numerics;

public static class Rng
{

    private static ulong s0 = 1321861022983091513;
    private static ulong s1 = 3123198108391880477;
    private static ulong s2 = 1451815097307991481;
    private static ulong s3 = 5520930533486498032;

    /// <summary>
    /// XoShiRo implementation from https://de.wikipedia.org/wiki/Xorshift
    /// because C# Standard Rng is not as randomg as one would like it to be
    /// </summary>
    public static ulong XoShiRoNext()
    {
        // pseudo-randomg numbers are generated due to the nature
        // of the xor operation
        ulong t = s1 << 17;
        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;
        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 45);

        return s3 + s0;
    }
}
