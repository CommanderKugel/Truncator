
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SearchThread : IDisposable
{
    private Thread myThread;
    private ManualResetEvent myResetEvent = new ManualResetEvent(false);

    // thread Managing Variables
    public readonly int id;
    public bool IsMainThread => id == 0;

    public volatile bool doSearch = false;
    public volatile bool die = false;


    // search variables
    public RootPos rootPos;

    public volatile int ply;
    public volatile int seldepth;
    public long nodeCount = 0;

    public int completedDepth = 0;

    // search objects
    public PV pv_;
    public string GetPV => pv_.GetPV();
    public int RootScore => pv_[completedDepth];
    public void NewPVLine() => pv_[ply, ply] = Move.NullMove;
    public void PushToPV(Move m) => pv_.Push(m, ply);

    public TranspositionTable tt = ThreadPool.tt;
    public RepetitionTable repTable;
    public volatile unsafe Node* nodeStack = null;

    public History history;
    public CorrectionHistory CorrHist;


    public SearchThread(int id)
    {
        this.id = id;
        pv_ = new();
        repTable = new RepetitionTable();
        rootPos = new RootPos();

        history = new();
        CorrHist = new();

        myThread = new Thread(ThreadMainLoop);
        myThread.Name = $"SearchThread_{id}";
        myThread.Start();
    }

    private unsafe void ThreadMainLoop()
    {
        Console.WriteLine($"thread {id} started");

        Span<Node> NodeSpan = stackalloc Node[256];
        fixed (Node* NodePtr = NodeSpan)
        {
            this.nodeStack = NodePtr;

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

    public void UndoMove()
    {
        ply--;
        repTable.Pop();
    }

    /// <summary>
    /// Start searching from UCI commands
    /// only works when already in ThreadMainLoop
    /// not applicable to Bench
    /// </summary>
    public void Go()
    {
        myResetEvent.Set();
    }
    
    /// <summary>
    /// Stops searching from UCI commands
    /// only works when already searching and in ThreadMainLoop
    /// </summary>
    public void Stop()
    {
        this.doSearch = false;
    }

    /// <summary>
    /// Make the thread ready inbetween searches
    /// </summary>
    public unsafe void Reset()
    {
        ply = 0;
        seldepth = 0;
        nodeCount = 0;
        completedDepth = 0;

        pv_.Clear();
        NativeMemory.Clear(nodeStack, (nuint)sizeof(Node) * 255);
    }

    /// <summary>
    /// Clear the threads stored information inbetween games
    /// </summary>
    public void Clear()
    {
        doSearch = true;
        Reset();

        pv_.Clear();
        history.Clear();
        CorrHist.Clear();
    }

    /// <summary>
    /// Stops the thread and disposes of this object
    /// </summary>
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
        this.repTable.Dispose();
        this.history.Dispose();
        this.CorrHist.Dispose();
    }

    public unsafe void RunBench()
    {
        var watch = new Stopwatch();
        long totalNodes = 0;

        Span<Node> NodeSpan = stackalloc Node[256];
        fixed (Node* NodePtr = NodeSpan)
        {
            this.nodeStack = NodePtr;

            watch.Start();
            foreach (var fen in Bench.Positions)
            {
                this.Clear();
                ThreadPool.tt.Clear();
                TimeManager.PrepareBench(TimeManager.maxDepth);

                rootPos.SetNewFen(fen);
                rootPos.InitRootMoves();

                Search.IterativeDeepen(this);
                totalNodes += nodeCount;
            }
            watch.Stop();

        } // fixed

        Stop();

        long nps = totalNodes * 1000 / Math.Max(watch.ElapsedMilliseconds, 1);
        Console.WriteLine($"{totalNodes} nodes {nps} nps");

        Console.WriteLine($"{Bench.BenchNodes} / {totalNodes} : {Bench.BenchNodes == totalNodes}");
    }

}