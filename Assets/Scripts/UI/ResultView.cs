using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>Win / lose popup with stars and a counting coin reward.</summary>
    public sealed class ResultView
    {
        private const string Shown = "overlay--shown";

        private readonly VisualElement _overlay;
        private readonly VisualElement _ribbon;
        private readonly Label _title;
        private readonly Label _message;
        private readonly VisualElement _stars;
        private readonly VisualElement[] _star = new VisualElement[3];
        private readonly VisualElement _reward;
        private readonly Label _rewardLabel;
        private readonly Button _primary;
        private readonly Button _secondary;
        private float _countFrom;
        private float _countTo;
        private float _countTime;
        private bool _counting;

        public ResultView(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("resultOverlay");
            _ribbon = root.Q<VisualElement>("resultRibbon");
            _title = root.Q<Label>("resultTitle");
            _message = root.Q<Label>("resultMessage");
            _stars = root.Q<VisualElement>("stars");
            for (int i = 0; i < _star.Length; i++)
            {
                _star[i] = root.Q<VisualElement>("star" + (i + 1));
            }
            _reward = root.Q<VisualElement>("reward");
            _rewardLabel = root.Q<Label>("rewardLabel");
            _primary = root.Q<Button>("resultPrimary");
            _secondary = root.Q<Button>("resultSecondary");
            _primary.clicked += () => PrimaryClicked?.Invoke();
            _secondary.clicked += () => SecondaryClicked?.Invoke();
        }

        public event Action PrimaryClicked;
        public event Action SecondaryClicked;

        public bool IsVisible => _overlay.ClassListContains(Shown);

        public void ShowWin(int stars, int reward, string message)
        {
            _title.text = "VICTORY!";
            _message.text = message;
            _ribbon.EnableInClassList("panel__ribbon--red", false);
            _stars.EnableInClassList("stars--hidden", false);
            _reward.EnableInClassList("reward--hidden", false);
            _primary.text = "NEXT";
            _secondary.text = "HOME";
            _rewardLabel.text = "+0";
            for (int i = 0; i < _star.Length; i++)
            {
                VisualElement star = _star[i];
                star.RemoveFromClassList("star--earned");
                if (i < stars)
                {
                    star.schedule.Execute(() => star.AddToClassList("star--earned")).ExecuteLater(350 + i * 260);
                }
            }

            _countFrom = 0f;
            _countTo = reward;
            _countTime = -0.6f;
            _counting = true;
            Show();
        }

        public void ShowLose(string message)
        {
            _title.text = "BASE LOST!";
            _message.text = message;
            _ribbon.EnableInClassList("panel__ribbon--red", true);
            _stars.EnableInClassList("stars--hidden", true);
            _reward.EnableInClassList("reward--hidden", true);
            _primary.text = "TRY AGAIN";
            _secondary.text = "HOME";
            _counting = false;
            Show();
        }

        private void Show()
        {
            _overlay.RemoveFromClassList(Shown);
            _overlay.schedule.Execute(() => _overlay.AddToClassList(Shown)).ExecuteLater(16);
        }

        public void Hide()
        {
            _overlay.RemoveFromClassList(Shown);
            _counting = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_counting)
            {
                return;
            }

            _countTime += deltaTime;
            float t = Mathf.Clamp01(_countTime / 0.8f);
            int value = Mathf.RoundToInt(Mathf.Lerp(_countFrom, _countTo, 1f - (1f - t) * (1f - t)));
            _rewardLabel.text = "+" + value;
            if (t >= 1f)
            {
                _counting = false;
            }
        }
    }
}
