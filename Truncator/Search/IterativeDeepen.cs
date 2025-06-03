
using System.Diagnostics;

public static partial class Search
{

    public static unsafe void IterativeDeepen(SearchThread thread)
    {
        thread.nodeCount = 0;

        int alpha = -SCORE_MATE;
        int beta = SCORE_MATE;

        for (
            int depth = 1;
            thread.doSearch && !TimeManager.IsSoftTimeout(depth) && depth <= TimeManager.maxDepth || depth <= 3;
            depth++)
        {
            
            thread.currIteration = depth;
            thread.seldepth = depth;

            int rootScore = Negamax<RootNode>(thread, UCI.rootPos.p, alpha, beta, depth, &thread.nodeStack[thread.ply]);

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

                    Console.WriteLine($"info depth {depth} seldepth {thread.seldepth} nodes {nodes} time {time} nps {nps} score cp {score} pv {thread.GetPV}");
                }
            }
        }

        Move bestMove = thread.pv_.BestMove;
        Move ponderMove = thread.pv_.PonderMove;
        Debug.Assert(bestMove.NotNull, "bestmove cant be null! something went wrong");
        Debug.Assert(ponderMove.NotNull, "pondermove cant be null! something went wrong");
        
        Console.WriteLine($"bestmove {thread.pv_.BestMove} ponder {ponderMove}");
    }

}
