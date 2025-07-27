
using System.Diagnostics;

public static partial class Search
{

    public static unsafe int Negamax<Type>(SearchThread thread, Pos p, int alpha, int beta, int depth, Node* ns, bool cutnode)
        where Type : NodeType
    {
        Debug.Assert(thread.ply < 256);
        Debug.Assert(ns == &thread.nodeStack[thread.ply]);

        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV = isRoot || typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);
        bool inSingularity = ns->ExcludedMove.NotNull;

        // overwrite previous pv line
        thread.NewPVLine();

        if (!isRoot)
        {
            // Draw Detection
            // - twofold repetition
            // - insufficient mating material
            // - fiftx move rule
            if (p.IsDraw(thread))
            {
                return SCORE_DRAW;
            }

            // ToDo: return eval at max-ply
            // ToDo: mate distance pruning (elo neutral & nice to have)
        }

        // if leaf-node: drop into QSearch
        // static evaluation should only be returned from quiet positions.
        // because material is by far the most important evaluation term
        // by playing out all good captures, qsearch eliminates possible material-swings
        if (depth <= 0 || thread.ply >= 128)
        {
            return QSearch<Type>(thread, p, alpha, beta, ns);
        }

        // probe the transposition table for already visited positions
        bool ttHit = thread.tt.Probe(p.ZobristKey, out TTEntry ttEntry);
        Move ttMove = new(ttEntry.MoveValue);

        // return the tt-score if the entry is any good
        if (nonPV && ttHit && ttEntry.Depth >= depth && !inSingularity && (
            ttEntry.Flag == LOWER_BOUND && ttEntry.Score >= beta ||
            ttEntry.Flag == UPPER_BOUND && ttEntry.Score <= alpha ||
            ttEntry.Flag == EXACT_BOUND))
        {
            return ttEntry.Score;
        }

        // clear childrens relevant node data
        (ns + 1)->KillerMove = Move.NullMove;
        (ns + 1)->CutoffCount = 0;
        if (isRoot)
        {
            ns->CutoffCount = 0;
        }

        
        bool inCheck = p.GetCheckers() != 0;

        // static evaluaton
        // although this is a noisy position and we have to distrust the static
        // evaluation of the current node to an extend, we can draw some conclusion from it.
        if (inCheck)
        {
            ns->StaticEval = ns->UncorrectedStaticEval = -SCORE_MATE;
        }
        else
        {
            ns->UncorrectedStaticEval = Pesto.Evaluate(ref p);
            thread.CorrHist.Correct(thread, ref p, ns);
        }


        bool improving = thread.ply > 1 && !inCheck &&
                        (ns - 2)->StaticEval != -SCORE_MATE && 
                        ns->StaticEval >= (ns - 2)->StaticEval;

        // sometimes whole-node-pruning can be skippedentirely
        if (inCheck || isPV || inSingularity)
        {
            goto skip_whole_node_pruning;
        }


        // reverse futility pruning (RFP)
        if (depth <= 5 &&
            ns->StaticEval - 75 * depth >= beta)
        {
            return ns->StaticEval;
        }

        // razoring
        if (!IsTerminal(alpha) &&
            depth <= 4 &&
            ns->StaticEval + 300 * depth <= alpha)
        {
            int RazoringScore = QSearch<NonPVNode>(thread, p, alpha, beta, ns);

            if (RazoringScore <= alpha)
            {
                return RazoringScore;
            }
        }

        // null move pruning
        if ((ns - 1)->move.NotNull &&
            ns->StaticEval >= beta)
        {
            Pos PosAfterNull = p;
            PosAfterNull.MakeNullMove(thread);
            thread.repTable.Push(PosAfterNull.ZobristKey);

            int R = 3 + depth / 6;
            int ScoreAfterNull = -Negamax<NonPVNode>(thread, PosAfterNull, -beta, -alpha, depth - R, ns + 1, !cutnode);

            thread.UndoMove();

            if (ScoreAfterNull >= beta)
            {
                return ScoreAfterNull;
            }
        }


        skip_whole_node_pruning:


        // check extensions
        if (inCheck && !inSingularity)
        {
            depth++;
        }

        // ToDo: Internal Iterative Reductions
        // if (depth >= 4 && isPV && !ttHit) depth--;

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

            // skip the first move in singular confirmation search
            if (m == ns->ExcludedMove)
            {
                continue;
            }

            long startnodes = thread.nodeCount;
            bool notLoosing = !IsLoss(bestscore);

            bool isCapture = p.IsCapture(m);
            bool isNoisy = isCapture || m.IsPromotion; // ToDo: GivesCheck()

            int ButterflyScore = isCapture ? 0 : thread.history.Butterfly[p.Us, m];

            // move loop pruning
            if (!isRoot &&
                !inSingularity &&
                !isNoisy &&
                notLoosing)
            {

                // futility pruning 
                if (nonPV &&
                    !inCheck &&
                    depth <= 4 &&
                    ns->StaticEval + depth * 150 <= alpha)
                {
                    continue;
                }

                // late move pruning
                if (depth <= 4 &&
                    movesPlayed >= 2 + depth * depth)
                {
                    continue;
                }

                // main-history pruning
                if (depth <= 5 &&
                    ButterflyScore < -(15 * depth + 9 * depth * depth))
                {
                    continue;
                }

                // ToDo: Continuation Pruning

            }

