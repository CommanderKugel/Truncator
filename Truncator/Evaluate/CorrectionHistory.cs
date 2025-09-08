
using System.Diagnostics;
using System.Runtime.InteropServices;

using static Tunables;

public struct CorrectionHistory : IDisposable
{
    /// <summary>
    /// Size of a table per each color
    /// </summary>
    public const int SIZE = 16_384;

    private const int MAX_BONUS = 256;
    private const int MIN_BONUS = -256;

    private unsafe HistVal* PawnTable;
    private unsafe HistVal* WhiteNonPawnTable;
    private unsafe HistVal* BlackNonPawnTable;
    private unsafe HistVal* MinorTable;
    private unsafe HistVal* MajorTable;
    private unsafe HistVal* ThreatTable;

    public PieceToHistory MoveTable;


    public unsafe CorrectionHistory()
    {
        PawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        WhiteNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        BlackNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MinorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MajorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        ThreatTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);

        MoveTable = new();
    }

    private ulong MakeKey(Color c, ulong key)
    {
        Debug.Assert(c != Color.NONE);
        return (ulong)c * SIZE + key % SIZE;
    }

    /// <summary>
    /// Correct Static Evaluation based on past Search results
    /// of positions with similar features
    /// </summary>
    public unsafe int Correct(SearchThread thread, ref Pos p, Node* n)
    {
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null);
        Debug.Assert(MinorTable != null && MajorTable != null && ThreatTable != null);

        int pawn = PawnCorrhistWeight * PawnTable[MakeKey(p.Us, p.PawnKey)];
        int npwhite = NpCorrhistWeight * WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))];
        int npblack = NpCorrhistWeight * BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))];
        int minor = MinorCorrhistWeight * MinorTable[MakeKey(p.Us, p.MinorKey)];
        int major = MajorCorrhistWeight * MajorTable[MakeKey(p.Us, p.MajorKey)];
        int threat = ThreatCorrhistWeight * ThreatTable[MakeKey(p.Us, Utils.murmurHash(p.Threats & p.ColorBB[(int)p.Us]))];

        int prevPiece = (n - 1)->move.NotNull ?
            PrevPieceCorrhistWeight * MoveTable[p.Us, (n - 1)->MovedPieceType, (n - 1)->move.to] : 0;

        int CorrectionValue = (pawn + npwhite + npblack + minor + major + threat + prevPiece) / CorrhistFinalDiv;
        n->StaticEval = Math.Clamp(n->UncorrectedStaticEval + CorrectionValue, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);

        return CorrectionValue;
    }

    /// <summary>
    /// Update moving Average of difference between static-evaluation and search score
    /// based on some features of the position
    /// </summary>
    public unsafe void Update(SearchThread thread, ref Pos p, Node* n, int score, int eval, int depth)
    {
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null && MinorTable != null);
        var delta = Math.Clamp((score - eval) * depth * CorrhistDelta / 1024, MIN_BONUS, MAX_BONUS);

        PawnTable[MakeKey(p.Us, p.PawnKey)].Update(delta, PawnCorrhistDiv);
        WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))].Update(delta, NpCorrhistDiv);
        BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))].Update(delta, NpCorrhistDiv);
        MinorTable[MakeKey(p.Us, p.MinorKey)].Update(delta, MinorCorrhistDiv);
        MajorTable[MakeKey(p.Us, p.MajorKey)].Update(delta, MajorCorrhistDiv);

        ThreatTable[MakeKey(p.Us, Utils.murmurHash(p.Threats & p.ColorBB[(int)p.Us]))].Update(delta, ThreatCorrhistDiv);

        if (thread.ply > 0 && (n - 1)->move.NotNull)
        {
            MoveTable[p.Us, (n - 1)->MovedPieceType, (n - 1)->move.to].Update(delta, PrevPieceCorrhistDiv);
        }
    }

    /// <summary>
    /// Fills all tables with zero
    /// </summary>
    public unsafe void Clear()
    {
        Debug.Assert(PawnTable != null && WhiteNonPawnTable != null && BlackNonPawnTable != null && MinorTable != null);
        NativeMemory.Clear(PawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(WhiteNonPawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(BlackNonPawnTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(MinorTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(MajorTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(ThreatTable, (nuint)sizeof(HistVal) * SIZE * 2);
        MoveTable.Clear();
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
            NativeMemory.Free(ThreatTable);
            PawnTable = WhiteNonPawnTable = BlackNonPawnTable = null;
            MinorTable = MajorTable = ThreatTable = null;
        }

        MoveTable.Dispose();
    }
}
