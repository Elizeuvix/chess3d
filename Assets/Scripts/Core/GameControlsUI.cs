using UnityEngine;
using UnityEngine.UI;

namespace Chess3D.Core
{
    /// <summary>
    /// UI simples com botões Undo / Redo / Restart.
    /// Adicione este script num GameObject de UI e atribua os botões no Inspector.
    /// </summary>
    public class GameControlsUI : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        public Button undoButton;
        public Button redoButton;
        public Button restartButton;
        public Toggle autoHideLobby;
    [SerializeField] GameObject panelLobby;
    [Header("Opcional: alvo do painel de controles (não desativar este GO)")]
        [SerializeField] GameObject controlsPanel;
        [SerializeField] bool autoWireOnAwake = true;
        [SerializeField] string controlsPanelName = "GameControlsPanel";
    [SerializeField] string undoButtonName = "UndoButton";
    [SerializeField] string redoButtonName = "RedoButton";
    [SerializeField] string restartButtonName = "RestartButton";
 
         void Awake()
         {
             if (synchronizer == null)
             {
                 synchronizer = FindObjectOfType<BoardSynchronizer>();
             }
            if (autoWireOnAwake)
            {
                AutoWire();
            }
         }

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (undoButton != null) undoButton.onClick.AddListener(OnUndo);
            if (redoButton != null) redoButton.onClick.AddListener(OnRedo);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        }

        void Update()
        {
            if (synchronizer == null) return;
            if (undoButton != null) undoButton.interactable = synchronizer.History.CanUndo;
            if (redoButton != null) redoButton.interactable = synchronizer.History.CanRedo;
            if (restartButton != null) restartButton.interactable = true;
            // Mostra/oculta apenas o painel de controles, não este GameObject controlador
            if (controlsPanel != null)
            {
                bool show = !(synchronizer.History.CanUndo || synchronizer.History.CanRedo);
                if (controlsPanel.activeSelf != show) controlsPanel.SetActive(show);
            }

            if (panelLobby != null && autoHideLobby != null)
            {
                panelLobby.SetActive(autoHideLobby.isOn);
            }
        }

        private void OnUndo()
        {
            if (synchronizer != null) synchronizer.UndoLast();
        }

        private void OnRedo()
        {
            if (synchronizer != null) synchronizer.Redo();
        }

        private void OnRestart()
        {
            if (synchronizer != null) synchronizer.ResetGame();
        }

        private void AutoWire()
        {
            // Se nenhum painel foi definido, tenta localizar um GameObject por nome comum
            if (controlsPanel == null)
            {
                var all = FindObjectsOfType<Transform>(true);
                foreach (var tr in all)
                {
                    string n = tr.name.ToLower();
                    if (n == controlsPanelName.ToLower() || (n.Contains("controls") && n.Contains("panel")))
                    {
                        controlsPanel = tr.gameObject;
                        break;
                    }
                }
            }
            // Botões por nome (globais para suportar controlador central)
            if (undoButton == null) undoButton = FindButtonAnywhere(undoButtonName) ?? FindButtonAnywhere("Undo");
            if (redoButton == null) redoButton = FindButtonAnywhere(redoButtonName) ?? FindButtonAnywhere("Redo");
            if (restartButton == null) restartButton = FindButtonAnywhere(restartButtonName) ?? FindButtonAnywhere("Restart");
        }

        private Button FindButtonAnywhere(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                if (tr.name == name)
                {
                    var b = tr.GetComponent<Button>();
                    if (b) return b;
                }
            }
            return null;
        }
    }
}
