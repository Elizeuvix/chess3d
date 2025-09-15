using System;

namespace Chess3D.Core
{
    public readonly struct Move
    {
        public readonly int FromX; public readonly int FromY; public readonly int ToX; public readonly int ToY;
        public readonly PieceType Promotion;
        public Move(int fromX,int fromY,int toX,int toY, PieceType promotion = PieceType.None)
        { FromX=fromX; FromY=fromY; ToX=toX; ToY=toY; Promotion=promotion; }
        public override string ToString() => $"{FromX}{FromY}->{ToX}{ToY}{(Promotion!=PieceType.None?"="+Promotion:"")}";
    }
}
