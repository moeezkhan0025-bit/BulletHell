# 05 - Dev Log

Project: Bullet Hell (working title), Unity 6.3 LTS, 2D URP. Repo history: 50 commits, 2026-09-28 to 2026-09-30.
Compiled 2026-09-30 from `git log`, `git show --stat`, `git log -p -- CLAUDE.md` and `git log -p -- Docs/BUGS.md`, plus the code in the working tree at commit `799d55b`.

## How to add an entry

1. Append at the bottom (or insert in date order), one entry per milestone, using the template below.
2. Get the facts from the repo, not from memory: `git log --reverse --format='%h|%ad|%s|%(trailers:key=Co-Authored-By,valueonly)' --date=format:'%Y-%m-%d %H:%M'` and `git show --stat <hash>`.
3. Model = the `Co-Authored-By` trailer of the commit. No trailer means the commit has no model attribution (every commit in this repo has the git author `Cyan`, including the agent-made ones, so the author field tells you nothing).
4. Problems and fixes come from `Docs/BUGS.md` (fixed entries are under "Fixed"; `git log -p -- Docs/BUGS.md` recovers entries that were later removed or reworded) and from commit message bodies.
5. Anything you infer rather than read gets a `[VERIFY]` tag.

```
### <Milestone> - <title>
- Date: YYYY-MM-DD (commit times)
- Commits: `hash` ...
- Model: ...
- Built: ...
- Problems and fixes: ...
- Tests / verification: ...
- Notes: [VERIFY] ...
```

Conventions used below: "Model" is what the commit trailer says. Test file attribution uses `git log --diff-filter=A -- 'Assets/Tests/EditMode/*.cs'` (the commit that first added the file). Test pass/fail status at the time of each milestone is NOT recorded in the repo; the only recorded run result is the M10-era bug entry (11 EditMode tests failing outside Play mode, see the M10 entry). Everything about "verified in Play mode" is [VERIFY] unless a commit message says it.

## Attribution summary

| Trailer | Commits | Milestones |
|---|---|---|
| `Claude Opus 5.5` | 5: `480dc6a`, `c52770a`, `2addb41`, `150865a`, `4a707ff` | M0, M1, M1.5 |
| `Claude Sonnet 5.5` | 31: `f92fefd` (M0 package add), then `578c096` (M3a) through `799d55b` (UI2), minus the untagged ones | M3a to UI2 |
| none | 14: `c581c13`, `bececa0`, `8800221`, `5b356f7`, `6d96071`, `8cefcd0`, `9d5fef6`, `921ffae`, `4bfa35d`, `6c30daa`, `7bfb75e`, `c3fc59a`, `dbd3019`, `4300774` | Setup, design notes, art uploads, doc pastes, hand-committed work |

Counts from `git log --format='%(trailers:key=Co-Authored-By,valueonly,separator=)' | sed '/^$/d' | sort | uniq -c` (5 Opus, 31 Sonnet; 50 total). Every commit has the git author `Cyan`, so the author field does not distinguish human from agent. The PF1 session (this case study) is not committed yet.

[VERIFY] Several untagged commits contain large code changes that look agent-written, not hand-made: `5b356f7` (WIP M1, 37 files, +3467, adds `ArmSelectorTests`), `8cefcd0` (labelled "M2 done; M3 design", 49 files, +1982, adds `FireTimerTests` and `HealthTests`, so it almost certainly holds the M2 code), and `9d5fef6` (labelled "Design: game flow...", 34 files, +1291, adds `ArmInstanceTests` and `StatCalculatorTests`, so it holds early M3b code). The likely reason is that the human committed the working tree by hand at those points, sweeping in the agent's uncommitted work; the model that wrote that code is probably the one of the surrounding milestone (Opus for M1 and M2, Sonnet for the M3b start). This cannot be proven from git alone.
### Models from the session transcripts (added 2026-09-30, with the author's permission)

The commit trailers only say which model was active when a commit was made. The Claude Code session transcripts
(`~/.claude/projects/C--Dev-BulletHell/*.jsonl`, read only for the `model`, timestamp and `/model` fields) show what actually did the work.
Times below are local (UTC-5); transcripts store UTC. Model switches were made by the author with `/model` (confirmed by the
`Set model to ...` stdout lines in the transcripts). Milestone names come from the "We're doing Mx from CLAUDE.md" prompts.

