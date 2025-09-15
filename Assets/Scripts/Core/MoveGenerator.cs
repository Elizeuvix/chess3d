using System.Collections.Generic;

namespace Chess3D.Core
{
    public static class MoveGenerator
    {
        public static IEnumerable<Move> GenerateLegalMoves(BoardState board)
        {
            foreach (var mv in GeneratePseudoLegal(board))
            {
                if (IsLegal(board, mv))
                    yield return mv;
            }
        }

        private static bool IsLegal(BoardState board, Move mv)
        {
            var clone = board.Clone();
            // Apply minimally (we can't call full MoveApplier because it toggles side etc.)
            var piece = clone.GetPiece(mv.FromX, mv.FromY);
            if (piece == null) return false;
            clone.SetPiece(mv.FromX, mv.FromY, null);
            if (mv.Promotion != PieceType.None && piece.Type == PieceType.Pawn)
                piece = new Piece(mv.Promotion, piece.Color);
            clone.SetPiece(mv.ToX, mv.ToY, piece);

            // Find king position for side to move
            var color = board.SideToMove;
            var (kx, ky) = AttackEvaluator.FindKing(clone, color);
            // If square is attacked by opponent -> illegal
            var opp = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            return !AttackEvaluator.IsSquareAttacked(clone, kx, ky, opp);
        }

        private static IEnumerable<Move> GeneratePseudoLegal(BoardState board)
        {
            for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
            {
                var p = board.GetPiece(x,y);
                if (p == null || p.Color != board.SideToMove) continue;
                switch (p.Type)
                {
                    case PieceType.Pawn:
                        foreach (var mv in PawnMoves(board,x,y,p)) yield return mv; break;
                    case PieceType.Knight:
                        foreach (var mv in KnightMoves(board,x,y,p)) yield return mv; break;
                    case PieceType.Bishop:
                        foreach (var mv in Slide(board,x,y,p, new[]{ (1,1), (1,-1), (-1,1), (-1,-1)})) yield return mv; break;
                    case PieceType.Rook:
                        foreach (var mv in Slide(board,x,y,p, new[]{ (1,0), (-1,0), (0,1), (0,-1)})) yield return mv; break;
                    case PieceType.Queen:
                        foreach (var mv in Slide(board,x,y,p, new[]{ (1,0), (-1,0), (0,1), (0,-1), (1,1), (1,-1), (-1,1), (-1,-1)})) yield return mv; break;
                    case PieceType.King:
                        foreach (var mv in KingMoves(board,x,y,p)) yield return mv; break;
                }
            }
        }

        private static IEnumerable<Move> PawnMoves(BoardState b,int x,int y, Piece p)
        {
            int dir = p.Color == PieceColor.White ? 1 : -1;
            int startRank = p.Color == PieceColor.White ? 1 : 6;
            int nextY = y + dir;
            if (InBoard(x,nextY) && b.GetPiece(x,nextY) == null)
            {
                foreach (var promo in PromoteOrAll(new Move(x,y,x,nextY), nextY, p.Color))
                    yield return promo;
                if (y == startRank && b.GetPiece(x,y+dir*2) == null)
                    yield return new Move(x,y,x,y+dir*2);
            }
            // captures
            foreach (int dx in new[]{-1,1})
            {
                int nx = x+dx;
                int ny = y+dir;
                if (!InBoard(nx,ny)) continue;
                var target = b.GetPiece(nx,ny);
                if (target != null && target.Color != p.Color)
                {
                    foreach (var promo in PromoteOrAll(new Move(x,y,nx,ny), ny, p.Color))
                        yield return promo;
                }
                // En Passant
                if (b.EnPassantTarget.HasValue && b.EnPassantTarget.Value.x == nx && b.EnPassantTarget.Value.y == ny)
                {
                    yield return new Move(x,y,nx,ny);
                }
            }
        }

