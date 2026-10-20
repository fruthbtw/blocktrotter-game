using System;
using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>
    /// Square sliding-block board. The exit is on the right edge of
    /// <see cref="ExitRow"/>; the puzzle is solved when the target block's right
    /// end touches that edge.
    /// </summary>
    public sealed class Board
    {
        /// <summary>Default board width/height.</summary>
        public const int DefaultSize = 6;

        /// <summary>Default exit row (third row from the top).</summary>
        public const int DefaultExitRow = 2;

        private readonly Block[] _blocks;

        /// <summary>Width and height in cells.</summary>
        public int Size { get; }

        /// <summary>Row whose right edge has the exit.</summary>
        public int ExitRow { get; }

        /// <summary>Blocks on the board; index is stable for the life of the board.</summary>
        public IReadOnlyList<Block> Blocks => _blocks;

        /// <summary>Index of the target block.</summary>
        public int TargetIndex { get; }

        /// <summary>
        /// Creates a board. Throws if there is not exactly one horizontal target on
        /// the exit row, or if blocks overlap or leave the board.
        /// </summary>
        public Board(IEnumerable<Block> blocks, int size = DefaultSize, int exitRow = DefaultExitRow)
        {
            Size = size;
            ExitRow = exitRow;
            _blocks = new List<Block>(blocks).ToArray();
            TargetIndex = -1;

            var occupied = new bool[size, size];
            for (int i = 0; i < _blocks.Length; i++)
            {
                Block b = _blocks[i];
                if (b.Length < 2)
                    throw new ArgumentException($"Block {i} is shorter than 2 cells.");
                if (b.IsTarget)
                {
                    if (TargetIndex >= 0)
                        throw new ArgumentException("Board has more than one target block.");
                    if (b.Orientation != Orientation.Horizontal || b.Row != exitRow)
                        throw new ArgumentException("Target block must be horizontal on the exit row.");
                    TargetIndex = i;
                }
                foreach ((int r, int c) in Cells(b))
                {
                    if (r < 0 || c < 0 || r >= size || c >= size)
                        throw new ArgumentException($"Block {i} is outside the board.");
                    if (occupied[r, c])
                        throw new ArgumentException($"Block {i} overlaps another block.");
                    occupied[r, c] = true;
                }
            }
            if (TargetIndex < 0)
                throw new ArgumentException("Board has no target block.");
        }

        private Board(Block[] blocks, int size, int exitRow, int targetIndex)
        {
            _blocks = blocks;
            Size = size;
            ExitRow = exitRow;
            TargetIndex = targetIndex;
        }

        /// <summary>True when the target block reaches the exit.</summary>
        public bool IsSolved => _blocks[TargetIndex].Col + _blocks[TargetIndex].Length == Size;

        /// <summary>
        /// Furthest the block can slide in each direction: (maxBackward, maxForward),
        /// both non-negative cell counts.
        /// </summary>
        public (int Backward, int Forward) GetMoveRange(int blockIndex)
        {
            bool[,] occupied = BuildOccupancy();
            Block b = _blocks[blockIndex];
            int back = 0, fwd = 0;
            if (b.Orientation == Orientation.Horizontal)
            {
                while (b.Col - back - 1 >= 0 && !occupied[b.Row, b.Col - back - 1]) back++;
                while (b.Col + b.Length + fwd < Size && !occupied[b.Row, b.Col + b.Length + fwd]) fwd++;
            }
            else
            {
                while (b.Row - back - 1 >= 0 && !occupied[b.Row - back - 1, b.Col]) back++;
                while (b.Row + b.Length + fwd < Size && !occupied[b.Row + b.Length + fwd, b.Col]) fwd++;
            }
            return (back, fwd);
        }

        /// <summary>True if the move stays on the board and does not pass through another block.</summary>
        public bool CanApply(Move move)
        {
            if (move.BlockIndex < 0 || move.BlockIndex >= _blocks.Length || move.Delta == 0)
                return false;
            (int back, int fwd) = GetMoveRange(move.BlockIndex);
            return move.Delta < 0 ? -move.Delta <= back : move.Delta <= fwd;
        }

        /// <summary>Returns a new board with the move applied. Throws if the move is illegal.</summary>
        public Board Apply(Move move)
        {
            if (!CanApply(move))
                throw new InvalidOperationException($"Illegal move: {move}");
            var next = (Block[])_blocks.Clone();
            Block b = next[move.BlockIndex];
            next[move.BlockIndex] = b.WithPosition(b.Position + move.Delta);
            return new Board(next, Size, ExitRow, TargetIndex);
        }

        /// <summary>Every legal move from this position (each block, each distance).</summary>
        public IEnumerable<Move> GetLegalMoves()
        {
            for (int i = 0; i < _blocks.Length; i++)
            {
                (int back, int fwd) = GetMoveRange(i);
                for (int d = 1; d <= back; d++) yield return new Move(i, -d);
                for (int d = 1; d <= fwd; d++) yield return new Move(i, d);
            }
        }

        /// <summary>Returns a board with each block at the given slide-axis position (solver use).</summary>
        internal Board WithPositions(int[] positions)
        {
            var next = new Block[_blocks.Length];
            for (int i = 0; i < next.Length; i++)
                next[i] = _blocks[i].WithPosition(positions[i]);
            return new Board(next, Size, ExitRow, TargetIndex);
        }

        private bool[,] BuildOccupancy()
        {
            var occupied = new bool[Size, Size];
            foreach (Block b in _blocks)
                foreach ((int r, int c) in Cells(b))
                    occupied[r, c] = true;
            return occupied;
        }

        private static IEnumerable<(int, int)> Cells(Block b)
        {
            for (int k = 0; k < b.Length; k++)
                yield return b.Orientation == Orientation.Horizontal ? (b.Row, b.Col + k) : (b.Row + k, b.Col);
        }
    }
}
