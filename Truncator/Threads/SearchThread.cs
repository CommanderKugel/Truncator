
public class SearchThread : IDisposable
{
    private Thread myThread;
    private ManualResetEvent myResetEvent = new ManualResetEvent(false);

    public readonly int id;
    public bool IsMainThread => id == 0;

    public bool search = false;
    public bool die = false;

    public Pos p;


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
            // wait for go signal
            Console.WriteLine("thread waits now");


            myResetEvent.WaitOne();
            this.search = true;
            Console.WriteLine("thread woke up");


            if (die)
            {
                Console.WriteLine("thread dies after idle");
                break;
            }
            else
            {
                Console.WriteLine("thread works after idle");
                Search.GetBestMove(this);
            }


            // go back to idle
            myResetEvent.Reset();
        }

        Console.WriteLine("thread-loop broken");
    }

    public void Go()
    {
        this.search = true;
        myResetEvent.Set();
    }

    public void Stop()
    {
        this.search = false;
    }

    public void Die()
    {
        myResetEvent.Set();
        Stop();
        this.die = true;
        Dispose();
    }

    public void Join()
    {
        this.myThread.Join();
    }

    public void Dispose()
    {
        
    }

}