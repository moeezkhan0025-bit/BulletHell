# System deep dive 03: Bullet pooling and performance tooling

One pooled projectile type serves the player's arms and every enemy and boss pattern. This file covers the pool, how bullets are simulated, the data that drives enemy volleys, and the tools built to measure it all.

## Problem

The target is "hundreds of bullets on screen at 60 fps" on the weakest platforms (mobile, Switch), with "pooled everything, no per-frame allocations in gameplay loops" (`CLAUDE.md`, Target platforms). A bullet hell creates and destroys bullets constantly, so `Instantiate`/`Destroy` per bullet would produce GC spikes and frame hitches. The rules in `CLAUDE.md` are explicit:

- ALL projectiles (player and enemy) use `UnityEngine.Pool.ObjectPool<T>`.
- Never `Instantiate`/`Destroy` bullets during gameplay.
- Log a warning if any pool grows unexpectedly.

Secondary problems: enemy bullets must be readable (bright core, dark outline) without per-bullet allocation, fast bullets must not tunnel through targets, and performance claims needed numbers rather than feel.

## Design

### One class, one pool

`ProjectilePool` (`Assets/Scripts/Projectiles/ProjectilePool.cs`) wraps `ObjectPool<Projectile>` with collection checks on. The Game scene sets `prewarm: 512` and `maxSize: 2048` (`Assets/Scenes/Game.unity`; the script defaults are 128 and 1024). In `Awake` it `Get()`s `prewarm` projectiles and releases them all, so creation cost is paid at scene load.

- `Create` increments `TotalCreated` and logs `Debug.LogWarning("ProjectilePool grew past its prewarm size ...")` when `TotalCreated > prewarm`. That is the "pool growth warning".
- Every bullet carries a `PoolIndex` into an `active` list; `Release` does a swap-remove, so both `Get` and `Release` are O(1) and the pool can `ReleaseAll()` at the start of a round.
- `OnGet`/`OnRelease` only toggle `SetActive`. `OnDestroyItem` null-checks because on leaving Play mode the scene objects are already gone.

Other pools follow the same pattern and warning: `EnemyPool` (scene prewarm 32, max 128 from `Assets/Data/Waves/CombatTuning.asset`; a second small pool for the boss), `CoinField` (prewarm 64, `CoinTuning.asset`), plus an `AmmoPickup` count warning. [VERIFY: particle and dust pools (`FeedbackHub`, `DustPuffs`) were not inspected for warnings.]

### Launch paths

`Projectile` has two launch methods on the same class:

| Method | Caller | Behaviour |
|---|---|---|
| `Launch(...)` | `ArmFireController.Fire` | Player bullet. Uses `Physics2D.CircleCast` along each step against `hitMask`, supports pierce, ricochet, homing, on-hit effects (see file 04). |
| `LaunchHostile(...)` | `EnemyAttacker.Volley` | Enemy bullet. No physics query: one segment-versus-circle test against the player's hit radius plus `ArenaController.SegmentBlocked` for obstacles and walls. Hurts only the player, ignores enemies, carries no arm effects. |

`HostileStep` is the cheap path and it matters for scale. Enemy volleys are the big numbers (Pumpking dense ring is 24 bullets, `Pattern_PumpkingRingDense.asset`). While the player is invulnerable or dead bullets fly straight through, and obstacles use the grid (`ArenaGrid.SegmentBlocked`), not Box2D.

Both paths sweep a circle along the frame's step, so a fast bullet cannot tunnel. The player-bullet path remembers the last 8 colliders it hit (`recentHits` ring) so a piercing or ricocheting bullet never hits the same target twice.

### Bullet look without allocation

The bullet prefab has a lifted body sprite (`PerspectiveTuning.BulletVisualLift`, 0.25) and a flat shadow on the ground. `Bind` (called once per pooled object) builds three more `SpriteRenderer` children (Fill, Core, Glow) so an enemy bullet is outline, fill, core, glow, which is five renderers with the shadow. All layers are created at pool build time; `LaunchHostile` only sets sprite, colour and scale. Colours come from `EnemyBulletPalette` (Electric Violet for ordinary shots, Hot Magenta for boss and special shots). Player bullets disable the extra layers.

### Enemy volleys are data

The brief lists `BulletPatternData`. No class with that name exists in `Assets/Scripts`; the concept is implemented as `AttackPattern` (`Assets/Scripts/Enemies/AttackPattern.cs`), a ScriptableObject with shape (`Aimed`, `Spread`, `Ring`, `Spiral`), bullet count (max 64 per volley, `MaxBulletsPerVolley`), spread, spiral step, fire interval, initial delay, speed, size, damage, `BulletStyle`, sprite and a lifetime safety net. `AttackPatternMath.FillAngles` is static, allocation-free plain math that writes angles into a reused array. `EnemyAttacker.Volley` loops over the angles and calls `pool.Get().LaunchHostile(...)`. Bosses reuse the same assets (`Pattern_PumpkingRing.asset`: Ring, 16 bullets, speed 4, size 0.34, Hot Magenta style; `Pattern_PumpkingFastShot.asset`: Aimed, speed 11, size 0.36). [VERIFY: whether the name `BulletPatternData` was ever a class: `git log -S` shows it only in `CLAUDE.md` and a UI pack commit.]

