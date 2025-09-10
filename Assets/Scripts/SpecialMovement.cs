using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialMovement : MonoBehaviour
{
    //[HideInInspector] 
    public bool alreadyMoved = false;
    private BoardController boardController;
    [HideInInspector] public bool isAllowed = false;

    //pieces
    [HideInInspector] public PieceConfig specialSquare = null;
    //[HideInInspector] 
    public PieceConfig rook = null;
    [HideInInspector] public PieceConfig pawn = null;

    private void Awake()
    {
        alreadyMoved = false;
        isAllowed = false;
        boardController = GameObject.Find("ChessBoard").GetComponent<BoardController>();
    }

    private void AttackedAnPassant()
    {

    }

    private bool AlreadyMoved(SpecialMovement rook)
    {
        if (this.alreadyMoved || rook.alreadyMoved)
            return false;
        else
            return true;
    }

    private bool CheckBetweenPieces(PieceConfig squareClicked)
    {
        int column = squareClicked.columnPos;
        bool betweenIsClean = false;
        foreach (PieceConfig square in boardController.allSquares)
        {
            if(square.linePos == 1 || square.linePos == 8)
            {
                if (column == 3)
                {
                    if (square.columnPos == 2 &&
                        square.columnPos == 3 &&
                        square.columnPos == 4 &&
                        square.occupiedBy == GameObject.Find("ChessBoard"))
                    {
                        betweenIsClean = true;
                    }
                    else betweenIsClean = false;
                }else if (column == 7)
                {
                    if (square.columnPos == 6 &&
                        square.columnPos == 7 &&
                        square.occupiedBy == GameObject.Find("ChessBoard"))
                    {
                        betweenIsClean = true;
                    }
                    else betweenIsClean = false;
                }
                else betweenIsClean = false;
            }
        }

        return betweenIsClean;
    }

    public bool CastlingMovement(PieceConfig squareClicked)
    {
        PieceConfig king = GetComponent<PieceConfig>();        
        isAllowed = false;
        PieceConfig squareWhiteRight = null;
        PieceConfig squareWhiteLeft = null;
        PieceConfig squareBlackRight = null;
        PieceConfig squareBlackLeft = null;

        foreach (PieceConfig square in boardController.allSquares)
        {
            if(square.columnPos == 3 && square.linePos == 1)
            {
                squareWhiteLeft = square;
            }else if (square.columnPos == 7 && square.linePos == 1)
            {
                squareWhiteRight = square;
            }
            else if (square.columnPos == 3 && square.linePos == 8)
            {
                squareBlackLeft = square;
            }
            else if (square.columnPos == 7 && square.linePos == 8)
            {
                squareBlackRight = square;
            }
        }


            if (squareClicked.linePos == 1 && 
            squareClicked.columnPos ==3 &&
            king.pieceColor == PlayerPieces.WHITE)
        {
            rook = GameObject.Find("White_Rook_A_1").GetComponent<PieceConfig>();
            specialSquare = squareWhiteLeft;
        }else if (squareClicked.linePos == 1 &&
            squareClicked.columnPos == 7 &&
            king.pieceColor == PlayerPieces.WHITE)
        {
            rook = GameObject.Find("White_Rook_A_8").GetComponent<PieceConfig>();
            specialSquare = squareWhiteRight;
        }
        else if(squareClicked.linePos == 8 &&
            squareClicked.columnPos == 3 &&
            king.pieceColor == PlayerPieces.BLACK)
        {
            rook = GameObject.Find("Black_Rook_A_1").GetComponent<PieceConfig>();
            specialSquare = squareBlackLeft;
        }
        else if (squareClicked.linePos == 8 &&
            squareClicked.columnPos == 7 &&
            king.pieceColor == PlayerPieces.BLACK)
        {
            rook = GameObject.Find("Black_Rook_A_8").GetComponent<PieceConfig>();
            specialSquare = squareBlackRight;
        } else return false;

        //If the pieces (King an Rook) involved have been moved prevents there being the Castling Movement
        if (!AlreadyMoved(rook.transform.GetComponent<SpecialMovement>())) return false;
        //if the spaces between the pieces (King an Rook) involved are not unoccupied prevents there being the Castling Movement
        if (!CheckBetweenPieces(squareClicked)) return false;

        //Start Castling Movement
        isAllowed = true;
        return true;
    }
}