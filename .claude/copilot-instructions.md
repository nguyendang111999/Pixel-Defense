# Unity Mobile Puzzle Game Guidelines

Shared guidelines for my Unity mobile puzzle games. Only **Project Context** is project-specific; everything else applies to every game. `<Project>` is the project name from Project Context. Rules that name a package or platform apply only if Project Context lists it.

## Project Context
- Project name: **Pixel Defense**
- Genre: casual 3D puzzle; level-driven, short sessions, one-handed play.
- Platforms: Android + iOS. Orientation: **portrait only**, Canvas reference resolution 1080x1920.
- Performance target: 60 FPS on low/mid-range phones.
- Engine: Unity 6000.3.25f1 LTS, 3D URP, UITookit, Unity Test Framework.
- Stack: DOTween (animation), UniTask (async), Addressables (content loading).
- Input backend: legacy Input Manager.

## C# and Unity Constraints
- Only use APIs available in the project's Unity version (`ProjectSettings/ProjectVersion.txt`).
- **C# 9 / .NET Standard 2.1.** Don't use file-scoped namespaces, global usings, `record struct`, raw string literals, `required` members, collection expressions (`[]`), or primary constructors. Avoid `init`/`record` (need an `IsExternalInit` shim; not Unity-serializable).
- No `System.Text.Json`: use `JsonUtility` (`[Serializable]` fields only; no dictionaries or top-level arrays) or Newtonsoft (`com.unity.nuget.newtonsoft-json`) if installed.
- Null-check `UnityEngine.Object` with `== null` or implicit bool. Never use `?.`, `??`, `??=`, `is null`, or `is not null` on them (they skip Unity's destroyed-object check).
- IL2CPP + code stripping: avoid reflection, `dynamic`, and `Activator.CreateInstance`; if unavoidable, add `[Preserve]` or `link.xml`.
- Avoid static mutable state; if needed, reset it in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (Enter Play Mode Options can skip domain reload).
- Assembly definitions must explicitly reference each package assembly they use, e.g. `Unity.TextMeshPro`, `UniTask`, `Unity.Addressables` + `Unity.ResourceManager`, `DOTween.Modules` (create it via *DOTween Utility Panel > Create ASMDEF*). Awaiting tweens with UniTask needs the `UNITASK_DOTWEEN_SUPPORT` define.

## Architecture
- **Model (plain C#)**: board state, move validation, win/fail checks, undo, hints/solver, scoring. No MonoBehaviour or scene dependencies (`Vector2Int`/`Mathf` are fine). Deterministic and EditMode-testable.
- **View/controller (MonoBehaviour)**: forwards input to the model and reacts to model events with visuals, audio, and animation. No puzzle rules in MonoBehaviours.
- **Input**: one input component wraps the Project Context input backend; gameplay code never reads input APIs directly.
- **Config**: ScriptableObjects (or CSV/JSON tables) for tunables such as theme colors, animation timings, difficulty, and rewards. No magic numbers in code.
- **Composition**: one bootstrap wires dependencies through serialized references or `Init(...)`/constructors. No `GameObject.Find*`, `FindObjectOfType`, `SendMessage`, or string lookups at runtime. Singletons only for app-lifetime services.
- **Game flow**: explicit state machine (e.g. `Loading -> Playing -> Won/Failed -> Result`). Accept board input only in `Playing` and while no board-changing animation runs.
- **Events**: C# events/`Action`; subscribe in `OnEnable`, unsubscribe in `OnDisable`. `Awake` initializes self, `Start` talks to others.
- Undo, restart, and hints live in the model (command stack or state snapshots); never reload the scene for them.
- Model randomness uses an injected, seeded `System.Random`, not `UnityEngine.Random`.
- Levels are data, not scenes: one gameplay scene plays any level.
- Heavy solver/generator work runs in Editor tools or on a worker thread (e.g. `UniTask.RunOnThreadPool`) with cancellation. No Unity API calls off the main thread.

## Modularity
- Design systems as modules with one responsibility each (e.g. Save, Audio, Settings, Input, UI kit) and give each its own assembly definition (`<Project>.<Module>`).
- Generic modules never reference game-specific code (Gameplay, level data, game rules). Game assemblies reference them, never the reverse; the compiler enforces this.
- Modules talk through small interfaces and events. Keep types `internal` unless another assembly needs them; consumers depend on the interface, not the implementation.
- No circular references. Dependency direction: App (bootstrap) -> Gameplay/UI -> Services -> Core.
- Set `noEngineReferences: true` on Core and other engine-free modules where possible, and `autoReferenced: false` on module assemblies so they don't leak into `Assembly-CSharp`.
- Add a new assembly when a system has a clear boundary, is reusable, or slows compile times; not one per class.
- A module reused by two or more games moves to a local UPM package (`Packages/<name>/` with `package.json` and its own asmdef), referenced from each project's `manifest.json` via `file:` or a git URL, instead of being copied between projects.
- Each module has a short `README.md` (purpose, public API, dependencies); update it when the API changes.

## Levels and Data Files (JSON / text / CSV)
- Level packs are JSON, text, or CSV `TextAsset`s loaded via Addressables. No `Resources/`; no scene or ScriptableObject per level.
- One grid convention everywhere: `Vector2Int(x, y)`, origin bottom-left, index `y * width + x`. Text/CSV rows are top-to-bottom, so flip `y` in the parser only.
- Parse and format numbers with `CultureInfo.InvariantCulture` (comma-decimal device locales break `float.Parse`); compare IDs with `StringComparison.Ordinal`.
- Parsers tolerate UTF-8 BOM, `\r\n`, blank lines, `#` comments, and quoted CSV fields.
- Validate each level on load (size, bounds, symbols, required fields). On bad data, log an error with pack + level ID and skip it; never crash.
- Every level pack and save file has a `version` field; add migrations instead of breaking old data.
- Level tools (import, validate, solve, preview) live in `Editor/` folders; an EditMode test proves every shipped level parses and is solvable.
- Android `StreamingAssets` lives inside the APK/AAB: read it with `UnityWebRequest`, not `System.IO.File`.

## Save Data
- Progress is JSON in `Application.persistentDataPath`, written safely (temp file, then replace; keep a backup). `PlayerPrefs` only for small settings (sound, music, haptics).
- Key progress by stable level IDs, not array indices, so reordering or inserting levels doesn't corrupt saves.
- Save on level complete and in `OnApplicationPause(true)`; `OnApplicationQuit` is unreliable on mobile.

## Mobile Performance
- Zero GC allocations per frame (`Update`, drag handlers): no LINQ, string concatenation/interpolation, capturing lambdas, boxing, `GetComponent`, or new collections. Cache and reuse.
- Pool frequently spawned objects (tiles, path segments, particles, popups) with `UnityEngine.Pool.ObjectPool<T>`.
- Prefer event-driven code over polling; delete empty Unity messages (`Update`, `Start`, ...).
- Map touches to cells with grid math (screen -> world -> cell), not per-cell colliders or physics raycasts.
- Set `Application.targetFrameRate` to the Project Context FPS target at boot (mobile defaults to 30); use `OnDemandRendering.renderFrameInterval` on idle screens to save battery.
- Rendering: Sprite Atlases, few materials, low overdraw (avoid large transparent full-screen layers).
- UI: separate Canvases for static vs. frequently updated elements; disable `Raycast Target` on non-interactive graphics; no Layout Groups on often-changing elements; update TMP text with `SetText(format, value)` instead of building strings.
- No `Debug.Log` in per-frame code (slow on device, not stripped from release builds).
- DOTween: `SetLink(gameObject)` or kill tweens in `OnDisable`/`OnDestroy`; never create tweens every frame; use `SetUpdate(true)` for UI tweens that must run while `Time.timeScale == 0`.
- UniTask over coroutines: no `async void` (use `async UniTaskVoid` + `.Forget()`); always pass a `CancellationToken` (e.g. `destroyCancellationToken`).
- Addressables: every `LoadAssetAsync`/`InstantiateAsync` has a matching `Release`/`ReleaseInstance` with a clear owner.

## Mobile UX
- Canvas Scaler: Scale With Screen Size at the Project Context reference resolution. Layouts must work from 4:3 tablets to 20:9+ phones.
- Apply `Screen.safeArea` to a root `RectTransform` (notches, home indicator); re-apply when it changes.
- Fit the board to the free area (safe area minus HUD) at runtime; never hard-code board size.
- Touch: track one finger, ignore extra fingers, handle `TouchPhase.Canceled`, and skip board input over UI.
- Legacy Input: use `Input.touchCount`/`Input.GetTouch(i)` (`Input.touches` allocates); check UI with `EventSystem.current.IsPointerOverGameObject(touch.fingerId)` (the no-arg overload only checks the mouse); mouse fallback in Editor only (touches also simulate mouse input on device).
- Tap targets >= 48 dp; snap drags to the nearest valid cell and tolerate finger jitter.
- Every action gets instant feedback (tween + SFX, optional haptics); sound, music, and haptics toggles persist.
- Android back button (`KeyCode.Escape` in legacy Input): close the top popup, otherwise open a pause/quit confirmation.

## Code Conventions
- Naming: `PascalCase` types, methods, properties, events, constants; `_camelCase` private fields; `camelCase` locals/parameters; `I`-prefixed interfaces; `Async` suffix on async methods; past-tense events (`LevelCompleted`) with `On...` handlers.
- Namespaces: `<Project>.<Area>` matching folders (e.g. `<Project>.Core`, `<Project>.Gameplay`, `<Project>.UI`). Never name a namespace segment `Editor` (it shadows `UnityEditor.Editor`); use `EditorTools`.
- Inspector fields: `[SerializeField] private` (+ read-only property if needed), never public fields. When renaming a serialized field, add `[FormerlySerializedAs("oldName")]`.
- One MonoBehaviour/ScriptableObject per file, file name = class name. ScriptableObjects get `[CreateAssetMenu(menuName = "<Project>/...")]`.
- Comments only explain a non-obvious *why*, in one line; never restate the code or narrate the change.
- Code lives in assembly definitions (`<Project>.Core` for the model, `<Project>.Gameplay`, `<Project>.EditorTools`, `<Project>.Tests.EditMode`); test assemblies can't reference `Assembly-CSharp`. Dependencies point toward Core, and Core references no other project assembly, so the compiler enforces the model/view split.
- Folders: `Assets/Scripts/<Area>/`, `Assets/Tests/EditMode/`, `Assets/Levels/`.

## Working in Unity Projects
- Before using a Project Context package, confirm it's installed (`Packages/manifest.json` or `Assets/Plugins/`); if missing, say so instead of writing code that won't compile.
- Never hand-edit `.unity`, `.prefab`, `.asset`, or `.meta` YAML. When moving, renaming, or deleting a file under `Assets/`, do the same to its `.meta` (keeps GUID references; `.meta` files are hidden in the VS Code explorer).
- Never edit generated files (reading is fine): `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `*.csproj`, `*.sln`.
- Ask before adding packages or changing `Packages/manifest.json` or `ProjectSettings/`.
- Keep each change to one concern; don't mix refactors or reformatting with behavior changes.
- Scenes and prefabs are opaque to you: when adding, renaming, or removing a scene, screen/popup, or entry point, create/update `Docs/ProjectMap.md` (purpose, controller class, data location). Read it before navigating unfamiliar features.
- If a change needs Editor work (add components, wire references, create prefabs/assets, Addressables entries, settings), end with a short **Editor setup** checklist.

## Verification
- Ground claims in evidence: read the relevant code before explaining or changing it. State a root cause only with proof (code path, log, stack trace, failing test); otherwise call it a hypothesis.
- Check APIs against the installed versions instead of guessing: package sources in `Library/PackageCache/` or `Assets/Plugins/`, plugin XML docs (e.g. `DOTween.XML`).
- After editing C#, check IDE diagnostics; they're unreliable for new files until Unity regenerates the `.csproj`. Unity's actual compile errors and Console output are in the Editor log (Windows `%LOCALAPPDATA%\Unity\Editor\Editor.log`, macOS `~/Library/Logs/Unity/Editor.log`), updated when Unity recompiles on regaining focus.
- Performance claims and library swaps need Profiler/Memory Profiler data from a Development Build on device, not intuition.
- Finish by stating what you verified (diagnostics, tests run), plus a short **Verify** list of Play Mode/device checks only I can do (visuals, input feel, performance).

## Testing
- Write/update NUnit EditMode tests with every model change: move validation, win/fail detection, undo, level parsing, solver.
- Fix model bugs test-first: add a failing test that reproduces the bug, then make it pass.
- Test asmdef in `Assets/Tests/EditMode/`: Editor platform only, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`, references `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, and the model asmdef.
- Names: `Method_Condition_ExpectedResult`. Build test boards from compact ASCII strings through the real level parser. Seed all randomness.
- Run via *Window > General > Test Runner*. CLI only when the Editor is closed: `Unity -batchmode -projectPath <path> -runTests -testPlatform EditMode -testResults TestResults.xml` (no `-quit`; on Windows wrap it in `Start-Process -Wait`).

## Build and Release
- Android: IL2CPP + ARM64 (required by Google Play), publish AAB. iOS always uses IL2CPP.
- ASTC texture compression on Android and iOS.
- Stripping, IL2CPP, and performance issues don't show in the Editor: test release builds and profile Development Builds on a real low-end device.