| Session (id prefix) | Local time | Model(s) in the transcript | Milestone work |
|---|---|---|---|
| `489b827c` | 09-28 13:47-14:32 | Sonnet 5.5 until 14:19, then Opus 5.5 | M0 (Sonnet), then M1 started (Opus). Switch via `/model` |
| `7c9ff7db` | 14:38-14:52 | Opus 5.5 | M1 finish (plan-and-approve rounds) |
| `4466eaf4` | 15:10-15:36 | Opus 5.5 | M1.5 |
| `53252171` | 15:37-16:02 | Sonnet 5.5 | M2 (so `8cefcd0`, untagged, holds Sonnet code, not Opus) |
| `10dfe826` | 16:05-16:22 | Sonnet 5.5 | M3a |
| `235077d1` | 16:26-16:40 | Sonnet 5.5 | M3b |
| `9c330260` | 16:42-18:31 | Sonnet 5.5 | M4 (two halves), M5a/M5b; the user's test-from-Boot rule was set here |
| `abae63f2` | 18:33-19:39 | Sonnet 5.5 | M6, M7 |
| `47ba1452` | 19:46-21:29 | Sonnet 5.5 | M7.5, M7.6 |
| `58b753f1` | 21:31-22:33 | Sonnet 5.5 | M7.7, M8 |
- Model: `f92fefd` Claude Sonnet 5.5; M0 work itself was Sonnet 5.5 (session `489b827c`, until 14:19); `480dc6a` and `150865a` carry an Opus 5.5 trailer because Opus took over at 14:19 (transcript)
| `bb035825` | 13:11-15:06 | Sonnet 5.5 | M8.6 |
| `1af0887c` | 15:19-16:22 | Sonnet 5.5 | console-error fix + art scale test (painted backdrop, Chaser) |
| `bd0d9026` | 16:23 | Sonnet 5.5 | merge of `Docs/CLAUDE.new.md` into CLAUDE.md |
| `745ad93a` | 16:25-18:25 | Sonnet 5.5 | scale test fixes, doc merges |
| `3cb55dc3` | 18:34-00:07 | Sonnet 5.5 (one 5.5-hour session, 1167 model turns) | C1, scale lock, UI1, CC1, M9a, M9b, M9c, M9d |
| `9a558f0e` | 09-30 09:00-12:06 | Sonnet 5.5 (to 09:28) then **Fable 5.1** (09:28-10:44) then Sonnet 5.5 | docs rule and Skirmisher art (Sonnet); **M10 planning and first implementation (Fable 5.1)**; rest of M10 and P1 (Sonnet) |
| `760a494d` | (ends 12:06) | duplicate of the previous session's turns | looks like a resumed/forked copy of `9a558f0e` [VERIFY]; counted once |
| `7ff0c1b1` | 09-30 12:15-14:52 | Sonnet 5.5 (plus Haiku for a no-op wait agent) | UI2, PF1 (this session) |

Findings that change the attribution above:
- Model: Claude Opus 5.5 (`c52770a`, `2addb41`, and untagged `5b356f7`: transcript shows Opus from 14:19, so it is Opus work)
- **M2 was Sonnet 5.5.** The Opus-for-M2 guess in the VERIFY note below was wrong: session `53252171` is Sonnet only. `5b356f7` (WIP M1) was made after the switch to Opus, so it is Opus work.
- **M10 was split across models.** The commit trailer for `1a79436` says Sonnet 5.5, but the M10 prompt was sent in the same minute the author switched to Fable 5.1, and Fable 5.1 produced about 340 model turns (09:28 to 10:44 local) before the author switched back to Sonnet 5.5 (`/model`, 10:44) for the rest of M10 and the P1 tooling. [VERIFY] which Pumpking files were written in the Fable window (the transcript has the tool calls).
- **No Opus after M1.5.** Opus 5.5 appears only in the tail of the M0/M1 session, in M1 and in M1.5. Sonnet 5.5 did everything else, including UI2 and PF1.
- Assistant-message records per model (excluding the duplicate session `760a494d`): Sonnet 5.5 5,331, Opus 5.5 249, Fable 5.1 340 (Haiku 2, a no-op helper agent). [VERIFY] these are record counts, not tokens or hours; recount with `grep -o '"model":"[^"]*"' <file> | sort | uniq -c`.
- Why the switches: the transcripts record the switch, not the reason [VERIFY: ask the author; likely capability/cost or trying the new model on the boss, the biggest single design task].


