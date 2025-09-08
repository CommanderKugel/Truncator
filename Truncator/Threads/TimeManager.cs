
using System.Diagnostics;

public static class TimeManager
{
    public static int wtime, btime, winc, binc;
    public static int movestogo, movetime;
    public static int depth;

    public static long UciSoftnodes = -1;
    public static long UciHardnodes = -1;
    public static long softnodes, hardnodes;

    private static long HardTimeout = 0;
    private static long SoftTimeout = 0;
    private static bool IsSelfManaging = true;
    public  static int  maxDepth = 128;

    public static int MoveOverhead = 10;


    private static Stopwatch watch = new Stopwatch();

    public static void Reset()
    {
        watch.Restart();

        wtime = int.MaxValue;
        btime = int.MaxValue;

        winc = -1;
        binc = -1;

        movestogo = -1;
        movetime = -1;

        depth = -1;
        
        softnodes = long.MaxValue;
        hardnodes = long.MaxValue;
    }

    public static void Start(Color Us)
    {

        if (UciSoftnodes != -1 || UciHardnodes != -1)
        {
            Debug.Assert(UciSoftnodes != -1 && UciHardnodes != -1);
            softnodes = UciSoftnodes;
            hardnodes = UciHardnodes;
            Console.WriteLine($"înfo string set softnodes={softnodes} and hardnodes={hardnodes} via UCI options");
            return;
        }

        IsSelfManaging = softnodes == long.MaxValue
            && hardnodes == long.MaxValue
            && depth == -1;

        maxDepth = 128;

        // #1 we do not manage time ourselves
        if (!IsSelfManaging)
        {

            // #1.1 searching should be completed after n nodes
            // this is very helpfull for recreating bugs and datageneration
            if (softnodes != long.MaxValue)
            {
                HardTimeout = int.MaxValue;
                SoftTimeout = int.MaxValue;

                // assume soft and hardnodes have been set proberly already
                // for "go nodes 5000", softnodes is assumed
                // for hardnodes, "go hardnodes 5000" is required to be sent

                if (hardnodes == long.MaxValue)
                {
                    hardnodes = softnodes * 20; // go nodes 5000 -> implicit 100k hardnodes
                }

                Debug.WriteLine($"nodes: softnodes = {softnodes}, hardnodes = {hardnodes}");
            }

            // #1.2 search for N ID iterations
            // mostly used for bench
            else if (depth != -1)
            {
                HardTimeout = int.MaxValue;
                SoftTimeout = int.MaxValue;
                maxDepth = depth;
                Debug.WriteLine($"depth: max-depth = {depth}");
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
                Debug.WriteLine($"movetime: hard- & softlimit = {HardTimeout}");
            }

            // #2.2 Play N moves in M time + o per move, then get time bonus for next N moves
            else if (movestogo != -1)
            {
                HardTimeout = time / Math.Min(movestogo, 2) + inc / 2;
                SoftTimeout = time / movestogo + inc / 2;
                Debug.WriteLine($"movestogo: mtg = {movestogo}, hardlimit = {HardTimeout}, softlimit = {SoftTimeout}");
            }

            // #2.3 Play whole game in M time
            else
            {
                HardTimeout = time / 5 + inc / 2;
                SoftTimeout = time / 30 + inc / 2;
                Debug.WriteLine($"normal: hardlimit = {HardTimeout}, softlimit = {SoftTimeout}");
            }
        }
    }

    public static void PrepareBench(int depth_)
    {
        watch.Restart();

        wtime = int.MaxValue;
        btime = int.MaxValue;

        winc = binc = -1;
        movestogo = -1;
        movetime = -1;

        maxDepth = depth_;
        depth = depth_;

        softnodes = long.MaxValue;
        hardnodes = long.MaxValue;

        IsSelfManaging = false;
    }

    public static bool IsHardTimeout(SearchThread thread)
    {
        Debug.Assert(!IsSelfManaging || HardTimeout != 0);
        return IsSelfManaging && watch.ElapsedMilliseconds > HardTimeout
            || thread.nodeCount >= hardnodes;
    }

    public static bool IsSoftTimeout(SearchThread thread, int iteration)
    {
        Debug.Assert(!IsSelfManaging || SoftTimeout != 0 && iteration > 0);

        // pv-tm
        // node-tm
        // score-tm

        return IsSelfManaging && watch.ElapsedMilliseconds > SoftTimeout
            || thread.nodeCount >= softnodes;
    }

    public static long ElapsedMilliseconds => Math.Max(watch.ElapsedMilliseconds, 1);
}
