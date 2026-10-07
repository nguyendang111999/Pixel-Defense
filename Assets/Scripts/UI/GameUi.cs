using UnityEngine;
using UnityEngine.UIElements;

namespace PixelDefense.UI
{
    /// <summary>
    /// Owns the single UIDocument: applies the safe area, answers "is this screen point over UI?" for gameplay
    /// input, converts screen points to panel space and hosts the screen views.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameUi : MonoBehaviour
    {
        private const string HiddenClass = "hidden";

        // Full-screen popups carry this class so taps on them never reach the board.
        private const string BlocksInputClass = "blocks-input";

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _safe;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreen;

        public HudView Hud { get; private set; }
        public HomeView Home { get; private set; }
        public ResultView Result { get; private set; }
        public SettingsView Settings { get; private set; }

        /// <summary>Fraction of screen height covered by the top HUD (for camera framing).</summary>
        public float TopInsetFraction { get; private set; } = 0.17f;

        /// <summary>Fraction of screen height reserved at the bottom (safe area + margin).</summary>
        public float BottomInsetFraction { get; private set; } = 0.02f;

        public void Init()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            _safe = _root.Q<VisualElement>("safe");
            Hud = new HudView(_root);
            Home = new HomeView(_root);
            Result = new ResultView(_root);
            Settings = new SettingsView(_root);
            _root.RegisterCallback<ClickEvent>(OnAnyClick, TrickleDown.TrickleDown);
            ApplySafeArea(force: true);
        }

        /// <summary>Raised for every button click (for a shared UI click sound).</summary>
        public event System.Action ButtonClicked;

        private void OnAnyClick(ClickEvent evt)
        {
            if (evt.target is VisualElement element && (element is Button || element.GetFirstAncestorOfType<Button>() != null))
            {
                ButtonClicked?.Invoke();
            }
        }

        public void ShowHud(bool show)
        {
            Hud.Root.EnableInClassList(HiddenClass, !show);
        }

        public void ShowHome(bool show)
        {
            Home.Root.EnableInClassList(HiddenClass, !show);
        }

        /// <param name="screenPosition">Screen pixels, origin bottom-left (Input System convention).</param>
        public bool IsOverInteractive(Vector2 screenPosition)
        {
            if (_root?.panel == null)
            {
                return false;
            }

            Vector2 panelPoint = ToPanel(screenPosition);
            for (VisualElement e = _root.panel.Pick(panelPoint); e != null; e = e.parent)
            {
                if (e is Button || e.ClassListContains(BlocksInputClass))
                {
                    return true;
                }
            }
            return false;
        }

        /// <param name="screenPosition">Screen pixels, origin bottom-left.</param>
        public Vector2 ToPanel(Vector2 screenPosition)
        {
            var flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            return RuntimePanelUtils.ScreenToPanel(_root.panel, flipped);
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            ApplySafeArea(force: false);
            Hud.Tick(Time.unscaledDeltaTime);
            Result.Tick(Time.unscaledDeltaTime);
        }

        private void ApplySafeArea(bool force)
        {
            if (_root.panel == null)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == _appliedSafeArea && screen == _appliedScreen)
            {
                return;
            }

            _appliedSafeArea = safe;
            _appliedScreen = screen;
            Vector2 topLeft = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
            Vector2 bottomRight = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(safe.xMax, Screen.height - safe.yMin));
            Vector2 panelSize = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(Screen.width, Screen.height));
            _safe.style.paddingLeft = Mathf.Max(0f, topLeft.x);
            _safe.style.paddingTop = Mathf.Max(0f, topLeft.y);
            _safe.style.paddingRight = Mathf.Max(0f, panelSize.x - bottomRight.x);
            _safe.style.paddingBottom = Mathf.Max(0f, panelSize.y - bottomRight.y);

            float height = Mathf.Max(1f, Screen.height);
            TopInsetFraction = Mathf.Clamp01((Screen.height - safe.yMax) / height + 0.155f);
            BottomInsetFraction = Mathf.Clamp01(safe.yMin / height + 0.025f);
        }
    }
}
