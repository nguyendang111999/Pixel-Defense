# PixelDefense.Gameplay

Battle presentation: turns `Core.Battle` events into 3D visuals, sound and haptics. No rules live here.

| Area | Public API | Notes |
|---|---|---|
| `Battle/BattleView` | `Init(audio, haptics, input, settings)`, `Prepare(level, viewport)`, `Begin(slitherIn, token)`, `TapColumn(column)`, `UseFreeze/UseBomb/AddSlot`, `BeginPick/CancelPick/IsPicking`, `AutoPlay`, `SpeedMultiplier`, `Progress`, `Danger`, `Stars`, `MinDistance`, `GetHealthSegments`, `TryGetHintScreenPoint`, events `Finished`, `Praise`, `JamChanged`, `Deployed`, `PickModeChanged`, `PickPlaced` | Owns the model, ticks it in `Update`, hit-tests taps against the tray, combos, hints, win/lose cinematics. Pick mode freezes time until a tray cannon is tapped. |
| `Battle/AutoPlayDriver` | used by `BattleView.AutoPlay` | Testing bot: plans with `LevelSolver.SolveFrom` on a worker thread, taps through `TapColumn`, shows the hint hand on its next column. |
| `Arena/` | `ArenaSpace` (slice units → world, slot/column layout, `SeatedCannonSize`), `ArenaView` (floor, pipe walls, base, pads, tray, danger glow + 2 s countdown ring) | Slot row fits `VisualConfig.SlotRowFill` of the platform; seated cannons shrink with the slot pitch so barrels clear neighbours. |
| `Dragon/` | `DragonView` (instanced scale cubes on critically damped springs, slither wave, voxel head with jaw/eyes that glides back when its front slices die, tail, shatter), `DragonHeadModel` | `DragonView.HeadReach` defines where the head touches the base. |
| `Cannons/` | `CannonView` (`EnterFrom` below the screen, tray idle, jump + shrink to slot, `Reseat`, aim, recoil, reveal, deny, pick glow, exit), `CannonMeshes`, `PixelFont` | Ammo is a pixel-font decal on the lid. Tray rows are `VisualConfig.RowSpacing` apart so barrels clear the cannon ahead. |
| `Fx/` | `ProjectileSystem`, `DebrisSystem` (instanced), `FxSystem` (Shuriken built in code) | |
| `Rendering/` | `InstancedBatch` (RenderMeshInstanced + per-instance color/fx), `ToyLook` (shader globals) | Shaders in `Assets/Art/Shaders`. |
| `Camera/CameraRig` | `Frame(points, viewport)`, `AddTrauma`, `Kick`, `FocusOn`, `ResetFocus` | `BattleView` frames the track's `Outline` plus the tray. |
| `Meshes/` | `MeshBuilder`, `MeshFactory`, `VoxelModel` | Everything is procedural; no imported models. |
| `Config/VisualConfig` | ScriptableObject | Palette, skins, sizes, materials, juice. |

Dependencies: Core, Services, UniTask (+ UniTask.DOTween), DOTween, URP.
