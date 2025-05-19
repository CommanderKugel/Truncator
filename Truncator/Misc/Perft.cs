using System.Diagnostics;

public static class Perft
{

    /// <summary>
    /// (fen, nodes, depth)
    /// </summary>
    private static readonly (string, long, int)[] PerftPositions = [
        (Utils.startpos, 119060324, 6),
        ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 193690690, 5),
        ("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 11030083, 6),
        ("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 706045033, 6),
        ("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 89941194, 5),
    ];


    public static unsafe void RunPerft()
    {
        var watch = new Stopwatch();
        watch.Reset();
        long totalNodes = 0;
        
        foreach (var entry in PerftPositions)
        {
            var fen   = entry.Item1;
            var nodes = entry.Item2;
            var depth = entry.Item3;

            Console.WriteLine(fen+"; depth "+depth);
            Pos p = new(fen);

            watch.Start();
            long res = Recurse(ref p, depth);
            watch.Stop();

            totalNodes += res;
            Console.WriteLine($"{res}/{nodes} - {res == nodes}");
        }
        watch.Stop();

        Console.WriteLine($"nps: {1000 * totalNodes / watch.ElapsedMilliseconds}");
    }



    public static void SplitPerft(Pos p, int depth)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p);
        Console.WriteLine("num pseudolegal moves: " + moveCount);

        for (int i = 0; i < moveCount; i++)
        {
            Move m = moves[i];
            if (!p.IsLegal(m))
            {
                //Console.WriteLine($"{m} - illegal");
                continue;
            }

            Pos next = p;
            next.MakeMove(m);

            long nodes = Recurse(ref next, depth - 1);
            Console.WriteLine($"{m} - {nodes}");
        }
    }
    
    public static void SplitPerft(string fen, int depth)
        => SplitPerft(new Pos(fen), depth);


    public static long Recurse(ref Pos p, int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p);

        long nodes = 0;
        for (int i = 0; i < moveCount; i++)
        {
            Move m = moves[i];
            if (!p.IsLegal(m))
                continue;

            Pos next = p;
            next.MakeMove(m);
            nodes += Recurse(ref next, depth - 1);
        }
        return nodes;
    }

}