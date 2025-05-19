using System.Diagnostics;


public unsafe struct RootPos
{
    private fixed ushort rootMoves[256];
    private fixed int moveScores[256];
    private fixed long moveNodes[256];

    public Pos p;
    public int moveCount;

    public fixed long movesPlayed[100];
    public int ply;


    public void MakeMove(string movestr)
    {
        Move m = new(movestr, ref p);
        Debug.Assert(m.NotNull);

        // make move on board representation
        p.MakeMove(m);

        // save move in game hostory (necessary for 3-fold detection later)
        movesPlayed[ply % 100] = m.value;
        ply++;
    }

    public void SetNewFen(string fen)
    {
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
        }
    }

}