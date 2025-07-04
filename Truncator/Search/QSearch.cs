using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        TTEntry ttEntry = thread.tt.Probe(p.ZobristKey);
        bool ttHit = ttEntry.Key == p.ZobristKey;
        Move ttMove = ttHit ? new(ttEntry.MoveValue) : Move.NullMove;

        // try for tt-cutoff if the entry is any good
        if (nonPV && ttHit && (
                ttEntry.Flag == EXACT_BOUND ||
                ttEntry.Flag == LOWER_BOUND && ttEntry.Score >= beta ||
                ttEntry.Flag == UPPER_BOUND && ttEntry.Score <= alpha
            ))
        {
            return ttEntry.Score;
        }

        bool inCheck = p.GetCheckers() != 0;

        // stand pat logic
        // stop captureing pieces (& return) if it does not increase evaluation
        int eval = inCheck ? -SCORE_MATE + thread.ply : Pesto.Evaluate(ref p);
        int bestscore = eval;

        if (!inCheck)
        {
            if (eval >= beta)
            {
                return eval;
            }

            if (eval > alpha)
            {
                alpha = eval;
            }
        }        

        // move generation and picking
        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(thread, ref p, ttMove, ref moves, ref scores, inQS: !inCheck);

        // main move loop
        for (Move m = picker.Next(); m.NotNull; m = picker.Next())
        {
            Debug.Assert(m.NotNull);

            // skip quiets if there is a non-loosing line already
            if (inCheck &&
                !p.IsCapture(m) &&
                !IsLoss(bestscore))
            {
                continue;
            }

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