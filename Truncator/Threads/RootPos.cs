using System.Diagnostics;


public class RootPos : IDisposable
{
    private SearchThread thread;
    public Dictionary<Move, RootMove> RootMoves;
    public PV[] PVs;

    public Pos p;
    public int moveCount;

    
    public RootPos(SearchThread thread)
    {
        this.thread = thread;
        RootMoves = [];
        PVs = new PV[thread.MultiPvCount];

        for (int i = 0; i < PVs.Length; i++)
        {
            PVs[i] = new();
        }
    }

    public RootPos(SearchThread thread, string fen)
    {
        this.thread = thread;
        RootMoves = new();

        PVs = new PV[thread.MultiPvCount];

        for (int i = 0; i < PVs.Length; i++)
        {
            PVs[i] = new();
        }

        SetNewFen(fen);
    }


    public void MakeMove(string movestr) => MakeMove(new Move(thread, ref p, movestr));

    public void MakeMove(Move m)
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

    public void SetNewFen(string fen)
    {
        Clear();
        p = new(thread, fen);
    }

    public void InitRootMoves()
    {
        RootMoves.Clear();

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

        foreach (var pv in PVs)
        {
            pv.Clear();
        }
    }

    public void CopyFrom(RootPos Parent)
    {
        RootMoves = new Dictionary<Move, RootMove>(Parent.RootMoves);
        p = Parent.p;
        moveCount = Parent.moveCount;
    }

    public void Dispose()
    {
        foreach (var pv in PVs)
        {
            pv.Dispose();
        }
    }

}