        private static IEnumerable<Move> PromoteOrAll(Move baseMove, int targetRank, PieceColor color)
        {
            int lastRank = (color == PieceColor.White ? 7 : 0);
            if (targetRank == lastRank)
            {
                yield return new Move(baseMove.FromX, baseMove.FromY, baseMove.ToX, baseMove.ToY, PieceType.Queen);
                yield return new Move(baseMove.FromX, baseMove.FromY, baseMove.ToX, baseMove.ToY, PieceType.Rook);
                yield return new Move(baseMove.FromX, baseMove.FromY, baseMove.ToX, baseMove.ToY, PieceType.Bishop);
                yield return new Move(baseMove.FromX, baseMove.FromY, baseMove.ToX, baseMove.ToY, PieceType.Knight);
            }
            else
            {
                yield return baseMove;
            }
        }

        private static IEnumerable<Move> KnightMoves(BoardState b,int x,int y, Piece p)
        {
            int[] dx = {1,2,2,1,-1,-2,-2,-1};
            int[] dy = {2,1,-1,-2,-2,-1,1,2};
            for (int i=0;i<8;i++)
            {
                int nx = x+dx[i]; int ny = y+dy[i];
                if (!InBoard(nx,ny)) continue;
                var t = b.GetPiece(nx,ny);
                if (t == null || t.Color != p.Color)
                    yield return new Move(x,y,nx,ny);
            }
        }

        private static IEnumerable<Move> KingMoves(BoardState b,int x,int y, Piece p)
        {
            for (int dx=-1; dx<=1; dx++)
            for (int dy=-1; dy<=1; dy++)
            {
                if (dx==0 && dy==0) continue;
                int nx = x+dx; int ny = y+dy;
                if (!InBoard(nx,ny)) continue;
                var t = b.GetPiece(nx,ny);
                if (t == null || t.Color != p.Color)
                    yield return new Move(x,y,nx,ny);
            }
            // Castling: king not moved, rook present, path empty and not attacked
            if (!p.HasMoved)
            {
                var color = p.Color;
                int rank = (color == PieceColor.White) ? 0 : 7;
                var opp = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
                // King side
                bool canK = (color==PieceColor.White ? b.WhiteCanCastleKingSide : b.BlackCanCastleKingSide);
                if (canK && b.GetPiece(5,rank) == null && b.GetPiece(6,rank) == null)
                {
                    // squares not attacked: e,f,g (4,5,6)
                    if (!AttackEvaluator.IsSquareAttacked(b,4,rank,opp) && !AttackEvaluator.IsSquareAttacked(b,5,rank,opp) && !AttackEvaluator.IsSquareAttacked(b,6,rank,opp))
                    {
                        var rook = b.GetPiece(7,rank);
                        if (rook != null && rook.Type == PieceType.Rook && !rook.HasMoved)
                            yield return new Move(4,rank,6,rank);
                    }
                }
                // Queen side
                bool canQ = (color==PieceColor.White ? b.WhiteCanCastleQueenSide : b.BlackCanCastleQueenSide);
                if (canQ && b.GetPiece(1,rank) == null && b.GetPiece(2,rank) == null && b.GetPiece(3,rank) == null)
                {
                    if (!AttackEvaluator.IsSquareAttacked(b,4,rank,opp) && !AttackEvaluator.IsSquareAttacked(b,3,rank,opp) && !AttackEvaluator.IsSquareAttacked(b,2,rank,opp))
                    {
                        var rook = b.GetPiece(0,rank);
                        if (rook != null && rook.Type == PieceType.Rook && !rook.HasMoved)
                            yield return new Move(4,rank,2,rank);
                    }
                }
            }
        }

        private static IEnumerable<Move> Slide(BoardState b,int x,int y, Piece p, (int dx,int dy)[] dirs)
        {
            foreach (var (dx,dy) in dirs)
            {
                int nx = x+dx; int ny = y+dy;
                while (InBoard(nx,ny))
                {
                    var t = b.GetPiece(nx,ny);
                    if (t == null)
                    {
                        yield return new Move(x,y,nx,ny);
                    }
                    else
                    {
                        if (t.Color != p.Color)
                            yield return new Move(x,y,nx,ny);
                        break; // blocked
                    }
                    nx += dx; ny += dy;
                }
            }
        }

        private static bool InBoard(int x,int y) => x>=0 && x<8 && y>=0 && y<8;
    }
}
