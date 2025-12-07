
using System.Diagnostics;
using static Tunables;

public static partial class Search
{

    public static unsafe int Negamax<Type>(SearchThread thread, int alpha, int beta, int depth, Node* ns, bool cutnode)
        where Type : NodeType
    {
        Debug.Assert(thread.ply < 256);
        Debug.Assert(ns == &thread.nodeStack[thread.ply]);

        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV = isRoot || typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);
        bool inSingularity = ns->ExcludedMove.NotNull;

        // overwrite previous pv line
        thread.rootPos.PVs[thread.MultiPvIdx][thread.ply, thread.ply] = Move.NullMove;

        if (!isRoot)
        {
            // Draw Detection
            // + twofold repetition
            // + insufficient mating material
            // + fiftx move rule
            // - stalemate (covered by move-generation)

            if (ns->p.IsDraw(thread))
            {
                return SCORE_DRAW;
            }

            // mate distance pruning
            // if we already found a mate and are already deeper in the tree than the best mate
            // we can no longer fing a faster mate and can stop searching

            int mdAlpha = Math.Max(alpha, -SCORE_MATE + thread.ply);
            int mdBeta  = Math.Min(beta ,  SCORE_MATE - thread.ply - 1);
            if (mdAlpha >= mdBeta)
            {
                return mdAlpha;
            }
        }

        // for leaf-nodes: drop into QSearch
        // material is the most important 

        if (depth <= 0 || thread.ply >= 128)
        {
            return QSearch<Type>(thread, alpha, beta, ns);
        }

        // probe the transposition table for already visited positions

        bool ttHit = thread.tt.Probe(ns->p.ZobristKey, out TTEntry ttEntry, thread.ply);
        Move ttMove = ttHit ? new(ttEntry.MoveValue) : Move.NullMove;
        bool ttPV = ttHit && ttEntry.PV == 1 || isPV;

        // return the tt-score if the entry is sufficient

        if (nonPV
            && ttHit
            && ttEntry.Depth >= depth
            && !inSingularity
            && (ttEntry.Flag == LOWER_BOUND && ttEntry.Score >= beta
                || ttEntry.Flag == UPPER_BOUND && ttEntry.Score <= alpha
                || ttEntry.Flag == EXACT_BOUND))
        {
            return ttEntry.Score;
        }

        // tablebase probing
        // TODO: check for root-ply >= tbprobeply

        int SyzygyMin = -SCORE_MATE;
        int SyzygyMax = SCORE_MATE;

        if (!isRoot
            && Fathom.DoTbProbing
            && ns->p.CastlingRights == 0
            && ns->p.FiftyMoveRule == 0
            && thread.ply >= Fathom.SyzygyProbePly
            && Utils.popcnt(ns->p.blocker) <= Fathom.TbLargest)
        {

            int res = Fathom.ProbeWdl(ref ns->p);
            Debug.Assert(res >= (int)TbResult.TbLoss && res <= (int)TbResult.TbWin, "unexpected tb probing result");

            thread.tbHits++;

            int TbScore = res switch
            {
                (int)TbResult.TbLoss => -SCORE_TB_WIN + thread.ply,
                (int)TbResult.TbWin => SCORE_TB_WIN - thread.ply,

                // blessed losses and cursed wins are draws in practice
                _ => SCORE_DRAW
            };

            int TbBound = res switch
            {
                (int)TbResult.TbLoss => UPPER_BOUND,
                (int)TbResult.TbWin => LOWER_BOUND,
                _ => EXACT_BOUND
            };

            // check for tb cutoff via bounds

            if (TbBound == EXACT_BOUND
                || TbBound == UPPER_BOUND && TbScore <= alpha
                || TbBound == LOWER_BOUND && TbScore >= beta)
            {
                thread.tt.Write(
                    ns->p.ZobristKey,
                    TbScore,
                    Move.NullMove,
                    depth,
                    TbBound,
                    isPV,
                    thread
                );
                return TbScore;
            }

            // clamp the bestscore to syzygy probing scores
            // only for exact scores (pv nodes)

            if (isPV && TbBound == LOWER_BOUND)
            {
                SyzygyMin = TbScore;
                alpha = Math.Max(alpha, TbScore);
            }

            if (isPV && TbBound == UPPER_BOUND)
            {
                SyzygyMax = TbScore;
            }

        }


