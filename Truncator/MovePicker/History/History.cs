
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

    public void UpdateQuiet(short delta, Color c, Move m)
    {
        Debug.Assert(c == Color.White || c == Color.Black);
        Debug.Assert(m.NotNull);

        Butterfly.Update(delta, c, m);
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
