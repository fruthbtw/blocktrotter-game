using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>
    /// Compact, allocation-light view of a board's positions for search. A state is
    /// every block's slide-axis position packed into a ulong (3 bits per block);
    /// occupancy is a bitmask with one bit per cell (row * size + col).
    /// </summary>
    internal sealed class StateSpace
    {
        private const int BitsPerBlock = 3;
        internal const int MaxBlocks = 64 / BitsPerBlock;

        private readonly Board _board;
        private readonly int _count;
        private readonly int _targetIndex;
        private readonly int _targetSolvedPosition;
        private readonly int[] _maxPosition;
        private readonly ulong[][] _masks;   // [block][position] -> cells covered
        private readonly int[] _scratch;

        internal StateSpace(Board board)
        {
            if (board.Blocks.Count > MaxBlocks)
                throw new System.ArgumentException($"Search supports at most {MaxBlocks} blocks.");
            if (board.Size * board.Size > 64)
                throw new System.ArgumentException("Search supports boards up to 8x8.");

            _board = board;
            _count = board.Blocks.Count;
            _targetIndex = board.TargetIndex;
            _targetSolvedPosition = board.Size - board.Blocks[_targetIndex].Length;
            _maxPosition = new int[_count];
            _masks = new ulong[_count][];
            _scratch = new int[_count];

            for (int i = 0; i < _count; i++)
            {
                Block b = board.Blocks[i];
                _maxPosition[i] = board.Size - b.Length;
                _masks[i] = new ulong[_maxPosition[i] + 1];
                for (int p = 0; p <= _maxPosition[i]; p++)
                {
                    ulong mask = 0;
                    for (int k = 0; k < b.Length; k++)
                    {
                        int r = b.Orientation == Orientation.Vertical ? p + k : b.Row;
                        int c = b.Orientation == Orientation.Horizontal ? p + k : b.Col;
                        mask |= 1UL << (r * board.Size + c);
                    }
                    _masks[i][p] = mask;
                }
            }
        }

        internal ulong StartKey
        {
            get
            {
                for (int i = 0; i < _count; i++) _scratch[i] = _board.Blocks[i].Position;
                return Pack(_scratch);
            }
        }

        internal bool IsSolved(ulong key) => PositionOf(key, _targetIndex) == _targetSolvedPosition;

        internal Board ToBoard(ulong key)
        {
            Unpack(key, _scratch);
            return _board.WithPositions(_scratch);
        }

        /// <summary>Clears <paramref name="output"/> and fills it with every (neighbor key, move) from <paramref name="key"/>.</summary>
        internal void GetNeighbors(ulong key, List<(ulong Key, Move Move)> output)
        {
            output.Clear();
            Unpack(key, _scratch);
            ulong occupied = 0;
            for (int i = 0; i < _count; i++) occupied |= _masks[i][_scratch[i]];

            for (int i = 0; i < _count; i++)
            {
                int pos = _scratch[i];
                ulong others = occupied & ~_masks[i][pos];
                int shift = i * BitsPerBlock;
                ulong cleared = key & ~(0b111UL << shift);

                for (int p = pos - 1; p >= 0 && (_masks[i][p] & others) == 0; p--)
                    output.Add((cleared | ((ulong)p << shift), new Move(i, p - pos)));
                for (int p = pos + 1; p <= _maxPosition[i] && (_masks[i][p] & others) == 0; p++)
                    output.Add((cleared | ((ulong)p << shift), new Move(i, p - pos)));
            }
        }

        private static int PositionOf(ulong key, int index) => (int)((key >> (index * BitsPerBlock)) & 0b111);

        private ulong Pack(int[] positions)
        {
            ulong key = 0;
            for (int i = 0; i < _count; i++)
                key |= (ulong)positions[i] << (i * BitsPerBlock);
            return key;
        }

        private void Unpack(ulong key, int[] positions)
        {
            for (int i = 0; i < _count; i++)
                positions[i] = PositionOf(key, i);
        }
    }
}
