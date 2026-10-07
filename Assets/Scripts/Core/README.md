# PixelDefense.Core

Engine-free rules and data (`noEngineReferences: true`), fully covered by EditMode tests.

- **Level format** — `LevelParser.Parse(text, packName)` → `LevelPack` (levels + issues). Text format documented on `LevelParser`; validation rules in `LevelValidator` (ammo per color must equal scales per color).
- **Track geometry** — `TrackPresets.Create(id, lanes)` → `TrackShape` in slice units: `Sample(s, …)`, `EndS`, `Walls`, `ArenaRadius`. Used by views (scaled to world) and by simulations (path lengths).
- **Simulation** — `Battle(level, settings, contactFront)`: `Deploy(column)`, `Tick(dt)`, boosters (`UseFreeze`, `UseBomb`, `AddSlot`), C# events for every visible change. Event-driven and frame-rate independent.
  - Neck-first targeting: only the front `Window` alive slices are exposed; cannons hit their own color only.
  - The body stays packed: a cleared slice knocks the head back `RecoilFraction` of a slice and the tail closes the rest.
  - Lose: head touches the base for `ContactLoseTime` continuous seconds. Win: all scales destroyed.
- **Tools** — `LevelSolver.Solve` (DFS over deploy orders at quiescence), `BotPlayer.Play` (real-time replay to measure time pressure).

Dependencies: none.
