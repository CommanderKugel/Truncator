
public static class SEE
{

    public static ReadOnlySpan<int> SEEMaterial => [
        100, 450, 450, 650, 1250, 0, 0,
    ];

    public static unsafe bool SEE_threshold(Move m, ref Pos p, int threshold)
    {
        int from = m.from;
        int to = m.to;

        PieceType attacker = p.PieceTypeOn(m.from);
        PieceType victim = p.GetCapturedPieceType(m);

        // best case: captureing a hanging piece
        // if we cant beat the threashhold now, we never will
        int balance = SEEMaterial[(int)victim] - threshold;
        if (balance < 0)
        {
            return false;
        }

        // worst case: attacker gets re-captured without any follow-up
        // if threshold is still beaten, it already won the exchange
        balance -= SEEMaterial[(int)attacker];
        if (balance >= 0)
        {
            return true;
        }

        // if the move is not immediately winning or loosing, compute complete SEE

        ulong bishops = p.PieceBB[(int)PieceType.Bishop] | p.PieceBB[(int)PieceType.Queen];
        ulong rooks = p.PieceBB[(int)PieceType.Rook] | p.PieceBB[(int)PieceType.Queen];

        ulong block = (p.blocker ^ (1ul << from)) | (1ul << to);
        if (m.IsEnPassant)
        {
            block ^= 1ul << p.EnPassantSquare;
        }

        ulong allAttacker = p.AttackerTo(to, block) & block;

        Color stm = p.Them;

        while (true)
        {
            ulong myAttacker = allAttacker & p.ColorBB[(int)stm];

            // stm looses if it cant re-capture
            if (myAttacker == 0)
            {
                break;
            }

            // get next attacker
            PieceType pt = PieceType.Pawn;
            for (; pt <= PieceType.King; pt++)
            {
                if ((myAttacker & p.PieceBB[(int)pt]) != 0)
                {
                    break;
                }
            }

            // pseudo make move, by remembering the from square
            block ^= 1ul << Utils.lsb(myAttacker & p.PieceBB[(int)pt]);

            // maybe new sliding pieces were revealed?
            if (pt == PieceType.Pawn || pt == PieceType.Bishop || pt == PieceType.Queen)
            {
                allAttacker |= Attacks.BishopAttacks(to, block);
            }
            if (pt == PieceType.Rook || pt == PieceType.Queen)
            {
                allAttacker |= Attacks.RookAttacks(to, block);
            }

            // pseudo remove last capturer
            allAttacker &= block;
            stm = 1 - stm;

            balance = -balance - 1 - SEEMaterial[(int)pt];

            // breaking condition if we lost the exchange by now
            if (balance >= 0)
            {
                // king cant capture, if square is still attacked by opponent
                if (pt == PieceType.King && (allAttacker & p.ColorBB[(int)stm]) != 0)
                {
                    stm = 1 - stm;
                }

                break;
            }
        }

        return stm != p.Us;
    }

}