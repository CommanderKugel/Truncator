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

        foreach (var thread in pool)
        {
            thread.Go();
        }
    }

    public static void ClearAll()
    {
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

    public static unsafe void ReportToUCI()
    {
        long nodes = 0;
        foreach (var thread in pool)
        {
            nodes += thread.nodeCount;
        }

        int bestIdx = UCI.rootPos.GetBestIndex();
        Move bestMove = new(UCI.rootPos.rootMoves[bestIdx]);
        int score = UCI.rootPos.moveScores[bestIdx];
        int depth = UCI.rootPos.completedDepth[bestIdx];

        long nps = nodes * 1000 / Math.Max(TimeManager.ElapsedMilliseconds, 1);

        Console.WriteLine($"info depth {depth} nps {nps} score cp {score} pv {bestMove}");
    }

    /// <summary>
    /// Stops all Search Threads managed by this ThreadPool
    /// </summary>
    public static void Join()
    {
        foreach (var thread in pool)
        {
            thread.Join();
        }
    }
}
