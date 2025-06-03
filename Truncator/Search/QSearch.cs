using System.Diagnostics;
using System.Security.Cryptography;

public static partial class Search
{

    public static int QSearch<Type>(SearchThread thread, Pos p, int alpha, int beta)
        where Type : NodeType
    {
        Debug.Assert(typeof(Type) != typeof(RootNode), "QSearch can never examine root-nodes");
        Debug.Assert(thread.ply < 256);

        bool isPV = typeof(Type) == typeof(PVNode);
        bool nonPV = typeof(Type) == typeof(NonPVNode);

        if (isPV)
        {
            thread.NewPVLine();
        }

        // probe the tt for a transposition
        TTEntry entry = thread.tt.Probe(p.ZobristKey);
        bool ttHit = entry.Key == p.ZobristKey;
        Move ttMove = ttHit ? new(entry.MoveValue) : Move.NullMove;

        // try for tt-cutoff if the entry is any good
        if (nonPV && ttHit && (
                entry.Flag == EXACT_BOUND ||
                entry.Flag == LOWER_BOUND && entry.Score >= beta ||
                entry.Flag == UPPER_BOUND && entry.Score <= alpha
            ))
        {
            return entry.Score;
        }

        // stand pat logic
        // stop captureing pieces (& return) if it does not increase evaluation
        int eval = Pesto.Evaluate(ref p);

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

        // move generation and picking
        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(thread, ref p, ttMove, ref moves, ref scores, inQS: true);

        // main move loop
        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);

            // prune all bad captures
            if (nonPV &&
                !SEE.SEE_threshold(m, ref p, 0))
            {
                continue;
            }

            // only make legal moves
            if (!p.IsLegal(m))
            {
                continue;
            }

            Pos next = p;
            next.MakeMove(m, thread);

            // no re-searches, we only ever pass null-windows in nonPV nodes
            int score = -QSearch<Type>(thread, next, -beta, -alpha);

            thread.ply--;


            if (score > bestscore)
            {
                bestscore = score;

                if (isPV)
                {
                    thread.seldepth = Math.Max(thread.ply, thread.seldepth);
                    thread.PushToPV(m);
                }

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