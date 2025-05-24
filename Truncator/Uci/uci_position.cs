
using System.Diagnostics;

public static partial class UCI
{
    public static RootPos rootPos;

    public static void Position(string[] tokens)
    {
        Debug.Assert(state == UciState.Idle, "do not change root pos when not idle!");
        Debug.Assert(tokens[0] == "position");

        if (tokens.Length < 2)
        {
            throw new Exception("position string does not specify any position!");
        }

        string fen = tokens[1] == "startpos" ? Utils.startpos : string.Join(' ', SkipPast(tokens, "fen").Take(6));
        rootPos.SetNewFen(fen);

        foreach (string movestr in SkipPast(tokens, "moves"))
        {
            rootPos.MakeMove(movestr);
        }

        rootPos.InitRootMoves();
    }

}
