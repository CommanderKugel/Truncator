
using System.Diagnostics;

public static class BasicPsqt
{
    public static int Evaluate(ref Pos p)
    {
        int eval = 0;

        for (int us = 0; us < 2; us++)
        {
            Color them = (Color)(1 - us);

            for (int pt = (int)PieceType.Pawn; pt <= (int)PieceType.King; pt++)
            {
                ulong pieces = p.GetPieces((Color)us, (PieceType)pt);

                while (pieces != 0)
                {
                    int sq = Utils.popLsb(ref pieces) ^ (us == (int)Color.White ? 56 : 0);
                    Debug.Assert(sq >= 0 && sq < 64);

                    eval += (PieceType)pt switch
                    {
                        PieceType.Pawn => PawnTable_[sq],
                        PieceType.Knight => KnightTable_[sq],
                        PieceType.Bishop => BishopTable_[sq],
                        PieceType.Rook => RookTable_[sq],
                        PieceType.Queen => QueenTable_[sq],
                        PieceType.King => GetKingPsqtValue(ref p),
                        _ => 0,
                    };

                    unsafe int GetKingPsqtValue(ref Pos p)
                    {
                        bool hasQueen = p.GetPieces(them, PieceType.Queen) != 0;
                        bool notOnlyQueen = Utils.MoreThanOne(p.ColorBB[(int)them] ^ p.GetPieces(them, PieceType.King, PieceType.Queen));
                        return (hasQueen && notOnlyQueen) ? KingMiddlegameTable_[sq] : KingEndgameTable_[sq];
                    }

                } // piece
            } // pt
        } // color

        return eval;
    }
    

    private static ReadOnlySpan<int> PawnTable_ => [
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    ];

    private static ReadOnlySpan<int> KnightTable_ => [
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    ];

    private static ReadOnlySpan<int> BishopTable_ => [
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    ];

    private static ReadOnlySpan<int> RookTable_ => [
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0
    ];

    private static ReadOnlySpan<int> QueenTable_ => [
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    ];

    private static ReadOnlySpan<int> KingMiddlegameTable_ => [
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    ];

    private static ReadOnlySpan<int> KingEndgameTable_ => [
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    ];
}
