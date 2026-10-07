using System.Runtime.InteropServices;
using UnityEngine;

namespace PixelDefense.Services.Haptics
{
    public enum HapticStyle
    {
        Selection,
        Light,
        Medium,
        Heavy,
        Success,
        Failure
    }

    /// <summary>
    /// Short taptic feedback on iOS (UIFeedbackGenerator) and Android (VibrationEffect), rate-limited so rapid
    /// events don't turn into a continuous buzz. No-op in the Editor.
    /// </summary>
    public sealed class HapticService
    {
        private const float MinInterval = 0.045f;

        private bool _enabled = true;
        private float _lastTime = -1f;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _effectClass;
        private int _sdk;
#endif

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public HapticService()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _sdk = version.GetStatic<int>("SDK_INT");
                }

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (_sdk >= 26)
                {
                    _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Haptics unavailable: " + exception.Message);
                _vibrator = null;
            }
#endif
        }

        public void Play(HapticStyle style)
        {
            if (!_enabled)
            {
                return;
            }

            float now = Time.unscaledTime;
            bool important = style == HapticStyle.Heavy || style == HapticStyle.Success || style == HapticStyle.Failure;
            if (!important && now - _lastTime < MinInterval)
            {
                return;
            }
            _lastTime = now;

#if UNITY_IOS && !UNITY_EDITOR
            PDHaptics_Play((int)style);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(style);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PDHaptics_Play(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private void PlayAndroid(HapticStyle style)
        {
            if (_vibrator == null)
            {
                return;
            }

            try
            {
                if (_sdk >= 29)
                {
                    // Predefined effects feel crisp on modern actuators: TICK=2, CLICK=0, HEAVY_CLICK=5, DOUBLE_CLICK=1.
                    int effect = style == HapticStyle.Selection || style == HapticStyle.Light ? 2
                        : style == HapticStyle.Medium ? 0
                        : style == HapticStyle.Failure ? 1
                        : 5;
                    using (AndroidJavaObject vibration = _effectClass.CallStatic<AndroidJavaObject>("createPredefined", effect))
                    {
                        _vibrator.Call("vibrate", vibration);
                    }
                }
                else if (_sdk >= 26)
                {
                    long duration = style == HapticStyle.Heavy || style == HapticStyle.Failure ? 40L : style == HapticStyle.Medium ? 22L : 12L;
                    int amplitude = style == HapticStyle.Heavy || style == HapticStyle.Failure ? 255 : style == HapticStyle.Medium ? 150 : 70;
                    using (AndroidJavaObject vibration = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", duration, amplitude))
                    {
                        _vibrator.Call("vibrate", vibration);
                    }
                }
                else if (style == HapticStyle.Heavy || style == HapticStyle.Failure)
                {
                    // Pre-Oreo devices can't do short ticks; only signal the big moments. Also pulls in VIBRATE permission.
                    Handheld.Vibrate();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Haptics failed: " + exception.Message);
                _vibrator = null;
            }
        }
#endif
    }
}
