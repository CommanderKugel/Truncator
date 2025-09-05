
using System.Diagnostics;

public ref struct MovePicker<Type> where Type : PickerType
{

    private Span<Move> moves;
    private Span<int> scores;

    private SearchThread thread;
    private Move ttMove;

    public Stage stage;
    readonly bool evasions;
    readonly int SEEMargin;

    private int captureCount;
    private int captureIndex;
    private int quietCount;
    private int quietIndex;


    public MovePicker(SearchThread thread_, Move ttMove_, ref Span<Move> moves_, ref Span<int> scores_, bool inCheck, int SEEMargin_)
    {
        moves = moves_;
        scores = scores_;

        thread = thread_;
        ttMove = ttMove_;

        stage = Stage.TTMove;
        evasions = inCheck;
        SEEMargin = SEEMargin_;
    }

    private unsafe void ScoreMoves<Type>(int start, int end, SearchThread thread, ref Pos p)
        where Type : GenType
    {
        Move killer = thread.nodeStack[thread.ply].KillerMove;
        var ContHist1ply = thread.nodeStack[thread.ply - 1].ContHist;
        var ContHist2ply = thread.nodeStack[thread.ply - 2].ContHist;

        for (int i = start; i < end; i++)
        {
            ref Move m = ref moves[i];

            // captures
            if (typeof(Type) == typeof(Captures))
            {
                PieceType pt = p.PieceTypeOn(m.from);
                PieceType vict = p.GetCapturedPieceType(m);

                scores[i] = (SEE.SEE_threshold(m, ref p, SEEMargin) ? 1_000_000 : -1_000_000)
                    + thread.history.CaptHist[p.Us, pt, vict, m.to]
                    + SEE.SEEMaterial[(int)vict] * 10;
            }

            // quiets
            else if (m == killer)
            {
                scores[i] = 900_000;
            }
            else // quiet
            {
                PieceType pt = p.PieceTypeOn(m.from);

                scores[i] = thread.history.Butterfly[p.Threats, p.Us, m];
                scores[i] += (*ContHist1ply)[p.Us, pt, m.to];
                scores[i] += (*ContHist2ply)[p.Us, pt, m.to];
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
                        stage = Stage.GenerateCaptures;
                        if (p.IsPseudoLegal(thread, ttMove) && (typeof(Type) == typeof(PVSPicker) || p.IsCapture(ttMove)))
                        {
                            return ttMove;
                        }
                        continue;
                    }

                case Stage.GenerateCaptures:
                    {
                        stage = Stage.GoodCaptures;
                        captureCount = captureIndex = 0;

                        if (!evasions) MoveGen.GeneratePseudolegalMoves<Captures>(thread, ref moves, ref captureCount, ref p);
                        else MoveGen.GeneratePseudolegalMoves<CaptureEvasions>(thread, ref moves, ref captureCount, ref p);

                        ScoreMoves<Captures>(0, captureCount, thread, ref p);
                        continue;
                    }

                case Stage.GoodCaptures:
                    {

                        // find the next best scoring move
                        int idx = GetNextIndex(captureIndex, captureCount);
                        Move m = moves[idx];
                        int score = scores[idx];

                        // if move is bad capture or does not exist, try quiets
                        // skip quiet moves in qsearch, except when evading check
                        if (m.IsNull || score < 0 || captureIndex >= captureCount)
                        {
                            stage = Stage.GenerateQuiets;
                            continue;
                        }

                        // incrementally sort the move
                        (moves[captureIndex], moves[idx]) = (moves[idx], moves[captureIndex]);
                        (scores[captureIndex], scores[idx]) = (scores[idx], scores[captureIndex]);
                        captureIndex++;

                        // skip ttMove, we already played that
                        if (m == ttMove && !m.IsNull)
                        {
                            continue;
                        }

                        return m;
                    }

                case Stage.GenerateQuiets:
                    {
                        if (typeof(Type) == typeof(QSPicker) && !evasions)
                        {
                            stage = Stage.Done;
                            return Move.NullMove;
                        }

                        stage = Stage.Quiets;
                        quietCount = quietIndex = captureCount;

                        if (!evasions) MoveGen.GeneratePseudolegalMoves<Quiets>(thread, ref moves, ref quietCount, ref p);
                        else MoveGen.GeneratePseudolegalMoves<QuietEvasions>(thread, ref moves, ref quietCount, ref p);

                        ScoreMoves<Quiets>(quietIndex, quietCount, thread, ref p);
                        continue;
                    }

                case Stage.Quiets:
                    {
                        // find the next best scoring move
                        int idx = GetNextIndex(quietIndex, quietCount);
                        Move m = moves[idx];
                        int score = scores[idx];

                        Debug.Assert(m.IsNull || !p.IsCapture(m));

                        // if move is bad capture or does not exist, try quiets
                        if (m.IsNull)
                        {
                            stage = Stage.BadCaptures;
                            continue;
                        }

                        // incrementally sort the move
                        (moves[quietIndex], moves[idx]) = (moves[idx], moves[quietIndex]);
                        (scores[quietIndex], scores[idx]) = (scores[idx], scores[quietIndex]);
                        quietIndex++;

                        // skip ttMove, we already played that
                        if (m == ttMove && !m.IsNull)
                        {
                            continue;
                        }

                        return m;
                    }

                case Stage.BadCaptures:
                    {
                        if (captureIndex >= captureCount)
                        {
                            stage = Stage.Done;
                            continue;
                        }

                        // find the next best scoring move
                        int idx = GetNextIndex(captureIndex, captureCount);
                        Move m = moves[idx];

                        Debug.Assert(p.IsCapture(m));

                        // incrementally sort the move
                        // dont worry about next stages, this is the last
                        (moves[captureIndex], moves[idx]) = (moves[idx], moves[captureIndex]);
                        (scores[captureIndex], scores[idx]) = (scores[idx], scores[captureIndex]);
                        captureIndex++;

                        // skip ttMove, we already played that
                        if (m == ttMove && !m.IsNull)
                        {
                            continue;
                        }
                        return m;
                    }

                case Stage.Done:
                default:
                    {
                        return Move.NullMove;
                    }
            }
        }        
    }

    /// <summary>
    /// Find the index of the best scoring Move
    /// </summary>
    private int GetNextIndex(int start, int end)
    {
        int idx = start;
        int score = scores[idx];

        for (int i = start + 1; i < end; i++)
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