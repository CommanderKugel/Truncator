
using System.Diagnostics;

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

    public static void ConvertDirWithPgnsToViriformat(SearchThread thread, string PgnPath, bool tbCorrect = false)
    {
        tbCorrect = false;
        Debug.Assert(!tbCorrect || tbCorrect && Fathom.DoTbProbing, "forgot to initialize Syzygy?");
        Console.WriteLine($"use tb correction: {tbCorrect && Fathom.DoTbProbing}");

        var files = Directory.GetFiles(PgnPath, "*.PGN");
        Console.WriteLine($"{files.Length} PGN files found");

        string outFileName = (tbCorrect && Fathom.DoTbProbing ? "convertd_tbcorr" : "converted") + ".viriformat";
        var outPath = Path.Combine(PgnPath, outFileName);

        if (!File.Exists(outPath))
        {
            var stream = new FileStream(outPath, FileMode.Create);
            stream.Close();
            Console.WriteLine("created new viriformat-file");
        }
        else
        {
            Console.WriteLine("found existing viriformat-file");
        }

        Console.WriteLine($"viriformat file can be found as '{outPath}'");

        long totalGames = 0;
        long totalPos = 0;
        long tbCorrections = 0;
        var dist = new long[30];

        for (int i=0; i<files.Length; i++)
        {
            var file = files[i];
            Console.WriteLine($"({i+1}/{files.Length}): {file}");

            var (gameCount, posCount, tbCorrCount) = ConvertPgnToViriformat(
                thread,
                Path.Combine(PgnPath, file),
                outPath,
                dist,
                tbCorrect
            );

            totalGames += gameCount;
            totalPos += posCount;
            tbCorrections += tbCorrCount;

            Console.WriteLine($"parsed {gameCount} games, {posCount} poitions, to total {totalGames} games and total {totalPos} positions");
        }

        Console.WriteLine("Done!");

        Console.WriteLine("{");
        for (int i = 0; i < dist.Length; i++)
            Console.WriteLine($"\"{i + 3}\": {dist[i]},");
        Console.WriteLine($"\"all fens\": {totalPos},");
        Console.WriteLine($"\"quiet fens\": {dist.Sum()},");
        Console.WriteLine($"\"games\": {totalGames}");
        Console.WriteLine($"\"TbCorrections\": {tbCorrections}");
        Console.WriteLine("}");
    }

    public static (long, long, long) ConvertPgnToViriformat(SearchThread thread, string PgnPath, string ViriPath, long[] dist = null, bool TbCorrect = false)
    {
        // read all games from the file
        // and instantly convert them to viriformat

        using var PgnReader = new StreamReader(PgnPath);
        using var ViriWriter = new BinaryWriter(new FileStream(ViriPath, FileMode.Append));

        long gameCount = 0;
        long posCount = 0;
        long tbCorrCount = 0;

        while (!PgnReader.EndOfStream)
        {
            var pgn = new Pgn(thread, PgnReader, dist, TbCorrect: TbCorrect);

            gameCount++;
            posCount += pgn.MainLine.Count;

            // Marlinformat Headder

            Marlinformat headder = new(thread, pgn);

            if (pgn.FirstTbResult != -1 && pgn.ResultToInt() != pgn.FirstTbResult)
            {
                tbCorrCount++;
            }

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

        return (gameCount, posCount, tbCorrCount);
    }
}
