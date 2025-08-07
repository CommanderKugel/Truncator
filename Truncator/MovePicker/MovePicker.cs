
public ref struct MovePicker
{

    private Span<Move> moves;
    private Span<int> scores;

    private SearchThread thread;
    private Move ttMove;

    private Stage stage;
    private bool inQS;
    private int moveCount;
    private int moveIdx;

    public readonly int CurrentScore => scores[moveIdx - 1];



    public MovePicker(SearchThread thread_, Move ttMove_, ref Span<Move> moves_, ref Span<int> scores_, bool inQS_)
    {
        this.moves = moves_;
        this.scores = scores_;
        this.ttMove = ttMove_;
        this.thread = thread_;
        this.inQS = inQS_;

        this.stage = Stage.TTMove;
        this.moveIdx = 0;
        this.moveCount = 0;
    }

    private unsafe void ScoreMoves(SearchThread thread, ref Pos p)
    {
        Move killer = thread.nodeStack[thread.ply].KillerMove;

        var ContHist1ply = thread.nodeStack[thread.ply - 1].ContHist;
        var ContHist2ply = thread.nodeStack[thread.ply - 2].ContHist;

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
                          + (int)p.GetCapturedPieceType(m) * HistVal.HIST_VAL_MAX
                          + thread.history.CaptHist[p.Us, p.PieceTypeOn(m.from), p.GetCapturedPieceType(m), m.to];
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
            }
        }
    }

    public Move Next(ref Pos p)
    {
        while (true)
        {
            switch (stage)
            {
                case Stage.TTMove:
                {
                    stage = Stage.Generate;

                    if (p.IsPseudoLegal(ttMove)
                        && (!inQS || p.IsCapture(ttMove)))
                    {
                        return ttMove;
                    }

                    continue;
                }

                case Stage.Generate:
                {
                    stage = Stage.Moves;

                    this.moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p, inQS);
                    this.ScoreMoves(thread, ref p);

                    continue;
                }

                case Stage.Moves:
                {
                
                    // find the next best scoring move
                    // incrementally sort the best scoring move to the front
                    // ~incremental insertion sort
                    // although it is an O(n^2) algorithm, its faster when if you dont have to 
                    // sort all moves in movepicking, than a O(n log n) algorithm
                    
                    int idx = GetNextIndex();
                    (moves[moveIdx], moves[idx]) = (moves[idx], moves[moveIdx]);
                    (scores[moveIdx], scores[idx]) = (scores[idx], scores[moveIdx]);
                    Move m = moves[moveIdx++];
                    
                    // skip ttMove, we already played that

                    if (m == ttMove && !m.IsNull)
                    {
                        continue;
                    }

                    return m;
                }
            }
        }        
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