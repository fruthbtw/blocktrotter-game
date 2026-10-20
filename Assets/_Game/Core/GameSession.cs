using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>Hints the player owns; shared across levels.</summary>
    /// <example><code>var wallet = new HintWallet(3); if (wallet.TrySpend()) { /* show hint */ }</code></example>
    public sealed class HintWallet
    {
        /// <summary>Hints available.</summary>
        public int Count { get; private set; }

        /// <summary>Creates a wallet holding <paramref name="count"/> hints.</summary>
        public HintWallet(int count) => Count = count;

        /// <summary>Removes one hint; false if none left.</summary>
        public bool TrySpend()
        {
            if (Count <= 0) return false;
            Count--;
            return true;
        }

        /// <summary>Adds hints (e.g. after a rewarded ad).</summary>
        public void Add(int amount) => Count += amount;
    }

    /// <summary>
    /// One play-through of a level: current board, move history (for undo and the
    /// move counter), restart and hints. No Unity dependencies.
    /// </summary>
    /// <example>
    /// <code>
    /// var session = new GameSession(levelDatabase.LoadBoard(0));
    /// session.TryMove(new Move(2, -1));
    /// if (session.IsSolved) { int moves = session.MoveCount; }
    /// </code>
    /// </example>
    public sealed class GameSession
    {
        private readonly Stack<Board> _history = new Stack<Board>();

        /// <summary>Board the level started from.</summary>
        public Board InitialBoard { get; }

        /// <summary>Current board.</summary>
        public Board Board { get; private set; }

        /// <summary>Moves made so far (undo removes one).</summary>
        public int MoveCount => _history.Count;

        /// <summary>True once the target block reaches the exit.</summary>
        public bool IsSolved => Board.IsSolved;

        /// <summary>Starts a session on <paramref name="initial"/>.</summary>
        public GameSession(Board initial)
        {
            InitialBoard = initial;
            Board = initial;
        }

        /// <summary>Applies a legal move; false (and no change) if illegal or already solved.</summary>
        public bool TryMove(Move move)
        {
            if (IsSolved || !Board.CanApply(move))
                return false;
            _history.Push(Board);
            Board = Board.Apply(move);
            return true;
        }

        /// <summary>Reverts the last move; false if there is none or the level is solved.</summary>
        public bool Undo()
        {
            if (IsSolved || _history.Count == 0)
                return false;
            Board = _history.Pop();
            return true;
        }

        /// <summary>Back to the initial board with zero moves.</summary>
        public void Restart()
        {
            _history.Clear();
            Board = InitialBoard;
        }

        /// <summary>
        /// Spends one hint and plays the next optimal move. Returns the move, or null
        /// (spending nothing) when solved, unsolvable or the wallet is empty.
        /// </summary>
        public Move? UseHint(HintWallet wallet)
        {
            if (IsSolved || wallet.Count <= 0)
                return null;
            Move? hint = BoardSolver.GetHint(Board);
            if (!hint.HasValue)
                return null;
            wallet.TrySpend();
            TryMove(hint.Value);
            return hint;
        }
    }
}
