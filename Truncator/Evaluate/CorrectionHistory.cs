
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CorrectionHistory : IDisposable
{

    public const int SIZE = 16_384;

    private const int MAX_BONUS = HistVal.HIST_VAL_MAX / 4;
    private const int MIN_BONUS = -HistVal.HIST_VAL_MAX / 4;

    private unsafe HistVal* PawnTable;


    public unsafe CorrectionHistory()
    {
        PawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
    }

    /// <summary>
    /// Alter Static Evaluation based on past Search results
    /// of positions with similar features
    /// </summary>
    public unsafe int Correct(ref Pos p, Node* n)
    {
        int PawnVal = 16 * PawnTable[p.PawnKey];
        int CorrectionValue = PawnVal;

        CorrectionValue /= HistVal.HIST_VAL_MAX;
        n->StaticEval = Math.Clamp(n->UncorrectedStaticEval + CorrectionValue, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);

        return CorrectionValue;
    }

    /// <summary>
    /// Update moving Average of difference between static-evaluation and search score
    /// based on some features of the position
    /// </summary>
    public unsafe void Update(ref Pos p, int score, int eval, int depth)
    {
        var delta = Math.Clamp((score - eval) * depth / 8, MIN_BONUS, MAX_BONUS);

        PawnTable[p.PawnKey].Update(delta);
    }


    public unsafe void Clear()
    {
        Debug.Assert(PawnTable != null);
        NativeMemory.Clear(PawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
    }

    public unsafe void Dispose()
    {
        if (PawnTable != null)
        {
            NativeMemory.Free(PawnTable);
            PawnTable = null;
        }
    }
}
