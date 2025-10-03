
using System.Diagnostics;

public static partial class UCI
{
    private static void SetOption(string[] tokens)
    {
        Debug.Assert(tokens[0] == "setoption");

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

        else if (nameStr == "MultiPv")
        {
            Debug.Assert(tokens.Length == 5);
            ThreadPool.UCI_MultiPVCount = Math.Clamp(int.Parse(valueStr), 1, 256);
            ThreadPool.UpdateMultiPv();
            Console.WriteLine($"info string set MultiPv={ThreadPool.UCI_MultiPVCount}");
        }
        
        else if (nameStr == "UCI_Chess960")
        {
            if (tokens.Length == 5)
            {
                Castling.UCI_Chess960 = bool.Parse(valueStr);
            }

            else if (tokens.Length == 3)
            {
                Castling.UCI_Chess960 = !Castling.UCI_Chess960;
            }

            Console.WriteLine($"info string set UCI_Chess960={Castling.UCI_Chess960}");
        }

        else if (nameStr == "Move" && tokens[3] == "Overhead")
        {
            Debug.Assert(tokens.Length == 6);
            TimeManager.MoveOverhead = Math.Clamp(int.Parse(tokens[5]), 0, 999999);
            Console.WriteLine($"info string set Move Overhead={TimeManager.MoveOverhead}");
        }

        /*
        // disabled for now
        else if (nameStr == "UCI_ShowWDL" && tokens.Length >= 5)
        {
            WDL.UCI_showWDL = valueStr == "true";
            Console.WriteLine($"info string set UCI_ShowWDL to {WDL.UCI_showWDL}");
        }
        */

        else if (nameStr == "SyzygyPath" && tokens.Length >= 5)
        {
            Debug.Assert(tokens.Length == 5);
            var path = valueStr;
            Fathom.Init(path);
        }

        else if (nameStr == "SyzygyProbePly")
        {
            Debug.Assert(tokens.Length == 5);
            Fathom.SyzygyProbePly = int.Parse(valueStr);
            Console.WriteLine($"info string set SyzygyProbePly={Fathom.SyzygyProbePly}");
        }

        else if (nameStr == "UCI_TbLargest")
        {
            Debug.Assert(tokens.Length == 5);
            Fathom.UCI_TbLargest = Math.Clamp(int.Parse(valueStr), 1, 7);
            Console.WriteLine($"info string set UCI_TbLargest={Fathom.UCI_TbLargest}");
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
