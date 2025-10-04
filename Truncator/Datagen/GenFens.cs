
public static class GenFens
{

    public static void Generate(string[] args)
    {
        // accepts arguments in the form of:
        // genfens N seed S book <None|Books/my_book.epd> <?extra>

        if (!int.TryParse(args[1], out int N))
            throw new ArgumentException("info string couldnt parse the number of fens to generate!");

        if (!int.TryParse(args[3], out int seed))
            throw new ArgumentException("info string coudnt parse the seed to initialize rng from!");

        string BookPath = args[5];

        bool dfrc = args.Length > 6 && args[6] == "dfrc";

        Console.WriteLine($"info string generating {N} fens, using the seed {seed}, book found at {BookPath}, and dfrc={dfrc}");

        // ignore extra for now

        Random rng = new Random(seed);
        SearchThread thread = ThreadPool.MainThread;
        Span<Move> moves = stackalloc Move[256];

        // ToDo: open books for position here

        int RandomMoves = 8;

        for (int positions = 0; positions < N;)
        {
            // ToDo: read fen from book here

            try
            {

                string fen = !dfrc ? Utils.startpos : Frc.GetDfrcFen(rng.Next() % 960, rng.Next() % 960);
                thread.rootPos.SetNewFen(fen);

                bool success = true;

                for (int i = 0; i < RandomMoves; i++)
                {
                    moves.Clear();
                    int moveCount = MoveGen.GenerateLegaMoves(thread, ref moves, ref thread.rootPos.p);

                    // position is terminal, make a new one

                    if (moveCount == 0)
                    {
                        success = false;
                        break;
                    }

                    // position is not terminal, thus make a random move

                    thread.rootPos.MakeMove(moves[rng.Next() % moveCount]);
                }

                // if the position didnt become terminal and is not terminal right now - print it

                if (success && MoveGen.GenerateLegaMoves(thread, ref moves, ref thread.rootPos.p) > 0)
                {

                    TimeManager.Reset();
                    TimeManager.depth = 10;
                    TimeManager.Start(thread.rootPos.p.Us);

                    thread.Clear(containRootpos: true);
                    thread.rootPos.InitRootMoves();

                    unsafe
                    {
                        thread.nodeStack[0].acc.Accumulate(ref thread.rootPos.p);
                    }

                    Search.IterativeDeepen(thread, isBench: true);
                    int score = thread.rootPos.PVs[0][thread.completedDepth];

                    // filter out very imbalanced positions

                    if (Math.Abs(score) > 250)
                    {
                        continue;
                    }

                    Console.WriteLine($"info string genfens {thread.rootPos.p.GetFen()}");
                    positions++;
                }
            }
            catch
            {
                continue;
            }
        }
    }

}