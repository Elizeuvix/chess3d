using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace Chess3D.Online
{
    // Lightweight TCP transport for 1v1. Newline-delimited UTF8 messages.
    public class SimpleTcpTransport : MonoBehaviour
    {
        public int listenPort = 7777;
        public string connectIp = "127.0.0.1";
        public int connectPort = 7777;

        private TcpListener _listener;
        private TcpClient _client; // active connection (host's accepted or client's own)
        private NetworkStream _stream;
        private Thread _readThread;
        private volatile bool _running;
        private readonly object _sendLock = new object();
        private readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();

        public Action OnConnected;
        public Action OnDisconnected;
        public Action<string> OnMessage;

        public bool IsConnected => _client != null && _client.Connected;
        public bool IsHosting { get; private set; }

        void Update()
        {
            while (_incoming.TryDequeue(out var msg))
            {
                try { OnMessage?.Invoke(msg); } catch (Exception ex) { Debug.LogWarning($"[TCP] OnMessage handler error: {ex.Message}"); }
            }
        }

        public void StartHost()
        {
            StopAll();
            try
            {
                _listener = new TcpListener(IPAddress.Any, listenPort);
                _listener.Start();
                IsHosting = true;
                _listener.BeginAcceptTcpClient(OnClientAccepted, null);
                Debug.Log($"[TCP] Hosting on port {listenPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP] Failed to host: {ex.Message}");
                StopAll();
            }
        }

        public void Connect()
        {
            StopAll();
            try
            {
                _client = new TcpClient();
                _client.Connect(connectIp, connectPort);
                SetupClient(_client);
                Debug.Log($"[TCP] Connected to {connectIp}:{connectPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP] Failed to connect: {ex.Message}");
                StopAll();
            }
        }

        private void OnClientAccepted(IAsyncResult ar)
        {
            try
            {
                if (_listener == null) return;
                var client = _listener.EndAcceptTcpClient(ar);
                // Only single client for 1v1; stop accepting further
                _listener.Stop();
                _listener = null;
                SetupClient(client);
                Debug.Log("[TCP] Client connected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TCP] Accept failed: {ex.Message}");
            }
        }

        private void SetupClient(TcpClient c)
        {
            _client = c;
            _stream = _client.GetStream();
            _running = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
            try { OnConnected?.Invoke(); } catch { }
        }

        private void ReadLoop()
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    int n = _stream.Read(buffer, 0, buffer.Length);
                    if (n <= 0) break;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, n));
                    // Extract lines
                    string content = sb.ToString();
                    int idx;
                    while ((idx = content.IndexOf('\n')) >= 0)
                    {
                        string line = content.Substring(0, idx).Trim('\r');
                        if (!string.IsNullOrEmpty(line)) _incoming.Enqueue(line);
                        content = content.Substring(idx + 1);
                    }
                    sb = new StringBuilder(content);
                }
            }
            catch (Exception)
            {
                // Socket errors cause disconnect
            }
            finally
            {
                _running = false;
                UnityMain(() => { try { OnDisconnected?.Invoke(); } catch { } });
                StopAll();
            }
        }

        private void UnityMain(Action a)
        {
            // In this simple approach, we already enqueue messages to Update; this helper is a placeholder
            try { a(); } catch { }
        }

        public void Send(string message)
        {
            if (!IsConnected) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message + "\n");
                lock (_sendLock)
                {
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TCP] Send failed: {ex.Message}");
            }
        }

        public void StopAll()
        {
            _running = false;
            try { _stream?.Close(); } catch { }
            _stream = null;
            try { _client?.Close(); } catch { }
            _client = null;
            try { _listener?.Stop(); } catch { }
            _listener = null;
            try { _readThread?.Join(5); } catch { }
            _readThread = null;
            IsHosting = false;
        }

        void OnDestroy()
        {
            StopAll();
        }
    }
}
