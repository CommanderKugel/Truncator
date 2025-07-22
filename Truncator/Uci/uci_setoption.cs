
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
            Console.WriteLine($"info string 'set Threadcount to {ThreadPool.ThreadCount}'");
        }

        if (tokens[2] == "Hash")
        {
            Debug.Assert(tokens.Length == 5);
            int sizemb = int.Parse(tokens[4]);
            ThreadPool.tt.Resize(sizemb);
            Console.WriteLine($"info string 'set tt Hash Size to {ThreadPool.tt.SizeMB}'");
        }

        if (tokens[2] == "Move" && tokens[3] == "Overhead")
        {
            Debug.Assert(tokens.Length == 6);
            int overhead = int.Parse(tokens[5]);
            TimeManager.MoveOverhead = Math.Clamp(overhead, TimeManager.OVERHEAD_MIN, TimeManager.OVERHEAD_MAX);
            Console.WriteLine($"info string 'set Overhead to {TimeManager.MoveOverhead}'");
        }

        if (tokens[2] == "UCI_ShowWDL" && tokens.Length >= 5)
        {
            WDL.UCI_showWDL = tokens[4] == "true";
            Console.WriteLine($"info string 'set UCI_ShowWDL to {WDL.UCI_showWDL}'");
        }

    }
}
