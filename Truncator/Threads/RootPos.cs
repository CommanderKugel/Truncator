using System.Diagnostics;


public class RootPos
{
    public Dictionary<Move, RootMove> RootMoves;

    public Pos p;
    public int moveCount;

    public Move bestMove;
    public int pvStability;


    public RootPos() => RootMoves = new();

    public RootPos(SearchThread thread, string fen)
    {
        RootMoves = new();
        SetNewFen(thread, fen);

        bestMove = Move.NullMove;
        pvStability = 0;
    }

    public void MakeMove(string movestr, SearchThread thread)
        => MakeMove(new Move(thread, ref p, movestr), thread);

    public void MakeMove(Move m, SearchThread thread)
    {
        Debug.Assert(m.NotNull);

        // make move on board representation
        p.MakeMove(m, ThreadPool.MainThread);
        thread.ply--;

        if (p.FiftyMoveRule == 0)
        {
            thread.repTable.Clear();
        }
        else
        {
            thread.repTable.Push(p.ZobristKey);
        }
    }

    public unsafe void Print()
    {
        Console.WriteLine($"moveCount: {moveCount}");
        foreach (RootMove rm in RootMoves.Values)
        {
            Console.WriteLine($"move {rm.Move} score {rm.Score} nodes {rm.Nodes}");
        }
    }

    public void ReportBackMove(Move m, int score, long nodes, int depth)
    {
        Debug.Assert(m.NotNull, "cant report on null moves for root pos!");
        Debug.Assert(nodes > 0, "did you even search?");
        Debug.Assert(depth >= 1, "depth needs to '1' or greater!");
        Debug.Assert(RootMoves.ContainsKey(m), "move not in rootmoves!");

        RootMoves[m] = new(m, score, nodes);
    }

    public void SetNewFen(SearchThread thread, string fen)
    {
        Clear();
        p = new(thread, fen);
    }

    public void InitRootMoves(SearchThread thread)
    {
        Span<Move> moves = stackalloc Move[256];
        moveCount = MoveGen.GenerateLegaMoves(thread, ref moves, ref p);
        Debug.Assert(moveCount < 256);

        for (int i = 0; i < moveCount && i < 256; i++)
        {
            RootMoves.Add(moves[i], new RootMove(moves[i], 0, 0));
        }
    }

    public void Clear()
    {
        p = new();
        moveCount = 0;
        RootMoves.Clear();

        bestMove = Move.NullMove;
        pvStability = 0;
    }

    public void CopyFrom(RootPos Parent)
    {
        RootMoves = new Dictionary<Move, RootMove>(Parent.RootMoves);
        p = Parent.p;
        moveCount = Parent.moveCount;

        bestMove = Parent.bestMove;
        pvStability = Parent.pvStability;
    }

}