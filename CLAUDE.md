# Project: Bullet Hell (working title)

Roguelike arcade bullet hell, top-down 2D. Theme: the player is a CANDY GLADIATOR fighting food-based
combatants in a VEGETABLE COLOSSEUM (arena, crowd, announcer vibe; bright, playful, readable). Developed and playtested on Windows with a PS5 DualSense.
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

## Game flow
Boot scene (bootstrapper) -> Main Menu scene -> Game scene.
- Boot: first scene in the build. Creates persistent services once (DontDestroyOnLoad): SaveSystem,
  SettingsService, GameStateMachine/run manager, audio stub, scene loader. Then loads Main Menu.
  Pressing Play in any scene in the Editor must still work (services self-bootstrap if Boot was skipped).
- Main Menu: New Game, Continue, Settings, Quit. Continue is disabled when no run save exists.
  New Game with an existing run save asks to confirm overwriting it.
- Settings (from Main Menu and Pause): master / music / SFX volume, screen shake on/off, controller
  vibration on/off, aim sensitivity (scales the InputTuning thresholds within safe limits), show debug
  overlay (dev builds), and on PC fullscreen/windowed + resolution. Applied immediately, saved in the
  settings file, Back returns to wherever Settings was opened from.
- New Game -> Gladiator customization (cosmetics) -> Round intro -> Combat.
  Continue skips customization and resumes at the Shop for the saved round.
- Round intro: every round starts with a banner/announcer moment ("Round 1 - Begin!", boss rounds get
  a special banner) and a short countdown; player can move during it but enemies and traps are idle.
- Run loop (Game scene, one GameStateMachine):
  RoundIntro -> Combat (waves) -> Round Results -> Shop -> Armory -> RoundIntro (round N+1, harder).
  Boss rounds: 3, 5, 7. After round 7: [TBD - e.g. boss every 2 rounds / endless scaling / game ends].
  Pause and Game Over can happen during Combat. Game Over returns to Main Menu.
- Arms and armaments can only be equipped in the Armory, between rounds - never during combat.
  [TBD: also allow the Armory between waves inside a round?]

## Gladiator customization (cosmetics)
- Purely visual; never changes stats. CosmeticData assets, each belonging to a slot. Starting slots:
  candy coating (body color/pattern), headgear (helmet/crest), cape/trail, and arm tint.
- Cosmetics render as layered sprites/tints on the gladiator, so new items are new assets, not code.
- The customization screen shows a live preview of the gladiator, cycles items per slot with the
  controller, and has Randomize and Confirm. Placeholder items for now (colored shapes/tints).
- The chosen look is stored in the profile file and pre-selected on the next New Game.
  [TBD: how new cosmetics are unlocked - all unlocked for now]

## Save system
- Exactly ONE run save (single slot), JSON, written through ISaveSystem (platform save APIs plug in later).
- Separate from the run save: a settings file and a profile file (chosen cosmetics, unlocks). These are
  never deleted by Game Over or New Game.
- Autosave when entering the Shop after each round, and after leaving the Armory.
- Saved: round number, currency, arm inventory, armament inventory, loadout (8 slots of arm instances
  with their equipped armaments), 4 ammo slots, save version number.
- Continue loads the save and resumes at the Shop for the saved round.
- Game Over deletes the run save (roguelike). [TBD: any permanent meta-progression, saved separately]
- Save data uses plain serializable classes and asset IDs, never direct ScriptableObject references
  (an AssetRegistry maps IDs -> WeaponArmData / ArmamentData / AmmoTypeData).

