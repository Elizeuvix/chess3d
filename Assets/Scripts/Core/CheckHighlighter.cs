using UnityEngine;

namespace Chess3D.Core
{
    public class CheckHighlighter : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        public GameObject checkPrefab; // prefab vermelho/alerta
        public float yOffset = 0.03f;
        public float sizeScale = 0.95f;
        private GameObject _instance;
        private bool _visible;

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (synchronizer != null)
            {
                synchronizer.OnMoveApplied += OnMoveApplied;
                // Avaliar estado inicial (lado a mover pode estar em cheque inicial em algum cenário custom)
                Evaluate();
            }
        }

        private void OnMoveApplied(Move mv, BoardState state)
        {
            // Após o movimento, o lado a mover pode estar em cheque.
            Evaluate();
        }

        private void EnsureInstance()
        {
            if (_instance == null && checkPrefab != null)
            {
                _instance = Instantiate(checkPrefab, transform);
            }
        }

        private void Evaluate()
        {
            if (synchronizer == null) return;
            var state = synchronizer.State;
            // Encontrar rei do lado a mover. FindKing lança exceção se não achar; capturamos para cenários custom inválidos.
            int kx, ky;
            try
            {
                (kx, ky) = AttackEvaluator.FindKing(state, state.SideToMove);
            }
            catch
            {
                Hide();
                return;
            }

            var opponent = state.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            bool attacked = AttackEvaluator.IsSquareAttacked(state, kx, ky, opponent);
            if (attacked) ShowAt(kx, ky); else Hide();
        }

        private void ShowAt(int x,int y)
        {
            EnsureInstance();
            if (_instance == null || synchronizer == null) return;
            float s = synchronizer.squareSize * sizeScale;
            _instance.transform.position = synchronizer.originOffset + new Vector3(x * synchronizer.squareSize, yOffset, y * synchronizer.squareSize);
            _instance.transform.localScale = new Vector3(s, _instance.transform.localScale.y, s);
            _instance.SetActive(true);
            _visible = true;
        }

        public void Hide()
        {
            if (_instance != null) _instance.SetActive(false);
            _visible = false;
        }
    }
}
