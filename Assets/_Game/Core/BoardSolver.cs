using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>Result of solving a board.</summary>
    public sealed class SolveResult
    {
        /// <summary>False when no sequence of moves frees the target block.</summary>
        public bool IsSolvable { get; }

        /// <summary>Optimal move sequence (empty if already solved or unsolvable).</summary>
        public IReadOnlyList<Move> Moves { get; }

        /// <summary>Minimum number of moves, or -1 if unsolvable.</summary>
        public int MinMoves => IsSolvable ? Moves.Count : -1;

        internal SolveResult(bool isSolvable, IReadOnlyList<Move> moves)
        {
            IsSolvable = isSolvable;
            Moves = moves;
        }
    }

    /// <summary>
    /// Breadth-first solver. A move is one block sliding any distance in one
    /// direction, so the result is the minimum move count a player can achieve.
    /// </summary>
    /// <example>
    /// <code>
    /// SolveResult r = BoardSolver.Solve(board);
    /// if (r.IsSolvable &amp;&amp; r.Moves.Count &gt; 0) Move hint = r.Moves[0];
    /// </code>
    /// </example>
    public static class BoardSolver
    {
        /// <summary>Solves the board; <paramref name="maxStates"/> caps the search (0 = no cap).</summary>
        public static SolveResult Solve(Board board, int maxStates = 0)
        {
            if (board.IsSolved)
                return new SolveResult(true, new List<Move>());

            var space = new StateSpace(board);
            ulong startKey = space.StartKey;
            var parents = new Dictionary<ulong, (ulong Parent, Move Move)> { [startKey] = (startKey, default) };
            var queue = new Queue<ulong>();
            var neighbors = new List<(ulong Key, Move Move)>();
            queue.Enqueue(startKey);

            while (queue.Count > 0)
            {
                ulong key = queue.Dequeue();
                space.GetNeighbors(key, neighbors);
                foreach ((ulong next, Move move) in neighbors)
                {
                    if (parents.ContainsKey(next))
                        continue;
                    parents[next] = (key, move);
                    if (space.IsSolved(next))
                        return new SolveResult(true, Reconstruct(parents, startKey, next));
                    if (maxStates > 0 && parents.Count >= maxStates)
                        return new SolveResult(false, new List<Move>());
                    queue.Enqueue(next);
                }
            }
            return new SolveResult(false, new List<Move>());
        }

        /// <summary>Next optimal move from this position, or null if solved/unsolvable.</summary>
        public static Move? GetHint(Board board)
        {
            SolveResult r = Solve(board);
            return r.IsSolvable && r.Moves.Count > 0 ? r.Moves[0] : (Move?)null;
        }

        private static List<Move> Reconstruct(Dictionary<ulong, (ulong Parent, Move Move)> parents, ulong startKey, ulong endKey)
        {
            var moves = new List<Move>();
            for (ulong k = endKey; k != startKey; k = parents[k].Parent)
                moves.Add(parents[k].Move);
            moves.Reverse();
            return moves;
        }
    }
}
