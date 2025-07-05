
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 2)]
public struct HistVal
{
    public const short HIST_VAL_MAX = 1024;

    public short value;

    /// <summary>
    /// Apply history gravity formula, then clamp to min and max values
    /// </summary>
    public void Update(int delta) => value += (short)(delta - value * Math.Abs(delta) / HIST_VAL_MAX);

    public static implicit operator short(HistVal val) => val.value;
    public static implicit operator int(HistVal val) => val.value;

    public static implicit operator HistVal(short val) => new() { value = val };
    public static implicit operator HistVal(int val) => new() { value = (short)val };

}
