
using System.Diagnostics;

public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
    }

    public unsafe void UpdateQuietMoves(short bonus, short penalty, ref Pos p, ref Span<Move> quiets, int count, Move bestmove)
    {
        for (int i = 0; i < count; i++)
        {
            var delta = (quiets[i] == bestmove) ? bonus : penalty;
            Butterfly[p.Us, quiets[i]].Update(delta);
        }
    }

    public void Clear()
    {
        Butterfly.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Butterfly.Dispose();
            isDisposed = true;
        }
    }

}
