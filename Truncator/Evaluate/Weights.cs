using static Settings;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class Weights
{

    public static unsafe short* l0_weight = null;
    public static unsafe short* l0_bias = null;

    public static unsafe short* l1_weight = null;
    public static unsafe short* l1_bias = null;


    public static unsafe void Load()
    {
        // allocate the arrays

        l0_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * IN_SIZE * L1_SIZE, 256);
        l0_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);
        l1_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * 2 * L1_SIZE * OUTPUT_BUCKETS, 256);
        l1_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * OUTPUT_BUCKETS, 256);

        // access embedded weights-file

        using var net = new BinaryReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Truncator.Evaluate.Nets.{NET_NAME}.bin")
            ?? throw new FileNotFoundException("info string Embedded NNUE weights not found!"));

        // read weights from file

        for (int feat = 0; feat < IN_SIZE; feat++)
            for (int node = 0; node < L1_SIZE; node++)
                l0_weight[feat * L1_SIZE + node] = net.ReadInt16();

        for (int node = 0; node < L1_SIZE; node++)
            l0_bias[node] = net.ReadInt16();

        for (int buck=0; buck < OUTPUT_BUCKETS; buck++)
            for (int node = 0; node < L1_SIZE * 2; node++)
                l1_weight[buck * L1_SIZE * 2 + node] = net.ReadInt16();

        for (int buck = 0; buck < OUTPUT_BUCKETS; buck++)
            l1_bias[buck] = net.ReadInt16();

        Debug.WriteLine("info string Done loading net weights");
    }


    public static unsafe void Dispose()
    {
        if (l0_weight != null)
        {
            NativeMemory.AlignedFree(l0_weight);
            NativeMemory.AlignedFree(l0_bias);
            NativeMemory.AlignedFree(l1_weight);
            NativeMemory.AlignedFree(l1_bias);

            l0_weight = l0_bias = l1_weight = l1_bias = null;
            Debug.WriteLine("Disposed of NNUE Weights");
        }
    }

}