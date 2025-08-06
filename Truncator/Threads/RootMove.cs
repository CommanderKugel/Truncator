public struct RootMove
{
    public Move Move;
    public int Score;
    public long Nodes;

    public RootMove(Move move, int score, long nodes)
    {
        Move = move;
        Score = score;
        Nodes = nodes;
    }
}