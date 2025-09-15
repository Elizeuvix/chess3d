using System.Collections.Generic;

namespace Chess3D.Core
{
    public class GameHistory
    {
        public readonly List<Move> Moves = new();
        // Snapshots opcionais (para undo completo). Guardamos estados antes de cada move.
        private readonly List<BoardState> _snapshots = new();

        public void RecordPreState(BoardState state)
        {
            _snapshots.Add(state.Clone());
        }

        public void AddMove(Move m)
        {
            Moves.Add(m);
        }

        public bool CanUndo => _snapshots.Count > 0 && _snapshots.Count == Moves.Count;

        public BoardState UndoLast(out Move undone)
        {
            undone = default;
            if (!CanUndo) return null;
            int idx = Moves.Count - 1;
            undone = Moves[idx];
            Moves.RemoveAt(idx);
            var restored = _snapshots[idx];
            _snapshots.RemoveAt(idx);
            return restored.Clone();
        }

        public void Reset()
        {
            Moves.Clear();
            _snapshots.Clear();
        }
    }
}
