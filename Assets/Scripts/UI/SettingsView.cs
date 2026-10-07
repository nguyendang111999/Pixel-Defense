using System;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>
    /// Settings / pause popup: sound, music and haptics switches, in-battle restart and home, and (development
    /// builds only) the autoplay bot and game speed.
    /// </summary>
    public sealed class SettingsView
    {
        private const string Shown = "overlay--shown";
        private const string On = "setting-row--on";

        private readonly VisualElement _overlay;
        private readonly Button _sound;
        private readonly Button _music;
        private readonly Button _haptics;
        private readonly VisualElement _battleActions;
        private readonly VisualElement _debug;
        private readonly Button _bot;
        private readonly Label _speed;

        public SettingsView(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("settingsOverlay");
            _sound = root.Q<Button>("soundToggle");
            _music = root.Q<Button>("musicToggle");
            _haptics = root.Q<Button>("hapticsToggle");
            _battleActions = root.Q<VisualElement>("settingsBattleActions");
            _debug = root.Q<VisualElement>("settingsDebug");
            _bot = root.Q<Button>("botToggle");
            _speed = root.Q<Label>("botSpeedValue");
            _bot.clicked += () => BotToggled?.Invoke();
            root.Q<Button>("botSpeed").clicked += () => SpeedClicked?.Invoke();
            root.Q<Button>("settingsClose").clicked += () => CloseClicked?.Invoke();
            root.Q<Button>("restartButton").clicked += () => RestartClicked?.Invoke();
            root.Q<Button>("quitButton").clicked += () => HomeClicked?.Invoke();
            _sound.clicked += () => SoundToggled?.Invoke();
            _music.clicked += () => MusicToggled?.Invoke();
            _haptics.clicked += () => HapticsToggled?.Invoke();
        }

        public event Action CloseClicked;
        public event Action RestartClicked;
        public event Action HomeClicked;
        public event Action SoundToggled;
        public event Action MusicToggled;
        public event Action HapticsToggled;
        public event Action BotToggled;
        public event Action SpeedClicked;

        public bool IsVisible => _overlay.ClassListContains(Shown);

        public void Show(bool inBattle)
        {
            _battleActions.EnableInClassList("hidden", !inBattle);
            _overlay.RemoveFromClassList(Shown);
            _overlay.schedule.Execute(() => _overlay.AddToClassList(Shown)).ExecuteLater(16);
        }

        public void Hide()
        {
            _overlay.RemoveFromClassList(Shown);
        }

        public void SetToggles(bool sound, bool music, bool haptics)
        {
            _sound.EnableInClassList(On, sound);
            _music.EnableInClassList(On, music);
            _haptics.EnableInClassList(On, haptics);
        }

        /// <summary>Shows the testing rows (autoplay bot, game speed); keep them out of release builds.</summary>
        public void ShowDebug(bool show)
        {
            _debug.EnableInClassList("hidden", !show);
        }

        public void SetBot(bool on, float speed)
        {
            _bot.EnableInClassList(On, on);
            _speed.text = "x" + speed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
