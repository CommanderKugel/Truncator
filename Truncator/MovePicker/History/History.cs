
using System.Diagnostics;

public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;
    public PawnHistory PawnHist;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
        PawnHist = new();
    }

    public unsafe void UpdateQuietMoves(short bonus, short penalty, SearchThread thread, Node* n, ref Pos p, ref Span<Move> quiets, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            UpdateSingleQuiet(penalty, p.Us, quiets[i], thread.ply, n, p.PieceTypeOn(quiets[i].from), p.PawnKey);
        }

        UpdateSingleQuiet(bonus, p.Us, quiets[count - 1], thread.ply, n, p.PieceTypeOn(quiets[count - 1].from), p.PawnKey);
    }

    private unsafe void UpdateSingleQuiet(short delta, Color c, Move m, int ply, Node* n, PieceType pt, ulong pawnKey)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(m.NotNull);
        Butterfly[c, m] <<= delta;
        PawnHist[c, pt, m.to, pawnKey] <<= delta;
    }

    public void Clear()
    {
        Butterfly.Clear();
        PawnHist.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Butterfly.Dispose();
            PawnHist.Dispose();
            isDisposed = true;
        }
    }

}
