using System.Collections.Generic;
using System.Linq;
using Blocktrotter.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blocktrotter.Tests
{
    public class LocalizerTests
    {
        private const string EnglishPath = "Assets/_Game/Data/Localization/en.txt";
        private const string TurkishPath = "Assets/_Game/Data/Localization/tr.txt";

        [Test]
        public void Parse_ReadsKeysSkipsCommentsAndUnescapesNewlines()
        {
            Dictionary<string, string> table = Localizer.Parse("# comment\r\na.b=Hello {0}\r\nc=Line1\\nLine2\r\n\r\n");

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Hello {0}", table["a.b"]);
            Assert.AreEqual("Line1\nLine2", table["c"]);
        }

        [TestCase(EnglishPath)]
        [TestCase(TurkishPath)]
        public void StringTable_EveryLineIsCommentBlankOrKeyValue(string path)
        {
            // Regression: a literal newline inside a value split it into an orphan line that Parse silently dropped.
            string[] lines = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                Assert.Greater(line.IndexOf('='), 0, $"{path} line {i + 1} is not key=value: \"{line}\"");
            }
        }

        [Test]
        public void EnglishAndTurkish_HaveTheSameKeys()
        {
            var en = Localizer.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(EnglishPath).text);
            var tr = Localizer.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(TurkishPath).text);

            CollectionAssert.AreEquivalent(en.Keys.ToList(), tr.Keys.ToList());
        }

        [Test]
        public void Get_FormatsArgumentsAndFallsBackToKey()
        {
            var loc = new Localizer(
                AssetDatabase.LoadAssetAtPath<TextAsset>(EnglishPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(TurkishPath));

            loc.SetLanguage(Localizer.Turkish);

            Assert.AreEqual("Bölüm 7", loc.Get("game.level", 7));
            Assert.AreEqual("missing.key", loc.Get("missing.key"));
        }
    }
}
