
using System.Diagnostics;

public struct CorrPsqt
{
    public const int SIZE = 2 * 6 * 64;

    private unsafe fixed int table[SIZE];


    public unsafe int this[int idx]
    {
        get
        {
            Debug.Assert(idx >= 0 && idx < SIZE);
            return table[idx];
        }
        set
        {
            Debug.Assert(idx >= 0 && idx < SIZE);
            table[idx] = value;
        }
    }


    /// <summary>
    /// Update the Correction PSQT on the current position and search result
    /// </summary>
    public unsafe void Update(ref Pos p, int eval, int score)
    {
        // for now, just update the value by 1 in either direction
        int delta = p.Us == Color.White ? (eval < score ? 1 : -1) : (eval < score ? -1 : 1);

        for (Color c = Color.White; c <= Color.Black; c++)
        {
            int d = c == Color.White ? delta : -delta;

            for (PieceType pt = PieceType.Pawn; pt <= PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces(c, pt);

                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces);
                    int idx = (int)c * 6 * 64 + (int)pt * 64 + (sq ^ ((int)c * 56));
                    table[idx] += d;
                }
            }
        }
    }

    /// <summary>
    /// Fill the table with Zeroes
    /// </summary>
    public unsafe void Clear()
    {
        for (int i = 0; i < SIZE; i++)
        {
            table[i] = 0;
        }
    }

}
