using System.Diagnostics;

public static class Perft
{

    /// <summary>
    /// (fen, nodes, depth)
    /// </summary>
    private static readonly (string, long, int)[] PerftPositions = [
        // standard chess
        (Utils.startpos, 119060324, 6),
        ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 193690690, 5),
        ("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 11030083, 6),
        ("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 706045033, 6),
        ("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 89941194, 5),

        // (d)frc
        ("1rqbkrbn/1ppppp1p/1n6/p1N3p1/8/2P4P/PP1PPPP1/1RQBKRBN w FBfb - 0 9", 191762235, 6),
        ("rbbqn1kr/pp2p1pp/6n1/2pp1p2/2P4P/P7/BP1PPPP1/R1BQNNKR w HAha - 0 9", 26302461, 5),
        ("rqbbknr1/1ppp2pp/p5n1/4pp2/P7/1PP5/1Q1PPPPP/R1BBKNRN w GAga - 0 9", 308553169, 6),
    ];


    public static unsafe void RunPerft()
    {
        var watch = new Stopwatch();
        watch.Reset();
        long totalNodes = 0;

        var thread = ThreadPool.MainThread;
        
        foreach (var (fen, nodes, depth) in PerftPositions)
        {
            Console.WriteLine($"{fen}; depth {depth}");
            Pos p = new(fen);

            Span<Node> NodeSpan = stackalloc Node[256];
            fixed (Node* NodePtr = NodeSpan) {
                thread.nodeStack = NodePtr;    

                watch.Start();
                long res = Recurse(thread, ref p, depth);
                watch.Stop();

                totalNodes += res;
                Console.WriteLine($"{res}/{nodes} - {res == nodes}");
            }
        }
        watch.Stop();

        Console.WriteLine($"nps: {1000 * totalNodes / watch.ElapsedMilliseconds}");
    }



    public static unsafe void SplitPerft(Pos p, int depth)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p, false);
        Console.WriteLine("num pseudolegal moves: " + moveCount);

        var thread = ThreadPool.MainThread;

        Span<Node> NodeSpan = stackalloc Node[256];
        fixed (Node* NodePtr = NodeSpan)
        {
            thread.nodeStack = NodePtr;

            for (int i = 0; i < moveCount; i++)
            {
                Move m = moves[i];
                if (!p.IsLegal(m))
                {
                    //Console.WriteLine($"{m} - illegal");
                    continue;
                }

                Pos next = p;
                next.MakeMove(m, ThreadPool.MainThread);

                long nodes = Recurse(thread, ref next, depth - 1);
                Console.WriteLine($"{m} - {nodes}");
            }
        }
    }
    
    public static void SplitPerft(string fen, int depth)
        => SplitPerft(new Pos(fen), depth);

    public static void SplitPerft(int idx, int depth)
        => SplitPerft(new Pos(PerftPositions[idx].Item1), depth);


    public static long Recurse(SearchThread thread, ref Pos p, int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p, false);

        long nodes = 0;
        for (int i = 0; i < moveCount; i++)
        {
            Move m = moves[i];
            if (!p.IsLegal(m))
                continue;

            Pos next = p;
            next.MakeMove(m, ThreadPool.MainThread);
            nodes += Recurse(thread, ref next, depth - 1);
            thread.ply--;
        }
        return nodes;
    }

}