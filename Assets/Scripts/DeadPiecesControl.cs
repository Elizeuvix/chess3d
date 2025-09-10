using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadPiecesControl : MonoBehaviour
{
    public List<GameObject> pieceWhite;
    public List<GameObject> pieceBlack;
    public List<GameObject> squareWhite;
    public List<GameObject> squareBlack;
    private int whiteCounter = 0;
    private int whitePawnCounter = 0;
    private int whiteBishopCounter = 0;
    private int whiteKnightCounter = 0;
    private int whiteRookCounter = 0;
    private int whiteQueenCounter = 0;
    private int blackCounter = 0;
    private int blackPawnCounter = 0;
    private int blackBishopCounter = 0;
    private int blackKnightCounter = 0;
    private int blackRookCounter = 0;
    private int blackQueenCounter = 0;
    public PieceConfig lastCaptured = null;
    public bool isFakeCapture = false;

    private void Awake()
    {
        pieceWhite = new List<GameObject>();
        pieceBlack = new List<GameObject>();
        squareWhite = new List<GameObject>();
        squareBlack = new List<GameObject>();
    }
    private void Start()
    {
        
    }
    public void AddSquare(GameObject square)
    {
        string[] fullname = square.name.Split("_");

        if (fullname[1] == "49" || fullname[1] == "39" || fullname[1] == "29" || fullname[1] == "19" || fullname[1] == "09")
        {
            squareBlack.Add(square);
        }
        if (fullname[1] == "90" || fullname[1] == "80" || fullname[1] == "70" || fullname[1] == "60" || fullname[1] == "50")
        {
            squareWhite.Add(square);
        }

    }

    public void AddDeadPeace(PieceConfig piece)
    {
        Destroy(piece.transform.GetComponent<Collider>());
        Destroy(piece.transform.GetComponent<Rigidbody>());

        lastCaptured = piece;

        Debug.Log($"Peça {piece} foi capturada!");

        if (piece.pieceColor == PlayerPieces.WHITE)
        {

            whiteCounter++;

            switch (piece.pieceType)
            {
                case PieceType.ROOK:       
                    if(whiteRookCounter == 0)
                    {
                        pieceWhite.Add(piece.gameObject);
                    }
                    whiteRookCounter++;
                    break;
                case PieceType.BISHOP:
                    if (whiteBishopCounter == 0)
                    {
                        pieceWhite.Add(piece.gameObject);
                    }
                    whiteBishopCounter++;
                    break;
                case PieceType.KNIGHT:
                    if (whiteKnightCounter == 0)
                    {
                        pieceWhite.Add(piece.gameObject);
                    }

                    piece.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);

                    whiteKnightCounter++;
                    break;
                case PieceType.PAWN:
                    if (whitePawnCounter == 0)
                    {
                        pieceWhite.Add(piece.gameObject);
                    }
                    whitePawnCounter++;
                    break;
                case PieceType.QUEEN:
                    if (whiteQueenCounter == 0)
                    {
                        pieceWhite.Add(piece.gameObject);
                    }
                    whiteQueenCounter++;
                    break;
            }

            Dictionary<int, bool> dPos = new Dictionary<int, bool>();

            foreach (GameObject obj in pieceWhite)
            {
                dPos.Add(obj.GetComponent<PieceConfig>().deadPosition, true);

                if (obj.GetComponent<PieceConfig>().pieceType == piece.pieceType)
                {
                    piece.deadPosition = obj.GetComponent<PieceConfig>().deadPosition;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                if (!dPos.ContainsKey(i))
                {
                    piece.deadPosition = i;
                    piece.transform.position = squareWhite[i].transform.position;
                }
            }

            piece.transform.Rotate(0.0f, 0.0f, 90.0f, Space.Self);
            piece.transform.Translate(0.0f, 0.2f, 0.0f, Space.Self);
        }
        else
        {
            blackCounter++;
            switch (piece.pieceType)
            {
                case PieceType.ROOK:
                    if (blackRookCounter == 0)
                    {
                        pieceBlack.Add(piece.gameObject);
                    }
                    blackRookCounter++;
                    break;
                case PieceType.BISHOP:
                    if (blackBishopCounter == 0)
                    {
                        pieceBlack.Add(piece.gameObject);
                    }
                    blackBishopCounter++;
                    break;
                case PieceType.KNIGHT:
                    if (blackKnightCounter == 0)
                    {
                        pieceBlack.Add(piece.gameObject);
                    }

                    piece.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);

                    blackKnightCounter++;
                    break;
                case PieceType.PAWN:
                    if (blackPawnCounter == 0)
                    {
                        pieceBlack.Add(piece.gameObject);
                    }
                    blackPawnCounter++;
                    break;
                case PieceType.QUEEN:
                    if (blackQueenCounter == 0)
                    {
                        pieceBlack.Add(piece.gameObject);
                    }
                    blackQueenCounter++;
                    break;
            }

            Dictionary<int, bool> dPos = new Dictionary<int, bool>();

            foreach (GameObject obj in pieceBlack)
            {
                dPos.Add(obj.GetComponent<PieceConfig>().deadPosition, true);

                if (obj.GetComponent<PieceConfig>().pieceType == piece.pieceType)
                {
                    piece.deadPosition = obj.GetComponent<PieceConfig>().deadPosition;

                }
            }

            for (int i = 4; i > -1; i--)
            {
                if(!dPos.ContainsKey(i))
                {
                    piece.deadPosition = i;
                    piece.transform.position = squareBlack[i].transform.position;
                }
            }            

            piece.transform.Rotate(0.0f, 0.0f, 270.0f, Space.Self);
            piece.transform.Translate(0.0f, 0.2f, 0.0f, Space.Self);           
        }
    }
}
