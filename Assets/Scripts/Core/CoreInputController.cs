using UnityEngine;
using System.Linq;

namespace Chess3D.Core
{
    // Simple testing controller: click source square then destination square.
    public class CoreInputController : MonoBehaviour
    {
        public Camera cam;
        public LayerMask boardLayer;
        public BoardSynchronizer synchronizer;
        [Header("Promotion UI")] public PromotionChoiceUI promotionUI;
    private (int x,int y)? selected;
        private Move[] cachedMoves = new Move[0];
    [Header("Debug")]
    public bool debugLogs = false;
    [Header("Highlight")]
    public MoveHighlightManager highlightManager;

        void Start()
        {
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RaycastBoard(out var square))
                {
                    HandleClick(square.x, square.y);
                }
            }
        }

        private void HandleClick(int x,int y)
        {
            if (selected == null)
            {
                // Select if piece of side to move
                var piece = synchronizer.State.GetPiece(x,y);
                if (piece != null && piece.Color == synchronizer.State.SideToMove)
                {
                    selected = (x,y);
                    cachedMoves = MoveGenerator.GenerateLegalMoves(synchronizer.State)
                        .Where(m => m.FromX == x && m.FromY == y)
                        .ToArray();
                    if (highlightManager != null)
                        highlightManager.ShowMoves(cachedMoves);
                }
            }
            else
            {
                // Attempt move
                var (sx,sy) = selected.Value;
                var found = false;
                // Coletar todos os movimentos que chegam ao destino
                var destMoves = cachedMoves.Where(m=>m.ToX==x && m.ToY==y).ToList();
                if (destMoves.Count > 0)
                {
                    // Verifica se é caso de promoção múltipla
                    var promoMoves = destMoves.Where(m=>m.Promotion != PieceType.None).ToList();
                    if (promoMoves.Count > 1)
                    {
                        // Abrir UI e adiar aplicação
                        if (promotionUI != null)
                        {
                            if (debugLogs)
                                Debug.Log($"[CoreInput] Abrindo UI de promoção para destino {x},{y} opções: {string.Join(",", promoMoves.Select(p=>p.Promotion))}");
                            promotionUI.Show(promoMoves, x, y, pieceType =>
                            {
                                var chosen = promoMoves.FirstOrDefault(m=>m.Promotion == pieceType);
                                if (chosen.Promotion == PieceType.None)
                                {
                                    // fallback caso não encontre (deveria achar)
                                    chosen = promoMoves.First();
                                }
                                try
                                {
                                    synchronizer.ApplyMove(chosen);
                                }
                                catch (System.Exception ex)
                                {
                                    if (debugLogs)
                                        Debug.LogError($"[CoreInput] Exceção ao aplicar promoção {chosen}: {ex.Message}\n{ex.StackTrace}");
                                }
                                if (highlightManager != null) highlightManager.Clear();
                                selected = null;
                                cachedMoves = new Move[0];
                            });
                        }
                        else
                        {
                            // Se UI não atribuída, escolher dama por padrão
                            var queen = promoMoves.FirstOrDefault(m=>m.Promotion == PieceType.Queen);
                            if (queen.Promotion == PieceType.None) queen = promoMoves.First();
                            try
                            {
                                synchronizer.ApplyMove(queen);
                            }
                            catch (System.Exception ex)
                            {
                                if (debugLogs)
                                    Debug.LogError($"[CoreInput] Exceção ao aplicar promoção fallback {queen}: {ex.Message}\n{ex.StackTrace}");
                            }
                            found = true;
                            if (highlightManager != null) highlightManager.Clear();
                            selected = null; cachedMoves = new Move[0];
                        }
                        return; // Espera callback ou já resolveu fallback
                    }
                    else
                    {
                        // Movimento normal (ou promoção única)
                        var mv = destMoves[0];
                        try
                        {
                            synchronizer.ApplyMove(mv);
                            found = true;
                        }
                        catch (System.Exception ex)
                        {
                            if (debugLogs)
                                Debug.LogError($"[CoreInput] Exceção ao aplicar movimento {mv}: {ex.Message}\n{ex.StackTrace}");
                            // Mantém seleção para investigação
                        }
                    }
                }
                if (!found)
                {
                    if (debugLogs)
                    {
                        var piece = synchronizer.State.GetPiece(sx,sy);
                        var targetPiece = synchronizer.State.GetPiece(x,y);
                        Debug.Log($"[CoreInput] Destino inválido {x},{y}. Origem {sx},{sy} peça={piece?.Color}-{piece?.Type}. Alvo={(targetPiece!=null ? targetPiece.Color+"-"+targetPiece.Type : "vazio")}. Movimentos válidos: {string.Join(",", cachedMoves.Select(m=>m.ToString()))}");
                    }
                    // Mantém seleção para tentar outro destino
                    return;
                }
                if (highlightManager != null)
                    highlightManager.Clear();
                selected = null;
                cachedMoves = new Move[0];
            }
        }

        private bool RaycastBoard(out (int x,int y) square)
        {
            square = default;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, boardLayer))
            {
                // Primeiro tenta SquareRef direto no objeto ou em pais
                var sr = hit.collider.GetComponent<SquareRef>();
                if (sr == null) sr = hit.collider.GetComponentInParent<SquareRef>();
                if (sr != null)
                {
                    if (sr.x>=0 && sr.x<8 && sr.y>=0 && sr.y<8)
                    {
                        square = (sr.x, sr.y);
                        return true;
                    }
                }
                // Fallback: cálculo por posição
                Vector3 local = hit.point - synchronizer.originOffset;
                float fx = local.x / synchronizer.squareSize;
                float fy = local.z / synchronizer.squareSize;
                int x = Mathf.RoundToInt(fx);
                int y = Mathf.RoundToInt(fy);
                if (x>=0 && x<8 && y>=0 && y<8) { square = (x,y); return true; }
            }
            return false;
        }
    }
}
