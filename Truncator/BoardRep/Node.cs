
/// <summary>
/// This is an entry of the Search-stack
/// </summary>
public struct Node
{

    public PieceType MovedPieceType;
    public PieceType CapturedPieceType;
    public Move move;

    public int StaticEval;

    public Move KillerMove;
    public Move ExcludedMove;

}