
public static class Params
{

    /// <summary>
    /// size of first hidden layer aka feature transformer
    /// </summary>
    public const int IN_SIZE = 768;

    /// <summary>
    /// size of hidden layer after the accumulator
    /// </summary>
    public const int L1_SIZE = 16;


    public const int SCALE = 400;

    public const short QA = 255;
    public const short QB = 64;
    public const short QAB = QA * QB;

}