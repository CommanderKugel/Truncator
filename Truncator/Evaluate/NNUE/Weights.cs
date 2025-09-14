
using System.Diagnostics;
using System.Reflection;
using static Params;

public static class Weights
{

    public static short[,] l1_weight = new short[IN_SIZE, L1_SIZE];

    public static short[] l1_bias = new short[L1_SIZE];

    public static short[] l2_weight = new short[2 * L1_SIZE];

    public static short l2_bias;



    public static unsafe void Load()
    {
        string name = "pesto_virifmt_16hl_1.0wdl-50";

        Stream? stream;
        if ((stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            $"Truncator.Evaluate.Nets.{name}.bin"
        )) == null)
        {
            throw new Exception($"Could not find embedded {name}");
        }

        using BinaryReader net = new(stream);

        for (int feat = 0; feat < IN_SIZE; feat++)
            for (int node = 0; node < L1_SIZE; node++)
                l1_weight[feat, node] = net.ReadInt16();

        for (int node = 0; node < L1_SIZE; node++)
            l1_bias[node] = net.ReadInt16();

        for (int node = 0; node < 2 * L1_SIZE; node++)
            l2_weight[node] = net.ReadInt16();

        l2_bias = net.ReadInt16();

        Debug.WriteLine("info string NNUE weights loaded");
    }


    public static unsafe void Dispose()
    {

    }

}