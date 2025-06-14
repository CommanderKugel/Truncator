
public static partial class Search
{

    public static unsafe void IterativeDeepen(SearchThread thread)
    {
        thread.nodeCount = 0;

        int alpha = -SCORE_MATE;
        int beta = SCORE_MATE;

        for (
            int depth = 1;
            thread.doSearch && !TimeManager.IsSoftTimeout(thread, depth) && depth <= TimeManager.maxDepth || depth <= 3;
            depth++)
        {
            
            thread.seldepth = depth;
            int rootScore = Negamax<RootNode>(thread, thread.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply]);

            if (-Math.Abs(rootScore) != SCORE_TIMEOUT)
            {
                thread.completedDepth = depth;
            }

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

}
