using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    public enum BoosterKind
    {
        Freeze,
        Bomb,
        Slot
    }

    /// <summary>Battle HUD: level, coins, dragon health bar, boosters, praise text, jam banner, danger vignette, tutorial hand.</summary>
    public sealed class HudView
    {
        private const string PraiseShow = "praise--show";
        private const string PraiseHide = "praise--hide";
        private const string HiddenModifier = "--hidden";

        private readonly Label _level;
        private readonly Label _coins;
        private readonly DragonHealthBar _health;
        private readonly IconElement _dragonIcon;
        private readonly Label _praise;
        private readonly VisualElement _jam;
        private readonly VisualElement _tutorial;
        private readonly Label _tutorialText;
        private readonly VisualElement _hand;
        private readonly VisualElement _handRing;
        private readonly VisualElement _vignette;
        private readonly Booster[] _boosters = new Booster[3];
        private float _praiseTimer;
        private float _handTime;
        private bool _handVisible;
        private Vector2 _handTarget;

        public HudView(VisualElement root)
        {
            Root = root.Q<VisualElement>("hud");
            _level = root.Q<Label>("levelLabel");
            _coins = root.Q<Label>("coinLabel");
            _health = root.Q<DragonHealthBar>("healthBar");
            _dragonIcon = root.Q<IconElement>("dragonIcon");
            _praise = root.Q<Label>("praise");
            _jam = root.Q<VisualElement>("jamBanner");
            _tutorial = root.Q<VisualElement>("tutorial");
            _tutorialText = root.Q<Label>("tutorialText");
            _hand = root.Q<VisualElement>("hand");
            _handRing = root.Q<VisualElement>("handRing");
            _vignette = root.Q<VisualElement>("vignette");
            PauseButton = root.Q<Button>("pauseButton");

            _boosters[(int)BoosterKind.Freeze] = new Booster(root, "freeze");
            _boosters[(int)BoosterKind.Bomb] = new Booster(root, "bomb");
            _boosters[(int)BoosterKind.Slot] = new Booster(root, "slot");
            for (int i = 0; i < _boosters.Length; i++)
            {
                var kind = (BoosterKind)i;
                _boosters[i].Button.clicked += () => BoosterClicked?.Invoke(kind);
            }
        }

        public event Action<BoosterKind> BoosterClicked;

        public VisualElement Root { get; }
        public Button PauseButton { get; }

        public void SetLevel(int number)
        {
            _level.text = "LEVEL " + number;
        }

        public void SetCoins(int coins)
        {
            _coins.text = coins.ToString();
        }

        public void SetDragonColor(Color color)
        {
            _dragonIcon.Tint = color;
        }

        public void SetHealth(IReadOnlyList<Color> colors, IReadOnlyList<int> counts, int total)
        {
            _health.SetSegments(colors, counts, total);
        }

        public void HitHealth()
        {
            _health.Shake();
        }

        /// <param name="price">Shown instead of the count when the player has none left.</param>
        public void SetBooster(BoosterKind kind, int count, int price, bool enabled)
        {
            _boosters[(int)kind].Set(count, price, enabled);
        }

        public void PunchBooster(BoosterKind kind)
        {
            Booster booster = _boosters[(int)kind];
            booster.Button.RemoveFromClassList("booster--punch");
            booster.Button.schedule.Execute(() => booster.Button.AddToClassList("booster--punch")).ExecuteLater(10);
            booster.Button.schedule.Execute(() => booster.Button.RemoveFromClassList("booster--punch")).ExecuteLater(160);
        }

        public void ShowPraise(string text, Color color)
        {
            _praise.text = text;
            _praise.style.color = color;
            _praise.RemoveFromClassList(PraiseShow);
            _praise.RemoveFromClassList(PraiseHide);
            _praise.schedule.Execute(() => _praise.AddToClassList(PraiseShow)).ExecuteLater(16);
            _praiseTimer = 1.1f;
        }

        public void SetJammed(bool jammed)
        {
            _jam.EnableInClassList("jam-banner" + HiddenModifier, !jammed);
        }

        public void SetTutorial(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (show)
            {
                _tutorialText.text = text;
            }
            _tutorial.EnableInClassList("tutorial" + HiddenModifier, !show);
        }

        /// <param name="panelPoint">Fingertip target in panel coordinates.</param>
        public void SetHand(bool visible, Vector2 panelPoint)
        {
            if (visible && !_handVisible)
            {
                _handTime = 0f;
            }

            _handVisible = visible;
            _handTarget = panelPoint;
            _hand.EnableInClassList("hand" + HiddenModifier, !visible);
            _handRing.EnableInClassList("hand-ring" + HiddenModifier, !visible);
        }

        /// <param name="danger">0 calm .. 1 dragon touching the base.</param>
        public void SetDanger(float danger, bool contact)
        {
            float pulse = contact ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f) : 1f;
            _vignette.style.opacity = Mathf.Clamp01(danger * danger * 0.6f * pulse);
        }

        public void Tick(float deltaTime)
        {
            _health.Tick(deltaTime);
            if (_praiseTimer > 0f)
            {
                _praiseTimer -= deltaTime;
                if (_praiseTimer <= 0f)
                {
                    _praise.AddToClassList(PraiseHide);
                }
            }

            if (_handVisible)
            {
                _handTime += deltaTime;
                // Tap motion: dip toward the target every 0.9s.
                float phase = Mathf.Repeat(_handTime, 0.9f) / 0.9f;
                float press = phase < 0.25f ? phase / 0.25f : phase < 0.45f ? 1f - (phase - 0.25f) / 0.2f : 0f;
                float handWidth = _hand.resolvedStyle.width;
                float offset = 26f - press * 22f;
                _hand.style.translate = new Translate(_handTarget.x - handWidth * 0.5f, _handTarget.y + offset);
                _hand.style.scale = new Scale(Vector2.one * (1f - press * 0.08f));
                float ringSize = _handRing.resolvedStyle.width;
                float ring = press > 0f ? 0.6f + (1f - press) * 0.2f : 1.4f;
                _handRing.style.translate = new Translate(_handTarget.x - ringSize * 0.5f, _handTarget.y - ringSize * 0.5f);
                _handRing.style.scale = new Scale(Vector2.one * ring);
                _handRing.style.opacity = press > 0f ? 0.9f : Mathf.Max(0f, 0.9f - (phase - 0.45f) * 2f);
            }
        }

        private sealed class Booster
        {
            public readonly Button Button;
            private readonly Label _badge;
            private readonly VisualElement _price;
            private readonly Label _priceLabel;

            public Booster(VisualElement root, string id)
            {
                Button = root.Q<Button>(id + "Button");
                _badge = Button.Q<Label>(className: "booster__badge");
                _price = Button.Q<VisualElement>(className: "booster__price");
                _priceLabel = _price.Q<Label>(className: "booster__price-text");
            }

            public void Set(int count, int price, bool enabled)
            {
                bool owned = count > 0;
                _badge.text = count.ToString();
                _badge.EnableInClassList("booster__badge--hidden", !owned);
                _priceLabel.text = price.ToString();
                _price.EnableInClassList("booster__price--hidden", owned);
                Button.EnableInClassList("booster--disabled", !enabled);
            }
        }
    }
}
