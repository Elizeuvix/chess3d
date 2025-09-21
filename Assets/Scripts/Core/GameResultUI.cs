using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess3D.Core
{
    /// <summary>
    /// Exibe um painel simples com o resultado da partida.
    /// Mostra quando OnGameEnded é disparado; esconde quando houver Undo/Reset.
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
    [Header("Referências UI")] public GameObject panel;
    [Tooltip("Use TMP_Text para TextMeshPro. Se não atribuído, tenta auto-localizar.")] public TMP_Text resultTMP;
    [Tooltip("Opcional, fallback se não usar TMP (auto-localizado se vazio). ")] public Text resultText;
    [Header("Auto-Wire (Opcional)")]
    [Tooltip("Se verdadeiro, tenta localizar painel/texto pelos nomes abaixo ou por primeiro disponível.")] public bool autoWireOnAwake = true;
    public string panelName = "GameResultPanel";
    public string textName = "ResultText";

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (autoWireOnAwake)
            {
                AutoWire();
            }
            if (panel != null) panel.SetActive(false);
            if (synchronizer != null)
            {
                synchronizer.OnGameEnded += OnGameEnded;
                // Esconde imediatamente quando reiniciar ou houver mudança global (Undo/Redo/Reset)
                synchronizer.OnBoardReset += HandleBoardEvent;
                synchronizer.OnBoardChanged += HandleBoardEvent;
            }
        }

        private void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (synchronizer != null)
            {
                synchronizer.OnGameEnded -= OnGameEnded;
                synchronizer.OnBoardReset -= HandleBoardEvent;
                synchronizer.OnBoardChanged -= HandleBoardEvent;
            }
        }

        private void HandleBoardEvent(BoardState state)
        {
            Hide();
        }

        private void OnGameEnded(GameResult result, PieceColor winner)
        {
            if (panel == null)
            {
                // Tenta última vez auto-wire
                AutoWire();
                if (panel == null)
                {
                    // Evite usar o próprio GO (controller), tente deduzir pelo texto
                    if (resultTMP != null) panel = resultTMP.transform.root.gameObject;
                    else if (resultText != null) panel = resultText.transform.root.gameObject;
                }
            }
            if (panel == null)
            {
                Debug.LogWarning("[GameResultUI] Painel não encontrado; criando objeto em runtime para exibir.");
                panel = new GameObject("GameResultPanel_Runtime");
            }
            panel.SetActive(true);
            string msg;
            switch (result)
            {
                case GameResult.WhiteWinsCheckmate:
                    msg = "Checkmate! Brancas vencem";
                    break;
                case GameResult.BlackWinsCheckmate:
                    msg = "Checkmate! Pretas vencem";
                    break;
                case GameResult.Stalemate:
                    msg = "Empate por afogamento (Stalemate)";
                    break;
                case GameResult.DrawFiftyMoveRule:
                    msg = "Empate (Regra dos 50 lances)";
                    break;
                case GameResult.DrawThreefoldRepetition:
                    msg = "Empate (Tríplice repetição)";
                    break;
                case GameResult.DrawInsufficientMaterial:
                    msg = "Empate (Material insuficiente)";
                    break;
                default:
                    msg = "Resultado: " + result;
                    break;
            }
            if (resultTMP == null && resultText == null)
            {
                // Busca uma última vez um alvo de texto
                AutoWireTextOnly();
            }
            if (resultTMP != null) resultTMP.text = msg;
            else if (resultText != null) resultText.text = msg;
        }

        // Pode ser chamado por botões da UI
        public void Hide()
        {
            if (panel != null && panel != this.gameObject) panel.SetActive(false);
        }

        void Update()
        {
            // Esconde se o jogo voltar a estado Ongoing via Undo/Reset
            if (synchronizer != null && synchronizer.CurrentResult == GameResult.Ongoing)
            {
                if (panel != null && panel != this.gameObject && panel.activeSelf) panel.SetActive(false);
            }
        }

        private void AutoWire()
        {
            // Painel
            if (panel == null)
            {
                // Procura por nome
                var t = FindInChildren<Transform>(panelName);
                if (t == null) t = FindAnywhereByName<Transform>(panelName);
                if (t != null) panel = t.gameObject;
                // Se ainda não achou, tenta pegar o primeiro painel desativado encontrado na cena com nome contendo GameResult
                if (panel == null)
                {
                    var any = FindFirstAnywhere<Transform>("gameresult");
                    if (any != null) panel = any.gameObject;
                }
                // Não use this.gameObject como painel (para não desligar o controller)
            }
            // Texto
            AutoWireTextOnly();
        }

        private void AutoWireTextOnly()
        {
            if (resultTMP == null && resultText == null)
            {
                // Primeiro tenta por nome
                var tmpByName = FindInChildren<TMP_Text>(textName);
                if (tmpByName == null) tmpByName = FindAnywhereByName<TMP_Text>(textName);
                if (tmpByName != null) { resultTMP = tmpByName; return; }
                var txtByName = FindInChildren<Text>(textName);
                if (txtByName == null) txtByName = FindAnywhereByName<Text>(textName);
                if (txtByName != null) { resultText = txtByName; return; }
                // Depois pega o primeiro disponível nos filhos
                var anyTMP = GetComponentInChildren<TMP_Text>(true);
                if (anyTMP != null) { resultTMP = anyTMP; return; }
                var anyText = GetComponentInChildren<Text>(true);
                if (anyText != null) { resultText = anyText; return; }
                // Finalmente, procura globalmente qualquer TMP_Text/Text
                var allTMP = FindObjectsOfType<TMP_Text>(true);
                if (allTMP != null && allTMP.Length > 0) { resultTMP = allTMP[0]; return; }
                var allText = FindObjectsOfType<Text>(true);
                if (allText != null && allText.Length > 0) { resultText = allText[0]; return; }
            }
        }

        private T FindInChildren<T>(string childName) where T : Component
        {
            if (string.IsNullOrEmpty(childName)) return null;
            var trs = GetComponentsInChildren<Transform>(true);
            foreach (var t in trs)
            {
                if (t.name == childName)
                {
                    var c = t.GetComponent<T>();
                    if (c) return c;
                }
            }
            return null;
        }

        // Busca global por nome exato (inclui objetos inativos)
        private T FindAnywhereByName<T>(string name) where T : Component
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                if (tr.name == name)
                {
                    var c = tr.GetComponent<T>();
                    if (c) return c;
                }
            }
            return null;
        }

        // Busca global pelo primeiro Transform cujo nome contenha o fragmento informado
        private Transform FindFirstAnywhere<TransformType>(string nameFragment)
        {
            if (string.IsNullOrEmpty(nameFragment)) return null;
            string frag = nameFragment.ToLowerInvariant();
            var all = FindObjectsOfType<UnityEngine.Transform>(true);
            foreach (var tr in all)
            {
                if (tr.name.ToLowerInvariant().Contains(frag))
                {
                    return tr;
                }
            }
            return null;
        }
    }
}
