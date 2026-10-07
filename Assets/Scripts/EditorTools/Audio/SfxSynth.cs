using System;
using System.IO;

namespace PixelDefense.EditorTools
{
    /// <summary>Tiny offline synthesizer: oscillators, envelopes, filters and a WAV writer for placeholder audio.</summary>
    public sealed class SfxSynth
    {
        public const int SampleRate = 44100;
        private const float TwoPi = (float)(Math.PI * 2.0);

        private readonly Random _random;

        public SfxSynth(int seed)
        {
            _random = new Random(seed);
        }

        public float[] Buffer(float seconds)
        {
            return new float[(int)(seconds * SampleRate)];
        }

        public float Noise()
        {
            return (float)(_random.NextDouble() * 2.0 - 1.0);
        }

        public float Range(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        public static float Sine(float phase)
        {
            return (float)Math.Sin(phase * TwoPi);
        }

        public static float Triangle(float phase)
        {
            float p = phase - (float)Math.Floor(phase);
            return p < 0.5f ? p * 4f - 1f : 3f - p * 4f;
        }

        public static float Saw(float phase)
        {
            float p = phase - (float)Math.Floor(phase);
            return p * 2f - 1f;
        }

        public static float Pulse(float phase, float width)
        {
            float p = phase - (float)Math.Floor(phase);
            return p < width ? 1f : -1f;
        }

        /// <summary>Exponential decay envelope with a linear attack.</summary>
        public static float Decay(float t, float attack, float decay)
        {
            if (t < 0f)
            {
                return 0f;
            }
            if (t < attack)
            {
                return t / attack;
            }
            return (float)Math.Exp(-(t - attack) / decay);
        }

        /// <summary>Attack / hold / release envelope.</summary>
        public static float Ahr(float t, float attack, float hold, float release)
        {
            if (t < 0f)
            {
                return 0f;
            }
            if (t < attack)
            {
                return t / attack;
            }
            if (t < attack + hold)
            {
                return 1f;
            }
            float r = (t - attack - hold) / release;
            return r >= 1f ? 0f : (1f - r) * (1f - r);
        }

        public static float Note(int semitonesFromA4)
        {
            return 440f * (float)Math.Pow(2.0, semitonesFromA4 / 12.0);
        }

        /// <summary>Adds an oscillator voice whose frequency and amplitude are functions of time.</summary>
        public void Voice(float[] buffer, float start, Func<float, float> frequency, Func<float, float> amplitude,
            Func<float, float> wave, float duration)
        {
            int from = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            float phase = 0f;
            for (int i = 0; i < count && from + i < buffer.Length; i++)
            {
                float t = (float)i / SampleRate;
                phase += frequency(t) / SampleRate;
                if (from + i >= 0)
                {
                    buffer[from + i] += wave(phase) * amplitude(t);
                }
            }
        }

        /// <summary>Adds filtered noise; cutoff in Hz as a function of time (one-pole low-pass, optional high-pass).</summary>
        public void NoiseVoice(float[] buffer, float start, Func<float, float> cutoff, Func<float, float> amplitude, float duration,
            float highPass = 0f)
        {
            int from = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            float low = 0f;
            float hpState = 0f;
            float hpPrev = 0f;
            float hpAlpha = highPass > 0f ? 1f / (1f + TwoPi * highPass / SampleRate) : 0f;
            for (int i = 0; i < count && from + i < buffer.Length; i++)
            {
                float t = (float)i / SampleRate;
                float alpha = 1f - (float)Math.Exp(-TwoPi * Math.Max(20f, cutoff(t)) / SampleRate);
                low += (Noise() - low) * alpha;
                float sample = low;
                if (highPass > 0f)
                {
                    hpState = hpAlpha * (hpState + sample - hpPrev);
                    hpPrev = sample;
                    sample = hpState;
                }

                if (from + i >= 0)
                {
                    buffer[from + i] += sample * amplitude(t);
                }
            }
        }

        public static void SoftClip(float[] buffer, float drive)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (float)Math.Tanh(buffer[i] * drive) / (float)Math.Tanh(drive);
            }
        }

        public static void LowPass(float[] buffer, float cutoff)
        {
            float alpha = 1f - (float)Math.Exp(-TwoPi * cutoff / SampleRate);
            float state = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                state += (buffer[i] - state) * alpha;
                buffer[i] = state;
            }
        }

        /// <summary>Scales to the given peak and applies short fades so clips never click.</summary>
        public static void Finish(float[] buffer, float peak)
        {
            float max = 1e-6f;
            for (int i = 0; i < buffer.Length; i++)
            {
                max = Math.Max(max, Math.Abs(buffer[i]));
            }

            float gain = peak / max;
            int fade = Math.Min(buffer.Length / 4, SampleRate / 200);
            for (int i = 0; i < buffer.Length; i++)
            {
                float g = gain;
                if (i < fade)
                {
                    g *= (float)i / fade;
                }
                if (i > buffer.Length - fade)
                {
                    g *= (float)(buffer.Length - i) / fade;
                }
                buffer[i] *= g;
            }
        }

        public static void WriteWav(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int byteCount = samples.Length * 2;
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + byteCount);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(byteCount);
                for (int i = 0; i < samples.Length; i++)
                {
                    float clamped = Math.Max(-1f, Math.Min(1f, samples[i]));
                    writer.Write((short)(clamped * 32767f));
                }
            }
        }
    }
}
