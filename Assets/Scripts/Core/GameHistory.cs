using System.Collections.Generic;

namespace Chess3D.Core
{
    public class GameHistory
    {
        public readonly List<Move> Moves = new();
        public readonly List<string> SAN = new();
        // Snapshots opcionais (para undo completo). Guardamos estados antes de cada move.
        private readonly List<BoardState> _snapshots = new();
        // Pilha de redo (movimentos desfeitos que podem ser refeitos)
        private readonly List<Move> _redo = new();
        private readonly List<string> _redoSAN = new();

        public void RecordPreState(BoardState state)
        {
            _snapshots.Add(state.Clone());
        }

        public void AddMove(Move m)
        {
            Moves.Add(m);
            // Não limpamos redo aqui; quem chamar AddMove decide quando limpar (ex: BoardSynchronizer.ApplyMove(clearRedo:true))
        }

        public void AddSAN(string san)
        {
            SAN.Add(san);
        }

        public bool CanUndo => _snapshots.Count > 0 && _snapshots.Count == Moves.Count;

        public BoardState UndoLast(out Move undone)
        {
            undone = default;
            if (!CanUndo) return null;
            int idx = Moves.Count - 1;
            undone = Moves[idx];
            Moves.RemoveAt(idx);
            // Pop matching SAN
            string san = SAN.Count > idx ? SAN[idx] : string.Empty;
            if (SAN.Count > idx) SAN.RemoveAt(idx);
            var restored = _snapshots[idx];
            _snapshots.RemoveAt(idx);
            // Empilha para possível redo
            _redo.Add(undone);
            _redoSAN.Add(san);
            return restored.Clone();
        }

    public bool CanRedo => _redo.Count > 0;

        public Move PopRedo()
        {
            int idx = _redo.Count - 1;
            var mv = _redo[idx];
            _redo.RemoveAt(idx);
            // remove paired SAN in lockstep
            if (_redoSAN.Count > idx) _redoSAN.RemoveAt(idx);
            return mv;
        }

        public void ClearRedo() { _redo.Clear(); _redoSAN.Clear(); }

        public void Reset()
        {
            Moves.Clear();
            SAN.Clear();
            _snapshots.Clear();
            _redo.Clear();
            _redoSAN.Clear();
        }
    }
}
