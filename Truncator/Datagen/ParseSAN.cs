using System.Diagnostics;

public static class ParseSAN
{

    /// <summary>
    /// Parses a move in Short Algebraic Notation (SAN) and returns a move struct
    /// hevaily inspired by Pawnocchips SAN Move parsing code found at
    /// https://github.com/JonathanHallstrom/pawnocchio/blob/1601cb420863c06d81ee8c0d40b19a9dcb95d7b4/src/Board.zig#L1062
    /// </summary>
    public static unsafe Move ParseSANMove(SearchThread thread, ref Pos p, string s)
    {
        Debug.Assert(s != "" && s != null);

        // remove additional but useless notation
        // (evan castling can give check(-mate))

        if (s[^1] == '#' || s[^1] == '+')
        {
            s = s[..^1];
        }

        // handle castling first, its the most different

        if (s[0] == 'O')
        {
            // kingside castling

            if (s == "O-O")
            {
                return new(
                    thread.castling.kingStart[(int)p.Us],
                    thread.castling.kingTargets[Castling.GetCastlingIdx(p.Us, true)],
                    MoveFlag.Castling
                );
            }

            // quenside castling

            if (s == "O-O-O")
            {
                return new(
                    thread.castling.kingStart[(int)p.Us],
                    thread.castling.kingTargets[Castling.GetCastlingIdx(p.Us, false)],
                    MoveFlag.Castling
                );
            }

            throw new Exception("something weng wrong parsing castling moves");
        }

        // parse for promotions
        // only promotions dont have a square as the last 2 char

        PieceType promoPt = (s[^1] >= '1' && s[^1] <= '8') ? PieceType.NONE : Utils.CharToPt(s[^1]);
        bool promotion = promoPt != PieceType.NONE;

        if (promoPt != PieceType.NONE)
        {
            s = s[0..(s.Length - 2)];
        }

        // parse destination square

        int toRank = Utils.NumberToRank(s[^1]);
        int toFile = Utils.LetterToFile(s[^2]);
        int to = toRank * 8 + toFile;

        // parse moving piecetype

        PieceType pt = s[0] switch
        {
            >= 'a' and <= 'h' => PieceType.Pawn,
            'N' => PieceType.Knight,
            'B' => PieceType.Bishop,
            'R' => PieceType.Rook,
            'Q' => PieceType.Queen,
            'K' => PieceType.King,
            _ => throw new Exception("couldnt properly parse the moving PieceType"),
        };

        // parse for invalid startfiles and -ranks
        // and if its a capture or not

        bool capture = false;
        ulong allowedMask = 0xFFFF_FFFF_FFFF_FFFFul;

        foreach (char c in s[0..^2])
        {
            if (c >= '1' && c <= '8')
            {
                allowedMask &= 0xFFul << (8 * (c - '1'));
            }

            else if (c >= 'a' && c <= 'h')
            {
                allowedMask &= 0x0101_0101_0101_0101ul << (c - 'a');
            }

            else if (c == 'x')
            {
                capture = true;
            }

            // else skip
        }

        // generate moves and compare them to the SAN move

        Span<Move> moves = stackalloc Move[256];
        List<Move> valids = [];

        int moveCount = 0;
        if (capture) MoveGen.GeneratePseudolegalMoves<Captures>(thread, ref moves, ref moveCount, ref p);
        else MoveGen.GeneratePseudolegalMoves<Quiets>(thread, ref moves, ref moveCount, ref p);

        for (int i = 0; i < moveCount; i++)
        {
            Move m = moves[i];

            if (m.to != to) continue;

            if (((1ul << m.from) & allowedMask) == 0) continue;

            if (p.PieceTypeOn(m.from) != pt) continue;

            if (promotion && (!m.IsPromotion || promoPt != m.PromoType)) continue;

            if (!p.IsLegal(thread, m)) continue;

            valids.Add(m);
        }

        if (valids.Count == 1)
        {
            return valids[0];
        }

        Utils.print(p);
        throw new Exception($"Error encountered parsing '{s}', too many or no viable moves found.");
    }

}