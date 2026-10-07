using System;

namespace PixelDefense.EditorTools
{
    /// <summary>Recipes for every placeholder sound. Each returns mono samples normalized to a peak.</summary>
    public static class SfxDesigns
    {
        private static float Exp(float x)
        {
            return (float)Math.Exp(x);
        }

        private static float Sin(float x)
        {
            return (float)Math.Sin(x);
        }

        public static float[] Pop(SfxSynth s)
        {
            float[] b = s.Buffer(0.14f);
            s.Voice(b, 0f, t => 640f * (1f + 1.6f * Exp(-t / 0.011f)), t => SfxSynth.Decay(t, 0.002f, 0.042f), SfxSynth.Sine, 0.14f);
            s.Voice(b, 0f, t => 1290f * (1f + 1.1f * Exp(-t / 0.009f)), t => 0.25f * SfxSynth.Decay(t, 0.002f, 0.024f), SfxSynth.Sine, 0.1f);
            s.NoiseVoice(b, 0f, t => 7000f, t => 0.3f * SfxSynth.Decay(t, 0.0005f, 0.004f), 0.02f, 1800f);
            SfxSynth.Finish(b, 0.9f);
            return b;
        }

        public static float[] Shoot(SfxSynth s)
        {
            float[] b = s.Buffer(0.12f);
            s.Voice(b, 0f, t => 250f * (1f + 2f * Exp(-t / 0.01f)), t => SfxSynth.Decay(t, 0.001f, 0.035f), SfxSynth.Sine, 0.12f);
            s.NoiseVoice(b, 0f, t => 2400f * Exp(-t / 0.03f) + 300f, t => 0.55f * SfxSynth.Decay(t, 0.001f, 0.02f), 0.09f);
            SfxSynth.Finish(b, 0.75f);
            return b;
        }

        public static float[] Tap(SfxSynth s)
        {
            float[] b = s.Buffer(0.07f);
            s.Voice(b, 0f, t => 1100f * (1f + 0.5f * Exp(-t / 0.005f)), t => SfxSynth.Decay(t, 0.001f, 0.013f), SfxSynth.Sine, 0.07f);
            s.NoiseVoice(b, 0f, t => 9000f, t => 0.3f * SfxSynth.Decay(t, 0.0005f, 0.003f), 0.02f, 3000f);
            SfxSynth.Finish(b, 0.7f);
            return b;
        }

        public static float[] Ui(SfxSynth s)
        {
            float[] b = s.Buffer(0.09f);
            s.Voice(b, 0f, t => 520f + 180f * Exp(-t / 0.01f), t => SfxSynth.Decay(t, 0.002f, 0.03f), SfxSynth.Triangle, 0.09f);
            s.Voice(b, 0f, t => 1040f, t => 0.25f * SfxSynth.Decay(t, 0.002f, 0.02f), SfxSynth.Sine, 0.07f);
            SfxSynth.Finish(b, 0.65f);
            return b;
        }

