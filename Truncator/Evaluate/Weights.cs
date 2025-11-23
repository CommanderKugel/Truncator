using static Settings;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

public struct Weights : IDisposable
{
    public static Weights BigNetWeights = new(true);
    public static Weights SmallNetWeights = new(false);


    public unsafe short* l1_weight = null;
    public unsafe short* l1_bias = null;

    public unsafe short* l2_weight = null;
    public unsafe short* l2_bias = null;

    public bool bigNet;
    public int l1_size;

    public unsafe Weights(bool bigNet)
    {
        this.bigNet = bigNet;
        l1_size = bigNet ? L1_SIZE : L1_SIZE_SMOL;
    }

    public unsafe void Load()
    {
        // allocate the arrays

        l1_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * IN_SIZE * (nuint)l1_size, 256);
        l1_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * (nuint)l1_size, 256);
        l2_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * 2 * (nuint)l1_size * OUTPUT_BUCKETS, 256);
        l2_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * OUTPUT_BUCKETS, 256);

        // access embedded weights-file

        using var net = new BinaryReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Truncator.Evaluate.Nets.{(bigNet ? BIG_NET_NAME : SMOL_NET_NAME)}.bin")
            ?? throw new FileNotFoundException("info string Embedded NNUE weights not found!"));

        // read weights from file

        for (int feat = 0; feat < IN_SIZE; feat++)
            for (int node = 0; node < l1_size; node++)
                l1_weight[feat * l1_size + node] = net.ReadInt16();

        for (int node = 0; node < l1_size; node++)
            l1_bias[node] = net.ReadInt16();

        for (int buck=0; buck < OUTPUT_BUCKETS; buck++)
            for (int node = 0; node < l1_size * 2; node++)
                l2_weight[buck * l1_size * 2 + node] = net.ReadInt16();

        for (int buck = 0; buck < OUTPUT_BUCKETS; buck++)
            l2_bias[buck] = net.ReadInt16();

        Debug.WriteLine("info string Done loading net weights");
    }


    public unsafe void Dispose()
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
