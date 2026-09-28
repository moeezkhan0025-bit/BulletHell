# Project: Bullet Hell (working title)

Roguelike arcade bullet hell, top-down 2D. Developed and playtested on Windows with a PS5 DualSense.
Unity 6.3 LTS (6000.3), 2D URP, new Input System. Solo developer. I playtest every change myself.
Project root: C:\Dev\BulletHell. Version control: Git (GitHub private repo), shell: Git Bash.

## Target platforms
Steam (Windows first; Mac/Linux later), iOS, Android, Nintendo Switch, Xbox.
Consoles come later (need platform approval + Unity Pro), but the code must be console-ready from day one:
- Input bindings use GENERIC gamepad paths (<Gamepad>/rightShoulder, <Gamepad>/leftStickPress,
  <Gamepad>/buttonSouth...), never DualSense-only paths, so Xbox, Switch and PS controllers all work.
- Button prompts go through a ButtonGlyph lookup (PlayStation / Xbox / Nintendo / touch sets), never hard-coded text.
- A separate Touch control scheme for mobile: [TBD - designed at M8, e.g. virtual left stick for aim,
  virtual right stick for movement, on-screen fire/lock/ammo buttons]. Keep gameplay logic independent of input device.
- Performance budget set by the weakest target (mobile / Switch): pooled everything, no per-frame allocations
  in gameplay loops, target 60 fps with hundreds of bullets on screen. Log a warning if any pool grows unexpectedly.
- UI and camera must handle aspect ratios from 4:3 to 21:9 plus phone notches (use Safe Area).
- Saving goes through an ISaveSystem interface (local file for now; platform save APIs plug in later).
- No platform-specific code outside Scripts/Platform/.

## Core loop
Stage (survive waves, collect currency) -> Results/payout -> Shop (buy upgrades & ammo types) -> next Stage (harder).
Boss rounds: 3, 5, 7. After round 7: [TBD - e.g. boss every 2 rounds / endless scaling / game ends].