        public static float[] Jump(SfxSynth s)
        {
            float[] b = s.Buffer(0.26f);
            s.Voice(b, 0f, t => (320f + 950f * (float)Math.Pow(Math.Min(1f, t / 0.2f), 1.6)) * (1f + 0.03f * Sin(t * 190f)),
                t => SfxSynth.Ahr(t, 0.012f, 0.09f, 0.13f), w => 0.75f * SfxSynth.Sine(w) + 0.25f * SfxSynth.Triangle(w), 0.26f);
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Land(SfxSynth s)
        {
            float[] b = s.Buffer(0.2f);
            s.Voice(b, 0f, t => 150f * Exp(-t / 0.05f) + 55f, t => SfxSynth.Decay(t, 0.002f, 0.07f), SfxSynth.Sine, 0.2f);
            s.NoiseVoice(b, 0f, t => 900f * Exp(-t / 0.04f) + 150f, t => 0.5f * SfxSynth.Decay(t, 0.001f, 0.03f), 0.12f);
            SfxSynth.Finish(b, 0.8f);
            return b;
        }

        public static float[] Crunch(SfxSynth s)
        {
            float[] b = s.Buffer(0.15f);
            s.NoiseVoice(b, 0f, t => 3800f, t => SfxSynth.Decay(t, 0.001f, 0.04f), 0.15f, 450f);
            s.Voice(b, 0f, t => 125f * Exp(-t / 0.04f) + 60f, t => 0.8f * SfxSynth.Decay(t, 0.002f, 0.05f), SfxSynth.Sine, 0.15f);
            SfxSynth.SoftClip(b, 2.2f);
            SfxSynth.Finish(b, 0.75f);
            return b;
        }

        public static float[] Empty(SfxSynth s)
        {
            float[] b = s.Buffer(0.6f);
            Bell(s, b, 0f, 1318.5f, 0.17f, 1f);
            Bell(s, b, 0.07f, 1975.5f, 0.2f, 0.8f);
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Poof(SfxSynth s)
        {
            float[] b = s.Buffer(0.32f);
            s.NoiseVoice(b, 0f, t => 4200f * Exp(-t / 0.08f) + 300f, t => SfxSynth.Ahr(t, 0.015f, 0.04f, 0.22f), 0.32f, 200f);
            SfxSynth.Finish(b, 0.55f);
            return b;
        }

        public static float[] Deny(SfxSynth s)
        {
            float[] b = s.Buffer(0.26f);
            for (int k = 0; k < 2; k++)
            {
                s.Voice(b, k * 0.12f, t => 150f, t => SfxSynth.Ahr(t, 0.004f, 0.06f, 0.025f), w => 0.6f * SfxSynth.Pulse(w, 0.5f) + 0.4f * SfxSynth.Saw(w), 0.1f);
            }
            SfxSynth.LowPass(b, 2400f);
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Reveal(SfxSynth s)
        {
            float[] b = s.Buffer(0.35f);
            float[] notes = { 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < notes.Length; i++)
            {
                float f = notes[i];
                s.Voice(b, i * 0.05f, t => f, t => SfxSynth.Decay(t, 0.002f, 0.07f), w => SfxSynth.Sine(w) + 0.3f * SfxSynth.Triangle(w), 0.2f);
            }
            SfxSynth.Finish(b, 0.55f);
            return b;
        }

        public static float[] Roar(SfxSynth s, float duration, float pitch)
        {
            float[] b = s.Buffer(duration);
            float attack = duration * 0.1f;
            float hold = duration * 0.42f;
            float release = duration * 0.45f;
            float jitter = 0f;
            s.Voice(b, 0f, t =>
            {
                jitter += (s.Noise() * 12f - jitter) * 0.02f;
                return pitch * (85f + 22f * Sin(t * 44f) + 12f * Sin(t * 145f) - 25f * t / duration) + jitter;
            }, t => SfxSynth.Ahr(t, attack, hold, release) * (0.7f + 0.3f * Sin(t * 176f)), SfxSynth.Saw, duration);
            s.NoiseVoice(b, 0f, t => (900f + 500f * Sin(t * 19f)) * pitch, t => 0.9f * SfxSynth.Ahr(t, attack * 0.8f, hold, release), duration, 150f);
            s.Voice(b, 0f, t => 52f * pitch, t => 0.5f * SfxSynth.Ahr(t, attack, hold, release), SfxSynth.Sine, duration);
            SfxSynth.LowPass(b, 2300f);
            SfxSynth.SoftClip(b, 2.6f);
            SfxSynth.Finish(b, 0.95f);
            return b;
        }

        public static float[] Warning(SfxSynth s)
        {
            float[] b = s.Buffer(0.36f);
            s.Voice(b, 0f, t => 880f, t => SfxSynth.Ahr(t, 0.005f, 0.12f, 0.03f), w => 0.5f * SfxSynth.Pulse(w, 0.5f) + 0.5f * SfxSynth.Triangle(w), 0.16f);
            s.Voice(b, 0.17f, t => 660f, t => SfxSynth.Ahr(t, 0.005f, 0.12f, 0.03f), w => 0.5f * SfxSynth.Pulse(w, 0.5f) + 0.5f * SfxSynth.Triangle(w), 0.16f);
            SfxSynth.LowPass(b, 4200f);
            SfxSynth.Finish(b, 0.55f);
            return b;
        }

        public static float[] Heartbeat(SfxSynth s)
        {
            float[] b = s.Buffer(0.45f);
            s.Voice(b, 0f, t => 58f + 30f * Exp(-t / 0.02f), t => SfxSynth.Decay(t, 0.004f, 0.06f), SfxSynth.Sine, 0.25f);
            s.Voice(b, 0.17f, t => 52f + 25f * Exp(-t / 0.02f), t => 0.8f * SfxSynth.Decay(t, 0.004f, 0.07f), SfxSynth.Sine, 0.25f);
            SfxSynth.LowPass(b, 420f);
            SfxSynth.Finish(b, 0.9f);
            return b;
        }

        public static float[] Chime(SfxSynth s)
        {
            float[] b = s.Buffer(0.9f);
            float[] notes = { 1046.5f, 1318.5f, 1568f, 2093f };
            for (int i = 0; i < notes.Length; i++)
            {
                Bell(s, b, i * 0.06f, notes[i], 0.25f, 0.85f);
            }
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Explosion(SfxSynth s, float duration)
        {
            float[] b = s.Buffer(duration);
            s.NoiseVoice(b, 0f, t => 6500f * Exp(-t / 0.12f) + 150f, t => SfxSynth.Decay(t, 0.002f, duration * 0.32f), duration);
            s.Voice(b, 0f, t => 72f * Exp(-t / 0.2f) + 34f, t => SfxSynth.Decay(t, 0.003f, duration * 0.28f), SfxSynth.Sine, duration);
            SfxSynth.SoftClip(b, 2f);
            SfxSynth.Finish(b, 0.95f);
            return b;
        }

        public static float[] Win(SfxSynth s)
        {
            float[] b = s.Buffer(1.7f);
            float[] run = { 523.3f, 659.3f, 784f, 1046.5f };
            for (int i = 0; i < run.Length; i++)
            {
                float f = run[i];
                s.Voice(b, i * 0.11f, t => f, t => 0.5f * SfxSynth.Ahr(t, 0.005f, 0.07f, 0.05f), w => SfxSynth.Pulse(w, 0.25f), 0.13f);
            }

            float[] chord = { 1046.5f, 1318.5f, 1568f };
            for (int i = 0; i < chord.Length; i++)
            {
                float f = chord[i];
                s.Voice(b, 0.46f, t => f * (1f + 0.006f * Sin(t * 35f)), t => 0.35f * SfxSynth.Ahr(t, 0.01f, 0.5f, 0.6f),
                    w => 0.6f * SfxSynth.Triangle(w) + 0.4f * SfxSynth.Pulse(w, 0.25f), 1.2f);
            }
            s.Voice(b, 0.46f, t => 261.6f, t => 0.5f * SfxSynth.Ahr(t, 0.01f, 0.5f, 0.5f), SfxSynth.Triangle, 1.1f);
            SfxSynth.LowPass(b, 6000f);
            SfxSynth.Finish(b, 0.7f);
            return b;
        }

        public static float[] Lose(SfxSynth s)
        {
            float[] b = s.Buffer(1.25f);
            float[] notes = { 392f, 329.6f, 261.6f };
            for (int i = 0; i < notes.Length; i++)
            {
                float f = notes[i];
                bool last = i == notes.Length - 1;
                s.Voice(b, i * 0.25f, t => last ? f * (1f - 0.09f * t) : f,
                    t => last ? SfxSynth.Ahr(t, 0.01f, 0.3f, 0.4f) : SfxSynth.Ahr(t, 0.01f, 0.15f, 0.08f),
                    w => SfxSynth.Triangle(w) + 0.3f * SfxSynth.Pulse(w, 0.5f), last ? 0.75f : 0.25f);
            }
            SfxSynth.LowPass(b, 3000f);
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Freeze(SfxSynth s)
        {
            float[] b = s.Buffer(0.95f);
            for (int i = 0; i < 16; i++)
            {
                float f = s.Range(2200f, 5200f);
                s.Voice(b, s.Range(0f, 0.55f), t => f, t => 0.35f * SfxSynth.Decay(t, 0.001f, 0.08f), SfxSynth.Sine, 0.3f);
            }
            s.Voice(b, 0f, t => 1600f * Exp(-t / 0.4f) + 400f, t => 0.3f * SfxSynth.Ahr(t, 0.02f, 0.3f, 0.45f), SfxSynth.Triangle, 0.8f);
            SfxSynth.Finish(b, 0.55f);
            return b;
        }

        public static float[] Bomb(SfxSynth s)
        {
            float[] b = s.Buffer(1.05f);
            s.Voice(b, 0f, t => 1700f - 1300f * Math.Min(1f, t / 0.28f), t => 0.35f * SfxSynth.Ahr(t, 0.01f, 0.22f, 0.05f), SfxSynth.Sine, 0.28f);
            float[] boom = Explosion(s, 0.75f);
            int offset = (int)(0.28f * SfxSynth.SampleRate);
            for (int i = 0; i < boom.Length && offset + i < b.Length; i++)
            {
                b[offset + i] += boom[i];
            }
            SfxSynth.Finish(b, 0.95f);
            return b;
        }

        public static float[] Slot(SfxSynth s)
        {
            float[] b = s.Buffer(0.5f);
            s.Voice(b, 0f, t => Math.Min(1600f, 300f * (float)Math.Pow(2.0, t / 0.1)), t => 0.6f * SfxSynth.Ahr(t, 0.005f, 0.26f, 0.1f),
                w => 0.5f * SfxSynth.Pulse(w, 0.5f) + 0.5f * SfxSynth.Triangle(w), 0.4f);
            Bell(s, b, 0.3f, 2093f, 0.12f, 0.6f);
            SfxSynth.LowPass(b, 5000f);
            SfxSynth.Finish(b, 0.6f);
            return b;
        }

        public static float[] Whoosh(SfxSynth s)
        {
            float[] b = s.Buffer(1f);
            s.NoiseVoice(b, 0f, t =>
            {
                float x = Sin((float)Math.PI * t);
                return 300f + 2600f * x * x;
            }, t => SfxSynth.Ahr(t, 0.3f, 0.2f, 0.5f), 1f, 200f);
            SfxSynth.Finish(b, 0.65f);
            return b;
        }

        public static float[] Coin(SfxSynth s)
        {
            float[] b = s.Buffer(0.32f);
            s.Voice(b, 0f, t => 988f, t => SfxSynth.Ahr(t, 0.002f, 0.06f, 0.02f), w => SfxSynth.Pulse(w, 0.5f), 0.09f);
            s.Voice(b, 0.07f, t => 1318.5f, t => SfxSynth.Decay(t, 0.002f, 0.12f), w => SfxSynth.Pulse(w, 0.5f), 0.25f);
            SfxSynth.LowPass(b, 6000f);
            SfxSynth.Finish(b, 0.5f);
            return b;
        }

        private static void Bell(SfxSynth s, float[] b, float start, float frequency, float decay, float level)
        {
            s.Voice(b, start, t => frequency, t => level * SfxSynth.Decay(t, 0.002f, decay), SfxSynth.Sine, decay * 5f);
            s.Voice(b, start, t => frequency * 2.76f, t => 0.25f * level * SfxSynth.Decay(t, 0.001f, decay * 0.35f), SfxSynth.Sine, decay * 2f);
            s.Voice(b, start, t => frequency * 2f, t => 0.2f * level * SfxSynth.Decay(t, 0.002f, decay * 0.6f), SfxSynth.Sine, decay * 3f);
        }

        /// <summary>Seamless 8-bar chiptune loop (I-vi-IV-V in C) at 112 BPM.</summary>
        public static float[] Music(SfxSynth s)
        {
            const float bpm = 112f;
            float beat = 60f / bpm;
            const int beats = 32;
            float loop = beat * beats;
            float[] b = s.Buffer(loop + 1.5f);

            int[][] chords =
            {
                new[] { -9, -5, -2 },   // C
                new[] { -12, -9, -5 },  // Am
                new[] { -16, -12, -9 }, // F
                new[] { -14, -10, -7 }, // G
            };
            int[] roots = { -33, -36, -40, -38 };

            for (int bar = 0; bar < 8; bar++)
            {
                int[] chord = chords[bar % 4];
                float barStart = bar * 4 * beat;

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    float f = SfxSynth.Note(chord[eighth % 3] + 12 * (eighth >= 4 ? 1 : 0));
                    s.Voice(b, barStart + eighth * beat * 0.5f, t => f, t => 0.12f * SfxSynth.Decay(t, 0.004f, 0.09f),
                        w => SfxSynth.Pulse(w, 0.125f), beat * 0.5f);
                }

                for (int q = 0; q < 4; q++)
                {
                    float root = SfxSynth.Note(roots[bar % 4] + (q % 2 == 1 ? 12 : 0));
                    s.Voice(b, barStart + q * beat, t => root, t => 0.35f * SfxSynth.Ahr(t, 0.005f, beat * 0.55f, beat * 0.3f),
                        SfxSynth.Triangle, beat * 0.95f);
                }

                for (int q = 0; q < 4; q++)
                {
                    float t0 = barStart + q * beat;
                    if (q % 2 == 0)
                    {
                        s.Voice(b, t0, t => 120f * Exp(-t / 0.03f) + 45f, t => 0.55f * SfxSynth.Decay(t, 0.002f, 0.09f), SfxSynth.Sine, 0.25f);
                    }
                    else
                    {
                        s.NoiseVoice(b, t0, t => 5000f, t => 0.16f * SfxSynth.Decay(t, 0.001f, 0.06f), 0.18f, 900f);
                    }
                    s.NoiseVoice(b, t0 + beat * 0.5f, t => 9000f, t => 0.05f * SfxSynth.Decay(t, 0.001f, 0.02f), 0.06f, 5000f);
                }
            }

            // Pentatonic hook: (semitones from A4, length in eighths); 99 = rest.
            int[,] melody =
            {
                { 3, 2 }, { 7, 2 }, { 10, 2 }, { 7, 2 }, { 12, 3 }, { 10, 1 }, { 7, 4 },
                { 5, 2 }, { 7, 2 }, { 3, 4 }, { 99, 2 }, { 0, 2 }, { 3, 2 }, { 5, 4 },
                { 3, 2 }, { 7, 2 }, { 10, 2 }, { 12, 2 }, { 15, 3 }, { 12, 1 }, { 10, 4 },
                { 7, 2 }, { 10, 2 }, { 7, 2 }, { 5, 2 }, { 3, 8 },
            };
            float cursor = 0f;
            for (int i = 0; i < melody.GetLength(0); i++)
            {
                int note = melody[i, 0];
                float length = melody[i, 1] * beat * 0.5f;
                if (note != 99)
                {
                    float f = SfxSynth.Note(note);
                    float noteLength = length;
                    s.Voice(b, cursor, t => f * (1f + 0.004f * Sin(t * 33f)), t => 0.2f * SfxSynth.Ahr(t, 0.01f, noteLength * 0.6f, noteLength * 0.35f),
                        w => 0.7f * SfxSynth.Triangle(w) + 0.3f * SfxSynth.Pulse(w, 0.5f), noteLength);
                }
                cursor += length;
            }

            // Fold the tail onto the start so the loop point is seamless.
            int loopSamples = (int)(loop * SfxSynth.SampleRate);
            var looped = new float[loopSamples];
            for (int i = 0; i < b.Length; i++)
            {
                looped[i % loopSamples] += b[i];
            }

            SfxSynth.LowPass(looped, 7000f);
            float max = 1e-6f;
            for (int i = 0; i < looped.Length; i++)
            {
                max = Math.Max(max, Math.Abs(looped[i]));
            }
            for (int i = 0; i < looped.Length; i++)
            {
                looped[i] *= 0.8f / max;
            }
            return looped;
        }
    }
}
