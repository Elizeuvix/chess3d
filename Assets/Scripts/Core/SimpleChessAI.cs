using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;


namespace Chess3D.Core
{
    // IA simples: joga automaticamente para o lado configurado
    public class SimpleChessAI : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        [Header("Configuração")]
        public PieceColor aiColor = PieceColor.Black;
        public float moveDelay = 1.0f; // segundos entre jogadas
        public bool enableAI = false;

        void Awake()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
        }

        void OnEnable()
        {
            if (synchronizer != null) synchronizer.OnBoardChanged += OnBoardChanged;
        }

        void OnDisable()
        {
            if (synchronizer != null) synchronizer.OnBoardChanged -= OnBoardChanged;
        }

        private void OnBoardChanged(BoardState state)
        {
            if (!enableAI) return;
            if (synchronizer.CurrentResult != GameResult.Ongoing) return;
            if (state.SideToMove != aiColor) return;
            StopAllCoroutines();
            StartCoroutine(PlayMoveDelayed());
        }

        private IEnumerator PlayMoveDelayed()
        {
            yield return new WaitForSeconds(moveDelay);
            var moves = MoveGenerator.GenerateLegalMoves(synchronizer.State).ToList();
            if (moves.Count == 0) yield break;
            // Heurística simples: prioriza captura, senão aleatório
            Move chosen = moves.OrderByDescending(m => IsCapture(m)).FirstOrDefault();
            synchronizer.ApplyMove(chosen);
        }

        private bool IsCapture(Move m)
        {
            var target = synchronizer.State.GetPiece(m.ToX, m.ToY);
            return target != null && target.Color != aiColor;
        }
    }
}
