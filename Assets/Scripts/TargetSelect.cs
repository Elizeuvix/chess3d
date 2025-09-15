using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TargetSelect : MonoBehaviour
{
    // LEGACY: This script is being phased out by CoreInputController.
    public new Camera camera; // kept legacy; could be renamed to mainCamera if still used
    bool hasSelected = false;
    public PlayerPieces playerPieces;
    bool startMove = false;    

    //Capture Action
    [HideInInspector] public Transform squareClicked = null;
    public GameObject selectedObject = null;
    private Vector3 objective;
    public PieceConfig pieceToMove = null;
    private PieceConfig pieceToCapture = null;
    bool isCaptureState = false;

    SpecialMovement specialMovement;
    public PieceConfig specialPieceToMove = null;
    private Vector3 specialObjective;
    bool startSpecialMovement = false;

    private void Start()
    {
        camera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                if(hitInfo.collider.gameObject.GetComponent<PieceConfig>() != null)
                {
                    if (hitInfo.collider.gameObject.GetComponent<PieceConfig>().pieceType != PieceType.SQUARE &&
                        hitInfo.collider.gameObject.GetComponent<PieceConfig>().pieceColor == playerPieces)
                    {
                        pieceToMove = hitInfo.collider.gameObject.GetComponent<PieceConfig>();
                        pieceToMove.SelectThisObject(!hasSelected);
                        SelectThis(!hasSelected, hitInfo.collider.gameObject);

                    }
                    else if (hitInfo.collider.gameObject.GetComponent<PieceConfig>().pieceType == PieceType.SQUARE)
                    {
                        //Tentar mover a pe�a selecionada
                        objective = new Vector3 (hitInfo.collider.transform.position.x, 0.5f, hitInfo.collider.transform.position.z);
                        squareClicked = hitInfo.collider.transform;
                        
                        PieceConfig toTest = squareClicked.GetComponent<PieceConfig>();
                        if (toTest.IsAllowedMovement(toTest) && selectedObject != null)
                        {                   
                            squareClicked.GetComponent<PieceConfig>().pieceMovingCollider = selectedObject;
                            pieceToMove.isMoving = true;

                            if (selectedObject.GetComponent<PieceConfig>().pieceType == PieceType.KING)
                            {
                                specialMovement = selectedObject.GetComponent<SpecialMovement>();
                                if (specialMovement.isAllowed)
                                {
                                    specialPieceToMove = specialMovement.rook;                                   
                                    specialObjective = new Vector3(specialMovement.specialSquare.transform.position.x, 0.5f, specialMovement.specialSquare.transform.position.z);

                                    startSpecialMovement = true;
                                }
                            }
                            UpdateSquare(pieceToMove);
                            startMove = true;
                        }
                    } 
                    else if(hitInfo.collider.gameObject.GetComponent<PieceConfig>().pieceColor != playerPieces)
                    {                        
                        pieceToCapture = hitInfo.collider.gameObject.GetComponent<PieceConfig>();
                        BoardController boardController = GameObject.Find("ChessBoard").GetComponent<BoardController>();

                        foreach (PieceConfig piece in boardController.allSquares)
                        {
                            if (piece.columnPos == pieceToCapture.columnPos && piece.linePos == pieceToCapture.linePos)
                            {                                                            
                                squareClicked = piece.transform;                                
                            }
                        }
                        objective = new Vector3(squareClicked.position.x, 0.5f, squareClicked.position.z);

                        PieceConfig toTest = squareClicked.GetComponent<PieceConfig>();
                        if (toTest.IsAllowedMovement(toTest) && selectedObject != null) 
                        { 
                            squareClicked.GetComponent<PieceConfig>().pieceMovingCollider = selectedObject;
                            UpdateSquare(pieceToMove);
                            pieceToMove.isMoving = true;
                            isCaptureState = true;
                            startMove = true;                            
                        }
                    }                   
                }
            }
        }      

        if (selectedObject != null)
        {
            startMove = selectedObject.GetComponent<PieceConfig>().isMoving;
        }
        else
        {
            startMove = false;
        }

        
        if (isCaptureState)
        {
            //GameObject.Find("Black_King_E_8").GetComponent<PieceConfig>().pieceColor == PlayerPieces.WHITE
            //if (pieceToCapture.kingInCheck) { return; }
            StartCoroutine(pieceToCapture.Capture(pieceToCapture, pieceToMove));
            isCaptureState = false;
        }
    }

    private void FixedUpdate()
    {        
        if (!startMove) { return; }
        selectedObject.transform.LookAt(objective);
        selectedObject.transform.Translate(0, 0, 1.0f, Space.Self);

        if (startSpecialMovement)
        {
            specialPieceToMove.transform.LookAt(specialObjective);
            specialPieceToMove.transform.Translate(0, 0, 1.5f, Space.Self);
        }
    }

    private void UpdateSquare(PieceConfig piece)
    {
        foreach (PieceConfig item in GetComponent<BoardController>().allSquares)
        {
            if(item.linePos == piece.linePos && item.columnPos == piece.columnPos)
            {
                item.occupiedBy = this.gameObject;
            }
        }
    }

    public bool HasSelected()
    {
        return hasSelected;
    }

    public void StopMove()
    {
        selectedObject = null;
        startMove = false;
        startSpecialMovement = false;

        if (playerPieces == PlayerPieces.WHITE)
        {
            playerPieces = PlayerPieces.BLACK;
        }
        else
        {
            playerPieces = PlayerPieces.WHITE;
        }
    }

    public void CaptureState(bool state)
    {
        isCaptureState = state;
    }

    public void SetSelected(bool sel)
    {
        hasSelected = sel;
    }

    public void SelectThis(bool sel, GameObject selected)
    {
        hasSelected = sel;

        if (hasSelected)        
            selectedObject = selected;
        else
            selectedObject = null;
    }

    public GameObject GetSelected()
    {
        return selectedObject;
    }
}
