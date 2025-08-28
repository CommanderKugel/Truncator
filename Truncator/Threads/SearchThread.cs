
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

    public bool IsDisposed = false;


    // search variables
    public RootPos rootPos;
    public Castling castling;
    public RepetitionTable repTable;

    public volatile int ply;
    public volatile int seldepth;
    public long nodeCount = 0;
    public long tbHits = 0;

    public int completedDepth = 0;

    // search objects
    public PV PV;

    public void NewPVLine() => PV[ply, ply] = Move.NullMove;
    public void PushToPV(Move m) => PV.Push(m, ply);

    public TranspositionTable tt = ThreadPool.tt;
    public volatile unsafe Node* nodeStack = null;

    public History history;
    public CorrectionHistory CorrHist;


    public void CopyFrom(SearchThread Parent)
    {
        rootPos.CopyFrom(Parent.rootPos);
        ply = Parent.ply;
        repTable.CopyFrom(ref Parent.repTable);
    }


    public SearchThread(int id)
    {
        this.id = id;
        PV = new PV();
        rootPos = new RootPos();
        repTable = new RepetitionTable();
        castling = new Castling();

        history = new();
        CorrHist = new();

        myThread = new Thread(ThreadMainLoop);
        myThread.Name = $"SearchThread_{id}";
        myThread.Start();

        IsDisposed = false;
    }

    private unsafe void ThreadMainLoop()
    {
        Console.WriteLine($"thread {id} started");

        Span<Node> NodeSpan = stackalloc Node[256 + 8];
        fixed (Node* NodePtr = NodeSpan)
        {
            nodeStack = NodePtr + 8;
            for (int i = 0; i < 8; i++)
            {
                (NodePtr + i)->ContHist = history.ContHist.NullHist;
                (NodePtr + i)->ContCorrHist = CorrHist.ContHist.NullHist;
                (NodePtr + i)->move = Move.NullMove;
            }

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
        tbHits = 0;
        completedDepth = 0;

        PV.Clear();
        NativeMemory.Clear(nodeStack, (nuint)sizeof(Node) * 255);
    }

    /// <summary>
    /// Clear the threads stored information inbetween games
    /// </summary>
    public void Clear()
    {
        doSearch = true;
        Reset();

        history.Clear();
        CorrHist.Clear();
    }

    /// <summary>
    /// Stops the thread and disposes of this object
    /// </summary>
    public void Join()
    {
        // set flags for thread to stop the search-loop
        // and then stop the main loop when woken up again

        die = true;
        doSearch = false;

        // wake thread up again to then break the main loop 
        // Dispose() will be called from the main loop method already
        // so no need to dispose of multiple times

        myResetEvent.Set();
    }

    /// <summary>
    /// Free all allocated memory and kill the thread
    /// </summary>
    public void Dispose()
    {
        Debug.WriteLine($"attempt to dispose of this thread: {id}, IsDisposed: {IsDisposed}");

        if (!IsDisposed)
        {
            // free all allocated memory
            
            PV.Dispose();
            repTable.Dispose();
            history.Dispose();
            CorrHist.Dispose();

            // make sure to not dispose of this multiple times

            IsDisposed = true;
        }
    }

    public unsafe void RunBench()
    {
        var watch = new Stopwatch();
        long totalNodes = 0;

        Span<Node> NodeSpan = stackalloc Node[256 + 8];
        fixed (Node* NodePtr = NodeSpan)
        {
            this.nodeStack = NodePtr + 8;
            for (int i = 0; i < 8; i++)
            {
                (NodePtr + i)->ContHist = this.history.ContHist.NullHist;
            }

            watch.Start();
            foreach (var fen in Bench.Positions)
            {
                this.Clear();
                ThreadPool.tt.Clear();
                TimeManager.PrepareBench(TimeManager.maxDepth);

                rootPos.SetNewFen(this, fen);
                rootPos.InitRootMoves(this);

                Search.IterativeDeepen(this, isBench: true);
                totalNodes += nodeCount;
            }
            watch.Stop();

        } // fixed

        Stop();

        long nps = totalNodes * 1000 / Math.Max(watch.ElapsedMilliseconds, 1);
        //Console.WriteLine($"{Bench.BenchNodes} / {totalNodes} : changed {Bench.BenchNodes != totalNodes}");
        Console.WriteLine($"{totalNodes} nodes {nps} nps");
    }

}