## Controls (action map "Gameplay")
PlayStation names below; Xbox = RB / LS click / A B X Y, Switch = R / L-stick click / B A Y X.
- Right stick: move the player.
- Left stick + L3: select, lock and aim weapon arms. Two states:

  SOFT SELECT (default, unlocked) - for rapid arm switching mid-fight:
  - Arm slots sit at 8 fixed directions around the player (N, NE, E, SE, S, SW, W, NW = 45 degree slices).
  - Pushing the left stick into the outer threshold soft-selects the arm in that direction.
  - Rotating the stick while in the outer threshold switches the selection to whichever arm is in the
    new direction. The arm does NOT rotate; it fires in its slot's fixed direction.
  - Stick drops out of the outer threshold -> arm deselects.
  - Select threshold: magnitude >= 0.85. Deselect threshold: magnitude < 0.65 (hysteresis).
  - Angle hysteresis: ~8 degrees past a slice boundary before switching arms (no jitter on boundaries).
  - Directions whose loadout slot is empty select nothing.

  LOCKED (after pressing L3) - for committing to one arm and aiming it freely:
  - Pressing L3 while an arm is soft-selected locks that arm.
  - While locked, the left stick aims the locked arm freely through 360 degrees: the arm rotates around
    the player to point where the stick points, and fires in that direction. Rotating the stick no
    longer switches arms.
  - Stick released while locked: the arm stays locked and keeps its last aim direction.
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
- Weapon arms: each arm TYPE is a WeaponArmData asset with its own sprite (drawn pointing right),
  display name, tint/ID color, muzzle offset, and stats. There are currently 4 unique arm types.
- Arm loadout: an ArmLoadout asset holds 8 slots (N, NE, E, SE, S, SW, W, NW). Each slot is empty or
  references a WeaponArmData; the same arm type may be equipped in more than one slot.
  The player's arms are spawned from the loadout at runtime - no arm is hard-coded in the scene.
  - StartingLoadout asset: ONE arm equipped (slot N by default). DebugLoadout asset: all 8 slots filled
    for testing. A field on the player (or a debug setting) chooses which loadout is used.
  - Equipping in-game happens in the Armory (M4); StartingLoadout defines a new run.
- Arm instances: each filled loadout slot is a runtime ArmInstance = WeaponArmData + 3 armament slots.
  Armaments belong to the instance (two slots holding the same arm type can be kitted differently).
  Never modify ScriptableObject assets at runtime; all run state lives on instances.
- Arm stats = damage, fire rate, projectile speed, projectiles per shot, spread (base values on WeaponArmData).
- Armaments (arm upgrades): ArmamentData assets, each a list of stat modifiers (flat add or percent
  multiply). Final stat = base, then all flat adds, then all percent multipliers (order documented in code).
  3 armament slots per arm instance. Armaments are bought in the Shop into the armament inventory and
  equipped in the Armory. Unequipping returns the armament to the inventory.
- Arm effects: WeaponArmData can carry special effects beyond stats (e.g. pierce, burn, ricochet).
  Built as a small effect interface so new effects are new classes/assets, not edits to firing code.
  Start with 1-2 test effects.
- Ammo types: AmmoTypeData assets. Starter set: Basic, Shotgun (multiple pellets + spread),
  Laser (continuous beam, heat), Gatling (spin-up, heat). Later: Tracking, Automatic, more.
  Ammo type defines projectile behavior; the arm's (armament-modified) stats scale it.
- Ammo slots: the player carries 4 ammo slots, one per face button (Cross/Circle/Square/Triangle).
  Tapping a face button makes that slot's ammo active for firing. Empty slots can't be selected.
  Run start: slot 1 = Basic, others empty.
- Ammo pickups (found in the world as the game progresses):
  - Walking over a pickup while any ammo slot is empty auto-fills the first empty slot.
  - If all 4 slots are full, standing near the pickup and HOLDING a face button (default 0.75s,
    tunable) replaces that slot's ammo. The replaced ammo drops on the ground as a pickup.
  - Tap still just switches ammo; only a hold near a pickup swaps. Show a simple hold-progress indicator
    and a prompt when near a pickup (basic debug-style UI is fine for now).
  - Picking up an ammo type already carried: [DEFAULT: does nothing / stays on the ground].
- Heat/overheat: heat is tracked per arm instance. Ammo types with heat (Laser, Gatling) add heat while
  firing; at max heat the arm overheats and can't fire until it cools to a restart threshold.
  Heat decays when not firing. All heat values live on AmmoTypeData. One shared HeatComponent.
- Player health: [DEFAULT: 5 HP], small visible hitbox core (bullet-hell style - smaller than the sprite),
  ~1s invulnerability with flashing after a hit. HP 0 -> Game Over (run save deleted) -> Main Menu.
  Values on PlayerData.
