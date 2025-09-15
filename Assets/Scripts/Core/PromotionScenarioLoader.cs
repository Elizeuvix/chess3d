using UnityEngine;

namespace Chess3D.Core
{
    // Utilitário para preparar rapidamente cenários de promoção para testes.
    // Adicione este componente a um GameObject vazio na cena e chame PrepareWhite() / PrepareBlack() via Inspector.
    public class PromotionScenarioLoader : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        public bool clearBoard = true;

#if UNITY_EDITOR
        [ContextMenu("Prepare White Promotion A7 -> A8")]
        public void PrepareWhite()
        {
            if (synchronizer == null) { Debug.LogWarning("Synchronizer não atribuído"); return; }
            var st = synchronizer.State;
            if (clearBoard)
            {
                for (int y=0;y<8;y++) for (int x=0;x<8;x++) st.SetPiece(x,y,null);
            }
            // Rei branco e rei preto para manter estado válido mínimo
            st.SetPiece(4,0,new Piece(PieceType.King, PieceColor.White));
            st.SetPiece(4,7,new Piece(PieceType.King, PieceColor.Black));
            // Peão branco em a7
            st.SetPiece(0,6,new Piece(PieceType.Pawn, PieceColor.White));
            st.SetSideToMove(PieceColor.White);
            synchronizer.ForceRebuild();
            Debug.Log("Cenário de promoção branca preparado (a7 -> a8)");
        }

        [ContextMenu("Prepare Black Promotion A2 -> A1")]
        public void PrepareBlack()
        {
            if (synchronizer == null) { Debug.LogWarning("Synchronizer não atribuído"); return; }
            var st = synchronizer.State;
            if (clearBoard)
            {
                for (int y=0;y<8;y++) for (int x=0;x<8;x++) st.SetPiece(x,y,null);
            }
            st.SetPiece(4,0,new Piece(PieceType.King, PieceColor.White));
            st.SetPiece(4,7,new Piece(PieceType.King, PieceColor.Black));
            // Peão preto em a2 (x=0,y=1) assumindo pretas movem para y-1
            st.SetPiece(0,1,new Piece(PieceType.Pawn, PieceColor.Black));
            st.SetSideToMove(PieceColor.Black);
            synchronizer.ForceRebuild();
            Debug.Log("Cenário de promoção preta preparado (a2 -> a1)");
        }
#endif
    }
}
