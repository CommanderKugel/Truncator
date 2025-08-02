
using System.Diagnostics;

public static class TimeManager
{
    public static int wtime, btime, winc, binc;
    public static int movestogo, movetime;
    public static int depth;
    public static long softnodes, hardnodes;

    private static long HardTimeout = 0;
    private static long SoftTimeout = 0;
    private static bool IsSelfManaging = true;
    public  static int  maxDepth = 128;
    public  static long maxNodes = long.MaxValue;

    public static int MoveOverhead = 10;


    private static Stopwatch watch = new Stopwatch();

    public static void Reset()
    {
        watch.Restart();
        wtime = btime = int.MaxValue;
        winc = binc = -1;
        movestogo = -1;
        movetime = -1;
        depth = -1;
        softnodes = hardnodes = long.MaxValue;

        PvStability = 0;
    }

    public static void Start(Color Us)
    {
        IsSelfManaging = hardnodes == long.MaxValue
                      && softnodes == long.MaxValue
                      && depth == -1;

        maxDepth = 128;

        // #1 we do not manage time ourselves
        if (!IsSelfManaging)
        {

            // #1.1 searching should be completed after n nodes
            // this is very helpfull for recreating bugs and datageneration
            if (hardnodes != long.MaxValue)
            {
                HardTimeout = int.MaxValue;
                SoftTimeout = int.MaxValue;
                maxNodes = softnodes = hardnodes;
                Console.WriteLine($"nodes: max-nodes = {hardnodes}");
            }

            // #1.2 search for N ID iterations
            // mostly used for bench
            else if (depth != -1)
            {
                HardTimeout = int.MaxValue;
                SoftTimeout = int.MaxValue;
                maxDepth = depth;
                Console.WriteLine($"depth: max-depth = {depth}");
            }

            else
            {
                throw new NotImplementedException("Did not recognize time control!");
            }
        }

        // #2 we manage time ourselves
        else
        {
            int time = Math.Max((Us == Color.White ? wtime : btime) - MoveOverhead, 1);
            int inc = Math.Max(Us == Color.White ? winc : binc, 0);

            // #2.1 searching should take exactly the given time
            //      technically this is not self-managing, but we need to enable the 
            //      soft- and hard-timeout checks
            if (movetime != -1)
            {
                HardTimeout = movetime;
                SoftTimeout = movetime;
                Console.WriteLine($"movetime: hard- & softlimit = {HardTimeout}");
            }

            // #2.2 Play N moves in M time + o per move, then get time bonus for next N moves
            else if (movestogo != -1)
            {
                HardTimeout = time / Math.Min(movestogo, 2) + inc / 2;
                SoftTimeout = time / movestogo + inc / 2;
                Console.WriteLine($"movestogo: mtg = {movestogo}, hardlimit = {HardTimeout}, softlimit = {SoftTimeout}");
            }

            // #2.3 Play whole game in M time
            else
            {
                HardTimeout = time / 5 + inc / 2;
                SoftTimeout = time / 30 + inc / 2;
                Console.WriteLine($"normal: hardlimit = {HardTimeout}, softlimit = {SoftTimeout}");
            }
        }
    }

    public static void PrepareBench(int depth_)
    {
        watch.Restart();

        wtime = btime = int.MaxValue;
        winc = binc = -1;
        movestogo = -1;
        movetime = -1;
        maxDepth = depth = depth_;
        softnodes = hardnodes = long.MaxValue;

        IsSelfManaging = false;
    }

    public static bool IsHardTimeout(SearchThread thread)
    {
        Debug.Assert(!IsSelfManaging || HardTimeout != 0);
        return IsSelfManaging && watch.ElapsedMilliseconds > HardTimeout || thread.nodeCount >= hardnodes;
    }


    public static int PvStability = 0;

    public static unsafe bool IsSoftTimeout(SearchThread thread, int iteration)
    {
        Debug.Assert(!IsSelfManaging || SoftTimeout != 0 && iteration > 0);

        // sometimes we dont even want to check for a soft-timeout
        if (iteration < 4 || !IsSelfManaging)
        {
            return false;
        }

        // when playing with nodes rather than time
        if (thread.nodeCount >= softnodes)
        {
            return true;
        }

        // pv stability
        // spend less time, if we agree over manyiterations
        // what the best move might be

        ref var PV = ref thread.PV;
        PvStability = PV.moves[iteration] == PV.moves[iteration - 1] ? Math.Min(PvStability + 1, 10) : 0;
        double PvStabilityFactor = 1.20 - 0.04 * PvStability;

        // pv-tm
        // node-tm
        // score-tm

        return watch.ElapsedMilliseconds > SoftTimeout * PvStabilityFactor;
    }

    public static long ElapsedMilliseconds => Math.Max(watch.ElapsedMilliseconds, 1);
}
