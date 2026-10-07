using System;
using UnityEngine;

namespace PixelDefense.Services.Settings
{
    /// <summary>Player toggles (sound, music, haptics) persisted in PlayerPrefs.</summary>
    public sealed class SettingsStore
    {
        private const string SoundKey = "settings.sound";
        private const string MusicKey = "settings.music";
        private const string HapticsKey = "settings.haptics";

        public event Action Changed;

        public bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => Set(SoundKey, value);
        }

        public bool Music
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => Set(MusicKey, value);
        }

        public bool Haptics
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set => Set(HapticsKey, value);
        }

        private void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
