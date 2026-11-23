using System;
using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>One guided step: which block to move, the text shown, and when the step counts as done.</summary>
    public readonly struct TutorialStep
    {
        /// <summary>Index of the only block the player may drag during this step.</summary>
        public readonly int BlockIndex;

        /// <summary>Localization key of the instruction.</summary>
        public readonly string TextKey;

        /// <summary>Cells the pointer animation suggests (sign = direction along the block).</summary>
        public readonly int SuggestedDelta;

        /// <summary>True once the board satisfies this step.</summary>
        public readonly Func<Board, bool> IsDone;

        /// <summary>Creates a step.</summary>
        public TutorialStep(int blockIndex, string textKey, int suggestedDelta, Func<Board, bool> isDone)
        {
            BlockIndex = blockIndex;
            TextKey = textKey;
            SuggestedDelta = suggestedDelta;
            IsDone = isDone;
        }
    }

    /// <summary>
    /// The guided first puzzle. Teaches, in four moves: vertical blocks slide up/down,
    /// horizontal blocks slide left/right, sometimes a blocker must be freed by moving
    /// another block first, and the red block leaves through the gate.
    /// <code>
    /// . . . . E .     B: slide up (clears the red block's row)
    /// . . B . E .     D: slide left (frees the cell under C)
    /// A A B . C .     C: slide down (E stops it escaping upward instead)
    /// . . . . C .     A: drive out
    /// . . . . D D
    /// . . . . . .
    /// </code>
    /// Steps judge the board, not the exact move, so any drag that achieves the step counts.
    /// </summary>
    /// <example><code>Board b = TutorialScript.CreateBoard(); TutorialStep first = TutorialScript.Steps[0];</code></example>
    public static class TutorialScript
    {
        /// <summary>The tutorial board as a level string.</summary>
        public const string BoardCode =
            "....E." +
            "..B.E." +
            "AAB.C." +
            "....C." +
            "....DD" +
            "......";

        // Block indices follow LevelCodec's letter order: A=0, B=1, C=2, D=3, E=4.
        private const int Red = 0, FirstVertical = 1, SecondVertical = 2, Horizontal = 3;

        /// <summary>The steps, in order.</summary>
        public static readonly IReadOnlyList<TutorialStep> Steps = new[]
        {
            new TutorialStep(FirstVertical, "tutorial.step1", -1, b => !CoversRow(b, FirstVertical, b.ExitRow)),
            new TutorialStep(Horizontal, "tutorial.step2", -2, b => !CoversCell(b, Horizontal, row: 4, col: 4)),
            new TutorialStep(SecondVertical, "tutorial.step3", 2, b => !CoversRow(b, SecondVertical, b.ExitRow)),
            new TutorialStep(Red, "tutorial.step4", 4, b => b.IsSolved),
        };

        /// <summary>A fresh tutorial board.</summary>
        public static Board CreateBoard() => LevelCodec.Decode(BoardCode);

        private static bool CoversRow(Board board, int index, int row)
        {
            Block b = board.Blocks[index];
            return b.Orientation == Orientation.Vertical ? b.Row <= row && row < b.Row + b.Length : b.Row == row;
        }

        private static bool CoversCell(Board board, int index, int row, int col)
        {
            Block b = board.Blocks[index];
            return b.Orientation == Orientation.Horizontal
                ? b.Row == row && b.Col <= col && col < b.Col + b.Length
                : b.Col == col && b.Row <= row && row < b.Row + b.Length;
        }
    }
}
