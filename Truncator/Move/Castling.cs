using System.Diagnostics;
using System.Runtime.InteropServices;

public static class Castling
{
    const byte WhiteKingside  = 0b0010,
               WhiteQueenside = 0b0001,
               BlackKingside  = 0b1000,
               BlackQueenside = 0b0100;

    public static byte GetCastlingRightMask(Color c, bool kingside) 
    {
        Debug.Assert(c != Color.NONE);
        return (c == Color.White) ? 
            (kingside ? WhiteKingside : WhiteQueenside) :
            (kingside ? BlackKingside : BlackQueenside);
    }

    public static unsafe ulong GetCastlingBlocker(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return paths[((int)c * 2) + (kingside ? 1 : 0)];
    }

    public static (int, int) GetRookCastlingSquares(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        int start = (int)(kingside ? Square.H1 : Square.A1) ^ ((int)c * 56);
        int end   = (int)(kingside ? Square.F1 : Square.D1) ^ ((int)c * 56);
        return (start, end);
    }

    public static (int, int) GetKingCastlingSquares(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        int start = (int) Square.E1                         ^ ((int)c * 56);
        int end   = (int)(kingside ? Square.G1 : Square.C1) ^ ((int)c * 56);
        return (start, end);
    }


    public static unsafe byte* modifier = null;
    public static unsafe ulong* paths = null;
    public static unsafe int* kingTargets = null;

    static unsafe Castling()
    {
        modifier = (byte*)NativeMemory.Alloc(64);
        for (int sq = 0; sq < 64; sq++)
        {
            modifier[sq] = 0xF;
        }

        modifier[(int)Square.A1] &= (byte)~GetCastlingRightMask(Color.White, false);
        modifier[(int)Square.H1] &= (byte)~GetCastlingRightMask(Color.White, true);
        modifier[(int)Square.E1] &= (byte)(modifier[(int)Square.A1] & modifier[(int)Square.H1]);

        modifier[(int)Square.A8] &= (byte)~GetCastlingRightMask(Color.Black, false);
        modifier[(int)Square.H8] &= (byte)~GetCastlingRightMask(Color.Black, true);
        modifier[(int)Square.E8] &= (byte)(modifier[(int)Square.A8] & modifier[(int)Square.H8]);

        paths = (ulong*)NativeMemory.Alloc(sizeof(ulong) * 4);
        paths[0] = 0xE; // Q
        paths[1] = 0x60; // K
        paths[2] = 0xe00000000000000; // q
        paths[3] = 0x6000000000000000; // 

        kingTargets = (int*)NativeMemory.Alloc(sizeof(int) * 4);
        kingTargets[0] = (int)Square.C1;
        kingTargets[1] = (int)Square.G1;
        kingTargets[2] = (int)Square.C8;
        kingTargets[3] = (int)Square.G8;
    }

    public unsafe static void Dispose()
    {
        if (modifier is not null)
        {
            NativeMemory.Free(modifier);
            NativeMemory.Free(paths);
            modifier = null;
            paths = null;
        }
    }
}