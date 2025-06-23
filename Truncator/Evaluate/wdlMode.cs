using System.Data;

public static class WDL
{
    public static bool UCI_showWDL = false;

    const double a = 105.25220935229856;
    const double b = 95.9031373937154;

    /*
    maybe usefull:
    Sigmoid with a and b approx. poly4(s) = c0 + s * (c1 + s * (c2 + s * (c3 + s * c4)))
    for -300 < s < 300
    c0 = 0.19265319266057163
    c1 = 0.0019561911192871326
    c2 = 5.946239435173553e-06
    c3 = -5.1026982083248756e-09
    c4 = -3.544681089646665e-11
    */

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
