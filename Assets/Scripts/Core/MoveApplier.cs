using System;
#nullable enable

namespace Chess3D.Core
{
    public static class MoveApplier
    {
        public static void Apply(BoardState board, Move move)
        {
            // Defensive: ensure move appears in legal move list (avoid external misuse)
            bool found = false;
            foreach (var legal in MoveGenerator.GenerateLegalMoves(board))
            {
                if (legal.FromX == move.FromX && legal.FromY == move.FromY && legal.ToX == move.ToX && legal.ToY == move.ToY && legal.Promotion == move.Promotion)
                {
                    found = true; break;
                }
            }
            if (!found)
            {
                #if UNITY_EDITOR
                var legalSet = string.Join(",", MoveGenerator.GenerateLegalMoves(board));
                UnityEngine.Debug.LogWarning($"[MoveApplier] Illegal move attempted {move}. Side: {board.SideToMove}. Legal set: {legalSet}");
                #endif
                throw new System.InvalidOperationException($"Illegal move attempted: {move}");
            }
            var piece = board.GetPiece(move.FromX, move.FromY);
            if (piece == null) throw new InvalidOperationException("No piece on source square");
            var target = board.GetPiece(move.ToX, move.ToY);
            // Capture resets halfmove clock
            if (target != null) board.ResetHalfmoveClock(); else board.IncrementHalfmoveClock();

            board.SetPiece(move.FromX, move.FromY, null);
            bool isPawn = piece.Type == PieceType.Pawn;
            bool isKing = piece.Type == PieceType.King;

            if (move.Promotion != PieceType.None && isPawn)
            {
                piece = new Piece(move.Promotion, piece.Color);
            }
            piece.MarkMoved();
            board.SetPiece(move.ToX, move.ToY, piece);

            // Handle castling rook move (king moves two squares)
            if (isKing && System.Math.Abs(move.ToX - move.FromX) == 2)
            {
                // King side or queen side
                if (move.ToX == 6) // king side target file
                {
                    // rook from 7 -> 5
                    var rook = board.GetPiece(7, move.FromY);
                    board.SetPiece(7, move.FromY, null);
                    if (rook != null)
                    {
                        rook.MarkMoved();
                        board.SetPiece(5, move.FromY, rook);
                    }
                }
                else if (move.ToX == 2) // queen side
                {
                    // rook from 0 -> 3
                    var rook = board.GetPiece(0, move.FromY);
                    board.SetPiece(0, move.FromY, null);
                    if (rook != null)
                    {
                        rook.MarkMoved();
                        board.SetPiece(3, move.FromY, rook);
                    }
                }
            }

            // --- En Passant Handling (ordering adjusted) ---
            // Preserve previous EP target for capture detection
            var previousEp = board.EnPassantTarget;

            // Detect en passant capture BEFORE updating new target:
            if (isPawn && target == null && previousEp.HasValue && previousEp.Value.x == move.ToX && previousEp.Value.y == move.ToY && System.Math.Abs(move.ToX - move.FromX) == 1)
            {
                int dir = (piece.Color == PieceColor.White) ? -1 : 1; // captured pawn sits one rank behind destination relative to mover
                int capturedPawnY = move.ToY + dir;
                var capturedPawn = board.GetPiece(move.ToX, capturedPawnY);
                if (capturedPawn != null && capturedPawn.Type == PieceType.Pawn)
                {
                    board.SetPiece(move.ToX, capturedPawnY, null);
                    board.ResetHalfmoveClock();
                }
            }

            // Now update en passant target for NEXT move
            if (isPawn && System.Math.Abs(move.ToY - move.FromY) == 2)
            {
                int epY = (move.FromY + move.ToY) / 2;
                board.SetEnPassant(move.FromX, epY);
            }
            else
            {
                board.ClearEnPassant();
            }
            // --- End En Passant Handling ---

            // Update castling rights if king or rook moved/captured
            UpdateCastlingRights(board, move, piece, target);

            board.ToggleSide();
        }

        private static void UpdateCastlingRights(BoardState board, Move move, Piece moved, Piece? captured)
        {
            // White moved piece
            if (moved.Color == PieceColor.White)
            {
                if (moved.Type == PieceType.King)
                {
                    board.RevokeWhiteKingSide();
                    board.RevokeWhiteQueenSide();
                }
                else if (moved.Type == PieceType.Rook)
                {
                    if (move.FromX == 0 && move.FromY == 0) board.RevokeWhiteQueenSide();
                    else if (move.FromX == 7 && move.FromY == 0) board.RevokeWhiteKingSide();
                }
            }
            // Black moved piece
            if (moved.Color == PieceColor.Black)
            {
                if (moved.Type == PieceType.King)
                {
                    board.RevokeBlackKingSide();
                    board.RevokeBlackQueenSide();
                }
                else if (moved.Type == PieceType.Rook)
                {
                    if (move.FromX == 0 && move.FromY == 7) board.RevokeBlackQueenSide();
                    else if (move.FromX == 7 && move.FromY == 7) board.RevokeBlackKingSide();
                }
            }
            // Rook captured
            if (captured != null && captured.Type == PieceType.Rook)
            {
                if (move.ToX == 0 && move.ToY == 0) board.RevokeWhiteQueenSide();
                else if (move.ToX == 7 && move.ToY == 0) board.RevokeWhiteKingSide();
                else if (move.ToX == 0 && move.ToY == 7) board.RevokeBlackQueenSide();
                else if (move.ToX == 7 && move.ToY == 7) board.RevokeBlackKingSide();
            }
        }
    }
}
