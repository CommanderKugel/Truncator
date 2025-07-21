
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CorrectionHistory : IDisposable
{

    public const int SIZE = 16_384;

    private const int MAX_BONUS = HistVal.HIST_VAL_MAX / 4;
    private const int MIN_BONUS = -HistVal.HIST_VAL_MAX / 4;

    // MinorTable failed +1 elo stc (yellow :[) 
    // MajorTable failed -3 elo stc 
    private unsafe HistVal* PawnTable;
    private unsafe HistVal* WhiteNonPawnTable;
    private unsafe HistVal* BlackNonPawnTable;
    private unsafe HistVal* MinorTable;
    private unsafe HistVal* MajorTable;


    public unsafe CorrectionHistory()
    {
        PawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        WhiteNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        BlackNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MinorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MajorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
    }

    private ulong MakeKey(Color c, ulong key) => (ulong)c * SIZE + key % SIZE;

    /// <summary>
    /// Alter Static Evaluation based on past Search results
    /// of positions with similar features
    /// </summary>
    public unsafe int Correct(ref Pos p, Node* n)
    {
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null && MinorTable != null && MajorTable != null);

        int CorrectionValue = 0;
        CorrectionValue += 16 * PawnTable[MakeKey(p.Us, p.PawnKey)];
        CorrectionValue += 12 * WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))];
        CorrectionValue += 12 * BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))];
        CorrectionValue += 12 * MinorTable[MakeKey(p.Us, p.MinorKey)];
        CorrectionValue += 12 * MajorTable[MakeKey(p.Us, p.MajorKey)];
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
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null && MinorTable != null && MajorTable != null);
        var delta = Math.Clamp((score - eval) * depth / 8, MIN_BONUS, MAX_BONUS);

        PawnTable[MakeKey(p.Us, p.PawnKey)].Update(delta);
        WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))].Update(delta);
        BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))].Update(delta);
        MinorTable[MakeKey(p.Us, p.MinorKey)].Update(delta);
        MajorTable[MakeKey(p.Us, p.MajorKey)].Update(delta);
    }

    /// <summary>
    /// Fills all tables with zero
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null && MinorTable != null && MajorTable != null);
        NativeMemory.Clear(PawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(WhiteNonPawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(BlackNonPawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(MinorTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(MajorTable, (nuint)sizeof(HistVal) * SIZE * 2);
    }

    /// <summary>
    /// Frees all allocated Memory. Always free memory before destroying a Corrhist instance
    /// </summary>
    public unsafe void Dispose()
    {
        if (PawnTable != null)
        {
            NativeMemory.Free(PawnTable);
            NativeMemory.Free(WhiteNonPawnTable);
            NativeMemory.Free(BlackNonPawnTable);
            NativeMemory.Free(MinorTable);
            NativeMemory.Free(MajorTable);
            PawnTable = WhiteNonPawnTable = BlackNonPawnTable =
            MinorTable = MajorTable = null;
        }
    }
}
