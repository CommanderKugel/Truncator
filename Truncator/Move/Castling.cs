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

    /// <summary>
    /// Retuns the square of the rook to 'capture'
    /// </summary>
    public static unsafe int GetKingCastlingTarget(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return kingTargets[(kingside ? 1 : 0) + ((int)c * 2)];
    }

    /// <summary>
    /// Returns the square the king starts from
    /// </summary>
    public static unsafe int GetKingStart(Color c)
    {
        Debug.Assert(c != Color.NONE);
        return kingTargets[(int)c + 4];
    }

    /// <summary>
    /// Returns the square the king actually ends up on
    /// Will return G1, C1, G8 or C8
    /// </summary>
    public static int GetKingDestination(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return (kingside ? (int)Square.G1 : (int)Square.C1) ^ (56 * (int)c);
    }

    /// <summary>
    /// Returns a bitboard containing the squares that need to be empty
    /// for the king and rook to move across
    /// </summary>
    public static unsafe ulong GetCastlingBlocker(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        return paths[((int)c * 2) + (kingside ? 1 : 0)]; // QKqk
    }

    /// <summary>
    /// Returns the squares the King and Rook actually end up on
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

        kingTargets = (int*)NativeMemory.Alloc(sizeof(int) * 6);
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

        kingTargets[4 + (int)Color.White] = p.KingSquares[(int)Color.White];
        kingTargets[4 + (int)Color.Black] = p.KingSquares[(int)Color.Black];

        if (p.HasCastlingRight(Color.White, false)) // Q
        {
            int rook = Utils.lsb(p.GetPieces(Color.White, PieceType.Rook) & 0xFFul);
            kingTargets[0] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.White, false);
            modifier[p.KingSquares[(int)Color.White]] &= modifier[rook];
            paths[0] = Utils.GetRay(p.KingSquares[(int)Color.White], (int)Square.C1) | (1ul << (int)Square.C1)
                     | Utils.GetRay(rook, (int)Square.D1) | (1ul << (int)Square.D1);
        }

        if (p.HasCastlingRight(Color.White, true)) // K
        {
            int rook = Utils.msb(p.GetPieces(Color.White, PieceType.Rook) & 0xFF);
            kingTargets[1] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.White, true);
            modifier[p.KingSquares[(int)Color.White]] &= modifier[rook];
            paths[1] = Utils.GetRay(p.KingSquares[(int)Color.White], (int)Square.G1) | (1ul << (int)Square.G1)
                     | Utils.GetRay(rook, (int)Square.F1) | (1ul << (int)Square.F1);
        }

        if (p.HasCastlingRight(Color.Black, false)) // q
        {
            int rook = Utils.lsb(p.GetPieces(Color.Black, PieceType.Rook) & 0xFF00_0000_0000_0000ul);
            kingTargets[2] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.Black, false);
            modifier[p.KingSquares[(int)Color.Black]] &= modifier[rook];
            paths[2] = Utils.GetRay(p.KingSquares[(int)Color.Black], (int)Square.C8) | (1ul << (int)Square.C8)
                     | Utils.GetRay(rook, (int)Square.D8) | (1ul << (int)Square.D8);
        }

        if (p.HasCastlingRight(Color.Black, true)) // k
        {
            int rook = Utils.msb(p.GetPieces(Color.Black, PieceType.Rook) & 0xFF00_0000_0000_0000ul);
            kingTargets[3] = rook;
            modifier[rook] &= (byte)~GetCastlingRightMask(Color.Black, true);
            modifier[p.KingSquares[(int)Color.Black]] &= modifier[rook];
            paths[3] = Utils.GetRay(p.KingSquares[(int)Color.Black], (int)Square.G8) | (1ul << (int)Square.G8)
                     | Utils.GetRay(rook, (int)Square.F8) | (1ul << (int)Square.F8);
        }

    }

    public static unsafe bool IsUCICastlingMove(int from, int to, ref Pos p)
    {
        // assumes the moving PieceType is PieceType.King
        Debug.Assert(p.HasCastlingRight(p.Us, from < to));

        int KingEnd = !UCI.IsChess960 ? GetKingDestination(p.Us, from < to) : GetKingCastlingTarget(p.Us, from < to);
        int KingStart = GetKingStart(p.Us);
        return KingStart == from && KingEnd == to;
    }

    public static unsafe Move MakeCastingMove(Color c, bool kingside)
    {
        Debug.Assert(c != Color.NONE);
        int from = GetKingStart(c);
        int to = GetKingCastlingTarget(c, kingside);
        Console.WriteLine((Square)to);

        Debug.Assert(from >= 0 && from < 64);
        Debug.Assert(to >= 0 && to < 64);
        return new Move(from, to, MoveFlag.Castling);
    }

    public unsafe static void Dispose()
    {
        if (modifier != null)
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