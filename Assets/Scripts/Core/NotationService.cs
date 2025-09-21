using System.Linq;

namespace Chess3D.Core
{
    /// <summary>
    /// Generates SAN (Standard Algebraic Notation) strings for moves based on board state before and after the move.
    /// Keeps implementation self-contained and stateless.
    /// </summary>
    public static class NotationService
    {
        public static string GenerateSAN(BoardState pre, BoardState post, Move move)
        {
            var mover = pre.GetPiece(move.FromX, move.FromY);
            if (mover == null) return ""; // defensive

            // Castling first
            if (mover.Type == PieceType.King && System.Math.Abs(move.ToX - move.FromX) == 2)
            {
                string baseSan = (move.ToX > move.FromX) ? "O-O" : "O-O-O";
                return baseSan + CheckSuffix(post, mover.Color);
            }

            bool isPawn = mover.Type == PieceType.Pawn;
            var targetBefore = pre.GetPiece(move.ToX, move.ToY);
            bool isEnPassant = false;
            if (isPawn && targetBefore == null && move.FromX != move.ToX)
            {
                // Diagonal pawn move without target -> en passant
                if (pre.EnPassantTarget.HasValue && pre.EnPassantTarget.Value.x == move.ToX && pre.EnPassantTarget.Value.y == move.ToY)
                {
                    isEnPassant = true;
                }
            }
            bool isCapture = targetBefore != null || isEnPassant;

            string san;
            if (isPawn)
            {
                // Pawn SAN
                string dest = SquareName(move.ToX, move.ToY);
                if (isCapture)
                {
                    san = FileChar(move.FromX) + "x" + dest;
                }
                else
                {
                    san = dest;
                }
                if (move.Promotion != PieceType.None)
                {
                    san += "=" + PieceLetter(move.Promotion);
                }
            }
            else
            {
                // Piece SAN with potential disambiguation
                string letter = PieceLetter(mover.Type);
                string disamb = ComputeDisambiguation(pre, move, mover.Type);
                string dest = SquareName(move.ToX, move.ToY);
                san = letter + disamb + (isCapture ? "x" : "") + dest;
            }

            // Check/checkmate suffix
            san += CheckSuffix(post, mover.Color);
            return san;
        }

        private static string ComputeDisambiguation(BoardState pre, Move move, PieceType type)
        {
            // Find other legal moves by same piece type that also target the same square
            var sameTarget = MoveGenerator.GenerateLegalMoves(pre)
                .Where(m => m.ToX == move.ToX && m.ToY == move.ToY);
            var sameType = sameTarget.Where(m =>
            {
                var p = pre.GetPiece(m.FromX, m.FromY);
                return p != null && p.Type == type;
            }).ToList();

            if (sameType.Count <= 1)
                return string.Empty;

            // Remove our own move origin
            var others = sameType.Where(m => !(m.FromX == move.FromX && m.FromY == move.FromY)).ToList();
            if (others.Count == 0)
                return string.Empty;

            // Minimal disambiguation per SAN rules
            bool fileUnique = !others.Any(o => o.FromX == move.FromX);
            bool rankUnique = !others.Any(o => o.FromY == move.FromY);
            if (fileUnique) return FileChar(move.FromX).ToString();
            if (rankUnique) return RankChar(move.FromY).ToString();
            return SquareName(move.FromX, move.FromY);
        }

        private static string CheckSuffix(BoardState post, PieceColor moverColor)
        {
            // Opponent is now to move
            var opponent = (moverColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            // Locate opponent king
            var (kx, ky) = AttackEvaluator.FindKing(post, opponent);
            bool inCheck = AttackEvaluator.IsSquareAttacked(post, kx, ky, moverColor);
            if (!inCheck) return string.Empty;
            // If opponent has no legal moves, it's mate
            bool hasReply = MoveGenerator.GenerateLegalMoves(post).Any();
            return hasReply ? "+" : "#";
        }

        public static string SquareName(int x, int y)
        {
            return FileChar(x).ToString() + RankChar(y);
        }

        private static char FileChar(int x) => (char)('a' + x);
        private static char RankChar(int y) => (char)('1' + y);

        private static string PieceLetter(PieceType t)
        {
            return t switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                _ => string.Empty // pawns have no letter in SAN
            };
        }
    }
}
