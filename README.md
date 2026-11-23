# Blocktrotter: Slide Puzzle

A sliding-block puzzle game for iOS (Android later), built in **Unity 6.3 LTS** with C#.
Slide the blocks out of the way and guide the red block to the exit — across **5 worlds** and **500 levels**,
in English and Turkish.

<p align="center">
  <img src="docs/screenshots/menu-garden.png" width="200" alt="Main menu, Japanese Garden world">
  <img src="docs/screenshots/gameplay.png" width="200" alt="Gameplay">
  <img src="docs/screenshots/solved.png" width="200" alt="Puzzle solved panel with stars">
  <img src="docs/screenshots/menu-neon.png" width="200" alt="Main menu, Neon City world">
</p>

## Features

- **500 original levels**, generated and verified by the game's own solver — from 5 moves to 20, and every one needs real planning: no level can be solved by just pushing each block out of the red one's way.
- **5 worlds**, one every 100 levels: Japanese Garden, Egyptian Desert, Tropical Island, Arctic Glaciers, Neon City — each with its own blocks (bamboo, carved sandstone, sea glass, ice, neon circuitry), its own red block, palette and animated background (falling petals, pyramids, waves, snow, a neon skyline).
- **1–3 stars** per level based on moves versus the optimal solution; unlocks, best stars and hints are saved on the device.
- **A guided tutorial** on first launch (replayable from Settings) that teaches the rules — and the key idea that sometimes you must move another block first.
- **Hints** that play the next optimal move, **undo** and **restart**.
- **A red block with personality**: a light sweeps across it while you play; when the way is clear it winds up, dashes out with a stretch and a trail, and the exit gate flashes as it passes.
- **Celebration**: confetti, a pop-in "Puzzle solved" panel with stars ringing in one by one, and a "New world unlocked!" screen.
- **Sound**: 10 original effects synthesized from scratch in code (block snap, exit whoosh, star bells, solved jingle, taps, hint, undo, new world), with an on/off setting.
- **English and Turkish**, picked from the device language and switchable in Settings.
- **Ads (Google AdMob)**: a banner on the main menu only, an interstitial every 5 solved levels or 200 seconds (only on the player's tap after a solve, never mid-puzzle) and an optional rewarded ad for a hint — with Google UMP consent and Apple App Tracking Transparency.
- **Original art only**: icons, stars, confetti, bevel lighting and background decor are drawn in code; the block surfaces are seamless textures I generated with AI image generation and own. No asset packs, no third-party sprites — and deliberately no wood, so it looks like nothing else in the genre.

## How it works

| Area | What's there |
|---|---|
| **Puzzle core** (`Assets/_Game/Core`, plain C#, no Unity dependency) | `Board` (move legality, win check), `BoardSolver` (breadth-first search over a packed 64-bit state; also powers hints), `GameSession` (moves, undo, restart, hints), `PuzzleDifficulty`, `StarRating`, `AdPolicy`, `LevelCodec` (36-character level strings). |
| **Level generation** | `StateCluster` explores every position reachable from a random board and labels each with its distance to a solved position; `LevelSetGenerator` picks positions at exactly the move counts a difficulty curve asks for. Move count alone isn't difficulty, so for each level it compares 12 candidates with `PuzzleDifficulty` — *detours* (moves beyond the obvious "clear each blocker once" plan, i.e. blocker-of-a-blocker moves), reversals and solver search effort — keeps the most demanding one and rejects any with no detour. Seeded, so the same settings always produce the same 500 levels. Run from the editor: *Blocktrotter › Generate Levels*. |
| **App & screens** (`Assets/_Game/Runtime`) | One scene; screens (main menu, worlds, level grid, game, settings, world unlocked) are built in code under a safe-area-aware canvas. |
| **Look & feel** | `BlockSkins` paints every block face once per theme, size and orientation (texture sampling with mips, rounded corners, bevel light, per-style accents); `BoardView` drives the red block's wind-up/dash/trail exit and the gate; `SoundPlayer` mixes effects on a small voice pool. |
| **Data-driven tuning** | `GameConfig`, `ThemeDefinition` ×5, `SoundConfig`, `AdConfig`, `LevelDatabase` ScriptableObjects — gameplay, feel, art and volume live in assets, not code. |
| **Ads** | `IAdService` with an editor stand-in; the AdMob implementation sits in an optional assembly that compiles only when the Google Mobile Ads package is installed. |
| **Tests** (`Assets/Tests/EditMode`) | 79 NUnit tests: board rules, solver, tutorial script, difficulty metric, generator determinism, every one of the 500 levels solved, replayed and checked for a detour, progress saving, ad pacing, localization tables, every sound wired. |

<p align="center">
  <img src="docs/screenshots/world-desert.png" width="200" alt="Egyptian Desert: carved sandstone blocks">
  <img src="docs/screenshots/world-island.png" width="200" alt="Tropical Island: sea-glass blocks and a life-ring red block">
  <img src="docs/screenshots/world-arctic.png" width="200" alt="Arctic Glaciers: ice blocks">
  <img src="docs/screenshots/world-neon.png" width="200" alt="Neon City: glowing circuit blocks">
</p>

<p align="center">
  <img src="docs/screenshots/worlds.png" width="200" alt="World select">
  <img src="docs/screenshots/levels-neon.png" width="200" alt="Level grid in the Neon City world">
  <img src="docs/screenshots/confetti.png" width="200" alt="Confetti after solving">
  <img src="docs/screenshots/world-unlocked.png" width="200" alt="New world unlocked">
</p>

## Running it

1. Open the folder in **Unity 6000.3.26f1** (Unity Hub › Add project from disk).
2. Press Play — the editor always starts from `Assets/_Game/Scenes/Game.unity`. Ads appear as grey "TEST" placeholders in the editor.
3. Tests: *Window › General › Test Runner › EditMode › Run All*.

Ads use Google's public **test** ad unit IDs until release.

## Legal

[Privacy Policy](https://fruthbtw.github.io/blocktrotter/privacy-en.html) · [Terms of Service](https://fruthbtw.github.io/blocktrotter/terms-en.html)
([Türkçe](https://fruthbtw.github.io/blocktrotter/privacy-tr.html))

## License

**Copyright © 2026 Yunus Emre Uludağ. All rights reserved.**
The source is published so it can be read as a portfolio piece. No licence is granted to copy, modify, distribute or publish the game or its code.

Developed with [Claude Code](https://claude.com/claude-code) as a pair-programming assistant.
