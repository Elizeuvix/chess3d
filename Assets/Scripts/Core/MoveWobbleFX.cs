using UnityEngine;

namespace Chess3D.Core
{
    // Adds a tiny, natural-looking tilt (wobble) on Z when a piece is moved.
    // Hook this to BoardSynchronizer.OnMoveApplied.
    public class MoveWobbleFX : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        [Header("Wobble Config")]
    [Tooltip("Ângulo máximo (graus) de rotação aleatória em Y.")]
    public float maxYDegrees = 12.0f;
        [Tooltip("Duração (seg) da animação de inclinar e retornar.")]
        public float duration = 0.25f;
        [Tooltip("Suavização da curva (0 = linear, 1 = ease-in-out).")]
        [Range(0f,1f)] public float smooth = 0.8f;
        [Tooltip("Aplica também um leve deslocamento vertical (Y) proporcional ao wobble.")]
        public bool addLift = true;
        public float maxLift = 0.02f;

        void Awake()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
        }

        void OnEnable()
        {
            if (synchronizer != null) synchronizer.OnMoveApplied += HandleMoveApplied;
        }

        void OnDisable()
        {
            if (synchronizer != null) synchronizer.OnMoveApplied -= HandleMoveApplied;
        }

        private void HandleMoveApplied(Move mv, BoardState state)
        {
            // Só aplica para cavalo, bispo e rei
            var piece = state.GetPiece(mv.ToX, mv.ToY);
            if (piece == null) return;
            if (piece.Type != PieceType.Knight && piece.Type != PieceType.Bishop && piece.Type != PieceType.King) return;
            var pieceGo = synchronizer.GetPieceObjectAt(mv.ToX, mv.ToY);
            if (pieceGo == null) return;
            var tr = pieceGo.transform;
            float angle = Random.Range(-maxYDegrees, maxYDegrees);
            float startY = tr.eulerAngles.y;
            float targetY = Mathf.Repeat(startY + angle, 360f);
            // Para garantir que só o cavalo/bispo/rei movido anima, usamos um helper MonoBehaviour
            var helper = tr.GetComponent<MoveWobbleHelper>();
            if (helper == null) helper = tr.gameObject.AddComponent<MoveWobbleHelper>();
            helper.AnimateYRotation(startY, targetY, duration, smooth);
        }

        private System.Collections.IEnumerator AnimateYRotation(Transform tr, float startY, float targetY)
        {
            float t = 0f;
            float dur = duration;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t/dur);
                float y = Mathf.LerpAngle(startY, targetY, smooth > 0f ? Mathf.SmoothStep(0f,1f,u) : u);
                var euler = tr.eulerAngles;
                euler.y = y;
                tr.eulerAngles = euler;
                yield return null;
            }
            var finalEuler = tr.eulerAngles;
            finalEuler.y = targetY;
            tr.eulerAngles = finalEuler;
        }
    }
}
