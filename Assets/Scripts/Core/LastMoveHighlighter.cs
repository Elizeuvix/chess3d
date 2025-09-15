using UnityEngine;

namespace Chess3D.Core
{
    public class LastMoveHighlighter : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        public GameObject originPrefab;
        public GameObject destinationPrefab;
        public float yOffset = 0.02f;
        public float sizeScale = 0.95f;

        private GameObject _originInst;
        private GameObject _destInst;
        private Move? _lastMove;

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (synchronizer != null)
            {
                synchronizer.OnMoveApplied += HandleMoveApplied;
            }
        }

        private void HandleMoveApplied(Move mv, BoardState state)
        {
            _lastMove = mv;
            EnsureInstances();
            if (synchronizer == null) return;
            PositionMarker(_originInst, mv.FromX, mv.FromY);
            PositionMarker(_destInst, mv.ToX, mv.ToY);
            _originInst.SetActive(true);
            _destInst.SetActive(true);
        }

        private void EnsureInstances()
        {
            if (_originInst == null && originPrefab != null)
            {
                _originInst = Instantiate(originPrefab, transform);
            }
            if (_destInst == null && destinationPrefab != null)
            {
                _destInst = Instantiate(destinationPrefab, transform);
            }
        }

        private void PositionMarker(GameObject go, int x, int y)
        {
            if (go == null || synchronizer == null) return;
            float s = synchronizer.squareSize * sizeScale;
            go.transform.position = synchronizer.originOffset + new Vector3(x * synchronizer.squareSize, yOffset, y * synchronizer.squareSize);
            go.transform.localScale = new Vector3(s, go.transform.localScale.y, s);
        }

        public void Hide()
        {
            if (_originInst) _originInst.SetActive(false);
            if (_destInst) _destInst.SetActive(false);
        }

        public void Refresh()
        {
            if (_lastMove.HasValue && synchronizer != null)
            {
                var mv = _lastMove.Value;
                PositionMarker(_originInst, mv.FromX, mv.FromY);
                PositionMarker(_destInst, mv.ToX, mv.ToY);
            }
        }
    }
}
