﻿

using System.Diagnostics;

public static class Truncator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Truncator Chess Engine");

        try
        {
            ThreadPool.Resize(1);

            if (args.Length == 0)
            {
                Debug.WriteLine("no args given, starting UCI protocol");
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

            else if (args[0].StartsWith("genfens"))
            {
                GenFens.Generate(args[0].Split(' '));
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
            Debug.WriteLine("\n\t1) Disposing Fathom Dll and trying to delete File");
            Fathom.Dispose();
            BindingHandler.Dispose();

            Debug.WriteLine("\n\t2) Disposing off miscellaeous stuff");
            Attacks.Dispose();
            Debug.WriteLine("attacks done");
            Utils.Dispose();
            Debug.WriteLine("utils done");
            Zobrist.Dispose();
            Debug.WriteLine("zobrist done");

            Debug.WriteLine("\n\t3) Stopping and Disposing of the Threadpool");
            ThreadPool.Join();
        }
    }
}
