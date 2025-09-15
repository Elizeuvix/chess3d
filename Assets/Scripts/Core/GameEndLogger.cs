using UnityEngine;

namespace Chess3D.Core
{
    /// <summary>
    /// Componente simples que loga o resultado do jogo quando o BoardSynchronizer dispara OnGameEnded.
    /// Pode ser expandido para UI.
    /// </summary>
    public class GameEndLogger : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (synchronizer != null)
            {
                synchronizer.OnGameEnded += OnGameEnded;
            }
        }

        private void OnGameEnded(GameResult result, PieceColor side)
        {
            switch (result)
            {
                case GameResult.WhiteWinsCheckmate:
                    Debug.Log("[GameEndLogger] Checkmate! Brancas vencem.");
                    break;
                case GameResult.BlackWinsCheckmate:
                    Debug.Log("[GameEndLogger] Checkmate! Pretas vencem.");
                    break;
                case GameResult.Stalemate:
                    Debug.Log("[GameEndLogger] Stalemate (empate por afogamento).");
                    break;
                case GameResult.DrawFiftyMoveRule:
                    Debug.Log("[GameEndLogger] Empate (regra dos 50 lances).");
                    break;
                case GameResult.DrawThreefoldRepetition:
                    Debug.Log("[GameEndLogger] Empate (tríplice repetição).");
                    break;
                case GameResult.DrawInsufficientMaterial:
                    Debug.Log("[GameEndLogger] Empate (material insuficiente).");
                    break;
                default:
                    Debug.Log("[GameEndLogger] Resultado: " + result);
                    break;
            }
        }
    }
}
