using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blocktrotter.Localization
{
    /// <summary>
    /// String tables loaded from "key=value" text files (one per language).
    /// Lines starting with '#' are comments; "\n" in a value becomes a newline.
    /// A missing key returns the key itself so gaps are visible on screen.
    /// </summary>
    /// <example>
    /// <code>
    /// var loc = new Localizer(enTable, trTable);
    /// loc.SetLanguage(Localizer.DetectLanguage());
    /// label.text = loc.Get("game.level", 12);   // "Level 12" / "Bölüm 12"
    /// </code>
    /// </example>
    public sealed class Localizer
    {
        /// <summary>English language code.</summary>
        public const string English = "en";

        /// <summary>Turkish language code.</summary>
        public const string Turkish = "tr";

        private readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> _current;

        /// <summary>Active language code.</summary>
        public string Language { get; private set; }

        /// <summary>Raised after the language changes, so screens can refresh text.</summary>
        public event Action OnLanguageChanged;

        /// <summary>Creates a localizer from English and Turkish tables; English is active.</summary>
        public Localizer(TextAsset english, TextAsset turkish)
        {
            _tables[English] = Parse(english.text);
            _tables[Turkish] = Parse(turkish.text);
            SetLanguage(English);
        }

        /// <summary>"tr" on a Turkish device, otherwise "en".</summary>
        public static string DetectLanguage() =>
            Application.systemLanguage == SystemLanguage.Turkish ? Turkish : English;

        /// <summary>Switches language; unknown codes fall back to English.</summary>
        public void SetLanguage(string code)
        {
            Language = _tables.ContainsKey(code) ? code : English;
            _current = _tables[Language];
            OnLanguageChanged?.Invoke();
        }

        /// <summary>Localized string for <paramref name="key"/>, formatted with <paramref name="args"/> ({0}, {1}...).</summary>
        public string Get(string key, params object[] args)
        {
            if (!_current.TryGetValue(key, out string value) && !_tables[English].TryGetValue(key, out value))
                return key;
            return args.Length == 0 ? value : string.Format(value, args);
        }

        /// <summary>Parses "key=value" lines. Public for tests.</summary>
        public static Dictionary<string, string> Parse(string text)
        {
            var table = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                table[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
            }
            return table;
        }
    }
}
