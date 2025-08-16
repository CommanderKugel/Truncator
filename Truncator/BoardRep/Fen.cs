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

        string[] tokens = fen.Split(' ');
        
        // place pieces on the bitboards

        int r = 7, f = 0;
        foreach (char c in tokens[0])
        {
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

        Us = (tokens[1] == "w") ? Color.White : Color.Black;

        // castling rights

        foreach (char c in tokens[2])
        {
            if (c == '-')
            {
                break;
            }

            SetCastlingRight(c);
        }

        // en passant

        if (tokens[3] != "-")
        {
            int file = LetterToFile(tokens[3][0]);
            int rank = NumberToRank(tokens[3][1]);

            EnPassantSquare = 8 * rank + file + (Us == Color.White ? -8 : 8);
            Debug.Assert(EnPassantSquare >= 0 && EnPassantSquare < 64);
        }
        else
        {
            EnPassantSquare = (int)Square.NONE;
        }

        // fifty move rule and full move counter

        FiftyMoveRule = int.Parse(tokens[4]);
        FullMoveCounter = int.Parse(tokens[5]);

        // miscellaneous stuff not included in the fen itself
        
        Threats = ComputeThreats();
        Castling.UpdateNewPosition(ref this);
        Zobrist.ComputeFromZero(ref this);
    }

    /// <summary>
    /// place a piece on the bitboards
    /// </summary>
    private unsafe void SetPiece(Color c, PieceType pt, int sq)
    {
        PieceBB[(int)pt] |= 1ul << sq;
        ColorBB[(int)c] |= 1ul << sq;
    }

    /// <summary>
    /// check for a castling right
    /// use this to read from a fen
    /// allowed inputs:
    /// 'KQkq' for Kingside/Queenside and color
    /// 'ABCDEFGHabcdefgh' for (d)frc castling file and 
    /// implicid Kingside/Queenside and color
    /// dont pass '-'
    /// </summary>
    /// <param name="cr"></param>
    private unsafe void SetCastlingRight(char cr)
    {
        Debug.Assert("KQkqABCDEFGHabcdefgh".Contains(cr),
            $"Invalid char {cr} to set castling right from");

        // upper case chars represent whites castling rights
        // lower case chars represent blacks castling rights

        Color c = char.IsUpper(cr) ? Color.White : Color.Black;

        // Kk always means kingside and Qq always means queenside
        // furthermore, kingside/queenside is always implicitly given by the 
        // king and rook files
        // even in (d)frc, there will always be a rook on the left and right 
        // of the king to allow for kingside and queenside castling

        int kingFile = FileOf(KingSquares[(int)c]);
        int castleFile = cr switch
        {
            'K' or 'k' => FileOf((int)Square.H1),
            'Q' or 'q' => FileOf((int)Square.A1),
            _ => LetterToFile(char.ToLower(cr)),
        };

        // if the king moves towards a bigger fileindex, its kingside
        // if the king moves towards a smller fileindex, its queenside
        // (a_file = 0) < (h_file = 7)

        bool kingside = castleFile > kingFile;
        CastlingRights |= Castling.GetCastlingRightMask(c, kingside);
    }
    
    public string GetFen()
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

        fen += $" {FullMoveCounter} {FiftyMoveRule}";

        return fen;
    }

}