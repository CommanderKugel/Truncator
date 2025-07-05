
using System.Diagnostics;

public struct CorrectionHistory : IDisposable
{

    private const int SIZE = 16_384;

    private HistVal[,] PawnTable;


    public unsafe CorrectionHistory()
    {
        PawnTable = new HistVal[2, SIZE];
    }

    public unsafe int Correct(ref Pos p, Node* n)
    {
        int PawnVal = 16 * PawnTable[(int)p.Us, Zobrist.GetWholePieceKey(PieceType.Pawn, ref p) % SIZE];
        int CorrectionValue = PawnVal;

        CorrectionValue /= HistVal.HIST_VAL_MAX;
        n->StaticEval = Math.Clamp(n->UncorrectedStaticEval + CorrectionValue, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);

        return CorrectionValue;
    }

    private const int MAX_BONUS = HistVal.HIST_VAL_MAX / 4;
    private const int MIN_BONUS = -HistVal.HIST_VAL_MAX / 4;

    public unsafe void Update(ref Pos p, int score, int eval, int depth)
    {
        var delta = Math.Clamp((score - eval) * depth / 8, MIN_BONUS, MAX_BONUS);

        PawnTable[(int)p.Us, Zobrist.GetWholePieceKey(PieceType.Pawn, ref p) % SIZE].Update(delta);
    }


    public unsafe void Clear()
    {
        Debug.Assert(PawnTable != null);
        Array.Clear(PawnTable);
    }

    public unsafe void Dispose()
    {
        // make this future-proof
    }
}
