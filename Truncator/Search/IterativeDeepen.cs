
public static partial class Search
{

    public static void IterativeDeepen(SearchThread thread)
    {
        thread.nodeCount = 0;

        int alpha = -SCORE_MATE;
        int beta = SCORE_MATE;

        for (int iteration = 1; true; iteration++)
        {
            Negamax<RootNode>(thread, UCI.rootPos.p, alpha, beta, iteration);

            // report to uci
            if (thread.IsMainThread)
            {
                unsafe
                {
                    int idx = UCI.rootPos.GetBestIndex();
                    int depth = UCI.rootPos.completedDepth[idx];
                    int score = UCI.rootPos.moveScores[idx];
                    Move bestmove = new(UCI.rootPos.rootMoves[idx]);

                    long time = TimeManager.ElapsedMilliseconds;
                    long nodes = ThreadPool.GetNodes();
                    long nps = nodes * 1000 / time;

                    Console.WriteLine($"info depth {depth} time {time} score cp {score} nodes {nodes} nps {nps} pv {bestmove}");
                }
            }

            // check for breaking conditions
            if (!thread.doSearch ||
                 thread.IsMainThread && TimeManager.IsSoftTimeout(iteration) ||
                 iteration >= TimeManager.maxDepth)
            {
                break;
            }
        }

        if (thread.IsMainThread)
        {
            ThreadPool.StopAll();
            UCI.state = UciState.Idle;
            Console.WriteLine($"bestmove {UCI.rootPos.GetBestMove()}");
            return;
        }
    }

}
