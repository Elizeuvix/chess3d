using System;

namespace Chess3D.Core
{
    public static class AttackEvaluator
    {
        public static bool IsSquareAttacked(BoardState board, int x, int y, PieceColor byColor)
        {
            // Pawns
            int dir = byColor == PieceColor.White ? 1 : -1;
            foreach (int dx in new[]{-1,1})
            {
                int px = x - dx; // inverse: attacker at (x-dx, y-dir)
                int py = y - dir;
                if (InBoard(px,py))
                {
                    var p = board.GetPiece(px,py);
                    if (p != null && p.Color == byColor && p.Type == PieceType.Pawn)
                        return true;
                }
            }
            // Knights
            int[] kdx = {1,2,2,1,-1,-2,-2,-1};
            int[] kdy = {2,1,-1,-2,-2,-1,1,2};
            for (int i=0;i<8;i++)
            {
                int nx = x + kdx[i]; int ny = y + kdy[i];
                if (!InBoard(nx,ny)) continue;
                var p = board.GetPiece(nx,ny);
                if (p != null && p.Color == byColor && p.Type == PieceType.Knight) return true;
            }
            // Sliding (rook/queen)
            if (RayAttack(board,x,y,byColor,new[]{(1,0),(-1,0),(0,1),(0,-1)}, PieceType.Rook, PieceType.Queen)) return true;
            // Sliding (bishop/queen)
            if (RayAttack(board,x,y,byColor,new[]{(1,1),(1,-1),(-1,1),(-1,-1)}, PieceType.Bishop, PieceType.Queen)) return true;
            // King (adjacent)
            for (int dx=-1; dx<=1; dx++)
            for (int dy=-1; dy<=1; dy++)
            {
                if (dx==0 && dy==0) continue;
                int nx = x+dx; int ny = y+dy;
                if (!InBoard(nx,ny)) continue;
                var p = board.GetPiece(nx,ny);
                if (p != null && p.Color == byColor && p.Type == PieceType.King) return true;
            }
            return false;
        }

        private static bool RayAttack(BoardState board,int x,int y, PieceColor color,(int dx,int dy)[] dirs, PieceType t1, PieceType t2)
        {
            foreach (var (dx,dy) in dirs)
            {
                int nx = x+dx; int ny = y+dy;
                while (InBoard(nx,ny))
                {
                    var p = board.GetPiece(nx,ny);
                    if (p != null)
                    {
                        if (p.Color == color && (p.Type == t1 || p.Type == t2)) return true;
                        break;
                    }
                    nx += dx; ny += dy;
                }
            }
            return false;
        }

        private static bool InBoard(int x,int y)=> x>=0 && x<8 && y>=0 && y<8;

        public static (int x,int y) FindKing(BoardState board, PieceColor color)
        {
            for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
            {
                var p = board.GetPiece(x,y);
                if (p != null && p.Color == color && p.Type == PieceType.King) return (x,y);
            }
            throw new InvalidOperationException("King not found for color " + color);
        }
    }
}
