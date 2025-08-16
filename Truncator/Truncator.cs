

using System.Diagnostics;

public static class Truncator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Truncator Chess Engine");

        try
        {

            if (args.Length == 0)
            {
                Console.WriteLine("no args given, starting UCI protocol");
                UCI.MainLoop();
            }

            else if (args.Length == 1 && args[0] == "bench")
            {
                Bench.runBench(Bench.BenchDepth);
            }

            else if (args.Length == 1 && args[0] == "perft")
            {
                Perft.RunPerft();
            }

            else
            {
                Console.WriteLine($"unknown args, shutting down...");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Truncator crashed with an Exception! Shutting down now!");
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
            Debug.WriteLine("\n\t1) Disposing Fathom Dll and deleting File");
            Fathom.Dispose();
            BindingHandler.Dispose();

            Debug.WriteLine("\n\t2) Stopping and Disposing of the Threadpool");
            ThreadPool.StopAll();
            ThreadPool.Join();

            Debug.WriteLine("\n\t3) Disposing off miscellaeous stuff");
            Attacks.Dispose();
            Castling.Dispose();
            Utils.Dispose();
            Zobrist.Dispose();

            Debug.WriteLine("\n\t4) Disposing of all Threads");
            ThreadPool.StopAll();
            ThreadPool.Join();
        }
    }
}