---

## Setup

### Setup - Unity project, repo, rules
- Date: 2026-09-28 (13:23 to 13:46)
- Commits: `c581c13`, `bececa0`
- Model: none (human)
- Built: Unity 6.3 project, `.gitignore`, first `CLAUDE.md` (103 lines of design and working rules), then a rule requiring approval for commits and pushes.
- Problems and fixes: none recorded.
- Tests / verification: none.
- Notes: the first commit already holds 6510 lines of inserted content (project template plus `CLAUDE.md`).

## M0 - Project skeleton
- Model: Claude Sonnet 5.5 (session `53252171`; `8cefcd0` is untagged because the author committed by hand)
### M0 - Skeleton, Boot/Game scenes, Gameplay action map
- Date: 2026-09-28 (14:11 to 14:52)
- Commits: `f92fefd` (Unity Pipeline package for CLI Editor control), `480dc6a` (skeleton), `150865a` (tick in CLAUDE.md)
- Model: `f92fefd` Claude Sonnet 5.5; `480dc6a` and `150865a` Claude Opus 5.5
- Built: folder layout, Boot and Game scenes, Gameplay action map and generated Input Actions C# class, plus the `com.unity.pipeline` package so the agent could drive the Editor from the CLI.
- Problems and fixes: none recorded.
- Tests / verification: CLAUDE.md working agreement requires a Console compile check; result not recorded. [VERIFY]
- Notes: `8800221` ("Updated controls", human, 14:36) rewrote the Controls section of `CLAUDE.md` (+34/-22) before M1 started.

## M1 - Movement and arm selection

### M1 - Soft select, L3 lock, free aim
- Date: 2026-09-28 (14:37 to 15:04)
- Commits: `5b356f7` (WIP, untagged), `c52770a`, `2addb41`, `6d96071` (human: "M1 done; plan arm loadout system")
- Model: Claude Opus 5.5 (`c52770a`, `2addb41`); `5b356f7` untagged [VERIFY agent-made]
- Built: player movement on the right stick, arm selection with hysteresis (8 degrees at slice boundaries), L3 lock with free 360 degree aim, unlock returns the arm to its home slot, debug overlay with none/soft/locked state. Soft select was a "pie" split over owned arms; soft = cyan indicator, locked = yellow plus outline.
- Problems and fixes: decision recorded in the commit body: lock is bound to L3 (`leftStickPress`) instead of R3. The pie-of-owned-arms approach later gave way to the 8 fixed direction slots in the spec. [VERIFY when exactly; `6d96071` rewrote 22 lines of `CLAUDE.md`]
- Tests / verification: `ArmSelectorTests` (first added in `5b356f7`, 21 `[Test]` methods today).
- Notes: M0 and M1 were ticked in `CLAUDE.md` by two separate one-line commits at 14:52.

- Model: Claude Sonnet 5.5 (`4b0398d`; session `235077d1`); `9d5fef6` untagged, Sonnet work

### M1.5 - WeaponArmData, loadouts, arms spawned from loadout
- Date: 2026-09-28 (15:36)
- Commits: `4a707ff`
- Model: Claude Opus 5.5
- Built: `WeaponArmData` for the four hand-drawn arm sprites (blue, red, green, purple then), `ArmLoadout` (8 slots), `StartingLoadout` and `DebugLoadout`, arms spawned at runtime from the loadout, arm prefab with ID-colour halo, Scene-view art alignment tool, sprite import settings, debug overlay shows arm name.
- Problems and fixes: replaced the earlier InputTuning "equip all" debug toggle with the loadout choice.
- Tests / verification: loadout ownership test (commit message).
- Notes: 40 files, +1780/-1980 (large deletions are the replaced placeholder code). [VERIFY]

## M2 - Firing

