using System.Collections.Generic;
using PixelDefense.Services.Motion;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    /// <summary>
    /// Static arena geometry (floor, spiral walls, base platform, slot pads, cannon tray) plus base telegraphs:
    /// a proximity glow and the 2-second contact countdown ring.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        private const float PlatformHeight = 0.42f;
        private const float PadHeight = 0.12f;
        private const float FloorSize = 140f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        private readonly List<Pad> _pads = new List<Pad>();
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private VisualConfig _config;
        private ArenaSpace _space;
        private Transform _baseRoot;
        private MeshRenderer _dangerRenderer;
        private MeshRenderer _countdownRenderer;
        private MeshFilter _countdownFilter;
        private MaterialPropertyBlock _block;
        private MeshBuilder _ringBuilder;
        private Mesh _countdownMesh;
        private float _danger;
        private float _countdown;
        private float _shake;
        private int _slotCount;

        public Transform BaseRoot => _baseRoot;
        public float SlotSurfaceHeight => (PlatformHeight + PadHeight) * _config.WorldPerSlice;

        public void Build(ArenaSpace space, VisualConfig config, int slotCount, int columnCount, int maxColumnDepth)
        {
            _space = space;
            _config = config;
            _block = new MaterialPropertyBlock();
            _ringBuilder = new MeshBuilder();
            float s = config.WorldPerSlice;

            Transform root = transform;
            CreateRenderer("Floor", root, MeshFactory.GroundQuad(FloorSize), config.ToyMaterial, Vector3.down * 0.002f, config.FloorColor, castShadows: false);

            var walls = new MeshBuilder();
            var points = new Vector3[4096];
            foreach (float[] wall in space.Track.Walls)
            {
                int count = Mathf.Min(wall.Length / 2, points.Length);
                for (int i = 0; i < count; i++)
                {
                    points[i] = space.ToWorld(wall[i * 2], wall[i * 2 + 1]) - space.Origin;
                }
                MeshFactory.AddWall(walls, points, count, space.Track.WallWidth * s, config.WallHeight * s, 6, config.WallColor);
            }
            CreateRenderer("Walls", root, Track(walls.Build("Walls")), config.ToyMaterial, Vector3.zero, Color.white, castShadows: true);

            _baseRoot = new GameObject("Base").transform;
            _baseRoot.SetParent(root, false);
            _owned.Add(_baseRoot.gameObject);

            var platform = new MeshBuilder();
            float radius = space.Track.BaseRadius * s;
            MeshFactory.AddRoundedSlab(platform, Matrix4x4.identity, radius * 2f, radius * 2f, PlatformHeight * s, radius, 0.12f, 24,
                config.PlatformColor, Color.Lerp(config.PlatformColor, Color.black, 0.2f));
            MeshFactory.AddRoundedSlab(platform, Matrix4x4.Translate(Vector3.up * (PlatformHeight * s - 0.01f)), radius * 1.84f, radius * 1.84f, 0.035f, radius * 0.92f, 0.02f, 24,
                Color.Lerp(config.PlatformColor, config.PlatformRimColor, 0.35f), config.PlatformColor);
            CreateRenderer("Platform", _baseRoot, Track(platform.Build("Platform")), config.ToyMaterial, Vector3.zero, Color.white, castShadows: true);

            BuildTray(columnCount, maxColumnDepth);
            BuildTelegraphs();
            SetSlotCount(slotCount);
        }

        private void BuildTray(int columnCount, int maxDepth)
        {
            float s = _config.WorldPerSlice;
            Vector3 front = _space.ColumnPosition(0, 1, 0);
            float width = (columnCount * _config.ColumnSpacing + 1.4f) * s;
            float top = front.z + _config.CannonSize * 0.8f * s;
            const float depth = 40f;
            float rim = 0.35f * s * 2f;

            var tray = new MeshBuilder();
            MeshFactory.AddRoundedSlab(tray, Matrix4x4.identity, width + rim, depth + rim, 0.1f, 2.4f * s, 0.06f, 10,
                Color.Lerp(_config.TrayColor, _config.WallColor, 0.35f), Color.Lerp(_config.TrayColor, Color.black, 0.2f));
            MeshFactory.AddRoundedSlab(tray, Matrix4x4.Translate(Vector3.up * 0.06f), width, depth, 0.06f, 2.1f * s, 0.03f, 10,
                _config.TrayColor, _config.TrayColor);
            CreateRenderer("Tray", transform, Track(tray.Build("Tray")), _config.ToyMaterial,
                new Vector3(_space.Origin.x, 0f, top - depth * 0.5f), Color.white, castShadows: false);
        }

        private void BuildTelegraphs()
        {
            float s = _config.WorldPerSlice;
            float radius = _space.Track.BaseRadius;

            var ring = new MeshBuilder();
            MeshFactory.BuildRing(ring, (radius + 0.2f) * s, (radius + 2.6f) * s, 1f, 72, Color.white);
            _dangerRenderer = CreateRenderer("DangerGlow", _baseRoot, Track(ring.Build("DangerGlow")), _config.RingMaterial,
                Vector3.up * 0.02f, Color.clear, castShadows: false);

            _countdownMesh = Track(new Mesh { name = "Countdown" });
            _countdownMesh.MarkDynamic();
            _countdownRenderer = CreateRenderer("Countdown", _baseRoot, _countdownMesh, _config.RingMaterial,
                Vector3.up * 0.03f, Color.clear, castShadows: false);
            _countdownFilter = _countdownRenderer.GetComponent<MeshFilter>();
            _dangerRenderer.enabled = false;
            _countdownRenderer.enabled = false;
        }

        public void SetSlotCount(int count)
        {
            float s = _config.WorldPerSlice;
            for (int i = 0; i < count; i++)
            {
                Pad pad;
                if (i < _pads.Count)
                {
                    pad = _pads[i];
                }
                else
                {
                    var builder = new MeshBuilder();
                    pad = new Pad
                    {
                        Renderer = CreateRenderer("Slot" + i, _baseRoot, Track(new Mesh()), _config.ToyMaterial, Vector3.zero, Color.white, castShadows: false)
                    };
                    pad.Builder = builder;
                    _pads.Add(pad);
                }

                float size = _space.SlotSize(count) * s;
                pad.Builder.Clear();
                MeshFactory.AddRoundedSlab(pad.Builder, Matrix4x4.identity, size, size, PadHeight * s, size * 0.22f, 0.025f, 6,
                    _config.SlotColor, Color.Lerp(_config.SlotColor, Color.black, 0.3f));
                pad.Builder.Build("SlotPad", pad.Renderer.GetComponent<MeshFilter>().sharedMesh);
                Vector3 position = _space.SlotPosition(i, count, PlatformHeight * s) - _space.Origin;
                pad.Renderer.transform.localPosition = position;
                pad.Home = position;
                pad.Renderer.gameObject.SetActive(true);
            }

            if (count > _slotCount && _slotCount > 0)
            {
                PunchSlot(count - 1, 1.2f);
            }
            _slotCount = count;
        }

        public Vector3 SlotWorldPosition(int index)
        {
            return _space.SlotPosition(index, _slotCount, (PlatformHeight + PadHeight) * _config.WorldPerSlice);
        }

        public void PunchSlot(int index, float strength = 1f)
        {
            if (index < 0 || index >= _pads.Count)
            {
                return;
            }

            Pad pad = _pads[index];
            pad.Scale.Kick(-6f * strength);
            pad.Flash = Mathf.Max(pad.Flash, 0.65f * strength);
        }

        /// <param name="proximity">0 far .. 1 touching.</param>
        /// <param name="countdown">Fraction of the contact clock used (0..1).</param>
        public void SetDanger(float proximity, float countdown)
        {
            _danger = proximity;
            _countdown = countdown;
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Max(_shake, amount);
        }

        private void Update()
        {
            if (_config == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;
            for (int i = 0; i < _pads.Count; i++)
            {
                Pad pad = _pads[i];
                pad.Scale.Step(0f, 7f, 0.35f, dt);
                pad.Flash = Mathf.MoveTowards(pad.Flash, 0f, dt * 3.5f);
                float scale = 1f + pad.Scale.Value * 0.04f;
                Transform t = pad.Renderer.transform;
                t.localScale = new Vector3(scale, 1f, scale);
                _block.SetFloat(FlashId, pad.Flash);
                _block.SetColor(BaseColorId, Color.white);
                pad.Renderer.SetPropertyBlock(_block);
            }

            if (_shake > 0f)
            {
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.5f);
                float offset = _shake * 0.06f;
                _baseRoot.localPosition = new Vector3(Mathf.Sin(time * 61f) * offset, 0f, Mathf.Cos(time * 53f) * offset);
            }
            else
            {
                _baseRoot.localPosition = Vector3.zero;
            }

            bool showDanger = _danger > 0.01f;
            _dangerRenderer.enabled = showDanger;
            if (showDanger)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(time * Mathf.Lerp(4f, 14f, _danger));
                Color color = _config.DangerColor * (_danger * pulse * 1.6f);
                color.a = _danger * pulse;
                _block.Clear();
                _block.SetColor(BaseColorId, color);
                _dangerRenderer.SetPropertyBlock(_block);
            }

            bool showCountdown = _countdown > 0.001f;
            _countdownRenderer.enabled = showCountdown;
            if (showCountdown)
            {
                float s = _config.WorldPerSlice;
                float radius = _space.Track.BaseRadius;
                MeshFactory.BuildRing(_ringBuilder, (radius + 0.25f) * s, (radius + 1.25f) * s, 1f - _countdown, 72, Color.white);
                _ringBuilder.Build("Countdown", _countdownMesh);
                float flicker = 0.75f + 0.25f * Mathf.Sin(time * 40f);
                _block.Clear();
                _block.SetColor(BaseColorId, Color.Lerp(new Color(1f, 0.85f, 0.3f), _config.DangerColor, _countdown) * (2.2f * flicker));
                _countdownRenderer.SetPropertyBlock(_block);
            }
        }

        private MeshRenderer CreateRenderer(string objectName, Transform parent, Mesh mesh, Material material, Vector3 localPosition,
            Color color, bool castShadows)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (color != Color.white)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(block);
            }
            _owned.Add(go);
            return renderer;
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

        private sealed class Pad
        {
            public MeshRenderer Renderer;
            public MeshBuilder Builder;
            public FloatSpring Scale;
            public float Flash;
            public Vector3 Home;
        }
    }
}
