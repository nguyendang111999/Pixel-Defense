# PixelDefense.App

Composition root. `GameFlow` builds services, wires `BattleView` and `GameUi`, and runs the state machine
(`Home → Playing → Result`). It owns progress (`ProgressData`, saved with `JsonFileStore` on level end and
`OnApplicationPause`), the booster economy (use owned boosters, else buy with coins; the pick booster is paid only when
a cannon is actually placed), tutorial prompts, pause and the Android back button.

**Autoplay bot (Editor and development builds):** pause menu ▸ TESTING ▸ AUTOPLAY BOT, the **B** key, or menu
Pixel Defense ▸ Bot ▸ Toggle Autoplay. The bot plays the level, logs `[Bot] L15 …: WON 2 stars in 51.4s` to the
console, then moves on to the next level (retries a loss, skips a level after `GameConfig.BotRetries` losses). GAME
SPEED (or **N**) cycles `GameConfig.BotSpeeds`. Its wins advance your saved progress like normal play.

| Type | Notes |
|---|---|
| `GameConfig` | Level pack, `VisualConfig`, `AudioLibrary`, `BattleSettings`, economy (incl. `StartPick`, `PickPrice`), loop and bot settings. |
| `ProgressData` | Versioned save keyed by level id (v2 added `Pick`; v1 saves get the starting picks). |
| `GameFlow.DebugPlayLevel(index)`, `DebugToggleBot()` | Editor / development builds only: jump to a level, toggle the bot. |

Dependencies: Core, Services, Gameplay, UI, UniTask, Input System.