### M2 - R1 fires the selected arm, pooled projectiles
- Date: 2026-09-28 (around 16:05)
- Commits: no dedicated commit; the work appears to be inside `8cefcd0` ("M2 done; M3 design: ammo slots, pickups, etc"), untagged [VERIFY]
- Model: unknown, probably Claude Opus 5.5 [VERIFY]
- Built: R1 (hold = continuous fire) fires the selected arm from its muzzle in slot or aim direction, pooled projectiles, per-arm stats.
- Problems and fixes: none recorded.
- Tests / verification: `FireTimerTests` and `HealthTests` first added in `8cefcd0`.
- Notes: the same commit edits `CLAUDE.md` (+31/-10) with the M3 ammo and pickup design.

## M3a - Ammo

### M3a - Ammo types, slots, heat, pickups
- Date: 2026-09-28 (16:27)
- Commits: `578c096`
- Model: Claude Sonnet 5.5 (first Sonnet feature commit)
- Built: `AmmoTypeData` and four starter types, four face-button ammo slots, `HeatComponent` and overheat per arm, ammo pickups (auto-fill, hold-to-replace, dropped ammo), test pickups in scene.
- Problems and fixes: none recorded.
- Tests / verification: `AmmoSlotSetTests`, `HeatComponentTests` (8 each today).

## M3b - Armaments core

### M3b - ArmInstance, stat calculation, inventories, test effects
- Date: 2026-09-28 (16:41 design/early code, 16:54 milestone)
- Commits: `9d5fef6` (untagged, "Design: game flow, save system, shop and armory" but contains code), `4b0398d`
- Model: Claude Sonnet 5.5 (`4b0398d`); `9d5fef6` untagged [VERIFY]
- Built: `ArmInstance` with armament slots, `ArmamentData` stat modifiers, final stat calculation (base, flat, percent), arm and armament inventories, test arm effects (pierce, burn, stun, ricochet), debug controls, overlay with final stats. Same session added the game flow, save and Shop/Armory design to `CLAUDE.md` (+60/-25).
- Problems and fixes: none recorded.
- Tests / verification: `ArmInstanceTests`, `StatCalculatorTests` (in `9d5fef6`), `ArmamentInventoryTests` (in `4b0398d`).

## M4 - Game flow skeleton

### M4 - Boot, menu, save, state machine, Shop and Armory skeletons
- Date: 2026-09-28 (17:44)
- Commits: `ced81c5`
- Model: Claude Sonnet 5.5
- Built: Boot bootstrapper, Main Menu (Start/Continue/Quit), single-slot JSON save system behind `ISaveSystem`, `GameStateMachine` with a stub round, Round Results, Shop with fixed stock, Armory (select arm, 3 slots, equip), autosave, Continue. Plain skeleton UI, controller navigable.
- Problems and fixes: none recorded.
- Tests / verification: `GameFlowTests`, `ShopArmoryTests`.
- Notes: largest early commit (119 files, +12,244). Less than an hour after M3b.

## M5a / M5b - Combat and rounds

### M5a - Player health, enemy bullets, patterns
- Date: 2026-09-28 (18:07)
- Commits: `295524f`
- Model: Claude Sonnet 5.5
- Built: player health, hitbox and i-frames, Game Over, `BulletPatternData`, pooled enemy bullets, four starter enemies (Grunt, Spinner, Charger, Sniper), enemy test mode, pause, start-button routing.
- Tests / verification: `CombatTests`.

### M5b - Waves, rounds 1-7, coins, difficulty
- Date: 2026-09-28 (18:23; `921ffae` 18:32 is a human design commit)
- Commits: `0e117c7`, `921ffae` ("M5 done; design: refine stage", untagged, `CLAUDE.md` +80/-15, the long enemy/arena/round design)
- Model: Claude Sonnet 5.5
- Built: `WaveData`/`RoundData` for rounds 1-7, wave spawner, coin drops with magnet and round-end collection, `DifficultyCurve`, currency on Round Results, debug round skip.
- Tests / verification: `WaveTests`.
- Notes: 127 files, +4292. The commit message says "M5" (covers M5b). `921ffae` is where the project turned from a skeleton into a "refine" phase (perspective, AI, arena).

## M6 - Front end

### M6 - Menu, Settings, customization, Round intro
- Date: 2026-09-28 (19:00)
- Commits: `9864589`
- Model: Claude Sonnet 5.5
- Built: Main Menu (New Game/Continue/Settings/Quit), `SettingsService` and settings file, cosmetics and profile file with live preview, RoundIntro state with banner and countdown.
- Tests / verification: `M6Tests`.

