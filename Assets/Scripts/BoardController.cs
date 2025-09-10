using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BoardController : MonoBehaviour
{
    int width = 8;
    int depth = 8;
    [Header("Board")]
    [Range(0.03f, 0.3f)]
    public float thickness = 0.3f;
    public Material brown;
    public Material yvore;
    public Material whiteMetalic;
    public Material darkMetalic;
    public Material highlightDark;
    public Material highlightWhite;

    [Header("Pieces")]
    public GameObject pieceRook;
    public GameObject pieceKnight;
    public GameObject pieceBishop;
    public GameObject pieceQueen;
    public GameObject pieceKing;
    public GameObject piecePawn;
    public List<PieceConfig> allSquares;
    public List<PieceConfig> allPieces;

    bool isPair = false;
    private int coluna;
    DeadPiecesControl deadControl;

    PieceConfig blackKing = null;
    PieceConfig whiteKing = null;
    public bool isStarted = false;
    [SerializeField]private TMP_Text textInfo;
    public string message;

    private void Awake()
    {
        gameObject.name = "ChessBoard";
    }

    private void Start()
    {
        allSquares = new List<PieceConfig>();
        allPieces = new List<PieceConfig>();
        deadControl = GetComponent<DeadPiecesControl>();
        SpawnBoard();
    }

    private void Update()
    {

        //if (isStarted)
        //{
            if (GetKings()[0].kingInCheck && !GetKings()[1].kingInCheck)
            {
                //Branco
                textInfo.text = "The White King is in check!";
            }
            else if (!GetKings()[0].kingInCheck && GetKings()[1].kingInCheck)
            {
                //Preto
                textInfo.text = "The Dark King is in check!";
            }
            else if (!GetKings()[0].kingInCheck && !GetKings()[1].kingInCheck)
            {
                //Nenhum
                textInfo.text = message;
            }
        //}
    }

    public List<PieceConfig> GetKings()
    {
        List<PieceConfig> kings = new List<PieceConfig>();
        kings.Add(whiteKing);
        kings.Add(blackKing);

        return kings;
    }

    private void SpawnBoard()
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(x, 0, z);
                go.transform.localScale = new Vector3(1f, thickness, 1f);
                go.transform.SetParent(this.transform);          
                go.AddComponent<PieceConfig>();
                PieceConfig config = go.GetComponent<PieceConfig>();
                allSquares.Add(config);
                config.pieceType = PieceType.SQUARE;
                config.occupiedBy = this.gameObject;
                config.linePos = z + 1;
                config.columnPos = x + 1;

                BoxCollider col = go.GetComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.5f, 5f, 0.5f);
                coluna = x+1;

                go.name = $"Square_{coluna}_{z + 1}";

                Renderer rend = go.GetComponent<Renderer>();
                
                if (isPair)
                {
                    rend.material = yvore;
                    go.GetComponent<PieceConfig>().StartPiece(yvore, $"Square_{coluna}_{z + 1}");
                }
                else
                {
                    rend.material = brown;
                    go.GetComponent<PieceConfig>().StartPiece(brown, $"Square_{coluna}_{z + 1}");
                }
                isPair = !isPair;
            }

            isPair = !isPair;
        }

        ApplyDetails();

        for (int i = 0; i < this.transform.childCount; i++)
        {
            if (i > 15 && i < 48) { continue; }
            SpawnPieces(this.transform.GetChild(i).gameObject);
        }
    }

    string pieceCode;
    private void SpawnPieces(GameObject square)
    {
        string[] objPosition = square.name.Split("_");
        int col = int.Parse(objPosition[1]);
        int line = int.Parse(objPosition[2]);

        GameObject piecePrefab = null;
        Material pieceMaterial;
        PlayerPieces playerPieces;
        string prefix;
        string suffix;

        if (line <=2)
        {
            pieceMaterial = whiteMetalic;
            playerPieces = PlayerPieces.WHITE;
            prefix = "White";
        }
        else
        {
            pieceMaterial = darkMetalic;
            playerPieces = PlayerPieces.BLACK;
            prefix = "Black";
        }

        if (col == 1) suffix = "A";
        else if (col == 2) suffix = "B";
        else if (col == 3) suffix = "C";
        else if (col == 4) suffix = "D";
        else if (col == 5) suffix = "E";
        else if (col == 6) suffix = "F";
        else if (col == 7) suffix = "G";
        else if (col == 8) suffix = "H";
        else suffix = "";

        if (line == 1 || line == 8)
        { 
            if ( col == 1 || col == 8)
            {
                piecePrefab = pieceRook;
                pieceCode = $"Rook_{col}{line}";
            }   
            else if (col == 2 || col == 7)
            {
                piecePrefab = pieceKnight;
                pieceCode = $"Knight_{col}{line}";
            }                
            else if (col == 3 || col == 6)
            {
                piecePrefab = pieceBishop;
                pieceCode = $"Bishop_{col}{line}";
            }
            else if (col == 4)
            {
                piecePrefab = pieceQueen;
                pieceCode = $"Queen_{col}{line}";
            }
            else if (col == 5)
            {
                piecePrefab = pieceKing;
                pieceCode = $"King_{col}{line}";
            }
        }
        else if (line == 2 || line == 7) 
        {
            piecePrefab = piecePawn;
            pieceCode = $"Pawn_{col}{line}";
        }

        GameObject piece = Instantiate(piecePrefab, square.transform.position, square.transform.rotation);
        string[] nameSplited = piece.name.Split("(");
        piece.name = $"{prefix}_{nameSplited[0]}_{suffix}_{line}";

        piece.transform.Translate(0f, 0.5f, 0f);
        piece.transform.SetParent(GameObject.Find("ChessPieces").transform);
        PieceConfig config = piece.GetComponent<PieceConfig>();
        allPieces.Add(config);

        if (pieceCode == "Knight_21" || pieceCode == "Knight_28")        
            piece.transform.GetChild(0).Rotate(0, 180, 0);

        if (pieceCode == "Bishop_31" || pieceCode == "Bishop_38")
            piece.transform.GetChild(0).Rotate(0, 180, 0);

        GameObject.Find($"Square_{col}_{line}").GetComponent<PieceConfig>().occupiedBy = piece;

        config.StartPiece(pieceMaterial, pieceCode);
        config.pieceColor = playerPieces;
        config.linePos = line;
        config.columnPos = col;

        foreach (PieceConfig p in allPieces)
        {
            if(p.pieceColor == PlayerPieces.WHITE && p.gameObject.name.Contains("King"))
                whiteKing = p;
            else if(p.pieceColor == PlayerPieces.BLACK && p.gameObject.name.Contains("King"))
                blackKing = p;
        }

        foreach (PieceConfig p in allPieces)
        {
            if (p.pieceColor == PlayerPieces.WHITE)
                p.enemyKing = blackKing;
            else
                p.enemyKing = whiteKing;
        }
    }

    private void ApplyDetails()
    {
        for (int z = 0; z < 10; z++)
        {
            for (int x = 0; x < 10; x++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(x-1, 0, z-1);
                go.transform.localScale = new Vector3(1f, thickness * 2, 1f);

                Renderer rend = go.GetComponent<Renderer>();
                rend.material = darkMetalic;

                go.name = $"Detail_{z}{x}";
                go.transform.SetParent(GameObject.Find("ChessDetails").transform);
                deadControl.AddSquare(go);

                int number = int.Parse($"{z}{x}");

                if (number > 10 && number < 19 ||
                    number > 20 && number < 29 ||
                    number > 30 && number < 39 ||
                    number > 40 && number < 49 ||
                    number > 50 && number < 59 ||
                    number > 60 && number < 69 ||
                    number > 70 && number < 79 ||
                    number > 80 && number < 89)
                    DestroyImmediate(go);
            }
        }
    }

    public PieceConfig GetSquare(int col, int line)
    {
        PieceConfig sq = null;
        foreach (PieceConfig square in allSquares)
        {
            if(square.columnPos == col && square.linePos == line)
            {
                sq = square;
            }
        }

        return sq;
    }
}

public enum PlayerPieces
{
    WHITE,
    BLACK
}

public enum PieceType
{
    SQUARE,
    ROOK,
    KNIGHT,
    BISHOP,
    PAWN,
    QUEEN,
    KING
}
