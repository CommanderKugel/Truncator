
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
            thread.nodeStack[0].p = thread.rootPos.p;

            int multiPv = Math.Min(ThreadPool.UCI_MultiPVCount, thread.rootPos.RootMoves.Count);
            for (int i = 0; i < multiPv; i++)
            {
                thread.MultiPvIdx = i;
                AspirationWindows(thread, depth);
            }

            thread.completedDepth = depth;
            thread.MultiPvIdx = 0;

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
            return Negamax<RootNode>(thread, -SCORE_MATE, SCORE_MATE, depth, &thread.nodeStack[thread.ply], false);
        }

        int delta = Tunables.AspDelta;
        int alpha = thread.rootPos.PVs[thread.MultiPvIdx][thread.completedDepth] - delta;
        int beta = thread.rootPos.PVs[thread.MultiPvIdx][thread.completedDepth] + delta;

        while (true)
        {
            int score = Negamax<RootNode>(thread, alpha, beta, depth, &thread.nodeStack[thread.ply], false);

            // dont retry if the search already timed out
            if (!thread.doSearch || thread.IsMainThread && TimeManager.IsSoftTimeout(thread, depth))
            {
                return score;
            }

            // if the score falls outside the window
            // widen the window and try again 
            if (score <= alpha || score >= beta)
            {
                alpha = IsTerminal(alpha) ? -SCORE_MATE : alpha - delta * Tunables.AspWidenFactor / 1024;
                beta = IsTerminal(beta) ? SCORE_MATE : beta + delta * Tunables.AspWidenFactor / 1024;
                delta += delta * Tunables.AspDeltaGrowthFactor / 1024;
                continue;
            }

            // return an exact score
            return score;
        }
    }

}
