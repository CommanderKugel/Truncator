
using System.Diagnostics;

public static partial class Search
{

    public static int Negamax<Type>(SearchThread thread, Pos p, int alpha, int beta, int depth)
        where Type : NodeType
    {
        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV = isRoot || typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);


        if (!isRoot && isPV)
        {
            thread.NewPVLine();
        }

        if (!isRoot)
        {
            if (thread.repTable.IsTwofoldRepetition(ref p))
            {
                return SCORE_DRAW;
            }
        }

        if (depth <= 0)
        {
            return QSearch<Type>(thread, p, alpha, beta);
        }


        TTEntry entry = thread.tt.Probe(p.ZobristKey);
        bool ttHit = entry.Key == p.ZobristKey;
        Move ttMove = ttHit ? new(entry.MoveValue) : Move.NullMove;


        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(thread, ref p, ttMove, ref moves, ref scores, false);


        int bestscore = -SCORE_MATE;
        int score = -SCORE_MATE;
        int flag = NONE_BOUND;
        int movesPlayed = 0;
        Move bestmove = Move.NullMove;

        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);
            bool isCapture = p.IsCapture(m);

            long startnodes = thread.nodeCount;

            if (!p.IsLegal(m))
            {
                continue;
            }

            Pos next = p;
            next.MakeMove(m, thread);
            movesPlayed++;

            thread.ply++;
            thread.repTable.Push(next.ZobristKey);

            if (movesPlayed > 1 && depth >= 2 && !isCapture)
            {
                int R = 1 + (int)Math.Log(depth);
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - R);

                if (score > alpha && R > 1)
                {
                    score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - 1);
                }
            }
            else if (nonPV || movesPlayed > 1)
            {
                score = -Negamax<NonPVNode>(thread, next, -alpha - 1, -alpha, depth - 1);
            }

            if (isPV && (score > alpha || movesPlayed == 1))
            {
                score = -Negamax<PVNode>(thread, next, -beta, -alpha, depth - 1);
            }

            // UndoMove
            thread.ply--;
            thread.repTable.Pop();


            if (isRoot)
            {
                UCI.rootPos.ReportBackMove(m, score, thread.nodeCount - startnodes, depth);
            }

            if (!thread.doSearch ||
                 thread.IsMainThread && TimeManager.IsHardTimeout())
            {
                return SCORE_TIMEOUT;
            }

            if (score > bestscore)
            {
                bestscore = score;

                if (isPV)
                {
                    thread.PushToPV(m);
                }

                if (score > alpha)
                {
                    alpha = score;
                    bestmove = m;
                    flag = EXACT_BOUND;

                    if (score >= beta)
                    {
                        flag = LOWER_BOUND;

                        if (!isCapture)
                        {
                            thread.history.Butterfly.Update((short)(depth * depth), p.Us, m);
                        }

                        break;
                    }
                }
            }
        }

        if (movesPlayed == 0)
        {
            return p.GetCheckers() == 0 ? SCORE_DRAW : -SCORE_MATE + thread.ply;
        }

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

        return movesPlayed == 0 && p.GetCheckers() == 0 ? SCORE_DRAW : bestscore;
    }
}
