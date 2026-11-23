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
        private const string Folder = "Assets/_Game/Data/Localization/";

        private static IEnumerable<string> AllCodes => Localizer.Supported;

        private static TextAsset Table(string code) => AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + code + ".txt");

        private static Localizer LoadAll() => new Localizer(Localizer.Supported.Select(Table));

        [Test]
        public void Parse_ReadsKeysSkipsCommentsAndUnescapesNewlines()
        {
            Dictionary<string, string> table = Localizer.Parse("# comment\r\na.b=Hello {0}\r\nc=Line1\\nLine2\r\n\r\n");

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Hello {0}", table["a.b"]);
            Assert.AreEqual("Line1\nLine2", table["c"]);
        }

        [TestCaseSource(nameof(AllCodes))]
        public void StringTable_EveryLineIsCommentBlankOrKeyValue(string code)
        {
            // Regression: a literal newline inside a value split it into an orphan line that Parse silently dropped.
            Assert.IsNotNull(Table(code), $"{code}.txt is missing");
            string[] lines = Table(code).text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                Assert.Greater(line.IndexOf('='), 0, $"{code}.txt line {i + 1} is not key=value: \"{line}\"");
            }
        }

        [TestCaseSource(nameof(AllCodes))]
        public void StringTable_HasExactlyTheEnglishKeys(string code)
        {
            var en = Localizer.Parse(Table(Localizer.English).text);
            var other = Localizer.Parse(Table(code).text);

            CollectionAssert.AreEquivalent(en.Keys.ToList(), other.Keys.ToList(), $"{code}.txt keys differ from en.txt");
        }

        [TestCaseSource(nameof(AllCodes))]
        public void StringTable_KeepsTheEnglishPlaceholders(string code)
        {
            // A translation that drops or renumbers {0}/{1} shows wrong numbers or throws in string.Format.
            var en = Localizer.Parse(Table(Localizer.English).text);
            var other = Localizer.Parse(Table(code).text);

            foreach (var pair in en)
                for (int i = 0; i < 4; i++)
                {
                    string slot = "{" + i + "}";
                    Assert.AreEqual(pair.Value.Contains(slot), other[pair.Key].Contains(slot), $"{code}.txt {pair.Key}: {slot}");
                }
        }

        [Test]
        public void Get_FormatsArgumentsAndFallsBackToKey()
        {
            var loc = LoadAll();

            loc.SetLanguage(Localizer.Turkish);

            Assert.AreEqual("Bölüm 7", loc.Get("game.level", 7));
            Assert.AreEqual("missing.key", loc.Get("missing.key"));
        }

        [Test]
        public void Languages_ListsEveryTableInPickerOrder()
        {
            CollectionAssert.AreEqual(Localizer.Supported, LoadAll().Languages);
        }

        [Test]
        public void SetLanguage_UnknownCodeFallsBackToEnglish()
        {
            var loc = LoadAll();

            loc.SetLanguage("xx");

            Assert.AreEqual(Localizer.English, loc.Language);
        }

        [TestCase(SystemLanguage.Turkish, "tr")]
        [TestCase(SystemLanguage.Spanish, "es")]
        [TestCase(SystemLanguage.Portuguese, "pt")]
        [TestCase(SystemLanguage.Indonesian, "id")]
        [TestCase(SystemLanguage.Vietnamese, "vi")]
        [TestCase(SystemLanguage.Thai, "th")]
        [TestCase(SystemLanguage.German, "en")]
        [TestCase(SystemLanguage.English, "en")]
        public void DetectLanguage_MapsDeviceLanguage(SystemLanguage system, string expected)
        {
            Assert.AreEqual(expected, Localizer.DetectLanguage(system));
        }
    }
}
