
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

    }
}
