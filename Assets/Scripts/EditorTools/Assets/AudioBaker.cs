using System;
using System.Collections.Generic;
using System.IO;
using PixelDefense.Services.Audio;
using UnityEditor;
using UnityEngine;

namespace PixelDefense.EditorTools
{
    /// <summary>Renders the synthesized sound set to WAVs and builds the AudioLibrary that maps ids to clips.</summary>
    internal static class AudioBaker
    {
        private const string MusicPath = AssetPaths.Music + "/Loop.wav";

        private struct Recipe
        {
            public string Id;
            public Func<SfxSynth, float[]> Render;
            public float Volume;
            public Vector2 Pitch;
            public int Voices;
            public float Cooldown;
            public int Variations;
        }

        private static Recipe[] Recipes()
        {
            return new[]
            {
                R("pop", SfxDesigns.Pop, 0.5f, 0.97f, 1.03f, 6, 0.022f, 3),
                R("shoot", SfxDesigns.Shoot, 0.3f, 0.92f, 1.08f, 5, 0.02f, 2),
                R("tap", SfxDesigns.Tap, 0.6f, 0.95f, 1.05f, 2, 0.03f),
                R("ui", SfxDesigns.Ui, 0.55f, 0.97f, 1.03f, 2, 0.03f),
                R("jump", SfxDesigns.Jump, 0.45f, 0.95f, 1.1f, 3, 0.03f),
                R("land", SfxDesigns.Land, 0.6f, 0.9f, 1.05f, 3, 0.03f),
                R("crunch", SfxDesigns.Crunch, 0.32f, 0.9f, 1.1f, 3, 0.05f, 2),
                R("empty", SfxDesigns.Empty, 0.42f, 0.98f, 1.04f, 3, 0.05f),
                R("poof", SfxDesigns.Poof, 0.45f, 0.95f, 1.05f, 3, 0.05f),
                R("deny", SfxDesigns.Deny, 0.6f, 1f, 1f, 1, 0.15f),
                R("reveal", SfxDesigns.Reveal, 0.5f, 1f, 1.05f, 2, 0.05f),
                R("roar", s => SfxDesigns.Roar(s, 1.3f, 1f), 0.85f, 0.95f, 1.05f, 1, 0.3f),
                R("hurt", s => SfxDesigns.Roar(s, 0.45f, 1.45f), 0.55f, 0.95f, 1.1f, 1, 0.2f, 2),
                R("warning", SfxDesigns.Warning, 0.42f, 1f, 1f, 1, 0.2f),
                R("heartbeat", SfxDesigns.Heartbeat, 0.7f, 1f, 1f, 1, 0.2f),
                R("chime", SfxDesigns.Chime, 0.5f, 1f, 1f, 2, 0.1f),
                R("explosion", s => SfxDesigns.Explosion(s, 1.1f), 0.85f, 0.95f, 1.05f, 2, 0.1f),
                R("win", SfxDesigns.Win, 0.62f, 1f, 1f, 1, 0.5f),
                R("lose", SfxDesigns.Lose, 0.6f, 1f, 1f, 1, 0.5f),
                R("freeze", SfxDesigns.Freeze, 0.6f, 1f, 1f, 1, 0.2f),
                R("bomb", SfxDesigns.Bomb, 0.8f, 1f, 1f, 1, 0.2f),
                R("slot", SfxDesigns.Slot, 0.6f, 1f, 1f, 1, 0.2f),
                R("whoosh", SfxDesigns.Whoosh, 0.5f, 1f, 1f, 1, 0.3f),
                R("coin", SfxDesigns.Coin, 0.55f, 1f, 1.05f, 2, 0.05f),
            };
        }

        private static Recipe R(string id, Func<SfxSynth, float[]> render, float volume, float minPitch, float maxPitch, int voices,
            float cooldown, int variations = 1)
        {
            return new Recipe
            {
                Id = id,
                Render = render,
                Volume = volume,
                Pitch = new Vector2(minPitch, maxPitch),
                Voices = voices,
                Cooldown = cooldown,
                Variations = variations
            };
        }

        public static AudioLibrary BakeAll()
        {
            Directory.CreateDirectory(AssetPaths.Sfx);
            Directory.CreateDirectory(AssetPaths.Music);
            Recipe[] recipes = Recipes();
            var paths = new List<string>[recipes.Length];
            for (int i = 0; i < recipes.Length; i++)
            {
                paths[i] = new List<string>();
                for (int v = 0; v < recipes[i].Variations; v++)
                {
                    // Each variation uses a different seed so repeated sounds don't phase-match.
                    var synth = new SfxSynth(1000 + i * 31 + v * 7);
                    float[] samples = recipes[i].Render(synth);
                    string path = AssetPaths.Sfx + "/" + recipes[i].Id + (recipes[i].Variations > 1 ? "_" + (v + 1) : string.Empty) + ".wav";
                    SfxSynth.WriteWav(path, samples);
                    paths[i].Add(path);
                }
            }

            SfxSynth.WriteWav(MusicPath, SfxDesigns.Music(new SfxSynth(42)));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var entries = new AudioLibrary.Entry[recipes.Length];
            for (int i = 0; i < recipes.Length; i++)
            {
                var clips = new AudioClip[paths[i].Count];
                for (int c = 0; c < clips.Length; c++)
                {
                    ConfigureSfx(paths[i][c]);
                    clips[c] = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i][c]);
                }

                entries[i] = new AudioLibrary.Entry
                {
                    Id = recipes[i].Id,
                    Clips = clips,
                    Volume = recipes[i].Volume,
                    PitchRange = recipes[i].Pitch,
                    MaxVoices = recipes[i].Voices,
                    Cooldown = recipes[i].Cooldown
                };
            }

            ConfigureMusic(MusicPath);
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetPaths.AudioLibrary);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, AssetPaths.AudioLibrary);
            }

            library.SetEntries(entries, AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath));
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void ConfigureSfx(string path)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            importer.loadInBackground = false;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void ConfigureMusic(string path)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.55f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
