namespace Chess3D.Core
{
    public enum GameResult
    {
        Ongoing = 0,
        WhiteWinsCheckmate = 1,
        BlackWinsCheckmate = 2,
        Stalemate = 3,
        DrawFiftyMoveRule = 4, // (futuro)
        DrawThreefoldRepetition = 5, // (futuro)
        DrawInsufficientMaterial = 6 // (futuro)
    }
}
