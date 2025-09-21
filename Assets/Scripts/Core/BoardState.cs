using System;
using System.Collections.Generic;
using System.Text;
#nullable enable

namespace Chess3D.Core
{
    public sealed class BoardState
    {
        // 8x8 board indexed [file, rank] or [x,y]
        private readonly Piece?[,] _squares = new Piece?[8,8];
        public PieceColor SideToMove { get; private set; } = PieceColor.White;
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; } = 1;
        public (int x,int y)? EnPassantTarget { get; private set; }
        // Castling rights: K Q k q
        public bool WhiteCanCastleKingSide { get; private set; } = true;
        public bool WhiteCanCastleQueenSide { get; private set; } = true;
        public bool BlackCanCastleKingSide { get; private set; } = true;
        public bool BlackCanCastleQueenSide { get; private set; } = true;

        public Piece? GetPiece(int x, int y) => _squares[x,y];
        public void SetPiece(int x, int y, Piece? piece) => _squares[x,y] = piece;

        public BoardState Clone()
        {
            var clone = new BoardState
            {
                SideToMove = SideToMove,
                HalfmoveClock = HalfmoveClock,
                FullmoveNumber = FullmoveNumber,
                EnPassantTarget = EnPassantTarget,
                WhiteCanCastleKingSide = WhiteCanCastleKingSide,
                WhiteCanCastleQueenSide = WhiteCanCastleQueenSide,
                BlackCanCastleKingSide = BlackCanCastleKingSide,
                BlackCanCastleQueenSide = BlackCanCastleQueenSide
            };
            for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
            {
                var p = _squares[x,y];
                clone._squares[x,y] = p?.Clone();
            }
            return clone;
        }

        public void ToggleSide()
        {
            SideToMove = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            if (SideToMove == PieceColor.White) FullmoveNumber++;
        }

        public void ResetHalfmoveClock() => HalfmoveClock = 0;
        public void IncrementHalfmoveClock() => HalfmoveClock++;

        // Setters for FEN import
        public void SetHalfmoveClock(int value) => HalfmoveClock = System.Math.Max(0, value);
        public void SetFullmoveNumber(int value) => FullmoveNumber = System.Math.Max(1, value);
        public void SetCastlingRights(bool whiteK, bool whiteQ, bool blackK, bool blackQ)
        {
            WhiteCanCastleKingSide = whiteK;
            WhiteCanCastleQueenSide = whiteQ;
            BlackCanCastleKingSide = blackK;
            BlackCanCastleQueenSide = blackQ;
        }

        public static BoardState CreateInitial()
        {
            var b = new BoardState();
            // Pawns
            for (int x=0;x<8;x++)
            {
                b.SetPiece(x,1,new Piece(PieceType.Pawn, PieceColor.White));
                b.SetPiece(x,6,new Piece(PieceType.Pawn, PieceColor.Black));
            }
            // Rooks
            b.SetPiece(0,0,new Piece(PieceType.Rook, PieceColor.White));
            b.SetPiece(7,0,new Piece(PieceType.Rook, PieceColor.White));
            b.SetPiece(0,7,new Piece(PieceType.Rook, PieceColor.Black));
            b.SetPiece(7,7,new Piece(PieceType.Rook, PieceColor.Black));
            // Knights
            b.SetPiece(1,0,new Piece(PieceType.Knight, PieceColor.White));
            b.SetPiece(6,0,new Piece(PieceType.Knight, PieceColor.White));
            b.SetPiece(1,7,new Piece(PieceType.Knight, PieceColor.Black));
            b.SetPiece(6,7,new Piece(PieceType.Knight, PieceColor.Black));
            // Bishops
            b.SetPiece(2,0,new Piece(PieceType.Bishop, PieceColor.White));
            b.SetPiece(5,0,new Piece(PieceType.Bishop, PieceColor.White));
            b.SetPiece(2,7,new Piece(PieceType.Bishop, PieceColor.Black));
            b.SetPiece(5,7,new Piece(PieceType.Bishop, PieceColor.Black));
            // Queens
            b.SetPiece(3,0,new Piece(PieceType.Queen, PieceColor.White));
            b.SetPiece(3,7,new Piece(PieceType.Queen, PieceColor.Black));
            // Kings
            b.SetPiece(4,0,new Piece(PieceType.King, PieceColor.White));
            b.SetPiece(4,7,new Piece(PieceType.King, PieceColor.Black));
            return b;
        }

        // Explicit API ------------------------------------
        public void ClearEnPassant() => EnPassantTarget = null;
        public void SetEnPassant(int x,int y) => EnPassantTarget = (x,y);

        public void RevokeWhiteKingSide() => WhiteCanCastleKingSide = false;
        public void RevokeWhiteQueenSide() => WhiteCanCastleQueenSide = false;
        public void RevokeBlackKingSide() => BlackCanCastleKingSide = false;
        public void RevokeBlackQueenSide() => BlackCanCastleQueenSide = false;

    // Test utility / controlled setup
    public void SetSideToMove(PieceColor color) => SideToMove = color;
    }
}
