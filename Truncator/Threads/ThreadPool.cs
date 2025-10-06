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
    private static Random TempRng;

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
        if (UCI_Temperature is not null)
        {
            ReportBestMoveWithTemperature();
            return;
        }

        var bestThread = GetBestThread();

        while (bestThread.rootPos.PVs[0].BestMove.IsNull)
        {
            bestThread = GetBestThread();
        }

        ref var bestPv = ref bestThread.rootPos.PVs[0];
        Console.WriteLine($"info depth {bestThread.completedDepth} seldepth {bestThread.seldepth} score cp {bestPv[bestThread.completedDepth]} nodes {GetNodes()} time {TimeManager.ElapsedMilliseconds} pv {bestPv.ToString()}");
        Console.WriteLine($"bestmove {bestPv.BestMove} ponder {bestPv.PonderMove}");

        Console.WriteLine($"bestmove {bestPv.BestMove} ponder {bestPv.PonderMove}");
    }


    public static void InitTemperature(double temp)
    {
        UCI_Temperature = temp;
        TempRng ??= new();
    }

    public static void ReportBestMoveWithTemperature()
    {
        Debug.Assert(UCI_Temperature is not null);
        Debug.Assert(UCI_MultiPVCount > 1);

        int pvCount = Math.Min(UCI_MultiPVCount, MainThread.rootPos.RootMoves.Count);

        // collect pv-scores and later choose move based on scores index

        List<int> scores = [];

        for (int i = 0; i < pvCount; i++)
        {
            int s = MainThread.rootPos.PVs[i][MainThread.completedDepth];
            scores.Add(s);
        }

        // dont do random-moves for big scores

        if (Math.Abs(scores[0]) >= 750)
        {
            ref var fixPv = ref MainThread.rootPos.PVs[0];
            Console.WriteLine($"info depth {MainThread.completedDepth} seldepth {MainThread.seldepth} score cp {fixPv[MainThread.completedDepth]} nodes {GetNodes()} time {TimeManager.ElapsedMilliseconds} pv {fixPv.ToString()}");
            Console.WriteLine($"bestmove {fixPv.BestMove} ponder {fixPv.PonderMove}");
            return;
        }

        // compute logits

        var max = scores.Max();
        var expScores = scores.Select(x => Math.Exp((double)(x) / 100.0d / ((double)UCI_Temperature))).ToList();

        // normalize logits

        double sum = expScores.Sum();
        var probs = expScores.Select(s => s / sum).ToList();

        // random sampling

        double rand = TempRng.NextDouble();
        int choiceIdx = 0;

        for (double total = 0.0d; choiceIdx < pvCount; choiceIdx++)
        {
            total += probs[choiceIdx];

            if (total >= rand)
            {
                break;
            }
        }

        // better not mess with false mates

        if (Search.IsTerminal(scores.Max()) || Search.IsTerminal(scores.Min()))
        {
            choiceIdx = 0;
        }

        // uci reporting
        // fix awkward multipv info printing here
        // because it does not properly work with cutechess right now

        ref var chosenPv = ref MainThread.rootPos.PVs[choiceIdx];
        Console.WriteLine($"info depth {MainThread.completedDepth} seldepth {MainThread.seldepth} score cp {chosenPv[MainThread.completedDepth]} nodes {GetNodes()} time {TimeManager.ElapsedMilliseconds} pv {chosenPv.ToString()}");
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
