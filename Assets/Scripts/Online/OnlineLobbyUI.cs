using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess3D.Core;

namespace Chess3D.Online
{
    public class OnlineLobbyUI : MonoBehaviour
    {
        public SimpleTcpTransport transport;
        public OnlineMatchManager match;
        public Core.BoardSynchronizer synchronizer;

    [Header("UI")]
    // Exclusivo TMP (TextMeshPro)
    public TMP_InputField ipFieldTMP; public TMP_InputField portFieldTMP;
    public Button hostButton; public Button joinButton; public Button resetButton; public Button resignButton; public Button helpButton;
    public TMP_Text statusTextTMP; public TMP_Text colorTextTMP;

    [Header("Auto-Wire")] public bool autoWireOnAwake = true;
    [Tooltip("Child names to auto-find under this Panel. Change if your hierarchy uses other names.")]
    public string ipFieldName = "IPInputField"; public string portFieldName = "PortInputField";
    public string hostButtonName = "HostButton"; public string joinButtonName = "JoinButton";
    public string resetButtonName = "ResetButton"; public string resignButtonName = "ResignButton"; public string helpButtonName = "HelpButton";
        public string statusTextName = "StatusText"; public string colorTextName = "ColorText";

    [Header("Atalho (Opcional)")]
    [Tooltip("Permite alternar a visibilidade do painel com uma tecla (padrão F1)")]
    public bool enableShortcut = true;
    public KeyCode toggleKey = KeyCode.F1;
    [Tooltip("Painel do Lobby a ser mostrado/ocultado (pode estar fora deste GO). Se vazio, usa este GO.")]
    public GameObject targetPanel;

        [Header("Ajuda (Opcional)")]
        [Tooltip("URL para ajuda online quando em build. Se vazio, mostra um painel de ajuda na cena.")]
        public string helpUrl = "";
        [Tooltip("Painel de ajuda opcional (ex.: PanelTCPLobby/HelpPanel)")]
        public GameObject helpPanel;
        public string helpPanelName = "HelpPanel";
        [Tooltip("Texto do painel de ajuda (TMP_Text)")]
        public TMP_Text helpTextTMP;
        public string helpTextName = "HelpText";
        [Tooltip("Botão para fechar o painel de ajuda (opcional)")]
        public Button helpCloseButton; public string helpCloseButtonName = "HelpCloseButton";
        [Tooltip("Se verdadeiro, mesmo no Editor usará o painel de ajuda in-game em vez de abrir o README")]
        public bool preferInGameHelpInEditor = true;
        [TextArea(4,12)]
        public string helpMessage =
            "Como jogar online (1v1)\n\n" +
            "1) Host: clique em Host (porta padrão 7777).\n" +
            "2) Cliente: informe IP e Porta e clique em Join.\n" +
            "Indicadores: Status=Conectado/Desconectado; Cor=Brancas/Negras.\n" +
            "Reset: reinicia e sincroniza o tabuleiro. Resign: desistir.";

        void Awake()
        {
            if (transport == null) transport = FindObjectOfType<SimpleTcpTransport>();
            if (match == null) match = FindObjectOfType<OnlineMatchManager>();
            if (synchronizer == null) synchronizer = FindObjectOfType<Core.BoardSynchronizer>();
            if (autoWireOnAwake)
            {
                AutoWire();
            }
            // Garante que o painel de ajuda não comece visível
            EnsureHelpPanelHidden();
        }

        void Start()
        {
            if (hostButton) hostButton.onClick.AddListener(()=> {
                if (transport!=null) transport.listenPort = GetPort();
                UpdateStatus($"Hospedando na porta {GetPort()}...");
                match?.Host();
            });
            if (joinButton) joinButton.onClick.AddListener(()=> {
                if (transport!=null){ transport.connectIp = GetIp(); transport.connectPort = GetPort(); }
                UpdateStatus($"Conectando a {GetIp()}:{GetPort()}...");
                match?.Join();
            });
            if (resetButton) resetButton.onClick.AddListener(()=> { match?.ResetNetwork(); });
            if (resignButton) resignButton.onClick.AddListener(()=> { match?.Resign(); });
            if (helpButton) helpButton.onClick.AddListener(OpenHelp);
            if (transport!=null)
            {
                transport.OnConnected += ()=>UpdateStatus("Conectado");
                transport.OnDisconnected += ()=>UpdateStatus("Desconectado");
            }
            // Load last IP/Port
            string savedIp = PlayerPrefs.GetString("Online_Ip", "127.0.0.1");
            int savedPort = PlayerPrefs.GetInt("Online_Port", 7777);
            if (ipFieldTMP) ipFieldTMP.text = savedIp;
            if (portFieldTMP) portFieldTMP.text = savedPort.ToString();
            // Show initial status so the field is never empty
            UpdateStatus("Pronto — escolha Host ou Join");
            // Garante painel de ajuda iniciado escondido (reforço)
            EnsureHelpPanelHidden();
        }

        void OnEnable()
        {
            // Se o GameObject for reativado, mantém helpPanel oculto até ser solicitado
            EnsureHelpPanelHidden();
        }

