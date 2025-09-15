using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Chess3D.Core
{
    // Simple UI controller for choosing a promotion piece.
    // Expected setup in Unity:
    // - A Canvas (Screen Space - Overlay) with this component on a root panel (disabled by default).
    // - Four Buttons assigned to the public fields (Queen, Rook, Bishop, Knight).
    // - Each button's onClick wired to the corresponding OnChoose* method OR assigned automatically if you call AutoWire() and buttons are children named accordingly.
    public class PromotionChoiceUI : MonoBehaviour
    {
        [Header("Buttons (assign in Inspector)")] public Button queenButton; public Button rookButton; public Button bishopButton; public Button knightButton;
        [Header("Optional auto-wire by child names: Queen, Rook, Bishop, Knight")]
        public bool autoWireOnAwake = true;

    private Action<PieceType> _onChosen;
    [Header("Fallback")] public bool enableTimeoutFallback = false; public float fallbackSeconds = 5f;
    private float _shownTime;
        private List<Move> _candidateMoves = new();
        private int _toX; private int _toY;

        private void Awake()
        {
            if (autoWireOnAwake)
            {
                AutoWire();
            }
            RegisterHandlers();
            gameObject.SetActive(false);
        }

        public void AutoWire()
        {
            if (queenButton == null) queenButton = FindButton("Queen");
            if (rookButton == null) rookButton = FindButton("Rook");
            if (bishopButton == null) bishopButton = FindButton("Bishop");
            if (knightButton == null) knightButton = FindButton("Knight");
        }

        private Button FindButton(string name)
        {
            var t = transform.Find(name);
            if (t == null) return null;
            return t.GetComponent<Button>();
        }

        private void RegisterHandlers()
        {
            if (queenButton) queenButton.onClick.AddListener(()=>Choose(PieceType.Queen));
            if (rookButton) rookButton.onClick.AddListener(()=>Choose(PieceType.Rook));
            if (bishopButton) bishopButton.onClick.AddListener(()=>Choose(PieceType.Bishop));
            if (knightButton) knightButton.onClick.AddListener(()=>Choose(PieceType.Knight));
        }

        public void Show(List<Move> promotionMoves, int toX, int toY, Action<PieceType> onChosen)
        {
            _candidateMoves = promotionMoves;
            _toX = toX; _toY = toY;
            _onChosen = onChosen;
            gameObject.SetActive(true);
            _shownTime = Time.time;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
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
    }
}
