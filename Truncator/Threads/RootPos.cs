using System.Diagnostics;


public unsafe struct RootPos : IDisposable
{
    public fixed ushort rootMoves[256];
    public fixed int moveScores[256];
    public fixed long moveNodes[256];
    public fixed int completedDepth[256];

    public Pos p;
    public int moveCount;

    public RepetitionTable repTable;
    public fixed long movesPlayed[100];
    public int ply;

    /// <summary>
    /// lock this object when changing values, to avoid racing conditions between threads
    /// </summary>
    public object lockobject;

    public RootPos()
    {
        repTable = new RepetitionTable();
        lockobject = new object();
    }

    public RootPos(string fen)
    {
        SetNewFen(fen);
        lockobject = new object();
    }

    public void MakeMove(string movestr)
    {
        Move m = new(movestr, ref p);
        Debug.Assert(m.NotNull);

        // make move on board representation
        p.MakeMove(m, ThreadPool.MainThread);

        // save move in game hostory (necessary for 3-fold detection later)
        movesPlayed[ply % 100] = m.value;
        ply++;
        
        if (p.FiftyMoveRule == 0)
        {
            repTable.Clear();
        }
        else
        {
            repTable.Push(p.ZobristKey);
        }
    }

    public Move GetBestMove()
    {
        return new(rootMoves[GetBestIndex()]);
    }

    /// <summary>
    /// Falsely returns indices that are just upper bounds that match the bestscore! needs fixing!
    /// </summary>
    /// <returns></returns>
    public int GetBestIndex()
    {
        lock (lockobject)
        {
            int bestIdx = 0;

            for (int i = 0; i < moveCount; i++)
            {
                if (moveScores[i] > moveScores[bestIdx] &&
                    completedDepth[i] >= completedDepth[bestIdx])
                {
                    bestIdx = i;
                }
            }

            return bestIdx;
        }
    }

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

    public unsafe void Print()
    {
        Console.WriteLine($"moveCount: {moveCount}");
        for (int i = 0; i < moveCount; i++)
        {
            Console.WriteLine($"move {new Move(rootMoves[i])} score {moveScores[i]} nodes {moveNodes[i]} depth {completedDepth[i]}");
        }
    }

    public void ReportBackMove(Move m, int score, long nodes, int depth)
    {
        Debug.Assert(m.NotNull, "cant report on null moves for root pos!");
        Debug.Assert(Math.Abs(score) != Search.SCORE_TIMEOUT, "tiemout scores are always wrong!");
        Debug.Assert(nodes > 0, "did you even search?");
        Debug.Assert(depth >= 1, "depth needs to '1' or greater!");

        lock (lockobject)
        {
            int idx = 0;
            for (int i = 0; i < moveCount; i++)
            {
                if (rootMoves[i] == m.value)
                {
                    idx = i;
                    break;
                }
            }

            Debug.Assert(idx < moveCount, "no move found to report back to!");

            moveScores[idx] = score;
            moveNodes[idx] = nodes;
            completedDepth[idx] = depth;
        }
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
            moveScores[i] = 0;
            moveNodes[i] = 0;
        }
    }

    public void Clear()
    {
        p = new();
        moveCount = 0;

        for (int i = 0; i < 256; i++)
        {
            rootMoves[i] = 0;
            moveScores[i] = 0;
            moveNodes[i] = 0;
            completedDepth[i] = 0;
        }
    }

    public void Dispose()
    {
        repTable.Dispose();
    }

}