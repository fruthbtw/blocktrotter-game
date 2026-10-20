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
        private const int BitsPerBlock = 3;   // positions 0..7 fit any board up to 8x8
        private const int MaxBlocks = 64 / BitsPerBlock;

        /// <summary>Solves the board; <paramref name="maxStates"/> caps the search (0 = no cap).</summary>
        public static SolveResult Solve(Board board, int maxStates = 0)
        {
            if (board.IsSolved)
                return new SolveResult(true, new List<Move>());

            int n = board.Blocks.Count;
            if (n > MaxBlocks)
                throw new System.ArgumentException($"Solver supports at most {MaxBlocks} blocks.");

            var start = new int[n];
            for (int i = 0; i < n; i++) start[i] = board.Blocks[i].Position;

            ulong startKey = Pack(start);
            var parents = new Dictionary<ulong, (ulong Parent, Move Move)> { [startKey] = (startKey, default) };
            var queue = new Queue<ulong>();
            queue.Enqueue(startKey);
            var positions = new int[n];

            while (queue.Count > 0)
            {
                ulong key = queue.Dequeue();
                Unpack(key, positions);
                Board state = board.WithPositions(positions);

                foreach (Move move in state.GetLegalMoves())
                {
                    positions[move.BlockIndex] += move.Delta;
                    ulong nextKey = Pack(positions);
                    bool solved = move.BlockIndex == board.TargetIndex
                        && positions[move.BlockIndex] + board.Blocks[board.TargetIndex].Length == board.Size;
                    positions[move.BlockIndex] -= move.Delta;

                    if (parents.ContainsKey(nextKey))
                        continue;
                    parents[nextKey] = (key, move);
                    if (solved)
                        return new SolveResult(true, Reconstruct(parents, startKey, nextKey));
                    if (maxStates > 0 && parents.Count >= maxStates)
                        return new SolveResult(false, new List<Move>());
                    queue.Enqueue(nextKey);
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

        private static ulong Pack(int[] positions)
        {
            ulong key = 0;
            for (int i = 0; i < positions.Length; i++)
                key |= (ulong)positions[i] << (i * BitsPerBlock);
            return key;
        }

        private static void Unpack(ulong key, int[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
                positions[i] = (int)((key >> (i * BitsPerBlock)) & 0b111);
        }
    }
}
