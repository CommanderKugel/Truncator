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
                Console.WriteLine("id name Truncator 0.43");
                Console.WriteLine("id author CommanderKugel");
                
                Console.WriteLine($"option name Hash type spin default {TranspositionTable.DEFAULT_SIZE} min {TranspositionTable.MIN_SIZE} max {TranspositionTable.MAX_SIZE}");
                Console.WriteLine($"option name Threads type spin default 1 min 1 max {ThreadPool.MAX_THREAD_COUNT}");
                Console.WriteLine($"option name UCI_ShowWDL type check default false");

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
                ThreadPool.StopAll();
                ThreadPool.Join();
                return;
            }

            // ===== CUSTOM COMMANDS ===== //

            // print the current board to the commandline
            else if (command == "print")
            {
                Utils.print(ThreadPool.MainThread.rootPos.p);
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

                if (tokens.Length == 1)
                {
                    Perft.RunPerft();
                }

                else if (tokens.Length == 3)
                {
                    try
                    {
                        Perft.SplitPerft(tokens[1], int.Parse(tokens[2]));
                    }
                    catch
                    {
                        Console.WriteLine("An error occured when executing 'perft <fen> <depth>' command!");
                    }
                }
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