### Release conditions

A bullet returns to the pool on a final hit, on leaving `ViewBounds` (the camera view plus `despawnMargin` 1), or when `lifeLeft` hits zero (6 s for player ammo via `AmmoTypeData.maxLifetime`, 10 s default on patterns). A `released` flag makes `ReleaseToPool` idempotent, because a bounce, a pierce and a despawn can all be decided in the same frame.

### Measure first: `Assets/Scripts/Perf`

The P1 milestone was written as "measure first, then fix", and the tooling is the main deliverable so far.

- `PerfMarkers`: `ProfilerMarker`s named `BH.Nav.Update`, `BH.Enemy.Brain`, `BH.Bullet.Update`, `BH.Arms.Fire` and so on. They compile away in release builds and each becomes a CSV column.
- `PerfStats` + `PerfOverlay` (F8): frame time ring of 256 frames, average, p99, max, fps, GC bytes per frame (via `ProfilerRecorder` on "GC Allocated In Frame"), bullets, enemies, particles, pool counts. Built allocation-free; text refreshes at 4 Hz.
- `PerfLogger`: one preallocated float array (max 60000 frames), one row per frame, written to CSV only when it stops, so logging does no I/O while measuring.
- `StressTest` (F10 or `-perfstress`): seeded (`System.Random(1234)`), scripted figure-8 movement, all eight arms firing (`ArmFireController.DebugFireAll`), Homing, Ricochet and Pierce equipped as slots allow, swarm held at a target size, Pumpking forced into phase 2 at 20 s with shortened cooldowns. Player cannot die. Enemy pools are prewarmed first so creation does not pollute the measurement.
- `PerfArgs` switches (`-perfstress`, `-perflabel`, `-perfseconds`, `-perfenemies`, `-perfvsync`, `-perfnoquit`) work only in development builds (`Debug.isDebugBuild`).
- `Tools/perf_analyze.ps1`: percentiles, per-marker cost, worst frames, and `-Compare` for before/after tables.

## Key classes

| Class | File path | Responsibility |
|---|---|---|
| `ProjectilePool` | `Assets/Scripts/Projectiles/ProjectilePool.cs` | `ObjectPool<Projectile>`, prewarm, growth warning, active list, view bounds, `ReleaseAll` |
| `Projectile` | `Assets/Scripts/Projectiles/Projectile.cs` | Pooled bullet: swept movement, hit resolution, hostile layers, release |
| `ProjectileRules` | `Assets/Scripts/Projectiles/ProjectileRules.cs` | Pure pierce/bounce/homing rules (testable without physics) |
| `AttackPattern` | `Assets/Scripts/Enemies/AttackPattern.cs` | Data for one enemy or boss volley (the implemented "bullet pattern") |
| `AttackPatternMath` | `Assets/Scripts/Enemies/AttackPatternMath.cs` | Allocation-free angle generation per shape |
| `EnemyAttacker` | `Assets/Scripts/Enemies/EnemyAttacker.cs` | Timing and volley firing through the pool |
| `EnemyBulletPalette` | `Assets/Scripts/Enemies/EnemyBulletPalette.cs` | Reserved bullet colours, outline, core, glow |
| `EnemyPool` | `Assets/Scripts/Enemies/EnemyPool.cs` | Enemy and boss pools with growth warning and `DebugPrewarm` |
| `CoinField` | `Assets/Scripts/Pickups/CoinField.cs` | Pooled coins with growth warning |
| `PerfHost`, `PerfStats`, `PerfOverlay`, `PerfLogger`, `PerfMarkers`, `PerfArgs`, `StressTest` | `Assets/Scripts/Perf/` | Overlay, CSV logging, markers, command-line switches, stress script |
| `BulletPathDebug` | `Assets/Scripts/Projectiles/BulletPathDebug.cs` | F6 bullet path visualiser, free when off |

## Data flow

```mermaid
flowchart LR
    subgraph Fire[Firing]
      A[ArmFireController.Fire] -->|Launch| P
      E[EnemyAttacker.Volley] -->|AttackPatternMath.FillAngles| E2[angles]
      E2 -->|LaunchHostile per bullet| P
    end
    P[ProjectilePool.Get<br/>ObjectPool: prewarm 512, max 2048] --> B[Projectile.Update]
    B -->|player bullet| C[Physics2D.CircleCast<br/>ProjectileRules: pierce, bounce, homing]
    B -->|hostile bullet| H[ArenaGrid.SegmentBlocked<br/>+ segment vs player hit circle]
    C --> R{stop, offscreen or lifetime?}
    H --> R
    R -->|yes| Rel[ProjectilePool.Release<br/>swap-remove from active list]
    R -->|no| B
    Rel --> P
    P -. growth past prewarm .-> W[Debug.LogWarning]
    B -. BH.Bullet.Update marker .-> M[PerfLogger CSV]
    M --> An[Tools/perf_analyze.ps1]
```

