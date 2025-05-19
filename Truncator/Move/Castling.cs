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

    public static int GetKingCastlingTarget(Color c, bool kingside)
        => GetKingCastlingTarget(c, kingside ? 1 : 0);

    public static unsafe int GetKingCastlingTarget(Color c, int kingside)
    {
        Debug.Assert(kingside == 0 || kingside == 1);
        Debug.Assert(c != Color.NONE);
        return kingTargets[kingside + ((int)c * 2)];
    }

    public static int GetKingDestination(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return (kingside ? (int)Square.G1 : (int)Square.C1) ^ (56 * (int)c);
    }

    public static unsafe ulong GetCastlingBlocker(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return paths[((int)c * 2) + (kingside ? 1 : 0)]; // QKqk
    }

    /// <summary>
    /// Castling Moves are encoded as king captures rook.
    /// returns (kingEndSq, rookEndSq)
    /// </summary>
    public static (int, int) GetCastlingSquares(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        int kingEnd = (int)(kingside ? Square.G1 : Square.C1) ^ ((int)c * 56);
        int rookEnd = (int)(kingside ? Square.F1 : Square.D1) ^ ((int)c * 56);
        return (kingEnd, rookEnd);
    }


    public static unsafe byte* modifier = null;
    public static unsafe int* kingTargets = null;
    public static unsafe ulong* paths = null;

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

        kingTargets = (int*)NativeMemory.Alloc(sizeof(int) * 4);
        kingTargets[0] = (int)Square.A1;
        kingTargets[1] = (int)Square.H1;
        kingTargets[2] = (int)Square.A8;
        kingTargets[3] = (int)Square.H8;
        
        paths = (ulong*)NativeMemory.Alloc(sizeof(ulong) * 4);
        paths[0] = 0xE; // Q
        paths[1] = 0x60; // K
        paths[2] = 0xe00000000000000; // q
        paths[3] = 0x6000000000000000; // k
    }

    public static unsafe void UpdateNewPosition(ref Pos p)
    {
        for (int f = 0; f < 8; f++)
        {
            modifier[f] = 0xF;
            modifier[56 + f] = 0xF;
        }

        if (p.HasCastlingRight(Color.White, false)) // Q
        {
            int rook = Utils.lsb(p.GetPieces(Color.White, PieceType.Rook));
            kingTargets[0] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.White, false);
            modifier[p.KingSquares[(int)Color.White]] &= modifier[rook];
            paths[0] = Utils.GetRay(p.KingSquares[(int)Color.White], (int)Square.C1) | (1ul << (int)Square.C1)
                     | Utils.GetRay(rook, (int)Square.D1) | (1ul << (int)Square.D1);
        }

        if (p.HasCastlingRight(Color.White, true)) // K
        {
            int rook = Utils.msb(p.GetPieces(Color.White, PieceType.Rook));
            kingTargets[1] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.White, true);
            modifier[p.KingSquares[(int)Color.White]] &= modifier[rook];
            paths[1] = Utils.GetRay(p.KingSquares[(int)Color.White], (int)Square.G1) | (1ul << (int)Square.G1)
                     | Utils.GetRay(rook, (int)Square.F1) | (1ul << (int)Square.F1);
        }

        if (p.HasCastlingRight(Color.Black, false)) // q
        {
            int rook = Utils.lsb(p.GetPieces(Color.Black, PieceType.Rook));
            kingTargets[2] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.Black, false);
            modifier[p.KingSquares[(int)Color.Black]] &= modifier[rook];
            paths[2] = Utils.GetRay(p.KingSquares[(int)Color.Black], (int)Square.C8) | (1ul << (int)Square.C8)
                     | Utils.GetRay(rook, (int)Square.D8) | (1ul << (int)Square.D8);
        }

        if (p.HasCastlingRight(Color.Black, true)) // k
        {
            int rook = Utils.msb(p.GetPieces(Color.Black, PieceType.Rook));
            kingTargets[3] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.Black, true);
            modifier[p.KingSquares[(int)Color.Black]] &= modifier[rook];
            paths[3] = Utils.GetRay(p.KingSquares[(int)Color.Black], (int)Square.G8) | (1ul << (int)Square.G8)
                     | Utils.GetRay(rook, (int)Square.F8) | (1ul << (int)Square.F8);
        }
    }

    public unsafe static void Dispose()
    {
        if (modifier is not null)
        {
            NativeMemory.Free(modifier);
            NativeMemory.Free(paths);
            NativeMemory.Free(kingTargets);
            modifier = null;
            paths = null;
            kingTargets = null;
        }
    }
}