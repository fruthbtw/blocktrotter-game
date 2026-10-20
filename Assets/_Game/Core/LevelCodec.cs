using System;
using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>
    /// Text form of a 6x6 board: 36 characters, row-major from the top-left.
    /// '.' is empty, 'A' is the target block, any other letter is one block
    /// (each letter used once; its cells must form a straight line).
    /// </summary>
    /// <example>
    /// <code>
    /// Board b = LevelCodec.Decode("...BB.......AA.C.....C.....DD.......");
    /// string s = LevelCodec.Encode(b);
    /// </code>
    /// </example>
    public static class LevelCodec
    {
        /// <summary>Character for an empty cell.</summary>
        public const char Empty = '.';

        /// <summary>Character for the target block.</summary>
        public const char Target = 'A';

        /// <summary>Parses a level string. Throws <see cref="FormatException"/> on malformed input.</summary>
        public static Board Decode(string code, int size = Board.DefaultSize, int exitRow = Board.DefaultExitRow)
        {
            if (code == null || code.Length != size * size)
                throw new FormatException($"Level code must be {size * size} characters.");

            var cellsByLetter = new SortedDictionary<char, List<(int Row, int Col)>>();
            for (int i = 0; i < code.Length; i++)
            {
                char ch = code[i];
                if (ch == Empty) continue;
                if (!char.IsLetter(ch))
                    throw new FormatException($"Unexpected character '{ch}' at {i}.");
                if (!cellsByLetter.TryGetValue(ch, out var cells))
                    cellsByLetter[ch] = cells = new List<(int, int)>();
                cells.Add((i / size, i % size));
            }

            var blocks = new List<Block>();
            foreach (var pair in cellsByLetter)
            {
                List<(int Row, int Col)> cells = pair.Value;
                (int row, int col) = cells[0];   // row-major order => first cell is top-left
                bool horizontal = cells.TrueForAll(c => c.Row == row);
                bool vertical = cells.TrueForAll(c => c.Col == col);
                if (cells.Count < 2 || !(horizontal || vertical))
                    throw new FormatException($"Block '{pair.Key}' is not a straight line of 2+ cells.");
                for (int k = 0; k < cells.Count; k++)
                {
                    var expected = horizontal ? (row, col + k) : (row + k, col);
                    if (cells[k] != expected)
                        throw new FormatException($"Block '{pair.Key}' has a gap.");
                }
                blocks.Add(new Block(row, col, cells.Count,
                    horizontal ? Orientation.Horizontal : Orientation.Vertical, pair.Key == Target));
            }
            return new Board(blocks, size, exitRow);
        }

        /// <summary>Writes a board as a level string; the target is 'A', others 'B', 'C', ... in block order.</summary>
        public static string Encode(Board board)
        {
            var chars = new char[board.Size * board.Size];
            for (int i = 0; i < chars.Length; i++) chars[i] = Empty;

            char next = 'B';
            for (int i = 0; i < board.Blocks.Count; i++)
            {
                Block b = board.Blocks[i];
                char ch = b.IsTarget ? Target : next++;
                for (int k = 0; k < b.Length; k++)
                {
                    int r = b.Orientation == Orientation.Vertical ? b.Row + k : b.Row;
                    int c = b.Orientation == Orientation.Horizontal ? b.Col + k : b.Col;
                    chars[r * board.Size + c] = ch;
                }
            }
            return new string(chars);
        }
    }
}
