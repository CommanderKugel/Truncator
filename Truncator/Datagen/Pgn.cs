
using System.Diagnostics;

public class Pgn
{
    public string Fen;
    public string Result;
    public string tbResult;

    public List<PgnMove> MainLine;

    public Pgn(SearchThread thread, StreamReader file, long[] dist = null, int distMinPly = 16, bool tbCorrect = false)
    {
        MainLine = [];
        string? line;
        int emptyCount = 0;

        List<string> lines = [];

        while (!file.EndOfStream && (line = file.ReadLine()) != null && emptyCount < 2)
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

                skip_to_end();
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
                        ref var p = ref thread.rootPos.p;

                        if (dist is not null
                            && Math.Abs(score) < 10_000
                            && MainLine.Count >= distMinPly
                            && !p.IsCapture(m)
                            && !m.IsCastling
                            && !m.IsPromotion
                            && p.Checkers == 0)
                        {
                            dist[Utils.popcnt(thread.rootPos.p.blocker) - 3]++;
                        }


                        if (tbCorrect
                            && Fathom.DoTbProbing
                            && p.CastlingRights == 0
                            && p.FiftyMoveRule == 0
                            && Utils.popcnt(p.blocker) <= Fathom.TbLargest)
                        {
                            var res = (TbResult)Fathom.ProbeWdl(ref p);
                            Debug.Assert(res >= TbResult.TbLoss && res <= TbResult.TbWin);

                            var gameResult = res == TbResult.TbWin ? (p.Us == Color.White ? "1-0" : "0-1")
                                : res == TbResult.TbLoss ? (p.Us == Color.White ? "0-1" : "1-0")
                                : "1/2-1/2";

                            // if there is already a tb result and a player threw the result
                            // ignore the rest of the game, its result is false

                            if (tbResult is not null && gameResult != tbResult)
                            {
                                while (!string.IsNullOrWhiteSpace(line))
                                {
                                    line = file.ReadLine();
                                }
                                emptyCount = 2;
                                break;
                            }

                            // otherwise keep going and save the potentially first result

                            tbResult = gameResult;
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

        // overwrite the gamresult with the correct tb result

        if (tbResult is not null && tbResult != Result)
        {
            Result = tbResult;
            tbResult = "corrected";
        }

        Debug.Assert(Fen is not null);
        Debug.Assert(Result is not null);

        void skip_to_end()
        {
            while (!string.IsNullOrWhiteSpace(line) || ++emptyCount < 2)
            {
                line = file.ReadLine();
                Debug.WriteLine("skipping: " + line);
            }
        }
    }
    

    public int ResultToInt()
    {
        Debug.Assert(Result is not null);
        return Result switch
        {
            "1-0"     => 2,
            "1/2-1/2" => 1,
            "0-1"     => 0,
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
