public static partial class Search
{

    public static int Negamax<Type>(SearchThread thread, Pos p, int alpha, int beta, int depth)
        where Type : NodeType
    {

        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV   = typeof(Type) == typeof(RootNode) || typeof(Type) == typeof(PVNode);
        bool nonPV  = typeof(Type) == typeof(NonPVNode);


        // #1 Draw-detection
        // #2 Mate-Distance pruning
        // #3 ply-overflow

        // #4 drop into Quiescense Search
        if (depth <= 0)
        {
            return SimpleEval.Evaluate(ref p);
        }

        // #5 TT cutoffs
        // #6 TB cutoffs(?)

        // if (inCheck || isPV) go_to MoveLoop;

        // #7 Razoring
        // #8 RFP
        // #9 NMP (+NMP confirmation)
        // #10 Probcut

        // #11 check-extensions
        // #12 IIR
        // #13 IID


        // #14 MoveGen 
        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        Span<bool> see = stackalloc bool[256];
        MovePicker picker = new MovePicker(ref p, Move.NullMove, ref moves, ref scores, ref see);

        // initialize variables
        Move m,
            bestMove = Move.NullMove;
        int bestScore = -SCORE_MATE,
            movesPlayed = 0,
            movesTried = 0;

        // enter main move-loop
        while ((m = picker.Next()).NotNull)
        {

            movesTried++;
            long startNodes = thread.nodeCount;

            // isCapture or isTactical
            // givesCheck

            // #15 LMP
            // #16 FP
            // #17 History Pruning
            // #18 SEE Pruning

            // #19 make the move (or skip if illegal)
            if (!p.IsLegal(m))
            {
                continue;
            }

            // #19 SE
            // #20 Multi-Cut
            // #21 double-/triple-/negative-extensions

            Pos next = p;
            next.MakeMove(m, thread);
            movesPlayed++;

            // #22 LMR
            // #23 PVS
            // #24 shallower/deeper

            int score = -Negamax<PVNode>(thread, next, -beta, -alpha, depth - 1);

            // #25 time-out check
            //     do not use SCORE_TIMEOUT for anything, as it is invalid
            if (!isRoot && (!thread.doSearch || thread.IsMainThread && TimeManager.IsHardTimeout()) ||
                 score == SCORE_TIMEOUT || -score == SCORE_TIMEOUT)
            {
                return SCORE_TIMEOUT;
            }

            // report to root/UCI
            if (isRoot)
            {
                UCI.rootPos.ReportBackMove(m, score, thread.nodeCount - startNodes, depth);
            }


            if (score > bestScore)
            {
                bestScore = score;

                // #26 fail-low check
                if (score > alpha)
                {
                    alpha = score;
                    bestMove = m;

                    // #27 fail-high check
                    if (score >= beta)
                    {
                        // #28 update move histories
                        // #29 update corrhist
                        // #30 update killer moves
                        break;
                    }
                }
            }
        } // move-loop

        // #31 (stale-)mate detection

        // #32 write to tt

        return bestScore;

    }

}