        // clear childrens relevant node data

        (ns + 1)->KillerMove = Move.NullMove;
        (ns + 1)->CutoffCount = 0;

        if (isRoot)
        {
            ns->CutoffCount = 0;
        }

        
        ns->InCheck = ns->p.Checkers != 0;

        // static evaluaton
        // although this might be a noisy position and we have to distrust the static
        // evaluation of the current node to an extend, we can still draw some conclusion from it
        // most importantly, big/unnecessary material shifts can be measured

        if (ns->InCheck)
        {
            ns->StaticEval = ns->UncorrectedStaticEval = -SCORE_MATE;
        }
        else
        {
            if (!isRoot)
            {
                Accumulator.DoLazyUpdates(ns);
            }
            
            ns->UncorrectedStaticEval = NNUE.Evaluate(ref ns->p, ns->acc);
            thread.CorrHist.Correct(thread, ref ns->p, ns);
        }

        // the past series of moves improved our static evaluation and indicates
        // that we should take a deeper look at this position than on others
        // only applicable if we have usable evaluations 
        // (not in check now and 2 plies ago)

        bool improving = thread.ply > 1
            && !ns->InCheck
            && (ns - 2)->StaticEval != -SCORE_MATE
            && ns->StaticEval >= (ns - 2)->StaticEval;

        // sometimes whole-node-pruning can be skipped entirely

        if (ns->InCheck || isPV || inSingularity)
        {
            goto skip_whole_node_pruning;
        }


        // reverse futility pruning (RFP)

        if (depth <= RfpDepth
            && ns->StaticEval - RfpMargin - RfpMult * (improving ? depth - 1 : depth) >= beta)
        {
            return ns->StaticEval;   
        }

        // razoring

        if (depth <= RazoringDepth
            && !IsTerminal(alpha)
            && ns->StaticEval + RazoringMargin + RazoringMult * depth <= alpha)
        {
            int RazoringScore = QSearch<NonPVNode>(thread, alpha, beta, ns);

            if (RazoringScore <= alpha)
            {
                return RazoringScore;
            }
        }

        // null move pruning (NMP)

        if ((ns - 1)->move.NotNull
            && ns->StaticEval >= beta
            && ns->p.HasNonPawnMaterial(ns->p.Us)
            && (!ttHit || ttEntry.Flag > UPPER_BOUND || ttEntry.Score >= beta))
        {
            (ns + 1)->p = ns->p;
            (ns + 1)->p.MakeNullMove(thread, updateAcc: false);
            thread.repTable.Push((ns + 1)->p.ZobristKey);

            int R = NmpBaseReduction + depth / NmpDepthDivisor;

            if (!IsTerminal(beta))
            {
                R += Math.Min((ns->StaticEval - beta) / NmpEvalDivisor, 3);
            }

            int ScoreAfterNull = -Negamax<NonPVNode>(thread, -beta, -alpha, depth - R, ns + 1, !cutnode);

            thread.UndoMove();

            if (ScoreAfterNull >= beta)
            {
                return ScoreAfterNull;
            }
        }

    // probcut
    // 'inspired' by Stockfish
    // https://github.com/official-stockfish/Stockfish/blob/adfddd2c984fac5f2ac02d87575af821ec118fa8/src/search.cpp#L910