- Arena: the vegetable colosseum. ArenaData defines bounds (walls), player spawn, enemy spawn gates,
  and placed obstacles/traps. Rounds can reference different ArenaData layouts. Placeholder art for now.
- Obstacles (both block movement AND all bullets, player's and enemies'):
  - Solid: permanent, indestructible (e.g. stone/giant veggie pillars).
  - Breakable: has HP, takes damage from any bullets, shows damage stages, breaks into non-blocking debris
    (e.g. cabbage crates, pumpkins). Breaking one updates enemy navigation.
- Traps: TrapData = shape/area, damage, telegraph time, active time, cooldown. Traps hurt ANYTHING inside
  them - the player and enemies - so luring enemies into traps is a valid tactic. Always telegraphed
  (visual warning before activating). Starter traps: periodic floor vent (area burst), a spike/skewer line,
  and a hazard zone that ticks damage while you stand in it. Traps are idle during the Round intro.
- Enemies: food-based combatants. EnemyData assets = HP, movement behavior, attack(s), fire rate,
  contact damage, currency value, placeholder shape + color. Movement must feel ACTIVE and grounded:
  enemies pursue, reposition and flank with acceleration/turning limits - no floaty drifting.
  - Navigation: grid flow field toward the player over the arena (cheap for many enemies), rebuilt
    when a breakable obstacle breaks; local separation so enemies don't stack.
  - Ranged enemies need line of sight to fire; without it they reposition.
  Archetypes:
  - Chaser: aggressively pursues and closes distance, melee/contact damage (fast swarm variants later).
  - Skirmisher (ranged): holds a preferred distance, strafes, backs off when approached, fires patterns.
  - Mobile Sentry: moves to a firing position with line of sight, plants, fires a continuous stream,
    then overheats and slows/stops firing while cooling (reuse the HeatComponent) - its vulnerable window.
  - Charger and Sniper stay as variants (telegraph + dash; warning line + fast shot).
  Existing Grunt/Spinner are converted: Grunt -> Chaser, Spinner -> Sentry-style or retired.
- Waves and rounds: WaveData = list of spawn entries (enemy, count, spawn edge/point, delay between spawns).
  RoundData = ordered list of waves. A round ends when its last wave is fully cleared.
  Authored RoundData for rounds 1-7; beyond the authored rounds, reuse the last ones with scaling.
  Boss rounds (3, 5, 7) use a tougher placeholder wave until bosses are built.
  Short breather (~2s, tunable) between waves with a "Wave X/Y" message.
- Difficulty: DifficultyCurve asset scales enemy HP, enemy fire rate, enemy bullet speed and spawn count by
  round number (AnimationCurves or per-round multipliers).
- Currency: enemies drop coins worth their currency value. Coins are attracted to the player within a
  magnet radius. At round end any remaining coins fly to the player automatically. Currency banks into
  the run state and shows on Round Results.
- Shop (after each round): offers a few random items from pools - new arms (with effects) and armaments
  (and later ammo types). Buying adds the item to the arm or armament inventory. Prices scale per round.
  Skeleton first: fixed test stock, plain list UI, controller navigable.
- Armory (after the Shop): shows the player with its 8 arm slots.
  - Select an arm -> a panel shows its 3 armament slots -> pick a slot -> choose an armament from the
    inventory (or remove the current one).
  - Selecting an EMPTY arm slot lets the player place an arm from the arm inventory there.
    Removing an arm from a slot returns it (with its armaments still attached) to the arm inventory.
  - "Continue" starts the next round. Skeleton UI first; visual polish later.
- Difficulty: each round scales enemy count, HP, fire rate and bullet speed via a DifficultyCurve asset.
- Bosses: multi-phase, each phase = list of attack patterns.

## Architecture rules (follow these strictly)
- All tunable data lives in ScriptableObjects: WeaponArmData, AmmoTypeData, ArmamentData,
  EnemyData, WaveData, BossData, DifficultyCurve, InputTuning, ArmLoadout, PickupTuning, ShopPool, AssetRegistry, BulletPatternData, RoundData, PlayerData, ArenaData, TrapData, CosmeticData, SettingsDefaults. No gameplay numbers hard-coded in MonoBehaviours.
