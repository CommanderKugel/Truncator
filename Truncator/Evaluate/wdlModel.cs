
public static class WDL
{
    public static bool UCI_showWDL = false;
    public static bool UCI_NormaliseScore = true;

    private static ReadOnlySpan<double> a_s => [
        108.301, -259.159, 28.935, 280.108,
    ];

    private static ReadOnlySpan<double> b_s => [
        28.550, -185.027, 331.834, 23.372
    ];


    /// <summary>
    /// Computes the predicted Win/Draw/Loss probabilities
    /// fitted to data gathered from ltc selfplay games
    /// </summary>
    public static (int, int, int, int) GetWDL(int score, int mom)
    {
        if (Search.IsTerminal(score))
        {
            return score > 0 ? (score, 1000, 0, 0) : (score, 0, 0, 1000);
        }

        double a = ((a_s[0] * mom / 58 + a_s[1]) * mom / 58 + a_s[2]) * mom / 58 + a_s[3];
        double b = ((b_s[0] * mom / 58 + b_s[1]) * mom / 58 + b_s[2]) * mom / 58 + b_s[3];

        double weighted_sigmoid(int x) => 1.0d / (1.0d + Math.Exp(-(x - a) / b));

        int w = (int)(1000 * weighted_sigmoid(score));
        int l = (int)(1000 * weighted_sigmoid(-score));
        int d = (int)(1000 - w - l);

        return ((int)(score * 100 / a), w, d, l);
    }

}
