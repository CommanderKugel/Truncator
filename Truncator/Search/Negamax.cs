
using System.Diagnostics;
using System.Security.Cryptography;

public static partial class Search
{

    public static unsafe int Negamax<Type>(SearchThread thread, Pos p, int alpha, int beta, int depth, Node* ns)
        where Type : NodeType
    {
        Debug.Assert(thread.ply < 256);
        Debug.Assert(ns == &thread.nodeStack[thread.ply]);

        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV = isRoot || typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);

        // overwrite previous pv line
        if (!isRoot && isPV)
        {
            thread.NewPVLine();
        }

        // Draw Detection
        // - twofold repetition
        // - insufficient mating material
        // - fiftx move rule
        if (!isRoot)
        {
            if (p.IsDraw(thread))
            {
                return SCORE_DRAW;
            }
        }

        // if leaf-node: drop into QSearch
        // static evaluation should only be returned from quiet positions.
        // because material is by far the most important evaluation term
        // by playing out all good captures, qsearch eliminates possible material-swings
        if (depth <= 0)
        {
            return QSearch<Type>(thread, p, alpha, beta);
        }

        // probe the transposition table for already visited positions
        TTEntry entry = thread.tt.Probe(p.ZobristKey);
        bool ttHit = entry.Key == p.ZobristKey;
        Move ttMove = ttHit ? new(entry.MoveValue) : Move.NullMove;

        // return the tt-score if the entry is any good
        if (nonPV && ttHit && entry.Depth >= depth && (
            entry.Flag == LOWER_BOUND && entry.Score >= beta ||
            entry.Flag == UPPER_BOUND && entry.Score <= alpha ||
            entry.Flag == EXACT_BOUND
        ))
        {
            return entry.Score;
        }

        // clear childrens killer-move
        Debug.Assert(thread.ply < 256 - 1);
        (ns + 1)->KillerMove = Move.NullMove;

        
        bool inCheck = p.GetCheckers() != 0;

        // static evaluaton
        // although this is a noisy position and we have to distrust the static
        // evaluation of the current node to an extend, we can draw some conclusion from it.
        int staticEval = inCheck ? 0 : Pesto.Evaluate(ref p);

        if (inCheck)
        {
            goto skip_whole_node_pruning;
        }


        // reverse futility pruning (RFP)
        if (nonPV &&
            depth <= 5 &&
            staticEval - 75 * depth >= beta)
        {
            return staticEval;
        }

        // razoring
        if (nonPV &&
            !IsTerminal(alpha) &&
            depth <= 4 &&
            staticEval + 300 * depth <= alpha)
        {
            int RazoringScore = QSearch<NonPVNode>(thread, p, alpha, beta);

            if (RazoringScore <= alpha)
            {
                return RazoringScore;
            }
        }

        // null move pruning
        if (nonPV &&
            (ns - 1)->move.NotNull &&
            staticEval >= beta)
        {
            Pos PosAfterNull = p;
            PosAfterNull.MakeNullMove(thread);
            thread.repTable.Push(PosAfterNull.ZobristKey);

            int R = 3;
            int ScoreAfterNull = -Negamax<NonPVNode>(thread, PosAfterNull, -beta, -alpha, depth - R, ns + 1);

            thread.UndoMove();

            if (ScoreAfterNull >= beta)
            {
                return ScoreAfterNull;
            }
        }


        skip_whole_node_pruning:


        // check extensions
        if (inCheck)
        {
            depth++;
        }

        // movegeneration, scoring and ordering is outsourced to the move-picker
        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(thread, ref p, ttMove, ref moves, ref scores, false);


        int bestscore = -SCORE_MATE;
        int score = -SCORE_MATE;
        int flag = NONE_BOUND;

        int movesPlayed = 0;
        int quitesCount = 0;
        Span<Move> quietMoves = stackalloc Move[128];

        Move bestmove = Move.NullMove;

