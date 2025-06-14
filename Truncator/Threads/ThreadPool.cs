using System.Diagnostics;

public static class ThreadPool
{

    public const int MIN_THREAD_COUNT = 1;
    public const int MAX_THREAD_COUNT = 16;

    private static volatile SearchThread[] pool;
    public static SearchThread MainThread => pool[0];
    public static int ThreadCount => pool.Length;

    public static TranspositionTable tt = new TranspositionTable();


    static ThreadPool()
    {
        pool = [ new SearchThread(0) ];
    }

    /// <summary>
    /// Call this via uci options
    /// Changes the amount of threads used to search 
    /// </summary>
    /// <param name="count"></param>
    public static void Resize(int count)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts all Search Threads managed by this ThreadPool.
    /// Does not return anything, to not clutter uci command parsing!
    /// </summary>
    public static void Go()
    {
        Debug.Assert(ThreadCount == 1, "Multithreading is not implemented yet!");

        for (int i = 0; i < ThreadCount; i++)
        {
            pool[i].rootPos = MainThread.rootPos;
            pool[i].repTable.CopyFrom(ref MainThread.repTable);
            pool[i].Go();
        }

        // delay using the main thread to not alter its repetition table
        // while other threads copy from it
        MainThread.Go();
    }

    public static unsafe void ReportToUci()
    {
        SearchThread bestThread = GetBestThread();

        Move bestMove = bestThread.pv_.BestMove;
        int rootIdx = bestThread.rootPos.IndexOfMove(bestMove) ?? 256;

        if (rootIdx == 256)
        {
            throw new Exception("something went wrong checking the best move!");
        }

        
    }

    /// <summary>
    /// choose the best thread from all search threads to report the best move from
    /// </summary>
    public static SearchThread GetBestThread()
    {
        SearchThread bestThread = pool[0];
        int bestDepth = bestThread.completedDepth;
        int bestScore = bestThread.RootScore;

        for (int i = 0; i < ThreadCount; i++)
        {
            var thread = pool[i];
            int depth = pool[i].completedDepth;
            int score = pool[i].RootScore;

            // if depths are equal, choose the higher score
            if (depth == bestDepth && score > bestScore)
            {
                bestThread = thread;
                bestDepth = depth;
                bestScore = score;
            }

            // if depths are not equal, choose the higher one
            // except if a mate was found, then always choose the shorter path to mate
            if (depth > bestDepth && (score > bestScore || !Search.IsTerminal(bestScore)))
            {
                bestThread = thread;
                bestDepth = depth;
                bestScore = score;
            }
        }

        return bestThread;
    }

    public static void Clear()
    {
        tt.Clear();

        foreach (var thread in pool)
        {
            thread.Clear();
        }
    }

    public static void StopAll()
    {
        foreach (var thread in pool)
        {
            thread.Stop();
        }
    }

    public static long GetNodes()
    {
        long nodes = 0;
        foreach (var thread in pool)
        {
            nodes += thread.nodeCount;
        }
        return nodes;
    }

    /// <summary>
    /// Stops all Search Threads managed by this ThreadPool
    /// </summary>
    public static void Join()
    {
        tt.Dispose();

        foreach (var thread in pool)
        {
            thread.Join();
        }
    }
}
