public static partial class Search
{

    public const int SCORE_MATE = 30_000;
    public const int SCORE_DRAW = 0;

    public static bool IsTerminal(int score) => Math.Abs(score) >= SCORE_MATE;

    public const int NONE_BOUND = 0b00,
                     UPPER_BOUND = 0b01,
                     LOWER_BOUND = 0b10,
                     EXACT_BOUND = 0b11;

    private static ReadOnlySpan<byte> Log_ => [
        1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5,
    ];

}