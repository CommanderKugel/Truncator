public static class CountMatDist
{

    public static void CountPgnsMaterialDistribution(SearchThread thread, string PgnPath, int distMinPly = 16)
    {
        var files = Directory.GetFiles(PgnPath, "*.PGN");
        Console.WriteLine($"{files.Length} PGN files found");

        MaterialDistribution dist = new();

        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"({i + 1}/{files.Length}): {files[i]}");
            using var PgnReader = new StreamReader(Path.Combine(PgnPath, files[i]));

            while (!PgnReader.EndOfStream)
            {
                var pgn = new Pgn(thread, PgnReader, dist: dist.dist, distMinPly: distMinPly);
                dist.gameCount++;
                dist.posCount += pgn.MainLine.Count;
            }
        }

        Console.WriteLine("Done!\nDistribution:");
        Console.WriteLine(dist.ToString());
        var distPath = Path.Combine(PgnPath, "dist.JSON");
        Console.WriteLine($"distribution can be found at: {distPath}");

        if (File.Exists(distPath))
        {
            File.Delete(distPath);
            Console.WriteLine($"Overwrotes existing dist-file");
        }

        using var f = new StreamWriter(distPath);
        f.Write(dist.ToString());
    }
}

public class MaterialDistribution
{
    public long[] dist = new long[30];
    public long gameCount;
    public long posCount;
    public long quietPosCount => dist.Sum();

    public override string ToString()
    {
        string s = "{\n";
        for (int i = 0; i < 30; i++)
            s += $"\"{i + 3}\": {dist[i]},\n";
        s += $"\"all fens\": {posCount},\n";
        s += $"\"quiet fens\": {dist.Sum()},\n";
        s += $"\"games\": {gameCount}\n";
        return s + "}";
    }

    public static MaterialDistribution operator +(MaterialDistribution x, MaterialDistribution y)
    {
        var ret = new MaterialDistribution()
        {
            gameCount = x.gameCount + y.gameCount,
            posCount = x.posCount + y.posCount,
        };

        for (int i = 0; i < 30; i++)
        {
            ret.dist[i] = x.dist[i] + y.dist[i];
        }

        return ret;
    } 
}
