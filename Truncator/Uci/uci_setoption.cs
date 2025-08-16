
using System.Diagnostics;

public static partial class UCI
{
    private static void SetOption(string[] tokens)
    {
        Debug.Assert(tokens[0] == "setoption");
        Debug.Assert(state == UciState.Idle, "cant use setoption when not idle!");

        // setoption name <ID> [value <X>]

        if (tokens[2] == "Threads")
        {
            Debug.Assert(tokens.Length == 5);
            int value = int.Parse(tokens[4]);
            ThreadPool.Resize(value);
        }

        if (tokens[2] == "Hash")
        {
            Debug.Assert(tokens.Length == 5);
            int sizemb = int.Parse(tokens[4]);
            ThreadPool.tt.Resize(sizemb);
        }

        if (tokens[2] == "Move" && tokens[3] == "Overhead")
        {
            Debug.Assert(tokens.Length == 6);
            int overhead = int.Parse(tokens[5]);
            throw new NotImplementedException("setting move overhead is not impleented yet");
        }

        if (tokens[2] == "UCI_Chess960")
        {
            UCI.IsChess960 = tokens[4] == "true";
            Console.WriteLine($"info string set UCI_Chess960 to {UCI.IsChess960}");
        }

        if (tokens[2] == "UCI_ShowWDL" && tokens.Length >= 5)
        {
            WDL.UCI_showWDL = tokens[4] == "true";
            Console.WriteLine($"into string set UCI_ShowWDL to {WDL.UCI_showWDL}");
        }

        if (tokens[2] == "SyzygyPath" && tokens.Length >= 5)
        {
            Debug.Assert(tokens.Length == 5);
            var path = tokens[4];
            Fathom.Init(path);
        }

        if (tokens[2] == "SyzygyProbePly")
        {
            Debug.Assert(tokens.Length == 5);
            int ply = int.Parse(tokens[4]);
            Fathom.SyzygyProbePly = ply;
            Console.WriteLine($"SyzygyProbePly set to {ply}");
        }

    }
}
