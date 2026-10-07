# PixelDefense.EditorTools

Editor-only generators that make the project reproducible from code (menu **Pixel Defense**).

| Area | Notes |
|---|---|
| `ProjectBuilder` | Menu entry points: rebuild assets, rebuild scene, reset configs. |
| `Assets/` | `TextureBaker` (procedural PNGs), `MaterialBaker`, `FontBaker` (copies Roboto Black / Inter SemiBold, makes SDF font assets), `AudioBaker`, `ConfigBaker` (configs, PanelSettings, post profile, URP quality), `SceneBuilder`, `AssetPaths`. |
| `Audio/` | `SfxSynth` (oscillators, envelopes, filters, WAV writer) and `SfxDesigns` (every sound + the music loop). |
| `Levels/CampaignGenerator` | Seeded campaign design → solver/replay proof → bot speed tuning → `Assets/Levels/main.txt`. Edit `Plans()` to change the curve. |
| `GameViewTools` | Portrait Game view resolutions. |

Dependencies: everything (Editor platform only).
