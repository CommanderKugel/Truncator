
public static partial class Search
{

    public static unsafe void IterativeDeepen(SearchThread thread, bool isBench = false)
    {
        thread.nodeCount = 0;
        int rootScore = 0;

        for (
            int depth = 1;
            thread.doSearch
                && !TimeManager.IsSoftTimeout(thread, depth)
                && !TimeManager.IsHardTimeout(thread)
                && depth <= TimeManager.maxDepth
            || depth <= 3;
            depth++)
        {

            thread.seldepth = depth;
            rootScore = AspirationWindows(thread, depth, rootScore);
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

    private static unsafe int AspirationWindows(SearchThread thread, int depth, int lastScore)
    {
        // dont do aspiration windows at low depth where scores fluctuate a lot
        if (depth <= 4)
        {
            int score = Negamax<RootNode>(thread, thread.rootPos.p, -SCORE_MATE, SCORE_MATE, depth, &thread.nodeStack[thread.ply], false);
            thread.PV[depth] = score;
            return score;
        }

        int delta = Tunables.AspDelta;
        int alpha = lastScore - delta;
        int beta = lastScore + delta;

        while (true)
        {
            int score = Negamax<RootNode>(thread, thread.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply], false);
            thread.PV[depth] = score;

            // dont retry if the search already timed out

            if (!thread.doSearch
                || thread.IsMainThread
                    && (TimeManager.IsSoftTimeout(thread, depth) || TimeManager.IsHardTimeout(thread)))
            {
                return lastScore;
            }

            // if the score falls outside the window
            // widen the window and try again 

            if (score <= alpha || score >= beta)
            {
                alpha = -SCORE_MATE;
                beta = SCORE_MATE;
                continue;
            }

            // return an exact score
            return score;
        }
    }

}
