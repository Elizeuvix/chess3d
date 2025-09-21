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
    [Tooltip("Use TMP_Text para TextMeshPro. Se não atribuído, usa Text padrão se disponível.")] public TMP_Text resultTMP;
    [Tooltip("Opcional, fallback se não usar TMP.")] public Text resultText;

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (panel != null) panel.SetActive(false);
            if (synchronizer != null)
            {
                synchronizer.OnGameEnded += OnGameEnded;
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
            }
        }

        private void OnGameEnded(GameResult result, PieceColor winner)
        {
            if (panel == null) return;
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
            if (resultTMP != null) resultTMP.text = msg;
            else if (resultText != null) resultText.text = msg;
        }

        // Pode ser chamado por botões da UI
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            // Esconde se o jogo voltar a estado Ongoing via Undo/Reset
            if (synchronizer != null && synchronizer.CurrentResult == GameResult.Ongoing)
            {
                if (panel != null && panel.activeSelf) panel.SetActive(false);
            }
        }
    }
}
