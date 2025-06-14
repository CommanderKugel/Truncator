
public struct HistVal
{
    public const short HIST_MAX_VAL = 16_000;
    public const short HIST_MIN_VAL = -16_000;

    public short value;

    /// <summary>
    /// Apply history gravity formula, then clamp to min and max values
    /// </summary>
    public static short operator <<(HistVal histVal, int delta)
    {
        histVal.value += (short)(delta - histVal.value * Math.Abs(delta) / 1024);
        return histVal.value = Math.Clamp(histVal.value, HIST_MIN_VAL, HIST_MAX_VAL);
    }

    public static implicit operator short(HistVal val) => val.value;
    public static implicit operator int(HistVal val) => val.value;

    public static implicit operator HistVal(short val) => new() { value = val };
    public static implicit operator HistVal(int val) => new() { value = (short)val };

}
