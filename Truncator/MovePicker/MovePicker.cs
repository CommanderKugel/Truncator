
public ref struct MovePicker
{

    private Span<Move> moves;
    private Span<int> scores;

    private Move ttMove;

    private int moveCount;
    private int moveIdx;

    public readonly int CurrentScore => scores[moveIdx - 1];



    public MovePicker(SearchThread thread, ref Pos p, Move ttMove_, ref Span<Move> moves_, ref Span<int> scores_, bool inQS)
    {
        this.moves = moves_;
        this.scores = scores_;
        this.ttMove = ttMove_;

        this.moveIdx = 0;
        this.moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p, inQS);
        this.ScoreMoves(thread, ref p);
    }

    private unsafe void ScoreMoves(SearchThread thread, ref Pos p)
    {
        Move killer = thread.nodeStack[thread.ply].KillerMove;

        var ContHist1ply = thread.nodeStack[thread.ply - 1].ContHist;
        var ContHist2ply = thread.nodeStack[thread.ply - 2].ContHist;
        var PawnHist = thread.history.PawnHist[p.PawnKey];

        for (int i = 0; i < moveCount; i++)
        {
            ref Move m = ref moves[i];

            if (m == ttMove)
            {
                scores[i] = 2_000_000;
            }
            else if (p.IsCapture(m))
            {
                scores[i] = (SEE.SEE_threshold(m, ref p, 0) ? 1_000_000 : -1_000_000)
                          + (int)p.GetCapturedPieceType(m) * 100
                          - (int)p.PieceTypeOn(m.from);
            }
            else if (m == killer)
            {
                scores[i] = HistVal.HIST_VAL_MAX + 1;
            }
            else // quiet
            {
                PieceType pt = p.PieceTypeOn(m.from);

                scores[i] = thread.history.Butterfly[p.Us, m];
                scores[i] += (*ContHist1ply)[p.Us, pt, m.to];
                scores[i] += (*ContHist2ply)[p.Them, pt, m.to];
                scores[i] += (*PawnHist)[p.Us, pt, m.to];
            }
        }
    }

    public Move Next()
    {   
        // find the next best scoring move
        int idx = GetNextIndex();

        // incrementally sort the best scoring move to the front
        (moves[moveIdx], moves[idx]) = (moves[idx], moves[moveIdx]);
        (scores[moveIdx], scores[idx]) = (scores[idx], scores[moveIdx]);

        // return the best scoring move
        return moves[moveIdx++];
    }

    /// <summary>
    /// Find the index of the best scoring Move
    /// </summary>
    private int GetNextIndex()
    {
        int idx = moveIdx;
        int score = scores[moveIdx];

        for (int i = moveIdx + 1; i < moveCount; i++)
        {
            if (score < scores[i])
            {
                idx = i;
                score = scores[i];
            }
        }
        return idx;
    }
    
}