using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testes : MonoBehaviour
{
    
    void Start()
    {
        StartCoroutine(teste());
    }

    IEnumerator teste()
    {
        yield return new WaitForSeconds(5);
        var go = GameObject.Find("Black_King_E_8");
        if (go == null)
        {
            Debug.LogWarning("Black_King_E_8 não encontrado para teste (script testes.cs). Remover ou ajustar nome se não for mais necessário.");
            yield break;
        }
        PieceConfig config = go.GetComponent<PieceConfig>();
        if (config == null)
        {
            Debug.LogWarning("PieceConfig não encontrado em Black_King_E_8.");
            yield break;
        }
        //config.teste();
    }
}
