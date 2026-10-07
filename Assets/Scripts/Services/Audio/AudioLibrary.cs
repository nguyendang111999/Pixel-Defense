using System;
using UnityEngine;

namespace PixelDefense.Services.Audio
{
    /// <summary>Named sound entries with variation and voice limits. Ids are resolved to handles once at startup.</summary>
    [CreateAssetMenu(menuName = "Pixel Defense/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private AudioClip _music;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.35f;

        public Entry[] Entries => _entries;
        public AudioClip Music => _music;
        public float MusicVolume => _musicVolume;

        public void SetEntries(Entry[] entries, AudioClip music)
        {
            _entries = entries;
            _music = music;
        }

        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public AudioClip[] Clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float Volume = 0.8f;
            public Vector2 PitchRange = new Vector2(0.95f, 1.05f);

            [Tooltip("Simultaneous voices allowed for this sound; extra plays are dropped.")]
            public int MaxVoices = 4;

            [Tooltip("Minimum seconds between two plays of this sound.")]
            public float Cooldown = 0.02f;
        }
    }
}