            // pvs SEE pruning
            // ToDo: margin -= histScore / 8
            if (!isRoot &&
                notLoosing &&
                !SEE.SEE_threshold(m, ref p, isCapture ? -150 * depth : -25 * depth * depth))
            {
                continue;
            }


            // singular extensions
            int extension = 0;

            if (m == ttMove &&
                !inSingularity &&
                !isRoot &&
                depth >= 8 &&
                ttEntry.Depth >= depth - 3 &&
                ttEntry.Flag > UPPER_BOUND &&
                thread.ply < thread.completedDepth * 2)
            {

                int singularBeta = Math.Max(-SCORE_MATE + 1, ttEntry.Score - depth * 2);
                int singularDepth = (depth - 1) / 2;

                ns->ExcludedMove = m;
                int singularScore = Negamax<NonPVNode>(thread, p, singularBeta - 1, singularBeta, singularDepth, ns, cutnode);
                ns->ExcludedMove = Move.NullMove;

                if (singularScore < singularBeta)
                {
                    if (!isPV && singularScore < singularBeta - 12)
                    {
                        extension = 2;
                    }
                    else
                    {
                        extension = 1;
                    }
                }
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

            if (movesPlayed > 1 && depth >= 2)
            {
                int R = 1;

                if (!isCapture)
                {
                    // late-move-reductions (LMR)
                    // assuming our move-ordering is good, the first played move should be the best
                    // all moves after that are expected to be worse. we will validate this thesis by 
                    // searching them at a cheaper shallower depth. if a move seems to beat the current best move,
                    // we need to re-search that move at full depth to confirm its the better move.
                    R = Math.Max(Log_[Math.Min(movesPlayed, 63)] * Log_[Math.Min(depth, 63)] / 4, 2);

                    // reduce more for bad history values, divisor = HIST_VAL_MAX / 3
                    R += -ButterflyScore / 341;

                    if (thread.ply > 1 && !improving) R++;

                    // ToDo: R += nonPV && !cutnode ? 2 : 1; // +1 if allnode
                    if ((ns + 1)->CutoffCount > 2) R++;

                    R = Math.Max(1, R);
                }

                else // isCapture
                {

                    // bad captures
                    if (picker.CurrentScore < 0) R++;

                }

                // zero-window-search (ZWS)
                // as part of the principal-variation-search, we assume that all lines that are not the pv
                // are worse than the pv. therefore, we search the nonPv lines with a zero-window around alpha
                // to cause a lot more curoffs. the returned value will never be an exact score, but an upper-
                // or lower-bound. if a move unexpectedly beats the pv, we need to re-search it at full depth,
                // to confirm it is really better than the pv and obtain its exact value.
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - R, ns + 1, true);

                // re-search if LMR seems to beat the current best move
                if (score > alpha && R > 1)
                {
                    score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - 1, ns + 1, !cutnode);
                }
            }

            // ZWS for moves that are not reduced by LMR
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth + extension - 1, ns + 1, !cutnode);
            }

            // full-window-search
            // either the pv-line is searched fully at first, or a high-failing ZWS search needs to be confirmed.
            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax<PVNode>(thread, next, -beta, -alpha, depth + extension - 1, ns + 1, false);
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
                flag = UPPER_BOUND;

                // lock pv-updates at root to avoid it being read for uci info printing and
                // written to by the searching thread at the same time. this is only relevant
                // at root nodes, because info printing only ever uses the full latest pv
                if (isRoot)
                {
                    lock (thread.pv_.lockObject)
                    {
                        thread.pv_[depth] = bestscore;
                        thread.PushToPV(m);
                    }
                }
                else if (isPV)
                {
                    thread.PushToPV(m);
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
                            // ToDo: increase Bonus for ttmoves -> (depth + 1) * depth
                            int HistDelta = depth * depth;
                            thread.history.UpdateQuietMoves(thread, ns, (short)HistDelta, (short)-HistDelta, ref p, ref quietMoves, quitesCount, m);

                            // update killer-move
                            ns->KillerMove = m;
                        }

                        ns->CutoffCount++;

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
                break;
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
                flag == UPPER_BOUND ? ttMove : bestmove,
                depth,
                flag,
                isPV,
                thread
            );
        }

        if (!inSingularity &&
            !inCheck &&
            !IsTerminal(bestscore) &&
            (bestmove.IsNull || !p.IsCapture(bestmove) && !bestmove.IsPromotion) &&
            (flag == EXACT_BOUND ||
             flag == UPPER_BOUND && bestscore < ns->StaticEval ||
             flag == LOWER_BOUND && bestscore > ns->StaticEval))
        {
            thread.CorrHist.Update(thread, ref p, ns, bestscore, ns->StaticEval, depth);
        }

        return bestscore;
    }
}
