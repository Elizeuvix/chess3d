using UnityEngine;

namespace Chess3D.Core
{
    /// <summary>
    /// Posiciona automaticamente a câmera para enquadrar o tabuleiro 8x8 baseado no BoardSynchronizer
    /// ou nos parâmetros manuais de tamanho.
    /// Útil para garantir visão consistente sem ajustes manuais.
    /// </summary>
    [ExecuteAlways]
    public class CameraAutoAlign : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        [Header("Modo Geral")] public bool autoFindSynchronizer = true;
        public bool applyOnStart = true;
        public bool applyOnUpdateInEditor = true; // atualizar em tempo real no editor
        public bool onlyIfSceneView = false; // se quiser evitar mexer em play runtime

        [Header("Opções de Enquadramento")] public bool orthographicMode = true;
        public float orthoPadding = 0.2f; // margem extra
        public float perspectiveFOV = 50f;
        public float perspectiveDistanceMultiplier = 1.15f;
        public Vector3 topDirection = Vector3.down; // direção para olhar para baixo (default -Y)
        public Vector3 boardUp = Vector3.up;

        [Header("Offsets")] public float height = 10f; // usado se orthoMode false e sem cálculo
        public Vector3 manualOffset = new Vector3(0, 0, 0); // ajuste fino

        [Header("Rotação Dinâmica")]
        public bool faceFromWhiteSide = true;
        public bool rotateWithSideToMove = false; // (futuro) girar 180° quando lado a mover mudar

        private Camera _cam;

        void OnEnable()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (autoFindSynchronizer && synchronizer == null)
            {
                synchronizer = FindObjectOfType<BoardSynchronizer>();
            }
            if (applyOnStart) Apply();
        }

        void Start()
        {
            if (applyOnStart) Apply();
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && applyOnUpdateInEditor)
            {
                Apply();
            }
#endif
        }

        public void Apply()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam == null) return;

            // Determinar tamanho efetivo do tabuleiro
            float squareSize = 1f;
            Vector3 origin = Vector3.zero;
            if (synchronizer != null)
            {
                squareSize = synchronizer.squareSize;
                origin = synchronizer.originOffset; // canto a1
            }

            // Calcular centro do tabuleiro (a1 -> h8) = a1 + (7,7)*squareSize / 2
            Vector3 boardCenter = origin + new Vector3(7 * squareSize * 0.5f, 0, 7 * squareSize * 0.5f);

            if (orthographicMode)
            {
                _cam.orthographic = true;
                // Necessário: metade do tamanho maior do tabuleiro + padding
                float halfExtent = (8 * squareSize) * 0.5f;
                _cam.orthographicSize = halfExtent * (1f + orthoPadding);
                // Posição: acima do centro olhando para baixo.
                Vector3 upDir = boardUp.sqrMagnitude < 0.001f ? Vector3.up : boardUp.normalized;
                // Forçar direção para baixo consistente
                Vector3 downDir = -upDir;
                float camHeight = halfExtent * 2f; // altura proporcional (não afeta escala ortográfica)
                transform.position = boardCenter + upDir * camHeight + manualOffset;
                // Olhar para o centro (evita inversões como olhar para cima)
                transform.rotation = Quaternion.LookRotation((boardCenter - transform.position).normalized, upDir);
            }
            else
            {
                _cam.orthographic = false;
                _cam.fieldOfView = perspectiveFOV;
                // Aponta câmera levemente inclinada do lado das brancas ou neutro
                Vector3 forwardBase;
                if (faceFromWhiteSide)
                {
                    // Olha na direção positiva de z? Depende de convenção sua; assumindo axis +z vai para ranks maiores
                    forwardBase = (boardCenter - (origin + new Vector3(3.5f * squareSize, 0, -4f * squareSize))); // heurístico posicionar atrás das brancas
                }
                else
                {
                    forwardBase = Vector3.down; // fallback
                }
                Vector3 lookTarget = boardCenter;
                forwardBase.y = 0;
                if (forwardBase.sqrMagnitude < 0.1f) forwardBase = Vector3.forward;
                forwardBase.Normalize();
                // Elevar câmera
                float distance = (8 * squareSize) * perspectiveDistanceMultiplier;
                float elevate = distance * 0.65f;
                Vector3 camPos = boardCenter - forwardBase * distance + Vector3.up * elevate;
                transform.position = camPos + manualOffset;
                transform.rotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, boardUp);
            }
        }
    }
}
