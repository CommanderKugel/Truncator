
// #define SPSA

using System.Diagnostics;

public static partial class UCI
{

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
                Console.WriteLine("id name Truncator 1.0");
                Console.WriteLine("id author CommanderKugel");

                Console.WriteLine($"option name Hash type spin default {TranspositionTable.DEFAULT_SIZE} min {TranspositionTable.MIN_SIZE} max {TranspositionTable.MAX_SIZE}");
                Console.WriteLine($"option name Threads type spin default 1 min 1 max {ThreadPool.MAX_THREAD_COUNT}");
                Console.WriteLine($"option name MultiPv type spin default 1 min 1 max 256");
                Console.WriteLine($"option name UCI_Chess960 type button default {Castling.UCI_Chess960}");
                Console.WriteLine($"option name UCI_ShowWDL type button default {WDL.UCI_showWDL}");
                Console.WriteLine($"option name UCI_NormaliseScore type button default {WDL.UCI_NormaliseScore}");

                Console.WriteLine($"option name Move Overhead type spin default {TimeManager.MoveOverhead} min 0 max 999999");

                Console.WriteLine($"option name SyzygyPath type string default <empty>");
                Console.WriteLine($"optino name SyzygyProbePly type spin default 40 min 1 max 256");
                Console.WriteLine($"optino name UCI_TbLargest type spin default 7 min 1 max 7");

                Console.WriteLine($"option name Softnodes type spin default {int.MaxValue} min {1} max {int.MaxValue}");
                Console.WriteLine($"option name Hardnodes type spin default {int.MaxValue} min {1} max {int.MaxValue}");

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
                ThreadPool.StopAll();
                return;
            }

            // ===== CUSTOM COMMANDS ===== //
            
            else if (command == "print")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
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
                Perft.RunPerft();
            }

            else if (tokens[0] == "pgntoviri")
            {
                Debug.Assert(state == UciState.Idle, "command only available, when engine is idle!");
                Debug.Assert(tokens.Length == 2);
                Viriformat.ConvertDirWithPgnsToViriformat(tokens[1], Fathom.DoTbProbing);
            }

            else if (tokens[0] == "pgncountmaterial")
            {
                Debug.Assert(state == UciState.Idle);
                Debug.Assert(tokens.Length == 2);
                CountMatDist.CountPgnsMaterialDistribution(ThreadPool.MainThread, tokens[1]);
            }

#if SPSA
            else if (command == "spsainput")
            {
                SpsaUciOption.PrintValuesInOBFormat();
            }
#endif

            else if (command == "eval")
            {
                foreach (var fen in new string[] {
                    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                    "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1",
                    "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
                    "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
                    "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1",

                    "r1b1k1nr/ppq3pp/2n1p3/2ppPp2/3P4/P1P1B1Q1/2P2PPP/R3KBNR b KQkq - 3 9",
                    "rn2kb1r/pp3ppp/2p1pn2/q7/3P4/2P2B1P/PP3PP1/RNBQ1RK1 b kq - 1 9"})
                {
                    unsafe
                    {
                        ThreadPool.MainThread.rootPos.SetNewFen(fen);
                        Accumulator.DoLazyUpdates(&ThreadPool.MainThread.nodeStack[0]);
                        int eval = NNUE.Evaluate(ref ThreadPool.MainThread.rootPos.p, ThreadPool.MainThread.nodeStack[0].acc);

                        Console.WriteLine($"FEN: {fen}");
                        Console.WriteLine($"EVAL: {eval}");
                    }
                }
            }
            
            else if (command == "scale")
            {
                Scale.ComputeScale();
            }

            else
            {
                Console.WriteLine($"info string Ignoring unknown command...");
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
