using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess3D.Core
{
    // UI para habilitar/desabilitar a IA e escolher o lado controlado
    public class SimpleChessAIUI : MonoBehaviour
    {
        private bool gameStarted = false;
        public SimpleChessAI ai;
        [Header("UI")]
        public Toggle enableAIToggle;
        public TMP_Dropdown colorDropdown;
        public TMP_Text statusText;
        [SerializeField] GameObject panelPvE;
        public Toggle enableTCPToggle;

        void Start()
        {
            if (ai == null) ai = FindObjectOfType<SimpleChessAI>(true);
            if (ai != null && ai.synchronizer != null)
            {
                ai.synchronizer.OnBoardChanged += OnBoardChanged;
            }
            if (enableAIToggle != null)
            {
                enableAIToggle.isOn = false;
                if (ai != null) ai.enableAI = false;
                if (panelPvE != null) panelPvE.SetActive(false);
                if (enableTCPToggle != null)
                {
                    enableTCPToggle.isOn = false;
                    enableTCPToggle.interactable = false;
                }
                enableAIToggle.onValueChanged.AddListener(val =>
                {
                    if (gameStarted && val)
                    {
                        // Não permite ativar IA após início
                        if (panelPvE != null) panelPvE.SetActive(false);
                        enableAIToggle.isOn = false;
                        if (ai != null) ai.enableAI = false;
                        UpdateStatus();
                        return;
                    }
                    if (ai != null) ai.enableAI = val;
                    if (panelPvE != null) panelPvE.SetActive(enableAIToggle.isOn);
                    if (enableTCPToggle != null)
                    {
                        if (val) // IA ativada
                        {
                            enableTCPToggle.isOn = false;
                            enableTCPToggle.interactable = false;
                        }
                        else // IA desativada
                        {
                            enableTCPToggle.interactable = true;
                        }
                    }
                    // Desabilita painel de conexão remota
                    var lobbyUI = FindObjectOfType<Chess3D.Online.OnlineLobbyUI>(true);
                    if (lobbyUI != null && lobbyUI.targetPanel != null)
                    {
                        lobbyUI.targetPanel.SetActive(!val);
                    }
                    UpdateStatus();
                });                
            }
            if (colorDropdown != null)
            {
                colorDropdown.ClearOptions();
                colorDropdown.AddOptions(new System.Collections.Generic.List<string>{"Pretas","Brancas"});
                colorDropdown.value = ai != null && ai.aiColor == PieceColor.White ? 1 : 0;
                colorDropdown.onValueChanged.AddListener(idx => {
                    if (ai != null)
                    {
                        ai.aiColor = idx == 1 ? PieceColor.White : PieceColor.Black;
                        // Se mudou para White, é a vez das brancas e IA está ativa, faz o movimento
                        if (ai.enableAI && ai.aiColor == PieceColor.White && ai.synchronizer != null && ai.synchronizer.State != null)
                        {
                            if (ai.synchronizer.State.SideToMove == PieceColor.White && ai.synchronizer.CurrentResult == GameResult.Ongoing)
                            {
                                ai.StopAllCoroutines();
                                ai.StartCoroutine("PlayMoveDelayed");
                            }
                        }
                    }
                    UpdateStatus();
                });
            }
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (statusText != null && ai != null)
            {
                string cor = ai.aiColor == PieceColor.White ? "Brancas" : "Pretas";
                string st = ai.enableAI ? $"IA ativa para: {cor}" : "IA desativada";
                statusText.text = st;
            }
            if (enableTCPToggle != null)
            {
                enableTCPToggle.interactable = !(ai != null && ai.enableAI);
                if (ai != null && ai.enableAI)
                    enableTCPToggle.isOn = false;
            }
        }

        private void OnBoardChanged(BoardState state)
        {
            if (!gameStarted && ai != null && ai.synchronizer != null && ai.synchronizer.History != null && ai.synchronizer.History.Moves.Count > 0)
            {
                gameStarted = true;
                if (enableAIToggle != null)
                {
                    enableAIToggle.interactable = false;
                }
            }
        }
    }
}
