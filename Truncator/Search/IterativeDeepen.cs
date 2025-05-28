
public static partial class Search
{

    public static void IterativeDeepen(SearchThread thread)
    {
        thread.nodeCount = 0;

        int alpha = -SCORE_MATE;
        int beta = SCORE_MATE;

        int rootscore = Negamax<RootNode>(thread, UCI.rootPos.p, alpha, beta, 3);

        /*
        for (
            int depth = 1;
            thread.doSearch && !TimeManager.IsSoftTimeout(depth) || depth <= 4;
            depth++)
        {

            Negamax<RootNode>(thread, UCI.rootPos.p, alpha, beta, depth);

            if (thread.IsMainThread)
            {
                unsafe
                {
                    int idx = UCI.rootPos.GetBestIndex();

                    Move bestmove = new(UCI.rootPos.rootMoves[idx]);
                    int score = UCI.rootPos.moveScores[idx];
                    long nodes = ThreadPool.GetNodes();

                    long time = TimeManager.ElapsedMilliseconds;
                    long nps = nodes * 1000 / time;

                    Console.WriteLine($"info depth {depth} nodes {nodes} time {time} nps {nps} score cp {score} pv {bestmove}");
                }
            }
        }
        */

        Console.WriteLine($"info depth {3} score cp {rootscore}");
        Console.WriteLine($"bestmove {UCI.rootPos.GetBestMove()}");
    }

}
