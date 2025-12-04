
using System.Diagnostics;

public class Pgn
{
    public string Fen;
    public string Result;

    public List<PgnMove> MainLine;

    public Pgn(SearchThread thread, StreamReader file, long[] dist = null, int distMinPly = 16)
    {
        MainLine = [];
        string? line;
        int emptyCount = 0;

        List<string> lines = [];

        while (!file.EndOfStream && (line = file.ReadLine()) != null)
        {
            lines.Add(line);

            // [FEN "<fen>"]

            if (line.StartsWith("[FEN"))
            {
                Fen = line[6..(line.Length - 2)];
                thread.rootPos.SetNewFen(Fen);
            }

            // [Result "1-0"]

            else if (line.StartsWith("[Result"))
            {
                Result = line[9..(line.Length - 2)];
            }

            // skip crashes/timeouts/unterminated games

            else if (line.EndsWith("stalled connection\"]")
                || line.EndsWith("unterminated\"]")
                || line.EndsWith("time forfeit\"]"))
            {
                // skip until next game in file, then parse anew

                while (!string.IsNullOrWhiteSpace(line) || ++emptyCount < 2)
                {
                    line = file.ReadLine();
                    Debug.WriteLine("skipping: " + line);
                }

                // now start reading next game

                emptyCount = 0;
                continue;
            }

            else if (!line.StartsWith('[') && line != "")
            {
                var plies = line.Split('}');

                foreach (var ply in plies)
                {
                    // stop when game ended

                    if (ply == " " || ply == string.Empty || ply == null)
                    {
                        break;
                    }

                    // remove leading empty chars
                    // all moves except the first have a leading char... 

                    var info = ply.Trim().Split(' ');

                    if (info.Length == 0 || info.Length == 1)
                    {
                        continue;
                    }

                    // remove full move counter

                    if (MainLine.Count == 0 || thread.rootPos.p.Us == Color.White)
                    {
                        info = info[1..];
                    }

                    // parse for move and comment data

                    bool mate = info[0].EndsWith('#');
                    Move m = ParseSAN.ParseSANMove(thread, ref thread.rootPos.p, info[0]);

                    string s = info[1][1..];
                    int score = s.Contains('M') ? 32_000 : (int)float.Parse(s);

                    int slashIdx = info[2].IndexOf('/');
                    int depth = int.Parse(info[2][..slashIdx]);
                    int seldepth = int.Parse(info[2][(slashIdx + 1)..]);

                    long time = long.Parse(info[3]);
                    string n = info[4].EndsWith(',') ? info[4][..^1] : info[4];
                    long nodes = long.Parse(n);

                    // flip search scores when black made a move

                    if (thread.rootPos.p.Us == Color.Black)
                    {
                        score = -score;
                    }

                    // count material distribution if wanted

                    unsafe
                    {
                        if (dist is not null
                            && Math.Abs(score) < 10_000
                            && MainLine.Count >= distMinPly
                            && !thread.rootPos.p.IsCapture(m)
                            && !m.IsCastling
                            && !m.IsPromotion
                            && thread.rootPos.p.Checkers == 0)
                        {
                            dist[Utils.popcnt(thread.rootPos.p.blocker) - 3]++;
                        }
                    }

                    // save the move-data

                    PgnMove pm = new(m, score, depth, seldepth, time, nodes);
                    MainLine.Add(pm);

                    // make the move

                    thread.rootPos.MakeMove(m);
                }
            }

            else if (string.IsNullOrWhiteSpace(line) && ++emptyCount >= 2)
            {
                break;
            }
        }

        Debug.Assert(Fen is not null);
        Debug.Assert(Result is not null);
    }
    

    public int ResultToInt()
    {
        Debug.Assert(Result is not null);
        return Result switch
        {
            "1-0" => 2,
            "0-1" => 0,
            "1/2-1/2" => 1,
            _ => throw new ArgumentException($"invalid gameresult '{Result}'"),
        };
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
        thread.rootPos.SetNewFen(Fen);
        Utils.print(thread.rootPos.p);

        foreach (var pm in MainLine)
        {
            if (thread.rootPos.p.Us != c)
            {
                thread.rootPos.MakeMove(pm.move);
                continue;
            }

            thread.Reset();
            thread.doSearch = true;
            thread.rootPos.RootMoves.Clear();
            thread.rootPos.InitRootMoves();

            TimeManager.Reset();
            TimeManager.softnodes = 5000;
            TimeManager.hardnodes = pm.nodes;
            TimeManager.Start(thread.rootPos.p.Us);

            Search.IterativeDeepen(thread, false);

            thread.rootPos.MakeMove(pm.move);
        }
    }

}
