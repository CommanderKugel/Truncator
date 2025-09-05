
public struct History : IDisposable
{
    public bool isDisposed;

    public CaptureHistory CaptHist;
    public ButterflyHistory Butterfly;
    public ContinuationHistory ContHist;

    public History()
    {
        isDisposed = false;
        CaptHist = new();
        Butterfly = new();
        ContHist = new();
    }

    public unsafe void UpdateQuietMoves(SearchThread thread, Node* n, int bonus, int penalty, ref Pos p, ref Span<Move> quiets, int count, Move bestmove)
    {
        var NullHist = thread.history.ContHist.NullHist;

        for (int i = 0; i < count; i++)
        {
            ref Move m = ref quiets[i];
            var delta = (m == bestmove) ? bonus : penalty;
            PieceType pt = p.PieceTypeOn(m.from);

            Butterfly[p.Threats, p.Us, m].Update(delta, Tunables.ButterflyDiv);

            if ((n - 1)->ContHist != NullHist)
            {
                (*(n - 1)->ContHist)[p.Us, pt, m.to].Update(delta, Tunables.ContHistDiv);
            }

            if ((n - 2)->ContHist != NullHist)
            {
                (*(n - 2)->ContHist)[p.Us, pt, m.to].Update(delta, Tunables.ContHistDiv);
            }
        }
    }

    public unsafe void UpdateCaptureMoves(int bonus, int penalty, ref Pos p, ref Span<Move> capts, int count, Move bestmove)
    {
        for (int i = 0; i < count; i++)
        {
            ref Move m = ref capts[i];
            PieceType pt = p.PieceTypeOn(m.from);
            PieceType vict = p.GetCapturedPieceType(m);

            int delta = (m == bestmove) ? bonus : penalty;
            CaptHist[p.Us, pt, vict, m.to].Update(delta, 1024);
        }
    }

    public void Clear()
    {
        CaptHist.Clear();
        Butterfly.Clear();
        ContHist.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            CaptHist.Dispose();
            Butterfly.Dispose();
            ContHist.Dispose();
            isDisposed = true;
        }
    }

}
