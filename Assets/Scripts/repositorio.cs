using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class repositorio : MonoBehaviour
    {
        private BoardController boardController;

        // Use this for initialization
        void Start()
        {
            boardController = GetComponent<BoardController>();
        }

        // Update is called once per frame
        void Update()
        {

        }


        

        private void CapturePieceOLD(PieceConfig newSquare)
        {
            foreach (PieceConfig piece in boardController.allPieces)
            {
                if (piece.columnPos == newSquare.columnPos && piece.linePos == newSquare.linePos)
                {
                    //boardController.allPieces.Remove(piece);
                    piece.gameObject.tag = "Untagged";
                    piece.transform.SetParent(GameObject.Find("ChessDetails").transform);
                    Destroy(piece.gameObject);
                }
            }
        }
    }
}