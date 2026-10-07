# PixelDefense.Services

Generic, game-agnostic services. Nothing here references gameplay, levels or rules.

| Area | Public API | Notes |
|---|---|---|
| `Audio` | `AudioLibrary` (SO), `AudioService.Resolve(id)` → handle, `Play(handle, pitch, volume)`, `PlayMusic(bool)` | Pooled 2D voices, per-sound voice caps and cooldowns, no per-play allocation. |
| `Haptics` | `HapticService.Play(HapticStyle)` | iOS `UIFeedbackGenerator` via `Plugins/iOS/PDHaptics.mm`, Android `VibrationEffect` via JNI, rate-limited, no-op in Editor. |
| `Save` | `JsonFileStore.TryLoad<T>`, `Save<T>` | JsonUtility files in `persistentDataPath`; temp file + replace + `.bak` fallback. |
| `Settings` | `SettingsStore.Sound/Music/Haptics`, `Changed` | PlayerPrefs toggles. |
| `Input` | `PointerInput.Pressed/Released`, `BlockedBy` | Input System: primary touch on device, mouse in Editor (and for CLI `simulate_pointer`). |
| `Motion` | `FloatSpring`, `Vector3Spring`, `Springs.Damp`, `Easing` | Frame-rate independent juice math. |

Dependencies: `Unity.InputSystem`.
