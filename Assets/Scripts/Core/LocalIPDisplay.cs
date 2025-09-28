using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Net;
using System.Net.Sockets;
using UnityEngine.EventSystems;

namespace Chess3D.Core
{
    public class LocalIPDisplay : MonoBehaviour, IPointerClickHandler

    {
        public TMP_Text ipText;
        public TMP_InputField inputField;
        public float doubleClickTime = 0.3f;
        private float lastClickTime = -1f;

        void Start()
        {
            if (ipText != null)
                ipText.text = "IP Local: " + GetLocalIPAddress();
        }

        public static string GetLocalIPAddress()
        {
            string localIP = "Não encontrado";
            try
            {
                foreach (var host in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (host.AddressFamily == AddressFamily.InterNetwork)
                        return host.ToString();
                }
            }
            catch { }
            return localIP;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Time.time - lastClickTime < doubleClickTime)
            {
                OnDoubleClick();
            }
            lastClickTime = Time.time;
        }

        private void OnDoubleClick()
        {
            Debug.Log("Duplo clique detectado!");
            inputField.text = GetLocalIPAddress();
        }        
    }
}
