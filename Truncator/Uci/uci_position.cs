
using System.Diagnostics;

public static partial class UCI
{
    public static void Position(string[] tokens)
    {
        Debug.Assert(state == UciState.Idle, "do not change root pos when not idle!");
        Debug.Assert(tokens[0] == "position");

        var thread = ThreadPool.MainThread;

        if (tokens.Length < 2)
        {
            throw new Exception("position string does not specify any position!");
        }

        string fen = tokens[1] == "startpos" ? Utils.startpos : string.Join(' ', SkipPast(tokens, "fen").Take(6));
        thread.rootPos.SetNewFen(fen);

        foreach (string movestr in SkipPast(tokens, "moves"))
        {
            thread.rootPos.MakeMove(movestr);
        }

        thread.rootPos.InitRootMoves();

        unsafe
        {
            thread.nodeStack[0].acc.Accumulate(ref thread.rootPos.p);
        }
    }

}
