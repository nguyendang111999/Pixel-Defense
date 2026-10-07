# PixelDefense.EditorTools

Editor-only generators that make the project reproducible from code (menu **Pixel Defense**).

| Area | Notes |
|---|---|
| `ProjectBuilder` | Menu entry points: rebuild assets, rebuild scene, reset configs. |
| `Assets/` | `TextureBaker` (procedural PNGs), `MaterialBaker`, `FontBaker` (copies Roboto Black / Inter SemiBold, makes SDF font assets), `AudioBaker`, `ConfigBaker` (configs, PanelSettings, post profile, URP quality), `SceneBuilder`, `AssetPaths`. |
| `Audio/` | `SfxSynth` (oscillators, envelopes, filters, WAV writer) and `SfxDesigns` (every sound + the music loop). |
| `Levels/CampaignGenerator` | Seeded campaign design → solver/replay proof → bot speed tuning → `Assets/Levels/main.txt`. Every color gets a multiple of 10 scales, split into 10/20/40-ammo cannons. Track order is `Tracks`; bosses use the same compact tracks (`BossTracks`) with longer bodies that trail off-screen. At most 4 columns. Edit `Plans()` to change the curve. |
| `Levels/BotReport` | Menu **Pixel Defense ▸ Bot ▸ Simulate All Levels**: solves every level and replays it with an expert (0.5 s/tap) and a careful (1.2 s/tap) bot; logs win/time/margin per level. Also **Toggle Autoplay (Play Mode)**. |
| `GameViewTools` | Portrait Game view resolutions. |

Dependencies: everything (Editor platform only).
