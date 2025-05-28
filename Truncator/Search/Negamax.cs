
using System.Data;
using System.Diagnostics;

public static partial class Search
{

    public static int Negamax<Type>(SearchThread thread, Pos p, int alpha, int beta, int depth)
        where Type : NodeType
    {
        bool isRoot = typeof(Type) == typeof(RootNode);
        bool isPV = isRoot || typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);


        if (depth <= 0)
        {
            return QSearch<Type>(thread, p, alpha, beta);
        }


        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(ref p, Move.NullMove, ref moves, ref scores, false);


        int bestscore = -SCORE_MATE + thread.ply;
        Move bestmove = Move.NullMove;

        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);

            long startnodes = thread.nodeCount;

            if (!p.IsLegal(m))
            {
                continue;
            }

            Pos next = p;
            next.MakeMove(m, thread);

            int score = -Negamax<PVNode>(thread, next, -beta, -alpha, depth - 1);


            if (!thread.doSearch ||
                 thread.IsMainThread && TimeManager.IsHardTimeout())
            {
                return SCORE_TIMEOUT;
            }


            if (isRoot)
            {
                UCI.rootPos.ReportBackMove(m, score, thread.nodeCount - startnodes, depth);
            }

            if (score > bestscore)
            {
                bestscore = score;

                if (score > alpha)
                {
                    alpha = score;
                    bestmove = m;

                    if (score >= beta)
                    {
                        break;
                    }
                }
            }
        }

        return bestscore;
    }
}
