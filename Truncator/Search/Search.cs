
public static class Search
{

    public static void GetBestMove(SearchThread thread)
    {
        Move bestMove = GetRandomMove(thread);

        if (thread.IsMainThread)
        {
            Console.WriteLine($"bestmove {bestMove}");
        }
    }

    public static Move GetRandomMove(SearchThread thread)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGen.GenerateLegaMoves(ref moves, ref thread.p);

        Random rng = new Random();
        return moves[rng.Next() % moveCount];
    }

}
