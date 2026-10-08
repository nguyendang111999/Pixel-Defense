# PixelDefense.Core

Engine-free rules and data (`noEngineReferences: true`), fully covered by EditMode tests.

- **Level format** — `LevelParser.Parse(text, packName)` → `LevelPack` (levels + issues). Text format documented on `LevelParser`; validation rules in `LevelValidator` (ammo per color must equal scales per color; at most `MaxColumns` = 4 cannon columns).
- **Track geometry** — `TrackPresets.Create(id, lanes)` → `TrackShape` in slice units: `Sample(s, …)`, `EndS`, `Walls`, `Outline` (convex arena outline for camera framing), `ArenaMin/MaxX/Y`. Layouts (`TrackPresets.Ids`): `spiral`, `square`, `spiral_long`, `hairpin` (two rings + U-turn), `maze` (three rings, two U-turns; wider than the others, not used in the campaign), `zigzag` (radial switchbacks), `snake` (rows above the base); each has a mirrored `_cw` twin. Paths are built from lines and arcs; walls are generated for any path (side-by-side passes share one wall). The base is centered on the origin (radius `TrackPresets.BaseRadius`).
- **Simulation** — `Battle(level, settings, contactFront)`: `Deploy(column)` (front cannon), `DeployCannon(id)` (any waiting cannon — the pick booster), `Tick(dt)`, boosters (`UseFreeze`, `UseBomb`, `AddSlot`), C# events for every visible change. Event-driven and frame-rate independent. `ActionCount` counts player actions; `CloneForPlanning()` makes a copy that never moves or loses on time.
  - Neck-first targeting: only the front `Window` alive slices are exposed; cannons hit their own color only.
  - Conveyor motion: the body (tail included) moves at the crawl speed; a cleared slice knocks only the head back (`RecoilFraction` = 1 slice). Lower values make the tail close part of the gap instead.
  - Lose: head touches the base for `ContactLoseTime` continuous seconds. Win: all scales destroyed.
- **Tools** — `LevelSolver.Solve` / `SolveFrom(liveBattle)` (DFS over deploy orders at quiescence; moves as columns and cannon ids), `BotPlayer.Play` (real-time replay to measure time pressure), `AutoPlayer` (autoplay policy: follow a plan, tap only once the board settles, replan after outside actions), `BattleHints.BestColumn` (the in-game hint).

Dependencies: none.
