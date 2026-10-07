using System.Collections.Generic;
using PixelDefense.Core;
using PixelDefense.Services.Motion;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Renders the dragon from the battle model: instanced scale cubes riding the track on springs (so recoils
    /// snap back with a little overshoot), a slithering body wave, the voxel head (jaw, blinking eyes), tail and
    /// spine spikes, plus hit pops and the final shatter.
    /// </summary>
    public sealed class DragonView : MonoBehaviour
    {
        private const float DyingDuration = 0.16f;
        private const int TailSegments = 4;
        private const float IntroDistance = 46f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        private readonly List<Dying> _dying = new List<Dying>(64);
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private Battle _battle;
        private ArenaSpace _space;
        private VisualConfig _config;
        private DragonSkin _skin;
        private DebrisSystem _debris;
        private InstancedBatch _cubes;
        private InstancedBatch _spines;
        private Mesh _cubeMesh;
        private Mesh _spineMesh;
        private float[] _targets;
        private float[] _display;
        private float[] _velocity;
        private float[] _punch;
        private bool[] _wasAlive;
        private Vector4[] _linearPalette;
        private DragonHeadModel _headModel;
        private Transform _head;
        private Transform _jawPivot;
        private Transform _eyeLeft;
        private Transform _eyeRight;
        private Transform _mouth;
        private MeshRenderer[] _headRenderers;
        private MeshRenderer[] _eyeRenderers;
        private MaterialPropertyBlock _block;
        private FloatSpring _flinch;
        private FloatSpring _squash;
        private FloatSpring _jaw;
        private float _jawTarget;
        private float _roarTimer;
        private float _blinkTimer;
        private float _blinkPhase;
        private float _introOffset;
        private float _introTime;
        private float _introDuration;
        private float _headS;
        private float _tailS;
        private float _freeze;
        private float _freezeTarget;
        private float _hitFlash;
        private bool _enraged;
        private bool _attacking;
        private bool _exploded;
        private bool _built;

        public Transform Head => _head;
        public Transform Mouth => _mouth;
        public bool IsIntroPlaying => _introTime < _introDuration;

        /// <summary>Front slice → snout tip distance in slice units; the battle's contact point uses it.</summary>
        public static float HeadReach(VisualConfig config)
        {
            return 0.5f + config.NeckGap + DragonHeadModel.LengthVoxels * config.HeadVoxel;
        }

        public void Build(Battle battle, ArenaSpace space, VisualConfig config, DragonSkin skin, DebrisSystem debris)
        {
            _battle = battle;
            _space = space;
            _config = config;
            _skin = skin;
            _debris = debris;
            _block = new MaterialPropertyBlock();

            int slices = battle.Dragon.SliceCount;
            _targets = new float[slices];
            _display = new float[slices];
            _velocity = new float[slices];
            _punch = new float[slices];
            _wasAlive = new bool[slices];
            battle.Dragon.GetSlicePositions(_targets);
            for (int s = 0; s < slices; s++)
            {
                _display[s] = _targets[s];
                _wasAlive[s] = true;
            }

            _linearPalette = new Vector4[config.ScaleColors.Length];
            for (int i = 0; i < _linearPalette.Length; i++)
            {
                _linearPalette[i] = config.ScaleColors[i].linear;
            }

            var bounds = new Bounds(space.Origin, Vector3.one * 200f);
            _cubeMesh = Track(MeshFactory.ChamferCube(0.14f, Color.white));
            _spineMesh = Track(BuildSpineMesh());
            _cubes = new InstancedBatch(_cubeMesh, config.ToyInstancedMaterial, true, bounds);
            _spines = new InstancedBatch(_spineMesh, config.ToyInstancedMaterial, true, bounds);

            BuildHead();
            _headS = _targets[0] + 0.5f + config.NeckGap;
            _tailS = _targets[slices - 1];
            _blinkTimer = Random.Range(1.5f, 3.5f);
            _introDuration = 0f;
            _introTime = 0f;
            _built = true;
        }

        private void BuildHead()
        {
            _headModel = new DragonHeadModel(_skin);
            float voxel = _config.HeadVoxel * _config.WorldPerSlice;

            _head = new GameObject("DragonHead").transform;
            _head.SetParent(transform, false);

            var renderers = new List<MeshRenderer>();
            renderers.Add(CreatePart("Skull", _head, Track(_headModel.Skull.BuildMesh(voxel, Vector3.zero, "Skull")), Vector3.zero));

            _jawPivot = new GameObject("JawPivot").transform;
            _jawPivot.SetParent(_head, false);
            _jawPivot.localPosition = DragonHeadModel.JawPivot * voxel;
            renderers.Add(CreatePart("Jaw", _jawPivot, Track(_headModel.Jaw.BuildMesh(voxel, DragonHeadModel.JawPivot, "Jaw")), Vector3.zero));

            _eyeLeft = new GameObject("EyeLeft").transform;
            _eyeLeft.SetParent(_head, false);
            _eyeLeft.localPosition = DragonHeadModel.EyePivotLeft * voxel;
            MeshRenderer leftEye = CreatePart("Eye", _eyeLeft, Track(_headModel.EyeLeft.BuildMesh(voxel, DragonHeadModel.EyePivotLeft, "EyeL")), Vector3.zero);

            _eyeRight = new GameObject("EyeRight").transform;
            _eyeRight.SetParent(_head, false);
            _eyeRight.localPosition = DragonHeadModel.EyePivotRight * voxel;
            MeshRenderer rightEye = CreatePart("Eye", _eyeRight, Track(_headModel.EyeRight.BuildMesh(voxel, DragonHeadModel.EyePivotRight, "EyeR")), Vector3.zero);

            _mouth = new GameObject("Mouth").transform;
            _mouth.SetParent(_head, false);
            _mouth.localPosition = DragonHeadModel.MouthPoint * voxel;

            _headRenderers = renderers.ToArray();
            _eyeRenderers = new[] { leftEye, rightEye };
        }

        private MeshRenderer CreatePart(string partName, Transform parent, Mesh mesh, Vector3 localPosition)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _config.ToyMaterial;
            return renderer;
        }

        public void PlayIntro(float duration)
        {
            _introDuration = duration;
            _introTime = 0f;
            _introOffset = -IntroDistance;
        }

        public void Roar(float duration = 0.9f)
        {
            _roarTimer = Mathf.Max(_roarTimer, duration);
            _squash.Kick(-3f);
        }

        public void SetAttacking(bool attacking)
        {
            _attacking = attacking;
        }

        public void SetEnraged()
        {
            _enraged = true;
            Roar(0.7f);
        }

        public void SetFrozen(bool frozen)
        {
            _freezeTarget = frozen ? 1f : 0f;
        }

        public void OnCubeDestroyed(int slice, int lane, byte color, bool bomb)
        {
            if (!_built)
            {
                return;
            }

            Vector3 position = CubePosition(slice, lane, out Quaternion rotation);
            _dying.Add(new Dying { Slice = slice, Lane = lane, Color = color, Age = 0f, Rotation = rotation });
            _punch[slice] = 1f;
            _hitFlash = Mathf.Max(_hitFlash, 0.25f);

            float scale = _config.CubeFill * _space.Scale;
            Color tint = _config.ScaleColor(color);
            _debris.Burst(position, tint, bomb ? _config.DebrisPerCube + 3 : _config.DebrisPerCube, _config.DebrisSpeed * (bomb ? 1.6f : 1f), scale * 0.42f);
        }

        public void OnSliceCleared(int slice)
        {
            _flinch.Kick(-9f);
            _squash.Kick(2.2f);
            _jawTarget = Mathf.Max(_jawTarget, 0.35f);
        }

        /// <summary>World position of a scale cube (alive or just destroyed), used to home projectiles.</summary>
        public Vector3 CubePosition(int slice, int lane, out Quaternion rotation)
        {
            float s = _display[slice];
            _space.Pose(s, out Vector3 position, out Vector3 forward, out Vector3 right);
            float bob = Wave(s);
            rotation = Quaternion.LookRotation(forward, Vector3.up);
            float height = _config.CubeHeight * _space.Scale * 0.5f + bob;
            return position + right * _space.LaneOffset(lane, _battle.Dragon.Width) + Vector3.up * height;
        }

        public Vector3 CubePosition(int slice, int lane)
        {
            return CubePosition(slice, lane, out _);
        }

        public Vector3 HeadCenter => _head.position + Vector3.up * (_space.Scale * 1.2f);

        private float Wave(float s)
        {
            return Mathf.Sin(s * 0.55f - Time.time * 5.2f) * _config.BodyWaveAmplitude * _space.Scale;
        }

        /// <summary>Hides the head and blasts its voxels outward as debris.</summary>
        public void Explode()
        {
            if (_exploded)
            {
                return;
            }

            _exploded = true;
            var positions = new List<Vector3>(512);
            var colors = new List<Color>(512);
            float voxel = _config.HeadVoxel * _config.WorldPerSlice;
            _headModel.Skull.CollectSurface(voxel, Vector3.zero, positions, colors);
            int skullCount = positions.Count;
            _headModel.Jaw.CollectSurface(voxel, DragonHeadModel.JawPivot, positions, colors);
            int jawCount = positions.Count;
            _headModel.EyeLeft.CollectSurface(voxel, DragonHeadModel.EyePivotLeft, positions, colors);
            int leftCount = positions.Count;
            _headModel.EyeRight.CollectSurface(voxel, DragonHeadModel.EyePivotRight, positions, colors);

            Vector3 center = HeadCenter;
            for (int i = 0; i < positions.Count; i++)
            {
                Transform part = i < skullCount ? _head : i < jawCount ? _jawPivot : i < leftCount ? _eyeLeft : _eyeRight;
                Vector3 world = part.TransformPoint(positions[i]);
                Vector3 outward = (world - center).normalized + Vector3.up * 0.9f;
                _debris.Spawn(world, outward * Random.Range(2.5f, 7.5f), colors[i], voxel * 1.05f, Random.Range(1.1f, 1.9f));
            }

            _head.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_built)
            {
                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;
            DragonBody body = _battle.Dragon;
            body.GetSlicePositions(_targets);

            float introOffset = 0f;
            if (_introTime < _introDuration)
            {
                _introTime += dt;
                introOffset = _introOffset * (1f - Easing.OutCubic(_introTime / _introDuration));
            }

            float frequency = _config.RecoilFrequency;
            float damping = _config.RecoilDamping;
            int frontSlice = body.FrontSlice;
            int lastAlive = -1;
            for (int s = 0; s < body.SliceCount; s++)
            {
                float target = _targets[s];
                if (float.IsNaN(target))
                {
                    _wasAlive[s] = false;
                    continue;
                }

                lastAlive = s;
                if (_introTime < _introDuration)
                {
                    _display[s] = target + introOffset;
                    _velocity[s] = 0f;
                }
                else
                {
                    Springs.Step(ref _display[s], ref _velocity[s], target, frequency, damping, dt);
                }
                _punch[s] = Mathf.MoveTowards(_punch[s], 0f, dt * 7f);
            }

            _freeze = Mathf.MoveTowards(_freeze, _freezeTarget, dt * 3f);
            _hitFlash = Mathf.MoveTowards(_hitFlash, 0f, dt * 4f);
            RenderBody(body, time);
            RenderDying(dt);
            UpdateHead(body, frontSlice, lastAlive, dt, time);
            RenderTail(lastAlive, time);

            _cubes.Render();
            _spines.Render();
        }

        private void RenderBody(DragonBody body, float time)
        {
            _cubes.Clear();
            _spines.Clear();
            float scale = _space.Scale;
            float cube = _config.CubeFill * scale;
            float height = _config.CubeHeight * scale;
            float pulse = _config.ExposedPulse * (0.55f + 0.45f * Mathf.Sin(time * 6.5f));
            var ice = new Vector4(0.62f, 0.86f, 1f, 1f);
            int lanes = body.Width;

            for (int s = 0; s < body.SliceCount; s++)
            {
                if (!body.IsSliceAlive(s))
                {
                    continue;
                }

                float ds = _display[s];
                _space.Pose(ds, out Vector3 position, out Vector3 forward, out Vector3 right);
                float bob = Wave(ds);
                float punch = _punch[s];
                Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
                bool exposed = body.IsExposed(s);
                float sliceScale = 1f + punch * 0.18f;
                var size = new Vector3(cube * sliceScale, height * (1f + punch * 0.1f), cube * sliceScale);

                for (int lane = 0; lane < lanes; lane++)
                {
                    if (!body.IsAlive(s, lane))
                    {
                        continue;
                    }

                    Vector3 p = position + right * _space.LaneOffset(lane, lanes) + Vector3.up * (height * 0.5f + bob);
                    Vector4 color = _linearPalette[body.ColorAt(s, lane)];
                    if (_freeze > 0f)
                    {
                        color = Vector4.Lerp(color, ice, 0.5f * _freeze);
                    }

                    var fx = new Vector4(punch * 0.35f + _hitFlash * 0.04f, exposed ? pulse : 0f, exposed ? 0f : _config.UnexposedDarken, 0f);
                    _cubes.Add(Matrix4x4.TRS(p, rotation, size), color, fx);
                }

            }
        }

        private void RenderDying(float dt)
        {
            float cube = _config.CubeFill * _space.Scale;
            float height = _config.CubeHeight * _space.Scale;
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                Dying d = _dying[i];
                d.Age += dt;
                if (d.Age >= DyingDuration)
                {
                    _dying.RemoveAt(i);
                    continue;
                }
                _dying[i] = d;

                float t = d.Age / DyingDuration;
                float grow = t < 0.3f ? Mathf.Lerp(1f, 1.4f, t / 0.3f) : Mathf.Lerp(1.4f, 0f, Easing.InCubic((t - 0.3f) / 0.7f));
                Vector3 position = CubePosition(d.Slice, d.Lane, out Quaternion rotation) + Vector3.up * (t * height * 0.8f);
                var size = new Vector3(cube * grow, height * grow, cube * grow);
                _cubes.Add(Matrix4x4.TRS(position, rotation, size), _linearPalette[d.Color], new Vector4(1f - t * 0.6f, 0.6f, 0f, 0f));
            }
        }

        private void UpdateHead(DragonBody body, int frontSlice, int lastAlive, float dt, float time)
        {
            if (_exploded)
            {
                return;
            }

            if (frontSlice < body.SliceCount)
            {
                _headS = _display[frontSlice] + 0.5f + _config.NeckGap;
            }

            float reach = DragonHeadModel.LengthVoxels * _config.HeadVoxel;
            _space.Pose(_headS, out Vector3 neck, out Vector3 forward, out _);
            _space.Pose(_headS + reach, out Vector3 tip, out _, out _);
            Vector3 chord = tip - neck;
            chord.y = 0f;
            if (chord.sqrMagnitude > 1e-6f)
            {
                forward = chord.normalized;
            }

            _flinch.Step(0f, 4.5f, 0.45f, dt);
            _squash.Step(0f, 5f, 0.4f, dt);

            _roarTimer = Mathf.Max(0f, _roarTimer - dt);
            float jawGoal = _roarTimer > 0f ? 1f : _attacking ? 0.75f + 0.25f * Mathf.Sin(time * 18f) : _jawTarget;
            _jawTarget = Mathf.MoveTowards(_jawTarget, 0f, dt * 2.5f);
            _jaw.Step(jawGoal, 6f, 0.6f, dt);

            float bob = Wave(_headS) + Mathf.Sin(time * 2.1f) * _space.Scale * 0.08f;
            float shake = (_roarTimer > 0f ? 1f : 0f) + (_attacking ? 0.6f : 0f);
            Vector3 jitter = shake > 0f ? new Vector3(Mathf.Sin(time * 47f), 0f, Mathf.Cos(time * 39f)) * (_space.Scale * 0.12f * shake) : Vector3.zero;
            _head.position = neck + Vector3.up * bob + jitter;

            float roarLift = _roarTimer > 0f ? -14f : 0f;
            _head.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(_flinch.Value * 2.2f + roarLift, Mathf.Sin(time * 1.3f) * 4f, 0f);
            float squash = _squash.Value * 0.06f;
            _head.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash * 0.5f);
            _jawPivot.localRotation = Quaternion.Euler(Mathf.Clamp01(_jaw.Value) * 32f, 0f, 0f);

            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blinkPhase = 0.14f;
                _blinkTimer = Random.Range(2f, 4.5f);
            }
            _blinkPhase = Mathf.Max(0f, _blinkPhase - dt);
            float eyeY = _blinkPhase > 0f ? 0.15f : 1f;
            _eyeLeft.localScale = new Vector3(1f, eyeY, 1f);
            _eyeRight.localScale = new Vector3(1f, eyeY, 1f);

            Color eyeColor = _enraged ? new Color(1f, 0.25f, 0.2f) : Color.white;
            _block.Clear();
            _block.SetColor(BaseColorId, eyeColor);
            _block.SetFloat(EmissionId, _enraged ? 1.2f + Mathf.Sin(time * 10f) * 0.4f : 0f);
            for (int i = 0; i < _eyeRenderers.Length; i++)
            {
                _eyeRenderers[i].SetPropertyBlock(_block);
            }

            _block.Clear();
            Color tint = Color.Lerp(Color.white, new Color(0.62f, 0.92f, 1.45f), _freeze);
            _block.SetColor(BaseColorId, tint);
            _block.SetFloat(FlashId, Mathf.Clamp01(_flinch.Value * -0.05f));
            for (int i = 0; i < _headRenderers.Length; i++)
            {
                _headRenderers[i].SetPropertyBlock(_block);
            }
        }

        private void RenderTail(int lastAlive, float time)
        {
            if (lastAlive < 0)
            {
                return;
            }

            _tailS = _display[lastAlive];
            float scale = _space.Scale;
            Vector4 main = _skin.Main.linear;
            Vector4 horn = _skin.Horn.linear;
            for (int i = 0; i < TailSegments; i++)
            {
                float s = _tailS - 1f - i * 0.9f;
                _space.Pose(s, out Vector3 position, out Vector3 forward, out Vector3 right);
                float sway = Mathf.Sin(time * 4f - i * 0.9f) * scale * 0.25f * (i + 1) / TailSegments;
                float size = Mathf.Lerp(0.85f, 0.4f, (float)i / (TailSegments - 1)) * scale;
                Vector3 p = position + right * sway + Vector3.up * (size * 0.5f + Wave(s) * 0.5f);
                _cubes.Add(Matrix4x4.TRS(p, Quaternion.LookRotation(forward), new Vector3(size * 1.6f, size, size)), main, Vector4.zero);
            }

            float tipS = _tailS - 1f - TailSegments * 0.9f;
            _space.Pose(tipS, out Vector3 tipPosition, out Vector3 tipForward, out Vector3 tipRight);
            float tipSway = Mathf.Sin(time * 4f - TailSegments * 0.9f) * scale * 0.3f;
            Vector3 tip = tipPosition + tipRight * tipSway + Vector3.up * scale * 0.25f;
            Quaternion spade = Quaternion.LookRotation(tipForward) * Quaternion.Euler(0f, 45f, 0f);
            _spines.Add(Matrix4x4.TRS(tip, spade * Quaternion.Euler(90f, 0f, 0f), new Vector3(scale * 0.9f, scale * 0.9f, scale * 0.5f)), horn, Vector4.zero);
        }

        private static Mesh BuildSpineMesh()
        {
            // Four-sided spike: square base at y=0, apex at y=1, pointing slightly back.
            var builder = new MeshBuilder();
            var apex = new Vector3(0f, 1f, -0.25f);
            var corners = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f)
            };
            var inside = new Vector3(0f, 0.3f, -0.05f);
            var poly = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                poly[0] = corners[i];
                poly[1] = corners[(i + 1) % 4];
                poly[2] = apex;
                builder.AddPolygon(poly, 3, inside, Color.white);
            }
            builder.AddPolygon(corners, 4, inside, Color.white);
            return builder.Build("Spine");
        }

        private Mesh Track(Mesh mesh)
        {
            _meshes.Add(mesh);
            return mesh;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                if (_meshes[i] != null)
                {
                    Destroy(_meshes[i]);
                }
            }
        }

        private struct Dying
        {
            public int Slice;
            public int Lane;
            public byte Color;
            public float Age;
            public Quaternion Rotation;
        }
    }
}
