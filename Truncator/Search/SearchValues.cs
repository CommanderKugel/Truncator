public static partial class Search
{

    public const int SCORE_MATE = 30_000;
    public const int SCORE_DRAW = 0;

    public const int SCORE_TIMEOUT = -30_001;

    public static bool IsTerminal(int score) => Math.Abs(score) >= SCORE_MATE;

    public const int NONE_BOUND = 0b00,
                     LOWER_BOUND = 0b01,
                     UPPER_BOUND = 0b10,
                     EXACT_BOUND = 0b11;

}