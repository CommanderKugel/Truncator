
using System.Diagnostics;

public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;
    public ContinuationHistory ContHist;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
        ContHist = new();
    }

    public unsafe void UpdateQuietMoves(SearchThread thread, Node* n, short bonus, short penalty, ref Pos p, ref Span<Move> quiets, int count, Move bestmove)
    {
        var NullHist = thread.history.ContHist.NullHist;

        for (int i = 0; i < count; i++)
        {
            ref Move m = ref quiets[i];
            var delta = (m == bestmove) ? bonus : penalty;
            PieceType pt = p.PieceTypeOn(m.from);

            Butterfly[p.Us, m, p.AllThreats].Update(delta);

            if ((n - 1)->ContHist != NullHist)
            {
                (*(n - 1)->ContHist)[p.Us, pt, m.to].Update(delta);
            }

            if ((n - 2)->ContHist != NullHist)
            {
                (*(n - 2)->ContHist)[p.Them, pt, m.to].Update(delta);
            }
        }
    }

    public void Clear()
    {
        Butterfly.Clear();
        ContHist.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Butterfly.Dispose();
            ContHist.Dispose();
            isDisposed = true;
        }
    }

}