        // main move loop
        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);
            long startnodes = thread.nodeCount;

            bool isCapture = p.IsCapture(m);
            bool isNoisy = isCapture || m.IsPromotion; // ToDo: GivesCheck()

            bool nonMatinglineExists = !IsTerminal(bestscore);

            // move loop pruning
            if (!isNoisy &&
                nonMatinglineExists)
            {

                // futility pruning 
                if (nonPV &&
                    !inCheck &&
                    depth <= 4 &&
                    staticEval + depth * 150 <= alpha)
                {
                    continue;
                }

                // ToDo: late move pruning

                // ToDo: histoy pruning

            }

            // skip illegal moves for obvious reasons
            if (!p.IsLegal(m))
            {
                continue;
            }

            // make the move and update the boardstate
            Pos next = p;
            next.MakeMove(m, thread);
            thread.repTable.Push(next.ZobristKey);

            movesPlayed++;
            if (!isCapture) quietMoves[quitesCount++] = m;

            if (movesPlayed > 1 && depth >= 2 && !isCapture)
            {
                // late-move-reductions (LMR)
                // assuming our move-ordering is good, the first played move should be the best
                // all moves after that are expected to be worse. we will validate this thesis by 
                // searching them at a cheaper shallower depth. if a move seems to beat the current best move,
                // we need to re-search that move at full depth to confirm its the better move.
                int R = Math.Max(Log_[Math.Min(movesPlayed, 63)] / 2, 2);

                // zero-window-search (ZWS)
                // as part of the principal-variation-search, we assume that all lines that are not the pv
                // are worse than the pv. therefore, we search the nonPv lines with a zero-window around alpha
                // to cause a lot more curoffs. the returned value will never be an exact score, but an upper-
                // or lower-bound. if a move unexpectedly beats the pv, we need to re-search it at full depth,
                // to confirm it is really better than the pv and obtain its exact value.
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - R, ns + 1);

                // re-search if LMR seems to beat the current best move
                if (score > alpha && R > 1)
                {
                    score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - 1, ns + 1);
                }
            }

            // ZWS for moves that are not reduced by LMR
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - 1, ns + 1);
            }

            // full-window-search
            // either the pv-line is searched fully at first, or a high-failing ZWS search needs to be confirmed.
            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax<PVNode>(thread, next, -beta, -alpha, depth - 1, ns + 1);
            }

            thread.UndoMove();

            // save move-data for e.g. soft-timeouts and other shenanigans
            if (isRoot)
            {
                thread.rootPos.ReportBackMove(m, score, thread.nodeCount - startnodes, depth);
            }

            if (score > bestscore)
            {
                // beating the best score does not mean we beat alpha
                // all scores below alpha are upper bounds and we do not know what score it really is
                // thus we dont know for sure what the best move is
                bestscore = score;

                if (isPV)
                {
                    thread.PushToPV(m);
                }

                if (isRoot)
                {
                    thread.pv_[depth] = bestscore;
                }

                if (score > alpha)
                {
                    // now we have an exact score, so save relevant data
                    alpha = score;
                    bestmove = m;
                    flag = EXACT_BOUND;

                    if (score >= beta)
                    {
                        // fail high
                        // the opponent can already force a better line and will not allow us to
                        // play the current one and we dont need to search further down this branch
                        flag = LOWER_BOUND;

                        if (!isCapture)
                        {
                            // update history
                            int HistDelta = depth * depth;

                            for (int i = 0; i < quitesCount; i++)
                            {
                                ref Move mv = ref quietMoves[i];
                                Debug.Assert(mv.NotNull);

                                short delta = (short)(mv == m ? HistDelta : -HistDelta);
                                thread.history.Butterfly.Update(delta, p.Us, quietMoves[i]);
                            }

                            // update killer-move
                            ns->KillerMove = m;
                        }

                        break;

                    } // beta beaten
                } // alpha beaten
            } // best-score update

            // check if we have time left
            // do this in the move-loop & after updaing the pv
            // otherwise we could end up without a best move
            if (!thread.doSearch ||
                 thread.IsMainThread && TimeManager.IsHardTimeout(thread))
            {
                return SCORE_TIMEOUT;
            }
            
        } // move-loop

        // stale-mate detection
        // and dont save to the tt if this is a terminal node
        if (movesPlayed == 0)
        {
            return p.GetCheckers() == 0 ? SCORE_DRAW : -SCORE_MATE + thread.ply;
        }
        
        // dont save matin-scores to the tt, it cant handle them at the moment
        if (!IsTerminal(bestscore))
        {
            thread.tt.Write(
                p.ZobristKey,
                bestscore,
                bestmove,
                depth,
                flag,
                isPV,
                thread
            );
        }

        return bestscore;
    }
}
