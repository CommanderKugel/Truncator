
using System.Runtime.InteropServices;

/// <summary>
/// This is an entry of the Search-stack
/// </summary>
public struct Node
{
    public Pos p;

    public PieceType MovedPieceType;
    public PieceType CapturedPieceType;
    public Move move;

    public bool InCheck;

    public int UncorrectedStaticEval;
    public int StaticEval;

    public Move KillerMove;
    public Move ExcludedMove;

    public unsafe PieceToHistory* ContHist;
    public int HistScore;

    public int Reductions;
    public int CutoffCount;

    public Accumulator acc;

    public unsafe void Clear()
    {
        fixed (Node* ptr = &this)
        {
            NativeMemory.Clear(ptr, (nuint)sizeof(Node) - (nuint)sizeof(Accumulator));
        }
    }

}