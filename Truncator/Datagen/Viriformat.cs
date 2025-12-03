
using System.Diagnostics;
using System.Numerics;

/// <summary>
/// Virifirmat is a highly compressed binary format to store chess games
/// mostly, the games are used for NNUE training
/// a viriformat-game consists of a Marlinformat-headder that provides all information a fen would contain
/// the headder is followed by move-score pairs of how the main-line of the game was played out
/// the scores are the engines white relative search-scores 
/// to train an NNUE, the label of a position most commonly constists of an interpolation of the search score and game result
/// official viriformat specs: https://github.com/cosmobobak/viriformat
/// </summary>
public static class Viriformat
{

    private class ViriConverter
    {
        public int idx;
        public Thread systemThread;
        public SearchThread thread;
        public string[] pgns;
        
        public long[] dist = new long[30];
        public long gameCount = 0;
        public long posCount = 0;
        public long quietPosCount = 0;

        public string pgnPath;
        public string viriPath;

        public bool isFinished = false;


        public ViriConverter(
            SearchThread  thread_,
            string[] pgns_,
            string pgnPath_,
            int idx_
        )
        {
            systemThread = new Thread(ConvertMyPgns);

            thread = thread_;
            pgns = pgns_;
            pgnPath = pgnPath_;

            idx = idx_;
            var outFileName = $"converted-{idx}.viriformat";
            viriPath = Path.Combine(pgnPath, outFileName);
        }


        public void ConvertMyPgns()
        {
            for (int i = 0; i < pgns.Length; i++)
            {
                var pgn = pgns[i];
                var (gameCount, posCount) = ConvertPgnToViriformat(
                    thread,
                    Path.Combine(pgnPath, pgn),
                    viriPath,
                    dist
                );

                gameCount += gameCount;
                posCount += posCount;

                Console.WriteLine($"({idx} - {i}/{pgns.Length}) pos: {posCount} games: {gameCount}");
            }

            isFinished = true;
        }
    }


    public static void ConvertDirWithPgnsToViriformat(SearchThread thread, string PgnPath)
    {
        var files = Directory.GetFiles(PgnPath, "*.PGN");
        Console.WriteLine($"{files.Length} PGN files found");

        // create thread-objects for parallel processing of pgns

        int concurrency = ThreadPool.ThreadCount;
        int chunkSize = (int)Math.Ceiling((double)files.Length / (double)concurrency);
        var converters = new ViriConverter[concurrency];

        for (int i=0; i<concurrency; i++)
        {
            int start = i * chunkSize;
            int lengh = Math.Min(chunkSize, files.Length - start);
            int end = start + lengh;

            converters[i] = new ViriConverter(
                ThreadPool.pool[i],
                files[start .. end],
                PgnPath,
                i
            );
        }

        // convert

        foreach (var converter in converters)
            converter.systemThread.Start();

        foreach (var converter in converters)
            while (!converter.isFinished);

        Console.WriteLine("finieshed conversion...");

        // save game stats

        long totalGames = 0;
        long totalPos = 0;
        var dist = new long[30];

        foreach (var converter in converters)
        {
            totalGames += converter.gameCount;
            totalPos += converter.posCount;

            for (int i=0; i<dist.Length; i++)
                dist[i] += converter.dist[i];
        }

        // merge files

        var viriFiles = Directory.GetFiles(PgnPath, "*.viriformat");
        var outPath = Path.Combine(PgnPath, "converted.viriformat");
        Console.WriteLine($"concatenating {viriFiles.Length} files...");

        using var concat = new FileStream(outPath, FileMode.Create, FileAccess.Write);

        foreach (var viriFile in viriFiles)
        {
            using var f = new FileStream(viriFile, FileMode.Open, FileAccess.Read);
            concat.CopyTo(f);
        }

        Console.WriteLine("Done!");

        Console.WriteLine("{");
        for (int i = 0; i < dist.Length; i++)
            Console.WriteLine($"\"{i + 3}\": {dist[i]},");
        Console.WriteLine($"\"all fens\": {totalPos},");
        Console.WriteLine($"\"quiet fens\": {dist.Sum()},");
        Console.WriteLine($"\"games\": {totalGames}");
        Console.WriteLine("}");
    }

    public static (long, long) ConvertPgnToViriformat(SearchThread thread, string PgnPath, string ViriPath, long[] dist = null)
    {
        // read all games from the file
        // and instantly convert them to viriformat

        using var PgnReader = new StreamReader(PgnPath);
        using var ViriWriter = new BinaryWriter(new FileStream(ViriPath, FileMode.Append));

        long gameCount = 0;
        long posCount = 0;

        while (!PgnReader.EndOfStream)
        {
            var pgn = new Pgn(thread, PgnReader, dist);

            gameCount++;
            posCount += pgn.MainLine.Count;

            // Marlinformat Headder

            Marlinformat headder = new(thread, pgn);

            // main line as (Move, score) pair
            // surprisingly my move-struct is identical to viriformat moves

            var mainLineBuff = new byte[pgn.MainLine.Count * 4 + 4];

            int idx = 0;
            foreach (var ply in pgn.MainLine)
            {
                // save move and score in 4 bytes/pair

                mainLineBuff[idx + 0] = (byte)(ply.move.value);
                mainLineBuff[idx + 1] = (byte)(ply.move.value >> 8);
                mainLineBuff[idx + 2] = (byte)(ply.score);
                mainLineBuff[idx + 3] = (byte)(ply.score >> 8);

                idx += 4;
            }

            // terminator

            for (int i = 0; i < 4; i++)
            {
                mainLineBuff[idx + i] = 0;
            }

            // finally, write to viriformat file

            ViriWriter.Write(headder.ToBytes());
            ViriWriter.Write(mainLineBuff);
        }

        return (gameCount, posCount);
    }
}
