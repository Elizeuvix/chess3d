using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Chess3D.Core
{
    // This MonoBehaviour is a thin adapter that keeps Unity scene objects in sync with the pure BoardState.
    public class BoardSynchronizer : MonoBehaviour
    {
        [Header("Piece Prefabs (Ordered: Pawn,Rook,Knight,Bishop,Queen,King)")]
    // Initialized to empty arrays so non-nullable contract is satisfied; normally populated via Unity Inspector.
    public GameObject[] whitePrefabs = System.Array.Empty<GameObject>();
    public GameObject[] blackPrefabs = System.Array.Empty<GameObject>();
        public float squareSize = 1f;
        public Vector3 originOffset = Vector3.zero;
        [Header("Vertical Alignment")]
        [Tooltip("Altura do 'chão' onde a base da peça deve encostar (normalmente 0).")] public float pieceBaseY = 0f;
        [Tooltip("Se true, ajusta automaticamente a peça para que o ponto mais baixo do Renderer encoste em pieceBaseY.")] public bool autoRaisePieces = true;
        [Tooltip("Offset adicional após alinhar a base (útil se quiser flutuar levemente)." )] public float extraPieceYOffset = 0f;

        public BoardState State { get; private set; } = BoardState.CreateInitial();
    public GameHistory History { get; private set; } = new GameHistory();
    public System.Action<Move, BoardState> OnMoveApplied; // callback após aplicar
    public System.Action<GameResult, PieceColor> OnGameEnded; // resultado e lado vencedor (ou PieceColor.White para empate especial?)
    public GameResult CurrentResult { get; private set; } = GameResult.Ongoing;

        private readonly GameObject?[,] _pieces = new GameObject?[8,8];

        void Start()
        {
            RebuildAllPieces();
            // After first build, proactively disable legacy scripts that can move pieces outside core state.
            DisableGlobalLegacyControllers();
        }

        public void ApplyMove(Move move)
        {
            // Update model
            History.RecordPreState(State);
            MoveApplier.Apply(State, move);
            // Simple rebuild for now (can be optimized incrementally)
            RebuildAllPieces();
            History.AddMove(move);
            OnMoveApplied?.Invoke(move, State);
            // Avaliar término de jogo (se ainda não terminou)
            if (CurrentResult == GameResult.Ongoing)
            {
                EvaluateGameEnd();
            }
        }

        public bool UndoLast()
        {
            if (!History.CanUndo) return false;
            var restored = History.UndoLast(out var undone);
            if (restored == null) return false;
            State = restored;
            RebuildAllPieces();
            return true;
        }

        // Public helper for external utilities (test loaders, scenario setup) to rebuild visuals
        public void ForceRebuild()
        {
            RebuildAllPieces();
        }

        private void RebuildAllPieces()
        {
            // Clear existing
            for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
            {
                if (_pieces[x,y] != null)
                {
                    Destroy(_pieces[x,y]);
                    _pieces[x,y] = null;
                }
            }

            // Instantiate from state
            for (int x=0;x<8;x++)
            for (int y=0;y<8;y++)
            {
                var piece = State.GetPiece(x,y);
                if (piece == null) continue;
                var prefab = GetPrefab(piece);
                if (prefab == null) continue;
                var go = Instantiate(prefab, SquareToWorld(x,y), Quaternion.identity, transform);
                go.name = piece.Color + "_" + piece.Type + $"_{x}{y}";
                if (autoRaisePieces)
                {
                    AdjustVertical(go);
                }
                _pieces[x,y] = go;
                DisableLegacyComponents(go);
            }
        }

        // Prevent legacy MonoBehaviours (PieceConfig / movement scripts) from mutating transforms
        // and causing divergence between visual board and core BoardState.
        private void DisableLegacyComponents(GameObject go)
        {
            // PieceConfig existed on old prefabs; disable if present.
            var pc = go.GetComponent<PieceConfig>();
            if (pc != null) pc.enabled = false;
            // SpecialMovement & other legacy helpers (if any) can also be disabled safely.
            var sm = go.GetComponent<SpecialMovement>();
            if (sm != null) sm.enabled = false;
        }

        private void DisableGlobalLegacyControllers()
        {
            // TargetSelect was the old board-level driver; disable it if found.
            var targetSelect = FindObjectOfType<TargetSelect>();
            if (targetSelect != null) targetSelect.enabled = false;
            // Also disable any already existing PieceConfig on scene objects (e.g., squares) to avoid OnMouse events.
            var legacyPieces = FindObjectsOfType<PieceConfig>();
            foreach (var lp in legacyPieces)
            {
                if (lp.enabled) lp.enabled = false;
            }
        }

        private GameObject? GetPrefab(Piece piece)
        {
            GameObject[] source = piece.Color == PieceColor.White ? whitePrefabs : blackPrefabs;
            int index = piece.Type switch
            {
                PieceType.Pawn => 0,
                PieceType.Rook => 1,
                PieceType.Knight => 2,
                PieceType.Bishop => 3,
                PieceType.Queen => 4,
                PieceType.King => 5,
                _ => -1
            };
            if (index < 0 || index >= source.Length) return null;
            return source[index];
        }

        private Vector3 SquareToWorld(int x,int y)
        {
            return originOffset + new Vector3(x * squareSize, 0, y * squareSize);
        }

        private void AdjustVertical(GameObject go)
        {
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend == null) return;
            var bounds = rend.bounds; // world space
            float bottom = bounds.min.y;
            float desiredBottom = pieceBaseY + extraPieceYOffset;
            float delta = desiredBottom - bottom;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                go.transform.position += new Vector3(0, delta, 0);
            }
        }

        private void EvaluateGameEnd()
        {
            // Gerar todos os lances legais para o lado a mover atual
            var legal = MoveGenerator.GenerateLegalMoves(State);
            if (legal.Any())
            {
                // Também checar outras condições especiais no futuro (50-move, repetição, insuficiente material) TODO
                return;
            }
            // Sem lances legais: ou checkmate ou stalemate
            // Verificar se rei está em cheque
            try
            {
                var (kx, ky) = AttackEvaluator.FindKing(State, State.SideToMove);
                var opponent = State.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                bool inCheck = AttackEvaluator.IsSquareAttacked(State, kx, ky, opponent);
                if (inCheck)
                {
                    CurrentResult = State.SideToMove == PieceColor.White ? GameResult.BlackWinsCheckmate : GameResult.WhiteWinsCheckmate;
                    OnGameEnded?.Invoke(CurrentResult, opponent);
                    Debug.Log($"[GameEnd] Checkmate. Resultado: {CurrentResult}");
                }
                else
                {
                    CurrentResult = GameResult.Stalemate;
                    OnGameEnded?.Invoke(CurrentResult, PieceColor.White); // vencedor irrelevante; segundo parâmetro pode ser ignorado em casos de empate
                    Debug.Log("[GameEnd] Stalemate (afogamento). Empate.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameEnd] Falha ao avaliar término: {ex.Message}");
            }
        }
    }
}
