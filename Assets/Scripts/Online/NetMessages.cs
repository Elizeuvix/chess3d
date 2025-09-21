using UnityEngine;

namespace Chess3D.Online
{
    // Envelope + specific message payloads (serialized via JsonUtility)
    [System.Serializable]
    public class NetEnvelope
    {
        public string type;
        public string payload; // nested JSON string
    }

    [System.Serializable]
    public class MsgHello
    {
        public string name;
        public string version;
    }

    [System.Serializable]
    public class MsgAssign
    {
        public string color; // "White" or "Black"
        public string fen;   // initial board state
    }

    [System.Serializable]
    public class MsgMove
    {
        public int fromX; public int fromY; public int toX; public int toY; public int promotion; // PieceType enum as int
    }

    [System.Serializable]
    public class MsgReset
    {
        public string fen;
        public string reason; // optional
    }

    [System.Serializable]
    public class MsgResign
    {
        public string color; // Who resigned
    }
}
