public static class Settings
{

    public const int IN_SIZE = 768;
    public const int L2_SIZE = 64;

    public const int EVAL_SCALE = 400;

    public const string NET_NAME = "64hl-0.25wdl-hm";

    public const int QA = 255;
    public const int QB = 64;

    public static ReadOnlySpan<int> KingBuckets => [
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
        0, 0, 0, 0, 1, 1, 1, 1,
    ];

}
