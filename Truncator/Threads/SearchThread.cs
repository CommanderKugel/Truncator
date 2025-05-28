
using System.Diagnostics;

public class SearchThread : IDisposable
{
    private Thread myThread;
    private ManualResetEvent myResetEvent = new ManualResetEvent(false);

    public readonly int id;
    public bool IsMainThread => id == 0;

    public volatile bool doSearch = false;
    public volatile bool die = false;

    
    // search variables
    public volatile int ply;
    public long nodeCount = 0;
    public volatile int currIteration = 0;

    // search objects
    public PV pv_;
    public string GetPV => pv_.GetPV();
    public void NewPVLine() => pv_[ply, ply] = Move.NullMove;
    public void PushToPV(Move m) => pv_.Push(m, ply);

    public TranspositionTable tt = ThreadPool.tt;


    public SearchThread(int id)
    {
        this.id = id;
        pv_ = new();
        myThread = new Thread(ThreadMainLoop);
        myThread.Start();
    }

    private unsafe void ThreadMainLoop()
    {
        Console.WriteLine($"thread {id} started");

        //fixed (void* nullptr = &id)
        {

            try
            {
                while (!die)
                {
                    myResetEvent.WaitOne();
                    this.doSearch = true;

                    if (die)
                    {
                        break;
                    }
                    else
                    {
                        Search.IterativeDeepen(this);
                    }


                    // go back idle
                    myResetEvent.Reset();
                    if (IsMainThread)
                    {
                        UCI.state = UciState.Idle;
                    }
                    

                } // while (!die)
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception thrown in thread {id}!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                Dispose();
            }
            Console.WriteLine("thread shutting down - main loop escaped");

        } // fixed
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
        ply = 0;
        nodeCount = 0;
    }

    // between games
    public void Clear()
    {
        // tt
        // move/corr hist
        doSearch = true;
        nodeCount = 0;
        ply = 0;
        pv_.Clear();
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
        this.pv_.Dispose();
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

        var oldBench = 1004443;
        Console.WriteLine($"{oldBench} / {totalNodes} : {oldBench == totalNodes}");
    }

}