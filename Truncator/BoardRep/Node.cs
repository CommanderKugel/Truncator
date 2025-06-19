
/// <summary>
/// This is an entry of the Search-stack
/// </summary>
public struct Node
{

    public PieceType MovedPieceType;
    public PieceType CapturedPieceType;
    public Move move;

    public Move KillerMove;
    public Move ExcludedMove;

    public unsafe PieceToHistory* ContHist;

}