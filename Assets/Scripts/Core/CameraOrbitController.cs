using UnityEngine;

namespace Chess3D.Core
{
    // Simple orbit/zoom/pan controller to showcase 3D models.
    // Attach alongside CameraAutoAlign. It will orbit around the board center derived from BoardSynchronizer.
    [RequireComponent(typeof(Camera))]
    public class CameraOrbitController : MonoBehaviour
    {
        public BoardSynchronizer synchronizer;
        [Header("Buttons/Keys")] public int orbitMouseButton = 1; // right mouse
        public KeyCode panKey = KeyCode.Mouse2; // middle mouse
        [Header("Orbit")] public float orbitSpeed = 120f; public float minTilt = 5f; public float maxTilt = 85f;
        [Header("Zoom")] public float zoomSpeed = 5f; public float minDistance = 4f; public float maxDistance = 40f;
        [Header("Pan")] public float panSpeed = 1.0f;
    [Header("Damping")] public float damping = 10f;
    [Tooltip("Ao soltar o botão do mouse, para a câmera imediatamente (sem inércia).")]
    public bool snapOnRelease = true;

        private float _yaw; private float _tilt = 35f; private float _distance = 14f;
        private Vector3 _target;
        private Vector3 _vel;
    public bool IsUserActive { get; private set; }

        void Start()
        {
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            UpdateTargetFromBoard();
            // Initialize angles based on current camera
            var fwd = (transform.position - _target).normalized;
            _distance = Mathf.Clamp(Vector3.Distance(transform.position, _target), minDistance, maxDistance);
            var flat = new Vector3(fwd.x, 0, fwd.z); flat.Normalize();
            _tilt = Mathf.Clamp(Vector3.SignedAngle(fwd, flat, Vector3.Cross(flat, Vector3.up)) + 90f, minTilt, maxTilt);
            _yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        void Update()
        {
            if (synchronizer != null)
            {
                // Keep center locked to board center
                UpdateTargetFromBoard();
            }

            IsUserActive = false;
            // Orbit
            if (Input.GetMouseButton(orbitMouseButton))
            {
                _yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                _tilt = Mathf.Clamp(_tilt - Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime, minTilt, maxTilt);
                IsUserActive = true;
            }

            // Zoom
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                _distance = Mathf.Clamp(_distance - scroll * zoomSpeed, minDistance, maxDistance);
                IsUserActive = true;
            }

            // Pan (hold middle mouse)
            if (Input.GetKey(panKey))
            {
                var right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;
                var forward = Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
                Vector3 delta = (-right * Input.GetAxis("Mouse X") + -forward * Input.GetAxis("Mouse Y")) * panSpeed;
                _target += delta;
                IsUserActive = true;
            }
        }

        private bool _wasUserActive;

        void LateUpdate()
        {
            // Compute desired camera transform
            Vector3 dir = Quaternion.Euler(_tilt, _yaw, 0) * Vector3.back;
            Vector3 desiredPos = _target + dir * _distance;
            Quaternion desiredRot = Quaternion.LookRotation((_target - transform.position).normalized, Vector3.up);

            if (IsUserActive)
            {
                // Suaviza enquanto o usuário está interagindo
                float t = 1f - Mathf.Exp(-damping * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPos, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
            }
            else
            {
                if (snapOnRelease && _wasUserActive)
                {
                    // Ao soltar, fixa imediatamente na pose desejada para evitar drifts
                    transform.position = desiredPos;
                    transform.rotation = desiredRot;
                }
                // Caso contrário, não mexe na câmera (mantém estática)
            }

            _wasUserActive = IsUserActive;
        }

        private void UpdateTargetFromBoard()
        {
            if (synchronizer == null) return;
            float s = synchronizer.squareSize;
            Vector3 origin = synchronizer.originOffset;
            _target = origin + new Vector3(7 * s * 0.5f, 0, 7 * s * 0.5f);
        }
    }
}
