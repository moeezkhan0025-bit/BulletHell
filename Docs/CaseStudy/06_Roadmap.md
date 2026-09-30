# 06 - Roadmap

Compiled 2026-09-30 from `CLAUDE.md` (grep of `[TBD`, `[DEFAULT`, unchecked milestones), `Docs/BUGS.md` (open section), `Docs/ART_CHECKLIST.md` and the UI2 mismatch entry. Priorities are SUGGESTIONS, not decisions; the owner decides. Nothing here was changed in the source docs.

Re-run the grep: `grep -n -E '\[TBD|\[DEFAULT|^- \[ \]|^- \[~\]' CLAUDE.md`

Priority key (suggestion): P1 = blocks the next milestone or the vertical slice; P2 = should happen before a public build; P3 = nice to have or long-term.

## 1. Open milestones (CLAUDE.md "Milestones")

| Milestone | State | Quote | Suggested priority |
|---|---|---|---|
| Vertical slice art for one arena | `[ ]` | "Vertical slice art for one arena (Docs/ART_SPEC.md section 9)." | P1 (gates M11) |
| M10 (rounds 5 and 7) Bosses | `[ ]` | "M10 (rounds 5 and 7) Bosses." | P1 |
| P1 Performance pass | `[~]` partial | "PARTIAL. Tools built ... fixes and the final test are tabled until content is near complete" | P2, resume when content is near complete |
| M11 Themed UI/visual pass | `[ ]` | "candy-colosseum style for menus, HUD, Shop, Armory, customization; final art." | P2 (UI2 already did a large part of the menus) [VERIFY how much of M11 UI2 covers] |
| M12 Polish | `[ ]` | "touch controls (incl. jump button), button glyphs, juice, announcer/audio, performance pass" | P2 to P3 |

## 2. Open design questions: every `[TBD]` in CLAUDE.md

| Section | Line | Quote | Suggested priority |
|---|---|---|---|
| Target platforms | 106 | "A separate Touch control scheme for mobile: [TBD - designed at M12, e.g. virtual left stick for aim, virtual right stick for movement, on-screen fire/lock/ammo buttons]." | P3 (decided at M12) |
| Game flow | 133 | "Boss rounds: 3, 5, 7. After round 7: [TBD - e.g. boss every 2 rounds / endless scaling / game ends]." | P1 (decides whether a run ends and so what the meta loop is) |
| Game flow | 136 | "[TBD: also allow the Armory between waves inside a round?]" | P3 |
| Gladiator customization | 145 | "[TBD: how new cosmetics are unlocked - all unlocked for now]" | P3 |
| Save system | 155 | "Game Over deletes the run save (roguelike). [TBD: any permanent meta-progression, saved separately]" | P2 (ties to the after-round-7 decision) |
| Bosses | 369 | "Rounds 5 and 7: still the placeholder boss wave. [TBD: their bosses.]" | P1 |

## 3. Current defaults that could be revisited: every `[DEFAULT]` in CLAUDE.md

These are working defaults, not open problems. Listed so the owner can confirm or change each after playtesting.

| Section | Line | Quote | Suggestion |
|---|---|---|---|
| Controls (Locked) | 183 | "Arm rotation speed when locked: [DEFAULT: instant snap to stick angle; tunable turn speed later]." | Consider a turn-speed field in `InputTuning` once playtests show whether snap feels good. |
| Armaments (Auto-fire) | 220 | "at [DEFAULT 50%] fire rate. Heat still applies." | Tune after the Armory/Shop are playtested with several Auto-fire arms. |
| Ammo pickups | 241 | "Picking up an ammo type already carried: [DEFAULT: does nothing / stays on the ground]." | Keep unless players report confusion. |
| Player health | 245 | "[DEFAULT: 5 HP]" | Matches the HUD (5 hearts); any change needs HUD work. |
| Jump | 254 | "[DEFAULT: air control 100%, tunable]" | Leave. |
| Jump | 258 | "STILL HIT BY enemy bullets [DEFAULT - toggle jumpDodgesBullets = false, keeps it a bullet hell]" | Leave; toggle exists. |

## 4. Content and gameplay

