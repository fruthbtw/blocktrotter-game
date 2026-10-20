using System.Collections.Generic;
using Blocktrotter.Core;
using Blocktrotter.Data;
using NUnit.Framework;
using UnityEditor;

namespace Blocktrotter.Tests
{
    /// <summary>Checks the shipped level set (Assets/_Game/Data/LevelDatabase.asset).</summary>
    public class LevelDatabaseTests
    {
        private const string DatabasePath = "Assets/_Game/Data/LevelDatabase.asset";
        private const int ExpectedLevelCount = 500;

        private LevelDatabase _database;

        [SetUp]
        public void LoadDatabase()
        {
            _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            Assert.IsNotNull(_database, "Level database missing — run Blocktrotter/Generate Levels.");
        }

        [Test]
        public void Database_HasExpectedLevelCount()
        {
            Assert.AreEqual(ExpectedLevelCount, _database.Count);
        }

        [Test]
        public void EveryLevel_SolutionReplaysToSolvedInStoredMoveCount()
        {
            for (int i = 0; i < _database.Count; i++)
            {
                Board board = _database.LoadBoard(i);
                SolveResult result = BoardSolver.Solve(board);

                Assert.AreEqual(_database.Get(i).MinMoves, result.MinMoves, $"Level {i + 1}");
                foreach (Move move in result.Moves)
                    board = board.Apply(move);
                Assert.IsTrue(board.IsSolved, $"Level {i + 1} replay did not solve");
            }
        }

        [Test]
        public void Levels_AreUniqueAndNeverGetEasier()
        {
            var codes = new HashSet<string>();
            for (int i = 0; i < _database.Count; i++)
            {
                Assert.IsTrue(codes.Add(_database.Get(i).Code), $"Level {i + 1} duplicates an earlier level");
                if (i > 0)
                    Assert.GreaterOrEqual(_database.Get(i).MinMoves, _database.Get(i - 1).MinMoves, $"Level {i + 1}");
            }
        }

        [Test]
        public void FirstLevel_IsNotAlreadySolved()
        {
            Assert.Greater(_database.Get(0).MinMoves, 0);
        }
    }
}
