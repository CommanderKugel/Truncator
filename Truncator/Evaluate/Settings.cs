public static class Settings
{

    public const int IN_SIZE = 768;
    public const int L2_SIZE = 1024;

    public const int OUTPUT_BUCKETS = 8;

    public const int EVAL_SCALE = 400;

    public const string NET_NAME = "matdistskipping";

    public const int QA = 255;
    public const int QB = 64;

    public static ReadOnlySpan<int> OutputBucketScheme => [
        0, 0, 0, 0, 0,  //  0,  1,  2,  3,  4, ~mostly irrelevant
        0, 0, 0, 0, 0,  //  5,  6,  7,  8,  9,
        1, 1, 1, 1,     // 10, 11, 12, 13
        2, 2, 2,        // 14, 15, 16,
        3, 3, 3,        // 17, 18, 19,
        4, 4, 4,        // 20, 21, 22,
        5, 5, 5,        // 23, 24, 25,
        6, 6, 6,        // 26, 27, 28,
        7, 7, 7, 7,     // 29, 30, 31, 32,
    ];

}
