using UnityEngine;

namespace Chess3D.Core
{
    // Attach this to an always-active object (e.g., GeneralUIController).
    // It will hide a Help panel when the player clicks on any chess piece or board square.
    public class HideHelpOnPieceClick : MonoBehaviour
    {
        [Tooltip("Painel de ajuda a esconder. Se vazio, tenta localizar nomes como 'PanelHelp' ou 'HelpPanel'.")]
        public GameObject helpPanel;
        [Tooltip("Layers considerados clicáveis para ocultar ajuda (Peças/Casas). Se 0, usa Physics.DefaultRaycastLayers.")]
        public LayerMask clickableLayers;
        [Tooltip("Tecla alternativa para fechar a ajuda (além do clique).")]
        public KeyCode closeKey = KeyCode.Escape;
        [SerializeField] private string[] helpPanelNameCandidates = new []{"PanelHelp","HelpPanel","Ajuda","PainelAjuda"};

        void Awake()
        {
            if (helpPanel == null)
            {
                helpPanel = FindHelpPanelAnywhere();
            }
            if (clickableLayers == 0)
            {
                clickableLayers = Physics.DefaultRaycastLayers;
            }
        }

        void Update()
        {
            if (helpPanel == null) return;

            // Fecha por tecla
            if (Input.GetKeyDown(closeKey))
            {
                if (helpPanel.activeSelf) helpPanel.SetActive(false);
            }

            // Fecha ao clicar em algo do tabuleiro/peças
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : new Ray(Vector3.zero, Vector3.forward);
                if (Physics.Raycast(ray, out var hit, 500f, clickableLayers))
                {
                    // Heurística: qualquer clique em collider fecha o help
                    if (helpPanel.activeSelf) helpPanel.SetActive(false);
                }
            }
        }

        private GameObject FindHelpPanelAnywhere()
        {
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                string n = tr.name.ToLower();
                foreach (var cand in helpPanelNameCandidates)
                {
                    if (n == cand.ToLower()) return tr.gameObject;
                }
                if (n.Contains("help") && n.Contains("panel")) return tr.gameObject;
                if (n.Contains("ajuda") && n.Contains("painel")) return tr.gameObject;
            }
            return null;
        }
    }
}
