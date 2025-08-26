
using System.Runtime.InteropServices;
using static Tunables;

public static partial class Search
{
    public const int MAX_PLY = 256;

    public const int SCORE_MATE = 32_000 + MAX_PLY;
    public const int SCORE_MATE_IN_MAX = 32_000;

    public const int SCORE_TB_WIN = 31_000 + MAX_PLY;
    public const int SCORE_TB_WIN_IN_MAX = 31_000;

    public const int SCORE_DRAW = 0;
    public const int SCORE_EVAL_MAX = 30_000;

    public static bool IsTerminal(int score) => Math.Abs(score) > SCORE_EVAL_MAX;
    public static bool IsLoss(int score) => score < -SCORE_EVAL_MAX;
    public static bool IsMate(int score) => Math.Abs(score) >= SCORE_MATE_IN_MAX;
    public static bool IsTbScore(int score) => Math.Abs(score) >= SCORE_TB_WIN_IN_MAX && !IsMate(score);

    public const int
        NONE_BOUND = 0b00,
        UPPER_BOUND = 0b01,
        LOWER_BOUND = 0b10,
        EXACT_BOUND = 0b11;

    private static ReadOnlySpan<byte> Log_ => [
        1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
    ];


    private static unsafe int* LmrBase = (int*)NativeMemory.Alloc(sizeof(int) * 64 * 64);

    public static unsafe int GetBaseLmr(int depth, int moves)
        => LmrBase[Math.Min(depth, 63) * 64 + Math.Min(moves, 63)];

    public static unsafe void ComputeLmrTable()
    {
        for (int depth = 0; depth < 64; depth++)
        {
            for (int moves = 0; moves < 64; moves++)
            {
                int idx = depth * 64 + moves;
                LmrBase[idx] = Math.Max((LmrBaseBase + LmrBaseMult * Log_[moves] * Log_[depth] / 4) / 1024, 2);
            }
        }
    }

    public static unsafe void Dispose()
    {
        if (LmrBase != null)
        {
            NativeMemory.Free(LmrBase);
            LmrBase = null;
        }
    }

}