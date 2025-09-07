
//#define SPSA

using System.Diagnostics;

public static partial class UCI
{
    public static bool IsChess960 = false;

    public static UciState state = UciState.Idle;


    public static void MainLoop()
    {
        while (true)
        {
            string command = Console.ReadLine() ?? "quit";
            string[] tokens = command.Split(' ');

            // initial command that signals the engine to use the uci-protocol
            // Truncator only implements uci, so uci initialization is basically skipped

            if (command == "uci")
            {
                Console.WriteLine("id name Truncator 0.79");
                Console.WriteLine("id author CommanderKugel");

                Console.WriteLine($"option name Hash type spin default {TranspositionTable.DEFAULT_SIZE} min {TranspositionTable.MIN_SIZE} max {TranspositionTable.MAX_SIZE}");
                Console.WriteLine($"option name Threads type spin default 1 min 1 max {ThreadPool.MAX_THREAD_COUNT}");
                Console.WriteLine($"option name UCI_ShowWDL type check default false");

                Console.WriteLine($"option name SyzygyPath type string default <empty>");
                //Console.WriteLine($"option name SyzygyProbePly type spin default 40 min 1 max 128");

                Console.WriteLine($"option name Softnodes type spin default {int.MaxValue - 1} min {1} max {int.MaxValue - 1}");
                Console.WriteLine($"option name Hardnodes type spin default {int.MaxValue - 1} min {1} max {int.MaxValue - 1}");

#if SPSA
                SpsaUciOption.CollectOptions();
                SpsaUciOption.PrintOptionsToUCI();
#endif

                Console.WriteLine("uciok");
            }

            // the 'are you still alive?' check from the match runner. will be answered between 
            // every uci command.

            else if (command == "isready")
            {
                Console.WriteLine("readyok");
            }

            else if (command == "stop")
            {
                Debug.Assert(state == UciState.Searching, "command only available, when engine is searching!");
                ThreadPool.StopAll();
                state = UciState.Idle;
            }

            // options can be set to different values. most important are
            // - size (mb) of the transposition table
            // - number of threads used for search

            else if (tokens[0] == "setoption")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                SetOption(tokens);
            }

            // signal to reset all heuristics that save data between moves

            else if (tokens[0] == "ucinewgame")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                ThreadPool.Clear();
            }

            // initialization of the position to start searching from.
            // comes with a list of moves most of the time

            else if (tokens[0] == "position")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                Position(tokens);
            }

            // command to start searching. comes with subcommands most of the time.

            else if (tokens[0] == "go")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                Go(tokens);
            }

            // signal to close the programm. dont forget to stop all the threads!

            else if (tokens[0] == "quit")
            {
                return;
            }

            // ===== CUSTOM COMMANDS ===== //
            
            else if (command == "print")
            {
                Utils.print(ThreadPool.MainThread.rootPos.p);
            }

            else if (tokens[0] == "move")
            {
                Debug.Assert(tokens.Length >= 2, "forgot to write the move?");
                string mvstr = tokens[1];
                Move m = new(ThreadPool.MainThread, ref ThreadPool.MainThread.rootPos.p, mvstr);
                Console.WriteLine(m);
                Console.WriteLine($"castling - {m.IsCastling}");
                Console.WriteLine($"ep       - {m.IsEnPassant}");
                Console.WriteLine($"promo    - {m.IsPromotion} ({m.PromoType})");
            }

            else if (tokens[0] == "bench")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                int depth = tokens.Length == 2 ? int.Parse(tokens[1]) : Bench.BenchDepth;
                Bench.runBench(depth);
            }

            else if (command == "perft")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                Perft.RunPerft();
            }

            else if (tokens[0] == "pgn")
            {
                Debug.Assert(tokens.Length == 3);
                var c = tokens[1] == "white" ? Color.White : tokens[1] == "black" ? Color.Black :
                    throw new Exception($"could not read color '{tokens[1]}', expected 'white' or 'black'");
                var pgn = new Pgn(ThreadPool.MainThread, new(tokens[2]));
                pgn.Replay(c);
            }

            else if (command == "spsainput")
            {
                SpsaUciOption.PrintValuesInOBFormat();
            }

        }
    }

    private static IEnumerable<string> SkipPast(string[] tokens, string tok)
    {
        Debug.Assert(tokens.Length > 0);
        Debug.Assert(tok.Length > 0);
        return tokens.SkipWhile(t => t != tok).Skip(1);
    }

}
