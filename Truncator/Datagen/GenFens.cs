using System.Diagnostics;

public static class GenFens
{

    public static unsafe void Generate(string[] args)
    {
        // accepts arguments in the form of:
        // genfens N seed S book <None|Books/my_book.epd> <?extra>

        if (!int.TryParse(args[1], out int N))
            throw new ArgumentException("info string couldnt parse the number of fens to generate!");

        if (!int.TryParse(args[3], out int seed))
            throw new ArgumentException("info string coudnt parse the seed to initialize rng from!");

        string BookPath = args[5];

        bool dfrc = args.Length > 6 && args[6] == "dfrc";
        Castling.UCI_Chess960 = dfrc;

        Console.WriteLine($"info string generating {N} fens, using the seed {seed}, book found at {BookPath}, and dfrc={dfrc}");

        // ignore extra for now

        Random rng = new(seed);
        Stopwatch watch = new();
        SearchThread thread = ThreadPool.MainThread;

        // ToDo: open books for position here

        int RandomMoves = 12;
        int searchDepth = 4;

        watch.Start();

        for (int positions = 0; positions < N;)
        {
            try
            {
                string fen = !dfrc ? Utils.startpos : Frc.GetDfrcFen(rng.Next() % 960, rng.Next() % 960);
                thread.rootPos.SetNewFen(fen);
                thread.Clear(clearRootPos: false);

                bool success = true;

                for (int i = 0; i < RandomMoves; i++)
                {
                    Debug.Assert((i & 1) == (int)thread.rootPos.p.Us);

                    // prepare search thread for multi-pv searching all the root moves

                    TimeManager.Reset();
                    TimeManager.depth = searchDepth;
                    TimeManager.Start(thread.rootPos.p.Us);
                    
                    thread.rootPos.InitRootMoves();
                    var moveCount = thread.rootPos.RootMoves.Count;

                    thread.nodeStack[0].acc.Accumulate(ref thread.rootPos.p);

                    ThreadPool.UCI_MultiPVCount = moveCount;
                    ThreadPool.UpdateMultiPv();

                    // do multi-pv search for all root-moves

                    Search.IterativeDeepen(thread, isBench: true);

                    // prepare weights with temperature to pick moves with

                    var scores = thread.rootPos.PVs.Select(pv => pv[searchDepth]).ToList();
                    var smallerScores = scores.Select(s => (s - scores.Max()) / scores.Max());
                    var logits = smallerScores.Select(s => Math.Exp(s) / 0.1).ToList();
                    var probs = logits.Select(l => l / logits.Sum()).ToList();

                    // pick a move

                    int idx = 0;
                    double bound = rng.NextDouble();
                    double cumulative = 0;

                    for (; idx < moveCount; idx++)
                    {
                        cumulative += probs[idx];

                        if (cumulative >= bound)
                            break;
                    }

                    // skip very imbalanced positions

                    if (i == RandomMoves - 1 && Math.Abs(thread.rootPos.PVs[idx][searchDepth]) > 1000)
                    {
                        success = false;
                        break;
                    }

                    // otherwise make the move

                    thread.rootPos.MakeMove(thread.rootPos.PVs[idx].BestMove);
                }

                // if the position didnt become terminal and is not terminal right now - print it

                if (success)
                {
                    Console.WriteLine($"info string genfens {thread.rootPos.p.GetFen()}");
                    positions++;
                }

                if (success && positions % 10 == 0)
                {
                    var fenps = positions * 1000 / watch.ElapsedMilliseconds;
                    Console.WriteLine($"info string {fenps} fen/s");
                }
            }
            catch
            {
                continue;
            }
        }
    }

}