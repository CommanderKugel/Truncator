
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class FathomDll
{

    public static bool DoTbProbing = false;
    public static int TbLargest = 0;
    public static string SyzygyPath = "";
    public static int SyzygyProbePly = 40;

    private enum Status
    {
        Uninitialized,
        Initialized,
        Disposed,
    }

    private static Status status = Status.Uninitialized;


    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_largest();

    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int tb_init_(string path);

    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tb_free_();

    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int tb_probe_wdl_(
        ulong white, ulong black,
        ulong kings, ulong queens,
        ulong rooks, ulong bishops,
        ulong knights, ulong pawns,
        uint rule50, uint castling,
        uint ep, bool stm
    );

    public static void Init(string path)
    {

        if (status == Status.Initialized)
        {
            Console.WriteLine("tb is already initialized");
            return;
        }

        // Fathon only lets us initialize it once
        // once it was dispsed once, it cannot be re-initialized again
        // or it crashes

        if (status == Status.Disposed)
        {
            Console.WriteLine("tb is disposed, cannot re-initialize");
            return;
        }

        try
        {
            SyzygyPath = path;
            int res = tb_init_(path);
            Console.WriteLine($"tb initialized with returncode {res}");

            TbLargest = get_largest();
            DoTbProbing = true;
            Console.WriteLine($"largest tb size {TbLargest}");

            if (res == 1)
            {
                status = Status.Initialized;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error initializing tb: {e.Message}");
        }
    }

    public static void Dispose()
    {
        if (status == Status.Uninitialized)
        {
            Console.WriteLine("tb is not initialized, nothing to dispose");
            return;
        }
        if (status == Status.Disposed)
        {
            Console.WriteLine("tb is already disposed");
            return;
        }

        try
        {
            tb_free_();
            Console.WriteLine($"tb successfully disposed");
            status = Status.Disposed;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error disposing tb: {e.Message}");
        }
    }

    /// <summary>
    /// Wrapper for Fathoms wdl probing function
    /// Make sure the tb is already initialized
    /// Can only be called when
    /// - there are no castling rights
    /// - the fifty move rule was just reset
    /// - there are no move pieces on the board than the largest
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static unsafe int ProbeWdl(ref Pos p)
    {
        Debug.Assert(status == Status.Initialized, "tb is not initialized");
        Debug.Assert(p.CastlingRights == 0);
        Debug.Assert(p.FiftyMoveRule == 0);
        Debug.Assert(Utils.popcnt(p.blocker) <= TbLargest);

        int res = tb_probe_wdl_(
            p.ColorBB[(int)Color.White],
            p.ColorBB[(int)Color.Black],
            p.PieceBB[(int)PieceType.King],
            p.PieceBB[(int)PieceType.Queen],
            p.PieceBB[(int)PieceType.Rook],
            p.PieceBB[(int)PieceType.Bishop],
            p.PieceBB[(int)PieceType.Knight],
            p.PieceBB[(int)PieceType.Pawn],
            (uint)p.FiftyMoveRule,
            (uint)p.CastlingRights,
            p.EnPassantSquare == (uint)Square.NONE ? 0 : (uint)p.EnPassantSquare,
            p.Us == Color.White
        );

        return res;
    }

}
