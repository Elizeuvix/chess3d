using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Diagonal : MonoBehaviour
{
    bool isPair = false;
    public int depth = 10;
    public int width = 2;
    public Material yvore;
    public Material brown;

    public int tamanho = 8;
    int[,] matriz;


    private void Awake()
    {
        matriz = new int[tamanho, tamanho];
    }
    private void Start1()
    {
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(x, 0, z);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                go.transform.SetParent(this.transform);
                go.name = $"{z}{x}";

                Renderer rend = go.GetComponent<Renderer>();

                if (isPair)
                {
                    rend.material = yvore;
                }
                else
                {
                    rend.material = brown;
                }
                isPair = !isPair;
            }

            isPair = !isPair;
        }
    }

    private void Start2()
    {
        int[,] columnLine = new int[8, 8]; // uma matriz 3x4 (3 linhas e 4 colunas)

        // preencher a matriz com alguns valores iniciais
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                columnLine[i, j] = i + j; // valores iniciais aleatórios
            }
        }

        // incrementar todos os elementos da matriz em 1
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                columnLine[i, j]++; // incrementar cada elemento
            }
        }

        Debug.Log($"Valor 1: {columnLine}");
    }


    private void Start()
    {
        for (int i = 0; i < tamanho; i++)
        {
            for (int j = 0; j < tamanho; j++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = new Vector3(i, 0, j);

                if (i == j)
                {
                    //RightUp
                    matriz[i, j] = 1;
                    go.transform.localScale = new Vector3(1f, 3f, 1f);
                }
                else if (i + j == tamanho - 1)
                {
                    //RightDown
                    matriz[i, j] = 2;
                    go.transform.localScale = new Vector3(1f, 3f, 1f);
                }
                else
                {
                    matriz[i, j] = 0;
                    go.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
        }
    }


}
