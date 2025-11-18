using static Settings;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

public static class Weights
{

    public static unsafe short* l0_weight = null;
    public static unsafe short* l0_bias = null;

    public static sbyte[,,] l1_weight = new sbyte[OUT_BUCKETS, L1_SIZE, L2_SIZE];
    public static float[,]  l1_bias   = new float[OUT_BUCKETS, L2_SIZE];

    public static float[,,] l2_weight = new float[OUT_BUCKETS, L2_SIZE, L3_SIZE];
    public static float[,]  l2_bias   = new float[OUT_BUCKETS, L3_SIZE];

    public static float[,]  l3_weight = new float[OUT_BUCKETS, L3_SIZE];
    public static float[]   l3_bias   = new float[OUT_BUCKETS];


    public static unsafe void Load()
    {
        // allocate the arrays

        l0_weight = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * IN_SIZE * L1_SIZE, 256);
        l0_bias = (short*)NativeMemory.AlignedAlloc((nuint)sizeof(short) * L1_SIZE, 256);

        // access embedded weights-file

        using var net = new BinaryReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Truncator.Evaluate.Nets.{NET_NAME}.bin")
            ?? throw new FileNotFoundException("info string Embedded NNUE weights not found!"));

        // read l0

        for (int feat = 0; feat < IN_SIZE; feat++)
            for (int node = 0; node < L1_SIZE; node++)
                l0_weight[feat * L1_SIZE + node] = net.ReadInt16();

        for (int node = 0; node < L1_SIZE; node++)
            l0_bias[node] = net.ReadInt16();

        // read l1

        for (int buck=0; buck < OUT_BUCKETS; buck++)
            for (int l2 = 0; l2 < L2_SIZE; l2++)
                for (int l1 = 0; l1 < L1_SIZE; l1++)
                    l1_weight[buck, l1, l2] = net.ReadSByte();

        for (int buck = 0; buck < OUT_BUCKETS; buck++)
            for (int l2 = 0; l2 < L2_SIZE; l2++)
                l1_bias[buck, l2] = net.ReadSingle();

        // read l2

        for (int buck=0; buck < OUT_BUCKETS; buck++)
            for (int l3 = 0; l3 < L3_SIZE; l3++)
                for (int l2 = 0; l2 < L2_SIZE; l2++)
                    l2_weight[buck, l2, l3] = net.ReadSingle();

        for (int buck = 0; buck < OUT_BUCKETS; buck++)
            for (int l3 = 0; l3 < L3_SIZE; l3++)
                l2_bias[buck, l3] = net.ReadSingle();

        // read l3

        for (int buck=0; buck < OUT_BUCKETS; buck++)
            for (int l3 = 0; l3 < L3_SIZE; l3++)
                l3_weight[buck, l3] = net.ReadSingle();

        for (int buck = 0; buck < OUT_BUCKETS; buck++)
            l3_bias[buck] = net.ReadSingle();

        Debug.WriteLine("info string Done loading net weights");

        try
        {
            while (true)
            {
                Debug.WriteLine(net.ReadChar());
            }
        }
        catch {}
    }


    public static unsafe void Dispose()
    {
        if (l0_weight != null)
        {
            NativeMemory.AlignedFree(l0_weight);
            NativeMemory.AlignedFree(l0_bias);

            l0_weight = l0_bias = null;
            Debug.WriteLine("Disposed of NNUE Weights");
        }
    }

}