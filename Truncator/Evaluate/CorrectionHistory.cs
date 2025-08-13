
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct CorrectionHistory : IDisposable
{
    /// <summary>
    /// Size of a table per each color
    /// </summary>
    public const int SIZE = 16_384;

    private const int MAX_BONUS = HistVal.HIST_VAL_MAX / 4;
    private const int MIN_BONUS = -HistVal.HIST_VAL_MAX / 4;

    private unsafe HistVal* PawnTable;
    private unsafe HistVal* WhiteNonPawnTable;
    private unsafe HistVal* BlackNonPawnTable;
    private unsafe HistVal* MinorTable;
    private unsafe HistVal* MajorTable;
    private unsafe HistVal* WhiteThreatTable;
    private unsafe HistVal* BlackThreatTable;
    private unsafe HistVal* WhiteKingThreatTable;
    private unsafe HistVal* BlackKingThreatTable;

    public PieceToHistory MoveTable;


    public unsafe CorrectionHistory()
    {
        PawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        WhiteNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        BlackNonPawnTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MinorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        MajorTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);

        WhiteThreatTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        BlackThreatTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        WhiteKingThreatTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);
        BlackKingThreatTable = (HistVal*)NativeMemory.AllocZeroed((nuint)sizeof(HistVal) * SIZE * 2);

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
        Debug.Assert(PawnTable != null);
        Debug.Assert(WhiteNonPawnTable != null && BlackNonPawnTable != null);
        Debug.Assert(MinorTable != null && MajorTable != null);
        Debug.Assert(WhiteThreatTable != null && BlackThreatTable != null);

        int pawn = 16 * PawnTable[MakeKey(p.Us, p.PawnKey)];
        int npwhite = 12 * WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))];
        int npblack = 12 * BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))];
        int minor = 12 * MinorTable[MakeKey(p.Us, p.MinorKey)];
        int major = 12 * MajorTable[MakeKey(p.Us, p.MajorKey)];

        var our_ttable = p.Us == Color.White ? WhiteThreatTable : BlackThreatTable;
        var their_ttable = p.Us == Color.Black ? WhiteThreatTable : BlackThreatTable;
        int threatus = 12 * our_ttable[MakeKey(p.Us, Utils.murmurHash(p.Threats[(int)p.Them] & p.ColorBB[(int)p.Us]))];
        int threatthem = 12 * their_ttable[MakeKey(p.Us, Utils.murmurHash(p.Threats[(int)p.Us] & p.ColorBB[(int)p.Them]))];

        var our_kttable = p.Us == Color.White ? WhiteKingThreatTable : BlackKingThreatTable;
        var their_kttable = p.Us == Color.Black ? WhiteKingThreatTable : BlackKingThreatTable;
        int kthreatus = 12 * our_kttable[MakeKey(p.Us, Utils.murmurHash(p.KingZoneAttacker[(int)p.Us]))];
        int kthreattem = 12 * their_kttable[MakeKey(p.Us, Utils.murmurHash(p.KingZoneAttacker[(int)p.Them]))];

        int prevPiece = (thread.ply > 0 && (n - 1)->move.NotNull) ?
            12 * MoveTable[p.Us, (n - 1)->MovedPieceType, (n - 1)->move.to] : 0;
        
        int CorrectionValue = (pawn + npwhite + npblack + minor + major + threatus + threatthem + prevPiece + kthreatus + kthreattem) / HistVal.HIST_VAL_MAX;
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
        var delta = Math.Clamp((score - eval) * depth / 8, MIN_BONUS, MAX_BONUS);

        PawnTable[MakeKey(p.Us, p.PawnKey)].Update(delta);
        WhiteNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.White))].Update(delta);
        BlackNonPawnTable[MakeKey(p.Us, p.NonPawnMaterialKey(Color.Black))].Update(delta);
        MinorTable[MakeKey(p.Us, p.MinorKey)].Update(delta);
        MajorTable[MakeKey(p.Us, p.MajorKey)].Update(delta);

        WhiteThreatTable[MakeKey(p.Us, Utils.murmurHash(p.Threats[(int)Color.Black] & p.ColorBB[(int)Color.White]))].Update(delta);
        BlackThreatTable[MakeKey(p.Us, Utils.murmurHash(p.Threats[(int)Color.White] & p.ColorBB[(int)Color.Black]))].Update(delta);

        WhiteKingThreatTable[MakeKey(p.Us, Utils.murmurHash(p.KingZoneAttacker[(int)Color.White]))].Update(delta);
        BlackKingThreatTable[MakeKey(p.Us, Utils.murmurHash(p.KingZoneAttacker[(int)Color.Black]))].Update(delta);

        if (thread.ply > 0 && (n - 1)->move.NotNull)
        {
            MoveTable[p.Us, (n - 1)->MovedPieceType, (n - 1)->move.to].Update(delta);
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
        NativeMemory.Clear(WhiteThreatTable, (nuint)sizeof(HistVal) * SIZE * 2);
        NativeMemory.Clear(BlackThreatTable, (nuint)sizeof(HistVal) * SIZE * 2);
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
            NativeMemory.Free(WhiteThreatTable);
            NativeMemory.Free(BlackThreatTable);
            PawnTable = WhiteNonPawnTable = BlackNonPawnTable = null;
            MinorTable = MajorTable = WhiteThreatTable = BlackThreatTable = null;
        }

        MoveTable.Dispose();
    }
}
