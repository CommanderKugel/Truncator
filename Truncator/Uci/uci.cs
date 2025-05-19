using System.Diagnostics;

public static partial class UCI
{

    public static UciState state = UciState.Idle;
    public static SearchThread thread = new SearchThread(0);


    public static void MainLoop()
    {
        while (true)
        {
            string command = Console.ReadLine() ?? "quit";
            string[] tokens = command.Split(' ');

            if (command == "uci")
            {
                Console.WriteLine("uciok");
                Console.WriteLine("id name NamelessEngine");
                Console.WriteLine("id author CommanderKugel");
            }

            else if (command == "isready")
            {
                Console.WriteLine("readyok");
            }

            else if (tokens[0] == "setoption")
            {
                Debug.Assert(state == UciState.Idle, "do not use setoption when engine is not idle!");
                SetOption(tokens);
            }

            else if (tokens[0] == "ucinewgame")
            {
                UciRootPos.Clear();
            }

            else if (tokens[0] == "position")
            {
                Position(tokens);
            }

            else if (tokens[0] == "go")
            {
                Go(tokens);
            }

            else if (tokens[0] == "quit")
            {
                return;
            }

        }
    }

    private static IEnumerable<string> SkipPast(string[] tokens, string tok)
    {
        Debug.Assert(tokens.Length > 0);
        Debug.Assert(tok.Length > 0);
        return tokens.SkipWhile(t => t != tok).Skip(1);
    }

}