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
 
         void Awake()
         {
             if (synchronizer == null)
             {
                 synchronizer = FindObjectOfType<BoardSynchronizer>();
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
            {
                if (synchronizer.History.CanUndo || synchronizer.History.CanRedo)
                {
                    if (gameObject.activeSelf) gameObject.SetActive(false);
                }
                else
                {
                    if (!gameObject.activeSelf) gameObject.SetActive(true);
                }
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
    }
}