## Trade-offs and alternatives

- **One class for player and enemy bullets.** Shared prefab, pool, despawn rules and visual lift keep the two consistent and halve the pooling code. The price is branching (`hostile`) in `Update` and a prefab that carries renderers a player bullet does not use.
- **Per-bullet `MonoBehaviour.Update`.** Simple and debuggable, but each live bullet costs a managed-to-native `Update` call. The alternative is one manager loop over a struct array (plus batched rendering), which is what a scale beyond a few thousand bullets would need. The measured cost today is `Bullet.Update` around 0.35 ms per frame in the stress test, so the simple design is still affordable. [VERIFY: the bullet count during that measurement is not recorded in the docs.]
- **Physics cast for player bullets, grid maths for enemy bullets.** Player bullets need to hit many enemy colliders and apply effects, so Box2D is used. Enemy bullets only ever test one circle and the grid, so no physics query is made at all.
- **Prewarm of 512 rather than growing lazily.** Costs memory and scene load time and, with five renderers per bullet, non-trivial GameObject count. [VERIFY: the load-time or memory cost of the prewarm was not measured.]
- **`maxSize` semantics.** Unity's `ObjectPool` does not cap creation at `maxSize`; it only destroys released items above it. So a runaway spawner would not be stopped, only reported by the warning (which fires past `prewarm`, not past `maxSize`). This is Unity's documented behaviour, stated here from knowledge of the API rather than from project docs.
- **5-renderer hostile bullets** buy readability (outline, core, glow) at the cost of overdraw and batching pressure. The SRP Batcher warning on the sprite materials means these draw without SRP batching (open in `BUGS.md`).

## What went wrong / lessons

Numbers below are from `Docs/BUGS.md` ("P1 hitches") and the P1 line in `CLAUDE.md`; the tests are development builds with the stress test (80-enemy swarm, Pumpking phase 2, eight arms firing, 90 s, VSync off, 1920x1080 windowed).

| Measure | Baseline |
|---|---|
| Average frame rate | about 165 fps (uncapped) |
| p99 frame time | 13.5 to 14.2 ms |
| Frames over 20 ms per run | 37 in run 1, then 3 to 4 in runs 2 and 3 (worst frames 27 to 74 ms) |
| GPU time | under 1 ms |
| GC allocations | about 180 allocations (about 8 KB) in every frame, source unknown |
| Attributed main-thread cost | Enemy.Brain about 1.0 ms, Nav.Separation about 0.5, Bullet.Update about 0.35, Physics2D about 0.17 |

- **Average looks great, hitches do not.** The average is nearly three times the 60 fps target but occasional 30 to 70 ms frames exist, and the BH markers only explain 1 to 3 ms of each frame. The hitch work sits in main-thread phases that were never timed. Lesson: markers around your own systems do not find cost in engine phases; time the PlayerLoop phases too. That is the first item of the resume checklist.
- **The analyzer misleads.** `wait_gfx_ms` shows as the top marker on the worst frames, but it is the render thread idling for the main thread, a symptom. The bug entry warns about this so it is not chased. The `canvas_overlay_ms` column also reports garbage (a negative counter).
- **Run-to-run variance** (37 versus 3 to 4 slow frames) means the before/after comparison the milestone promised needs several runs per build (`-Compare` exists for this).
- **Fixes were tabled on purpose** until content is nearly complete ("TABLED FOR THE FINAL PERFORMANCE TEST"). The user's own playtests feel fine. The risk is that the hitch cause is now baked into more content by the time it is tackled.
- **A 70 ms player hitstop and frame pacing** are listed as suspects but flagged as design decisions to ask about rather than fix.
- **Pool clean-up noise:** leaving Play mode logs about 30 "Destroy may not be called from edit mode" errors from `ObjectPool<Enemy>.Clear` via Unity's pool manager (open in `BUGS.md`). `ProjectilePool.OnDestroyItem` already guards with a null check, while `EnemyPool` passes `Destroy` directly. [VERIFY: whether that difference is the cause; the bug entry only suggests checking `actionOnDestroy`.]
- **Colour, not only count, made bullets unreadable:** enemy bullets shared the red family with the floor. Fixed in M9d with the reserved-hue palette and outline; the pooled five-layer bullet is the result.

## Open questions

- Where do the 180 allocations per frame come from? Candidates named in `BUGS.md`: DebugOverlay 4 Hz throttle, bullet pool, `Separation` O(n squared), per-frame LOS and path sweeps, homing scans. None confirmed.
- Does the per-bullet `Update` design hold on Switch and mobile? The stress test ran on a desktop PC only. [VERIFY: no device numbers exist in the docs.]
- Bullet size retune (0.2 to 0.3 P per `ART_SPEC`) is still open for the art pass.
- Should the pool enforce a hard cap (drop the oldest or refuse a launch) rather than only warn past prewarm?
- Should enemy bullets move to a manager-driven struct array if the final test misses the 60 fps budget on device?
