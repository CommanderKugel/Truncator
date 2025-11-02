
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
        var bookStartingIndex = seed & 0xFFFF_FFFF;

        bool dfrc = args.Length > 6 && args[6] == "dfrc";
        Castling.UCI_Chess960 = dfrc;

        Console.WriteLine($"info string generating {N} fens, using the seed {seed}, book found at {BookPath}, and dfrc={dfrc}");

        // ignore extra for now

        Random rng = new Random(seed);
        SearchThread thread = ThreadPool.MainThread;
        Span<Move> moves = stackalloc Move[256];

        // open book and read fens from there

        StreamReader book = null;

        if (BookPath != "None")
        {
            Console.WriteLine($"info string trying to use opening book");
            book = new(Path.Combine(Directory.GetCurrentDirectory(), BookPath));

            // skip to part of book assigned to this instance
            
            for (int i=0; i<bookStartingIndex; i++)
            {
                book.ReadLine();
            }
        }
        
        // make random moves from book/dfrc/starting position

        int RandomMoves = 4;

        for (int positions = 0; positions < N;)
        {
            try
            {
                string fen = book != null ? (book.ReadLine() ?? Utils.startpos)
                    : !dfrc ? Utils.startpos
                    : Frc.GetDfrcFen(rng.Next() % 960, rng.Next() % 960);
                
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

                    thread.Clear(clearRootPos: false);
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