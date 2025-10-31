public static class Settings
{

    public const int IN_SIZE = 768;
    public const int L2_SIZE = 768;

    public const int OUTPUT_BUCKETS = 8;

    public const int EVAL_SCALE = 400;

    public const string NET_NAME = "768hl_ib-2";

    public const int QA = 255;
    public const int QB = 64;

    public const int INPUT_BUCKETS = 6;

    public static ReadOnlySpan<int> KingBucketsLayout => [
         0,  1,  2,  3,  6,  7,  8,  9,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
         4,  4,  5,  5, 10, 10, 11, 11,
    ];

}
