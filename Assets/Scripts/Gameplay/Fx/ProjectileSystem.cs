using System.Collections.Generic;
using PixelDefense.Core;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Glowing balls lobbed from cannon muzzles to their reserved scales. Flight timing comes from the model
    /// (fired/impact times) and the end point tracks the moving scale, so shots always land on the cube that pops.
    /// </summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        private const int TrailGhosts = 3;
        private const float TrailSpacing = 0.045f;

        private readonly List<Projectile> _active = new List<Projectile>(128);
        private Battle _battle;
        private DragonView _dragon;
        private VisualConfig _config;
        private InstancedBatch _batch;
        private Mesh _mesh;

        public void Init(VisualConfig config)
        {
            _config = config;
            _mesh = MeshFactory.Sphere(12, 8, Color.white);
            _batch = new InstancedBatch(_mesh, config.ToyInstancedMaterial, false, new Bounds(Vector3.zero, Vector3.one * 400f));
        }

        public void Bind(Battle battle, DragonView dragon)
        {
            _battle = battle;
            _dragon = dragon;
            _active.Clear();
        }

        public void Launch(Shot shot, Vector3 muzzle, Color color)
        {
            _active.Add(new Projectile
            {
                ShotId = shot.Id,
                Start = muzzle,
                FiredAt = shot.FiredAt,
                ImpactAt = shot.ImpactAt,
                Slice = shot.Slice,
                Lane = shot.Lane,
                Color = color.linear
            });
        }

        public void Land(int shotId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].ShotId == shotId)
                {
                    _active.RemoveAt(i);
                    return;
                }
            }
        }

        public void Clear()
        {
            _active.Clear();
        }

        private void LateUpdate()
        {
            if (_battle == null || _dragon == null)
            {
                return;
            }

            _batch.Clear();
            float now = _battle.Time;
            float size = _config.ProjectileSize * _config.WorldPerSlice;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Projectile p = _active[i];
                float duration = Mathf.Max(0.01f, p.ImpactAt - p.FiredAt);
                float t = (now - p.FiredAt) / duration;
                if (t > 1.2f)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                Vector3 end = _dragon.CubePosition(p.Slice, p.Lane);
                float arc = _config.ShotArcHeight * _config.WorldPerSlice + Vector3.Distance(p.Start, end) * 0.12f;
                Vector3 position = Evaluate(p.Start, end, arc, t);
                Vector3 ahead = Evaluate(p.Start, end, arc, t + 0.02f);
                Vector3 direction = ahead - position;
                Quaternion rotation = direction.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(direction) : Quaternion.identity;

                float grow = Mathf.Clamp01(t * 6f);
                var stretched = new Vector3(size, size, size * 1.7f) * grow;
                _batch.Add(Matrix4x4.TRS(position, rotation, stretched), p.Color, new Vector4(0.3f, 2.6f, 0f, 0f));

                for (int g = 1; g <= TrailGhosts; g++)
                {
                    float tg = t - g * TrailSpacing;
                    if (tg <= 0f)
                    {
                        break;
                    }
                    float ghost = size * (1f - g / (TrailGhosts + 1f)) * grow;
                    _batch.Add(Matrix4x4.TRS(Evaluate(p.Start, end, arc, tg), rotation, new Vector3(ghost, ghost, ghost)), p.Color,
                        new Vector4(0.15f, 1.8f, 0f, 0f));
                }
            }

            _batch.Render();
        }

        private static Vector3 Evaluate(Vector3 start, Vector3 end, float arc, float t)
        {
            t = Mathf.Clamp01(t);
            return Vector3.Lerp(start, end, t) + Vector3.up * (arc * 4f * t * (1f - t));
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private struct Projectile
        {
            public int ShotId;
            public Vector3 Start;
            public float FiredAt;
            public float ImpactAt;
            public int Slice;
            public int Lane;
            public Vector4 Color;
        }
    }
}
