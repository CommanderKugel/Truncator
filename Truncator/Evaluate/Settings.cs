public static class Settings
{

    public const int IN_SIZE = 768;
    public const int L2_SIZE = 1024;

    public const int OUTPUT_BUCKETS = 8;

    public const int EVAL_SCALE = 400;

    public const string NET_NAME = "1024_inputbuckets";

    public const int QA = 255;
    public const int QB = 64;

    public const int INPUT_BUCKETS = 4;

    public static ReadOnlySpan<int> KingBucketsLayout => [
        0, 0, 1, 1, 4, 4, 5, 5,
        2, 2, 2, 2, 6, 6, 6, 6,
        3, 3, 3, 3, 8, 8, 8, 8,
        3, 3, 3, 3, 8, 8, 8, 8,
        3, 3, 3, 3, 8, 8, 8, 8,
        3, 3, 3, 3, 8, 8, 8, 8,
        3, 3, 3, 3, 8, 8, 8, 8,
        3, 3, 3, 3, 8, 8, 8, 8,
    ];

}
