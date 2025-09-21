using UnityEngine;

namespace Chess3D.Core
{
    // Helper para animar rotação Y individual sem afetar outros objetos
    public class MoveWobbleHelper : MonoBehaviour
    {
        private Coroutine _current;

        public void AnimateYRotation(float startY, float targetY, float duration, float smooth)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(DoAnim(startY, targetY, duration, smooth));
        }

        private System.Collections.IEnumerator DoAnim(float startY, float targetY, float duration, float smooth)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t/duration);
                float y = Mathf.LerpAngle(startY, targetY, smooth > 0f ? Mathf.SmoothStep(0f,1f,u) : u);
                var euler = transform.eulerAngles;
                euler.y = y;
                transform.eulerAngles = euler;
                yield return null;
            }
            var finalEuler = transform.eulerAngles;
            finalEuler.y = targetY;
            transform.eulerAngles = finalEuler;
            _current = null;
        }
    }
}
