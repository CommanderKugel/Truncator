
using System.Diagnostics;

public static partial class UCI
{
    public static void Go(string[] tokens)
    {
        Debug.Assert(tokens[0] == "go");
        Debug.Assert(state == UciState.Idle, "cant start going when not previously idle!");

        // searchmoves <move1> ... <movei>
        // ponder
        // wtime <x>
        // btime <x>
        // winc <x>
        // binc <x>
        // movestogo <x>
        // depth <x>
        // nodes <x>
        // mate <x>
        // movetime <x>
        // infinite

        thread.p = UciRootPos.p;

        Move bestMove = Search.GetRandomMove(thread);
        Console.WriteLine($"bestmove {bestMove}");
    }
}
