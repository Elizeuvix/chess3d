using UnityEngine;

namespace Chess3D.Core
{
    /// <summary>
    /// Gera proceduralmente as 64 casas do tabuleiro com materiais claros/escuros alternados.
    /// Integra-se com o BoardSynchronizer para usar o mesmo tamanho de casa e origem.
    /// Pode ser usado para substituir um tabuleiro único de textura sólida.
    /// </summary>
    [ExecuteAlways]
    public class BoardSquareGenerator : MonoBehaviour
    {
        [Header("Dependências (Opcional)")] public BoardSynchronizer synchronizer;

        [Header("Configuração Visual")] public GameObject squarePrefab; // Prefab simples (Quad/Plane) com MeshRenderer
        public Material lightMaterial;
        public Material darkMaterial;
        [Range(0.1f,5f)] public float squareSize = 1f; // Usado se não houver synchronizer
        public float yOffset = 0f;
        public bool centerPivot = true; // Se true, offset aplicado para alinhar centro do tabuleiro na posição do GameObject

        [Header("Geração")] public bool autoGenerateOnStart = true;
        public bool clearBeforeGenerate = true;
        public string squareLayerName = "Board"; // Layer usada para raycast de seleção

        [Header("Nomenclatura")] public bool nameAlgebraic = true; // a1..h8
        public bool addCoordsInName = false; // Ex: a1_(0,0)

    // Controle interno para regeneração segura em modo editor (evita DestroyImmediate em OnValidate)
    private bool _pendingRegenerateEditor;

    [Header("Sync com Peças")]
    [Tooltip("Se verdadeiro, ajusta automaticamente o pieceBaseY do BoardSynchronizer para o topo da casa (após gerar).")]
    public bool setPieceBaseToSquareTop = true;
    [Tooltip("Offset adicional aplicado sobre o topo da casa ao definir pieceBaseY (ex.: 0.005 para evitar clipping).")]
    public float pieceBaseExtraOffset = 0f;

        /// <summary>
        /// Gera ou regenera as casas.
        /// </summary>
        public void Generate()
        {
            if (squarePrefab == null)
            {
                Debug.LogWarning("BoardSquareGenerator: squarePrefab não atribuído.");
                return;
            }

            if (synchronizer != null)
            {
                squareSize = synchronizer.squareSize;
            }

            if (clearBeforeGenerate)
            {
                // Remover filhos existentes (somente em modo editor ou play se apropriado)
#if UNITY_EDITOR
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    if (!Application.isPlaying)
                        DestroyImmediate(transform.GetChild(i).gameObject);
                    else
                        Destroy(transform.GetChild(i).gameObject);
                }
#else
                for (int i = transform.childCount - 1; i >= 0; i--)
                    Destroy(transform.GetChild(i).gameObject);
#endif
            }

            int boardLayer = LayerMask.NameToLayer(squareLayerName);
            if (boardLayer == -1)
            {
                Debug.LogWarning("BoardSquareGenerator: Layer '" + squareLayerName + "' não existe. Crie-a em Project Settings > Tags and Layers se quiser usá-la.");
            }

            Vector3 origin = transform.position;
            if (centerPivot)
            {
                // Ajustar para que (0,0) fique deslocado de forma a centralizar tabuleiro no pivot
                origin -= new Vector3(squareSize * 7 / 2f, 0f, squareSize * 7 / 2f);
            }

            float lastTopY = 0f;
            for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                var go = Instantiate(squarePrefab, transform);
                go.transform.localPosition = Vector3.zero; // reset
                go.transform.position = origin + new Vector3(x * squareSize, yOffset, y * squareSize);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(squareSize, go.transform.localScale.y, squareSize);

                if (boardLayer != -1) go.layer = boardLayer;

                var mr = go.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    // Convenção: a1 (0,0) é casa escura. Portanto, (x+y)%2 == 0 -> escura
                    bool dark = (x + y) % 2 == 0;
                    mr.sharedMaterial = dark ? darkMaterial : lightMaterial;
                    // Registrar topo desta casa
                    lastTopY = mr.bounds.max.y;
                }

                // Collider para raycast de seleção
                if (go.GetComponent<Collider>() == null)
                {
                    var meshFilter = go.GetComponentInChildren<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = meshFilter.sharedMesh;
                    }
                    else
                    {
                        go.AddComponent<BoxCollider>();
                    }
                }

                if (nameAlgebraic)
                {
                    string alg = AlgebraicFromXY(x, y);
                    go.name = alg + (addCoordsInName ? $"_({x},{y})" : "");
                }
                else
                {
                    go.name = $"Square_{x}_{y}";
                }

                // Adicionar/metadados de referência
                var sr = go.GetComponent<SquareRef>();
                if (sr == null) sr = go.AddComponent<SquareRef>();
                sr.x = x; sr.y = y;
            }

            // Atualizar originOffset do synchronizer se desejado
            if (synchronizer != null)
            {
                if (centerPivot)
                {
                    synchronizer.originOffset = origin; // já é o canto a1
                }
                else
                {
                    synchronizer.originOffset = transform.position; // assume pivot no canto
                }

                if (setPieceBaseToSquareTop)
                {
                    synchronizer.pieceBaseY = lastTopY + pieceBaseExtraOffset;
                }
            }
        }

        private string AlgebraicFromXY(int x, int y)
        {
            // Converte (0,0) -> a1, (7,7) -> h8
            char file = (char)('a' + x);
            char rank = (char)('1' + y);
            return new string(new[] { file, rank });
        }

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                Generate();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoGenerateOnStart && !Application.isPlaying)
            {
                // Marca para regenerar no Update (contexto seguro)
                _pendingRegenerateEditor = true;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying && _pendingRegenerateEditor)
            {
                _pendingRegenerateEditor = false;
                Generate();
            }
        }
#endif
    }
}
