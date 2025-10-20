public static class CountMatDist
{

    public static void CountPgnsMaterialDistribution(SearchThread thread, string PgnPath)
    {
        var files = Directory.GetFiles(PgnPath, "*.PGN");
        Console.WriteLine($"{files.Length} PGN files found");

        var dist = new long[30];

        long gamesCount = 0;
        long posCount = 0;

        for (int i=0; i<files.Length; i++)
        {   
            Console.WriteLine($"({i+1}/{files.Length}): {files[i]}");
            using var PgnReader = new StreamReader(Path.Combine(PgnPath, files[i]));

            while (!PgnReader.EndOfStream)
            {
                var pgn = new Pgn(thread, PgnReader, dist);

                gamesCount++;
                posCount += pgn.MainLine.Count();
            }
        }

        Console.WriteLine("Done!\nDistribution:");

        for (int i = 0; i < dist.Length; i++)
        {
            Console.WriteLine($"{i}: {dist[i]}");
        }
    }

}