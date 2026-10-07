using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>Cheap physics for cube chips: gravity, floor bounces, spin and a shrink-out, drawn instanced.</summary>
    public sealed class DebrisSystem : MonoBehaviour
    {
        private const int Capacity = 1600;
        private const float Gravity = 24f;
        private const float Bounce = 0.36f;
        private const float GroundFriction = 0.72f;

        private Piece[] _pieces;
        private int _count;
        private InstancedBatch _batch;
        private Mesh _mesh;

        public void Init(VisualConfig config)
        {
            _pieces = new Piece[Capacity];
            _mesh = MeshFactory.ChamferCube(0.18f, Color.white);
            _batch = new InstancedBatch(_mesh, config.ToyInstancedMaterial, true, new Bounds(Vector3.zero, Vector3.one * 400f));
        }

        public void Clear()
        {
            _count = 0;
        }

        /// <summary>Radial burst biased upward.</summary>
        public void Burst(Vector3 position, Color color, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                var velocity = new Vector3(dir.x, Random.Range(0.9f, 1.8f), dir.y) * (speed * Random.Range(0.55f, 1f));
                Spawn(position, velocity, color, size * Random.Range(0.7f, 1.25f), Random.Range(0.55f, 0.95f));
            }
        }

        public void Spawn(Vector3 position, Vector3 velocity, Color color, float size, float life)
        {
            if (_pieces == null)
            {
                return;
            }

            int index;
            if (_count < Capacity)
            {
                index = _count++;
            }
            else
            {
                // Recycle the oldest-looking slot instead of dropping new, more visible debris.
                index = Random.Range(0, Capacity);
            }

            _pieces[index] = new Piece
            {
                Position = position,
                Velocity = velocity,
                Rotation = Random.rotationUniform,
                Spin = Random.insideUnitSphere * 14f,
                Size = size,
                Age = 0f,
                Life = life,
                Color = color.linear
            };
        }

        private void LateUpdate()
        {
            if (_pieces == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            _batch.Clear();
            for (int i = _count - 1; i >= 0; i--)
            {
                ref Piece p = ref _pieces[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    _pieces[i] = _pieces[--_count];
                    continue;
                }

                p.Velocity.y -= Gravity * dt;
                p.Position += p.Velocity * dt;
                float half = p.Size * 0.5f;
                if (p.Position.y < half)
                {
                    p.Position.y = half;
                    if (p.Velocity.y < 0f)
                    {
                        p.Velocity.y = -p.Velocity.y * Bounce;
                        p.Velocity.x *= GroundFriction;
                        p.Velocity.z *= GroundFriction;
                        p.Spin *= 0.6f;
                    }
                }

                p.Rotation = Quaternion.Euler(p.Spin * dt) * p.Rotation;
                float t = p.Age / p.Life;
                float shrink = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                float pop = t < 0.08f ? Mathf.Lerp(0.5f, 1f, t / 0.08f) : 1f;
                float size = p.Size * shrink * pop;
                float flash = t < 0.1f ? 0.6f * (1f - t / 0.1f) : 0f;
                _batch.Add(Matrix4x4.TRS(p.Position, p.Rotation, new Vector3(size, size, size)), p.Color, new Vector4(flash, 0.1f, 0f, 0f));
            }

            _batch.Render();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private struct Piece
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Quaternion Rotation;
            public Vector3 Spin;
            public float Size;
            public float Age;
            public float Life;
            public Vector4 Color;
        }
    }
}
