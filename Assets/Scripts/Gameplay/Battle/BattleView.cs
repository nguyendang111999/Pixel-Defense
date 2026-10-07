using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PixelDefense.Core;
using PixelDefense.Services.Audio;
using PixelDefense.Services.Haptics;
using PixelDefense.Services.Input;
using UnityEngine;

namespace PixelDefense.Gameplay
{
    public enum BattleResult
    {
        Won,
        Lost
    }

    /// <summary>
    /// Runs one level: owns the <see cref="Battle"/> model, builds the arena/dragon/cannon views, turns taps into
    /// deploys and turns every model event into visuals, sound and haptics. Exposes HUD-facing state and events.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        private const float IntroDuration = 1.7f;
        private const float HintDelay = 4f;
        private const float ComboWindow = 0.6f;
        private const int ComboPraiseStep = 25;

        [SerializeField] private VisualConfig _visuals;
        [SerializeField] private CameraRig _cameraRig;
        [SerializeField] private ToyLook _look;

        private readonly List<CannonView> _pool = new List<CannonView>(64);
        private readonly List<Vector3> _framePoints = new List<Vector3>(64);
        private AudioService _audio;
        private HapticService _haptics;
        private PointerInput _input;
        private BattleSettings _settings;
        private BattleSfx _sfx;
        private DebrisSystem _debris;
        private ProjectileSystem _projectiles;
        private FxSystem _fx;
        private CannonMeshes _cannonMeshes;
        private Battle _battle;
        private LevelDefinition _level;
        private ArenaSpace _space;
        private GameObject _world;
        private ArenaView _arena;
        private DragonView _dragon;
        private CannonView[] _byId;
        private CannonView[] _slotViews;
        private CancellationTokenSource _cts;
        private Rect _viewport;
        private bool _running;
        private bool _inputEnabled;
        private bool _tutorial;
        private float _idleTime;
        private int _hintColumn = -1;
        private int _combo;
        private float _comboTimer;
        private float _heartbeatTimer;
        private float _alarmTimer;
        private int _slicesCleared;
        private bool _touchedBase;
        private bool _healthDirty;
        private float _startDistance;
        private float _minDistance;

        public event Action<BattleResult> Finished;
        public event Action<string, Color> Praise;
        public event Action<bool> JamChanged;
        public event Action Deployed;

        public Battle Battle => _battle;
        public LevelDefinition Level => _level;
        public bool IsRunning => _running;
        public bool InputEnabled => _running && _inputEnabled;
        public VisualConfig Visuals => _visuals;
        public int HintColumn => _hintColumn;
        public bool IsTutorial => _tutorial;

        public float Progress => _battle == null ? 0f : 1f - _battle.Dragon.AliveFraction;

        /// <summary>0 when the dragon is far from the base, 1 when touching.</summary>
        public float Danger
        {
            get
            {
                if (_battle == null)
                {
                    return 0f;
                }
                return _battle.InContact ? 1f : Mathf.Clamp01(1f - _battle.DistanceToBase / (_settings.DangerZone * 1.8f));
            }
        }

        public void Init(AudioService audio, HapticService haptics, PointerInput input, BattleSettings settings)
        {
            _audio = audio;
            _haptics = haptics;
            _input = input;
            _settings = settings;
            _sfx = new BattleSfx(audio);
            _look.Init(_visuals);

            _debris = CreateSystem<DebrisSystem>("Debris");
            _debris.Init(_visuals);
            _projectiles = CreateSystem<ProjectileSystem>("Projectiles");
            _projectiles.Init(_visuals);
            _fx = CreateSystem<FxSystem>("Fx");
            _fx.Init(_visuals);
            _cannonMeshes = new CannonMeshes();
            _input.Pressed += OnPressed;
        }

