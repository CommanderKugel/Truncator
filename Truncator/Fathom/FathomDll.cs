
using System.Diagnostics;
using System.Runtime.InteropServices;

public static partial class FathomDll
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
    private static extern int tb_probe_wdl_(
        ulong white, ulong black,
        ulong kings, ulong queens,
        ulong rooks, ulong bishops,
        ulong knights, ulong pawns,
        uint rule50, uint castling,
        uint ep, bool stm
    );

    /// <summary>
    /// Wrapper for Fathoms probe_wdl function
    /// Make sure the tb is already initialized
    /// Can only be called when
    /// - there are no castling rights
    /// - the fifty move rule was just reset
    /// - there are no move pieces on the board than the largest
    /// </summary>
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


    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static unsafe extern int tb_probe_root_(
        ulong white, ulong black,
        ulong kings, ulong queens,
        ulong rooks, ulong bishops,
        ulong knights, ulong pawns,
        uint rule50, uint castling,
        uint ep, bool stm,
        uint* results
    );

    /// <summary>
    /// Wrapper for Fathoms probe_root function
    /// Make sure the tb is already initialized
    /// Can only be called when
    /// - there are no castling rights
    /// - the fifty move rule was just reset
    /// - there are no move pieces on the board than the largest
    /// </summary>
    public static unsafe (int, Move, int) ProbeRoot(ref Pos p)
    {
        Debug.Assert(status == Status.Initialized);
        Debug.Assert(p.CastlingRights == 0);
        Debug.Assert(Utils.popcnt(p.blocker) <= TbLargest);

        int res = tb_probe_root_(
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
            p.Us == Color.White,
            null
        );

        // extract data from returned value
        int wdl = TB_GET_WDL(res);
        int from = TB_GET_FROM(res);
        int to = TB_GET_TO(res);
        int promo = TB_GET_PROMOTES(res);
        int ep = TB_GET_EP(res);
        int dtz = TB_GET_DTZ(res);

        // build the best-move from the returned value
        MoveFlag flag = MoveFlag.Normal;
        if (promo != TB_PROMOTES_NONE)
        {
            flag = promo switch
            {
                TB_PROMOTES_KNIGHT => MoveFlag.KnightPromo,
                TB_PROMOTES_BISHOP => MoveFlag.BishopPromo,
                TB_PROMOTES_ROOK => MoveFlag.RookPromo,
                TB_PROMOTES_QUEEN => MoveFlag.QueenPromo,
                _ => MoveFlag.Normal,
            };
        }
        else if (ep != 0)
        {
            flag = MoveFlag.EnPassant;
        }
        Move tbMove = new Move(from, to, flag);

        return (wdl, tbMove, dtz);
    }


    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int tb_init_(string path);

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


    [DllImport(@"fathomDll.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tb_free_();

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

}