        private void Update()
        {
            // Atalho para mostrar/ocultar o painel
            if (enableShortcut && Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
            if (match != null)
            {
                string txt = match.IsOnlineActive ? ($"Cor: {MapColorPt(match.AssignedColor)}") : "Desconectado";
                if (colorTextTMP) colorTextTMP.text = txt;
            }
        }

        private string GetIp()
        {
            string raw = null;
            if (ipFieldTMP != null) raw = ipFieldTMP.text;
            string ip = !string.IsNullOrWhiteSpace(raw) ? raw.Trim() : "127.0.0.1";
            PlayerPrefs.SetString("Online_Ip", ip);
            return ip;
        }

        private int GetPort()
        {
            int p = 7777;
            string raw = null;
            if (portFieldTMP != null) raw = portFieldTMP.text;
            if (!string.IsNullOrEmpty(raw)) int.TryParse(raw, out p);
            PlayerPrefs.SetInt("Online_Port", p);
            return p;
        }
        private void UpdateStatus(string s){ if (statusTextTMP!=null) statusTextTMP.text = s; }

        public void AutoWire()
        {
            // Try to find children by configured names if not assigned
            if (!ipFieldTMP) ipFieldTMP = FindInChildren<TMP_InputField>(ipFieldName) ?? FindAnywhereByName<TMP_InputField>(ipFieldName);
            if (!portFieldTMP) portFieldTMP = FindInChildren<TMP_InputField>(portFieldName) ?? FindAnywhereByName<TMP_InputField>(portFieldName);
            if (!hostButton) hostButton = FindInChildren<Button>(hostButtonName) ?? FindAnywhereByName<Button>(hostButtonName);
            if (!joinButton) joinButton = FindInChildren<Button>(joinButtonName) ?? FindAnywhereByName<Button>(joinButtonName);
            if (!resetButton) resetButton = FindInChildren<Button>(resetButtonName) ?? FindAnywhereByName<Button>(resetButtonName);
            if (!resignButton) resignButton = FindInChildren<Button>(resignButtonName) ?? FindAnywhereByName<Button>(resignButtonName);
            if (!helpButton) helpButton = FindInChildren<Button>(helpButtonName) ?? FindAnywhereByName<Button>(helpButtonName);
            if (!statusTextTMP) statusTextTMP = FindInChildren<TMP_Text>(statusTextName) ?? FindAnywhereByName<TMP_Text>(statusTextName);
            if (!colorTextTMP) colorTextTMP = FindInChildren<TMP_Text>(colorTextName) ?? FindAnywhereByName<TMP_Text>(colorTextName);
            if (!targetPanel) targetPanel = FindLobbyRoot() ?? this.gameObject;
            if (!helpPanel) helpPanel = FindHelpPanel();
            if (!helpTextTMP)
            {
                helpTextTMP = FindInChildren<TMP_Text>(helpTextName) ?? FindAnywhereByName<TMP_Text>(helpTextName);
            }
            if (!helpCloseButton) helpCloseButton = FindInChildren<Button>(helpCloseButtonName) ?? FindAnywhereByName<Button>(helpCloseButtonName);
            if (helpCloseButton)
            {
                helpCloseButton.onClick.RemoveListener(ToggleHelpPanel);
                helpCloseButton.onClick.AddListener(ToggleHelpPanel);
            }
        }

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

        private GameObject FindLobbyRoot()
        {
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                string n = tr.name.ToLower();
                if (n.Contains("paneltcplobby") || n.Equals("tcplobby") || n.Contains("lobby"))
                {
                    return tr.gameObject;
                }
            }
            return null;
        }

        private string MapColorPt(PieceColor c)
        {
            return c == PieceColor.White ? "Brancas" : "Negras";
        }

        public void TogglePanel()
        {
            if (!targetPanel) targetPanel = FindLobbyRoot() ?? this.gameObject;
            targetPanel.SetActive(!targetPanel.activeSelf);
        }

        public void OpenHelp()
        {
#if UNITY_EDITOR
            // Evita perder foco/fechar Game View: usa painel in-game se preferido
            if (!preferInGameHelpInEditor)
            {
                string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "README-online.md"));
                if (System.IO.File.Exists(path))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
                    return;
                }
            }
#endif
            if (!string.IsNullOrEmpty(helpUrl))
            {
                Application.OpenURL(helpUrl);
            }
            else
            {
                ToggleHelpPanel();
            }
        }

        private void ToggleHelpPanel()
        {
            if (!helpPanel) helpPanel = FindHelpPanel();
            if (helpPanel)
            {
                // Preenche o texto se existir
                if (helpTextTMP) helpTextTMP.text = helpMessage;
                helpPanel.SetActive(!helpPanel.activeSelf);
            }
            else
            {
                // Fallback: escreve no status
                UpdateStatus(helpMessage);
            }
        }

        private void EnsureHelpPanelHidden()
        {
            if (!helpPanel) helpPanel = FindHelpPanel();
            if (helpPanel && helpPanel.activeSelf) helpPanel.SetActive(false);
        }

        private GameObject FindHelpPanel()
        {
            // Tenta por nome exato
            var t = FindInChildren<Transform>(helpPanelName);
            if (t != null) return t.gameObject;
            // Fallback: procura por nome contendo "HelpPanel"
            var trs = GetComponentsInChildren<Transform>(true);
            foreach (var tr in trs)
            {
                if (tr.name.ToLower().Contains("helppanel"))
                {
                    return tr.gameObject;
                }
            }
            // Busca global caso este script esteja em um controller separado
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                var n = tr.name.ToLower();
                if (n == helpPanelName.ToLower() || n.Contains("helppanel") || (n.Contains("help") && n.Contains("panel")))
                {
                    return tr.gameObject;
                }
            }
            return null;
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
    }
}