        private T CreateSystem<T>(string systemName) where T : Component
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Pressed -= OnPressed;
            }
            Teardown();
            _cannonMeshes?.Dispose();
        }

        /// <summary>
        /// Builds the board for <paramref name="level"/> with the dragon parked at its start; nothing moves until
        /// <see cref="Begin"/>. Also used as the live background of the home screen.
        /// </summary>
        /// <param name="viewport">Viewport rect (0..1) the arena and columns must fit in, excluding HUD and safe areas.</param>
        public void Prepare(LevelDefinition level, Rect viewport)
        {
            Teardown();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _level = level;
            _viewport = viewport;
            _tutorial = string.Equals(level.Tutorial, "tap", StringComparison.Ordinal);
            _idleTime = 0f;
            _combo = 0;
            _slicesCleared = 0;
            _touchedBase = false;
            Time.timeScale = 1f;

            TrackShape track = TrackPresets.Create(level.Track, level.Width);
            _space = new ArenaSpace(track, _visuals, Vector3.zero);
            float contactFront = track.EndS - DragonView.HeadReach(_visuals);
            _battle = new Battle(level, _settings, contactFront);

            _world = new GameObject("BattleWorld");
            _world.transform.SetParent(transform, false);

            int maxDepth = 0;
            for (int c = 0; c < level.ColumnCount; c++)
            {
                maxDepth = Mathf.Max(maxDepth, level.ColumnLength(c));
            }

            _arena = new GameObject("Arena").AddComponent<ArenaView>();
            _arena.transform.SetParent(_world.transform, false);
            _arena.Build(_space, _visuals, level.Slots, level.ColumnCount, maxDepth);

            _dragon = new GameObject("Dragon").AddComponent<DragonView>();
            _dragon.transform.SetParent(_world.transform, false);
            _dragon.Build(_battle, _space, _visuals, _visuals.Skin(level.Skin), _debris);
            _projectiles.Bind(_battle, _dragon);
            _fx.AttachDragon(_dragon.Mouth, _dragon.Head);

            BuildCannons();
            _slotViews = new CannonView[_settings.MaxSlots + 1];
            Subscribe();
            FrameCamera();
            _startDistance = Mathf.Max(1f, _battle.DistanceToBase);
            _minDistance = _startDistance;
            _healthDirty = true;

            for (int c = 0; c < level.ColumnCount; c++)
            {
                LayoutColumn(c, intro: true);
            }
        }

        /// <summary>Plays the dragon's entrance (optionally slithering in from off-screen), then starts the battle.</summary>
        public async UniTask Begin(bool slitherIn, CancellationToken token)
        {
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token))
            {
                CancellationToken ct = linked.Token;
                if (slitherIn)
                {
                    _dragon.PlayIntro(IntroDuration);
                    _audio.Play(_sfx.Whoosh, 0.8f);
                    await UniTask.Delay(TimeSpan.FromSeconds(IntroDuration * 0.85f), cancellationToken: ct);
                }

                _dragon.Roar(0.9f);
                _audio.Play(_sfx.Roar);
                _cameraRig.AddTrauma(0.45f);
                _haptics.Play(HapticStyle.Heavy);
                await UniTask.Delay(TimeSpan.FromSeconds(0.45f), cancellationToken: ct);
            }

            _running = true;
            _inputEnabled = true;
            _audio.PlayMusic(true);
        }

        /// <summary>True once per change of the dragon's scales; the HUD refreshes its health bar then.</summary>
        public bool ConsumeHealthDirty()
        {
            bool dirty = _healthDirty;
            _healthDirty = false;
            return dirty;
        }

        public Color DragonColor => _visuals.Skin(_level.Skin).Main;

        /// <summary>
        /// Remaining body as runs of slices sharing a dominant color, head first, weighted by alive scales.
        /// </summary>
        public void GetHealthSegments(List<Color> colors, List<int> counts, out int total)
        {
            colors.Clear();
            counts.Clear();
            DragonBody body = _battle.Dragon;
            total = body.TotalCubes;
            int lastColor = -1;
            for (int s = 0; s < body.SliceCount; s++)
            {
                if (!body.IsSliceAlive(s))
                {
                    continue;
                }

                int alive = 0;
                int dominant = DominantColor(body, s, ref alive);
                if (dominant == lastColor)
                {
                    counts[counts.Count - 1] += alive;
                }
                else
                {
                    colors.Add(_visuals.ScaleColor((byte)dominant));
                    counts.Add(alive);
                    lastColor = dominant;
                }
            }
        }

        private static int DominantColor(DragonBody body, int slice, ref int alive)
        {
            int best = body.ColorAt(slice, body.Width / 2);
            int bestCount = 0;
            for (int lane = 0; lane < body.Width; lane++)
            {
                if (!body.IsAlive(slice, lane))
                {
                    continue;
                }

                alive++;
                byte color = body.ColorAt(slice, lane);
                int count = 0;
                for (int other = 0; other < body.Width; other++)
                {
                    if (body.IsAlive(slice, other) && body.ColorAt(slice, other) == color)
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = color;
                }
            }
            return best;
        }

        /// <summary>1-3 stars: 3 if the dragon never got past half its approach, 2 if it never touched the base.</summary>
        public int Stars
        {
            get
            {
                if (_touchedBase)
                {
                    return 1;
                }
                return _minDistance >= _startDistance * 0.5f ? 3 : 2;
            }
        }

        public void Teardown()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _running = false;
            _inputEnabled = false;
            Time.timeScale = 1f;
            Unsubscribe();
            _battle = null;
            _hintColumn = -1;

            if (_world != null)
            {
                Destroy(_world);
                _world = null;
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                {
                    _pool[i].Recycle();
                }
            }

            _projectiles?.Clear();
            _debris?.Clear();
            _fx?.StopAll();
            _cameraRig?.ResetFocus();
        }

        /// <summary>Re-fits the camera, e.g. after a resolution or safe-area change.</summary>
        public void Reframe(Rect viewport)
        {
            _viewport = viewport;
            if (_space != null)
            {
                FrameCamera();
            }
        }

        private void FrameCamera()
        {
            _framePoints.Clear();
            float radius = _space.ArenaRadiusWorld;
            float wall = _visuals.WallHeight * _space.Scale;
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2f / 24f;
                var p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                _framePoints.Add(p);
                _framePoints.Add(p + Vector3.up * wall);
            }

            int columns = _level.ColumnCount;
            float half = _visuals.ColumnSpacing * 0.5f * _space.Scale;
            float cannon = _visuals.CannonSize * _space.Scale;
            int rows = Mathf.Max(1, _visuals.VisibleRows);
            for (int c = 0; c < columns; c += Mathf.Max(1, columns - 1))
            {
                Vector3 front = _space.ColumnPosition(c, columns, 0);
                Vector3 back = _space.ColumnPosition(c, columns, rows - 1);
                float side = c == 0 ? -half : half;
                _framePoints.Add(front + new Vector3(side, cannon, cannon * 0.6f));
                _framePoints.Add(back + new Vector3(side, 0f, -cannon * 0.6f));
            }

            _cameraRig.Frame(_framePoints, _viewport);
        }

        private void BuildCannons()
        {
            _byId = new CannonView[_battle.CannonCount];
            while (_pool.Count < _byId.Length)
            {
                var go = new GameObject("Cannon");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<CannonView>();
                view.Create(_visuals, _cannonMeshes);
                go.SetActive(false);
                _pool.Add(view);
            }

            for (int id = 0; id < _byId.Length; id++)
            {
                CannonView view = _pool[id];
                view.Bind(id, _battle.GetCannon(id), _battle.IsRevealed(id));
                view.gameObject.SetActive(false);
                _byId[id] = view;
            }
        }

        private void LayoutColumn(int column, bool intro = false)
        {
            int head = _battle.ColumnHead(column);
            int length = _battle.ColumnLength(column);
            int visible = _visuals.VisibleRows + 1;
            for (int depth = head; depth < length; depth++)
            {
                int row = depth - head;
                CannonView view = _byId[_battle.CannonIdAt(column, depth)];
                if (row >= visible)
                {
                    view.Hide();
                    continue;
                }

                Vector3 target = _space.ColumnPosition(column, _level.ColumnCount, row);
                if (intro || !view.gameObject.activeSelf)
                {
                    view.PopIn(target, intro ? 0.35f + row * 0.07f + column * 0.05f : 0.12f + row * 0.03f);
                }
                else
                {
                    view.SlideTo(target, row * 0.035f);
                }
            }
        }

        private void Update()
        {
            if (!_running || _battle == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                _battle.Tick(dt);
            }

            if (_battle == null)
            {
                return;
            }

            _comboTimer -= dt;
            if (_comboTimer <= 0f)
            {
                _combo = 0;
            }

            _minDistance = Mathf.Min(_minDistance, _battle.DistanceToBase);
            _touchedBase |= _battle.InContact;

            UpdateAims();
            UpdateDanger(dt);
            UpdateHint(dt);
        }

        private void UpdateAims()
        {
            for (int i = 0; i < _battle.SlotCount; i++)
            {
                CannonView view = _slotViews[i];
                SlotState slot = _battle.GetSlot(i);
                if (view == null || view.State != CannonState.InSlot)
                {
                    continue;
                }

                if (slot.LastTargetSlice >= 0)
                {
                    view.Aim(_dragon.CubePosition(slot.LastTargetSlice, slot.LastTargetLane));
                }
                else if (_battle.Dragon.FrontSlice < _battle.Dragon.SliceCount)
                {
                    view.Aim(_dragon.CubePosition(_battle.Dragon.FrontSlice, _battle.Dragon.Width / 2));
                }
            }
        }

        private void UpdateDanger(float dt)
        {
            float danger = Danger;
            _arena.SetDanger(danger, _battle.InContact ? _battle.ContactProgress : 0f);

            if (_battle.InContact)
            {
                _alarmTimer -= dt;
                if (_alarmTimer <= 0f)
                {
                    _alarmTimer = 0.42f;
                    _audio.Play(_sfx.Warning, 1f + _battle.ContactProgress * 0.25f);
                    _haptics.Play(HapticStyle.Medium);
                    _cameraRig.AddTrauma(0.12f);
                    _arena.Shake(0.6f);
                }
                return;
            }

            if (danger > 0.35f)
            {
                _heartbeatTimer -= dt;
                if (_heartbeatTimer <= 0f)
                {
                    _heartbeatTimer = Mathf.Lerp(0.95f, 0.42f, danger);
                    _audio.Play(_sfx.Heartbeat, 1f, Mathf.Lerp(0.4f, 1f, danger));
                }
            }
        }

        private void UpdateHint(float dt)
        {
            _idleTime += dt;
            bool want = _inputEnabled && _battle.HasFreeSlot && !_battle.IsJammed && (_tutorial || _idleTime > HintDelay);
            int column = want ? BestColumn() : -1;
            if (column == _hintColumn)
            {
                return;
            }

            SetHintColumn(column);
        }

        private void SetHintColumn(int column)
        {
            if (_hintColumn >= 0 && _battle != null)
            {
                int old = _battle.FrontCannon(_hintColumn);
                if (old >= 0)
                {
                    _byId[old].SetHint(false);
                }
            }

            _hintColumn = column;
            if (column >= 0)
            {
                int id = _battle.FrontCannon(column);
                if (id >= 0)
                {
                    _byId[id].SetHint(true);
                }
            }
        }

        /// <summary>Column whose front cannon can fire immediately, preferring the most exposed matching scales.</summary>
        private int BestColumn()
        {
            int best = -1;
            int bestScore = 0;
            DragonBody body = _battle.Dragon;
            for (int c = 0; c < _battle.ColumnCount; c++)
            {
                int id = _battle.FrontCannon(c);
                if (id < 0)
                {
                    continue;
                }

                byte color = _battle.GetCannon(id).Color;
                int score = 0;
                for (int w = 0; w < body.WindowCount; w++)
                {
                    int slice = body.WindowSlice(w);
                    for (int lane = 0; lane < body.Width; lane++)
                    {
                        if (body.IsAlive(slice, lane) && body.ColorAt(slice, lane) == color)
                        {
                            score++;
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>Screen position of the hinted cannon for the tutorial hand.</summary>
        public bool TryGetHintScreenPoint(out Vector2 screen)
        {
            screen = default;
            if (_hintColumn < 0 || _battle == null)
            {
                return false;
            }

            int id = _battle.FrontCannon(_hintColumn);
            if (id < 0)
            {
                return false;
            }

            Vector3 world = _byId[id].transform.position + Vector3.up * (_visuals.CannonSize * _space.Scale * 0.5f);
            screen = _cameraRig.Camera.WorldToScreenPoint(world);
            return true;
        }

        private void OnPressed(Vector2 screen)
        {
            if (!InputEnabled)
            {
                return;
            }

            int column = HitColumn(screen);
            if (column < 0)
            {
                return;
            }

            _idleTime = 0f;
            if (!_tutorial)
            {
                SetHintColumn(-1);
            }

            DeployResult result = _battle.Deploy(column);
            if (result == DeployResult.SlotsFull)
            {
                int id = _battle.FrontCannon(column);
                if (id >= 0)
                {
                    _byId[id].Deny();
                }
                for (int i = 0; i < _battle.SlotCount; i++)
                {
                    _arena.PunchSlot(i, 0.6f);
                }
                _audio.Play(_sfx.Deny);
                _haptics.Play(HapticStyle.Failure);
                _cameraRig.AddTrauma(0.12f);
            }
        }

        /// <summary>Forgiving hit test: any press inside the tray, nearest column by screen x.</summary>
        private int HitColumn(Vector2 screen)
        {
            Camera camera = _cameraRig.Camera;
            int columns = _level.ColumnCount;
            Vector3 frontLeft = camera.WorldToScreenPoint(_space.ColumnPosition(0, columns, 0));
            Vector3 frontRight = camera.WorldToScreenPoint(_space.ColumnPosition(columns - 1, columns, 0));
            float spacing = columns > 1 ? Mathf.Abs(frontRight.x - frontLeft.x) / (columns - 1) : Screen.width * 0.25f;
            float cell = camera.WorldToScreenPoint(_space.ColumnPosition(0, columns, 0) + Vector3.forward * (_visuals.RowSpacing * _space.Scale)).y - frontLeft.y;
            float top = frontLeft.y + Mathf.Abs(cell) * 0.75f;
            if (screen.y > top)
            {
                return -1;
            }

            int best = -1;
            float bestDistance = spacing * 0.75f;
            for (int c = 0; c < columns; c++)
            {
                if (_battle.FrontCannon(c) < 0)
                {
                    continue;
                }

                float x = camera.WorldToScreenPoint(_space.ColumnPosition(c, columns, 0)).x;
                float distance = Mathf.Abs(screen.x - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = c;
                }
            }
            return best;
        }

        public bool UseFreeze()
        {
            return _running && _battle != null && _battle.UseFreeze();
        }

        public bool UseBomb()
        {
            if (!_running || _battle == null || _battle.Dragon.FrontSlice >= _battle.Dragon.SliceCount)
            {
                return false;
            }

            Vector3 target = _dragon.CubePosition(_battle.Dragon.FrontSlice, _battle.Dragon.Width / 2);
            _fx.Flash(target, new Color(1f, 0.6f, 0.2f) * 2f, 5f * _space.Scale, 0.3f);
            _fx.Sparks(target, new Color(1f, 0.7f, 0.3f), 24, 9f);
            _fx.Puff(target, new Color(0.3f, 0.28f, 0.3f, 0.8f), 10, 1.2f, 3f);
            _cameraRig.AddTrauma(0.55f);
            _cameraRig.Kick(2.5f);
            _audio.Play(_sfx.Bomb);
            _haptics.Play(HapticStyle.Heavy);
            return _battle.UseBomb() > 0;
        }

        public bool AddSlot()
        {
            return _running && _battle != null && _battle.AddSlot();
        }

        private void Subscribe()
        {
            _battle.CannonDeployed += OnCannonDeployed;
            _battle.CannonRevealed += OnCannonRevealed;
            _battle.CannonLanded += OnCannonLanded;
            _battle.ShotFired += OnShotFired;
            _battle.CubeDestroyed += OnCubeDestroyed;
            _battle.SliceCleared += OnSliceCleared;
            _battle.ColorCleared += OnColorCleared;
            _battle.CannonEmptied += OnCannonEmptied;
            _battle.CannonDismissed += OnCannonDismissed;
            _battle.SlotFreed += OnSlotFreed;
            _battle.SlotAdded += OnSlotAdded;
            _battle.ContactChanged += OnContactChanged;
            _battle.JamChanged += OnJamChanged;
            _battle.FreezeChanged += OnFreezeChanged;
            _battle.Enraged += OnEnraged;
            _battle.Won += OnWon;
            _battle.Lost += OnLost;
        }

        private void Unsubscribe()
        {
            if (_battle == null)
            {
                return;
            }

            _battle.CannonDeployed -= OnCannonDeployed;
            _battle.CannonRevealed -= OnCannonRevealed;
            _battle.CannonLanded -= OnCannonLanded;
            _battle.ShotFired -= OnShotFired;
            _battle.CubeDestroyed -= OnCubeDestroyed;
            _battle.SliceCleared -= OnSliceCleared;
            _battle.ColorCleared -= OnColorCleared;
            _battle.CannonEmptied -= OnCannonEmptied;
            _battle.CannonDismissed -= OnCannonDismissed;
            _battle.SlotFreed -= OnSlotFreed;
            _battle.SlotAdded -= OnSlotAdded;
            _battle.ContactChanged -= OnContactChanged;
            _battle.JamChanged -= OnJamChanged;
            _battle.FreezeChanged -= OnFreezeChanged;
            _battle.Enraged -= OnEnraged;
            _battle.Won -= OnWon;
            _battle.Lost -= OnLost;
        }

        private void OnCannonDeployed(int column, int slot, int cannonId)
        {
            CannonView view = _byId[cannonId];
            _slotViews[slot] = view;
            view.JumpTo(_arena.SlotWorldPosition(slot), _settings.DeployDuration, _cts.Token).Forget();
            _fx.Puff(view.transform.position, new Color(0.85f, 0.87f, 1f, 0.7f), 4, 0.35f);
            _audio.Play(_sfx.Tap);
            _audio.Play(_sfx.Jump, UnityEngine.Random.Range(0.95f, 1.1f), 0.8f);
            _haptics.Play(HapticStyle.Light);
            LayoutColumn(column);
            Deployed?.Invoke();
        }

        private void OnCannonRevealed(int column, int cannonId)
        {
            _byId[cannonId].Reveal();
            _audio.Play(_sfx.Reveal);
            _fx.Sparkle(_byId[cannonId].transform.position + Vector3.up * 0.3f, Color.white, 8, 0.3f, 0.25f);
        }

        private void OnCannonLanded(int slot)
        {
            Vector3 position = _arena.SlotWorldPosition(slot);
            _arena.PunchSlot(slot);
            _fx.Puff(position, new Color(0.9f, 0.92f, 1f, 0.75f), 6, 0.4f);
            _audio.Play(_sfx.Land, UnityEngine.Random.Range(0.9f, 1.05f));
            _haptics.Play(HapticStyle.Selection);
        }

        private void OnShotFired(Shot shot)
        {
            CannonView view = _slotViews[shot.Slot];
            if (view == null)
            {
                return;
            }

            view.Aim(_dragon.CubePosition(shot.Slice, shot.Lane));
            view.Fire();
            view.SetAmmo(_battle.GetSlot(shot.Slot).Ammo);
            Color color = _visuals.ScaleColor(shot.Color);
            Vector3 muzzle = view.MuzzlePosition;
            _projectiles.Launch(shot, muzzle, color);
            _fx.Flash(muzzle, color * 1.6f, 1.3f * _space.Scale, 0.09f);
            _audio.Play(_sfx.Shoot, UnityEngine.Random.Range(0.92f, 1.1f), 0.55f);
        }

        private void OnCubeDestroyed(int slice, int lane, byte color, int shotId)
        {
            if (shotId >= 0)
            {
                _projectiles.Land(shotId);
            }

            _dragon.OnCubeDestroyed(slice, lane, color, shotId < 0);
            _healthDirty = true;
            Vector3 position = _dragon.CubePosition(slice, lane);
            Color tint = _visuals.ScaleColor(color);
            _fx.Flash(position, tint * 1.5f + Color.white * 0.4f, 2.2f * _space.Scale, 0.13f);
            _fx.Sparks(position, tint, 3, 5f);

            _combo++;
            _comboTimer = ComboWindow;
            if (_combo % ComboPraiseStep == 0 && _battle.Dragon.AliveCubes > 0)
            {
                Praise?.Invoke("COMBO x" + _combo + "!", ComboColor(_combo));
            }
            float pitch = 1f + Mathf.Min(_combo, 36) * 0.018f;
            _audio.Play(_sfx.Pop, pitch, 0.75f);
            _haptics.Play(HapticStyle.Light);
        }

        private static Color ComboColor(int combo)
        {
            return combo >= 100 ? new Color(1f, 0.45f, 0.75f) : combo >= 50 ? new Color(0.55f, 0.85f, 1f) : new Color(1f, 0.82f, 0.23f);
        }

        private void OnSliceCleared(int slice)
        {
            _dragon.OnSliceCleared(slice);
            _cameraRig.AddTrauma(0.05f);
            _audio.Play(_sfx.Crunch, UnityEngine.Random.Range(0.9f, 1.1f), 0.45f);
            if (++_slicesCleared % 5 == 0)
            {
                _audio.Play(_sfx.Hurt, UnityEngine.Random.Range(0.9f, 1.15f), 0.6f);
            }
        }

        private void OnColorCleared(byte color)
        {
            if (_battle.Phase != BattlePhase.Playing || _battle.Dragon.AliveCubes == 0)
            {
                return;
            }

            Praise?.Invoke(ScaleColors.ToName(color).ToUpperInvariant() + " CLEARED!", _visuals.ScaleColor(color));
            _audio.Play(_sfx.Chime);
            _cameraRig.Kick(-1.2f);
            _haptics.Play(HapticStyle.Medium);
            if (_battle.Dragon.FrontSlice < _battle.Dragon.SliceCount)
            {
                _fx.Sparkle(_dragon.Head.position, _visuals.ScaleColor(color), 14, 0.8f, 0.35f);
            }
        }

        private void OnCannonEmptied(int slot)
        {
            ReleaseSlotView(slot, false);
        }

        private void OnCannonDismissed(int slot)
        {
            ReleaseSlotView(slot, true);
        }

        private void ReleaseSlotView(int slot, bool dismissed)
        {
            CannonView view = _slotViews[slot];
            _slotViews[slot] = null;
            if (view == null)
            {
                return;
            }

            _audio.Play(dismissed ? _sfx.Poof : _sfx.Empty, UnityEngine.Random.Range(0.95f, 1.08f));
            _fx.Sparkle(view.transform.position + Vector3.up * 0.25f, view.TintColor, 8, 0.25f, 0.3f);
            _fx.Puff(view.transform.position, new Color(1f, 1f, 1f, 0.7f), 5, 0.3f);
            view.Leave(dismissed, _cts.Token).Forget();
        }

        private void OnSlotFreed(int slot)
        {
            _arena.PunchSlot(slot, 0.5f);
        }

        private void OnSlotAdded(int slot)
        {
            _arena.SetSlotCount(_battle.SlotCount);
            for (int i = 0; i < _battle.SlotCount; i++)
            {
                CannonView view = _slotViews[i];
                if (view != null && view.State == CannonState.InSlot)
                {
                    view.SlideTo(_arena.SlotWorldPosition(i), 0f);
                }
            }
            _audio.Play(_sfx.SlotAdd);
            _fx.Sparkle(_arena.SlotWorldPosition(slot), Color.white, 12, 0.4f, 0.35f);
        }

        private void OnContactChanged(bool touching)
        {
            _dragon.SetAttacking(touching);
            _fx.SetFire(touching);
            if (touching)
            {
                _alarmTimer = 0f;
                _haptics.Play(HapticStyle.Heavy);
                _cameraRig.AddTrauma(0.3f);
            }
        }

        private void OnJamChanged(bool jammed)
        {
            JamChanged?.Invoke(jammed);
            if (jammed)
            {
                _audio.Play(_sfx.Deny, 0.8f);
                _haptics.Play(HapticStyle.Failure);
            }
        }

        private void OnFreezeChanged(bool frozen)
        {
            _dragon.SetFrozen(frozen);
            if (frozen)
            {
                _audio.Play(_sfx.Freeze);
                _fx.Sparkle(_dragon.Head.position, new Color(0.7f, 0.9f, 1f), 24, 1.2f, 0.4f);
            }
        }

        private void OnEnraged()
        {
            _dragon.SetEnraged();
            _fx.SetSmoke(true);
            _audio.Play(_sfx.Roar, 1.12f);
            _cameraRig.AddTrauma(0.35f);
            Praise?.Invoke("ENRAGED!", _visuals.DangerColor);
        }

        private void OnWon()
        {
            _inputEnabled = false;
            SetHintColumn(-1);
            RunWin(_cts.Token).Forget();
        }

        private void OnLost()
        {
            _inputEnabled = false;
            SetHintColumn(-1);
            RunLose(_cts.Token).Forget();
        }

        private async UniTaskVoid RunWin(CancellationToken token)
        {
            _fx.SetSmoke(false);
            _fx.SetFire(false);
            Vector3 head = _dragon.Head.position;
            Time.timeScale = 0.3f;
            _cameraRig.FocusOn(head, 1.18f);
            _cameraRig.Kick(-2.5f);
            _dragon.Roar(0.8f);
            _audio.Play(_sfx.Hurt, 0.8f);
            _haptics.Play(HapticStyle.Medium);
            await UniTask.Delay(TimeSpan.FromSeconds(0.65f), DelayType.UnscaledDeltaTime, cancellationToken: token);

            Time.timeScale = 1f;
            Vector3 center = _dragon.HeadCenter;
            _dragon.Explode();
            Praise?.Invoke("DRAGON SLAIN!", new Color(1f, 0.82f, 0.23f));
            _fx.Flash(center, new Color(1f, 0.95f, 0.8f) * 3f, 9f * _space.Scale, 0.32f);
            _fx.Sparks(center, new Color(1f, 0.8f, 0.4f), 40, 12f);
            _fx.Confetti(center + Vector3.up * 0.5f, 180, 11f);
            _cameraRig.AddTrauma(0.75f);
            _cameraRig.Kick(3.5f);
            _audio.Play(_sfx.Explosion);
            _haptics.Play(HapticStyle.Success);

            for (int i = 0; i < _battle.SlotCount; i++)
            {
                if (_slotViews[i] != null)
                {
                    ReleaseSlotView(i, false);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.4f), cancellationToken: token);
            _audio.Play(_sfx.Win);
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: token);
            _cameraRig.ResetFocus();
            _running = false;
            Finished?.Invoke(BattleResult.Won);
        }

        private async UniTaskVoid RunLose(CancellationToken token)
        {
            _fx.SetFire(true);
            _dragon.SetAttacking(true);
            _dragon.Roar(1.6f);
            _audio.Play(_sfx.Roar, 0.9f);
            _cameraRig.FocusOn(_space.Origin, 1.12f);
            _cameraRig.AddTrauma(0.5f);
            _arena.Shake(1.5f);
            _haptics.Play(HapticStyle.Failure);
            Time.timeScale = 0.6f;
            await UniTask.Delay(TimeSpan.FromSeconds(0.7f), DelayType.UnscaledDeltaTime, cancellationToken: token);

            Time.timeScale = 1f;
            Vector3 center = _space.Origin + Vector3.up * 0.3f;
            for (int i = 0; i < _battle.SlotCount; i++)
            {
                _debris.Burst(_arena.SlotWorldPosition(i), _visuals.PlatformColor, 8, 9f, _space.Scale * 0.6f);
                if (_slotViews[i] != null)
                {
                    ReleaseSlotView(i, true);
                }
            }
            _debris.Burst(center, _visuals.PlatformRimColor, 30, 10f, _space.Scale * 0.7f);
            _fx.Flash(center, new Color(1f, 0.5f, 0.2f) * 3f, 8f * _space.Scale, 0.35f);
            _fx.Puff(center, new Color(0.25f, 0.22f, 0.24f, 0.85f), 16, 1.4f, 3.5f);
            _cameraRig.AddTrauma(0.8f);
            _audio.Play(_sfx.Explosion, 0.85f);

            await UniTask.Delay(TimeSpan.FromSeconds(0.6f), cancellationToken: token);
            _fx.SetFire(false);
            _audio.Play(_sfx.Lose);
            await UniTask.Delay(TimeSpan.FromSeconds(0.9f), cancellationToken: token);
            _cameraRig.ResetFocus();
            _running = false;
            Finished?.Invoke(BattleResult.Lost);
        }
    }
}
