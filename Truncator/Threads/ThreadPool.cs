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

    public static double? UCI_Temperature = null;
    private static Random rng;


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
            var (norm_score, w, d, l) = WDL.GetWDL(dirty_score);
            string scoreString = Search.IsTerminal(dirty_score) ? $"mate {(Math.Abs(dirty_score) - Search.SCORE_MATE) / 2}" : $"cp {norm_score}";

            string wdl = WDL.UCI_showWDL ? $" wdl {(int)(w * 1000)} {(int)(d * 1000)} {(int)(l * 1000)}" : "";

            return $" score {scoreString}{wdl} {MainThread.rootPos.PVs[idx].ToString()}";
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
        if (UCI_MultiPVCount != 1 && UCI_Temperature is not null)
        {
            ReportRandomizedMultiPvMove();
            return;
        }

        var bestThread = GetBestThread();

        while (bestThread.rootPos.PVs[0].BestMove.IsNull)
        {
            bestThread = GetBestThread();
        }

        Console.WriteLine($"bestmove {bestThread.rootPos.PVs[0].BestMove} ponder {bestThread.rootPos.PVs[0].PonderMove}");
    }

    public static void ReportRandomizedMultiPvMove()
    {
        Debug.Assert(ThreadCount == 1, "only supported for single threaded search for now");

        // fetch scores of all pvs

        List<int> scores = [];

        for (int i = 0; i < Math.Min(UCI_MultiPVCount, MainThread.rootPos.moveCount); i++)
        {
            int score = MainThread.rootPos.PVs[i][MainThread.completedDepth];
            scores.Add(score);
            Debug.WriteLine($"{MainThread.rootPos.PVs[i].BestMove} - {score}");
        }

        // discard if scores get too high or low

        int min = scores.Min();
        int max = scores.Max();

        if (min < -500 || max > 500)
        {
            Console.WriteLine($"bestmove {MainThread.rootPos.PVs[0].BestMove} ponder {MainThread.rootPos.PVs[0].PonderMove}");
            return;
        }

        // compute probabilities of which move to choose

        var smallerScores = scores.Select(s => (double)(s - max) / max).ToList();
        var logits = smallerScores.Select(s => Math.Exp(s / (double)UCI_Temperature)).ToList();
        var probs = logits.Select(s => s / logits.Sum()).ToList();

        // ToTest: if (probs.Min() < n%) play bestmove
        // maybe position is tactical and a low-prob random move blunders the game (?)

        // choose random move by probability

        rng ??= new();
        double rand = rng.NextDouble();

        double sum = 0.0d;
        int choice = 0;

        foreach (var p in probs)
        {
            sum += p;

            if (sum > rand)
                break;

            // ToDo: if (p < n%) break
        }

        // final UCI reporting

        ref var chosenPv = ref MainThread.rootPos.PVs[choice];
        Console.WriteLine($"info depth {MainThread.completedDepth} seldepth {MainThread.seldepth} score cp {MainThread.rootPos.PVs[0][MainThread.completedDepth]} time {TimeManager.ElapsedMilliseconds} nodes {GetNodes()}");
        Console.WriteLine($"bestmove {chosenPv.BestMove} ponder {chosenPv.PonderMove}");
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
