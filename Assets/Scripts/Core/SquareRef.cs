using UnityEngine;

namespace Chess3D.Core
{
    /// <summary>
    /// Marca uma casa gerada do tabuleiro com suas coordenadas (0-7,0-7).
    /// Facilita raycast -> coordenada sem depender de cálculos de offset.
    /// </summary>
    public class SquareRef : MonoBehaviour
    {
        [Tooltip("Coordenada X (file) 0=a .. 7=h")] public int x;
        [Tooltip("Coordenada Y (rank) 0=1 .. 7=8")] public int y;
    }
}