## M7 - Arena

### M7 - Arena, obstacles, traps
- Date: 2026-09-28 (19:37; `4bfa35d` 19:45 adds concept art and the M7.5 plan)
- Commits: `28a5d2c`, `4bfa35d`
- Model: Claude Sonnet 5.5 (`28a5d2c`); `4bfa35d` untagged (human: concept art, `CLAUDE.md` +68/-11 for perspective rules)
- Built: `ArenaData` with test colosseum, shared collision grid, solid and breakable obstacles blocking movement and all bullets, three telegraphed traps hurting the player and enemies, spawn gates, rounds referencing an arena, simple enemy avoidance.
- Tests / verification: `ArenaTests` (21, tied for the largest file).

## M7.5 - Perspective and HUD

### M7.5 - Y-sorting, footprints, layered arena, combat HUD
- Date: 2026-09-28 (21:07)
- Commits: `ce21bf8`
- Model: Claude Sonnet 5.5
- Built: Custom Axis Y sorting with sorting layers and per-object Sorting Groups at the feet, footprint colliders plus bullet reach on obstacles/enemies/player, separate damage core, layered placeholder colosseum with foreground railing, fixed camera, combat HUD (portrait, hearts, heat bar, ammo slots, glyph library), ammo icons.
- Tests / verification: `PerspectiveTests` (this file is part of the 11-test failure in the M10 entry).
- Notes: 124 files, +11,510.

## M7.6 - Jump

### M7.6 - Fake-height jump
- Date: 2026-09-28 (21:25)
- Commits: `767e3ca`
- Model: Claude Sonnet 5.5
- Built: `JumpController` lifts only the Visuals child on a `JumpTuning` curve; shadow shrink and fade, apex scale, squash, pooled landing dust. Airborne uses a `PlayerAirborne` physics layer (no enemy contact), skips traps and pickups, still hit by bullets unless `jumpDodgesBullets`; Airborne sorting layer; nearest-free-spot push-out.
- Tests / verification: `JumpTests`.

## M7.7 - Arm ring

### M7.7 - Elliptical arm ring, bullet height, low damage core
- Date: 2026-09-28 (21:31 design, 21:51 build and tick)
- Commits: `6c30daa` (design), `2ba5edb`, `d1a53f0`
- Model: Claude Sonnet 5.5 (`2ba5edb`, `d1a53f0`)
- Built: arms on a flattened ellipse around the feet with front/back sorting, locked aim slides along the ring, consistent muzzles and bullet spawn, bullet visual lift with tiny shadow, damage core low on the body, `ArmRingTuning`.
- Tests / verification: `ArmRingTests`.
- Notes: designed and built inside 20 minutes.

## M8 - Enemy AI rework

### M8 - Flow field, steering, line of sight
- Date: 2026-09-28 (22:32)
- Commits: `6fa1892`, `52acd7b`
- Model: Claude Sonnet 5.5
- Built: grid flow field toward the player, local separation, line of sight, Chaser / Skirmisher / Mobile Sentry archetypes, Grunt converted to Chaser, enemies account for the player's jump, rounds retuned.
- Tests / verification: `EnemyAiTests`.
- Notes: end of day one, 29 commits on 2026-09-28.

## Docs refresh

### Docs - refresh
- Date: 2026-09-29 (12:21)
- Commits: `7bfb75e` (untagged)
- Built: `CLAUDE.md`, `ART_SPEC.md` and `BUGS.md` refreshed (+337 lines). This is where the first `BUGS.md` entry appears (the format example "Ricochet bullets pass through low walls").

## M8.5 - Arena progression

### M8.5 - Height classes and arena layouts
- Date: 2026-09-29 (12:46)
- Commits: `34bbe88`
- Model: Claude Sonnet 5.5
- Built: Low/Tall obstacle classes (jump over Low, Tall fades when something is behind it), `ArenaLayoutData` and `HazardBudget` per round with layouts for rounds 1-7, layout validator, debug layout preview, retuned rounds. Also re-ticked M7.7 in `CLAUDE.md`.
- Tests / verification: `LayoutTests` (16 today; part of the failing 11, see M10).

