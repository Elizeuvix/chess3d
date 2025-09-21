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
        [Header("Online")] public Chess3D.Online.OnlineMatchManager online;
        [Header("Promotion UI")] public PromotionChoiceUI promotionUI;
    [Header("Help UI")] public GameObject helpPanel; public string helpPanelName = "HelpPanel"; public bool autoWireHelpPanel = true;
    private (int x,int y)? selected;
        private Move[] cachedMoves = new Move[0];
    [Header("Debug")]
    public bool debugLogs = false;
    [Header("Highlight")]
    public MoveHighlightManager highlightManager;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (highlightManager == null) highlightManager = FindObjectOfType<MoveHighlightManager>();
            if (online == null) online = FindObjectOfType<Chess3D.Online.OnlineMatchManager>();
            if (promotionUI == null) promotionUI = FindObjectOfType<PromotionChoiceUI>(true);
            if (autoWireHelpPanel && helpPanel == null)
            {
                // Procura por nomes comuns para o painel de ajuda
                var all = FindObjectsOfType<Transform>(true);
                foreach (var tr in all)
                {
                    var n = tr.name.ToLower();
                    if (n == helpPanelName.ToLower() || n.Contains("helppanel") || (n.Contains("help") && n.Contains("panel")) || n.Contains("panelhelp"))
                    {
                        helpPanel = tr.gameObject;
                        break;
                    }
                }
            }
            if (synchronizer != null)
            {
                synchronizer.OnBoardReset += HandleBoardReset;
                synchronizer.OnBoardChanged += HandleBoardChanged;
            }
        }

        void OnDestroy()
        {
            if (synchronizer != null)
            {
                synchronizer.OnBoardReset -= HandleBoardReset;
                synchronizer.OnBoardChanged -= HandleBoardChanged;
            }
        }

        void Update()
        {
            // Undo / Redo rápidos (desabilitados em modo online)
            if (Input.GetKeyDown(KeyCode.Z) && (online == null || !online.IsOnlineActive))
            {
                if (synchronizer != null && synchronizer.History.CanUndo)
                {
                    synchronizer.UndoLast();
                    if (highlightManager != null) highlightManager.Clear();
                    selected = null; cachedMoves = new Move[0];
                }
            }
            if (Input.GetKeyDown(KeyCode.Y) && (online == null || !online.IsOnlineActive))
            {
                if (synchronizer != null && synchronizer.History.CanRedo)
                {
                    synchronizer.Redo();
                    if (highlightManager != null) highlightManager.Clear();
                    selected = null; cachedMoves = new Move[0];
                }
            }

            // Bloquear input se jogo terminou
            if (synchronizer != null && synchronizer.CurrentResult != GameResult.Ongoing)
            {
                return;
            }
            // Cancelar seleção com botão direito
            if (Input.GetMouseButtonDown(1))
            {
                if (selected != null)
                {
                    if (highlightManager != null)
                    {
                        highlightManager.Clear();
                        highlightManager.ClearSelectedOrigin();
                    }
                    selected = null; cachedMoves = new Move[0];
                    if (debugLogs) Debug.Log("[CoreInput] Seleção cancelada (clique direito).");
                }
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (RaycastBoard(out var square))
                {
                    if (helpPanel != null && helpPanel.activeSelf) helpPanel.SetActive(false);
                    HandleClick(square.x, square.y);
                }
                else if (debugLogs)
                {
                    Debug.Log("[CoreInput] Clique não atingiu nenhuma casa (raycast). Verifique Layer/Collider das casas.");
                }
            }
        }

        private void HandleClick(int x,int y)
        {
            if (selected == null)
            {
                // Select if piece of side to move (and matches assigned color in online)
                var piece = synchronizer.State.GetPiece(x,y);
                bool ownTurn = piece != null && piece.Color == synchronizer.State.SideToMove;
                if (online != null && online.IsOnlineActive)
                {
                    ownTurn = ownTurn && piece.Color == online.AssignedColor;
                }
                if (ownTurn)
                {
                    selected = (x,y);
                    cachedMoves = MoveGenerator.GenerateLegalMoves(synchronizer.State)
                        .Where(m => m.FromX == x && m.FromY == y)
                        .ToArray();
                    if (highlightManager != null)
                    {
                        highlightManager.ShowMoves(cachedMoves);
                        highlightManager.ShowSelectedOrigin(x,y);
                    }
                }
                else
                {
                    HandleSelectionFailure(x, y);
                }
            }
            else
            {
                // Attempt move
                var (sx,sy) = selected.Value;
                // Clique na própria casa selecionada: cancelar seleção
                if (x == sx && y == sy)
                {
                    if (highlightManager != null)
                    {
                        highlightManager.Clear();
                        highlightManager.ClearSelectedOrigin();
                    }
                    selected = null; cachedMoves = new Move[0];
                    if (debugLogs) Debug.Log("[CoreInput] Seleção cancelada (clique na mesma casa).");
                    return;
                }

                // Clique em outra peça do lado a mover: trocar seleção
                var clickedPiece = synchronizer.State.GetPiece(x,y);
                bool canReselect = clickedPiece != null && clickedPiece.Color == synchronizer.State.SideToMove;
                if (online != null && online.IsOnlineActive)
                {
                    canReselect = canReselect && clickedPiece.Color == online.AssignedColor;
                }
                if (canReselect)
                {
                    selected = (x,y);
                    cachedMoves = MoveGenerator.GenerateLegalMoves(synchronizer.State)
                        .Where(m => m.FromX == x && m.FromY == y)
                        .ToArray();
                    if (highlightManager != null)
                    {
                        highlightManager.ShowMoves(cachedMoves);
                        highlightManager.ShowSelectedOrigin(x,y);
                    }
                    if (debugLogs) Debug.Log($"[CoreInput] Seleção trocada para {x},{y}.");
                    return;
                }

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
                        // Movimento normal OU promoção única (1 opção)
                        var mv = destMoves[0];
                        if (mv.Promotion != PieceType.None && promotionUI != null)
                        {
                            // Mesmo com uma única opção, mostre a UI para confirmação
                            if (debugLogs)
                                Debug.Log($"[CoreInput] Promoção única disponível ({mv.Promotion}) — exibindo UI de confirmação");
                            promotionUI.Show(new System.Collections.Generic.List<Move>{ mv }, x, y, _ =>
                            {
                                try { synchronizer.ApplyMove(mv); found = true; }
                                catch (System.Exception ex)
                                {
                                    if (debugLogs) Debug.LogError($"[CoreInput] Exceção ao aplicar promoção única {mv}: {ex.Message}\n{ex.StackTrace}");
                                }
                                if (highlightManager != null) highlightManager.Clear();
                                selected = null; cachedMoves = new Move[0];
                            });
                        }
                        else
                        {
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
                {
                    highlightManager.Clear();
                    highlightManager.ClearSelectedOrigin();
                }
                selected = null;
                cachedMoves = new Move[0];
            }
        }

        private bool RaycastBoard(out (int x,int y) square)
        {
            square = default;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // RaycastAll e prioriza qualquer hit que tenha SquareRef (as casas)
            var hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                foreach (var h in hits.OrderBy(h => h.distance))
                {
                    var sr = h.collider.GetComponent<SquareRef>() ?? h.collider.GetComponentInParent<SquareRef>();
                    if (sr != null && sr.x>=0 && sr.x<8 && sr.y>=0 && sr.y<8)
                    {
                        square = (sr.x, sr.y);
                        return true;
                    }
                }
                // Se nenhuma casa tiver SquareRef (ex.: tabuleiro único), usa o hit mais próximo
                var hit = hits.OrderBy(h => h.distance).First();
                Vector3 local = hit.point - synchronizer.originOffset;
                float fx = local.x / synchronizer.squareSize;
                float fy = local.z / synchronizer.squareSize;
                int x = Mathf.Clamp(Mathf.RoundToInt(fx), 0, 7);
                int y = Mathf.Clamp(Mathf.RoundToInt(fy), 0, 7);
                if (x>=0 && x<8 && y>=0 && y<8) { square = (x,y); return true; }
            }
            // Fallback final: projetar o raio num plano horizontal passando por originOffset.y (mesmo sem colliders)
            var plane = new Plane(Vector3.up, new Vector3(0f, synchronizer.originOffset.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                var point = ray.GetPoint(enter);
                Vector3 local = point - synchronizer.originOffset;
                float fx = local.x / synchronizer.squareSize;
                float fy = local.z / synchronizer.squareSize;
                int x = Mathf.Clamp(Mathf.RoundToInt(fx), 0, 7);
                int y = Mathf.Clamp(Mathf.RoundToInt(fy), 0, 7);
                if (x>=0 && x<8 && y>=0 && y<8)
                {
                    square = (x,y);
                    if (debugLogs)
                        Debug.Log($"[CoreInput] Raycast fallback por plano usou ponto {point} -> square {x},{y}");
                    return true;
                }
                else if (debugLogs)
                {
                    Debug.Log($"[CoreInput] Fallback de plano calculou fora do tabuleiro: fx={fx:F2}, fy={fy:F2}");
                }
            }
            return false;
        }

        private void HandleSelectionFailure(int x, int y)
        {
            if (!debugLogs) return;
            var piece = synchronizer.State.GetPiece(x, y);
            if (piece == null)
            {
                Debug.Log($"[CoreInput] Nenhuma peça em {x},{y}.");
            }
            else
            {
                Debug.Log($"[CoreInput] Peça em {x},{y} não é do lado a mover ({piece.Color}-{piece.Type}, sideToMove={synchronizer.State.SideToMove}).");
            }
        }

        private void HandleBoardReset(BoardState state)
        {
            // Limpa qualquer seleção e destaques pendentes ao reiniciar o tabuleiro
            selected = null;
            cachedMoves = new Move[0];
            if (highlightManager != null)
            {
                highlightManager.Clear();
                highlightManager.ClearSelectedOrigin();
            }
        }

        private void HandleBoardChanged(BoardState state)
        {
            // Em qualquer mudança global do tabuleiro (inclusive Undo/Redo), limpamos seleção/visuals locais
            selected = null;
            cachedMoves = new Move[0];
            if (highlightManager != null)
            {
                highlightManager.Clear();
                highlightManager.ClearSelectedOrigin();
            }
        }
    }
}
