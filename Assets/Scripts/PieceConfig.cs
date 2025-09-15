using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PieceConfig : MonoBehaviour
{
    void Awake()
    {
        // LEGACY: desativado para evitar eventos OnMouse e lógica antiga interferindo no novo core.
        // Caso precise reativar para referência histórica, remova esta linha.
        enabled = false;
    }
    public PieceType pieceType;
    public PlayerPieces pieceColor;
    public string pieceName;
    public Renderer rend;
    public string pieceCode;
    public TargetSelect targetSelect;

    Color baseColor;
    // (Removed legacy isPreSelected flag; hover visuals retained without tracking.)
    bool isSelected = false;

    [Header("Movement")]
    public int linePos;
    public int columnPos;
    public bool isAllowedMovement = false;
    [HideInInspector]
    public bool isMoving = false;
    [HideInInspector]
    public GameObject pieceMovingCollider = null;

    [Header("Special Movement")]
    public MovimentSpecial specialMove = MovimentSpecial.NONE;
    PieceConfig specialPieceToMove = null;
    public PieceConfig squareNearKing = null;

    [Header("Attack")]
    public bool kingInCheck = false;
    public PieceConfig enemyKing = null;
    public List<PieceConfig> xeckedBy;
    public List<GameObject> attacking;
    //Square
    public GameObject occupiedBy = null;
    private BoardController boardController;

    //Captured Piece
    // Removed legacy capture flag (was never read). Capture will be represented by absence of piece in core state.
    // bool isCaptured = false; // (Removed) LEGACY flag; real capture flow will move to core state
    //[HideInInspector] 
    public PieceConfig capturedBy = null;
    //[HideInInspector] 
    public PieceConfig lastSquare = null; // Tracks previous square (legacy path tracking)

    [Header("Dead Peaces")]
    public int deadPosition = 999;
    DeadPiecesControl deadControl;

    private void Start()
    {
        deadPosition = 999;        

        if (transform.name.Contains("Rook"))
        {
            pieceName = "Rook";
        }
        else if (transform.name.Contains("Knight"))
        {
            pieceName = "Knight";
        }
        else if (transform.name.Contains("Bishop"))
        {
            pieceName = "Bishop";
        }
        else if (transform.name.Contains("Queen"))
        {
            pieceName = "Queen";
        }
        else if (transform.name.Contains("King"))
        {
            pieceName = "King";
        }
        else if (transform.name.Contains("Pawn"))
        {
            pieceName = "Pawn";
        }
        else
        {
            pieceName = "Square";
        }

        if (pieceType == PieceType.SQUARE)
            rend = transform.GetComponent<Renderer>();
        else
            rend = transform.GetChild(0).GetComponent<Renderer>();
    }

    public void StartPiece(Material color, string code)
    {
        boardController = GameObject.Find("ChessBoard").GetComponent<BoardController>();
        deadControl = GameObject.Find("ChessBoard").GetComponent<DeadPiecesControl>();
        xeckedBy = new List<PieceConfig>();
        kingInCheck = false;        

        if (pieceType == PieceType.SQUARE)
        {
            rend = transform.GetComponent<Renderer>();
        }
        else
        {
            rend = transform.GetChild(0).GetComponent<Renderer>();
        }            

        this.rend.material = color;
        this.pieceCode = code;

        baseColor = rend.material.color;
        targetSelect = GameObject.Find("ChessBoard").transform.GetComponent<TargetSelect>();
    }

    private void Xequemate()
    {
        Debug.Log("Xequemate!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(pieceMovingCollider == null) { return; }

        if(this.pieceType == PieceType.SQUARE)
        {
            if (other.gameObject == pieceMovingCollider)
            {
                PieceConfig config = pieceMovingCollider.GetComponent<PieceConfig>();
                if (!config.IsAllowedMovement(this)) {
                    Debug.Log("Movimento n�o permitido!");
                    return; 
                }
            
                UpdateChessBoard(config, this);
                targetSelect.StopMove();

                config.isMoving = false;
                config.NormalizeColor();
                this.NormalizeColor();
                occupiedBy = pieceMovingCollider;
                config.SelectThisObject(false);
                targetSelect.SetSelected(false);
                pieceMovingCollider.transform.position = this.transform.position;
                pieceMovingCollider.transform.Translate(0f, 0.5f, 0f);
                pieceMovingCollider = null;
            }            
        }        
    }    

    public void SelectThisObject(bool sel)
    {
           isSelected = sel;
    }

    private void OnMouseEnter()
    {
        if (targetSelect == null || rend == null) return; // legado desativado
        if (targetSelect.HasSelected() && pieceType == PieceType.SQUARE) 
        {
            this.rend.material.color = Color.green;
        }
        else if (!targetSelect.HasSelected() && pieceType != PieceType.SQUARE)
        {
            if (pieceColor == targetSelect.playerPieces)
            {
                this.rend.material.color = Color.green;
            }
            else
            {
                this.rend.material.color = Color.yellow;
            }
        }        
    }

    private void OnMouseExit()
    {
        if (targetSelect == null || rend == null) return; // legado desativado
        if (targetSelect.HasSelected() && pieceType == PieceType.SQUARE)
        {
            NormalizeColor();
        }
        else if (!targetSelect.HasSelected() && pieceType != PieceType.SQUARE)
        {
            NormalizeColor();
        }
    }

    public void NormalizeColor()
    {
        this.rend.material.color = baseColor;
    }

    private void SpecialMovement()
    {
        if (specialMove == MovimentSpecial.NONE) return;

        switch (specialMove)
        {
            case MovimentSpecial.ANPASSANT:
                break;
            case MovimentSpecial.CASTLING:
                specialPieceToMove.transform.position = new Vector3(squareNearKing.transform.position.x, 0.5f, squareNearKing.transform.position.z);
                squareNearKing.occupiedBy = specialPieceToMove.gameObject;
                break;
        }
    }

  
    public bool IsAllowedMovement(PieceConfig squareClicked, PieceConfig selectecPiece = null)
    {
        if (targetSelect.GetSelected() == null) { return false; }
        if(selectecPiece == null) selectecPiece = targetSelect.GetSelected().GetComponent<PieceConfig>();

        if (squareClicked.pieceType != PieceType.SQUARE)
        {
            foreach (PieceConfig item in boardController.allSquares)
            {
                if(squareClicked == item.occupiedBy.GetComponent<PieceConfig>())
                {
                    squareClicked = item;
                }
            }
        }
        
        attacking = new List<GameObject>();
        attacking.Clear();

        switch (selectecPiece.pieceType)
        {
            case PieceType.BISHOP:
                attacking.Clear();
                foreach (PieceConfig atc in BishopAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
            case PieceType.KING:
                attacking.Clear();
                foreach (PieceConfig atc in KingAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
            case PieceType.KNIGHT:
                attacking.Clear();
                foreach (PieceConfig atc in KnightAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
            case PieceType.PAWN:
                attacking.Clear();
                foreach (PieceConfig atc in PawnAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
            case PieceType.QUEEN:
                attacking.Clear();
                foreach (PieceConfig atc in QueenAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
            case PieceType.ROOK:
                attacking.Clear();
                foreach (PieceConfig atc in RookAttack(selectecPiece))
                {
                    attacking.Add(atc.gameObject);
                }
                break;
        }

        //returns
        foreach(GameObject sq in attacking)
        {
            if(sq.GetComponent<PieceConfig>() == squareClicked)
            {
                if (squareClicked.occupiedBy.name != "ChessBoard"){                
                    if (squareClicked.occupiedBy.GetComponent<PieceConfig>().pieceColor != selectecPiece.pieceColor)
                    {
                        if(selectecPiece.pieceType == PieceType.KNIGHT || selectecPiece.pieceType == PieceType.PAWN)
                        {
                            return true;
                        }                        
                        else if (selectecPiece.pieceType == PieceType.ROOK)
                        {
                            return CheckCleanVertHor(squareClicked, selectecPiece);               
                        }
                        else if (selectecPiece.pieceType == PieceType.BISHOP)
                        {
                            return CheckCleanDiagonal(squareClicked, selectecPiece);
                        }
                        else if (selectecPiece.pieceType == PieceType.QUEEN)
                        {
                            if(selectecPiece.linePos == squareClicked.linePos || selectecPiece.columnPos == squareClicked.columnPos)
                            {
                                return CheckCleanVertHor(squareClicked, selectecPiece);
                            }
                            else
                            {
                                return CheckCleanDiagonal(squareClicked, selectecPiece);
                            }
                        }
                        else if (selectecPiece.pieceType == PieceType.KING &&
                           !SquareIsInAttack(squareClicked, selectecPiece) ||
                           selectecPiece.GetComponent<SpecialMovement>().CastlingMovement(squareClicked) &&
                           !SquareIsInAttack(squareClicked, selectecPiece))
                        {
                            return true;
                        }
                    }
                }
                else //square est� vazio
                {
                    if (selectecPiece.pieceType == PieceType.KNIGHT)
                    {                       
                        return true;
                    }
                    else if (selectecPiece.pieceType == PieceType.KING && !SquareIsInAttack(squareClicked, selectecPiece))
                    {
                        return true;
                    }
                    else if (selectecPiece.pieceType == PieceType.ROOK)
                    {
                        return CheckCleanVertHor(squareClicked, selectecPiece);
                    }
                    else if (selectecPiece.pieceType == PieceType.BISHOP)
                    {
                        return CheckCleanDiagonal(squareClicked, selectecPiece);
                    }
                    else if (selectecPiece.pieceType == PieceType.QUEEN)
                    {
                        if (selectecPiece.linePos == squareClicked.linePos || selectecPiece.columnPos == squareClicked.columnPos)
                        {
                            return CheckCleanVertHor(squareClicked, selectecPiece);
                        }
                        else
                        {
                            return CheckCleanDiagonal(squareClicked, selectecPiece);
                        }
                    }
                    else if (selectecPiece.pieceType == PieceType.PAWN)
                    {                      
                        return false;
                    }
                }
            }
        }

        //Pawn special movements
        if(selectecPiece.pieceType == PieceType.PAWN)
        {
            if (selectecPiece.pieceColor == PlayerPieces.WHITE)
            {
                //WHITE normal movement
                if(squareClicked.linePos <= selectecPiece.linePos){
                    return false;
                }
                else 
                {
                    if (selectecPiece.linePos == 2)
                    {
                        if (squareClicked.columnPos == selectecPiece.columnPos &&
                            squareClicked.linePos == selectecPiece.linePos + 1 &&
                            squareClicked.occupiedBy.name == "ChessBoard" ||
                            squareClicked.columnPos == selectecPiece.columnPos &&
                            squareClicked.linePos == selectecPiece.linePos + 2 &&
                            boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                            squareClicked.occupiedBy.name == "ChessBoard")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if(squareClicked.columnPos == selectecPiece.columnPos &&
                            squareClicked.linePos == selectecPiece.linePos + 1 &&
                            squareClicked.occupiedBy.name == "ChessBoard")
                        {
                            return true;
                        }
                    }
                }
            }
            else //BLACK pieces
            {
                //BLACK normal movement
                if (squareClicked.linePos >= selectecPiece.linePos)
                {
                    return false;
                }
                else
                {
                    if (selectecPiece.linePos == 7)
                    {
                        if (squareClicked.columnPos == selectecPiece.columnPos &&
                        squareClicked.linePos == selectecPiece.linePos - 1 &&
                        squareClicked.occupiedBy.name == "ChessBoard" ||
                        squareClicked.columnPos == selectecPiece.columnPos &&
                        squareClicked.linePos == selectecPiece.linePos - 2 &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        squareClicked.occupiedBy.name == "ChessBoard")
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (squareClicked.columnPos == selectecPiece.columnPos &&
                        squareClicked.linePos == selectecPiece.linePos - 1 &&
                        squareClicked.occupiedBy.name == "ChessBoard")
                        {
                            return true;
                        }
                    }
                }
            }

            //An passant permission
            if (selectecPiece.pieceColor == PlayerPieces.WHITE && selectecPiece.linePos == 2 ||
                selectecPiece.pieceColor == PlayerPieces.BLACK && selectecPiece.linePos == 7)
            {
                if (squareClicked.linePos == selectecPiece.linePos && squareClicked.columnPos == selectecPiece.columnPos + 1 ||
                    squareClicked.linePos == selectecPiece.linePos && squareClicked.columnPos == selectecPiece.columnPos - 1)
                {
                    return true;
                }
            }
        }

        return false;
    }


    private bool CheckCleanVertHor(PieceConfig squareClicked, PieceConfig selectecPiece)
    {
        bool isClean = false;
        if (selectecPiece.columnPos == squareClicked.columnPos)
        {
            if (squareClicked.linePos > selectecPiece.linePos)
            {
                if (squareClicked.linePos == selectecPiece.linePos + 1 ||
                    squareClicked.linePos == selectecPiece.linePos + 2 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" ||
                    squareClicked.linePos == selectecPiece.linePos + 3 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 2).occupiedBy.name == "ChessBoard" ||
                    squareClicked.linePos == selectecPiece.linePos + 4 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 3).occupiedBy.name == "ChessBoard" ||
                    squareClicked.linePos == selectecPiece.linePos + 5 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 4).occupiedBy.name == "ChessBoard" ||
                    squareClicked.linePos == selectecPiece.linePos + 6 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 5).occupiedBy.name == "ChessBoard" ||
                    squareClicked.linePos == selectecPiece.linePos + 7 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 5).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos + 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
            else // menor
            {
                if (squareClicked.linePos == selectecPiece.linePos - 1 ||
                       squareClicked.linePos == selectecPiece.linePos - 2 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" ||
                       squareClicked.linePos == selectecPiece.linePos - 3 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 2).occupiedBy.name == "ChessBoard" ||
                       squareClicked.linePos == selectecPiece.linePos - 4 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 2).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 3).occupiedBy.name == "ChessBoard" ||
                       squareClicked.linePos == selectecPiece.linePos - 5 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 2).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 3).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 4).occupiedBy.name == "ChessBoard" ||
                       squareClicked.linePos == selectecPiece.linePos - 6 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 2).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 3).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 4).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 5).occupiedBy.name == "ChessBoard" ||
                       squareClicked.linePos == selectecPiece.linePos - 7 && boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 1).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 2).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 3).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 4).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 5).occupiedBy.name == "ChessBoard" &&
                           boardController.GetSquare(selectecPiece.columnPos, selectecPiece.linePos - 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
        }
        else //Line is equal
        {
            if (squareClicked.columnPos > selectecPiece.columnPos)
            {
                if (squareClicked.columnPos == selectecPiece.columnPos + 1 ||
                    squareClicked.columnPos == selectecPiece.columnPos + 2 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos + 3 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos + 4 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos + 5 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos + 6 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 5, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos + 7 && boardController.GetSquare(selectecPiece.columnPos + 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 5, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos + 6, selectecPiece.linePos).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
            else
            {
                if (squareClicked.columnPos == selectecPiece.columnPos - 1 ||
                    squareClicked.columnPos == selectecPiece.columnPos - 2 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos - 3 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos - 4 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos - 5 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos - 6 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 5, selectecPiece.linePos).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == selectecPiece.columnPos - 7 && boardController.GetSquare(selectecPiece.columnPos - 1, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 2, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 3, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 4, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 5, selectecPiece.linePos).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(selectecPiece.columnPos - 6, selectecPiece.linePos).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
        }

        return isClean;
    }

    private bool CheckCleanDiagonal(PieceConfig squareClicked, PieceConfig pieceSelected)
    {
        bool isClean = false;

        if (squareClicked.columnPos > pieceSelected.columnPos)
        {
            if (squareClicked.linePos > pieceSelected.linePos)
            {
                if(squareClicked.columnPos == pieceSelected.columnPos + 1 && squareClicked.linePos == pieceSelected.linePos + 1 ||
                    squareClicked.columnPos == pieceSelected.columnPos + 2 && squareClicked.linePos == pieceSelected.linePos + 2 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 3 && squareClicked.linePos == pieceSelected.linePos + 3 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 4 && squareClicked.linePos == pieceSelected.linePos + 4 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 5 && squareClicked.linePos == pieceSelected.linePos + 5 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 6 && squareClicked.linePos == pieceSelected.linePos + 6 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 5, pieceSelected.linePos + 5).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 7 && squareClicked.linePos == pieceSelected.linePos + 7 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 5, pieceSelected.linePos + 5).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 6, pieceSelected.linePos + 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
            else //line � menor
            {
                if (squareClicked.columnPos == pieceSelected.columnPos + 1 && squareClicked.linePos == pieceSelected.linePos - 1 ||
                    squareClicked.columnPos == pieceSelected.columnPos + 2 && squareClicked.linePos == pieceSelected.linePos - 2 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 3 && squareClicked.linePos == pieceSelected.linePos - 3 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 4 && squareClicked.linePos == pieceSelected.linePos - 4 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 5 && squareClicked.linePos == pieceSelected.linePos - 5 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 6 && squareClicked.linePos == pieceSelected.linePos - 6 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 5, pieceSelected.linePos - 5).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos + 7 && squareClicked.linePos == pieceSelected.linePos - 7 &&
                        boardController.GetSquare(pieceSelected.columnPos + 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 5, pieceSelected.linePos - 5).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos + 6, pieceSelected.linePos - 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
        }
        else //coluna � menor
        {
            if (squareClicked.linePos > pieceSelected.linePos)
            {
                if (squareClicked.columnPos == pieceSelected.columnPos - 1 && squareClicked.linePos == pieceSelected.linePos + 1 ||
                    squareClicked.columnPos == pieceSelected.columnPos - 2 && squareClicked.linePos == pieceSelected.linePos + 2 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 3 && squareClicked.linePos == pieceSelected.linePos + 3 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 4 && squareClicked.linePos == pieceSelected.linePos + 4 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 5 && squareClicked.linePos == pieceSelected.linePos + 5 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 6 && squareClicked.linePos == pieceSelected.linePos + 6 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 5, pieceSelected.linePos + 5).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 7 && squareClicked.linePos == pieceSelected.linePos + 7 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos + 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos + 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos + 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos + 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 5, pieceSelected.linePos + 5).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 6, pieceSelected.linePos + 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
            else //line � menor
            {
                if (squareClicked.columnPos == pieceSelected.columnPos - 1 && squareClicked.linePos == pieceSelected.linePos - 1 ||
                    squareClicked.columnPos == pieceSelected.columnPos - 2 && squareClicked.linePos == pieceSelected.linePos - 2 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 3 && squareClicked.linePos == pieceSelected.linePos - 3 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 4 && squareClicked.linePos == pieceSelected.linePos - 4 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 5 && squareClicked.linePos == pieceSelected.linePos - 5 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 6 && squareClicked.linePos == pieceSelected.linePos - 6 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 5, pieceSelected.linePos - 5).occupiedBy.name == "ChessBoard" ||
                    squareClicked.columnPos == pieceSelected.columnPos - 7 && squareClicked.linePos == pieceSelected.linePos - 7 &&
                        boardController.GetSquare(pieceSelected.columnPos - 1, pieceSelected.linePos - 1).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 2, pieceSelected.linePos - 2).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 3, pieceSelected.linePos - 3).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 4, pieceSelected.linePos - 4).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 5, pieceSelected.linePos - 5).occupiedBy.name == "ChessBoard" &&
                        boardController.GetSquare(pieceSelected.columnPos - 6, pieceSelected.linePos - 6).occupiedBy.name == "ChessBoard")
                {
                    isClean = true;
                }
            }
        }

        return isClean;
    }

    public bool SquareIsInAttack(PieceConfig squareClicked, PieceConfig pieceSelected)
    {
        GameObject[] allPieces = GameObject.FindGameObjectsWithTag("Pieces");
        List<PieceConfig> enemyPiece = new List<PieceConfig>();
        enemyPiece.Clear();
        bool isInAttack = false;

        foreach(GameObject obj in allPieces)
        {
            if(obj.GetComponent<PieceConfig>().pieceColor != pieceSelected.pieceColor)
            {
                enemyPiece.Add(obj.GetComponent<PieceConfig>());
            }
        }

        foreach (PieceConfig obj in enemyPiece)
        {
            switch (obj.pieceType){
                case PieceType.BISHOP:
                    foreach (PieceConfig atc in BishopAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
                case PieceType.KING:
                    foreach (PieceConfig atc in KingAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
                case PieceType.KNIGHT:
                    foreach (PieceConfig atc in KnightAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
                case PieceType.PAWN:
                    foreach (PieceConfig atc in PawnAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
                case PieceType.QUEEN:
                    foreach (PieceConfig atc in QueenAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
                case PieceType.ROOK:
                    foreach (PieceConfig atc in RookAttack(obj))
                    {
                        if (squareClicked == atc) isInAttack = true;
                    }
                    break;
            }
        }

        return isInAttack;
    }

    public List<PieceConfig> PawnAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();

        if (piece.pieceColor == PlayerPieces.WHITE)
        {
            foreach (PieceConfig spc in boardController.allSquares)
            {
                if (spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos + 1 ||
                    spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos + 1)
                    squares.Add(spc);
            }
        }
        else
        {
            foreach (PieceConfig spc in boardController.allSquares)
            {
                if (spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos - 1 ||
                    spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos - 1)
                    squares.Add(spc);
            }
        }

        return squares;
    }
    

    public List<PieceConfig> BishopAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();

        foreach (PieceConfig spc in boardController.allSquares)
        {
            if(spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos + 2 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos + 2 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos + 3 && spc.linePos == piece.linePos + 3 ||
                spc.columnPos == piece.columnPos - 3 && spc.linePos == piece.linePos + 3 ||
                spc.columnPos == piece.columnPos + 3 && spc.linePos == piece.linePos - 3 ||
                spc.columnPos == piece.columnPos - 3 && spc.linePos == piece.linePos - 3 ||
                spc.columnPos == piece.columnPos + 4 && spc.linePos == piece.linePos + 4 ||
                spc.columnPos == piece.columnPos - 4 && spc.linePos == piece.linePos + 4 ||
                spc.columnPos == piece.columnPos + 4 && spc.linePos == piece.linePos - 4 ||
                spc.columnPos == piece.columnPos - 4 && spc.linePos == piece.linePos - 4 ||
                spc.columnPos == piece.columnPos + 5 && spc.linePos == piece.linePos + 5 ||
                spc.columnPos == piece.columnPos - 5 && spc.linePos == piece.linePos + 5 ||
                spc.columnPos == piece.columnPos + 5 && spc.linePos == piece.linePos - 5 ||
                spc.columnPos == piece.columnPos - 5 && spc.linePos == piece.linePos - 5 ||
                spc.columnPos == piece.columnPos + 6 && spc.linePos == piece.linePos + 6 ||
                spc.columnPos == piece.columnPos - 6 && spc.linePos == piece.linePos + 6 ||
                spc.columnPos == piece.columnPos + 6 && spc.linePos == piece.linePos - 6 ||
                spc.columnPos == piece.columnPos - 6 && spc.linePos == piece.linePos - 6 ||
                spc.columnPos == piece.columnPos + 7 && spc.linePos == piece.linePos + 7 ||
                spc.columnPos == piece.columnPos - 7 && spc.linePos == piece.linePos + 7 ||
                spc.columnPos == piece.columnPos + 7 && spc.linePos == piece.linePos - 7 ||
                spc.columnPos == piece.columnPos - 7 && spc.linePos == piece.linePos - 7)
                squares.Add(spc);
        }

        foreach (PieceConfig pc in squares)
        {
            //Debug.Log(pc.transform.gameObject.name);
        }

        return squares;
    }

    public List<PieceConfig> KnightAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();
        foreach (PieceConfig spc in boardController.allSquares)
        {
            if(spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos +2 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos + 2 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos + 1)
                squares.Add(spc);
        }

        foreach (PieceConfig pc in squares)
        {
            //Debug.Log(pc.transform.gameObject.name);
        }

        return squares;
    }

    public List<PieceConfig> RookAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();

        foreach (PieceConfig spc in boardController.allSquares)
        {
            if (spc.columnPos == piece.columnPos ||
                spc.linePos == piece.linePos)
                squares.Add(spc);
        }

        foreach (PieceConfig pc in squares)
        {
            //Debug.Log(pc.transform.gameObject.name);
        }

        return squares;
    }

    public List<PieceConfig> QueenAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();

        foreach (PieceConfig spc in boardController.allSquares)
        {
            if (spc.columnPos == piece.columnPos ||
                spc.linePos == piece.linePos ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos + 2 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos + 2 ||
                spc.columnPos == piece.columnPos + 2 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos - 2 && spc.linePos == piece.linePos - 2 ||
                spc.columnPos == piece.columnPos + 3 && spc.linePos == piece.linePos + 3 ||
                spc.columnPos == piece.columnPos - 3 && spc.linePos == piece.linePos + 3 ||
                spc.columnPos == piece.columnPos + 3 && spc.linePos == piece.linePos - 3 ||
                spc.columnPos == piece.columnPos - 3 && spc.linePos == piece.linePos - 3 ||
                spc.columnPos == piece.columnPos + 4 && spc.linePos == piece.linePos + 4 ||
                spc.columnPos == piece.columnPos - 4 && spc.linePos == piece.linePos + 4 ||
                spc.columnPos == piece.columnPos + 4 && spc.linePos == piece.linePos - 4 ||
                spc.columnPos == piece.columnPos - 4 && spc.linePos == piece.linePos - 4 ||
                spc.columnPos == piece.columnPos + 5 && spc.linePos == piece.linePos + 5 ||
                spc.columnPos == piece.columnPos - 5 && spc.linePos == piece.linePos + 5 ||
                spc.columnPos == piece.columnPos + 5 && spc.linePos == piece.linePos - 5 ||
                spc.columnPos == piece.columnPos - 5 && spc.linePos == piece.linePos - 5 ||
                spc.columnPos == piece.columnPos + 6 && spc.linePos == piece.linePos + 6 ||
                spc.columnPos == piece.columnPos - 6 && spc.linePos == piece.linePos + 6 ||
                spc.columnPos == piece.columnPos + 6 && spc.linePos == piece.linePos - 6 ||
                spc.columnPos == piece.columnPos - 6 && spc.linePos == piece.linePos - 6 ||
                spc.columnPos == piece.columnPos + 7 && spc.linePos == piece.linePos + 7 ||
                spc.columnPos == piece.columnPos - 7 && spc.linePos == piece.linePos + 7 ||
                spc.columnPos == piece.columnPos + 7 && spc.linePos == piece.linePos - 7 ||
                spc.columnPos == piece.columnPos - 7 && spc.linePos == piece.linePos - 7)
                squares.Add(spc);
        }

        foreach (PieceConfig pc in squares)
        {
            //Debug.Log(pc.transform.gameObject.name);
        }

        return squares;
    }

    public List<PieceConfig> KingAttack(PieceConfig piece)
    {
        List<PieceConfig> squares = new List<PieceConfig>();
        PieceConfig squareClicked = null;
        PieceConfig rookCastling = null;
        if (targetSelect.squareClicked != null)
        {
            squareClicked = targetSelect.squareClicked.GetComponent<PieceConfig>();       

            if (!piece.transform.GetComponent<SpecialMovement>().alreadyMoved &&
                    squareClicked.columnPos == 3 && squareClicked.linePos == piece.linePos ||
                    !piece.transform.GetComponent<SpecialMovement>().alreadyMoved &&
                    squareClicked.columnPos == 7 && squareClicked.linePos == piece.linePos)
            {
                if(squareClicked.columnPos > piece.columnPos && piece.linePos == 1)
                {
                    rookCastling = GameObject.Find("White_Rook_H_1").GetComponent<PieceConfig>();
                    squareNearKing = GameObject.Find("Square_6_1").GetComponent<PieceConfig>();
                }
                else if (squareClicked.columnPos < piece.columnPos && piece.linePos == 1)
                {
                    rookCastling = GameObject.Find("White_Rook_A_1").GetComponent<PieceConfig>();
                    squareNearKing = GameObject.Find("Square_4_1").GetComponent<PieceConfig>();
                }
                else if (squareClicked.columnPos > piece.columnPos && piece.linePos == 8)
                {
                    rookCastling = GameObject.Find("Black_Rook_H_8").GetComponent<PieceConfig>();
                    squareNearKing = GameObject.Find("Square_6_8").GetComponent<PieceConfig>();
                }
                else if (squareClicked.columnPos < piece.columnPos && piece.linePos == 8)
                {
                    rookCastling = GameObject.Find("Black_Rook_A_8").GetComponent<PieceConfig>();
                    squareNearKing = GameObject.Find("Square_4_8").GetComponent<PieceConfig>();
                }
           
                if (BetweenPiecesIsEmpty(rookCastling, piece) && 
                    !rookCastling.GetComponent<SpecialMovement>().alreadyMoved)
                {
                    specialMove = MovimentSpecial.CASTLING;
                    specialPieceToMove = rookCastling;
                    squares.Add(squareClicked);
                }                
            }
        }

        foreach (PieceConfig spc in boardController.allSquares)
        {            
            if (spc.columnPos == piece.columnPos && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos + 1 ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos - 1 ||
                spc.columnPos == piece.columnPos + 1 && spc.linePos == piece.linePos ||
                spc.columnPos == piece.columnPos - 1 && spc.linePos == piece.linePos)
                squares.Add(spc);            
        }        

        return squares;
    }

    
    private bool UndoesXeque(PieceConfig squareClicked, PieceConfig kingXequed, PieceConfig attackingPiece)
    {
        var listas = new List<List<PieceConfig>>();
        Debug.Log("Tentar sair do xeque!");

        List<PieceConfig> betweenPieces = new List<PieceConfig>();
        betweenPieces = BetweenPieces(kingXequed, attackingPiece);

        foreach (PieceConfig item in kingXequed.xeckedBy)
        {
            if(item.pieceType == PieceType.KNIGHT &&
                !squareClicked.occupiedBy.name.Contains("Knight"))
            {
                Debug.Log("Mate o cavalo!");
                return false;
            }
        }

        switch (kingXequed.xeckedBy.Count)
        {
            case 1:
                if (squareClicked.occupiedBy.GetComponent<PieceConfig>() == attackingPiece)
                {
                    return true;
                }
                else
                {
                    foreach (PieceConfig square in betweenPieces)
                    {
                        if (squareClicked == square)
                        {
                            Debug.Log("Isso ai");
                            return true;
                        }
                    }

                    return false;
                }

            case 2:
                List<PieceConfig> listBetween1 = new List<PieceConfig>();
                List<PieceConfig> listBetween2 = new List<PieceConfig>();
                listBetween1 = BetweenPieces(kingXequed, kingXequed.xeckedBy[0]);
                listBetween2 = BetweenPieces(kingXequed, kingXequed.xeckedBy[1]);

                PieceConfig commonSquare2 = (PieceConfig)listBetween1.Intersect(listBetween2);

                if(squareClicked == commonSquare2)
                {
                    Debug.Log("Intersection");
                    return true;
                }
                else
                {
                    return false;
                }

            case 3:
                List<PieceConfig> listBetween3 = new List<PieceConfig>();
                List<PieceConfig> listBetween4 = new List<PieceConfig>();
                List<PieceConfig> listBetween5 = new List<PieceConfig>();
                listBetween3 = BetweenPieces(kingXequed, kingXequed.xeckedBy[0]);
                listBetween4 = BetweenPieces(kingXequed, kingXequed.xeckedBy[1]);
                listBetween5 = BetweenPieces(kingXequed, kingXequed.xeckedBy[2]);

                PieceConfig commonSquare3 = (PieceConfig)listBetween3.Intersect(listBetween4).Intersect(listBetween5);

                if (squareClicked == commonSquare3)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            case 4:
                List<PieceConfig> listBetween6 = new List<PieceConfig>();
                List<PieceConfig> listBetween7 = new List<PieceConfig>();
                List<PieceConfig> listBetween8 = new List<PieceConfig>();
                List<PieceConfig> listBetween9 = new List<PieceConfig>();
                listBetween6 = BetweenPieces(kingXequed, kingXequed.xeckedBy[0]);
                listBetween7 = BetweenPieces(kingXequed, kingXequed.xeckedBy[1]);
                listBetween8 = BetweenPieces(kingXequed, kingXequed.xeckedBy[2]);
                listBetween9 = BetweenPieces(kingXequed, kingXequed.xeckedBy[4]);

                PieceConfig commonSquare4 = (PieceConfig)listBetween6.Intersect(listBetween7).Intersect(listBetween8).Intersect(listBetween9);

                if (squareClicked == commonSquare4)
                {
                    return true;
                }
                else
                {
                    return false;
                }
        }

        return false;
    }   

    private bool BetweenPiecesIsEmpty(PieceConfig target, PieceConfig pieceToMove)
    {     
        foreach (PieceConfig square in BetweenPieces(target, pieceToMove))
        {
            if (square.occupiedBy.name != "ChessBoard")
            {
                return false;
            }                         
        }
        return true;
    }

    private List<PieceConfig> BetweenPieces(PieceConfig target, PieceConfig pieceToMove)
    {     
        int lineMoving = pieceToMove.linePos;
        int columnMoving = pieceToMove.columnPos;
        int lineTarget = target.linePos;
        int columnTarget = target.columnPos;
        int diagonalSize = 0;

        List<PieceConfig> betweenPieces = new List<PieceConfig>();
        betweenPieces.Clear();

        //VERTICAL
        foreach (PieceConfig square in boardController.allSquares)
        {
            if(target.columnPos == pieceToMove.columnPos)
            {
                if(target.linePos > pieceToMove.linePos)
                {

                    if (square.columnPos == target.columnPos && 
                        square.linePos == lineMoving + 1 &&
                        square.linePos < target.linePos)
                    {
                        betweenPieces.Add(square);
                        lineMoving = square.linePos;
                    }
                }
                else if (target.linePos < pieceToMove.linePos)
                {

                    if (square.columnPos == target.columnPos &&
                        square.linePos == lineTarget + 1 &&
                        square.linePos < pieceToMove.linePos)
                    {
                        betweenPieces.Add(square);
                        lineTarget = square.linePos;
                    }
                }
            }  
        }

        //HORIZONTAL
        for (int i = 0; i < boardController.allSquares.Count; i++)
        {
            if(target.linePos == pieceToMove.linePos && 
                pieceToMove.linePos == boardController.allSquares[i].linePos &&
                target.columnPos > pieceToMove.columnPos &&
                boardController.allSquares[i].columnPos == columnMoving + 1)
            {
                if (boardController.allSquares[i].columnPos >= target.columnPos) continue;

                betweenPieces.Add(boardController.allSquares[i]);
                columnMoving = boardController.allSquares[i].columnPos;

            }else if (target.linePos == pieceToMove.linePos &&
                pieceToMove.linePos == boardController.allSquares[i].linePos &&
                target.columnPos < pieceToMove.columnPos &&
                boardController.allSquares[i].columnPos == columnTarget + 1)
            {
                if (boardController.allSquares[i].columnPos >= pieceToMove.columnPos) continue;

                betweenPieces.Add(boardController.allSquares[i]);
                columnTarget = boardController.allSquares[i].columnPos;
            }          
        }        

        //DIAGONAL
        if (target.linePos > pieceToMove.linePos && target.columnPos > pieceToMove.columnPos)
        {
            //Left -> right & bottom -> top
            diagonalSize = target.linePos - pieceToMove.linePos;

            for (int x = 0; x < diagonalSize - 1; x++)
            {
                for (int i = 0; i < boardController.allSquares.Count; i++)
                {
                    if (boardController.allSquares[i].linePos == lineMoving + 1 && 
                        boardController.allSquares[i].columnPos == columnMoving + 1)
                    {
                        betweenPieces.Add(boardController.allSquares[i]);
                    }
                }
                lineMoving++;
                columnMoving++;
            }            
        }
        else if (target.linePos < pieceToMove.linePos && target.columnPos < pieceToMove.columnPos)
        {
            //Right -> left & top -> bottom
            diagonalSize = pieceToMove.linePos - target.linePos;

            for (int x = diagonalSize; x > 1; x--)
            {
                for (int i = 0; i < boardController.allSquares.Count; i++)
                {
                    if (boardController.allSquares[i].linePos == lineMoving - 1 && 
                        boardController.allSquares[i].columnPos == columnMoving - 1)
                    {
                        betweenPieces.Add(boardController.allSquares[i]);
                    }
                }
                lineMoving--;
                columnMoving--;
            }            
        }
        else if (target.linePos > pieceToMove.linePos && target.columnPos < pieceToMove.columnPos)
        {
            //Right -> left & bottom -> top
            List<PieceConfig> listTemp = new List<PieceConfig>();
            diagonalSize = target.linePos - pieceToMove.linePos;

            for (int x = 0; x < diagonalSize - 1; x++)
            {
                for (int i = 0; i < boardController.allSquares.Count; i++)
                {
                    if (boardController.allSquares[i].linePos == lineMoving + 1)
                    {
                        listTemp.Add(boardController.allSquares[i]);
                    }
                }
                lineMoving++;
            }

            for (int x = 0; x < diagonalSize; x++)
            {
                foreach (PieceConfig square in listTemp)
                {
                    if (square.columnPos == columnTarget + 1 && 
                        square.linePos == lineTarget - 1)
                    {
                        betweenPieces.Add(square);
                    }
                }
                lineTarget--;
                columnTarget++;
            }
        }
        else if (target.linePos < pieceToMove.linePos && target.columnPos > pieceToMove.columnPos)
        {
            /*/Left -> Right & top -> bottom
            Debug.Log($"King line: {target.linePos} Pe�a line: {pieceToMove.linePos}");
            Debug.Log($"King col: {target.columnPos} Pe�a col: {pieceToMove.columnPos}");

            diagonalSize = pieceToMove.linePos - target.linePos;

            for (int x = 0; x < diagonalSize - 1; x++)
            {
                for (int i = 0; i < boardController.allSquares.Count; i++)
                {
                    if (boardController.allSquares[i].linePos == lineTarget + 1 && 
                        boardController.allSquares[i].columnPos == columnTarget - 1)
                    {
                        betweenPieces.Add(boardController.allSquares[i]);
                    }
                }
                lineTarget++;
                columnTarget--;
            }*/

            Debug.Log("Lista: " + betweenPieces.Count);
        }

        return betweenPieces;
    }

    public IEnumerator Capture(PieceConfig captured, PieceConfig capturedBy, bool capture = true)
    {
        yield return new WaitForSeconds(2);
        if (capture)
        {
            deadControl.AddDeadPeace(captured);
            captured.capturedBy = capturedBy;
        }                
    }

    private void TryUdoesCapture(PieceConfig captured, PieceConfig capturedBy)
    {
        if (capturedBy == null) return;

        if(capturedBy.lastSquare.linePos != capturedBy.linePos && 
            capturedBy.lastSquare.columnPos != capturedBy.columnPos)
        {
            captured.transform.position = new Vector3(captured.lastSquare.transform.position.x, 0.5f,
                                                        captured.lastSquare.transform.position.z);
            captured.lastSquare.occupiedBy = captured.gameObject;
            captured.capturedBy = null;
        }
    }

    public void UpdateChessBoard(PieceConfig movedPiece, PieceConfig newSquare)
    {
    // Removed unused local variable lastSquare (shadowed field) - not needed.

        foreach (PieceConfig square in boardController.allSquares)
        {
            if(movedPiece.linePos == square.linePos && movedPiece.columnPos == square.columnPos)
            {
                movedPiece.lastSquare = square;
                square.occupiedBy = GameObject.Find("ChessBoard");
            }
        }

        foreach (PieceConfig piece in boardController.allPieces)
        {
            piece.kingInCheck = false;
        }

        movedPiece.linePos = newSquare.linePos;
        movedPiece.columnPos = newSquare.columnPos;
        newSquare.occupiedBy = movedPiece.gameObject;

        if(movedPiece.pieceType == PieceType.ROOK || movedPiece.pieceType == PieceType.KING)
        {
            movedPiece.transform.GetComponent<SpecialMovement>().alreadyMoved = true;
            if(specialMove == MovimentSpecial.CASTLING)
            {
                SpecialMovement();
            }
        }


        KingListener blackListener = GameObject.Find("Black_King_E_8").GetComponent<KingListener>();
        KingListener whiteListener = GameObject.Find("White_King_E_1").GetComponent<KingListener>();
        blackListener.UpdateListThreat();
        whiteListener.UpdateListThreat();
        blackListener.lastPieceMoved = movedPiece;
        whiteListener.lastPieceMoved = movedPiece;
        blackListener.lastPieceLocal = movedPiece.lastSquare;
        whiteListener.lastPieceLocal = movedPiece.lastSquare;
    }
}

public enum MovimentSpecial
{
    NONE,
    CASTLING,
    ANPASSANT
}
