using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class KingListener : MonoBehaviour
{
    // LEGACY: This logic will be replaced by core rule engine (king check detection) soon.
    PieceConfig kingConfig;
    BoardController boardController;
    DeadPiecesControl deadControl;
    PieceConfig kingLocate;
    TargetSelect targetSelect;

    public PieceConfig lastPieceMoved = null;
    public PieceConfig lastPieceLocal = null;
    bool undoesMovement = false;
    // Removed unused temp fields (lastMovedTemp, lastLocalTemp) - no longer referenced.

    public List<PieceConfig> listThreat;
    public List<PieceConfig> peripheralThreat;

    private void Awake()
    {
        kingConfig = GetComponent<PieceConfig>();
        boardController = GameObject.Find("ChessBoard").GetComponent<BoardController>();
        targetSelect = GameObject.Find("ChessBoard").GetComponent<TargetSelect>();
        deadControl = GameObject.Find("ChessBoard").GetComponent<DeadPiecesControl>();
        listThreat = new List<PieceConfig>();
        peripheralThreat = new List<PieceConfig>();
    }

    private void Update()
    {
        if (undoesMovement) return;
        UndoesMovement();
    }

    public void UpdateListThreat()
    {
        listThreat.Clear();        

        foreach (PieceConfig square in boardController.allSquares)
        {
            if (square.columnPos == kingConfig.columnPos && square.linePos == kingConfig.linePos)
            {
                kingLocate = square;
            }
        }

        foreach (PieceConfig piece in boardController.allPieces)
        {
            if (kingLocate.SquareIsInAttack(kingLocate, piece))
            {
                if (!kingLocate.IsAllowedMovement(kingLocate, piece)) continue;
                listThreat.Add(piece);
            }
        }

        if (listThreat.Count > 0)
            kingConfig.kingInCheck = true;
        else
            kingConfig.kingInCheck = false;        

        ShoutXeque(kingConfig.pieceColor);
    }

    private void UndoesMovement()
    {
    // isFakeCapture removed (not used in current logic)
        if (!kingConfig.kingInCheck) return;   
        if (lastPieceMoved == null) return;
        if (lastPieceMoved.pieceColor != kingConfig.pieceColor) return;        
        if (kingConfig.xeckedBy.Count <= 1 && 
            kingConfig.xeckedBy[0] == deadControl.lastCaptured)
        {
            boardController.message = $"The {kingConfig.xeckedBy[0].pieceName} has captured!";
            kingConfig.kingInCheck = false;
            lastPieceMoved.UpdateChessBoard(lastPieceMoved, lastPieceLocal);
            return;
        }
        else
        {
            // Legacy loop removed (was setting isFakeCapture which is no longer used)
            foreach (PieceConfig item in kingConfig.xeckedBy)
            {
                if (deadControl.lastCaptured != item) continue;
            }
        }

        //Defazendo movimento
        undoesMovement = true;
        //o problema est� na referencia da pe�a que foi capturada erroneamente (deadControl.lastCaptured)
        StopCoroutine(deadControl.lastCaptured.Capture(deadControl.lastCaptured, lastPieceMoved, false));

        targetSelect.playerPieces = lastPieceMoved.pieceColor;
        lastPieceMoved.transform.position = new Vector3(lastPieceLocal.transform.position.x, 0.5f, lastPieceLocal.transform.position.z);
        lastPieceLocal.occupiedBy = lastPieceMoved.gameObject;
        lastPieceMoved.UpdateChessBoard(lastPieceMoved, lastPieceLocal);
        boardController.message = "Forbidden move undone!";
        undoesMovement = false;

    }

        private void ShoutXeque(PlayerPieces colorPiece)
    {
        foreach (PieceConfig piece in boardController.allPieces)
        {
            //if (piece.pieceType == PieceType.KING)
                //continue;

            if (piece.pieceColor == colorPiece)
            {
                piece.kingInCheck = kingConfig.kingInCheck;
                piece.xeckedBy.Clear();
                foreach (PieceConfig item in listThreat)
                {
                    piece.xeckedBy.Add(item);
                }
            }
        }
    }
}
