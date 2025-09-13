using System.Diagnostics;

public struct UpdateData
{
    public Color c;
    public PieceType pt;
    public int sq;
    public readonly bool HasData = false;

    public UpdateData(Color c, PieceType pt, int sq)
    {
        Debug.Assert(c != Color.NONE);
        Debug.Assert(pt != PieceType.NONE);
        Debug.Assert(sq >= 0 && sq < 64);
        this.c = c;
        this.pt = pt;
        this.sq = sq;
        HasData = true;
    }
}
