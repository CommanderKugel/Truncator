
using System.Diagnostics;

public struct History : IDisposable
{
    public bool isDisposed;

    public ButterflyHistory Butterfly;
    public CaptureHistory CaptHist;

    public History()
    {
        isDisposed = false;
        Butterfly = new();
        CaptHist = new();
    }

    public unsafe void UpdateQuietMoves(short bonus, short penalty, SearchThread thread, Node* n, ref Pos p, ref Span<Move> quiets, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            UpdateSingleQuiet(penalty, p.Us, quiets[i], p.PieceTypeOn(quiets[i].from));
        }

        UpdateSingleQuiet(bonus, p.Us, quiets[count - 1], p.PieceTypeOn(quiets[count - 1].from));
    }

    private unsafe void UpdateSingleQuiet(short delta, Color c, Move m, PieceType pt)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(m.NotNull);
        Butterfly[c, m] <<= delta;
    }

    public unsafe void UpdateCatureMoves(short bonus, short penalty, ref Pos p, ref Span<Move> capts, int count, Move bestmove)
    {
        for (int i = 0; i < count; i++)
        {
            PieceType victim = p.GetCapturedPieceType(capts[i]);
            PieceType attacker = p.PieceTypeOn(capts[i].from);
            CaptHist[p.Us, victim, attacker, capts[i]] <<= capts[i] == bestmove ? bonus : penalty;
        }
    }

    public void Clear()
    {
        Butterfly.Clear();
        CaptHist.Clear();
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Butterfly.Dispose();
            CaptHist.Dispose();
            isDisposed = true;
        }
    }

}
