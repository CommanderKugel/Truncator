
public ref struct MovePicker
{

    private Span<Move> moves;
    private Span<int> scores;

    private Move ttMove;

    private int moveCount;
    private int moveIdx;



    public MovePicker(ref Pos p, Move ttMove_, ref Span<Move> moves_, ref Span<int> scores_, bool inQS)
    {
        this.moves = moves_;
        this.scores = scores_;
        this.ttMove = ttMove_;

        this.moveIdx = 0;
        this.moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p, inQS);
        this.ScoreMoves(ref p);
    }

    private void ScoreMoves(ref Pos p)
    {
        for (int i = 0; i < moveCount; i++)
        {
            ref Move m = ref moves[i];

            if (m == ttMove)
            {
                scores[i] = 2_000_000;
            }
            else if (p.IsCapture(m))
            {
                scores[i] = (int)p.GetCapturedPieceType(m) * 100 - (int)p.PieceTypeOn(m.from);
            }
        }
    }

    public Move Next()
    {   
        // find the next best scoring move
        int idx = GetNextIndex();

        // incrementally sort the best scoring move to the front
        (moves[moveIdx], moves[idx]) = (moves[idx], moves[moveIdx]);

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
        for (int i = 0; i < moveCount; i++)
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