        int ProbCutBeta = beta + ProbcutBetaMargin;
        if (depth >= ProbuctMinDepth
            && ttHit
            && (ttMove.IsNull || ns->p.IsCapture(ttMove) || ttMove.IsPromotion)
            && ttEntry.Score >= beta
            && !IsTerminal(beta))
        {
            int ProbCutDepth = depth - ProbcutBaseReduction;

            Span<Move> ProbCutMoves = stackalloc Move[128];
            Span<int> ProbCutScores = stackalloc int[128];
            
            var ProbcutPicker = new MovePicker<QSPicker>(
                thread, ttMove,
                ref ProbCutMoves,
                ref ProbCutScores,
                ns->InCheck,
                ProbCutBeta - ns->StaticEval
            );

            for (Move m = ProbcutPicker.Next(ref ns->p); m.NotNull; m = ProbcutPicker.Next(ref ns->p))
            {
                // skip bad and illegal moves

                if (!ns->p.IsLegal(thread, m))
                {
                    continue;
                }

                // make the move and
                // verify with a quick qsearch that the capture really is not loosing

                (ns + 1)->p = ns->p;
                (ns + 1)->p.MakeMove(m, thread, updateAcc: false);
                thread.repTable.Push((ns + 1)->p.ZobristKey);

                int ProbCutScore = -QSearch<NonPVNode>(thread, -ProbCutBeta, -ProbCutBeta + 1, ns + 1);

                // if qsearch passed, do a shallow search

                if (ProbCutScore >= ProbCutBeta && ProbCutDepth > 0)
                {
                    ProbCutScore = -Negamax<NonPVNode>(thread, -ProbCutBeta, -ProbCutBeta + 1, ProbCutDepth, ns + 1, !cutnode);
                }

                thread.UndoMove();

                // if beta is beaten by a large margin usign a shallower search
                // we can relatively savely cut this node

                if (ProbCutScore >= ProbCutBeta && !IsTerminal(ProbCutScore))
                {
                    thread.tt.Write(
                        ns->p.ZobristKey,
                        ProbCutScore,
                        m,
                        ProbCutDepth + 1,
                        LOWER_BOUND,
                        ttPV,
                        thread
                    );

                    return ProbCutScore - (ProbCutBeta - beta);
                }

                // stop at hard timeouts
                if (!thread.doSearch || thread.IsMainThread && TimeManager.IsHardTimeout(thread))
                {
                    return SCORE_MATE;
                }
            }
        }


        // go here on pv or singular nodes or when in check
        skip_whole_node_pruning:


        // check extensions

        if (ns->InCheck && !inSingularity)
        {
            depth++;
        }

        // ToDo: Internal Iterative Reductions

        // movegeneration, scoring and ordering
        // outsourced to the move-picker

        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker<PVSPicker> picker = new (thread, ttMove, ref moves, ref scores, ns->InCheck, SEEPvsBadNoisyThreshold);


        int bestscore = -SCORE_MATE;
        int score = -SCORE_MATE;
        int flag = NONE_BOUND;

        int movesPlayed = 0;
        int quitesCount = 0;
        int noisyCount = 0;
        Span<Move> quietMoves = stackalloc Move[128];
        Span<Move> captureMoves = stackalloc Move[128];

        Move bestmove = Move.NullMove;

        // main move loop
        
