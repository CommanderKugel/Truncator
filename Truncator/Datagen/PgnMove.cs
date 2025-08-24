
public struct PgnMove
{

    public Move move;
    public int score;

    public int depth;
    public int seldepth;

    public long time;
    public long nodes;

    public PgnMove(Move m, int s, int d, int sd, long t, long n)
    {
        move = m;
        score = s;
        depth = d;
        seldepth = sd;
        time = t;
        nodes = n;
    }

    public override string ToString() => move.ToString() + " {" + $"{score} {depth}/{seldepth} {time}ms {nodes}" + '}';

}
