
public static partial class Search
{

    public static void IterativeDeepen(SearchThread thread)
    {
        thread.nodeCount = 0;

        int alpha = -SCORE_MATE;
        int beta = SCORE_MATE;

        for (
            int depth = 1;
            thread.doSearch && !TimeManager.IsSoftTimeout(depth) && depth <= TimeManager.maxDepth || depth <= 3;
            depth++)
        {

            Negamax<RootNode>(thread, UCI.rootPos.p, alpha, beta, depth);

            if (thread.IsMainThread)
            {
                unsafe
                {
                    Move currBestMove = thread.pv_.BestMove;
                    int idx = UCI.rootPos.IndexOfMove(currBestMove) ?? 256;

                    if (idx == 256) // bestmove not found (?)
                    {
                        continue;
                    }

                    int score = UCI.rootPos.moveScores[idx];
                    long nodes = ThreadPool.GetNodes();

                    long time = TimeManager.ElapsedMilliseconds;
                    long nps = nodes * 1000 / time;

                    string pv = thread.GetPV;

                    Console.WriteLine($"info depth {depth} nodes {nodes} time {time} nps {nps} score cp {score} pv {pv}");
                }
            }
        }
        */
        
        Console.WriteLine($"info depth {3} score cp {rootscore} pv {thread.GetPV}");
        Console.WriteLine($"bestmove {thread.pv_.BestMove}");
    }

}
