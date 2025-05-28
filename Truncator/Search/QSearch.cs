using System.Diagnostics;

public static partial class Search
{

    public static int QSearch<Type>(SearchThread thread, Pos p, int alpha, int beta)
        where Type : NodeType
    {
        Debug.Assert(typeof(Type) != typeof(RootNode), "QSearch can never examine root-nodes");


        TTEntry entry = thread.tt.Probe(p.ZobristKey);
        bool ttHit = entry.Key == p.ZobristKey;
        Move ttMove = ttHit ? new(entry.MoveValue) : Move.NullMove;

        int eval = BasicPsqt.Evaluate(ref p);

        if (eval >= beta)
        {
            return eval;
        }

        if (eval > alpha)
        {
            alpha = eval;
        }

        // ToDo: mate-score when in check
        int bestscore = eval;

        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(ref p, ttMove, ref moves, ref scores, inQS: true);

        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);

            if (!p.IsLegal(m))
            {
                continue;
            }

            Pos next = p;
            next.MakeMove(m, thread);
            thread.ply++;

            int score = -QSearch<Type>(thread, next, -beta, -alpha);

            thread.ply--;


            if (score > bestscore)
                {
                    bestscore = score;

                    if (score > alpha)
                    {
                        alpha = score;

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