## M8.6 - Animation toolkit

### M8.6 - Procedural motion, shaders, particles, PrimeTween, rig test
- Date: 2026-09-29 (14:34)
- Commits: `64b7eee`
- Model: Claude Sonnet 5.5
- Built: procedural motion and feedback components, shader set (hit flash, dissolve, outline, pulse, tint, UV scroll, wave), particle presets, PrimeTween, 2D Animation + PSD Importer with a rigged `RigTest` scene.
- Problems and fixes: first open bug recorded here: Sprite_Character materials disable 2D SRP batching (console warning). Left open, later deferred to M12 (`a3a8740`).
- Tests / verification: `FeedbackTests`.
- Notes: 157 files, +81,658, mostly third-party package content [VERIFY].

## Art scale test

### Scale test - painted backdrop and Chaser, letterbox, arm ring
- Date: 2026-09-29 (15:14 to 18:24)
- Commits: `c3fc59a` (art, untagged), `15b44f0`, `c393055`, `f366fc7`, `637d92b`
- Model: Claude Sonnet 5.5 (`15b44f0`, `c393055`, `f366fc7`, `637d92b`); `c3fc59a` untagged (human art upload)
- Built: `Tools/export_art.ps1` (50% downscale of 4x masters), backdrop and painted Chaser with feet pivot and footprint collider, `ArenaData` 13.2 x 6.0 to match the painted floor (all layouts remapped), flat-ellipse damage-core marker, `CameraLetterbox` and `SafeAreaFitter`, wider arm ring (F=0.6) with see-through silhouette for back arms, StartingLoadout in slot E, placeholder outline/shadow look, player sprite re-export (302x315, PPU 315), docs for hovering armament bubbles.
- Problems and fixes (all four fixed on 2026-09-29, from BUGS.md history):
  - Yellow disc over the player sprite: it was the damage-core marker drawn above the body; now a flat ellipse behind the body.
  - ArenaData bounds (16 x 9) larger than the painted floor: resized to 13.2 x 6.0, layouts remapped.
  - Magenta square and white circle near pillars: not a shader bug, they were test ammo pickups; pickups and coins now get outline, shadow and muted colours.
  - Backdrop framed with dark bars, HUD cut off: backdrop cropped, camera letterboxed to 16:9, HUD kept inside that rectangle. A Windows 150% scaling side-note: the Game view Scale slider must be lowered.
- Also opened: input-system `NullReferenceException` (not reproduced), FeedbackHub leak, PrimeTween warnings, enemy bullets same red as the floor.
- Tests / verification: visual checks against the painted backdrop; screenshot checks. [VERIFY]

## C1 / Scale lock

### C1 - 1.5x scale bake
- Date: 2026-09-29 (18:47)
- Commits: `dbd3019` (third-party UI pack, 332 files, untagged), `245a1f0`
- Model: Claude Sonnet 5.5 (`245a1f0`); `dbd3019` untagged
- Built: the tested 1.5x character scale baked via import PPU and data (player 315 to 210, arms 400 to 266.67, painted enemy 220 to 146.67, placeholder enemy sizes x1.5); `CharacterScale` and the F5 key removed; ring, muzzles, jump height, footprints, nav radius retuned.
- Tests / verification: layout and ring tests re-run [VERIFY].

### Scale lock - 1.15x chosen
- Date: 2026-09-29 (20:02)
- Commits: `f74d79b` (also contains UI1 and CC1), `2c8407f` (docs: art pipeline P=660 to P=506)
- Model: Claude Sonnet 5.5
- Built: after playtesting 0.75x to 1.5x, character scale locked at 1.15x of the original spec (player 273.9 PPU, arms 347.8, painted enemy 191.3), everything re-baked.
- Notes: a human playtest decision. 218 files, +10,753 in the combined commit.

## UI1 and CC1

### UI1 - UI theme
- Date: 2026-09-29 (20:02)
- Commits: `f74d79b`
- Model: Claude Sonnet 5.5
- Built: `UITheme` asset on `GameConfig`, `ThemedImage` / `ThemedButton`, the dobo "Mega Cozy" demo sprites copied with 9-slice, applied to screens and HUD frames, `Docs/CREDITS.md`, screenshots. (Replaced by UI2 the next day.)

