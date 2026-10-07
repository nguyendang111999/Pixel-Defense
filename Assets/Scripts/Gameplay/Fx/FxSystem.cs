using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Shuriken effects configured in code: one-shot emitters driven by Emit (flash, sparks, puffs, confetti,
    /// ice) and two looping emitters that follow the dragon (fire breath, nostril smoke).
    /// </summary>
    public sealed class FxSystem : MonoBehaviour
    {
        private ParticleSystem _flash;
        private ParticleSystem _sparks;
        private ParticleSystem _puff;
        private ParticleSystem _confetti;
        private ParticleSystem _sparkle;
        private ParticleSystem _fire;
        private ParticleSystem _smoke;
        private Transform _fireAnchor;
        private Transform _smokeAnchor;
        private Color[] _confettiColors;

        public void Init(VisualConfig config)
        {
            _confettiColors = config.ScaleColors;

            _flash = Create("Flash", config.ParticleAdditive, 256, ParticleSystemRenderMode.Billboard);
            SetFade(_flash, 1f, 0f);
            SetSizeCurve(_flash, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.5f));

            _sparks = Create("Sparks", config.ParticleAdditive, 512, ParticleSystemRenderMode.Stretch);
            SetFade(_sparks, 1f, 0f);
            SetSizeCurve(_sparks, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ParticleSystem.MainModule sparkMain = _sparks.main;
            sparkMain.gravityModifier = 1.2f;
            var sparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.velocityScale = 0.06f;
            sparkRenderer.lengthScale = 1.6f;

            _puff = Create("Puff", config.ParticleAlpha, 256, ParticleSystemRenderMode.Billboard);
            SetFade(_puff, 0.85f, 0f);
            SetSizeCurve(_puff, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));
            ParticleSystem.LimitVelocityOverLifetimeModule limit = _puff.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 4f;

            _confetti = Create("Confetti", config.ParticleConfetti, 600, ParticleSystemRenderMode.Billboard);
            ParticleSystem.MainModule confettiMain = _confetti.main;
            confettiMain.gravityModifier = 0.55f;
            SetFade(_confetti, 1f, 0.9f);
            ParticleSystem.RotationOverLifetimeModule spin = _confetti.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            ParticleSystem.LimitVelocityOverLifetimeModule confettiDrag = _confetti.limitVelocityOverLifetime;
            confettiDrag.enabled = true;
            confettiDrag.drag = 1.6f;
            ParticleSystem.NoiseModule noise = _confetti.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.6f;

            _sparkle = Create("Sparkle", config.ParticleSparkle, 256, ParticleSystemRenderMode.Billboard);
            SetFade(_sparkle, 1f, 0f);
            SetSizeCurve(_sparkle, AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f));
            ParticleSystem.RotationOverLifetimeModule twinkle = _sparkle.rotationOverLifetime;
            twinkle.enabled = true;
            twinkle.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

            _fire = CreateLooping("FireBreath", config.ParticleAdditive, 400, 90f);
            ParticleSystem.MainModule fireMain = _fire.main;
            fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.42f);
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 7f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            fireMain.gravityModifier = -0.15f;
            ParticleSystem.ShapeModule fireShape = _fire.shape;
            fireShape.enabled = true;
            fireShape.shapeType = ParticleSystemShapeType.Cone;
            fireShape.angle = 16f;
            fireShape.radius = 0.06f;
            SetSizeCurve(_fire, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.1f));
            SetGradient(_fire, new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.35f),
                new GradientColorKey(new Color(0.9f, 0.15f, 0.05f), 0.75f),
                new GradientColorKey(new Color(0.3f, 0.05f, 0.05f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });

            _smoke = CreateLooping("NostrilSmoke", config.ParticleAlpha, 120, 14f);
            ParticleSystem.MainModule smokeMain = _smoke.main;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.25f);
            smokeMain.startColor = new Color(0.25f, 0.22f, 0.25f, 0.8f);
            smokeMain.gravityModifier = -0.25f;
            ParticleSystem.ShapeModule smokeShape = _smoke.shape;
            smokeShape.enabled = true;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 25f;
            smokeShape.radius = 0.05f;
            SetFade(_smoke, 0.8f, 0f);
            SetSizeCurve(_smoke, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2f));
        }

        public void Flash(Vector3 position, Color color, float size, float life = 0.14f)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                startSize = size,
                startLifetime = life,
                velocity = Vector3.zero,
                applyShapeToPosition = false
            };
            _flash.Emit(emit, 1);
        }

        public void Sparks(Vector3 position, Color color, int count, float speed)
        {
            var emit = new ParticleSystem.EmitParams { position = position, startColor = color, applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.35f;
                emit.velocity = dir.normalized * (speed * Random.Range(0.5f, 1f));
                emit.startSize = Random.Range(0.06f, 0.12f);
                emit.startLifetime = Random.Range(0.2f, 0.38f);
                _sparks.Emit(emit, 1);
            }
        }

        public void Puff(Vector3 position, Color color, int count, float size, float speed = 1.6f)
        {
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized;
                emit.position = position + new Vector3(ring.x, 0f, ring.y) * size * 0.3f;
                emit.velocity = new Vector3(ring.x, Random.Range(0.3f, 0.8f), ring.y) * speed;
                emit.startSize = size * Random.Range(0.7f, 1.2f);
                emit.startLifetime = Random.Range(0.4f, 0.65f);
                emit.rotation = Random.Range(0f, 360f);
                _puff.Emit(emit, 1);
            }
        }

        public void Confetti(Vector3 position, int count, float speed)
        {
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 1.6f + 0.6f;
                emit.position = position;
                emit.velocity = dir.normalized * (speed * Random.Range(0.4f, 1f));
                emit.startColor = _confettiColors[Random.Range(0, _confettiColors.Length - 2)];
                emit.startSize = Random.Range(0.1f, 0.2f);
                emit.startLifetime = Random.Range(1.6f, 2.6f);
                emit.rotation = Random.Range(0f, 360f);
                _confetti.Emit(emit, 1);
            }
        }

        public void Sparkle(Vector3 position, Color color, int count, float radius, float size = 0.3f)
        {
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                emit.position = position + Random.insideUnitSphere * radius;
                emit.velocity = Vector3.up * Random.Range(0.3f, 1.2f);
                emit.startSize = size * Random.Range(0.6f, 1.3f);
                emit.startLifetime = Random.Range(0.45f, 0.8f);
                emit.rotation = Random.Range(0f, 360f);
                _sparkle.Emit(emit, 1);
            }
        }

        public void AttachDragon(Transform mouth, Transform nostrils)
        {
            _fireAnchor = mouth;
            _smokeAnchor = nostrils;
            SetFire(false);
            SetSmoke(false);
        }

        public void SetFire(bool on)
        {
            SetLooping(_fire, on);
        }

        public void SetSmoke(bool on)
        {
            SetLooping(_smoke, on);
        }

        public void StopAll()
        {
            ParticleSystem[] all = { _flash, _sparks, _puff, _confetti, _sparkle, _fire, _smoke };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    all[i].Clear(true);
                }
            }
            SetFire(false);
            SetSmoke(false);
        }

        private void LateUpdate()
        {
            Follow(_fire, _fireAnchor, 0f);
            Follow(_smoke, _smokeAnchor, -35f);
        }

        private static void Follow(ParticleSystem system, Transform anchor, float pitch)
        {
            if (system == null || anchor == null || !anchor.gameObject.activeInHierarchy)
            {
                return;
            }

            system.transform.SetPositionAndRotation(anchor.position, anchor.rotation * Quaternion.Euler(pitch, 0f, 0f));
        }

        private static void SetLooping(ParticleSystem system, bool on)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = on;
        }

        private ParticleSystem Create(string systemName, Material material, int maxParticles, ParticleSystemRenderMode mode)
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = mode;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.alignment = ParticleSystemRenderSpace.View;

            system.Play();
            return system;
        }

        private ParticleSystem CreateLooping(string systemName, Material material, int maxParticles, float rate)
        {
            ParticleSystem system = Create(systemName, material, maxParticles, ParticleSystemRenderMode.Billboard);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
            emission.enabled = false;
            return system;
        }

        private static void SetFade(ParticleSystem system, float startAlpha, float endAlpha)
        {
            SetGradient(system, new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(startAlpha, 0.5f), new GradientAlphaKey(endAlpha, 1f) });
        }

        private static void SetGradient(ParticleSystem system, GradientColorKey[] colors, GradientAlphaKey[] alphas)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(colors, alphas);
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void SetSizeCurve(ParticleSystem system, AnimationCurve curve)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
    }
}
