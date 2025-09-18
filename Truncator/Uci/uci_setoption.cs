
using System.Diagnostics;

public static partial class UCI
{
    private static void SetOption(string[] tokens)
    {
        Debug.Assert(tokens[0] == "setoption");
        Debug.Assert(state == UciState.Idle, "cant use setoption when not idle!");

        // setoption name <ID> [value <X>]

        string nameStr = tokens[2];
        string valueStr = tokens.Length == 5 ? tokens[4] : "";

        if (nameStr == "Threads")
        {
            Debug.Assert(tokens.Length == 5);
            int value = int.Parse(valueStr);
            ThreadPool.Resize(value);
        }

        else if (nameStr == "Hash")
        {
            Debug.Assert(tokens.Length == 5);
            int sizemb = int.Parse(valueStr);
            ThreadPool.tt.Resize(sizemb);
        }

        else if (nameStr == "Move" && tokens[3] == "Overhead")
        {
            Debug.Assert(tokens.Length == 6);
            int overhead = int.Parse(tokens[5]);
            TimeManager.MoveOverhead = overhead;
            Console.WriteLine($"info string Move Overhead set to {overhead}");
        }

        else if (nameStr == "UCI_ShowWDL" && tokens.Length >= 5)
        {
            WDL.UCI_showWDL = valueStr == "true";
            Console.WriteLine($"info string set UCI_ShowWDL to {WDL.UCI_showWDL}");
        }

        else if (nameStr == "SyzygyPath" && tokens.Length >= 5)
        {
            Debug.Assert(tokens.Length == 5);
            var path = valueStr;
            Fathom.Init(path);
        }

        else if (nameStr == "SyzygyProbePly")
        {
            Debug.Assert(tokens.Length == 5);
            int ply = int.Parse(valueStr);
            Fathom.SyzygyProbePly = ply;
            Console.WriteLine($"info string SyzygyProbePly set to {ply}");
        }

        else if (nameStr == "Softnodes")
        {
            TimeManager.UciSoftnodes = long.Parse(valueStr);
            Console.WriteLine($"info string set softnodes={TimeManager.UciSoftnodes}");
        }

        else if (nameStr == "Hardnodes")
        {
            TimeManager.UciHardnodes = long.Parse(valueStr);
            Console.WriteLine($"info string set hardnodes={TimeManager.UciHardnodes}");
        }

        else if (SpsaUciOption.SpsaDict != null
            && SpsaUciOption.SpsaDict.ContainsKey(nameStr))
        {
            SpsaUciOption.ChangeField(nameStr, int.Parse(valueStr));
        }

        else
        {
            Console.WriteLine($"info string {nameStr} was not found, setoption unsuccessfull");
        }

    }
}
