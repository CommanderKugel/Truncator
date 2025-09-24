
using System.Diagnostics;

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

            for (int multipv = 0; multipv < Math.Min(UCI.UCI_MultiPV, thread.rootPos.moveCount); multipv++)
            {
                Debug.Assert(multipv >= 0);
                Debug.Assert(multipv < thread.rootPos.moveCount);

                thread.MultiPVIdx = multipv;
                thread.seldepth = depth;
                AspirationWindows(thread, depth);
                thread.completedDepth = depth;    
            }

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
        int alpha = thread.rootPos.PVs[thread.MultiPVIdx][thread.completedDepth] - delta;
        int beta = thread.rootPos.PVs[thread.MultiPVIdx][thread.completedDepth] + delta;

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
                alpha -= IsTerminal(alpha) ? -SCORE_MATE : delta;
                beta += IsTerminal(beta) ? SCORE_MATE : delta;
                delta += delta;
                continue;
            }

            // return an exact score
            return score;
        }
    }

}
