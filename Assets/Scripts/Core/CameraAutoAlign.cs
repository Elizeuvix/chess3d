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

        public enum CameraPreset { TopDownOrtho, Isometric, LowAngleCinematic }

    [Header("Opções de Enquadramento")] public bool orthographicMode = false;
        public CameraPreset preset = CameraPreset.Isometric;
        public float orthoPadding = 0.2f; // margem extra
        public float perspectiveFOV = 50f;
    public float perspectiveDistanceMultiplier = 0.9f;
        public Vector3 topDirection = Vector3.down; // direção para olhar para baixo (default -Y)
        public Vector3 boardUp = Vector3.up;

        [Header("Offsets")] public float height = 10f; // usado se orthoMode false e sem cálculo
        public Vector3 manualOffset = new Vector3(0, 0, 0); // ajuste fino

        [Header("Rotação Dinâmica")]
        public bool faceFromWhiteSide = true;
        public bool rotateWithSideToMove = false; // girar 180° quando lado a mover mudar

        [Header("Transições Suaves")]
        public bool smoothTransition = true;
        [Tooltip("Tempo de amortecimento (seg) para posição. Rotação usa Lerp com velocidade proporcional.")]
        public float smoothTime = 0.25f;
        public float rotationLerpSpeed = 10f;
    [Tooltip("Se verdadeiro, pausa as atualizações de câmera enquanto o usuário está orbitando/zoom/pan (evita trepidação).")]
    public bool suspendWhileUserInput = true;
    [Tooltip("Se verdadeiro, desabilita qualquer escrita de transform quando um CameraOrbitController ativo está presente (você assume controle manual).")]
    public bool disableIfOrbitControllerPresent = false;

    [Header("Ângulos de Preset (graus)")]
    [Range(0f, 89f)] public float isometricTilt = 50f; // inclinação para ISO
        [Range(0f, 89f)] public float lowAngleTilt = 25f;  // inclinação para Cinematic
        [Range(-180f, 180f)] public float additionalYaw = 0f; // ajuste fino de yaw

        private Camera _cam;
        private Vector3 _posVel;
        private Quaternion _targetRot;
        private Vector3 _targetPos;
        private bool _initialized;
        private PieceColor _lastSideToMove = PieceColor.White;
    private CameraOrbitController _orbit;

    // Guardar delegates para desinscrever corretamente
    private System.Action<BoardState> _onBoardResetHandler;
    private System.Action<BoardState> _onBoardChangedHandler;

        void OnEnable()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (autoFindSynchronizer && synchronizer == null)
            {
                synchronizer = FindObjectOfType<BoardSynchronizer>();
            }
            _orbit = GetComponent<CameraOrbitController>();
            if (synchronizer != null)
            {
                synchronizer.OnMoveApplied += HandleMoveApplied;
                _onBoardResetHandler = OnBoardReset;
                _onBoardChangedHandler = OnBoardChanged;
                synchronizer.OnBoardReset += _onBoardResetHandler;
                synchronizer.OnBoardChanged += _onBoardChangedHandler;
                _lastSideToMove = synchronizer.State.SideToMove;
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

        void OnDisable()
        {
            if (synchronizer != null)
            {
                synchronizer.OnMoveApplied -= HandleMoveApplied;
                if (_onBoardResetHandler != null) synchronizer.OnBoardReset -= _onBoardResetHandler;
                if (_onBoardChangedHandler != null) synchronizer.OnBoardChanged -= _onBoardChangedHandler;
            }
        }

        private void HandleMoveApplied(Move mv, BoardState state)
        {
            if (rotateWithSideToMove)
            {
                // Se lado a mover mudou, alterna a visão
                if (state.SideToMove != _lastSideToMove)
                {
                    faceFromWhiteSide = (state.SideToMove == PieceColor.White);
                    _lastSideToMove = state.SideToMove;
                    Apply();
                }
            }
        }

        public void Apply(bool instant = false)
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
                _targetPos = boardCenter + upDir * camHeight + manualOffset;
                _targetRot = Quaternion.LookRotation((boardCenter - _targetPos).normalized, upDir);
            }
            else
            {
                _cam.orthographic = false;
                _cam.fieldOfView = perspectiveFOV;
                // Selecionar tilt/yaw a partir do preset
                float tilt = isometricTilt;
                switch (preset)
                {
                    case CameraPreset.Isometric: tilt = isometricTilt; break;
                    case CameraPreset.LowAngleCinematic: tilt = lowAngleTilt; break;
                    case CameraPreset.TopDownOrtho: tilt = 89f; break;
                }
                float yaw = faceFromWhiteSide ? 0f : 180f;
                yaw += additionalYaw;
                // Distância proporcional ao tabuleiro
                float extent = (8 * squareSize);
                float distance = extent * perspectiveDistanceMultiplier;
                // Converter tilt para componentes vertical/horizontal
                float tRad = Mathf.Deg2Rad * Mathf.Clamp(tilt, 0.01f, 89f);
                float upComp = Mathf.Sin(tRad) * distance;
                float backComp = Mathf.Cos(tRad) * distance;
                Vector3 dirFlat = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Vector3 camPos = boardCenter - dirFlat * backComp + Vector3.up * upComp;
                _targetPos = camPos + manualOffset;
                _targetRot = Quaternion.LookRotation((boardCenter - _targetPos).normalized, boardUp);
            }

            if (!smoothTransition || !_initialized || instant)
            {
                transform.position = _targetPos;
                transform.rotation = _targetRot;
                _initialized = true;
            }
            else
            {
                // Aplicar suavização na próxima Update
            }
        }

        [ContextMenu("Apply Now (Instant)")]
        private void ApplyNowContextMenu()
        {
            Apply(true);
        }

        [ContextMenu("Preset: Board Showcase (White)")]
        private void ApplyPresetBoardShowcaseWhite()
        {
            // Approximate the look in your screenshot: perspective, isometric tilt, closer framing, from white side
            orthographicMode = false;
            preset = CameraPreset.Isometric;
            isometricTilt = 42f;
            perspectiveFOV = 50f;
            perspectiveDistanceMultiplier = 0.85f;
            faceFromWhiteSide = true;
            additionalYaw = 0f;
            smoothTransition = true;
            Apply(true);
        }

        void LateUpdate()
        {
            if (!smoothTransition || !_initialized) return;
            // Se há um controlador de órbita e está ativo, podemos pausar nossas escritas para evitar luta de controles
            if (_orbit != null && _orbit.enabled)
            {
                if (disableIfOrbitControllerPresent) return;
                if (suspendWhileUserInput && _orbit.IsUserActive)
                {
                    // Enquanto o usuário está ativo, não mexe.
                    return;
                }
                // Se o usuário acabou de soltar, aceita a pose atual como alvo para evitar um "pulo" pós-órbita.
                if (!_orbit.IsUserActive)
                {
                    _targetPos = transform.position;
                    _targetRot = transform.rotation;
                }
            }
            // Suavizar posição
            transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _posVel, Mathf.Max(0.0001f, smoothTime));
            // Suavizar rotação
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Mathf.Clamp01(rotationLerpSpeed * Time.deltaTime));
        }

        private void OnBoardReset(BoardState state)
        {
            // Em reset, aplicamos instantâneo para evitar transições estranhas
            Apply(true);
        }

        private void OnBoardChanged(BoardState state)
        {
            if (rotateWithSideToMove)
            {
                Apply();
            }
        }
    }
}
