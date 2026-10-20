using UnityEngine;

namespace Blocktrotter.Save
{
    /// <summary>Loads and saves <see cref="PlayerProgress"/>. Swap implementations in tests.</summary>
    public interface IProgressStore
    {
        /// <summary>Saved progress, or null if none (or unreadable).</summary>
        PlayerProgress Load();

        /// <summary>Persists progress.</summary>
        void Save(PlayerProgress progress);
    }

    /// <summary>Stores progress as JSON in PlayerPrefs (on-device only).</summary>
    /// <example><code>IProgressStore store = new PlayerPrefsProgressStore(); var p = store.Load() ?? PlayerProgress.Create(500, 3);</code></example>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private const string Key = "blocktrotter.progress";

        /// <inheritdoc/>
        public PlayerProgress Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<PlayerProgress>(json);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"[Save] Unreadable progress, starting fresh: {e.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public void Save(PlayerProgress progress)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }
    }
}
