using UnityEngine;

namespace Chess3D.Core
{
    // Attach temporarily to an empty GameObject to run automatic sanity checks for pawn captures.
    public class PawnCaptureTester : MonoBehaviour
    {
        public bool runOnStart = true;
        public bool logMoves = true;
        void Start()
        {
            if (runOnStart)
            {
                TestSimpleDiagonalCapture();
                TestEnPassant();
            }
        }

        private void TestSimpleDiagonalCapture()
        {
            var state = BoardState.CreateInitial();
            // Clear board except two pawns (white at d4 (3,3) black at e5 (4,4)) to test white capture
            for (int x=0;x<8;x++) for (int y=0;y<8;y++) state.SetPiece(x,y,null);
            state.SetPiece(3,3,new Piece(PieceType.Pawn, PieceColor.White));
            state.SetPiece(4,4,new Piece(PieceType.Pawn, PieceColor.Black));
            state.SetSideToMove(PieceColor.White);
            bool found = false;
            foreach (var mv in MoveGenerator.GenerateLegalMoves(state))
            {
                if (mv.FromX==3 && mv.FromY==3 && mv.ToX==4 && mv.ToY==4) { found = true; break; }
            }
            if (logMoves) Debug.Log("[PawnCaptureTester] Diagonal capture available? " + found);
        }

        private void TestEnPassant()
        {
            var state = BoardState.CreateInitial();
            // Clear board
            for (int x=0;x<8;x++) for (int y=0;y<8;y++) state.SetPiece(x,y,null);
            // White pawn at e5 (4,4), black pawn at d7 (3,6) will double push to d5 allowing en passant e5xd6
            state.SetPiece(4,4,new Piece(PieceType.Pawn, PieceColor.White));
            state.SetPiece(3,6,new Piece(PieceType.Pawn, PieceColor.Black));
            state.SetSideToMove(PieceColor.Black);
            // Black double push from (3,6) to (3,4)
            var doublePush = new Move(3,6,3,4);
            MoveApplier.Apply(state,doublePush);
            // Now white to move should have en passant capture from (4,4) to (3,5)
            bool epFound = false;
            foreach (var mv in MoveGenerator.GenerateLegalMoves(state))
            {
                if (mv.FromX==4 && mv.FromY==4 && mv.ToX==3 && mv.ToY==5) { epFound = true; break; }
            }
            if (logMoves) Debug.Log("[PawnCaptureTester] En Passant capture available? " + epFound + "; EP target=" + (state.EnPassantTarget.HasValue? state.EnPassantTarget.Value.ToString():"none"));
        }
    }
}
