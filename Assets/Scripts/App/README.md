# PixelDefense.App

Composition root. `GameFlow` builds services, wires `BattleView` and `GameUi`, and runs the state machine
(`Home → Playing → Result`). It owns progress (`ProgressData`, saved with `JsonFileStore` on level end and
`OnApplicationPause`), the booster economy (use owned boosters, else buy with coins), tutorial prompts, pause and the
Android back button.

| Type | Notes |
|---|---|
| `GameConfig` | Level pack, `VisualConfig`, `AudioLibrary`, `BattleSettings`, economy and loop settings. |
| `ProgressData` | Versioned save keyed by level id. |
| `GameFlow.DebugPlayLevel(index)` | Editor / development builds only: jump to a level. |

Dependencies: Core, Services, Gameplay, UI, UniTask, Input System.
