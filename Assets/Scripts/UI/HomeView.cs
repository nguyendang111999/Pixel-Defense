using System;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>Title screen: logo, current level and the play button.</summary>
    public sealed class HomeView
    {
        private readonly Label _level;

        public HomeView(VisualElement root)
        {
            Root = root.Q<VisualElement>("home");
            _level = root.Q<Label>("homeLevel");
            PlayButton = root.Q<Button>("playButton");
            SettingsButton = root.Q<Button>("homeSettingsButton");
            PlayButton.clicked += () => PlayClicked?.Invoke();
            SettingsButton.clicked += () => SettingsClicked?.Invoke();
        }

        public event Action PlayClicked;
        public event Action SettingsClicked;

        public VisualElement Root { get; }
        public Button PlayButton { get; }
        public Button SettingsButton { get; }

        public void SetLevel(int number, bool boss)
        {
            _level.text = boss ? "LEVEL " + number + "  ·  BOSS" : "LEVEL " + number;
            _level.EnableInClassList("home__level--boss", boss);
        }
    }
}