        for (Move m = picker.Next(ref ns->p); m.NotNull; m = picker.Next(ref ns->p))
        {
            Debug.Assert(m.NotNull);

            // skip the first move in singular confirmation search
            // this has to be the ttmove and is most likely the best move

            if (m == ns->ExcludedMove)
            {
                continue;
            }

            if (isRoot && thread.rootPos.MoveInMultiPV(m))
            {
                continue;
            }

            long startnodes = thread.nodeCount;
            bool notLoosing = !IsLoss(bestscore);

            bool isCapture = ns->p.IsCapture(m);
            bool isNoisy = isCapture || m.IsPromotion; // ToDo: GivesCheck()

            PieceType pt = ns->p.PieceTypeOn(m.from);

            ns->HistScore = isCapture ? thread.history.CaptHist[ns->p.Us, pt, ns->p.GetCapturedPieceType(m), m.to] :
                (ButterflySearchMult * thread.history.Butterfly[ns->p.Threats, ns->p.Us, m]
                + Conthist1plySearchMult * (*(ns - 1)->ContHist)[ns->p.Us, pt, m.to]
                + Conthist2plySearchMult * (*(ns - 2)->ContHist)[ns->p.Us, pt, m.to])
                / 1024;

            // move loop pruning
            if (!isRoot
                && !inSingularity
                && !isNoisy
                && notLoosing)
            {

                // futility pruning 
                if (nonPV
                    && !ns->InCheck
                    && depth <= FpDepth
                    && ns->StaticEval + FpMargin + depth * FpMult <= alpha)
                {
                    continue;
                }

                // late move pruning
                if (depth <= LmpDepth
                    && movesPlayed >= LmpBase + depth * depth)
                {
                    continue;
                }

                // main-history pruning
                if (depth <= HpDepth
                    && ns->HistScore < -(HpBase + HpLinMult * depth + HpSqrMult * depth * depth))
                {
                    continue;
                }

                // ToDo: Continuation Pruning

            }

            // pvs SEE pruning
            // ToDo: margin -= histScore / 8
            if (!isRoot
                && notLoosing
                && !SEE.SEE_threshold(m, ref ns->p, isCapture ? SEENoisyMult * depth : SEEQuietMult * depth * depth))
            {
                continue;
            }


            // singular extensions
            int extension = 0;

            if (m == ttMove
                && !inSingularity
                && !isRoot
                && depth >= 8
                && ttEntry.Depth >= depth - 3
                && ttEntry.Flag > UPPER_BOUND
                && thread.ply < thread.completedDepth * 2)
            {

                int singularBeta = Math.Max(-SCORE_MATE + 1, ttEntry.Score - depth - SEBetaDepthMargin);
                int singularDepth = (depth - 1) / 2;

                ns->ExcludedMove = m;
                int singularScore = Negamax<NonPVNode>(thread, singularBeta - 1, singularBeta, singularDepth, ns, cutnode);
                ns->ExcludedMove = Move.NullMove;

                if (singularScore < singularBeta)
                {
                    if (!isPV && singularScore < singularBeta - SEDoubleMargin)
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

            if (!ns->p.IsLegal(thread, m))
            {
                continue;
            }

            // make the move and update the boardstate

            (ns + 1)->p = ns->p;
            (ns + 1)->p.MakeMove(m, thread, updateAcc: false);
            thread.repTable.Push((ns + 1)->p.ZobristKey);

            movesPlayed++;
            if (!isCapture) quietMoves[quitesCount++] = m;
            else captureMoves[noisyCount++] = m;
            
            // late-move-reductions (LMR)
            // assuming our move-ordering is good, the first played move should be the best
            // all moves after that are expected to be worse. we will validate this thesis by 
            // searching them at a cheaper shallower depth. if a move seems to beat the current best move,
            // we need to re-search that move at full depth to confirm its the better move.

            if (movesPlayed > 1 && depth >= 2)
            {
                int R = 1;

                if (!isCapture)
                {

                    R = GetBaseLmr(depth, movesPlayed);

                    // reduce more for bad history values
                    R += -ns->HistScore / LmrHistDiv;

                    if (thread.ply > 1 && !improving) R++;

                    if (ttPV) R--;

                    // ToDo: R += isAllnode
                    if ((ns + 1)->CutoffCount > 2) R++;

                    if (m == ns->KillerMove) R--;

                    R = Math.Max(1, R);
                }

                else // isCapture
                {
                    R -= ns->HistScore / 512;

                    if (picker.stage == Stage.BadCaptures)
                        R++;

                    R = Math.Max(1, R);
                }

                // zero-window-search (ZWS)
                // as part of the principal-variation-search, we assume that all lines that are not the pv
                // are worse than the pv. therefore, we search the nonPv lines with a zero-window around alpha
                // to cause a lot more curoffs. the returned value will never be an exact score, but an upper-
                // or lower-bound. if a move unexpectedly beats the pv, we need to re-search it at full depth,
                // to confirm it is really better than the pv and obtain its exact value.

                score = -Negamax<NonPVNode>(thread, -alpha - 1, -alpha, depth - R, ns + 1, true);

                // re-search if LMR seems to beat the current best move

                if (score > alpha && R > 1)
                {
                    score = -Negamax<NonPVNode>(thread, -alpha - 1, -alpha, depth - 1, ns + 1, !cutnode);
                }
            }

            // ZWS for moves that are not reduced by LMR
            // aka only the first move of non-pv nodes

            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NonPVNode>(thread, -alpha - 1, -alpha, depth + extension - 1, ns + 1, !cutnode);
            }

            // full-window-search (FWS)
            // either the pv-line is searched fully at first, or a high-failing ZWS search needs to be confirmed

            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax<PVNode>(thread, -beta, -alpha, depth + extension - 1, ns + 1, false);
            }

            thread.UndoMove();

            if (isRoot)
            {
                thread.rootPos.ReportBackMove(m, score, thread.nodeCount - startnodes, depth);
            }

            if (isPV && (score > alpha || movesPlayed == 1))
            {
                if (isRoot)
                {
                    thread.rootPos.PVs[thread.MultiPvIdx][depth] = score;
                }

                // dont replace previous root bestmove on a fail-low
                // no low failing move can really be trusted to be better than the last best move

                thread.rootPos.PVs[thread.MultiPvIdx].Push(m, thread.ply);
            }

            if (score > bestscore)
            {
                // beating the best score does not mean we beat alpha (e.g. for the first move)
                // all scores below alpha are just upper bounds and might actually be even smaller

                bestscore = score;
                flag = UPPER_BOUND;

                if (score > alpha)
                {
                    // now we have an exact score and can confidently save a bestmove

                    alpha = score;
                    bestmove = m;
                    flag = EXACT_BOUND;

                    if (score >= beta)
                    {
                        // fail high
                        // the opponent can already force a better line and will not allow us to
                        // play the current one, so we dont need to search this branch any further
                        // the current bound is a lower bound and might actually be even bigger

                        flag = LOWER_BOUND;

                        // update history
                        // ToDo: Bonus = depth * (depth + (m == ttmove))
                        // ToDo: Bonus = depth * (depth + (eval < alpha))

                        int temp = depth;

                        if (!ns->InCheck && ns->StaticEval < alpha)
                        {
                            temp++;
                        }

                        int HistDelta = temp * temp;

                        if (!isCapture)
                        {
                            thread.history.UpdateQuietMoves(thread, ns, HistDelta, -HistDelta, ref ns->p, ref quietMoves, quitesCount, m);

                            // update killer-move
                            ns->KillerMove = m;
                        }

                        thread.history.UpdateCaptureMoves(thread, ns, HistDelta, HistDelta, ref ns->p, ref captureMoves, noisyCount, m);

                        // ToDo: CutoffCount += isPv || nonPV
                        ns->CutoffCount++;

                        break;

                    } // beta beaten
                } // alpha beaten
            } // best beaten

            // check if we have time left
            // do this in the move-loop & after updaing the pv
            // otherwise we could end up without a best move
            // TODO: instantly return instead of breaking, the search results 
            // are unfinished and unreliabla
            // TODO: move this up earlier in the moveloop for earlier and more correct exits 

            if (!thread.doSearch || thread.IsMainThread && TimeManager.IsHardTimeout(thread))
            {
                break;
            }

        } // move-loop

