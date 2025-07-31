using System.Diagnostics;


public class RootPos
{
    public Dictionary<Move, RootMove> RootMoves;

    public Pos p;
    public int moveCount;


    public RootPos() => RootMoves = new();

    public RootPos(string fen)
    {
        RootMoves = new();
        SetNewFen(fen);
    }

    public void MakeMove(string movestr, SearchThread thread)
    {
        Move m = new(movestr, ref p);
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

    public int GetAvgSqrScore()
    {
        int avg = 0;
        foreach (RootMove rm in RootMoves.Values)
        {
            avg += rm.Score * rm.Score;
        }
        return avg / moveCount;
    }

    public void SetNewFen(string fen)
    {
        Clear();
        p = new(fen);
    }

    public void InitRootMoves()
    {
        Span<Move> moves = stackalloc Move[256];
        moveCount = MoveGen.GenerateLegaMoves(ref moves, ref p);
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
    }

    public void CopyFrom(RootPos Parent)
    {
        RootMoves = new Dictionary<Move, RootMove>(Parent.RootMoves);
        p = Parent.p;
        moveCount = Parent.moveCount;
    }

}