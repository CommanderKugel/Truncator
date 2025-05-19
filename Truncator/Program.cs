Utils.InitUtils();
Attacks.InitAttacks();


try
{

    while (true)
    {
        string command = Console.ReadLine() ?? "quit";

        if (command == "quit")
        {
            break;
        }
        else if (command == "uci")
        {
            Console.WriteLine("uciok");
            Console.WriteLine("id name NamelessEngine");
            Console.WriteLine("id author CommanderKugel");
            UCI.MainLoop();
        }
        else if (command == "bench")
        {
            Bench.runBench();
        }
        else if (command == "perft")
        {
            Perft.RunPerft();
        }
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
