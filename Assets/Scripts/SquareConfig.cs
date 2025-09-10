using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareConfig : MonoBehaviour
{
    public Material squareMaterial = null;
    Color baseColor;
    Renderer rend;

    public void SetHighlight(bool highlight)
    {
        rend = transform.GetComponent<Renderer>();
            this.rend.material = squareMaterial;
    }

    private void Update()
    {
        //if (Input.GetMouseButtonDown(1))
            //Debug.Log(this.gameObject.name);
    }

    private void OnMouseEnter()
    {
        if (Input.GetMouseButtonDown(1))
            Debug.Log(this.gameObject.name);
    }
}
