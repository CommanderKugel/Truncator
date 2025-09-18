
public static class GenFens
{

    public static void Generate(string[] args)
    {
        Console.WriteLine("info string starting to generate fens");

        // accept commands of form of:
        // genfens N seed S book <None|Books/my_book.epd> <?extra>

        if (!int.TryParse(args[1], out int N))
            throw new ArgumentException("info string couldnt parse the number of fens to generate!");

        if (!int.TryParse(args[3], out int seed))
            throw new ArgumentException("info string coudnt parse the seed to initialize rng from!");

        string BookPath = args[5];

        Console.WriteLine($"info string generating {N} fens, using the seed {seed} and book found at {BookPath}");

        // ignore extra for now

        Random rng = new Random(seed);
        SearchThread thread = ThreadPool.MainThread;
        Span<Move> moves = stackalloc Move[256];

        // ToDo: open books for position here

        int RandomMoves = 8;

        for (int positions = 0; positions < N;)
        {
            // ToDo: read fen from book here

            thread.rootPos.SetNewFen(thread, Utils.startpos);
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

                thread.rootPos.MakeMove(moves[rng.Next() % moveCount], thread);
            }

            // if the position didnt become terminal and is not terminal right now - print it

            if (success && MoveGen.GenerateLegaMoves(thread, ref moves, ref thread.rootPos.p) > 0)
            {
                // ToDo: evaluate score with QSearch or low depth/nodes search
                //       and skip positions that have forced mates or are already dead lost

                Console.WriteLine($"info string genfens {thread.rootPos.p.GetFen()}");
                positions++;
            }
        }
    }

}