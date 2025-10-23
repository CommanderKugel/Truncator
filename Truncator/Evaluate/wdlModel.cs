
public static class WDL
{
    public static bool UCI_showWDL = false;

    private static ReadOnlySpan<double> a_s => [
        -286.38282197, 481.40620520, -766.67969579, 1455.15773716
    ];

    private static ReadOnlySpan<double> b_s => [
        -265.30570860, 524.70618459, -176.87926038, 374.60512747
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
