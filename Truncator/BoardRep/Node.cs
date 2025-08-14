
/// <summary>
/// This is an entry of the Search-stack
/// </summary>
public struct Node
{

    public PieceType MovedPieceType;
    public PieceType CapturedPieceType;
    public Move move;

    public int UncorrectedStaticEval;
    public int StaticEval;

    public Move KillerMove;
    public Move ExcludedMove;

    public unsafe PieceToHistory* ContHist;
    public int HistScore;
    public int ContHist1ply;
    public int ContHist2ply;

    public int CutoffCount;

}