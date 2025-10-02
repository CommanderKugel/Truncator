
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Marlinformat
{

    /// <summary>
    /// bitboard of all occupied squares by any piecetype or color
    /// </summary>
    public ulong occupancy;

    /// <summary>
    /// lower 3 bits = pt (using unmoved root for castling = 6)
    /// 4th bit = color, 0 = white & 1 = black
    /// empty = 0
    /// </summary>
    public unsafe fixed byte pieces[32 / 2];

    /// <summary>
    /// lower 7 bits = ep sq (using 64 as Square.NONE)
    /// msb for stm, 0 = white & 1 = black
    /// </summary>
    public byte stm_ep;

    /// <summary>
    /// ply count
    /// can be left as 0
    /// </summary>
    public byte halfMoveClock;

    /// <summary>
    /// full move count
    /// can be left as 0
    /// </summary>
    public ushort fullMoveCounter;

    /// <summary>
    /// can be left as 0
    /// </summary>
    public short score;

    /// <summary>
    /// black win = 0, draw = 1, white win = 2
    /// </summary>
    public byte gameResult;

    public byte padding;


    public unsafe Marlinformat(SearchThread thread, Pgn pgn)
    {
        thread.rootPos.SetNewFen(pgn.Fen);
        ref Pos p = ref thread.rootPos.p;

        occupancy = p.blocker;
        stm_ep = (byte)(((int)p.Us << 7) | (p.EnPassantSquare == (int)Square.NONE ? 64 : p.EnPassantSquare));
        halfMoveClock = (byte)p.FiftyMoveRule;
        fullMoveCounter = (ushort)p.FullMoveCounter;
        score = 0;
        padding = 0;

        ulong copy = occupancy;
        Span<byte> pieceArray = stackalloc byte[32];
        pieceArray.Clear();

        // find all pieces on the board

        for (int idx = 0; copy != 0 && idx < 32; idx++)
        {
            int sq = Utils.popLsb(ref copy);
            int pt = (int)p.PieceTypeOn(sq);
            int c = (int)p.ColorOn(sq);

            // unmoved rook instead of castling rights

            if (pt == (int)PieceType.Rook
                && p.HasCastlingRight((Color)c, sq > p.KingSquares[c])
                && sq == thread.castling.kingTargets[Castling.GetCastlingIdx((Color)c, sq > p.KingSquares[c])])
            {
                pt = 6;
            }

            pieceArray[idx] = (byte)((c << 3) | (pt & 7));
        }

        // fill fixed piece array with... all pieces on the board
        // leaves empty pieces at zero

        for (int i = 0; i < 32 / 2; i++)
        {
            pieces[i] = (byte)((pieceArray[2 * i + 1] << 4) | (pieceArray[2 * i]));
        }

        gameResult = pgn.Result switch
        {
            "1-0" => 2,
            "0-1" => 0,
            "1/2-1/2" => 1,
            _ => throw new ArgumentException($"invalid gameresult '{pgn.Result}'"),
        };
    }


    /// <summary>
    /// returns a bytearray that contains this structs data
    /// but packed in bytes
    /// usefull for writing to files
    /// </summary>
    public unsafe readonly byte[] ToBytes()
    {
        int size = sizeof(Marlinformat);
        byte[] buff = new byte[size];
        fixed (byte* ptr = buff)
        {
            *(Marlinformat*)ptr = this;
        }
        return buff;
    }

    public unsafe readonly void Print()
    {
        var buff = ToBytes();

        for (int i = 0; i < buff.Length; i++)
        {
            Console.Write(buff[i].ToString("x") + " ");

            if (i == 7 || i >= 7 + 16)
            {
                Console.Write('\n');
            }
        }
        Console.WriteLine();
    }
}
