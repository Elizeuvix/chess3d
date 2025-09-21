using System;
using UnityEngine;
using Chess3D.Core;

namespace Chess3D.Online
{
    public class OnlineMatchManager : MonoBehaviour
    {
        public SimpleTcpTransport transport;
        public BoardSynchronizer synchronizer;

        [Header("Role")] public bool isHost = true;
        public string playerName = "Player";

        public bool IsOnlineActive { get; private set; }
        public PieceColor AssignedColor { get; private set; } = PieceColor.White;

        private bool _applyingRemote;

        void Start()
        {
            if (transport == null) transport = FindObjectOfType<SimpleTcpTransport>();
            if (synchronizer == null) synchronizer = FindObjectOfType<BoardSynchronizer>();
            if (transport != null)
            {
                transport.OnConnected += OnConnected;
                transport.OnMessage += OnMessage;
                transport.OnDisconnected += OnDisconnected;
            }
            if (synchronizer != null)
            {
                synchronizer.OnMoveApplied += OnLocalMoveApplied;
            }
        }

        public void Host()
        {
            if (transport == null) return;
            isHost = true;
            transport.StartHost();
        }

        public void Join()
        {
            if (transport == null) return;
            isHost = false;
            transport.Connect();
        }

        private void OnConnected()
        {
            IsOnlineActive = true;
            // Send Hello
            var hello = new MsgHello { name = playerName, version = Application.version };
            Send("Hello", hello);

            if (isHost)
            {
                // Assign colors: host=White by default
                AssignedColor = PieceColor.White;
                var msg = new MsgAssign { color = AssignedColor.ToString(), fen = synchronizer != null ? synchronizer.GetFEN() : string.Empty };
                Send("Assign", msg);
            }
        }

        private void OnDisconnected()
        {
            IsOnlineActive = false;
        }

        private void OnMessage(string json)
        {
            try
            {
                var env = JsonUtility.FromJson<NetEnvelope>(json);
                switch (env.type)
                {
                    case "Hello":
                        // ignore for now
                        break;
                    case "Assign":
                    {
                        var a = JsonUtility.FromJson<MsgAssign>(env.payload);
                        if (Enum.TryParse<PieceColor>(a.color, out var col))
                        {
                            // If we are client, we get host's color; we take the opposite
                            AssignedColor = (col == PieceColor.White) ? PieceColor.Black : PieceColor.White;
                        }
                        if (synchronizer != null && !string.IsNullOrEmpty(a.fen)) synchronizer.LoadFEN(a.fen);
                        break;
                    }
                    case "Move":
                    {
                        var m = JsonUtility.FromJson<MsgMove>(env.payload);
                        if (synchronizer != null)
                        {
                            var mv = new Move(m.fromX, m.fromY, m.toX, m.toY, (Chess3D.Core.PieceType)m.promotion);
                            _applyingRemote = true;
                            synchronizer.ApplyMove(mv);
                            _applyingRemote = false;
                        }
                        break;
                    }
                    case "Reset":
                    {
                        var r = JsonUtility.FromJson<MsgReset>(env.payload);
                        if (synchronizer != null)
                        {
                            if (!string.IsNullOrEmpty(r.fen)) synchronizer.LoadFEN(r.fen); else synchronizer.ResetGame();
                        }
                        break;
                    }
                    case "RequestReset":
                    {
                        if (isHost && synchronizer != null)
                        {
                            synchronizer.ResetGame();
                            var fen = synchronizer.GetFEN();
                            Send("Reset", new MsgReset { fen = fen, reason = "HostAccepted" });
                        }
                        break;
                    }
                    case "Resign":
                    {
                        var rg = JsonUtility.FromJson<MsgResign>(env.payload);
                        Debug.Log($"[Online] Opponent resigned: {rg.color}. Resetting game.");
                        synchronizer?.ResetGame();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Online] Bad message: {ex.Message} | {json}");
            }
        }

        private void OnLocalMoveApplied(Move mv, BoardState state)
        {
            if (!IsOnlineActive || _applyingRemote) return;
            // Relay local move to peer
            var net = new MsgMove { fromX = mv.FromX, fromY = mv.FromY, toX = mv.ToX, toY = mv.ToY, promotion = (int)mv.Promotion };
            Send("Move", net);
        }

        private void Send(string type, object payload)
        {
            if (transport == null || !transport.IsConnected) return;
            var env = new NetEnvelope { type = type, payload = JsonUtility.ToJson(payload) };
            var json = JsonUtility.ToJson(env);
            transport.Send(json);
        }

        // Public API for UI -------------------------------------------------
        public void ResetNetwork()
        {
            if (synchronizer == null || transport == null || !transport.IsConnected) { synchronizer?.ResetGame(); return; }
            if (isHost)
            {
                synchronizer.ResetGame();
                var fen = synchronizer.GetFEN();
                Send("Reset", new MsgReset { fen = fen, reason = "HostManual" });
            }
            else
            {
                Send("RequestReset", new MsgReset { reason = "ClientRequest" });
            }
        }

        public void Resign()
        {
            var colorStr = AssignedColor.ToString();
            Send("Resign", new MsgResign { color = colorStr });
            Debug.Log("[Online] You resigned. Resetting.");
            synchronizer?.ResetGame();
        }
    }
}
