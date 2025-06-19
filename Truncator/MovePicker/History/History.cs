
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

    public unsafe void UpdateQuietMoves(short bonus, short penalty, SearchThread thread, ref Pos p, ref Span<Move> quiets, int count)
    {
        Node* n = &thread.nodeStack[thread.ply];
        PieceToHistory* ContHist = thread.ply <= 0 ? thread.history.ContHist.NullHist
            : (n - 1)->ContHist;

        for (int i = 0; i < count - 1; i++)
        {
            UpdateSingleQuiet(penalty, p.Us, quiets[i], p.PieceTypeOn(quiets[i].from), ContHist);
        }

        UpdateSingleQuiet(bonus, p.Us, quiets[count - 1], p.PieceTypeOn(quiets[count - 1].from), ContHist);
    }

    private unsafe void UpdateSingleQuiet(short delta, Color c, Move m,  PieceType pt, PieceToHistory* ContHist)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(m.NotNull);
        Butterfly[c, m] <<= delta;
        (*ContHist)[c, pt, m.to] <<= delta;
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
