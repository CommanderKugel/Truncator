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
            EnPassantSquare = 8 * rank + file;
            Debug.Assert(EnPassantSquare >= 0 && EnPassantSquare < 64);
        }
        else
        {
            EnPassantSquare = (int)Square.NONE;
        }

        // ToDo: half-move and full-move counters

        // fifty move rule
        FiftyMoveRule = 0;

        Castling.UpdateNewPosition(ref this);
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

}