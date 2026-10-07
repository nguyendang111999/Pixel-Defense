# PixelDefense.Gameplay

Battle presentation: turns `Core.Battle` events into 3D visuals, sound and haptics. No rules live here.

| Area | Public API | Notes |
|---|---|---|
| `Battle/BattleView` | `Init(audio, haptics, input, settings)`, `Prepare(level, viewport)`, `Begin(slitherIn, token)`, `UseFreeze/UseBomb/AddSlot`, `Progress`, `Danger`, `Stars`, `GetHealthSegments`, `TryGetHintScreenPoint`, events `Finished`, `Praise`, `JamChanged`, `Deployed` | Owns the model, ticks it in `Update`, hit-tests taps against the tray, combos, hints, win/lose cinematics. |
| `Arena/` | `ArenaSpace` (slice units → world, slot/column layout), `ArenaView` (floor, pipe walls, base, pads, tray, danger glow + 2 s countdown ring) | |
| `Dragon/` | `DragonView` (instanced scale cubes on springs, slither wave, voxel head with jaw/eyes, tail, shatter), `DragonHeadModel` | `DragonView.HeadReach` defines where the head touches the base. |
| `Cannons/` | `CannonView` (tray idle, jump + shrink to slot, aim, recoil, reveal, deny, exit), `CannonMeshes`, `PixelFont` | Ammo is a pixel-font decal on the lid. |
| `Fx/` | `ProjectileSystem`, `DebrisSystem` (instanced), `FxSystem` (Shuriken built in code) | |
| `Rendering/` | `InstancedBatch` (RenderMeshInstanced + per-instance color/fx), `ToyLook` (shader globals) | Shaders in `Assets/Art/Shaders`. |
| `Camera/CameraRig` | `Frame(points, viewport)`, `AddTrauma`, `Kick`, `FocusOn`, `ResetFocus` | |
| `Meshes/` | `MeshBuilder`, `MeshFactory`, `VoxelModel` | Everything is procedural; no imported models. |
| `Config/VisualConfig` | ScriptableObject | Palette, skins, sizes, materials, juice. |

Dependencies: Core, Services, UniTask (+ UniTask.DOTween), DOTween, URP.
