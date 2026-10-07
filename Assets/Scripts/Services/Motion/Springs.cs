using UnityEngine;

namespace PixelDefense.Services.Motion
{
    /// <summary>Damped spring state for continuous, interruptible motion (recoils, wobbles, follow).</summary>
    public struct FloatSpring
    {
        public float Value;
        public float Velocity;

        public FloatSpring(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        /// <param name="frequency">Oscillations per second.</param>
        /// <param name="damping">1 = critically damped, below 1 overshoots.</param>
        public void Step(float target, float frequency, float damping, float deltaTime)
        {
            Springs.Step(ref Value, ref Velocity, target, frequency, damping, deltaTime);
        }

        public void Kick(float impulse)
        {
            Velocity += impulse;
        }
    }

    public struct Vector3Spring
    {
        public Vector3 Value;
        public Vector3 Velocity;

        public Vector3Spring(Vector3 value)
        {
            Value = value;
            Velocity = Vector3.zero;
        }

        public void Step(Vector3 target, float frequency, float damping, float deltaTime)
        {
            Springs.Step(ref Value.x, ref Velocity.x, target.x, frequency, damping, deltaTime);
            Springs.Step(ref Value.y, ref Velocity.y, target.y, frequency, damping, deltaTime);
            Springs.Step(ref Value.z, ref Velocity.z, target.z, frequency, damping, deltaTime);
        }

        public void Kick(Vector3 impulse)
        {
            Velocity += impulse;
        }
    }

    public static class Springs
    {
        /// <summary>Semi-implicit spring step; stable for game frame rates at the frequencies used here.</summary>
        public static void Step(ref float value, ref float velocity, float target, float frequency, float damping, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float omega = 2f * Mathf.PI * frequency;
            // Sub-step long frames so hitches don't explode stiff springs.
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime * omega / 0.5f));
            float h = deltaTime / steps;
            for (int i = 0; i < steps; i++)
            {
                float acceleration = omega * omega * (target - value) - 2f * damping * omega * velocity;
                velocity += acceleration * h;
                value += velocity * h;
            }
        }

        /// <summary>Frame-rate independent exponential approach (sharpness = 1/seconds-ish).</summary>
        public static float Damp(float current, float target, float sharpness, float deltaTime)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));
        }

        public static Vector3 Damp(Vector3 current, Vector3 target, float sharpness, float deltaTime)
        {
            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));
        }
    }

    public static class Easing
    {
        public static float OutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f)
            {
                return t;
            }
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        /// <summary>0 → 1 → 0 bump, useful for punches and flashes.</summary>
        public static float Bump(float t)
        {
            t = Mathf.Clamp01(t);
            return 4f * t * (1f - t);
        }
    }
}
