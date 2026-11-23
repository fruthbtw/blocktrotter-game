using System;
using System.Collections.Generic;

namespace Blocktrotter.Core
{
    /// <summary>
    /// How much thinking a puzzle takes, beyond its move count.
    /// <list type="bullet">
    /// <item><see cref="Detours"/>: moves needed beyond the obvious plan "move each block in the
    /// target's path once, then drive out". A board with N blocks in the way needs at least
    /// N + 1 moves; every move above that is forced by a blocker-of-a-blocker. Exact, and
    /// independent of which optimal solution the solver happened to find.</item>
    /// <item><see cref="Reversals"/>: moves in the solver's optimal solution that slide a block
    /// opposite to its own previous move (moved out of the way, then back) — the
    /// counter-intuitive kind. Depends on the solution found, so it only nudges the score.</item>
    /// <item><see cref="StatesExplored"/>: positions the breadth-first search visited before
    /// finding the exit — many tempting but useless lines push this up.</item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// PuzzleDifficulty d = PuzzleDifficulty.Analyze(board);
    /// if (d.Detours == 0) { /* "just clear the row" puzzle — skip it */ }
    /// float score = d.Score(detourWeight: 3, reversalWeight: 2, effortWeight: 1);
    /// </code>
    /// </example>
    public readonly struct PuzzleDifficulty
    {
        /// <summary>Minimum number of moves (-1 if unsolvable).</summary>
        public readonly int MinMoves;

        /// <summary>Blocks standing between the target and the exit at the start.</summary>
        public readonly int DirectBlockers;

        /// <summary>MinMoves − (DirectBlockers + 1): moves forced by blocks that are not directly in the way.</summary>
        public readonly int Detours;

        /// <summary>Moves in the optimal solution that reverse a block's previous direction.</summary>
        public readonly int Reversals;

        /// <summary>Positions visited by the solver before it found the exit.</summary>
        public readonly int StatesExplored;

        private PuzzleDifficulty(int minMoves, int directBlockers, int reversals, int statesExplored)
        {
            MinMoves = minMoves;
            DirectBlockers = directBlockers;
            Detours = minMoves < 0 ? 0 : Math.Max(0, minMoves - (directBlockers + 1));
            Reversals = reversals;
            StatesExplored = statesExplored;
        }

        /// <summary>Weighted sum used to rank puzzles with the same move count (weights come from generation settings).</summary>
        public float Score(float detourWeight, float reversalWeight, float effortWeight) =>
            detourWeight * Detours + reversalWeight * Reversals + effortWeight * (float)Math.Log(StatesExplored + 1, 2);

        /// <summary>Solves the board and measures it.</summary>
        public static PuzzleDifficulty Analyze(Board board)
        {
            SolveResult solved = BoardSolver.Solve(board);
            return new PuzzleDifficulty(solved.MinMoves, CountDirectBlockers(board),
                CountReversals(solved.Moves), solved.StatesExplored);
        }

        /// <summary>Distinct blocks occupying the exit row between the target and the exit.</summary>
        public static int CountDirectBlockers(Board board)
        {
            Block target = board.Blocks[board.TargetIndex];
            var found = new HashSet<int>();
            for (int col = target.Col + target.Length; col < board.Size; col++)
            {
                for (int i = 0; i < board.Blocks.Count; i++)
                {
                    if (i == board.TargetIndex) continue;
                    Block b = board.Blocks[i];
                    bool covers = b.Orientation == Orientation.Vertical
                        ? b.Col == col && b.Row <= board.ExitRow && board.ExitRow < b.Row + b.Length
                        : b.Row == board.ExitRow && b.Col <= col && col < b.Col + b.Length;
                    if (covers) found.Add(i);
                }
            }
            return found.Count;
        }

        private static int CountReversals(IReadOnlyList<Move> moves)
        {
            var lastDirection = new Dictionary<int, int>();
            int reversals = 0;
            foreach (Move m in moves)
            {
                int dir = Math.Sign(m.Delta);
                if (lastDirection.TryGetValue(m.BlockIndex, out int previous) && previous != dir)
                    reversals++;
                lastDirection[m.BlockIndex] = dir;
            }
            return reversals;
        }
    }
}