        // check-/stalemate detection
        // if there are no legal moves in a position, it is terminal
        // if we have no legal moves and are in check, its checkmate and we lost
        // if we dont have legal moves and are not in check, its stalemate and a draw

        if (movesPlayed == 0)
        {
            return !ns->InCheck ? SCORE_DRAW : -SCORE_MATE + thread.ply;
        }

        // dont save mating-scores to the tt, it cant handle them at the moment
        // TODO: fix mate-scores for tt
        
        if (!inSingularity)
        {
            thread.tt.Write(
                ns->p.ZobristKey,
                bestscore,
                flag == UPPER_BOUND ? ttMove : bestmove,
                depth,
                flag,
                ttPV,
                thread
            );
        }

        // if the position is quiet (best move is not tacitcal and not in check)
        // and the static evaluation is out of bounds of the search result
        // update the corection terms for the given position in the direction
        // of the search result

        if (!inSingularity
            && !ns->InCheck
            && !IsTerminal(bestscore)
            && (bestmove.IsNull || !ns->p.IsCapture(bestmove) && !bestmove.IsPromotion)
            && (flag == EXACT_BOUND
                || flag == UPPER_BOUND && bestscore < ns->StaticEval
                || flag == LOWER_BOUND && bestscore > ns->StaticEval))
        {
            thread.CorrHist.Update(thread, ref ns->p, ns, bestscore, ns->StaticEval, depth);
        }

        // when recieving exact scores (only in pv/root nodes)
        // always stay in bounds of the syzygy probe result

        if (isPV)
        {
            bestscore = Math.Clamp(bestscore, SyzygyMin, SyzygyMax);
        }

        return bestscore;
    }
}
