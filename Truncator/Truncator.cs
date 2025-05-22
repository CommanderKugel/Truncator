
public static class Truncator
{
    public static void Main(string[] args)
    {
        try
        {
            Utils.InitUtils();
            Attacks.InitAttacks();

            if (args.Length == 0)
            {
                UCI.MainLoop();
            }

            if (args.Length == 1 && args[0] == "bench")
            {
                Bench.runBench();
            }

            if (args.Length == 1 && args[0] == "perft")
            {
                Perft.RunPerft();
            }
        }

        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        finally
        {
            Attacks.Dispose();
            Castling.Dispose();
            Utils.Dispose();
        }
    }
}
