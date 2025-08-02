using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

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
        Debug.Assert(count > 0, "there needs to be at least one thread!");
        count = Math.Clamp(count, MIN_THREAD_COUNT, MAX_THREAD_COUNT);

        // clear all threads
        Array.Clear(pool);
        GC.Collect();

        // fill with new threads
        pool = new SearchThread[count];
        for (int i = 0; i < count; i++)
        {
            pool[i] = new SearchThread(i);
        }

        Console.WriteLine($"resized the threadpool to {ThreadCount}");
    }

    /// <summary>
    /// Starts all Search Threads managed by this ThreadPool.
    /// Does not return anything, to not clutter uci command parsing!
    /// </summary>
    public static void Go()
    {
        for (int i = 1; i < ThreadCount; i++)
        {
            pool[i].Reset();
            pool[i].CopyFrom(MainThread);
            pool[i].Go();
        }

        // delay using the main thread to not alter its repetition table
        // while other threads copy from it
        MainThread.Go();
    }

    /// <summary>
    /// Info Printing for UCI communication inbetween ID iterations
    /// </summary>
    public static unsafe void ReportToUci()
    {
        // info printing in between iterations
        int depth = MainThread.completedDepth;
        int seldepth = MainThread.seldepth;
        int dirty_score = MainThread.PV.scores[MainThread.completedDepth];
        long nodes = GetNodes();
        long time = TimeManager.ElapsedMilliseconds;
        long nps = nodes * 1000 / time;
        int hashfull = tt.GetHashfull();

        // normalize score to +100 cp ~ 50% chance of winning
        var (norm_score, w, d, l) = WDL.GetWDL(dirty_score);
        string scoreString = Search.IsTerminal(dirty_score) ? $"mate {(Math.Abs(dirty_score) - Search.SCORE_MATE) / 2}" : $"cp {norm_score}";

        string info = $"info depth {depth} seldepth {seldepth} nodes {nodes} time {time} nps {nps} score {scoreString} hashfull {hashfull}";

        if (WDL.UCI_showWDL)
        {
            info += $" wdl {(int)(w * 1000)} {(int)(d * 1000)} {(int)(l * 1000)}";
        }

        info += $" pv {MainThread.PV.ToString()}";
        Console.WriteLine(info);
    }

    public static unsafe void ReportBestmove()
    {
        SearchThread bestThread = pool[0];
        int bestDepth = bestThread.completedDepth;
        int bestScore = bestThread.PV.scores[bestThread.completedDepth];

        for (int i = 0; i < ThreadCount; i++)
        {
            var thread = pool[i];
            int depth = pool[i].completedDepth;
            int score = pool[i].PV.scores[pool[i].completedDepth];

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

        Move bestmove = bestThread.PV.BestMove;
        Move pondermove = bestThread.PV.PonderMove;
        Console.WriteLine($"bestmove {bestmove} ponder {pondermove}");
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
