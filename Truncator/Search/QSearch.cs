using System.Diagnostics;

public static partial class Search
{

    public unsafe static int QSearch<Type>(SearchThread thread, Pos p, int alpha, int beta, Node* ns)
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
        // every entry that matches this positin is worth using

        if (nonPV
            && ttHit
            && (ttEntry.Flag == EXACT_BOUND
                || ttEntry.Flag == LOWER_BOUND && ttEntry.Score >= beta
                || ttEntry.Flag == UPPER_BOUND && ttEntry.Score <= alpha))
        {
            return ttEntry.Score;
        }

        bool inCheck = p.GetCheckers() != 0;

        // stand pat logic
        // stop captureing pieces (& return) if it does not increase evaluation
        if (inCheck)
        {
            ns->StaticEval = ns->UncorrectedStaticEval = -SCORE_MATE + thread.ply;
        }
        else
        {
            ns->UncorrectedStaticEval = Pesto.Evaluate(ref p);
            thread.CorrHist.Correct(thread, ref p, ns);
        }

        int bestscore = ns->StaticEval;

        // Stand pat logic
        // sometimes we can simply do nothing and enjoy having beta already beaten

        if (!inCheck)
        {
            if (ns->StaticEval >= beta)
            {
                return ns->StaticEval;
            }

            if (ns->StaticEval > alpha)
            {
                alpha = ns->StaticEval;
            }
        }

        // move generation and picking
        // outsourced to movepicker
        
        Span<Move> moves = stackalloc Move[256];
        Span<int> scores = stackalloc int[256];
        MovePicker picker = new MovePicker(thread, ttMove, ref moves, ref scores, !inCheck);

        // main move loop

        for (Move m = picker.Next(ref p); m.NotNull; m = picker.Next(ref p))
        {
            Debug.Assert(m.NotNull);

            // skip quiets if there is a non-loosing line already
            // quiets are only generated when in check anyways

            if (inCheck
                && !p.IsCapture(m)
                && !IsLoss(bestscore))
            {
                continue;
            }

            // simply prune all bad captures

            if (nonPV && !SEE.SEE_threshold(m, ref p, 0))
            {
                continue;
            }

            // only make legal moves

            if (!p.IsLegal(thread, m))
            {
                continue;
            }

            Pos next = p;
            next.MakeMove(m, thread);

            int score = -QSearch<Type>(thread, next, -beta, -alpha, ns + 1);

            // ~unmake
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
                    // now we have an exact score

                    alpha = score;

                    if (score >= beta)
                    {
                        // fail high
                        break;
                    }
                }
            }
        }

        return bestscore;
    }

}