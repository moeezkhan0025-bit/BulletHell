# System deep dive 02: Fake-height jump and obstacle height classes

A jump in a game whose physics is flat 2D: the player's root never leaves the ground plane, a single number drives how high the picture is drawn, and the rest of the game answers one question ("is the player airborne, and high enough?").

## Problem

The game is a 3/4 top-down bullet hell where "gameplay stays on the flat 2D XY plane. No 3D, no height physics" (`CLAUDE.md`). The player still needs a jump (R2) that is a real tactical tool:

- pass over enemy bodies, charger dashes, floor traps and hazard zones;
- pass over Low obstacles (low walls, crate rows), so they work as cover that can be vaulted;
- stay blocked by Tall obstacles (pillars, statues) and the arena boundary;
- **still get hit by enemy bullets** (default), because dodging by jumping would end the bullet-hell tension;
- look right in an oblique view: the body rises, the shadow stays on the floor, sorting does not break.

A real height axis would mean 3D collision, sorting against a moving Z, and every enemy and trap reasoning about it. The project rejected that up front.

## Design

### Height is a visual value, consumed by a few rules

- `JumpTimeline` (plain C#) owns timing: ground, airborne for `airtime`, cooldown, ground. `Progress01` runs 0 to 1 over the airtime. `Tick` returns true on the frame the jump lands. No double jump (`CanStart` needs not airborne and no cooldown).
- `JumpController` turns progress into `Height01 = tuning.HeightAt(progress)` and `Height = Height01 * MaxHeight`. It lifts only the `Visuals` child of the player (`visuals.localPosition = (0, Height + BodyCenterHeight * scale.y, 0)`). The root transform, footprint and ground shadow never move.
- Everything else reads three properties: `IsAirborne`, `Height`, and `ClearsLowObstacles` (`IsAirborne && Height >= LowClearHeight`).

### Values (from `Assets/Data/Player/JumpTuning.asset`)

| Field | Value | Meaning |
|---|---|---|
| `airtime` | 0.45 s | whole jump |
| `maxHeight` | 1.035 | world units of body lift at apex (0.9 x the 1.15 scale lock) |
| `heightCurve` | keys (0,0) slope 4, (0.5,1), (1,0) slope -4 | symmetric arc, steep takeoff |
| `cooldownAfterLanding` | 0.2 s | |
| `airControl` | 1 | full steering in the air |
| `jumpDodgesBullets` | false | bullets still hit |
| `apexScale` | 1.12 | slight scale-up at the top |
| `takeoffSquash` / `landingSquash` | (1.2, 0.8) for 0.1 s / (1.3, 0.7) for 0.14 s | |
| `shadowScaleAtApex` / `shadowAlphaAtApex` | 0.6 / 0.45 | shadow shrinks and fades with height |
| `airborneSortHeight` | 0.08 | above this (as a fraction), body moves to the Airborne sorting layer |
| `lowClearHeight` | 0.45 (capped at 0.95 x `maxHeight`) | minimum lift to pass over Low obstacles; `ART_SPEC` wants the apex to lift at least 0.7 P |

By my arithmetic from the curve (a Hermite segment, h(u) = 2u - u^2 on the rising half), the body is above 0.45 world units from about 12 percent to 88 percent of the airtime, roughly 0.34 s of the 0.45 s jump. [VERIFY: computed by hand from the curve keys, not measured in game.]

### Which rule switches, and how

| Rule | Mechanism | Where |
|---|---|---|
| No contact with enemy bodies or dashes | Root `gameObject.layer` switches `Player` to `PlayerAirborne`. The 2D collision matrix has Enemy x PlayerAirborne off (decoded from `ProjectSettings/Physics2DSettings.asset`: Enemy collides with Enemy, Obstacle and Player, not PlayerAirborne). | `JumpController.SetAirborneRules` |
| No contact damage, no trap damage, no pickup grab | Code checks `PlayerHealth.IsGrounded` / `IsAirborne`. | `EnemyAgent` (contact), `Trap` (`HurtsPlayer && player.IsGrounded`), `AmmoPickupCollector` |
| Bullets still hit | `PlayerHealth.CanBeHit` is `alive && not invulnerable && !(IsAirborne && dodgesBulletsInAir)`; `dodgesBulletsInAir` comes from `jumpDodgesBullets`. | `PlayerHealth.SetAirborne` |
| Low obstacles passable | `PlayerMover` picks the blocking grid per frame: `arena.TallGrid` when `jump.ClearsLowObstacles`, else `arena.Grid`. | `PlayerMover.Update` |
| Tall obstacles and walls still block | `TallGrid` holds only Tall obstacles and the arena border. | `Obstacle.Register` |
| Sorting | `SortingGroup` moves to the `Airborne` sorting layer when `height01 > airborneSortHeight`; the group's transform is at the feet, so depth order is still by the shadow position. | `JumpController.ApplyLook` |
| Enemies chase the shadow | `NavigationService` and `EnemyAgent` use the player's feet, which stay on the ground. | `AI/NavigationService.cs`, `AI/EnemyAgent.cs` |

The project notes say "collision rules use physics layers / layer masks switched while airborne, not per-object special cases". The real implementation is a hybrid: the physics layer switch covers enemy bodies, but traps, contact damage, pickups and obstacles are each answered by a cheap flag or by choosing a different grid. That is still centralised (one `SetAirborneRules`), but it is not purely a mask.

### Two parallel collision representations for obstacles

Obstacles register into three `ArenaGrid` instances (`Assets/Scripts/Arena/ArenaGrid.cs`), plus a Physics2D collider:

1. `Grid`: the plain footprint, used for walking (player and enemies) and landing checks.
2. `TallGrid`: footprint of Tall obstacles only, used when the player is high enough.
3. `BulletGrid`: footprint plus a vertical "reach" above it, used for enemy bullets (`ArenaGrid.SegmentBlocked`). Its bounds extend `bulletHeadroom` (1.0) above the floor.
4. A `BoxCollider2D` or ellipse `PolygonCollider2D` covering footprint plus reach, used for player bullet casts.

Reach is per height class (`PerspectiveTuning.BulletReachFor`): Tall 0.35 (`obstacleBulletAllowance`), Low 0.2 (`lowObstacleBulletAllowance`). Both classes stop every bullet; the allowance models that the art stands above the flat footprint. The jump never changes bullet blocking, so a jumping player cannot use a Low wall to shoot over it. [VERIFY: "player bullets are fired from the ground muzzle so they cannot pass over either" follows from `ArmSelectionController.GroundMuzzle`, but was not tested.]

### Height classes (Low / Tall)

`ObstacleHeightClass { Low, Tall }` in `ObstacleData` (`Assets/Scripts/Arena/ObstacleData.cs`). `Docs/ART_SPEC.md` section 3 sets the drawing rules: Low is up to 0.5 P, Tall is 1.5 P and up, with no 0.5-1.5 P in between so the player can tell at a glance. Tall obstacles fade to 40 percent when a character is behind them (`TallObstacleFader` with `FadeRule.IsBehind`, plain math in a static class, fade 0.15 s, 0.25 side margin); Low ones never fade. `HazardBudget` counts Low walls separately when picking layouts.

### Landing

`JumpController.Land` moves the root to `ResolveLanding`, which calls `LandingResolver.Resolve(position, isBlocked, step, 6)`: the position itself if free, else the nearest free sample in expanding rings (step = arena cell size, max 6 units). "Taken" means inside the arena grid (`Grid.CircleBlocked`, so Low obstacles count again once grounded) or overlapping a live enemy footprint (`Physics2D.OverlapCircle` into a static 24-element buffer). Nothing free within 6 units: the player stays put.

## Key classes

| Class | File path | Responsibility |
|---|---|---|
| `JumpController` | `Assets/Scripts/Player/JumpController.cs` | Input, height, squash, shadow, layer switch, sorting layer, landing |
| `JumpTimeline` | `Assets/Scripts/Player/JumpTimeline.cs` | Pure timing: airborne, progress, cooldown, cancel |
| `JumpTuning` | `Assets/Scripts/Player/JumpTuning.cs` | All jump numbers and the height curve |
| `LandingResolver` | `Assets/Scripts/Player/LandingResolver.cs` | Nearest free spot search in growing rings |
| `PlayerHealth` | `Assets/Scripts/Player/PlayerHealth.cs` | `IsAirborne`, `IsGrounded`, `CanBeHit` (bullet dodge toggle) |
| `PlayerMover` | `Assets/Scripts/Player/PlayerMover.cs` | Chooses `Grid` or `TallGrid` by `ClearsLowObstacles`; air control |
| `PlayerVisualRig` | `Assets/Scripts/Player/PlayerVisualRig.cs` | Root at feet, `Visuals` child, shadow, flat damage-core marker |
| `ArenaGrid` | `Assets/Scripts/Arena/ArenaGrid.cs` | Cell grid for movement, tall-only and bullet blocking |
| `Obstacle` / `ObstacleData` | `Assets/Scripts/Arena/Obstacle.cs`, `ObstacleData.cs` | Footprint, height class, breakable stages, grid registration |
| `TallObstacleFader` | `Assets/Scripts/Arena/TallObstacleFader.cs` | Fade Tall obstacles with something behind them |
| `PerspectiveTuning` | `Assets/Scripts/Arena/PerspectiveTuning.cs` | Bullet reach per class, fade values, bullet lift, shadow flatness |
| `BossJump` | `Assets/Scripts/Bosses/BossJump.cs` | The same fake-height idea for the Pumpking (reuses `JumpTuning`/timeline) |
| `SortingLayers` | `Assets/Scripts/Core/SortingLayers.cs` | Background, Ground, Default, Airborne, Bullets, Foreground |

## Data flow

```mermaid
flowchart TD
    A[R2 / Space pressed] --> B{JumpController.OnJumpPressed<br/>state RoundIntro or Combat, alive}
    B -->|timeline.TryStart fails| X[ignored: airborne or cooldown]
    B -->|ok| C[SetAirborneRules true]
    C --> C1[root layer Player -> PlayerAirborne]
    C --> C2[PlayerHealth.SetAirborne: IsGrounded false]
    T[Update: timeline.Tick dt] --> H[Height01 = HeightAt progress<br/>Height = Height01 x maxHeight]
    H --> V[ApplyLook: Visuals lifted + squash<br/>shadow scale/alpha, sorting layer]
    H --> M{Height >= lowClearHeight?}
    M -->|yes| G1[PlayerMover uses TallGrid<br/>Low obstacles pass]
    M -->|no| G2[PlayerMover uses Grid<br/>all obstacles block]
    C1 --> E[Enemy bodies and dashes: no physics contact]
    C2 --> F[Contact damage, traps, pickups skip grounded-only checks]
    T -->|progress >= 1| L[Land: SetAirborneRules false]
    L --> R[LandingResolver.Resolve: nearest free spot<br/>Grid + enemy footprints]
    R --> D[dust puff, landing squash, Landed event]
    H -. ArmSelectionController.LateUpdate .-> W[Arm ring anchor rises with Height]
```

## Trade-offs and alternatives

- **Flags and grids vs one physics matrix.** A strict "switch layer masks" design would be uniform, but traps are overlap zones checked in code, pickups are proximity checks, and obstacles are grid-based. Using `IsGrounded` in those places is cheap and explicit. The cost is that any new ground-only hazard must remember to check `IsGrounded`. A comment in `JumpController` documents the contract, but there is no test that catches a forgotten check. [VERIFY: `JumpTests` covers timeline, arc and landing only.]
- **Two movement grids instead of per-obstacle checks.** Choosing `Grid` or `TallGrid` per frame is O(1) and needs no per-obstacle branching. The cost is memory (one more grid per layout) and keeping the three grids in sync when a breakable breaks (`Obstacle.Break` clears the owner from all three).
- **Height threshold for passing Low obstacles.** The player clears Low obstacles for the middle of the arc rather than for the whole jump. That matches how it looks (feet above the wall) and means takeoff and landing right beside a wall are blocked, which is why the landing push-out exists. An alternative, "airborne means can pass Low", would make a player slide through a wall they visually have not cleared.
- **Bullets still hit.** Keeps the genre's rules, but the jump is then mainly a positioning and trap tool, not a defensive one. The toggle exists (`jumpDodgesBullets`) so the decision can be revisited.
- **Sorting by the shadow.** The group transform stays on the ground so the jumping body is ordered by where it will land. The Airborne layer puts it above characters but below bullets and foreground. A jumping player can therefore draw over a Tall obstacle that they are in front of in world space, which looks right, and the fader uses the ground position so the fade does not flicker. [VERIFY: the second half of this sentence is my reading of `TallObstacleFader`, which uses `player.transform.position`.]

## What went wrong / lessons

- **Yellow disc on the player sprite (fixed 2026-09-29, `Docs/BUGS.md`).** The damage-core marker was a round disc at sort order 10, above the body, and it stayed on the ground while the body rose in a jump. It is now a flat ellipse with the same flatness as the shadow, at order -5/-6, hidden by the body while standing and visible under it in the air. Lesson: anything that marks a ground-plane fact (shadow, damage core) has to be drawn as a ground ellipse, or it reads as a sticker on the body.
- **Height is tuned in world units, so the scale lock touched it.** `maxHeight` 1.035 is 0.9 x 1.15. When character scale was locked at 1.15x (commit `f74d79b`), jump height was re-baked alongside footprints and nav radius. `lowClearHeight` (0.45) is an absolute world unit and was not tied to the obstacle art in data. [VERIFY: whether `lowClearHeight` was re-baked with the scale lock is not stated in the docs.]
- **`CLAUDE.md` says "apex lifts body >= 0.7 P".** `maxHeight` 1.035 satisfies it if P is about 1.15 [VERIFY: P's current value in world units is defined in `ART_SPEC.md`, not cross-checked here].
- **EditMode tests failed outside Play (open in `BUGS.md`).** `Obstacle.ApplyContactShadow` calls `GameServices.Ensure()`, which calls `DontDestroyOnLoad` in edit mode. 11 tests (`ArenaTests`, `LayoutTests`, `PerspectiveTests`) fail. Lesson: a presentation detail (contact shadow) reached for a global service and broke the testability of obstacle setup, which is exactly what the height-class logic needs tests for.
- **Jump was added after the arena (M7.6 after M7.5) and the boss reuses it (M10).** `BossJump` builds on `JumpTuning`, and the boss can be hit in the air through a `HittableInAir` flag. Having the tuning asset separate from the controller is what made that reuse cheap.

## Open questions

- Should Low obstacles be jumpable by enemies later ("a jumping enemy type may come later")? The flow field would need a third grid.
- Should `jumpDodgesBullets` ever be enabled for a mode, difficulty or armament? It exists as a global toggle only.
- No test asserts the physics-layer collision matrix. A one-line edit-mode test on `Physics2D.GetIgnoreLayerCollision` would guard it. [VERIFY: whether any test does this already.]
- The jump has no coyote time or input buffer; with a 0.2 s cooldown a jump pressed 0.1 s early is dropped. [VERIFY: no buffering found in `JumpController`; playtest feel not documented.]
- Touch: a jump button is planned for M12.
