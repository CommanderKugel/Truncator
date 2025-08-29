
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

        int delta = Tunables.AspDelta;
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

            // lower alpha on fail-lows

            if (score <= alpha)
            {
                alpha = IsLoss(score) ? -SCORE_MATE : alpha - delta;
                delta += delta / 2;
                continue;
            }

            // increase beta on fail-highs

            if (score >= beta)
            {
                beta = IsWin(score) ? SCORE_MATE : beta + delta;
                delta += delta / 2;
                continue;
            }

            // only return exact scores

            return score;
        }
    }

}
