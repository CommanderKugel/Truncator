
public static partial class Search
{

    public static unsafe void IterativeDeepen(SearchThread thread, bool isBench = false)
    {
        thread.nodeCount = 0;

        for (
            int depth = 1;
            thread.doSearch && !TimeManager.IsSoftTimeout(thread, depth) && depth <= TimeManager.maxDepth || depth <= 3;
            depth++)
        {

            thread.seldepth = depth;
            int score = AspirationWindows(thread, depth);
            thread.completedDepth = depth;

            if (thread.IsMainThread && !isBench)
            {
                ThreadPool.ReportToUci();
            }
        }

        if (thread.IsMainThread && !isBench)
        {
            ThreadPool.StopAll();
            ThreadPool.ReportBestmove();
        }
    }

    private static unsafe int AspirationWindows(SearchThread thread, int depth)
    {
        // dont do aspiration windows at low depth where scores fluctuate a lot
        if (depth <= 4)
        {
            return Negamax<RootNode>(thread, thread.rootPos.p, -SCORE_MATE, SCORE_MATE, depth, &thread.nodeStack[thread.ply], false);
        }

        int delta = 30;
        int alpha = thread.PV[thread.completedDepth] - delta;
        int beta = thread.PV[thread.completedDepth] + delta;

        while (true)
        {
            int score = Negamax<RootNode>(thread, thread.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply], false);

            // dont retry if the search already timed out
            if (!thread.doSearch || thread.IsMainThread && TimeManager.IsSoftTimeout(thread, depth))
            {
                return score;
            }

            // return exact scores
            if (score > alpha && score < beta)
            {
                return score;
            }

            int avgSqr = thread.rootPos.GetAvgSqrScore();

            // if the score falls outside the window, widen the window and try again 
            // update on score, because it might not equal alpha/beta to begin with
            if (score <= alpha)
            {
                beta = (alpha + beta) / 2;
                alpha = Math.Max(score - avgSqr, int.MinValue);
            }

            else if (score >= beta)
            {
                // somehow most engines dont update alpha on a fail-high, so we dont (for now?)
                beta = Math.Min(score + avgSqr, int.MaxValue);
            }

            delta += delta;
            // continue with widened windows
        }
    }

}
