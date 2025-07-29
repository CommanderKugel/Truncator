using System.Diagnostics;
using System.Runtime;


public unsafe struct RootPos
{
    public fixed ushort rootMoves[256];
    public fixed int moveScore[256];
    public fixed long moveNodes[256];

    public Pos p;
    public int moveCount;

    public int AvgScore;


    public RootPos() { }

    public RootPos(string fen)
    {
        SetNewFen(fen);
    }

    /// <summary>
    /// Makes the move on p
    /// </summary>
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

    /// <summary>
    /// Computes the average root-moves search score
    /// </summary>
    public int GetAvgScore()
    {
        int avg = 0;
        for (int i = 0; i < moveCount; i++)
        {
            avg += moveScore[i];
        }
        return avg / moveCount;
    }

    /// <summary>
    /// returns the root-moves index in the roomoves-array
    /// returns null of m not in legal rootmoves
    /// </summary>
    public int? IndexOfMove(Move m)
    {
        for (int i = 0; i < moveCount; i++)
        {
            if (rootMoves[i] == m.value)
            {
                return i;
            }
        }
        return null;
    }

    /// <summary>
    /// Update a rootmoves score and nodecount
    /// </summary>
    public void ReportBackMove(Move m, int score, long nodes, int depth)
    {
        Debug.Assert(m.NotNull, "cant report on null moves for root pos!");
        Debug.Assert(nodes > 0, "did you even search?");
        Debug.Assert(depth >= 1, "depth needs to '1' or greater!");

        int idx = IndexOfMove(m) ?? 256;
        Debug.Assert(idx <= moveCount, "no move found to report back to!");

        moveScore[idx] = score;
        moveNodes[idx] = nodes;
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
            rootMoves[i] = moves[i].value;
            moveScore[i] = 0;
            moveNodes[i] = 0;
        }
    }

    public void Clear()
    {
        p = new();
        moveCount = 0;
        AvgScore = 0;

        for (int i = 0; i < 256; i++)
        {
            rootMoves[i] = 0;
            moveScore[i] = 0;
            moveNodes[i] = 0;
        }
    }

}