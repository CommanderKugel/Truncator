using static Utils;

using System.Diagnostics;

public partial struct Pos
{
    public Pos(string fen) 
    {
        ParseFen(fen);
    }

    public unsafe void ParseFen(string fen)
    {
        PieceBB[0] = PieceBB[1] = PieceBB[2] =
        PieceBB[3] = PieceBB[4] = PieceBB[5] = 0;
        ColorBB[0] = ColorBB[1] = 0;

        CastlingRights = 0;
        EnPassantSquare = 0;
        FiftyMoveRule = 0;

        int r = 7, f = 0, idx = 0;
        char c = 'x';

        for (; c != ' '; idx++)
        {
            c = fen[idx];
            int sq = 8 * r + f;
            f++;

            switch (c)
            {
                case 'P': SetPiece(Color.White, PieceType.Pawn, sq); break;
                case 'N': SetPiece(Color.White, PieceType.Knight, sq); break;
                case 'B': SetPiece(Color.White, PieceType.Bishop, sq); break;
                case 'R': SetPiece(Color.White, PieceType.Rook, sq); break;
                case 'Q': SetPiece(Color.White, PieceType.Queen, sq); break;
                case 'K': SetPiece(Color.White, PieceType.King, sq); break;
                case 'p': SetPiece(Color.Black, PieceType.Pawn, sq); break;
                case 'n': SetPiece(Color.Black, PieceType.Knight, sq); break;
                case 'b': SetPiece(Color.Black, PieceType.Bishop, sq); break;
                case 'r': SetPiece(Color.Black, PieceType.Rook, sq); break;
                case 'q': SetPiece(Color.Black, PieceType.Queen, sq); break;
                case 'k': SetPiece(Color.Black, PieceType.King, sq); break;
                case '/': f = 0; r--; break;
                case ' ': break;
                default: try { f += int.Parse(c.ToString()) - 1; } catch { } break;
            }
        }

        // set Kings Squares for faster lookup
        KingSquares[(int)Color.White] = lsb(GetPieces(Color.White, PieceType.King));
        KingSquares[(int)Color.Black] = lsb(GetPieces(Color.Black, PieceType.King));

        // Side to move (stm)
        Us = fen[idx++] == 'w' ? Color.White : Color.Black;

        // castling rights
        for (char cr = fen[++idx]; cr != ' ' && cr != '-'; cr = fen[++idx])
        {
            SetCastlingRight(cr);
        }

        // en passant
        if (fen[++idx] != '-' && fen[idx] != ' ')
        {
            int file = LetterToFile(fen[idx++]);
            int rank = NumberToRank(fen[idx++]);
            EnPassantSquare = 8 * rank + file + (Us == Color.White ? -8 : 8);
            Debug.Assert(EnPassantSquare >= 0 && EnPassantSquare < 64);
        }
        else
        {
            EnPassantSquare = (int)Square.NONE;
        }

        // ToDo: half-move and full-move counters

        // fifty move rule
        FiftyMoveRule = 0;

        Threats[(int)Color.White] = ComputeThreats(Color.White);
        Threats[(int)Color.Black] = ComputeThreats(Color.Black);

        Castling.UpdateNewPosition(ref this);
        Zobrist.ComputeFromZero(ref this);
    }

    private unsafe void SetPiece(Color c, PieceType pt, int sq)
    {
        PieceBB[(int)pt] |= 1ul << sq;
        ColorBB[(int)c ] |= 1ul << sq;
    }

    private unsafe void SetCastlingRight(char cr)
    {
        Debug.Assert(cr >= 'a' && cr <= 'h' ||
                     cr >= 'A' && cr <= 'H' ||
                     "KQkq".Contains(cr),
                     "Invalid char to set castling right from");

        Color c = char.IsUpper(cr) ? Color.White : Color.Black;

        int kingFile = FileOf(KingSquares[(int)c]);
        int castleFile = cr switch
        {
            'K' or 'k' => FileOf((int)Square.H1),
            'Q' or 'q' => FileOf((int)Square.A1),
            _ => LetterToFile(char.ToLower(cr)),
        };

        bool kingside = castleFile > kingFile;

        CastlingRights |= Castling.GetCastlingRightMask(c, kingside);

        // optional for (d)frc here: save rook and king start squares
    }
    
    public string get_fen()
    {
        string fen = "";

        // Piece Representation
        for (int rank = 7; rank >= 0; rank--)
        {
            // count squares between pieces on a rank
            int cnt = 0;

            for (int file = 0; file < 8; file++)
            {
                int sq = 8 * rank + file;
                PieceType pt = PieceTypeOn(sq);

                if (pt != PieceType.NONE)
                {
                    if (cnt > 0)
                    {
                        fen += (char)(cnt + '0');
                    }
                    fen += PieceChar(ColorOn(sq), pt);
                    cnt = 0;
                }
                else
                {
                    cnt++;
                }
            }

            if (cnt > 0)
            {
                fen += (char)(cnt + '0');
            }

            fen += rank == 0 ? ' ' : '/';
        }

        // stm
        fen += "wb"[(int)Us] + " ";

        // castling rights 
        Debug.Assert(!UCI.IsChess960, "computing fisher random fens is not supportet yet!");
        if (HasCastlingRight(Color.White, true)) fen += 'K';
        if (HasCastlingRight(Color.White, false)) fen += 'Q';
        if (HasCastlingRight(Color.Black, true )) fen += 'k';
        if (HasCastlingRight(Color.Black, false)) fen += 'q';
        if (fen[^1] == ' ') fen += "-";

        fen += " ";

        if (EnPassantSquare != (int)Square.NONE)
        {
            int epsq = EnPassantSquare + (Us == Color.White ? 8 : -8);
            fen += SquareToString(epsq);
        }
        else
        {
            fen += '-';
        }

        // approximation for full-move-counter and half-move-counter
        fen += $" {FiftyMoveRule / 2} {FiftyMoveRule}";

        return fen;
    }

}