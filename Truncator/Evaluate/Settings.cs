public static class Settings
{

    public const int IN_SIZE = 768;
    public const int L1_SIZE = 1024;

    public const int INPUT_BUCKETS = 6;
    public const int OUTPUT_BUCKETS = 8;

    public const int EVAL_SCALE = 400;

    public const string NET_NAME = "1024_kingbuckets";

    public const int QA = 255;
    public const int QB = 64;

    public static ReadOnlySpan<int> KingBuckets => [
        0, 1, 2, 3, 3, 2, 1, 0,
        4, 4, 4, 4, 4, 4, 4, 4,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
    ];

}
