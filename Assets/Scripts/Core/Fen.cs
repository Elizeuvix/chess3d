using System;
using System.Text;
using System.Collections.Generic;

namespace Chess3D.Core
{
    /// <summary>
    /// FEN import/export for BoardState.
    /// Supports pieces, side to move, castling (KQkq or -), en passant target, halfmove, fullmove.
    /// </summary>
    public static class Fen
    {
        public static string Export(BoardState state)
        {
            // 1) Piece placement
            var sb = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                int empties = 0;
                for (int file = 0; file < 8; file++)
                {
                    var p = state.GetPiece(file, rank);
                    if (p == null) { empties++; continue; }
                    if (empties > 0) { sb.Append(empties); empties = 0; }
                    sb.Append(PieceToFen(p));
                }
                if (empties > 0) sb.Append(empties);
                if (rank > 0) sb.Append('/');
            }

            // 2) Side to move
            sb.Append(' ');
            sb.Append(state.SideToMove == PieceColor.White ? 'w' : 'b');

            // 3) Castling
            sb.Append(' ');
            string castle = "";
            if (state.WhiteCanCastleKingSide) castle += "K";
            if (state.WhiteCanCastleQueenSide) castle += "Q";
            if (state.BlackCanCastleKingSide) castle += "k";
            if (state.BlackCanCastleQueenSide) castle += "q";
            sb.Append(castle.Length == 0 ? "-" : castle);

            // 4) En passant
            sb.Append(' ');
            if (state.EnPassantTarget.HasValue)
            {
                var (x,y) = state.EnPassantTarget.Value;
                sb.Append(NotationService.SquareName(x,y));
            }
            else sb.Append('-');

            // 5) Halfmove clock
            sb.Append(' ');
            sb.Append(state.HalfmoveClock);

            // 6) Fullmove number
            sb.Append(' ');
            sb.Append(state.FullmoveNumber);
            return sb.ToString();
        }

        public static BoardState Import(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) throw new ArgumentException("FEN vazio");
            var parts = fen.Trim().Split(' ');
            if (parts.Length < 4) throw new ArgumentException("FEN inválido: faltam campos obrigatórios");

            var state = new BoardState();
            // 1) Pieces
            ParsePieces(parts[0], state);
            // 2) Side
            state.SetSideToMove(parts[1] == "w" ? PieceColor.White : PieceColor.Black);
            // 3) Castling
            ParseCastling(parts[2], state);
            // 4) En passant
            ParseEnPassant(parts[3], state);
            // 5) Halfmove
            if (parts.Length > 4 && int.TryParse(parts[4], out int half))
                state.SetHalfmoveClock(half);
            // 6) Fullmove
            if (parts.Length > 5 && int.TryParse(parts[5], out int full))
                state.SetFullmoveNumber(full);
            return state;
        }

        private static void ParsePieces(string placement, BoardState state)
        {
            int file = 0; int rank = 7;
            foreach (char c in placement)
            {
                if (c == '/') { rank--; file = 0; continue; }
                if (char.IsDigit(c)) { file += (c - '0'); continue; }
                var piece = FenToPiece(c);
                if (piece != null)
                {
                    state.SetPiece(file, rank, piece);
                    file++;
                }
            }
        }

        private static void ParseCastling(string castling, BoardState state)
        {
            bool wK=false,wQ=false,bK=false,bQ=false;
            if (castling != "-")
            {
                foreach (char c in castling)
                {
                    switch (c)
                    {
                        case 'K': wK = true; break;
                        case 'Q': wQ = true; break;
                        case 'k': bK = true; break;
                        case 'q': bQ = true; break;
                    }
                }
            }
            state.SetCastlingRights(wK,wQ,bK,bQ);
        }

        // removed SetCastle helper (now using SetCastlingRights collector)

        private static void ParseEnPassant(string ep, BoardState state)
        {
            if (ep == "-" || ep.Length != 2) { state.ClearEnPassant(); return; }
            int file = ep[0] - 'a';
            int rank = ep[1] - '1';
            if (file>=0 && file<8 && rank>=0 && rank<8)
                state.SetEnPassant(file, rank);
            else state.ClearEnPassant();
        }

        private static char PieceToFen(Piece p)
        {
            char c = p.Type switch
            {
                PieceType.Pawn => 'p',
                PieceType.Rook => 'r',
                PieceType.Knight => 'n',
                PieceType.Bishop => 'b',
                PieceType.Queen => 'q',
                PieceType.King => 'k',
                _ => '?'
            };
            if (p.Color == PieceColor.White) c = char.ToUpperInvariant(c);
            return c;
        }

        private static Piece? FenToPiece(char c)
        {
            PieceColor color = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
            char u = char.ToLowerInvariant(c);
            PieceType t = u switch
            {
                'p' => PieceType.Pawn,
                'r' => PieceType.Rook,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => PieceType.None
            };
            if (t == PieceType.None) return null;
            return new Piece(t, color);
        }
    }
}
