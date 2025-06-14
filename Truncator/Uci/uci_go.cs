
using System.Diagnostics;

public static partial class UCI
{
    public static void Go(string[] tokens)
    {
        Debug.Assert(tokens[0] == "go");
        Debug.Assert(state == UciState.Idle, "cant start going when not previously idle!");
        state = UciState.Searching;

        TimeManager.Reset();

        if (tokens.Contains("wtime")) TimeManager.wtime = int.Parse(SkipPast(tokens, "wtime").First());
        if (tokens.Contains("btime")) TimeManager.btime = int.Parse(SkipPast(tokens, "btime").First());
        if (tokens.Contains("winc" )) TimeManager.winc  = int.Parse(SkipPast(tokens, "winc" ).First());
        if (tokens.Contains("binc" )) TimeManager.binc  = int.Parse(SkipPast(tokens, "binc" ).First());

        if (tokens.Contains("movestogo")) TimeManager.movestogo = int.Parse(SkipPast(tokens, "movestogo").First());
        if (tokens.Contains("depth"    )) TimeManager.depth     = int.Parse(SkipPast(tokens, "depth"    ).First());
        if (tokens.Contains("nodes"    )) TimeManager.hardnodes = int.Parse(SkipPast(tokens, "nodes"    ).First());
        if (tokens.Contains("movetime" )) TimeManager.movetime  = int.Parse(SkipPast(tokens, "movetime" ).First());

        // searchmoves <move1> ... <movei>
        // ponder
        // mate <x>

        TimeManager.Start(ThreadPool.MainThread.rootPos.p.Us);
        ThreadPool.Go();
    }
}
