using System.Diagnostics;

public static class ThreadPool
{

    public const int MIN_THREAD_COUNT = 1;
    public const int MAX_THREAD_COUNT = 16;

    private static volatile SearchThread[] pool;
    public static SearchThread MainThread => pool[0];
    public static int ThreadCount => pool.Length;

    public static TranspositionTable tt = new TranspositionTable();

    public static int UCI_MultiPVCount = 1;


    /// <summary>
    /// Call this via uci options
    /// Changes the amount of threads used to search 
    /// </summary>
    public static void Resize(int count)
    {
        Debug.Assert(count > 0, "there needs to be at least one thread!");
        count = Math.Clamp(count, MIN_THREAD_COUNT, MAX_THREAD_COUNT);

        // clear and dispose of all current threads
        // skip if the pool is not initialized yet

        if (pool != null)
        {
            foreach (var thread in pool)
            {
                thread.Join();
            }

            Array.Clear(pool);
            GC.Collect();
        }

        // just create some new threads

        pool = new SearchThread[count];
        for (int i = 0; i < count; i++)
        {
            pool[i] = new SearchThread(i, UCI_MultiPVCount);

            while (!pool[i].isReady)
            { }
        }

        Console.WriteLine($"info string resized the threadpool to {ThreadCount}");
    }

    /// <summary>
    /// Starts all Search Threads managed by this ThreadPool.
    /// Does not return anything, to not clutter uci command parsing!
    /// </summary>
    public static void Go()
    {
        // check if we can just pull the result from the tablebases
        if (Fathom.DoTbProbing
            && Utils.popcnt(MainThread.rootPos.p.blocker) <= Fathom.TbLargest
            && MainThread.rootPos.p.CastlingRights == 0)
        {
            TbProbeRoot();
            UCI.state = UciState.Idle;
            return;
        }

        // otherwise, search normally
        for (int i = 1; i < ThreadCount; i++)
        {
            pool[i].Reset();
            pool[i].CopyFrom(MainThread);
            pool[i].Go();
        }

        // delay using the main thread while copying from it
        // while other threads copy from it
        MainThread.Go();
    }


    public static void TbProbeRoot()
    {
        ref Pos p = ref MainThread.rootPos.p;

        Debug.Assert(Fathom.IsInitialized);
        Debug.Assert(Fathom.DoTbProbing);
        Debug.Assert(Utils.popcnt(p.blocker) <= Fathom.TbLargest);
        Debug.Assert(p.CastlingRights == 0);

        var (wdl, tbMove, dtz) = Fathom.ProbeRoot(ref p);
        string score = wdl == (int)TbResult.TbDraw ? "cp 0" : $"mate {dtz / 2}";

        Console.WriteLine($"info depth 1 score {score} tbhits {1} pv {tbMove}");
        Console.WriteLine($"bestmove {tbMove}");
    }


    /// <summary>
    /// Info Printing for UCI communication inbetween ID iterations
    /// </summary>
    public static unsafe void ReportToUci()
    {
        // info printing in between iterations
        int depth = MainThread.completedDepth;
        int seldepth = MainThread.seldepth;

        long nodes = GetNodes();
        long tbHits = GetTbHits();
        int hashfull = tt.GetHashfull();

        long time = TimeManager.ElapsedMilliseconds;
        long nps = nodes * 1000 / time;

        int mom = Utils.popcnt(MainThread.rootPos.p.PieceBB[(int)PieceType.Pawn]) * 1
            + Utils.popcnt(MainThread.rootPos.p.PieceBB[(int)PieceType.Knight]) * 3
            + Utils.popcnt(MainThread.rootPos.p.PieceBB[(int)PieceType.Bishop]) * 3
            + Utils.popcnt(MainThread.rootPos.p.PieceBB[(int)PieceType.Rook]) * 5
            + Utils.popcnt(MainThread.rootPos.p.PieceBB[(int)PieceType.Queen]) * 9;
        
        string info = $"info depth {depth} seldepth {seldepth} nodes {nodes} tbhits {tbHits} time {time} nps {nps} hashfull {hashfull}";

        // multipv dependant information

        if (MainThread.MultiPvCount == 1)
        {
            Console.WriteLine(info + GetMultipvInfo(0));
            return;
        }

        for (int i = 0; i < Math.Min(UCI_MultiPVCount, MainThread.rootPos.RootMoves.Count); i++)
        {
            info += $" multipv {i + 1}" + GetMultipvInfo(i);
        }
        Console.WriteLine(info);

        string GetMultipvInfo(int idx)
        {
            int dirty_score = MainThread.rootPos.PVs[idx][depth];
            var (norm_score, w, d, l) = WDL.GetWDL(dirty_score, mom);
            string scoreString = Search.IsTerminal(dirty_score) ? $"mate {(Math.Abs(dirty_score) - Search.SCORE_MATE) / 2}" : $"cp {(WDL.UCI_NormaliseScore ? norm_score : dirty_score)}";

            string wdl = WDL.UCI_showWDL ? $" wdl {w} {d} {l}" : "";

            return $" score {scoreString}{wdl} pv {MainThread.rootPos.PVs[idx].ToString()}";
        }
    }

    public static SearchThread GetBestThread()
    {
        SearchThread bestThread = pool[0];
        int bestDepth = bestThread.completedDepth;
        int bestScore = bestThread.rootPos.PVs[0][bestThread.completedDepth];

        for (int i = 0; i < ThreadCount; i++)
        {
            var thread = pool[i];
            int depth = pool[i].completedDepth;
            int score = pool[i].rootPos.PVs[0][depth];

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

    public static void ReportBestmove()
    {
        var bestThread = GetBestThread();

        while (bestThread.rootPos.PVs[0].BestMove.IsNull)
        {
            bestThread = GetBestThread();
        }

        Console.WriteLine($"bestmove {bestThread.rootPos.PVs[0].BestMove} ponder {bestThread.rootPos.PVs[0].PonderMove}");
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

    public static long GetTbHits()
    {
        long tbHits = 0;
        foreach (var thread in pool)
        {
            tbHits += thread.tbHits;
        }
        return tbHits;
    }

    public static void UpdateMultiPv()
    {
        foreach (var thread in pool)
        {
            thread.MultiPvCount = UCI_MultiPVCount;
            thread.rootPos.ResizeMultiPV(UCI_MultiPVCount);
        }
    }

    /// <summary>
    /// Stops all Search Threads managed by this ThreadPool
    /// </summary>
    public static void Join()
    {
        Debug.WriteLine("disposing of threadpools TT");
        tt.Dispose();

        foreach (var thread in pool)
        {
            Debug.WriteLine($"joining thread {thread.id}");
            thread.Join();
        }
    }
}
