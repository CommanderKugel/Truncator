
public static partial class Search
{

    public static unsafe void IterativeDeepen(SearchThread thread)
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

            if (thread.IsMainThread)
            {
                ThreadPool.ReportToUci(false);
            }
        }

        if (thread.IsMainThread)
        {
            ThreadPool.StopAll();
            ThreadPool.ReportToUci(true);
        }
    }

    private static unsafe int AspirationWindows(SearchThread thread, int depth)
    {
        // dont do aspiration windows at low depth where scores fluctuate a lot
        if (depth <= 4)
        {
            return Negamax<RootNode>(thread, thread.rootPos.p, -SCORE_MATE, SCORE_MATE, depth, &thread.nodeStack[thread.ply]);
        }

        int delta = 30;
        int alpha = thread.pv_[thread.completedDepth] - delta;
        int beta = thread.pv_[thread.completedDepth] + delta;

        while (true)
        {
            int score = Negamax<RootNode>(thread, thread.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply]);

            // dont retry if the search already timed out
            if (!thread.doSearch || thread.IsMainThread && TimeManager.IsSoftTimeout(thread, depth))
            {
                return score;
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
