
using System.Diagnostics;

public class SearchThread : IDisposable
{
    private Thread myThread;
    private ManualResetEvent myResetEvent = new ManualResetEvent(false);

    public readonly int id;
    public bool IsMainThread => id == 0;

    public volatile bool doSearch = false;
    public volatile bool die = false;


    public long nodeCount = 0;
    public int currIteration = 0;


    public SearchThread(int id)
    {
        this.id = id;
        myThread = new Thread(ThreadMainLoop);
        myThread.Start();
    }

    private void ThreadMainLoop()
    {
        Console.WriteLine("thread started");

        while (!die)
        {
            myResetEvent.WaitOne();
            this.doSearch = true;
            Console.WriteLine("thread woke up");

            if (die)
            {
                Console.WriteLine("thread dies after idle");
                break;
            }
            else
            {
                Console.WriteLine("thread works after idle");
                Search.IterativeDeepen(this);
            }


            // go back to idle
            myResetEvent.Reset();
        }

        Console.WriteLine("thread-loop broken");
    }

    public void Go()
    {
        myResetEvent.Set();
    }

    public void Stop()
    {
        this.doSearch = false;
    }

    // between searches
    public void Reset()
    {
        nodeCount = 0;
    }

    // between games
    public void Clear()
    {
        // tt
        // move/corr hist
        doSearch = true;
        nodeCount = 0;
    }

    public void Join()
    {
        this.die = true;
        Stop();
        myResetEvent.Set();
        Dispose();
        this.myThread.Join();
    }

    public void Dispose()
    {

    }

    public void RunBench()
    {
        var watch = new Stopwatch();
        long totalNodes = 0;

        watch.Start();
        foreach (var fen in Bench.Positions)
        {
            this.Clear();
            TimeManager.PrepareBench(TimeManager.maxDepth);

            UCI.rootPos.SetNewFen(fen);
            UCI.rootPos.InitRootMoves();

            Search.IterativeDeepen(this);
            totalNodes += nodeCount;
        }

        watch.Stop();
        Stop();

        long nps = totalNodes * 1000 / Math.Max(watch.ElapsedMilliseconds, 1);
        Console.WriteLine($"{totalNodes} nodes {nps} nps");
    }

}