- ALL projectiles (player and enemy) use object pooling (UnityEngine.Pool.ObjectPool<T>).
  Never Instantiate/Destroy bullets during gameplay.
- Run flow is a single GameStateMachine: Combat, RoundResults, Shop, Armory, Pause, GameOver.
- Systems talk through C# events or ScriptableObject event channels, not FindObjectOfType.
- Input only through the generated Input Actions class. No legacy Input Manager.
- Sprites are referenced from data assets / prefabs so art can be swapped without code changes.
- Placeholder art: simple colored shapes for enemies, bullets, pickups, bosses, UI.
  The player and weapon arms use MY sprites in Assets/Art/Player and Assets/Art/Arms - never replace them.

## Folder layout
Assets/
  Art/ (Player, Arms, Placeholder)
  Data/ (Arms, Loadouts, Ammo, Armaments, Pickups, Shop, Cosmetics, Arenas, Traps, Settings, Enemies, Waves, Bosses, Input)
  Prefabs/
  Scenes/ (Boot, MainMenu, Game)   (Shop and Armory are UI states inside Game)
  Scripts/ (Core, Save, Settings, Input, Player, Cosmetics, Weapons, Projectiles, Enemies, AI, Arena, Bosses, Shop, Armory, UI, Platform)
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
- [x] M0 Project skeleton: folder layout, Boot/Game scenes, Gameplay action map + generated C# class.
- [x] M1 Movement + arm selection: soft select, L3 lock with free 360 aim, indicators, debug overlay.
- [x] M1.5 Arm art + loadout: WeaponArmData for my 4 arm sprites (import settings, pivots, muzzles),
      ArmLoadout with 8 slots, Starting/Debug loadouts, arms spawned from the loadout.
- [x] M2 R1 fires the selected arm in its current direction (slot direction when soft, aim direction when locked) with one ammo type, pooled projectiles.
      Each arm fires from its own muzzle using its own WeaponArmData stats.
- [x] M3a Ammo: AmmoTypeData + 4 starter types, 4 face-button ammo slots, HeatComponent/overheat per arm,
      ammo pickups (auto-fill empty slot, hold button to replace, dropped ammo), test pickups in scene.
- [x] M3b Armaments core: ArmInstance with 3 armament slots, ArmamentData stat modifiers, stat calculation,
      arm + armament inventories, 1-2 test arm effects, debug controls, overlay shows final stats.
- [x] M4 Game flow skeleton: Boot bootstrapper, Main Menu (Start/Continue/Quit), single-slot save system,
      GameStateMachine with a stub round (ends when test enemies are cleared), Round Results -> Shop
      (fixed test stock) -> Armory (select arm -> 3 slots -> equip from inventory; place arms in empty
      slots) -> next round. Autosave + Continue working. Plain skeleton UI, fully controller navigable.
- [x] M5a Combat: player health/hitbox/i-frames/Game Over, BulletPatternData, pooled enemy bullets,
      4 starter enemy types (Grunt, Spinner, Charger, Sniper), enemy test mode to spawn each type.
- [x] M5b Rounds: WaveData/RoundData for rounds 1-7, wave spawner replacing the stub round,
      coin drops + magnet + round-end collection, DifficultyCurve scaling, currency on Round Results.
- [x] M6 Front end: Main Menu (New Game/Continue/Settings/Quit), Settings screen + settings file,
      Gladiator customization screen with placeholder cosmetics + profile file, Round intro banner and
      countdown state. Skeleton UI, fully controller navigable.
- [ ] M7 Arena: ArenaData, colosseum bounds, solid + breakable obstacles (block all bullets),
      3 starter traps (hurt player and enemies, telegraphed), one test arena layout.
- [ ] M8 Enemy AI rework: flow-field navigation + separation, line of sight, Chaser / Skirmisher /
      Mobile Sentry, convert existing enemies, retune rounds 1-7 for the arena.
- [ ] M9 Shop pools and pricing (random stock, scaling prices), more arms/armaments/effects.
- [ ] M10 Bosses (round 3 first, then 5 and 7).
- [ ] M11 Themed UI/visual pass: candy-colosseum style for menus, HUD, Shop, Armory, customization; final art.
- [ ] M12 Polish: touch controls, button glyphs, juice, announcer/audio, performance pass.
