public static class Scale
{
    
    public static unsafe void ComputeScale()
    {
        // last spsa tune on a net with this eval average on lichess big 3 resolved
        const int TARGET = 921;
        const string PATH = @"C:\Users\nikol\Desktop\Data\lichess-big3-resolved.book";

        if (!File.Exists(PATH))
        {
            Console.WriteLine($"info string abort - didnt find the lichess big 3 file");
            return;
        }

        using var fens = new StreamReader(PATH);

        long eval_sum = 0;
        long pos_count = 0;

        while (!fens.EndOfStream)
        {
            var fen = fens.ReadLine() ?? throw new Exception("no fen found :(");
            ThreadPool.MainThread.rootPos.SetNewFen(fen);

            int eval = NNUE.Evaluate(ref ThreadPool.MainThread.rootPos.p, ThreadPool.MainThread.nodeStack[0].acc);

            eval_sum += Math.Abs(eval);
            pos_count++;

            if (pos_count % 100_000 == 0)
            {
                Console.WriteLine($"{pos_count} : {eval_sum / pos_count} ({eval_sum}/{long.MaxValue})");
            }
        }

        int avrg = (int)(eval_sum / pos_count);
        Console.WriteLine(avrg);

        int new_scale = (int)(400.0d * (double)TARGET / (double)avrg);
        Console.WriteLine($"new scale: {new_scale}");
    }

}