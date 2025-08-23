
using System.Diagnostics;
using System.Security.Cryptography;

public class Pgn
{
    public string Fen;
    public string Result;
    public List<PgnMove> MainLine;

    public Pgn(SearchThread thread, StreamReader file)
    {
        MainLine = [];
        string? line;

        while (!file.EndOfStream && (line = file.ReadLine()) != null)
        {

            // [FEN "<fen>"]
            if (line.StartsWith("[FEN"))
            {
                Fen = line[6..(line.Length - 2)];
                thread.rootPos.SetNewFen(thread, Fen);
                Console.WriteLine("FEN: " + Fen);
            }

            // [Result "1-0"]
            else if (line.StartsWith("[Result"))
            {
                Result = line[9..(line.Length - 2)];
                Console.WriteLine("Result: " + Result);
            }

            else if (!line.StartsWith('[') && line != "")
            {
                var plies = line.Split('}');

                foreach (var ply in plies)
                {
                    // stop when game ended

                    if (ply == " " || ply == string.Empty)
                    {
                        break;
                    }

                    // remove leading empty chars
                    // all moves except the first have a leading char... 

                    var info = ply.Trim().Split(' ');

                    // parse for move and comment data

                    Move m = new(thread, ref thread.rootPos.p, info[0]);

                    string s = info[1][1..];
                    int score = s.Contains('M') ? 32_000 : (int)float.Parse(s);

                    int slashIdx = info[2].IndexOf('/');
                    int depth = int.Parse(info[2][..slashIdx]);
                    int seldepth = int.Parse(info[2][(slashIdx + 1)..]);

                    long time = long.Parse(info[3]);
                    long nodes = long.Parse(info[4]);

                    // flip search scores when black made a move

                    if (thread.rootPos.p.Us == Color.Black)
                    {
                        score = -score;
                    }

                    // save the move-data

                    PgnMove pm = new(m, score, depth, seldepth, time, nodes);
                    MainLine.Add(pm);

                    // make the move

                    thread.rootPos.MakeMove(m, thread);
                }

                break;
            }
        }
    }


    /// <summary>
    /// Replays the game for the given color
    /// searches are restricted by hardnodes to use deterministic behaviour
    /// </summary>
    public void Replay(Color c)
    {
        ThreadPool.Clear();
        TimeManager.Reset();

        var thread = ThreadPool.MainThread;
        thread.rootPos.SetNewFen(thread, Fen);

        foreach (var pm in MainLine)
        {
            if (thread.rootPos.p.Us != c)
            {
                thread.rootPos.MakeMove(pm.move, thread);
                continue;
            }

            thread.Reset();
            thread.doSearch = true;
            thread.rootPos.RootMoves.Clear();
            thread.rootPos.InitRootMoves(thread);
            
            TimeManager.Reset();
            TimeManager.softnodes = 5000;
            TimeManager.hardnodes = pm.nodes;
            TimeManager.Start(thread.rootPos.p.Us);

            if (pm.move == new Move((int)Square.F2, (int)Square.B6))
            {
                
            }

            Search.IterativeDeepen(thread, false);
            if (pm.move == new Move((int)Square.F2, (int)Square.B6))
            {
                return;
            }

            thread.rootPos.MakeMove(pm.move, thread);
        }
    }

}
