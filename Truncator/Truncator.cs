

public static class Truncator
{
    public static void Main(string[] args)
    {
#if !DEBUG
        try
#endif
        {

            if (args.Length == 0)
            {
                UCI.MainLoop();
            }

            if (args.Length == 1 && args[0] == "bench")
            {
                Bench.runBench(Bench.BenchDepth);
            }

            if (args.Length == 1 && args[0] == "perft")
            {
                Perft.RunPerft();
            }
        }

#if !DEBUG
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
        finally
#endif

        {
            Attacks.Dispose();
            Castling.Dispose();
            Utils.Dispose();
            Zobrist.Dispose();
            GC.Collect();

            FathomDll.Dispose();
        }
    }
}