- Bosses for rounds 5 and 7 (P1). Round 3 (Pumpking) is done as the pattern: `BossData`, phases, `Boss.prefab` variant, death sequence. Each new boss likely needs its own `BossData`, `EnemyData`, patterns, painted poses and a layout. [VERIFY effort]
- Pumpking windup and attack poses: "windup/attack poses fall back to idle until drawn" (CLAUDE.md, Bosses).
- Post-round-7 flow: boss every 2 rounds, endless scaling or an ending (see the TBD above). Suggestion: decide before building the bosses for 5 and 7 so their difficulty fits the curve.
- Enemy roster: Charger and Sniper variants, Mobile Sentry and Skirmisher exist; painted art exists for Chaser and Skirmisher only (see art gaps). Sentry, Charger, Sniper are still placeholder shapes per `ART_CHECKLIST.md` section B [VERIFY].
- Optional Shop services "later": sell an armament, remove/upgrade (CLAUDE.md, Shop).
- More ammo types "Later: Tracking, Automatic, more" (CLAUDE.md, Ammo types).
- Enemy bullet size retune (0.2 to 0.3 P) "is still open with the art pass" (BUGS.md, fixed entry, closing note).
- Merchant art: drawn in parts and rigged (CLAUDE.md, Animation approach). Currently the old placeholder olive.
- Cosmetics are "visual only" and all unlocked; unlock rules are a TBD above.

## 5. P1 Performance resume checklist (from BUGS.md "P1 hitches")

Baseline: uncapped dev build, stress test about 165 fps average, p99 13.5 to 14.2 ms, 3 to 37 frames per 80 s run over 20 ms (worst 27 to 74 ms), GPU under 1 ms, about 180 GC allocations (about 8 KB) per frame. Cause not attributed.

Checklist (verbatim intent):
1. Time the PlayerLoop phases (Update / LateUpdate / FixedUpdate / renderer update / canvas) and find the per-frame allocations.
2. Then the suspects from the P1 survey: `DebugOverlay` 4 Hz throttle, bullet pool and 5-renderer bullets, `Separation` O(n^2) and per-frame LOS / path sweeps, homing scans, the SRP-batcher `_MainTex` property, frame pacing, and the 70 ms player hitstop (design decision: ask).
3. Re-run 3 times and compare with the analyzer `-Compare`.
4. Fix the `PerfLogger` `canvas_overlay_ms` column (reports a negative counter).
5. Fill in the "after" column in `09_Metrics.md`.

Run command (from CLAUDE.md P1 line): build a Development player to `Builds/Perf`, then `BulletHell.exe -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -perfstress -perflabel X -perfseconds 90 -perfenemies 80 -perfvsync 0`. Target (CLAUDE.md): steady 60 fps, no visible hitches in the stress test, pooled everything, target 60 fps with hundreds of bullets on the weakest platform.

## 6. M11 and M12 details

M11 Themed UI/visual pass (suggestion: re-scope after UI2, see section 9):
- Candy-colosseum style for menus, HUD, Shop, Armory, customization, and final art.

M12 Polish, from the CLAUDE.md line:
- Touch controls including a jump button (Touch scheme is still a TBD; landscape-only is decided).
- Button glyphs (PlayStation / Xbox / Nintendo / touch sets through the `ButtonGlyph` lookup; a keyboard family already exists per M9d).
- Juice, announcer and audio (audio is a stub in Boot; UI sound slots are wired since M9d).
- Performance pass, which "also covers the 2D SRP Batcher warning on Mat_SpriteCharacter / Mat_SpriteOutline: _TexelSize / _ST properties in the Sprite Unlit graphs".
- Controller haptics and light bar: "not in scope until M12" (CLAUDE.md, Controls).
- Platform targets: Steam first; Mac/Linux, iOS, Android, Switch and Xbox later, consoles "need platform approval + Unity Pro" (CLAUDE.md, Target platforms). Platform-specific code must stay in `Scripts/Platform/` (currently 2 files).

## 7. Open bugs (Docs/BUGS.md "Open", 6 entries)

| Bug | Severity | Suggested priority |
|---|---|---|
| 11 EditMode tests fail outside Play mode: `Obstacle.ApplyContactShadow` calls `GameServices.Ensure()` (ArenaTests 2, LayoutTests 6, PerspectiveTests 3). Fix idea in the entry: pass `PerspectiveTuning` through `Setup`, or skip `DontDestroyOnLoad` when not playing. | minor | P1 (cheap, and restores a trustworthy test suite) |
| "Destroy may not be called from edit mode" logged when leaving Play mode: about 30 errors from `ObjectPool<Enemy>.Clear` via Unity's pool manager. | minor | P2 |
| P1 hitches: occasional 30 to 70 ms frames in the stress test, cause not attributed (tabled for the final performance test). | minor | P2 (see section 5) |
| Input System `NullReferenceException` in `InputEvent.get_handled` (Editor update), cannot reproduce, monitoring. Diagnostics (`InputDiagnostics.Raise`) are in place. | minor | P3, reopen only if it returns |
| Sprite_Character materials disable 2D SRP batching (console warning), deferred to M12. | minor | P3 (M12 performance pass) |
| UI2 mockup differences that could not be matched ("WORK THROUGH OVER TIME"), see section 9. | minor | P3, in pieces |

