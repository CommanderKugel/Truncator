using System.Diagnostics;

public static partial class UCI
{
    public static bool IsChess960 = false;

    public static UciState state = UciState.Idle;
    public static SearchThread thread = new SearchThread(0);


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
                Console.WriteLine("uciok");
                Console.WriteLine("id name NamelessEngine");
                Console.WriteLine("id author CommanderKugel");
            }

            // the 'are you still alive?' check from the match runner. will be answered between 
            // every uci command.
            else if (command == "isready")
            {
                Console.WriteLine("readyok");
            }

            // options can be set to different values. most importantly are
            // - size (mb) of the transposition table
            // - number of threads used for search
            else if (tokens[0] == "setoption")
            {
                if (state != UciState.Idle)
                {
                    Console.WriteLine("Only use setoption when Truncator is idle!");
                    continue;
                }
                SetOption(tokens);
            }

            // signal to reset all heuristics that save data between moves
            else if (tokens[0] == "ucinewgame")
            {
                UciRootPos.Clear();
            }

            // initialization of the position to start searching from.
            // comes with a list of moves most of the time
            else if (tokens[0] == "position")
            {
                Position(tokens);
            }

            // command to start searching. comes with subcommands most of the time.
            else if (tokens[0] == "go")
            {
                Go(tokens);
            }

            // signal to close the programm. dont forget to stop all the threads!
            else if (tokens[0] == "quit")
            {
                thread.Join();
                return;
            }

            // ===== CUSTOM COMMANDS ===== //

            // print the current board to the commandline
            else if (command == "print")
            {
                Utils.print(UciRootPos.p);
            }

            else if (command == "bench")
            {
                Bench.runBench();
            }

            else if (command == "perft")
            {
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