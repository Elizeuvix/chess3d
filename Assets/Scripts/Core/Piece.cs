using System;

namespace Chess3D.Core
{
    public sealed class Piece
    {
        public PieceType Type { get; }
        public PieceColor Color { get; }
        public bool HasMoved { get; private set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }

        public Piece Clone() => new Piece(Type, Color) { HasMoved = HasMoved };

        public void MarkMoved() => HasMoved = true;
    }
}
