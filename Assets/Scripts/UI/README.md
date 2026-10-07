# PixelDefense.UI

UI Toolkit screens for `Assets/UI/Game.uxml`. Views are plain classes over queried elements; styling is in USS
(`Assets/UI/Styles`), state changes toggle USS classes (transitions do the animation).

| Type | Public API |
|---|---|
| `GameUi` (on the `UIDocument`) | `Init()`, `Hud`, `Home`, `Result`, `Settings`, `ShowHud/ShowHome`, `IsOverInteractive(screenPos)`, `ToPanel(screenPos)`, `TopInsetFraction`, `BottomInsetFraction`, `ButtonClicked` |
| `HudView` | `SetLevel(number, tag)`, `SetCoins`, `SetDragonColor`, `SetHealth`, `SetBooster` (Freeze, Bomb, Slot, Pick), `PunchBooster`, `ShowPraise`, `SetJammed`, `SetPicking`, `SetTutorial`, `SetHand`, `SetDanger`, `BoosterClicked`, `PickCancelClicked`, `PauseButton` |
| `HomeView` | `SetLevel(number, boss)`, `PlayClicked`, `SettingsClicked` |
| `ResultView` | `ShowWin(stars, reward, message)`, `ShowLose(message)`, `Hide`, `PrimaryClicked`, `SecondaryClicked` |
| `SettingsView` | `Show(inBattle)`, `Hide`, `SetToggles`, `ShowDebug`, `SetBot`, toggle/restart/home/close/bot/speed events (bot rows only in Editor/development builds) |
| `IconElement` (UXML `pd:IconElement kind="Gear"`) | Painter2D icons: Gear, Close, Snowflake, Bomb, Plus, Coin, Hand, Play, Retry, Home, Sound, Music, Vibrate, Dragon, Star, Pick (arcade claw), Bot |
| `DragonHealthBar` | `SetSegments(colors, counts, total)`, `Shake()` |

Without Unity's default runtime theme, `Theme.uss` styles `.unity-ui-document__root` so the document fills the screen.
Fonts: Roboto Black (Apache-2.0) and Inter SemiBold (OFL) as dynamic SDF font assets in `Assets/UI/Fonts`.

Dependencies: Services, UniTask.
