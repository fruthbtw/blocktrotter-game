using System;
using Blocktrotter.Audio;
using NUnit.Framework;
using UnityEditor;

namespace Blocktrotter.Tests
{
    public class SoundConfigTests
    {
        private const string ConfigPath = "Assets/_Game/Data/SoundConfig.asset";

        [Test]
        public void EverySound_HasAClipAndAudibleVolume()
        {
            var config = AssetDatabase.LoadAssetAtPath<SoundConfig>(ConfigPath);
            Assert.IsNotNull(config, "SoundConfig missing — run Blocktrotter/Build Game Scene.");

            foreach (Sfx sound in Enum.GetValues(typeof(Sfx)))
            {
                SoundConfig.Entry e = config.Get(sound);
                Assert.IsNotNull(e.Clip, $"{sound} has no clip");
                Assert.Greater(e.Volume, 0f, $"{sound} is silent");
            }
        }
    }
}
