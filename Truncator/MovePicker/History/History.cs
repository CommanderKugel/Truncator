
public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
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