### CC1 - Character Creation v2
- Date: 2026-09-29 (20:02)
- Commits: `f74d79b`
- Model: Claude Sonnet 5.5
- Built: paper-doll player via Sprite Library + Sprite Resolver, `CosmeticPartData` assets (3 placeholder variants per slot), new creation screen (preview, Randomize on Square, "To the Arena!"), profile v2, HUD portrait from head plus accessory 1. Parts show only on the creation screen until `GameConfig.showCustomizationInGame` is on.
- Notes: M6 cosmetics screen superseded.

## M9a - Armament behaviors

### M9a - Effect interface, variable slots, starter armaments
- Date: 2026-09-29 (21:27)
- Commits: `ee19099`
- Model: Claude Sonnet 5.5
- Built: stack-aware `ArmEffect.ModifyShot` with generated descriptions, `ShotProperties`, `armamentSlots` 1-3 per arm, armament icon/rarity/tags/price tier/max stacks, Velocity, Pierce, Homing, Ricochet (walls only), Auto-fire, pure and tested `ProjectileRules` (pierce used up before stop, ricochet counts wall bounces only, homing re-targets after each pierce/bounce), save stores one armament ID per slot, F6 bullet-path visualization, `Docs/ARMAMENTS.md`.
- Tests / verification: `M9aTests` (20).

## M9b - Shop

### M9b - Card-based Shop
- Date: 2026-09-29 (21:55)
- Commits: `5ef5e62`
- Model: Claude Sonnet 5.5
- Built: merchant, stall, 2 arm + 3 armament cards, crate (pick 1 of 3), currency, Reroll/Leave, tooltip with fit and before/after, PrimeTween lift/buy/SOLD, `RarityTable` / `ShopTuning` / `ShopPool` weighted stock and scaling prices, `ShopVisit` (seed, rerolls, sold mask) saved with the run, `Docs/SHOP.md`, F7 debug currency.
- Tests / verification: `M9bTests` (20).
- Notes: 55 files, +7917/-1686.

## M9c - Armory

### M9c - Armory screen
- Model: commit trailer says Claude Sonnet 5.5, but M10 planning and first implementation ran on Claude Fable 5.1 (09:28-10:44 local), then Sonnet 5.5 finished M10 and P1 (see "Models from the session transcripts")
- Commits: `4711107`
- Model: Claude Sonnet 5.5
- Built: arm ring with gladiator on the left, hovering armament bubbles above the selected arm, tabbed inventory grid, arm to bubble to item equip flow, before/after stat preview, dimmed incompatible items, controller navigation; `Docs/ARMORY.md`.
- Tests / verification: `M9cTests` (9).
- Notes: +14,569/-5,226, mostly scene/prefab YAML [VERIFY].

## M9d - UI foundation and bug bash

### M9d - Consistency pass
- Date: 2026-09-30 (00:05)
- Commits: `a3a8740`
- Model: Claude Sonnet 5.5
- Built: shared `ConfirmDialog`, screen transitions, live button prompts (keyboard family added), UI sounds, `Docs/UI_AUDIT.md`, enemy bullet palette, landscape-only phones.
- Problems and fixes (six entries closed in BUGS.md):
  - Enemy bullets same red as the floor: per-pattern red tint and no outline. Now the `EnemyBulletPalette` asset (Electric Violet with white core and dark outline; Hot Magenta for boss and special shots); colours that clashed moved; checked by `EnemyBulletPaletteTests` and an Audit Reserved Hues menu.
  - FeedbackHub piled up `Application.quitting` lambdas each Play session: now a static method subscribed once.
  - PrimeTween "endValue equals current" warnings: tweens skipped when the end value equals the current.
  - Armory ring slot focus drew an opaque disc: now a ring sprite.
  - Phone portrait layout: resolved as a design decision, landscape-only on phones and tablets.
  - Ricochet through low walls: could not reproduce (it was the format example in the header of BUGS.md); logged as verified working with a numeric trace.
  - Input exception: still cannot reproduce; `InputDiagnostics.Raise` added to log context. SRP batcher warning: deferred to M12.
- Tests / verification: `M9dTests` (5), `EnemyBulletPaletteTests` (4); a numeric Ricochet trace; screenshots in `Docs/Screenshots/M9d` and `EnemyBullets`.

