
using static Tunables;

public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;
    public ContinuationHistory ContHist;
    public CaptureHistory CaptHist;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
        ContHist = new();
        CaptHist = new();
    }

    public unsafe void UpdateCaptureMoves(SearchThread thread, Node* n, int bonus, int penalty, ref Pos p, ref Span<Move> capts, int count, Move bestmove)
    {
        for (int i = 0; i < count; i++)
        {
            ref var m = ref capts[i];
            var att = p.PieceTypeOn(m.from);
            var vict = p.GetCapturedPieceType(m);

            var delta = (m == bestmove) ? bonus : penalty;
            CaptHist[p.Us, att, vict, m.to].Update(delta, CaptHistDiv);
        }
    }

    public unsafe void UpdateQuietMoves(SearchThread thread, Node* n, int bonus, int penalty, ref Pos p, ref Span<Move> quiets, int count, Move bestmove)
    {
        var NullHist = thread.history.ContHist.NullHist;

        for (int i = 0; i < count; i++)
        {
            ref Move m = ref quiets[i];
            var delta = (m == bestmove) ? bonus : penalty;
            PieceType pt = p.PieceTypeOn(m.from);

            Butterfly[p.Threats, p.Us, m].Update(delta, ButterflyDiv);

            if ((n - 1)->ContHist != NullHist)
            {
                (*(n - 1)->ContHist)[p.Us, pt, m.to].Update(delta, Conthist1PlyDiv);
            }

            if ((n - 2)->ContHist != NullHist)
            {
                (*(n - 2)->ContHist)[p.Us, pt, m.to].Update(delta, Conthist2PlyDiv);
            }
        }
    }

    public void Clear()
    {
        Butterfly.Clear();
        ContHist.Clear();
        CaptHist.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Butterfly.Dispose();
            ContHist.Dispose();
            CaptHist.Dispose();
            isDisposed = true;
        }
    }

}
