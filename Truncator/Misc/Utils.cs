using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;


public static class Utils
{
    public const string startpos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    public static int lsb(ulong bb) => BitOperations.TrailingZeroCount(bb);

    public static int msb(ulong bb)
    {
        while (bb != 0)
        {
            int sq = popLsb(ref bb);
            if (bb == 0)
            {
                return sq;
            }
        }
        return 64;
    }

    public static int popLsb(ref ulong bb)
    {
        int lsb = BitOperations.TrailingZeroCount(bb);
        Debug.Assert(lsb >= 0 && lsb < 64);
        bb &= bb - 1;
        return lsb;
    }

    public static int popcnt(ulong bb) => BitOperations.PopCount(bb);

    public static bool MoreThanOne(ulong bb)
        => (bb & (bb - 1)) != 0;


    public static int FileOf(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        return sq & 7; // sq % 8
    }

    public static int RankOf(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        return sq >> 3; // sq / 8
    }


    /// <summary>
    /// Copied this implementation verbatim from Sirius
    /// https://github.com/mcthouacbb/Sirius/blob/258f3eb1ce43c2d428484639b873b86ff50f5d97/Sirius/src/util/murmur.h#L5
    /// </summary>
    public static unsafe ulong murmurHash(ulong key)
    {
        key ^= key >> 33;
        key *= 0xff51afd7ed558ccd;
        key ^= key >> 33;
        key *= 0xc4ceb9fe1a85ec53;
        key ^= key >> 33;
        return key;
    }


    private static unsafe ulong* Rays = null;

    public static unsafe ulong GetRay(int x, int y)
    {
        Debug.Assert(x >= 0 && x < 64);
        Debug.Assert(y >= 0 && y < 64);
        return Rays[x * 64 + y];
    }

    static unsafe Utils()
    {
        Rays = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * 64 * 64, sizeof(ulong) * 64);

        for (int ksq = 0; ksq < 64; ksq++)
        {
            for (int sq = 0; sq < 64; sq++)
            {
                ulong block = (1ul << ksq) | (1ul << sq);

                if (ksq == sq)
                {
                    Rays[ksq * 64 + sq] = 0;
                }
                else if (FileOf(ksq) == FileOf(sq) || RankOf(ksq) == RankOf(sq) && sq != ksq)
                {
                    ulong ray = Attacks.ratt(ksq, block) & Attacks.ratt(sq, block);
                    Rays[ksq * 64 + sq] = ray;
                }
                else if ((Attacks.BishopAttacksEmpty[ksq] & (1ul << sq)) != 0 && sq != ksq)
                {
                    ulong ray = Attacks.batt(ksq, block) & Attacks.batt(sq, block);
                    Rays[ksq * 64 + sq] = ray;
                }
                else
                {
                    Rays[ksq * 64 + sq] = 0;
                }
            }
        }
    }

    public static unsafe void Dispose()
    {
        if (Rays is not null)
        {
            NativeMemory.Free(Rays);
            Rays = null;
        }
    }


    public static int LetterToFile(char c)
    {
        Debug.Assert("abcdefgh".Contains(c), $"Invalid char parsed for File: {c}");
        return c - 'a';
    }

    public static int NumberToRank(char c)
    {
        Debug.Assert("12345678".Contains(c), $"Invalid char parsed for Rank: {c}");
        return c - '1';
    }

    
    public static unsafe string SquareToString(int sq)
    {
        Debug.Assert(sq >= 0 && sq < 64);
        int f = FileOf(sq);
        int r = RankOf(sq) + 8;
        return BoardNotation_[FileOf(sq)].ToString() + BoardNotation_[RankOf(sq) + 8];
    }

    private static ReadOnlySpan<char> BoardNotation_ => [
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
        '1', '2', '3', '4', '5', '6', '7', '8',
    ];
    


    public static Piece MakePiece(PieceType pt, Color c)
        => (Piece)(((int)c << 3) | (int)pt);

    public static PieceType PieceTypeOf(Piece p)
        => (PieceType)((int)p & 0b111); // even accepts Piece.NONE
    

    public static Color ColorOf(Piece p)
        => (Color)((int)p >> 3); // even accepts Piece.NONE

    public static char PieceChar(Piece p)
    {
        Debug.Assert(p != Piece.NONE);
        return "PNBRQKpnbrqk"[(int)ColorOf(p) * 6 + (int)PieceTypeOf(p)];
    }

    public static char PieceChar(Color c, PieceType pt)
    {
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(c != Color.NONE);
        return "PNBRQKpnbrqk"[(int)c * 6 + (int)pt];
    }


    public static void print(Pos p)
    {
        Console.WriteLine("  | a b c d e f g h |\n--+-----------------+");
        for (int r = 7; r >= 0; r--)
        {
            string str = $"{r + 1} | ";
            for (int f = 0; f < 8; f++)
            {
                int sq = r * 8 + f;
                Color c = p.ColorOn(sq);
                PieceType pt = p.PieceTypeOn(sq);
                str += (pt == PieceType.NONE || c == Color.NONE) ?
                    ". " :
                    "PNBRQKpnbrqk"[(int)p.ColorOn(sq) * 6 + (int)p.PieceTypeOn(sq)] + " ";
            }
            str += "|";
            Console.WriteLine(str);
        }
        Console.WriteLine("--+-----------------+");
        Console.WriteLine(p.GetFen());

        string CastlingRights = "";
        if (p.HasCastlingRight(Color.White, true )) CastlingRights += 'K';
        if (p.HasCastlingRight(Color.White, false)) CastlingRights += 'Q';
        if (p.HasCastlingRight(Color.Black, true )) CastlingRights += 'k';
        if (p.HasCastlingRight(Color.Black, false)) CastlingRights += 'q';
        Console.WriteLine("Castling Rights: " + (CastlingRights == "" ? "-" : CastlingRights));
    }

    public static void print(ulong bb)
    {
        Console.WriteLine("  | a b c d e f g h |\n--+-----------------+");
        for (int r=7; r>=0; r--) {
            string str = $"{r+1} | ";
            for (int f=0; f<8; f++)
            {
                int sq = r * 8 + f;
                str += ((1ul << sq) & bb) != 0 ? "X ": ". ";
            }
            str += "|";
            Console.WriteLine(str);
        }
        Console.WriteLine("--+-----------------+");
    }

}
