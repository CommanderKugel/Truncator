
public static class WDL
{
    public static bool UCI_showWDL = false;

    /*
    Sigmoid with a and b approx. poly2(s) = c0 + s * (c1 + s * c2)
    for -300 < s < 300

    const double c0 = 217.27325121630592;  -> ~ 217
    const double c1 = 1.6805903050754072;  -> ~ * 42 / 25
    const double c2 = 0.03211224307882097; -> ~ * 4 / 125
    */

    /// <summary>
    /// Approximates Sigmoid((a-x)/b) for a=105, b=95, -300 < s < 300
    /// just returns 100% win/loss for scores outside of bounds
    /// </summary>
    private static int ApproxSigmoid(int s)
        => s < -300 ? -1000 : s > 300 ? 1000
        : 217 + s * (42 + s * 4 / 125) / 25;

    /// <summary>
    /// Approximates the fitted WDL Model with a polynomial,
    /// only usefull for scores in [-300, 300] range
    /// </summary>
    public static (int, int, int) ApproxWDLModel(int score)
    {
        int w = ApproxSigmoid(score);
        int l = ApproxSigmoid(-score);
        int d = 1000 - w - l;
        return (w, d, l);
    }


    const double a = 105.25220935229856;
    const double b = 95.9031373937154;

    /// <summary>
    /// Sigmoid fitted to match observed WDL data
    /// sigmoid(x) = 1.0 / (1.0 + exp(-(x-a)/b))
    /// </summary>
    private static double WeightedSigmoid(double x) => 1.0 / (1.0 + Math.Exp(-(x - a) / b));

    public static (double, double, double) GetWDL(int score)
    {
        double w = WeightedSigmoid(score);
        double l = WeightedSigmoid(-score);
        double d = 1.0 - w - l;
        return (w, d, l);
    }

    public static int NormalizedScoreFromWDL(double w, double d, double l)
    {
        double score = w + d / 2.0;
        int ret = (int)(400 * Math.Log10(score / (1.0 - score)));
        return ret;
    }

}
