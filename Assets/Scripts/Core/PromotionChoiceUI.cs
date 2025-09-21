using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess3D.Core
{
    // Simple UI controller for choosing a promotion piece.
    // Expected setup in Unity:
    // - A Canvas (Screen Space - Overlay) with this component on a root panel (disabled by default).
    // - Four Buttons assigned to the public fields (Queen, Rook, Bishop, Knight).
    // - Each button's onClick wired to the corresponding OnChoose* method OR assigned automatically if you call AutoWire() and buttons are children named accordingly.
    public class PromotionChoiceUI : MonoBehaviour
    {
        [Header("Panel alvo (pode estar fora deste GO)")] [SerializeField] private GameObject panel;
        [SerializeField] private string panelName = "PromotionPanel";
        [Header("Buttons (assign in Inspector)")] public Button queenButton; public Button rookButton; public Button bishopButton; public Button knightButton;
        [Header("Optional auto-wire by names: Queen, Rook, Bishop, Knight")] public bool autoWireOnAwake = true;

    private Action<PieceType> _onChosen;
    [Header("Fallback")] public bool enableTimeoutFallback = false; public float fallbackSeconds = 5f;
    private float _shownTime;
        private List<Move> _candidateMoves = new();
        private int _toX; private int _toY;

        private void Awake()
        {
            if (autoWireOnAwake) { AutoWire(); }
            RegisterHandlers();
            if (panel != null) panel.SetActive(false);
        }

        private void Start()
        {
            // Opcional: esconder automaticamente se o tabuleiro mudar (Undo/Redo/Reset/FEN) enquanto a UI estiver aberta
            var synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (synchronizer != null)
            {
                synchronizer.OnBoardReset += _ => Hide();
                synchronizer.OnBoardChanged += _ => Hide();
            }
        }

        public void AutoWire()
        {
            // Painel
            if (panel == null)
            {
                panel = FindByNameAnywhere(panelName);
                if (panel == null)
                {
                    // tenta heurística por nomes comuns
                    panel = FindByNameAnywhere("Promotion") ?? FindByNameAnywhere("PromotionPanel");
                }
            }
            // Botões
            if (queenButton == null) queenButton = FindButton("Queen");
            if (rookButton == null) rookButton = FindButton("Rook");
            if (bishopButton == null) bishopButton = FindButton("Bishop");
            if (knightButton == null) knightButton = FindButton("Knight");
        }

        private Button FindButton(string name)
        {
            Transform root = panel != null ? panel.transform : this.transform;
            // primeiro, procura como filho direto/indireto do painel
            var trans = root.GetComponentsInChildren<Transform>(true);
            foreach (var tr in trans)
            {
                if (tr.name == name)
                {
                    var b = tr.GetComponent<Button>();
                    if (b) return b;
                }
            }
            // fallback: busca global
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

        private void RegisterHandlers()
        {
            if (queenButton) queenButton.onClick.AddListener(()=>Choose(PieceType.Queen));
            if (rookButton) rookButton.onClick.AddListener(()=>Choose(PieceType.Rook));
            if (bishopButton) bishopButton.onClick.AddListener(()=>Choose(PieceType.Bishop));
            if (knightButton) knightButton.onClick.AddListener(()=>Choose(PieceType.Knight));
        }

        public bool IsReady => panel != null && (queenButton || rookButton || bishopButton || knightButton);

        public void Show(List<Move> promotionMoves, int toX, int toY, Action<PieceType> onChosen)
        {
            _candidateMoves = promotionMoves;
            _toX = toX; _toY = toY;
            _onChosen = onChosen;
            if (panel == null) AutoWire();
            if (panel == null)
            {
                Debug.LogWarning("[PromotionChoiceUI] Painel de promoção não encontrado. Verifique o nome 'PromotionPanel' ou arraste a referência no Inspector.");
                return;
            }
            panel.SetActive(true);
            _shownTime = Time.time;
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (enableTimeoutFallback && gameObject.activeSelf)
            {
                if (Time.time - _shownTime >= fallbackSeconds)
                {
                    ChooseDefault();
                }
            }
        }

        private void Choose(PieceType piece)
        {
            Hide();
            _onChosen?.Invoke(piece);
        }

        // Fallback call (e.g., if user clicks elsewhere or timeout) -> default queen
        public void ChooseDefault()
        {
            Choose(PieceType.Queen);
        }

        private GameObject FindByNameAnywhere(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = FindObjectsOfType<Transform>(true);
            foreach (var tr in all)
            {
                if (tr.name == name)
                {
                    return tr.gameObject;
                }
            }
            return null;
        }
    }
}
