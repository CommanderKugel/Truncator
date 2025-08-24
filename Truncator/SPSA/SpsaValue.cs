public struct SpsaValue
{

    public readonly string Name;

    public int Value;

    public readonly int Default;
    public readonly int Min;
    public readonly int Max;

    public readonly double C_end;
    public readonly double R_end;

    public SpsaValue(string name, int val, int min, int max, double? C_end = null, double? R_end = null)
    {
        Name = name;
        Value = val;

        Default = val;
        Min = min;
        Max = max;

        this.C_end = C_end ?? Math.Max(Max / 20, 0.5);
        this.R_end = R_end ?? 0.002;
    }

    public static implicit operator int(SpsaValue v) => v.Value;

    public readonly override string ToString()
        => $"option name {Name} type spin default {Value} min {Min} max {Max}";

    /// <summary>
    /// 1. Name
    /// 2. Type
    /// 3. (current) Value
    /// 4. Min Value
    /// 5. Max Value
    /// 6. (C_end) Step Size
    /// 7. (R_enc) Learning Rate
    /// </summary>
    public static string ToOBFormat(SpsaValue v)
        => $"{v.Name}, int, {v.Value}, {v.Min}, {v.Max}, {v.C_end}, {v.R_end}";

}