## Between M9d and M10

### Docs rule and Skirmisher art
- Date: 2026-09-30 (08:58 to 09:26)
- Commits: `4300774` (art upload, untagged), `fbdc702` (docs maintenance rule in the Working agreement), `d8e48d5` (painted Skirmisher on `Enemy_Weaver`: idle pose, feet pivot, visual-only shot warning pulse)
- Model: Claude Sonnet 5.5 (`fbdc702`, `d8e48d5`)
- Notes: `fbdc702` is the rule that made the agent maintain `CLAUDE.md`, `ART_SPEC`, `ART_CHECKLIST` and `BUGS.md` itself.

## M10 - The Pumpking (round 3)

### M10 - Boss round 3 and P1 partial
- Date: 2026-09-30 (12:04)
- Commits: `1a79436` (M10 and P1 together in one commit, 115 files, +25,976/-18,893)
- Model: Claude Sonnet 5.5
- Built: `BossData`-driven ground boss with phases (Circle Spread, Fast Shot, Jump & Smash; phase 2 at 50% HP with transition, glow, denser rings, chained jumps), `Boss.prefab` variant and `BossPool`, screen-space boss health bar, named intro banner, multi-stage death sequence (`IDeathSequence`), Pause screen debug Boss / HP buttons, `GameConfig.debugStartRound`. Painted idle pose hooked up; windup and attack poses pending.
- Problems and fixes: running the EditMode suite for M10 exposed 11 failing tests (`ArenaTests` 2, `LayoutTests` 6, `PerspectiveTests` 3) because `Obstacle.ApplyContactShadow` calls `GameServices.Ensure()` (and `DontDestroyOnLoad`) outside Play mode. Code untouched by M10; logged as an open bug, not fixed.
- Tests / verification: `BossTests` (4); screenshots in `Docs/Screenshots/M10`.

### P1 - Performance pass (partial)
- Date: 2026-09-30 (12:04, same commit as M10)
- Commits: `1a79436`
- Model: Claude Sonnet 5.5
- Built: overlay (F8), stress test (F10 / `-perfstress`), `PerfLogger` CSV, `Tools/perf_analyze.ps1`, profiler markers (`Assets/Scripts/Perf`, 7 files).
- Result: baseline recorded (uncapped dev build, stress test: about 165 fps average, p99 about 14 ms, occasional 30-70 ms hitches, about 180 GC allocations per frame). Hitch cause not attributed (BH markers explain only 1-3 ms), so fixes and the final test were tabled until content is near complete. Minor tool bug: PerfLogger `canvas_overlay_ms` is garbage.
- Notes: milestone marked `[~]` (partial).

## UI2 - VOX VEGETALLIS UI hot swap

### UI2
- Date: 2026-09-30 (14:17)
- Commits: `799d55b` (704 files, +141,624/-41,764)
- Model: Claude Sonnet 5.5
- Built: VoxVegetallis theme replacing Mega Cozy, TMP fonts (Cinzel Decorative plus Nunito), rebuilt screens (Main Menu, Settings, Character Creation, Shop, Armory, HUD), generated backdrop blur, builders at `BulletHell/Vox/5..9`, Mega Cozy removed.
- Problems and fixes: a long list of mockup differences that could not be matched was logged as one open bug (see the Roadmap). New minor bug found: "Destroy may not be called from edit mode" when stopping Play with live enemy pools (about 30 errors; seen at every Play stop).
- Tests / verification: screenshots of the built screens in `Docs/Screenshots/VoxUI` at 1920x1080 compared with mockups in `Docs/Reference/UI`. [VERIFY automated tests run]
- Notes: largest commit in the repo by far, dominated by font atlases, sprites and scene/prefab YAML [VERIFY].

## PF1 - Portfolio case study

### PF1 - Case study documentation
- Date: 2026-09-30 (this session; uncommitted at the time of writing, `git status` was clean at session start)
- Commits: none yet
- Model: Claude Sonnet 5.5
- Built: `Docs/CaseStudy/` (including this log, `06_Roadmap.md`, `09_Metrics.md`). No code or existing docs changed.
- Tests / verification: metrics in `09_Metrics.md` produced by real commands listed there.
- Notes: this milestone has no `CLAUDE.md` checkbox [VERIFY whether it should].