Also noted in the entries: PerfLogger `canvas_overlay_ms` is garbage (tool bug, not a separate entry).

## 8. Art checklist gaps (Docs/ART_CHECKLIST.md, summary level)

Goal of that file: "everything needed so one full run from the Main Menu through the round 3 boss uses final art"; estimate about 250 to 300 sprites in total. Line-level counts from `grep -c '\[x\]'` and `grep -c '\[ \]'` (a line can hold several checkboxes, so read these as approximate): 15 lines with `[x]` and 74 with `[ ]`.

| Section | Done lines | Open lines | Note |
|---|---|---|---|
| A Player | 2 | 4 | Body and 4 arms done; other poses open |
| B Enemies | 2 | 4 | Chaser and Skirmisher idle done; windup/attack poses open |
| C Projectiles | 0 | 9 | |
| D VFX | 0 | 13 | Mostly particle textures |
| E Pickups | 0 | 3 | |
| F The stadium | 0 | 9 | Painted backdrop is a scale-test image, not layered final art |
| G Obstacles | 0 | 6 | |
| H Traps | 0 | 3 | |
| I Combat HUD | 4 | 4 | Covered partly by UI2 kit assets |
| J Menus and screens | 7 | 19 | UI kit (panels, buttons, tooltip, fonts) covered by UI2 |

Suggested order from the checklist itself: scale test (done), combat look (stadium floor, walls, foreground, obstacles, traps, enemies, bullets, HUD), boss poses, screens, polish. Art rules in CLAUDE.md that the vertical slice must follow: layered arena (floor, back wall/crowd, side walls, obstacles, foreground), bullets that pop against the floor, feet pivots.

## 9. UI2 mismatch list (Docs/BUGS.md, "UI2 mockup differences", all minor)

Screenshots: `Docs/Screenshots/VoxUI`; mockups: `Docs/Reference/UI`.
- Backdrop: generated blur of `arena01_backdrop.png`, softer and more orange than the mockup's bokeh.
- Main Menu: focused button same size as others; version label shows `v` + `Application.version`; hint pill uses text prompts, not glyph icons.
- HUD: heat bar one blended tint (mock: three fixed zones); hold-to-replace ring is the old placeholder; glyph badges are the old discs.
- Shop: detail panel is title plus plain lines (no rarity/tag chips, "Fits" box or aligned before/after rows); merchant is the old placeholder olive; cards 0.92x the mock size; extra Reroll/Leave/Main Menu/bag buttons.
- Armory: ring smaller than the mock (about 242 px vs 380 px); filled slots show the arm beside a medallion; stat strip is one text panel; 4 portrait-card columns (mock: 3 landscape); generated dashed ring texture.
- Character Creation: eight placeholder arms around the doll (mock: one), front arms overlap the Randomize button; extra Main Menu button; soft glow spotlight.
- Settings: sliders step rather than follow the click; Video tab PC-only; extra Back button; fixed "Changes apply instantly" note.
- Fonts: static atlases, missing glyphs fall back to Liberation Sans.
- Old setup scripts `M4Setup..M9dSetup`, `Cc1Setup`, `M75Setup` still hold old skeleton builders: "do not re-run the old screen builders". Suggestion: delete or fence them (P2, avoids accidental re-runs).
- Retired art in docs: `Docs/Screenshots/UI1` still shows the removed Mega Cozy pack; "Delete the folder when convenient." Note `Docs/CREDITS.md` should be checked for Mega Cozy lines [VERIFY].

## 10. Suggested ordering (suggestion only)

1. Fix the 11 failing EditMode tests (small, restores CI confidence).
2. Decide the after-round-7 flow and meta-progression (two TBDs that shape everything else).
3. Bosses for rounds 5 and 7, reusing the Pumpking framework.
4. Vertical-slice art pass for one arena and enemies.
5. Resume P1: phase timing, allocation hunt, re-run and fill the Metrics "after" columns.
6. Retire old setup scripts and screenshots; chip away at the UI2 list.
7. M12: touch scheme, glyphs, audio/announcer, SRP batcher, haptics.
