
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

        // compute delta, alpha and beta
        // assuming the upcoming iterations score will vary just a bit from the last full iterations
        // we set alpha and beta to a window around that value
        // scores outside of that bound are just bounds and require re-searches

        int prevScore = thread.PV[thread.completedDepth];
        int delta = 10 + prevScore * prevScore / short.MaxValue;
        
        int alpha = prevScore - delta;
        int beta = prevScore + delta;

        while (true)
        {
            int score = Negamax<RootNode>(thread, thread.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply], false);

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
