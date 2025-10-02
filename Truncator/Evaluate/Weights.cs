using static Settings;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class Weights
{

    public static unsafe float* l1_weight = null;
    public static unsafe float* l1_bias = null;

    public static unsafe float* l2_weight = null;
    public static unsafe float l2_bias = 0;


    public static unsafe void Load()
    {
        // allocate the arrays

        l1_weight = (float*)NativeMemory.Alloc((nuint)sizeof(float) * IN_SIZE * L2_SIZE);
        l1_bias = (float*)NativeMemory.Alloc((nuint)sizeof(float) * L2_SIZE);
        l2_weight = (float*)NativeMemory.Alloc((nuint)sizeof(float) * 2 * L2_SIZE);

        // access embedded weights-file

        using var net = new BinaryReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Truncator.Evaluate.Nets.{NET_NAME}.bin")
            ?? throw new FileNotFoundException("info string Embedded NNUE weights not found!"));

        // read weights from file

        for (int feat = 0; feat < IN_SIZE; feat++)
            for (int node = 0; node < L2_SIZE; node++)
                l1_weight[feat * L2_SIZE + node] = net.ReadSingle();

        for (int node = 0; node < L2_SIZE; node++)
            l1_bias[node] = net.ReadSingle();

        for (int node = 0; node < L2_SIZE * 2; node++)
            l2_weight[node] = net.ReadSingle();

        l2_bias = net.ReadSingle();

        Debug.WriteLine("info string Done loading net weights");
    }


    public static unsafe void Dispose()
    {
        if (l1_weight != null)
        {
            NativeMemory.Free(l1_weight);
            NativeMemory.Free(l1_bias);
            NativeMemory.Free(l2_weight);
            l2_bias = 0;

            l1_weight = l1_bias = l2_weight = null;
            Debug.WriteLine("Disposed of NNUE Weights");
        }
    }

}