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
        PieceConfig config = GameObject.Find("Black_King_E_8").GetComponent<PieceConfig>();
        //config.teste();
    }
}