## Controls (action map "Gameplay")
PlayStation names below; Xbox = RB / LS click / A B X Y, Switch = R / L-stick click / B A Y X.
- Right stick: move the player.
- Left stick + L3: select, lock and aim weapon arms. Two states:

  SOFT SELECT (default, unlocked) - for rapid arm switching mid-fight:
  - Arm slots sit at 8 fixed directions around the player (N, NE, E, SE, S, SW, W, NW = 45 degree slices).
  - Selection areas work like a pie: the stick selects the NEAREST OWNED arm, with boundaries halfway
    between neighbouring owned arms. One arm = the whole circle; each new arm cuts the pie further.
    With all 8 owned this is the 45 degree slices.
  - Pushing the left stick into the outer threshold soft-selects the arm for that direction.
  - Rotating the stick while the selection is held (magnitude >= deselect threshold) switches the
    selection to whichever arm owns the new direction. The arm does NOT rotate; it fires in its slot's fixed direction.
  - Stick drops out of the outer threshold -> arm deselects.
  - Select threshold: magnitude >= 0.85. Deselect threshold: magnitude < 0.65 (hysteresis).
  - Angle hysteresis: ~8 degrees past a boundary before switching arms (no jitter on boundaries).
  - Arms are only drawn while soft-selected or locked; with nothing selected no arms are visible.

  LOCKED (after pressing L3) - for committing to one arm and aiming it freely:
  - Pressing L3 while an arm is soft-selected locks that arm.
  - While locked, the left stick aims the locked arm freely through 360 degrees: the arm rotates around
    the player to point where the stick points, and fires in that direction. Rotating the stick no
    longer switches arms.
  - Stick released while locked (below InputTuning's locked aim deadzone): the arm stays locked and keeps its last aim direction.
  - Pressing L3 again unlocks: the arm returns to its home slot and soft select resumes.
  - Pressing L3 with nothing soft-selected does nothing.
  - Clear on-screen indicator for locked vs soft-selected (e.g. solid outline vs dim outline).
  - Arm rotation speed when locked: [DEFAULT: instant snap to stick angle; tunable turn speed later].

  All thresholds and speeds live in an InputTuning ScriptableObject.
- R1: fire the selected arm. Hold R1 = continuous fire at the arm's fire rate. No arm selected = no fire.
- Cross / Circle / Square / Triangle: equip ammo slot 1-4 (buttonSouth / buttonEast / buttonWest / buttonNorth).
- Options: pause.
- Keyboard/mouse bindings exist only as a debug fallback (mouse direction + left click stands in for
  left stick + R1, WASD to move, L (or middle mouse) to lock, 1-4 for ammo slots, Esc to pause).
- Controller haptics/light bar: not in scope until M8.

## Systems
- Weapon arms: the player starts with ONE arm equipped and can own/equip up to 8 later (unlock/equip flow
  comes with the shop in M5). A debug toggle in InputTuning equips all 8 placeholder arms for testing.
- Arm stats = damage, fire rate, projectile speed, projectiles per shot, spread.
  Upgrades modify these stats; an arm can have multiple upgrade levels.
- Ammo types (4 equipped slots): Laser, Shotgun, Tracking, Automatic, Gatling (more later).
  Ammo type defines projectile behavior. Arm stats scale it.
- Heat/cooldown: Laser, Automatic, Gatling build heat while firing and overheat into a cooldown.
  One shared HeatComponent; each ammo type sets its own heat values.
- Currency: dropped by enemies, collected by the player, banked at round end.
- Shop: buy new ammo types, arm upgrades, and [TBD]. Prices scale per purchase.
- Difficulty: each round scales enemy count, HP, fire rate and bullet speed via a DifficultyCurve asset.
- Bosses: multi-phase, each phase = list of attack patterns.

## Architecture rules (follow these strictly)
- All tunable data lives in ScriptableObjects: WeaponArmData, AmmoTypeData, UpgradeData,
  EnemyData, WaveData, BossData, DifficultyCurve, InputTuning. No gameplay numbers hard-coded in MonoBehaviours.
- ALL projectiles (player and enemy) use object pooling (UnityEngine.Pool.ObjectPool<T>).
  Never Instantiate/Destroy bullets during gameplay.
- Game flow is a single GameStateMachine: Stage, Results, Shop, Boss, GameOver, Pause.
- Systems talk through C# events or ScriptableObject event channels, not FindObjectOfType.
- Input only through the generated Input Actions class. No legacy Input Manager.
- Sprites are referenced from data assets / prefabs so art can be swapped without code changes.
- Placeholder art: simple colored shapes for enemies, bullets, pickups, bosses, UI.
  The player and weapon arms use MY sprites in Assets/Art/Player and Assets/Art/Arms - never replace them.

## Folder layout
Assets/
  Art/ (Player, Arms, Placeholder)
  Data/ (Arms, Ammo, Upgrades, Enemies, Waves, Bosses, Input)
  Prefabs/
  Scenes/ (Boot, Game, Shop)
  Scripts/ (Core, Input, Player, Weapons, Projectiles, Enemies, Bosses, Shop, UI, Platform)
  Tests/

## Working agreement
- One milestone per session. Propose a plan first; wait for my OK before large changes.
- After code changes, check the Unity console for compile errors and fix them before reporting done.
- Keep summaries short: what changed, which files, what I should playtest.
- Don't refactor unrelated code. Don't rename or move my art assets.
- Never commit or push without asking me. When I approve, commit with a message like "M1: <summary>".
- Never touch Library/, Temp/, Logs/ or UserSettings/.
- If a request conflicts with these rules, say so instead of silently breaking them.

## Milestones
- [x] Setup: Unity 6.3 project, Git repo, .gitignore, this file.
- [ ] M0 Project skeleton: folder layout, Boot/Game scenes, Gameplay action map + generated C# class.
- [ ] M1 Movement + arm selection: right stick moves; left stick soft select with hysteresis; L3 lock with
      free 360 aim of the locked arm; unlock returns arm to its slot; soft/locked indicators; debug toggle to
      equip all 8 arms; debug overlay showing stick magnitude, angle, selected arm, state (none/soft/locked).
- [ ] M2 R1 fires the selected arm in its current direction (slot direction when soft, aim direction when locked) with one ammo type, pooled projectiles.
- [ ] M3 Data layer: arm/ammo/upgrade ScriptableObjects, 4 ammo slots, face-button swap, HeatComponent.
- [ ] M4 Enemies + data-driven waves + currency drops.
- [ ] M5 Round loop, results screen, shop, difficulty scaling.
- [ ] M6 Bosses (round 3 first, then 5 and 7).
- [ ] M7 Swap in final player/arm art.
- [ ] M8 Polish: touch controls, button glyphs, juice, audio, save data, performance pass.
