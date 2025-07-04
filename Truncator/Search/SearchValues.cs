public static partial class Search
{

    public const int SCORE_MATE = 32_000;
    public const int SCORE_DRAW = 0;
    public const int SCORE_EVAL_MAX = 30_000;

    public static bool IsTerminal(int score) => Math.Abs(score) > SCORE_EVAL_MAX;
    public static bool IsLoss(int score) => score < -SCORE_EVAL_MAX;

    public const int NONE_BOUND = 0b00,
                     UPPER_BOUND = 0b01,
                     LOWER_BOUND = 0b10,
                     EXACT_BOUND = 0b11;

    private static ReadOnlySpan<byte> Log_ => [
        1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
    ];

}