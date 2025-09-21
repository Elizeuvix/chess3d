using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Chess3D.Core
{
    /// <summary>
    /// Desenha gizmos de grade 8x8 e rótulos algébricos (a1..h8) sobre o tabuleiro.
    /// Usa squareSize e originOffset do BoardSynchronizer.
    /// </summary>
    [ExecuteAlways]
    public class BoardGridGizmos : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        [Header("Exibição")] public bool showGrid = true;
        public bool showLabels = true;
        public Color gridColor = new Color(1f, 1f, 0f, 0.5f);
        public Color labelColor = Color.yellow;
        public float labelOffsetY = 0.02f;
        public int labelFontSize = 12;
        [Tooltip("Se verdadeiro, só desenha no Editor. Desmarque para também desenhar em Play.")]
        public bool editorOnly = true;

        private void OnDrawGizmos()
        {
            if (editorOnly && Application.isPlaying) return;
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (synchronizer == null) return;

            float s = synchronizer.squareSize;
            Vector3 origin = synchronizer.originOffset; // canto a1

            if (showGrid)
            {
                Gizmos.color = gridColor;
                // Linhas verticais
                for (int i = 0; i <= 8; i++)
                {
                    Vector3 a = origin + new Vector3(i * s, 0f, 0f);
                    Vector3 b = origin + new Vector3(i * s, 0f, 8 * s);
                    Gizmos.DrawLine(a, b);
                }
                // Linhas horizontais
                for (int j = 0; j <= 8; j++)
                {
                    Vector3 a = origin + new Vector3(0f, 0f, j * s);
                    Vector3 b = origin + new Vector3(8 * s, 0f, j * s);
                    Gizmos.DrawLine(a, b);
                }
            }

#if UNITY_EDITOR
            if (showLabels)
            {
                var style = new GUIStyle();
                style.normal.textColor = labelColor;
                style.fontSize = labelFontSize;
                for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    string alg = AlgebraicFromXY(x, y);
                    Vector3 pos = origin + new Vector3((x + 0.5f) * s, labelOffsetY, (y + 0.5f) * s);
                    Handles.Label(pos, alg, style);
                }
            }
#endif
        }

        private string AlgebraicFromXY(int x, int y)
        {
            char file = (char)('a' + x);
            char rank = (char)('1' + y);
            return new string(new[] { file, rank });
        }
    }
}
