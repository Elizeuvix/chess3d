using System.Collections.Generic;
using UnityEngine;

namespace Chess3D.Core
{
    public class MoveHighlightManager : MonoBehaviour
    {
        [Tooltip("Prefab simples usado para destacar casas. Pode ser um plano com material semi-transparente.")]
        public GameObject highlightPrefab;
        [Tooltip("Altura Y para posicionar o highlight (levemente acima do tabuleiro).")]
        public float yOffset = 0.01f;
        [Tooltip("Escala multiplicadora para casar com o tamanho da casa (1 = igual ao squareSize).")]
        public float sizeScale = 0.95f;
        [Tooltip("Reutilizar instâncias em vez de destruir/criar.")]
        public bool reusePool = true;

        private readonly List<GameObject> _active = new();
        private readonly Queue<GameObject> _pool = new();
    [Tooltip("Referência ao BoardSynchronizer. Se vazio, tenta GetComponent no mesmo GameObject.")]
    public BoardSynchronizer synchronizer;

        void Awake()
        {
            if (synchronizer == null)
            {
                synchronizer = GetComponent<BoardSynchronizer>();
            }
        }

        public void ShowMoves(IEnumerable<Move> moves)
        {
            Clear();
            if (highlightPrefab == null || synchronizer == null) return;
            // Agrupar por destino para evitar duplicados em promoção (Q,R,B,N)
            var used = new HashSet<(int x,int y)>();
            foreach (var m in moves)
            {
                var key = (m.ToX, m.ToY);
                if (used.Contains(key)) continue;
                used.Add(key);
                var pos = SquareToWorld(m.ToX, m.ToY);
                var go = GetInstance();
                go.transform.position = pos;
                float s = synchronizer.squareSize * sizeScale;
                go.transform.localScale = new Vector3(s, go.transform.localScale.y, s);
                go.SetActive(true);
                _active.Add(go);
            }
        }

        public void Clear()
        {
            for (int i=0;i<_active.Count;i++)
            {
                if (reusePool)
                {
                    _active[i].SetActive(false);
                    _pool.Enqueue(_active[i]);
                }
                else
                {
                    Destroy(_active[i]);
                }
            }
            _active.Clear();
        }

        private GameObject GetInstance()
        {
            if (reusePool && _pool.Count > 0)
            {
                var inst = _pool.Dequeue();
                return inst;
            }
            return Instantiate(highlightPrefab, transform);
        }

        private Vector3 SquareToWorld(int x,int y)
        {
            if (synchronizer == null) return Vector3.zero;
            return synchronizer.originOffset + new Vector3(x * synchronizer.squareSize, yOffset, y * synchronizer.squareSize);
        }
    }
}
