using static Settings;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class Weights
{

    public static unsafe short* l1_weight = null;
    public static unsafe short* l1_bias = null;

    public static unsafe short* l2_weight = null;
    public static unsafe short* l2_bias = null;


    public static unsafe void Load()
    {
        // allocate the arrays

        l1_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * IN_SIZE * L2_SIZE * INPUT_BUCKETS, 256);
        l1_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L2_SIZE, 256);
        l2_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * 2 * L2_SIZE * OUTPUT_BUCKETS, 256);
        l2_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * OUTPUT_BUCKETS, 256);

        // access embedded weights-file

        using var net = new BinaryReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Truncator.Evaluate.Nets.{NET_NAME}.bin")
            ?? throw new FileNotFoundException("info string Embedded NNUE weights not found!"));

        // read weights from file

        for (int buck = 0; buck < INPUT_BUCKETS; buck++)
            for (int feat = 0; feat < IN_SIZE; feat++)
                for (int node = 0; node < L2_SIZE; node++)
                    l1_weight[buck * IN_SIZE * L2_SIZE + feat * L2_SIZE + node] = net.ReadInt16();

        for (int node = 0; node < L2_SIZE; node++)
            l1_bias[node] = net.ReadInt16();

        for (int buck=0; buck < OUTPUT_BUCKETS; buck++)
            for (int node = 0; node < L2_SIZE * 2; node++)
                l2_weight[buck * L2_SIZE * 2 + node] = net.ReadInt16();

        for (int buck = 0; buck < OUTPUT_BUCKETS; buck++)
            l2_bias[buck] = net.ReadInt16();

        Debug.WriteLine("info string Done loading net weights");
    }


    public static unsafe void Dispose()
    {
        if (l1_weight != null)
        {
            NativeMemory.AlignedFree(l1_weight);
            NativeMemory.AlignedFree(l1_bias);
            NativeMemory.AlignedFree(l2_weight);
            NativeMemory.AlignedFree(l2_bias);

            l1_weight = l1_bias = l2_weight = l2_bias = null;
            Debug.WriteLine("Disposed of NNUE Weights");
        }
    }

}