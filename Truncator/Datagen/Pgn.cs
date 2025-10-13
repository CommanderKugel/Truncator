
using System.Diagnostics;

public class Pgn
{
    public string Fen;
    public string Result;
    public int FirstTbResult = -1;
    public List<PgnMove> MainLine;

    public Pgn(SearchThread thread, StreamReader file, long[] dist = null, bool TbCorrect = false)
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

            // skip crashed games
            else if (line == "[Termination \"stalled connection\"]")
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

                    if (thread.rootPos.p.Us == Color.White)
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
                            && MainLine.Count > 8
                            && !thread.rootPos.p.IsCapture(m)
                            && !m.IsCastling
                            && !m.IsPromotion
                            && thread.rootPos.p.Checkers == 0)
                        {
                            dist[Utils.popcnt(thread.rootPos.p.blocker) - 3]++;
                        }
                    }

                    // use the tb-result to correct the game-outcome in case of non optimal play

                    if (TbCorrect
                        && Utils.popcnt(thread.rootPos.p.blocker) <= Fathom.TbLargest
                        && thread.rootPos.p.Checkers == 0
                        && thread.rootPos.p.CastlingRights == 0
                        && thread.rootPos.p.EnPassantSquare == (int)Square.NONE)
                    {
                        thread.rootPos.p.FiftyMoveRule = 0;
                        int res = Fathom.ProbeWdl(ref thread.rootPos.p);

                        if (thread.rootPos.p.Us == Color.Black)
                        {
                            res = 4 - res;
                        }

                        // save the first valid tb-result for later use

                        if (FirstTbResult == -1)
                        {
                            FirstTbResult = res switch
                            {
                                (int)TbResult.TbWin => 2,
                                (int)TbResult.TbCursedWin => 2,
                                (int)TbResult.TbDraw => 1,
                                (int)TbResult.TbBlessedLoss => 0,
                                (int)TbResult.TbLoss => 0,
                                _ => throw null,
                            };
                        }

                        // skip any positions that disagree with the first received tb-result

                        else if (res != FirstTbResult)
                        {
                            // skip until end of game

                            while (!string.IsNullOrWhiteSpace(line))
                            {
                                line = file.ReadLine();
                            }

                            return;
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
        Debug.Assert(MainLine.Count > 0);
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
