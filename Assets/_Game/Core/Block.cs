namespace Blocktrotter.Core
{
    /// <summary>Axis a block is allowed to slide along.</summary>
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Immutable block on the board. (Row, Col) is the top-left cell; a horizontal
    /// block extends right, a vertical block extends down.
    /// </summary>
    public readonly struct Block
    {
        /// <summary>Row of the top-left cell (0 = top).</summary>
        public readonly int Row;

        /// <summary>Column of the top-left cell (0 = left).</summary>
        public readonly int Col;

        /// <summary>Number of cells the block covers (2 or 3).</summary>
        public readonly int Length;

        /// <summary>Axis the block slides along.</summary>
        public readonly Orientation Orientation;

        /// <summary>True for the block the player must get out through the exit.</summary>
        public readonly bool IsTarget;

        /// <summary>Creates a block.</summary>
        public Block(int row, int col, int length, Orientation orientation, bool isTarget = false)
        {
            Row = row;
            Col = col;
            Length = length;
            Orientation = orientation;
            IsTarget = isTarget;
        }

        /// <summary>Position along the sliding axis (Col for horizontal, Row for vertical).</summary>
        public int Position => Orientation == Orientation.Horizontal ? Col : Row;

        /// <summary>Returns a copy of this block moved to <paramref name="position"/> on its sliding axis.</summary>
        public Block WithPosition(int position)
        {
            return Orientation == Orientation.Horizontal
                ? new Block(Row, position, Length, Orientation, IsTarget)
                : new Block(position, Col, Length, Orientation, IsTarget);
        }
    }

    /// <summary>One player move: a block slid by Delta cells (negative = left/up).</summary>
    public readonly struct Move
    {
        /// <summary>Index of the moved block in <see cref="Board.Blocks"/>.</summary>
        public readonly int BlockIndex;

        /// <summary>Signed number of cells moved.</summary>
        public readonly int Delta;

        /// <summary>Creates a move.</summary>
        public Move(int blockIndex, int delta)
        {
            BlockIndex = blockIndex;
            Delta = delta;
        }

        /// <inheritdoc/>
        public override string ToString() => $"Block {BlockIndex} {(Delta > 0 ? "+" : "")}{Delta}";
    }
}
