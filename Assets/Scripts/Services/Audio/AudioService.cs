using UnityEngine;

namespace PixelDefense.Services.Audio
{
    /// <summary>
    /// Pooled 2D sound playback. Callers resolve ids to handles once (<see cref="Resolve"/>) and play by handle,
    /// so per-frame playback never allocates or hashes strings.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int VoiceCount = 24;
        private const float MusicFadeSpeed = 1.5f;

        [SerializeField] private AudioLibrary _library;

        private AudioSource[] _voices;
        private int[] _voiceEntry;
        private float[] _lastPlayed;
        private AudioSource _music;
        private int _nextVoice;
        private bool _soundOn = true;
        private bool _musicOn = true;
        private float _musicTarget;

        public void Init(AudioLibrary library)
        {
            _library = library;
            EnsureVoices();
        }

        private void Awake()
        {
            EnsureVoices();
        }

        private void EnsureVoices()
        {
            if (_voices != null || _library == null)
            {
                return;
            }

            _voices = new AudioSource[VoiceCount];
            _voiceEntry = new int[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                _voices[i] = CreateSource("Voice" + i);
                _voiceEntry[i] = -1;
            }

            _lastPlayed = new float[_library.Entries.Length];
            for (int i = 0; i < _lastPlayed.Length; i++)
            {
                _lastPlayed[i] = -100f;
            }

            _music = CreateSource("Music");
            _music.loop = true;
            _music.clip = _library.Music;
            _music.volume = 0f;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Returns a handle for <paramref name="id"/>, or -1 (silently ignored by Play) if unknown.</summary>
        public int Resolve(string id)
        {
            if (_library == null)
            {
                return -1;
            }

            AudioLibrary.Entry[] entries = _library.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].Id, id, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            Debug.LogWarning("AudioService: unknown sound id '" + id + "'.");
            return -1;
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundOn = enabled;
            if (!enabled && _voices != null)
            {
                for (int i = 0; i < _voices.Length; i++)
                {
                    _voices[i].Stop();
                }
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicOn = enabled;
            RefreshMusic();
        }

        public void PlayMusic(bool play)
        {
            _musicTarget = play ? 1f : 0f;
            RefreshMusic();
        }

        private void RefreshMusic()
        {
            if (_music == null || _music.clip == null)
            {
                return;
            }

            if (_musicOn && _musicTarget > 0f && !_music.isPlaying)
            {
                _music.Play();
            }
        }

        /// <param name="pitch">Multiplier applied on top of the entry's random pitch range.</param>
        public void Play(int handle, float pitch = 1f, float volume = 1f)
        {
            if (!_soundOn || handle < 0 || _voices == null)
            {
                return;
            }

            AudioLibrary.Entry entry = _library.Entries[handle];
            if (entry.Clips.Length == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastPlayed[handle] < entry.Cooldown)
            {
                return;
            }

            int playing = 0;
            int oldestSame = -1;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voiceEntry[i] == handle && _voices[i].isPlaying)
                {
                    playing++;
                    if (oldestSame < 0)
                    {
                        oldestSame = i;
                    }
                }
            }

            int voice;
            if (playing >= entry.MaxVoices)
            {
                // Steal this sound's own voice so rapid streams (pops) keep sounding fresh instead of dropping.
                voice = oldestSame;
            }
            else
            {
                voice = FindFreeVoice();
            }

            AudioClip clip = entry.Clips.Length == 1 ? entry.Clips[0] : entry.Clips[Random.Range(0, entry.Clips.Length)];
            AudioSource source = _voices[voice];
            source.Stop();
            source.clip = clip;
            source.volume = entry.Volume * volume;
            source.pitch = Random.Range(entry.PitchRange.x, entry.PitchRange.y) * pitch;
            source.Play();
            _voiceEntry[voice] = handle;
            _lastPlayed[handle] = now;
        }

        private int FindFreeVoice()
        {
            for (int n = 0; n < _voices.Length; n++)
            {
                int i = (_nextVoice + n) % _voices.Length;
                if (!_voices[i].isPlaying)
                {
                    _nextVoice = (i + 1) % _voices.Length;
                    return i;
                }
            }

            int stolen = _nextVoice;
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return stolen;
        }

        private void Update()
        {
            if (_music == null || _music.clip == null)
            {
                return;
            }

            float target = _musicOn ? _musicTarget * _library.MusicVolume : 0f;
            _music.volume = Mathf.MoveTowards(_music.volume, target, MusicFadeSpeed * _library.MusicVolume * Time.unscaledDeltaTime);
            if (_music.volume <= 0f && target <= 0f && _music.isPlaying)
            {
                _music.Pause();
            }
            else if (target > 0f && !_music.isPlaying)
            {
                _music.UnPause();
                if (!_music.isPlaying)
                {
                    _music.Play();
                }
            }
        }
    }
}
