public static class SimpleEval
{

    public static int Evaluate(ref Pos p)
    {
        int eval = 100 * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Pawn  )) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Pawn  )))
                 + 300 * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Knight)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Knight)))
                 + 300 * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Bishop)) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Bishop)))
                 + 500 * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Rook  )) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Rook  )))
                 + 900 * (Utils.popcnt(p.GetPieces(Color.White, PieceType.Queen )) - Utils.popcnt(p.GetPieces(Color.Black, PieceType.Queen )));
        return p.Us == Color.White ? eval : -eval;
    }

}