using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using PixelDefense.Core;
using PixelDefense.Gameplay;
using PixelDefense.Services.Audio;
using PixelDefense.Services.Haptics;
using PixelDefense.Services.Input;
using PixelDefense.Services.Save;
using PixelDefense.Services.Settings;
using PixelDefense.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelDefense.App
{
    /// <summary>
    /// Composition root and game state machine: Home → Playing → Result. Wires services, battle and UI, owns
    /// progress, the booster economy and tutorial prompts. In the Editor and development builds it also runs the
    /// autoplay bot (pause menu, or B / N keys) which plays on through the campaign and logs each result.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        private const string SaveName = "progress";
        private const float TutorialFollowUpSeconds = 4.5f;

        [SerializeField] private GameConfig _config;
        [SerializeField] private BattleView _battle;
        [SerializeField] private GameUi _ui;
        [SerializeField] private AudioService _audio;
        [SerializeField] private PointerInput _input;

        private readonly List<Color> _healthColors = new List<Color>();
        private readonly List<int> _healthCounts = new List<int>();
        private LevelPack _pack;
        private ProgressData _progress;
        private JsonFileStore _store;
        private SettingsStore _settings;
        private HapticService _haptics;
        private State _state;
        private bool _paused;
        private int _levelIndex;
        private int _uiClick;
        private int _deny;
        private int _coinSound;
        private Vector2Int _screen;
        private float _tutorialTimer;
        private bool _tutorialDeployed;
        private bool _canPick;
        private bool _bot;
        private int _botSpeed;
        private int _botLosses;
        private CancellationTokenSource _botNext;

        private enum State
        {
            Home,
            Playing,
            Result
        }

        private LevelDefinition CurrentLevel => _pack.Levels[_levelIndex];

        private void Awake()
        {
            Application.targetFrameRate = GameConfig.TargetFrameRate;
            _pack = LevelParser.Parse(_config.LevelPack.text, _config.LevelPack.name);
            for (int i = 0; i < _pack.Issues.Count; i++)
            {
                LevelIssue issue = _pack.Issues[i];
                if (issue.IsError)
                {
                    Debug.LogError(issue.ToString());
                }
                else
                {
                    Debug.LogWarning(issue.ToString());
                }
            }

            _store = new JsonFileStore();
            _settings = new SettingsStore();
            _haptics = new HapticService();
            LoadProgress();

            _audio.Init(_config.Audio);
            _uiClick = _audio.Resolve("ui");
            _deny = _audio.Resolve("deny");
            _coinSound = _audio.Resolve("coin");
            _battle.Init(_audio, _haptics, _input, _config.Battle);
            _ui.Init();
            _input.BlockedBy = _ui.IsOverInteractive;
            ApplySettings();
            Wire();
        }

        private void Start()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            EnterHome();
        }

        private void Wire()
        {
            _ui.ButtonClicked += () => _audio.Play(_uiClick);
            _ui.Home.PlayClicked += OnPlay;
            _ui.Home.SettingsClicked += () => OpenSettings(false);
            _ui.Hud.PauseButton.clicked += () => OpenSettings(true);
            _ui.Hud.BoosterClicked += OnBooster;
            _ui.Result.PrimaryClicked += OnResultPrimary;
            _ui.Result.SecondaryClicked += EnterHome;
            _ui.Settings.CloseClicked += CloseSettings;
            _ui.Settings.RestartClicked += () => StartLevel(true);
            _ui.Settings.HomeClicked += EnterHome;
            _ui.Settings.SoundToggled += () => { _settings.Sound = !_settings.Sound; ApplySettings(); };
            _ui.Settings.MusicToggled += () => { _settings.Music = !_settings.Music; ApplySettings(); };
            _ui.Settings.HapticsToggled += () => { _settings.Haptics = !_settings.Haptics; ApplySettings(); };
            _ui.Hud.PickCancelClicked += () => _battle.CancelPick();

            _battle.Finished += OnBattleFinished;
            _battle.Praise += (text, color) => _ui.Hud.ShowPraise(text, color);
            _battle.JamChanged += jammed => _ui.Hud.SetJammed(jammed);
            _battle.Deployed += OnDeployed;
            _battle.PickModeChanged += picking => _ui.Hud.SetPicking(picking);
            _battle.PickPlaced += OnPickPlaced;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _ui.Settings.ShowDebug(true);
            _ui.Settings.BotToggled += ToggleBot;
            _ui.Settings.SpeedClicked += CycleBotSpeed;
            RefreshBotUi();
#else
            _ui.Settings.ShowDebug(false);
#endif
        }

        private void LoadProgress()
        {
            if (!_store.TryLoad(SaveName, out _progress) || _progress.Version > ProgressData.CurrentVersion)
            {
                _progress = new ProgressData
                {
                    Coins = _config.StartCoins,
                    Freeze = _config.StartFreeze,
                    Bomb = _config.StartBomb,
                    Slot = _config.StartSlot,
                    Pick = _config.StartPick
                };
            }
            else if (_progress.Version < 2)
            {
                // Saves from before the pick booster start with the same free picks as a new player.
                _progress.Pick = _config.StartPick;
                _progress.Version = ProgressData.CurrentVersion;
            }

            _levelIndex = Mathf.Max(0, string.IsNullOrEmpty(_progress.CurrentLevelId) ? 0 : _pack.IndexOf(_progress.CurrentLevelId));
            _progress.CurrentLevelId = CurrentLevel.Id;
        }

        private void SaveProgress()
        {
            _store.Save(SaveName, _progress);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _progress != null)
            {
                SaveProgress();
            }
        }

        private void ApplySettings()
        {
            _audio.SetSoundEnabled(_settings.Sound);
            _audio.SetMusicEnabled(_settings.Music);
            _haptics.Enabled = _settings.Haptics;
            _ui.Settings.SetToggles(_settings.Sound, _settings.Music, _settings.Haptics);
        }

        private Rect Viewport()
        {
            float top = _ui.TopInsetFraction;
            float bottom = _ui.BottomInsetFraction;
            return new Rect(0.025f, bottom, 0.95f, Mathf.Max(0.3f, 1f - top - bottom));
        }

        private void EnterHome()
        {
            CancelBotNext();
            Time.timeScale = _battle.SpeedMultiplier;
            _paused = false;
            _state = State.Home;
            _ui.Result.Hide();
            _ui.Settings.Hide();
            _ui.ShowHud(false);
            _ui.ShowHome(true);
            _ui.Hud.SetHand(false, Vector2.zero);
            _ui.Hud.SetTutorial(null);
            _ui.Home.SetLevel(_progress.LevelNumber, IsBoss(CurrentLevel));
            _battle.Prepare(CurrentLevel, Viewport());
            _audio.PlayMusic(true);
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        private void OnPlay()
        {
            if (_state != State.Home)
            {
                return;
            }

            _ui.ShowHome(false);
            EnterPlaying();
            _battle.Begin(false, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void StartLevel(bool slitherIn)
        {
            CancelBotNext();
            Time.timeScale = _battle.SpeedMultiplier;
            _paused = false;
            _ui.Result.Hide();
            _ui.Settings.Hide();
            _ui.ShowHome(false);
            _battle.Prepare(CurrentLevel, Viewport());
            EnterPlaying();
            _battle.Begin(slitherIn, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void EnterPlaying()
        {
            _state = State.Playing;
            _tutorialDeployed = false;
            _tutorialTimer = 0f;
            _ui.ShowHud(true);
            _ui.Hud.SetLevel(_progress.LevelNumber, _bot ? "BOT" : null);
            _ui.Hud.SetCoins(_progress.Coins);
            _ui.Hud.SetDragonColor(_battle.DragonColor);
            _ui.Hud.SetJammed(false);
            _ui.Hud.SetPicking(false);
            _canPick = true;
            RefreshBoosters();
            ShowTutorialIntro();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void ShowTutorialIntro()
        {
            switch (CurrentLevel.Tutorial)
            {
                case "tap":
                    _ui.Hud.SetTutorial("Tap a cannon to send it to your base!");
                    break;
                case "neck":
                    ShowTimedTutorial("Cannons can only hit the dragon's FRONT scales. Match the colors in order!");
                    break;
                case "slots":
                    ShowTimedTutorial("Only 5 slots! Wrong colors wait there and can jam your base.");
                    break;
                case "hidden":
                    ShowTimedTutorial("Mystery cannons reveal their color when they reach the front.");
                    break;
                case "boosters":
                    ShowTimedTutorial("In trouble? Freeze the dragon, bomb its neck or add a slot!");
                    break;
                default:
                    _ui.Hud.SetTutorial(null);
                    break;
            }
        }

        private void ShowTimedTutorial(string text)
        {
            _ui.Hud.SetTutorial(text);
            _tutorialTimer = TutorialFollowUpSeconds;
        }

        private void OnDeployed()
        {
            if (CurrentLevel.Tutorial == "tap" && !_tutorialDeployed)
            {
                _tutorialDeployed = true;
                ShowTimedTutorial("It blasts the dragon's front scales of its own color!");
            }
        }

        private void OnBattleFinished(BattleResult result)
        {
            LevelDefinition finished = CurrentLevel;
            _state = State.Result;
            _ui.Hud.SetHand(false, Vector2.zero);
            _ui.Hud.SetTutorial(null);
            _ui.Hud.SetJammed(false);
            _ui.Hud.SetDanger(0f, false);
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            if (result == BattleResult.Won)
            {
                int stars = _battle.Stars;
                int reward = _config.BaseReward + _config.RewardPerStar * stars + _progress.LevelNumber / 2;
                _progress.Coins += reward;
                _progress.Record(CurrentLevel.Id, stars);
                _progress.LevelNumber++;
                AdvanceLevel();
                SaveProgress();
                _ui.Hud.SetCoins(_progress.Coins);
                _ui.Result.ShowWin(stars, reward, WinMessage(stars));
                _audio.Play(_coinSound);
            }
            else
            {
                SaveProgress();
                _ui.Result.ShowLose("The dragon reached your base. Mind the color order on its neck!");
            }

            if (_bot)
            {
                BotAfterResult(finished, result);
            }
        }

        private static string WinMessage(int stars)
        {
            return stars >= 3 ? "Flawless! The base never felt a thing." : stars == 2 ? "Dragon slain! That was close." : "Phew! You held the line.";
        }

        private void AdvanceLevel()
        {
            int next = _levelIndex + 1;
            if (next >= _pack.Levels.Count)
            {
                int loop = Mathf.Clamp(_config.LoopLastLevels, 1, _pack.Levels.Count);
                next = _pack.Levels.Count - loop + (_progress.LevelNumber % loop);
            }
            _levelIndex = next;
            _progress.CurrentLevelId = CurrentLevel.Id;
        }

        private void OnResultPrimary()
        {
            StartLevel(true);
        }

        private void OpenSettings(bool inBattle)
        {
            if (_ui.Settings.IsVisible)
            {
                return;
            }

            if (inBattle && _state == State.Playing)
            {
                _paused = true;
                Time.timeScale = 0f;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RefreshBotUi();
#endif
            _ui.Settings.Show(inBattle && _state == State.Playing);
        }

        private void CloseSettings()
        {
            _ui.Settings.Hide();
            if (_paused)
            {
                _paused = false;
                Time.timeScale = _battle.SpeedMultiplier;
            }

            // The bot holds on the result screen while the menu is open; move on once it closes.
            if (_bot && _state == State.Result)
            {
                ContinueBot(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        private void OnBooster(BoosterKind kind)
        {
            if (_state != State.Playing || _paused || !_battle.InputEnabled)
            {
                return;
            }

            if (kind == BoosterKind.Pick && _battle.IsPicking)
            {
                _battle.CancelPick();
                return;
            }

            ref int count = ref BoosterCount(kind);
            int price = BoosterPrice(kind);
            bool owned = count > 0;
            if (!owned && _progress.Coins < price)
            {
                _audio.Play(_deny);
                _haptics.Play(HapticStyle.Failure);
                _ui.Hud.PunchBooster(kind);
                return;
            }

            if (kind == BoosterKind.Pick)
            {
                // Paid only once a cannon is actually placed (see OnPickPlaced); cancelling is free.
                if (!_battle.BeginPick())
                {
                    _audio.Play(_deny);
                }
                _ui.Hud.PunchBooster(kind);
                return;
            }

            bool used = kind == BoosterKind.Freeze ? _battle.UseFreeze()
                : kind == BoosterKind.Bomb ? _battle.UseBomb()
                : _battle.AddSlot();
            if (!used)
            {
                _audio.Play(_deny);
                _ui.Hud.PunchBooster(kind);
                return;
            }

            Spend(kind);
        }

        private void OnPickPlaced()
        {
            Spend(BoosterKind.Pick);
        }

        private void Spend(BoosterKind kind)
        {
            ref int count = ref BoosterCount(kind);
            if (count > 0)
            {
                count--;
            }
            else
            {
                _progress.Coins = Mathf.Max(0, _progress.Coins - BoosterPrice(kind));
                _ui.Hud.SetCoins(_progress.Coins);
            }

            _ui.Hud.PunchBooster(kind);
            SaveProgress();
            RefreshBoosters();
        }

        private ref int BoosterCount(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Freeze:
                    return ref _progress.Freeze;
                case BoosterKind.Bomb:
                    return ref _progress.Bomb;
                case BoosterKind.Pick:
                    return ref _progress.Pick;
                default:
                    return ref _progress.Slot;
            }
        }

        private int BoosterPrice(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Freeze:
                    return _config.FreezePrice;
                case BoosterKind.Bomb:
                    return _config.BombPrice;
                case BoosterKind.Pick:
                    return _config.PickPrice;
                default:
                    return _config.SlotPrice;
            }
        }

        private void RefreshBoosters()
        {
            bool canAddSlot = _battle.Battle == null || _battle.Battle.SlotCount < _config.Battle.MaxSlots;
            _ui.Hud.SetBooster(BoosterKind.Freeze, _progress.Freeze, _config.FreezePrice, true);
            _ui.Hud.SetBooster(BoosterKind.Bomb, _progress.Bomb, _config.BombPrice, true);
            _ui.Hud.SetBooster(BoosterKind.Slot, _progress.Slot, _config.SlotPrice, canAddSlot);
            _ui.Hud.SetBooster(BoosterKind.Pick, _progress.Pick, _config.PickPrice, _canPick);
        }

        /// <summary>The pick booster needs a free slot and a cannon left in the tray; its button dims otherwise.</summary>
        private void UpdatePickAvailability()
        {
            Battle battle = _battle.Battle;
            bool canPick = _battle.IsPicking || (battle.HasFreeSlot && battle.DeployedCount < battle.CannonCount);
            if (canPick != _canPick)
            {
                _canPick = canPick;
                _ui.Hud.SetBooster(BoosterKind.Pick, _progress.Pick, _config.PickPrice, _canPick);
            }
        }

        // ---------- Autoplay bot (testing) ----------

        private void ToggleBot()
        {
            _bot = !_bot;
            _botLosses = 0;
            _battle.AutoPlay = _bot;
            if (!_bot)
            {
                CancelBotNext();
            }
            else if (_state == State.Result)
            {
                ContinueBot(this.GetCancellationTokenOnDestroy()).Forget();
            }

            if (_state == State.Playing)
            {
                _ui.Hud.SetLevel(_progress.LevelNumber, _bot ? "BOT" : null);
            }
            RefreshBotUi();
            Debug.Log("[Bot] Autoplay " + (_bot ? "ON" : "OFF"));
        }

        private void CycleBotSpeed()
        {
            float[] speeds = _config.BotSpeeds;
            if (speeds == null || speeds.Length == 0)
            {
                return;
            }

            _botSpeed = (_botSpeed + 1) % speeds.Length;
            _battle.SpeedMultiplier = speeds[_botSpeed];
            if (_paused)
            {
                Time.timeScale = 0f;
            }
            RefreshBotUi();
        }

        private void RefreshBotUi()
        {
            _ui.Settings.SetBot(_bot, _battle.SpeedMultiplier);
        }

        private void BotAfterResult(LevelDefinition level, BattleResult result)
        {
            Battle battle = _battle.Battle;
            string time = battle != null ? battle.Time.ToString("F1", CultureInfo.InvariantCulture) + "s" : "?";
            string margin = _battle.MinDistance.ToString("F1", CultureInfo.InvariantCulture);
            if (result == BattleResult.Won)
            {
                _botLosses = 0;
                Debug.Log("[Bot] " + level.Id + " " + level.Name + ": WON " + _battle.Stars + " stars in " + time +
                          " (closest approach " + margin + " slices)");
            }
            else
            {
                _botLosses++;
                Debug.LogWarning("[Bot] " + level.Id + " " + level.Name + ": LOST after " + time + " (attempt " + _botLosses + ")");
                if (_botLosses >= Mathf.Max(1, _config.BotRetries))
                {
                    Debug.LogWarning("[Bot] Skipping " + level.Id + " after " + _botLosses + " losses.");
                    _botLosses = 0;
                    _progress.LevelNumber++;
                    AdvanceLevel();
                    SaveProgress();
                }
            }

            ContinueBot(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ContinueBot(CancellationToken destroyed)
        {
            CancelBotNext();
            _botNext = CancellationTokenSource.CreateLinkedTokenSource(destroyed);
            CancellationToken token = _botNext.Token;
            await UniTask.Delay(TimeSpan.FromSeconds(_config.BotResultPause), DelayType.UnscaledDeltaTime, cancellationToken: token)
                .SuppressCancellationThrow();
            if (token.IsCancellationRequested || !_bot || _state != State.Result || _ui.Settings.IsVisible)
            {
                return;
            }

            StartLevel(true);
        }

        private void CancelBotNext()
        {
            if (_botNext == null)
            {
                return;
            }

            _botNext.Cancel();
            _botNext.Dispose();
            _botNext = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Debug: start a level by pack index immediately (used from the Editor console / CLI).</summary>
        public void DebugPlayLevel(int index)
        {
            _levelIndex = Mathf.Clamp(index, 0, _pack.Levels.Count - 1);
            _progress.LevelNumber = _levelIndex + 1;
            _progress.CurrentLevelId = CurrentLevel.Id;
            StartLevel(true);
        }

        /// <summary>Debug: same as the pause-menu bot switch (menu Pixel Defense ▸ Bot).</summary>
        public void DebugToggleBot()
        {
            ToggleBot();
        }
#endif

        private static bool IsBoss(LevelDefinition level)
        {
            return level.Name != null && level.Name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Update()
        {
            HandleBackButton();
            HandleResize();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleBotKeys();
#endif

            if (_state != State.Playing || _battle.Battle == null)
            {
                return;
            }

            UpdatePickAvailability();

            _ui.Hud.SetDanger(_battle.Danger, _battle.Battle.InContact);
            if (_battle.ConsumeHealthDirty())
            {
                _battle.GetHealthSegments(_healthColors, _healthCounts, out int total);
                _ui.Hud.SetHealth(_healthColors, _healthCounts, total);
            }

            Vector2 screen = Vector2.zero;
            bool showHand = !_paused && _battle.TryGetHintScreenPoint(out screen);
            _ui.Hud.SetHand(showHand, showHand ? _ui.ToPanel(screen) : Vector2.zero);

            if (_tutorialTimer > 0f)
            {
                _tutorialTimer -= Time.unscaledDeltaTime;
                if (_tutorialTimer <= 0f)
                {
                    _ui.Hud.SetTutorial(null);
                }
            }
        }

        private void HandleBackButton()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (_ui.Settings.IsVisible)
            {
                CloseSettings();
            }
            else if (_battle.IsPicking)
            {
                _battle.CancelPick();
            }
            else if (_state == State.Playing)
            {
                OpenSettings(true);
            }
            else if (_state == State.Result)
            {
                EnterHome();
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>B toggles the autoplay bot, N cycles the game speed.</summary>
        private void HandleBotKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                ToggleBot();
            }

            if (keyboard.nKey.wasPressedThisFrame)
            {
                CycleBotSpeed();
            }
        }
#endif

        private void HandleResize()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == _screen)
            {
                return;
            }

            _screen = screen;
            _battle.Reframe(Viewport());
        }
    }
}
