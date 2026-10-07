using System;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>Settings / pause popup: sound, music and haptics switches plus in-battle restart and home.</summary>
    public sealed class SettingsView
    {
        private const string Shown = "overlay--shown";
        private const string On = "setting-row--on";

        private readonly VisualElement _overlay;
        private readonly Button _sound;
        private readonly Button _music;
        private readonly Button _haptics;
        private readonly VisualElement _battleActions;

        public SettingsView(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("settingsOverlay");
            _sound = root.Q<Button>("soundToggle");
            _music = root.Q<Button>("musicToggle");
            _haptics = root.Q<Button>("hapticsToggle");
            _battleActions = root.Q<VisualElement>("settingsBattleActions");
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
    }
}
