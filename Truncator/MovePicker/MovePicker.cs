using System.Security.Cryptography.X509Certificates;

public ref struct MovePicker
{

    private Span<Move> moves;
    private Span<int> scores;
    /// <summary>
    /// currently unused
    /// </summary>
    private Span<bool> hasPassedSEE;

    private Move ttMove;

    private int moveCount;
    private int moveIdx;



    public MovePicker(ref Pos p, Move ttMove_, ref Span<Move> moves_, ref Span<int> scores_, ref Span<bool> see_)
    {
        this.moves = moves_;
        this.scores = scores_;
        this.hasPassedSEE = see_;
        this.ttMove = ttMove_;

        this.moveIdx = 0;
        this.moveCount = MoveGen.GeneratePseudolegalMoves(ref moves, ref p);
        this.ScoreMoves();
    }

    private void ScoreMoves()
    {
        for (int i = 0; i < moveCount; i++)
        {
            ref Move m = ref moves[i];
            scores[i] = 0;
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