
using System.Diagnostics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public struct TTEntry
{
    // Size = 8 + 2 + 2 + 1 + 1 = 14 bytes

    public ulong Key;
    public short Score;
    public ushort MoveValue;
    public byte Depth;

    private byte Packed_PV_Age_Flag;
    public readonly int Flag => Packed_PV_Age_Flag & 0b11;
    public readonly int PV => (Packed_PV_Age_Flag >> 2) & 1;

    public void PackPVAgeFlag(bool pv, int age, int flag)
    {
        Debug.Assert(age >= 0);
        Debug.Assert(flag >= 0 && flag < 4);
        Packed_PV_Age_Flag = (byte)flag;
        if (pv)
        {
            Packed_PV_Age_Flag |= 0b0000_0100;
        }
    }

    public TTEntry(ulong key, int score, Move move, int depht, int flag, bool pv, SearchThread thread)
    {
        Key = key;
        Score = ConvertToSavescore(score, thread.ply);
        MoveValue = move.value;
        Depth = (byte)depht;
        PackPVAgeFlag(pv, 0, flag);
    }

    public static short ConvertToSavescore(int score, int ply)
    {
        Debug.Assert(score >= short.MinValue && score <= short.MaxValue, "Score is out of bounds!");
        return Search.IsTerminal(score) ? (short)(score + ply) : (short)score;
    }

    public static int ConvertToSearchscore(ref TTEntry entry, int ply)
    {
        return Search.IsTerminal(entry.Score) ? entry.Score - ply : entry.Score;
    }
}
