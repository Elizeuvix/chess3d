using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectsControlUI : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Toggle autoHideLobby;
    [SerializeField] GameObject panelLobby;
    [SerializeField] TextMeshProUGUI labelToggleText;

    void Start()
    {
        // Liga o Toggle ao painel do Lobby
        if (autoHideLobby != null)
        {
            autoHideLobby.onValueChanged.AddListener(isOn =>
            {
                if (panelLobby != null) panelLobby.SetActive(isOn);
                if (labelToggleText != null)
                    labelToggleText.text = isOn ? "Esconder Lobby" : "Exibir Lobby";
            });
            // Estado inicial
            if (panelLobby != null) panelLobby.SetActive(autoHideLobby.isOn);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
