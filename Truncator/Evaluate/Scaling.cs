
public static class Scaling
{
    public static unsafe int MaterialScaling(ref Pos p, int eval)
    {
        int nonPawnMaterial = Utils.popcnt(p.PieceBB[(int)PieceType.Knight]) * 450
                            + Utils.popcnt(p.PieceBB[(int)PieceType.Bishop]) * 450
                            + Utils.popcnt(p.PieceBB[(int)PieceType.Rook]) * 650
                            + Utils.popcnt(p.PieceBB[(int)PieceType.Queen]) * 1250;
        eval = eval * (26_500 + nonPawnMaterial) / 32_768;
        return Math.Clamp(eval, -Search.SCORE_EVAL_MAX, Search.SCORE_EVAL_MAX);
    }

}
