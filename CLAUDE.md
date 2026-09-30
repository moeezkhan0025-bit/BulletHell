# Project: VOX VEGETALLIS (formerly "Bullet Hell", the working title)

Roguelike arcade bullet hell, top-down 2D. Theme: the player is a CANDY GLADIATOR fighting food-based
combatants in a VEGETABLE COLOSSEUM (arena, crowd, announcer vibe; bright, playful, readable).
Developed and playtested on Windows with a PS5 DualSense.
Unity 6.3 LTS (6000.3), 2D URP, new Input System. Solo developer. I playtest every change myself.
Project root: C:\Dev\BulletHell. Version control: Git (GitHub private repo), shell: Git Bash.
Game title everywhere: VOX VEGETALLIS (Main Menu logo, window title). Unity Product Name: "VoxVegetallis" (saves live in
`%USERPROFILE%\AppData\LocalLow\DefaultCompany\VoxVegetallis\`).

## UI theme: VoxVegetallis (the ONLY UI theme)
The UI has exactly one look, "VOX VEGETALLIS - Garden Colosseum": marble panels, soil-brown outlines with hard drop shadows, corn-gold focus,
leaf-green checker trim strips, tomato hearts and rarity colors (common stone, rare leaf, epic carrot, legendary gold). There is no
alternate theme and no toggle; restyling means editing the theme asset or the kit art.
- Art kit: `Assets/Art/UI/VoxKit` (2x art, imported at PPU 200 so it draws at half size on the 1080p canvas; 9-slice borders and tokens come from
  `vox_ui_kit_manifest.json`, applied by the editor tool `BulletHell/Vox/1 Import Kit Sprites`). Mockups to match: `Docs/Reference/UI` (Main Menu,
  HUD, Shop, Armory, Character Creation, Settings). Screenshots of the built screens: `Docs/Screenshots/VoxUI`.
- Theme asset: `Assets/Data/UI/VoxVegetallis.asset` (`UITheme`, on GameConfig). It maps roles to kit sprites, the palette and rarity colors,
  fonts, heat colors (leaf -> carrot -> tomato), metrics (outline 6, shadow 9, focus lift 12 px / 0.12 s, buy 0.35 s, bubble pop 0.25 s / stagger 0.05 s,
  screen transition 0.2 s) and UI sounds. Screens never reference kit sprites directly; `ThemedImage`, `ThemedTrim` (tiled checker), `ThemedText`,
  `ThemedButton` and `FocusDecor` (trims, laurels, lift on focus) read the theme. Built by `BulletHell/Vox/4 Build Theme`.
- Fonts (TextMeshPro, SIL OFL, see Docs/CREDITS.md): Cinzel Decorative 700/900 (titles, logo), Lilita One (buttons, numbers), Nunito 600/800
  (body, small caps labels). All UI text is `TMP_Text`; never legacy `UnityEngine.UI.Text`. Outlines are shared material presets (`ThemedText`), not
  TMP's per-label outline properties (those create per-label materials that go stale when the font changes).
- Screens are built by the editor tools `BulletHell/Vox/5..9` (`VoxMenuScreens`, `VoxGameScreens`, `VoxShopArmory`, helpers in `VoxUi`); they rebuild the
  screen contents and keep the root objects and wiring. Re-run them after changing a layout. Earlier setup scripts (M4..M9d, CC1) are history and
  build the old look; do not re-run their screen builders. `VoxShots` renders any open scene to a PNG for checking against the mockups.

## Visual style and perspective
Reference: Docs/Reference/concept_arena.png (target look - not a game asset).
Art rules (sizes, perspective, pivots, height classes, colors, naming): Docs/ART_SPEC.md. Follow it when hooking up art.
- 3/4 top-down (oblique) view: art is drawn at an angle, but GAMEPLAY STAYS ON THE FLAT 2D XY PLANE.
  No 3D, no height physics. Movement speed is the same in all directions.
- Depth sorting: URP 2D Renderer Transparency Sort Mode = Custom Axis (0,1,0) - lower on screen draws in front.
  Characters/obstacles use Sprite Sort Point = Pivot with the pivot at their FEET/BASE.
- Colliders are FOOTPRINTS, not the whole sprite: a flat ellipse/box at the base of pillars, crates,
  enemies and the player. Tall sprites (pillars) visually overlap things behind them without blocking them.
  Bullets are blocked by an obstacle's footprint plus a modest vertical allowance [tunable].
- The player's damage hitbox stays a small core at the body's center (bullet-hell rule), separate from
  the movement footprint.
- Arm ring (3/4 look): the 8 arm slots sit on a flattened ELLIPSE around the player's FEET (ground level),
  like a ring spinning around the base - not a flat clock face around the body center.
  - Ellipse radii X/Y (Y = X x the floor ratio F from ART_SPEC, default 0.6) and a small vertical offset
    live in an ArmRingTuning asset.
  - Arms on the back half of the ellipse draw BEHIND the body; arms on the front half draw IN FRONT
    (sorted by their own ground Y, same rule as everything else).
  - Optional depth cue: back arms slightly smaller/darker, front arms slightly larger [tunable, subtle].
  - Readability over realism: the ring radius must be wide enough that arms clear the body silhouette,
    including the north (back) slot. When the selected/locked arm is hidden behind the body, draw an
    occluded-outline silhouette of it through the body so it's always visible.
  - Soft select still uses the 8 slot directions; locked aim moves the arm smoothly around the ellipse
    to the stick angle (it slides along the ring rather than orbiting a circle) and the arm sprite
    points in the true aim direction. Optional short ring "spin" easing when switching slots [tunable].
  - Aiming and bullets use the stick's true screen direction; the ellipse only changes where arms
    are drawn and where muzzles sit.
  - While jumping, the ring rises with the body visual; the shadow stays on the ground.
- Bullet height: all bullets and hurtboxes live on the ground plane (collision at ground positions).
  Bullets are drawn with a small visual lift and a tiny shadow so they read as flying, consistent for
  player and enemy bullets. The player's visible damage-core marker sits low on the body, near the feet,
  matching where collisions actually happen. Enemy hurtboxes are ground-plane footprints under them.
- Arena art is LAYERED, never one flattened image: floor (with decals like the crest/graffiti), back wall and
  crowd, side walls, individual obstacle sprites (solid pillars, breakable crates/tomatoes), and a FOREGROUND
  layer (front railing, front crowd) that draws over gameplay.
- Readability beats decoration: enemy bullets must pop against the busy floor (bright core + dark outline,
  never floor/splatter red). Floor decals stay lower-contrast than anything interactive.
- Arena fits on one screen with a fixed camera for the main arena size; extra width/height on other aspect
  ratios is filled with crowd/wall art, never gameplay space.

## Animation approach (hybrid: draw key poses, let code do the motion)
- Default for enemies/NPCs: 1-3 drawn key poses (idle, windup, attack) + procedural motion. Player: a short
  drawn run cycle (4 frames) + procedural motion. Bosses (for now): drawn key poses on the 2560 boss template
  + procedural motion, like enemies (rigging bosses in parts is deferred to a later milestone). Merchant: drawn in PARTS and rigged.
- Procedural toolkit (reusable components, values in data assets, all optional per character):
  - Motion: idle breathing (scale sine), hop-walk bob + tilt while moving, lean into movement,
    squash/stretch on start/stop/land, facing flip with a quick squash.
  - Combat feedback: white hit flash (shader), knockback nudge, scale punch, short hitstop, camera shake.
  - Telegraphs: windup = inflate + tremble + DANGER-color pulse (shader), so every attack reads.
  - Spawn: pop-in scale from a gate puff. Death: squash, flash, juice-splat particles, dissolve or shrink.
- Rigging: Unity 2D Animation + 2D PSD Importer (+ 2D IK where useful). Layered PSDs exported from
  Procreate, one layer per body part, named consistently (head, torso, arm_L, arm_R, leg_L, leg_R, ...).
- Shaders (Shader Graph, 2D): hit flash, dissolve, outline (arm soft/locked states, focus highlight),
  palette/tint swap (cosmetics, rarity), pulse/glow (telegraphs, heat), UV scroll (laser beam),
  wave (banners, flags), ripple (sauce puddles).
- Particles do most VFX (sparks, dust, smoke, steam, debris, coins, confetti, torch flames) from a few
  small textures: soft dot, spark streak, smoke puff, shard.
- Arena life: crowd heads bob procedurally with random offsets; banners use the wave shader; torches
  use particles.
- UI motion is all tweened (card lift, buy fly-in, SOLD stamp, heart pop, banners, screen transitions)
  with a free tween library (PrimeTween or DOTween - pick one and use it everywhere).

## HUD (combat)
- Bottom-left: gladiator portrait (reflects chosen cosmetics), 5 TOMATO hearts = 5 HP (1 heart per hit),
  inside the checkered portrait ring, and a heat bar beside them showing the SELECTED arm's heat (fills while firing heat ammo, tinted leaf -> carrot -> tomato
  as it rises, flashes tomato/marble when overheated, drains while cooling). Top center: round / wave pill; top right: currency pill. No arm selected -> bar shows the last selected arm, dimmed.
- Bottom-right: 4 ammo slot icons in button order, each with its face-button glyph. Active slot highlighted,
  empty slots shown as empty frames, hold-to-replace progress drawn around the slot being replaced.
  AmmoTypeData gets an icon field.
- HUD respects Safe Area, scales for phones, and never covers the arena's play space.

## Target platforms
Steam (Windows first; Mac/Linux later), iOS, Android, Nintendo Switch, Xbox.
Consoles come later (need platform approval + Unity Pro), but the code must be console-ready from day one:
- Input bindings use GENERIC gamepad paths (<Gamepad>/rightShoulder, <Gamepad>/leftStickPress,
  <Gamepad>/buttonSouth...), never DualSense-only paths, so Xbox, Switch and PS controllers all work.
- Button prompts go through a ButtonGlyph lookup (PlayStation / Xbox / Nintendo / touch sets), never hard-coded text.
- A separate Touch control scheme for mobile: [TBD - designed at M12, e.g. virtual left stick for aim,
  virtual right stick for movement, on-screen fire/lock/ammo buttons]. Keep gameplay logic independent of input device.
- Performance budget set by the weakest target (mobile / Switch): pooled everything, no per-frame allocations
  in gameplay loops, target 60 fps with hundreds of bullets on screen. Log a warning if any pool grows unexpectedly.
- UI and camera must handle aspect ratios from 4:3 to 21:9 plus phone notches (use Safe Area).
- Saving goes through an ISaveSystem interface (local file for now; platform save APIs plug in later).
- Phones and tablets: LANDSCAPE ONLY (iOS and Android auto-rotate between Landscape Left and Right; portrait is not supported).
  4:3 tablets keep the existing letterboxing (16:9 play area, crowd/wall art filling the rest).
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
  overlay (dev builds), and on PC fullscreen/windowed + resolution. Laid out on tabs (Audio, Video, Controls, Gameplay; L1/R1 switch) with sliders,
  toggles and arrow rows. Applied immediately, saved in the settings file, Back returns to wherever Settings was opened from.
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
- Autosave: on entering the Shop after each round, after every Shop purchase, reroll and crate pick, and after every Armory change (and on leaving the Armory). Code behavior; saves are cheap.
- Saved: round number, currency, arm inventory, armament inventory, loadout (8 slots of arm instances
  with their equipped armaments), 4 ammo slots, save version number.
- Continue loads the save and resumes at the Shop for the saved round.
- Game Over deletes the run save (roguelike). [TBD: any permanent meta-progression, saved separately]
- Save data uses plain serializable classes and asset IDs, never direct ScriptableObject references
  (an AssetRegistry maps IDs -> WeaponArmData / ArmamentData / AmmoTypeData).

## Controls (action map "Gameplay")
PlayStation names below; Xbox = RB / RT / LS click / A B X Y, Switch = R / ZR / L-stick click / B A Y X.
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
  - Pie split: the stick direction selects the nearest EQUIPPED arm (empty slots are skipped), so the ring has no dead zones. (Earlier wording "empty slots select nothing" no longer matches the code.)

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
- R2 (<Gamepad>/rightTrigger, press point ~0.5): jump. See Jump in Systems.
- Cross / Circle / Square / Triangle: equip ammo slot 1-4 (buttonSouth / buttonEast / buttonWest / buttonNorth).
- Options: pause.
- Keyboard/mouse bindings exist only as a debug fallback (mouse direction + left click stands in for
  left stick + R1, WASD to move, L (or middle mouse) to lock, Space to jump, 1-4 for ammo slots, Esc to pause).
- Controller haptics/light bar: not in scope until M12.

## Systems
- Weapon arms: each arm TYPE is a WeaponArmData asset with its own sprite (drawn pointing right),
  display name, tint/ID color, muzzle offset, and stats. There are currently 4 unique arm types.
- Arm loadout: an ArmLoadout asset holds 8 slots (N, NE, E, SE, S, SW, W, NW). Each slot is empty or
  references a WeaponArmData; the same arm type may be equipped in more than one slot.
  The player's arms are spawned from the loadout at runtime - no arm is hard-coded in the scene.
  - StartingLoadout asset: ONE arm equipped (slot E - to the player's right - by default). DebugLoadout asset: all 8 slots filled
    for testing. A field on the player (or a debug setting) chooses which loadout is used.
  - Equipping in-game happens in the Armory (M4); StartingLoadout defines a new run.
- Arm instances: each filled loadout slot is a runtime ArmInstance = WeaponArmData + its armament slots.
  The NUMBER of armament slots depends on the arm (WeaponArmData.armamentSlots, 1-3; rarer arms get more).
  Armaments belong to the instance (two slots holding the same arm type can be kitted differently).
  Never modify ScriptableObject assets at runtime; all run state lives on instances.
- Arm stats = damage, fire rate, projectile speed, projectiles per shot, spread (base values on WeaponArmData).
- Armaments: ArmamentData assets = name, icon, rarity (Common / Rare / Epic / Legendary), price tier,
  tags (e.g. Speed, Homing, Pierce, Bounce, Auto), max stacks, and a list of EFFECTS:
  - Stat modifiers (flat add or percent multiply). Final stat = base, then flat adds, then percent
    multipliers (order documented in code).
  - Behavior modifiers: small ArmEffect ScriptableObject subclasses (an abstract base class, not an interface), so new armaments
    are new assets/classes, never edits to the firing code.
  Descriptions and tooltips are generated from the effect data ("+25% bullet speed") so they never go stale.
  Bought in the Shop into the armament inventory, equipped in the Armory. Unequipping returns them.
- Starter armament catalog (more added as development goes on):
  - Homing (auto-tracking): bullets steer toward the nearest enemy inside a forward cone; stacks raise
    turn rate and cone size.
  - Auto-fire: this arm also fires on its own at enemies inside its slot's arc when it is NOT selected,
    at [DEFAULT 50%] fire rate. Heat still applies. When selected it fires normally.
  - Velocity: +X% bullet speed.
  - Pierce: bullets pass through +1 enemy per stack before despawning.
  - Ricochet: bullets bounce off walls and obstacles up to 3 times before despawning (+1 per extra stack);
    hitting an enemy still ends the bullet unless it has Pierce left.
  - Interaction rules, fixed and documented: Pierce is used up before a bullet stops; Ricochet counts only
    wall/obstacle bounces; Homing re-targets after each bounce or pierce.
- Arm effects: WeaponArmData can carry built-in effects too (same interface as armament behaviors),
  e.g. burn, pierce. Start with the existing test effects.
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
- Jump (fake height - gameplay stays 2D): the player root stays on the ground plane (XY); a "height" value
  drives a child visual offset. Values in a JumpTuning asset.
  - Visuals: body sprite rises and falls on an AnimationCurve arc; a ground shadow stays at the root and
    shrinks/fades with height; slight scale-up at the apex; squash on takeoff and landing; small dust puff
    on landing. Arms, cosmetics and HUD portrait follow the body.
  - Defaults: airtime ~0.45s, no double jump, ~0.2s cooldown after landing, full steering in the air
    [DEFAULT: air control 100%, tunable]. Firing and aiming work while airborne.
  - While airborne the player PASSES OVER: enemy bodies (no blocking, no contact damage), charger dashes,
    ground traps and hazard zones, pickups (collected on landing only if still overlapping).
  - While airborne the player also PASSES OVER Low-class obstacles (low walls, low crates) - see Obstacles.
  - While airborne the player is STILL HIT BY enemy bullets [DEFAULT - toggle jumpDodgesBullets = false,
    keeps it a bullet hell], and is still blocked by Tall obstacles and the arena boundary walls.
  - The jump apex must visibly clear Low obstacles (body lift >= 0.7 P, see ART_SPEC).
  - Takeoff next to a Low obstacle that the player can't fully clear before landing: the landing push-out
    moves them to the nearer free side.
  - Sorting: while airborne the player draws above enemies and ground objects near it, but still below
    the foreground layer. Sorting uses the ground (shadow) position, never the lifted sprite.
  - Landing on an enemy or inside an obstacle footprint: the player is pushed to the nearest free spot.
  - Implementation: a PlayerHeight/JumpController component exposes IsAirborne and Height; collision
    the physics layer is switched while airborne (enemy bodies); traps, pickups, contact damage and obstacles use IsGrounded flags or separate grids (Grid / TallGrid / BulletGrid).
    Enemy AI treats the shadow position as the player's position.
- Arena: the vegetable colosseum. ArenaData defines bounds (walls), player spawn, enemy spawn gates,
  and placed obstacles/traps. Rounds can reference different ArenaData layouts. Placeholder art for now.
- Obstacles block ground movement AND all bullets (player's and enemies'). Each has a HEIGHT CLASS
  (see ART_SPEC section 3) and is Solid or Breakable:
  - Low (<= 0.5 P): low walls, crate rows, fences. The player can JUMP over them; they still block
    bullets, so they work as cover. Enemies path around them (a jumping enemy type may come later).
  - Tall (>= 1.5 P): pillars, statues, big pumpkins. Never jumpable. When a character is behind one
    (sprite overlap + higher on screen), the obstacle fades to ~40% alpha so nothing is hidden.
  - Solid = permanent. Breakable = HP, takes damage from any bullets, damage stages, breaks into flat
    non-blocking debris; breaking updates enemy navigation.
  - Arena boundary walls: never jumpable, block everything.
- Traps: TrapData = shape/area, damage, telegraph time, active time, cooldown. Traps hurt ANYTHING inside
  them - the player and enemies - so luring enemies into traps is a valid tactic. Always telegraphed
  (visual warning before activating). Starter traps: periodic floor vent (area burst), a spike/skewer line,
  and a hazard zone that ticks damage while you stand in it. Traps are idle during the Round intro.
- Enemies: food-based combatants. All enemies are GROUND-BASED for now (no flying enemies): they walk on the flat XY plane,
  sort by their feet, and are hit by ground-plane bullets. EnemyData assets = HP, movement behavior, attack(s), fire rate,
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
  Round 3 is the Pumpking alone (see Bosses); rounds 5 and 7 use a tougher placeholder wave until their bosses are built.
- Arena progression: each RoundData picks an arena LAYOUT (ArenaLayoutData: obstacle and trap placements
  on the same colosseum). Hazards ramp up over the run:
  - Round 1: NO traps or hazard zones. A few obstacles for cover only.
  - Each later round adds complexity via a HazardBudget on RoundData (trap count and types, Low walls,
    breakables, Tall obstacles), introducing one new hazard type at a time.
  - Boss rounds use their own layouts (usually more open, a few hazards the boss can use).
  - Layouts must always leave a clear spawn area around the player and clear lanes from the enemy gates.
  - Beyond authored rounds, layouts are reused with the difficulty scaling.
  Short breather (~2s, tunable) between waves with a "Wave X/Y" message.
- Difficulty: DifficultyCurve asset scales enemy HP, enemy fire rate, enemy bullet speed and spawn count by
  round number (AnimationCurves or per-round multipliers).
- Currency: enemies drop coins worth their currency value. Coins are attracted to the player within a
  magnet radius. At round end any remaining coins fly to the player automatically. Currency banks into
  the run state and shows on Round Results.
- Shop (after each round) - a proper shop screen, in the spirit of Balatro / Slay the Spire:
  - Layout: a merchant NPC (e.g. a fig or olive Roman "mercator") and a stall/table. Items are CARDS laid out
    in rows: top row = arms (2), bottom row = armaments (3), plus one "crate" (open to pick 1 of 3 armaments).
    Currency top-right. Reroll button (price rises each reroll this visit). Leave button.
  - Cards show icon, name, rarity frame color, price tag. The focused card lifts and shows a tooltip: full
    effect text, tags, stacks, and a comparison ("fits: Red arm (1 free slot)", stat before -> after).
  - Buying: the card animates into the inventory, its spot shows SOLD. Can't afford = price shown in red.
  - Stock: random from ShopPool assets weighted by rarity and round; better rarities more likely later.
    Prices scale with rarity and round. Stock doesn't refill except by reroll.
  - Optional services later: sell an armament, remove/upgrade.
  - Controller-first: stick/d-pad moves focus between cards, Cross buys, Triangle rerolls, Circle leaves,
    Square toggles detailed stats. Mouse/touch works too.
- Armory (after the Shop) - equip screen, two halves:
  - LEFT: the gladiator large, wearing its gear, with the 8 arm slots on the ellipse ring around the feet.
  - HOVERING ARMAMENT SLOTS: selecting an arm makes that arm's armament slots (1-3, per arm) pop up as
    floating bubbles ABOVE the arm, fanned in a small arc, linked to it by a thin glowing tether, gently
    bobbing. Empty bubble = "+" icon; filled bubble = armament icon with a rarity-colored rim; focused bubble
    scales up with an outline. Arms with fewer slots show fewer bubbles. Bubbles tween in/out (pop + fade).
  - RIGHT: inventory panel with tabs: Arms / Armaments. Grid of item cards (same card style as the Shop).
    Items that can't go in the current selection are dimmed.
  - Flow (controller): left stick / d-pad cycles arms around the ring -> Cross selects an arm (bubbles appear)
    -> left/right moves between bubbles -> Cross on a bubble jumps focus to the Armaments tab -> pick an
    armament -> it flies into the bubble. Before confirming, the left panel previews stats before -> after.
    Triangle on a filled bubble removes the armament back to inventory. Circle backs out one level
    (bubble -> arm -> ring). Selecting an EMPTY arm slot jumps to the Arms tab to place an arm; removing an
    arm returns it with its armaments attached. Mouse/touch: click an arm, click a bubble, click an item.
  - Shoulder buttons (L1/R1) switch tabs; Circle backs out one level; a "Fight!" button starts the round.
- Bosses: multi-phase, each phase = list of attack patterns. See the Bosses section.

## Bosses
- A boss is an Enemy with `EnemyBehavior.Boss`: `BossBehavior` (Scripts/AI) picks and winds up attacks, `BossController` +
  `BossJump` (Scripts/Bosses, on the `Boss.prefab` VARIANT of Enemy.prefab, spawned from its own small BossPool) do the jump,
  the smash, the phase transition and the death sequence. All numbers live in a BossData asset (Data/Bosses) referenced by
  the boss's EnemyData: HP (replaces the EnemyData's), settle time, distance band, phases (threshold, speed multiplier, attack
  list with pattern preset / windup / volleys / interval / angle step / cooldown / weight, glow), jump (a JumpTuning asset),
  smash, transition and death settings. Attack presets are AttackPattern assets; boss bullets use the Hot Magenta style.
- Every attack has its own readable windup (toolkit inflate + tremble + DANGER pulse; the Fast Shot adds a warning line, the
  jump a crouch and a DANGER landing ring). Bosses are ground-based; the jump is fake height like the player's (shadow stays
  on the ground, body sorts on the Airborne layer while high). Bullets still hit the airborne boss [tunable, `HittableInAir`].
- Screen-space boss health bar (name plate, fill, trail, phase tick) at the top of the combat HUD, driven by `BossEvents`.
  The Boss Round intro banner names the boss. The boss's death is a multi-stage sequence (stagger + flashes + debris,
  squash, dissolve, coin burst) that ends the round only when it finishes (`IDeathSequence` on Enemy).
- **Pumpking (round 3, done):** ground boss, ~2.7 P tall as drawn (kept at the standard painted-character import, pivot at the
  art's real base), one idle pose (`ArtSource/Bosses/Pumpking/boss_pumpking_idle.png`) + toolkit motion; windup/attack poses
  fall back to idle until drawn. 420 HP (x the round's difficulty), footprint radius 0.7, hurtbox 3.0 x 2.6.
  - Phase 1: Circle Spread (16-bullet ring x2, rotated), Fast Shot (1 aimed shot along a warning line), Jump & Smash (jumps
    onto the player's spot, landing ring telegraph, radial ground damage r=1.9 that hurts grounded players AND enemies, then 3
    quick rings).
  - Phase 2 at 50% HP: 1.6 s transition (invulnerable, roar inflate, flash cycles, shake, persistent shader glow), then denser
    offset rings (24 bullets x3), 3-shot Fast Shot bursts, Jump & Smash chains into a second jump, movement x1.35.
  - Tuning: `Data/Bosses/Boss_Pumpking.asset` (+ `Jump_Pumpking`, `Motion/Hit/LifeCycle_Pumpking`, `Pattern_Pumpking*`).
- Debug: Pause screen row has **Boss** (skip to the first boss round) and **HP- / HP+** (living boss's health in 10% steps);
  `GameConfig.debugStartRound` (editor/dev builds) makes New Game start at that round (0 = off).
- Rounds 5 and 7: still the placeholder boss wave. [TBD: their bosses.]

## Architecture rules (follow these strictly)
- All tunable data lives in ScriptableObjects: WeaponArmData, AmmoTypeData, ArmamentData,
  EnemyData, WaveData, RoundData, BossData, DifficultyCurve, InputTuning, JumpTuning, ArmRingTuning, ArmLoadout, PickupTuning,
  ShopPool, RarityTable, AssetRegistry, AttackPattern, PlayerData, ArenaData, ArenaLayoutData, TrapData, CosmeticData, SettingsDefaults.
  No gameplay numbers hard-coded in MonoBehaviours.
- ALL projectiles (player and enemy) use object pooling (UnityEngine.Pool.ObjectPool<T>).
  Never Instantiate/Destroy bullets during gameplay.
- Run flow is a single GameStateMachine: RoundIntro, Combat, RoundResults, Shop, Armory, Pause, GameOver.
- Systems talk through C# events or ScriptableObject event channels, not FindObjectOfType.
- Input only through the generated Input Actions class. No legacy Input Manager.
- Sprites are referenced from data assets / prefabs so art can be swapped without code changes.
- Placeholder art: simple colored shapes for enemies, bullets, pickups, bosses, UI.
  The player and weapon arms use MY sprites in Assets/Art/Player and Assets/Art/Arms - never replace them.

## Folder layout
Assets/
  Art/ (Player, Arms, Placeholder, UI/VoxKit, UI/Backdrop)
  Fonts/ (Cinzel Decorative, Lilita One, Nunito: TTFs, TMP SDF font assets, OFL texts)
  TextMesh Pro/ (Unity's TMP Essential Resources)
  Data/ (Arms, Loadouts, Ammo, Armaments, Pickups, Shop, Cosmetics, Arenas, Traps, Settings, Enemies, Waves, Bosses, Input)
  Prefabs/
  Scenes/ (Boot, MainMenu, Game)   (Shop and Armory are UI states inside Game)
  Scripts/ (Core, Save, Settings, Input, Player, Cosmetics, Weapons, Projectiles, Feedback, Enemies, AI, Arena, Bosses, Shop, Armory, UI, Platform)
  Tests/
Docs/Reference/ (concept art and references, OUTSIDE Assets so Unity doesn't import them)
ArtSource/ (4x master PNGs from Procreate, mirrors Assets/Art folders; OUTSIDE Assets)
Tools/ (scripts, e.g. export_art: downscales ArtSource 4x masters 50% into Assets/Art as 2x game PNGs)

## Working agreement
- One milestone per session. Propose a plan first; wait for my OK before large changes.
- After code changes, check the Unity console for compile errors and fix them before reporting done.
- Keep summaries short: what changed, which files, what I should playtest.
- Don't refactor unrelated code. Don't rename or move my art assets.
- Never commit or push without asking me. When I approve, commit with a message like "M1: <summary>".
- Never touch Library/, Temp/, Logs/ or UserSettings/.
- When testing in Play mode, back up run_save.json, settings.json and profile.json first and restore them
  afterwards. Delete test screenshots/artifacts when done. Use guarded paths in rm commands (${VAR:?}).
- Bugs live in Docs/BUGS.md. Fix one bug at a time: reproduce it, fix it, verify, then mark it fixed there
  with a one-line note of the cause. If you notice a new bug while working, add it to the list; don't fix
  unrelated bugs silently.
- Claude maintains the project docs. When a task changes the design, adds or renames something, or finishes art or milestones, update CLAUDE.md, Docs/ART_SPEC.md, Docs/ART_CHECKLIST.md and Docs/BUGS.md directly as part of the task: keep existing checkmarks and notes, edit only the relevant sections, and end the task with a short 'Docs updated' list of what changed. The user no longer swaps in doc files manually; design changes arrive as prompts.
- At the end of every milestone, update Docs/CaseStudy: add a DevLog entry, update Architecture and GameFlow diagrams if structure changed, update the relevant Systems deep dive, update the Roadmap and Metrics. Note which model was used and anything the agent got wrong.
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
- [x] M5a Combat: player health/hitbox/i-frames/Game Over, AttackPattern (was planned as BulletPatternData), pooled enemy bullets,
      4 starter enemy types (Grunt, Spinner, Charger, Sniper), enemy test mode to spawn each type.
- [x] M5b Rounds: WaveData/RoundData for rounds 1-7, wave spawner replacing the stub round,
      coin drops + magnet + round-end collection, DifficultyCurve scaling, currency on Round Results.
- [x] M6 Front end: Main Menu (New Game/Continue/Settings/Quit), Settings screen + settings file,
      Gladiator customization screen with placeholder cosmetics + profile file, Round intro banner and
      countdown state. Skeleton UI, fully controller navigable.
- [x] M7 Arena: ArenaData, colosseum bounds, solid + breakable obstacles (block all bullets),
      3 starter traps (hurt player and enemies, telegraphed), one test arena layout.
- [x] M7.5 Perspective + HUD (converts M7's arena): Y-sorting, feet pivots, footprint colliders on player/enemies/obstacles/traps,
      layered placeholder arena (floor/back wall/foreground), combat HUD (portrait, 5 hearts, heat bar, 4 ammo slots with glyphs).
- [x] M7.6 Jump: R2 jump with fake height (arc, shadow, squash/stretch, dust), pass over enemies/contact damage/
      ground traps, still hit by bullets and blocked by obstacles, airborne sorting, landing push-out.
- [x] M7.7 Arm ring: arms on a flattened ellipse around the feet, front/back sorting around the body,
      locked aim slides along the ellipse, muzzles/bullet spawn consistent, bullet height + damage-core
      position consistent with ground-plane collision, ArmRingTuning.
- [x] M8 Enemy AI rework: flow-field navigation + separation, line of sight, Chaser / Skirmisher /
      Mobile Sentry, convert existing enemies, retune rounds 1-7 for the arena. Enemies account for the
      player's jump (chasers keep tracking the shadow; chargers can be jumped).
- [x] M8.5 Arena progression + height classes: Low/Tall obstacle classes, jumping over Low obstacles,
      Tall-obstacle fade when something is behind it, ArenaLayoutData per round with HazardBudget
      (round 1 has no traps/hazards, ramping up after), clear spawn areas and lanes, retune rounds 1-7.
- [x] M8.6 Animation toolkit: procedural motion + feedback components, hit flash / dissolve / outline /
      pulse / tint / UV-scroll / wave shaders, particle presets, tween library, 2D Animation + PSD Importer
      installed with a rigged test character; apply to player and current enemies (placeholders).
- [x] Art scale test: backdrop + Chaser hooked up, 16:9 framing, character scale 1.5x tried, 1.15x chosen after playtesting.
- [x] C1 Cleanup: 1.5x character scale baked via import PPU + data (player 315->210, arms 400->266.67, painted enemy 220->146.67,
      placeholder enemy sizes x1.5), CharacterScale + F5 key removed, damage core kept at 0.18, arm ring, muzzles,
      jump height, footprints and nav radius retuned. ScaleTestArt renamed ArenaArt (backdrop only).
- [x] Scale lock: after playtesting 0.75x-1.5x, character scale locked at 1.15x of the original spec (player 315->273.9 PPU, arms 400->347.8,
      painted enemy 220->191.3, enemy sizes, muzzles, ring, jump, footprints, nav radius re-baked); temporary F5 scale test removed.
- [x] UI1 UI theme: UITheme asset (roles -> sprites/colors/font/sounds) on GameConfig, ThemedImage/ThemedButton read it; its stand-in sprite pack was
      replaced by the VoxVegetallis kit in UI2.
- [x] UI2 VOX VEGETALLIS hot swap: the VoxVegetallis theme is the only UI theme (see "UI theme" above). Kit imported from its manifest, TMP fonts,
      all legacy Text converted to TextMeshPro, Mega Cozy pack + theme removed, Main Menu / HUD / Shop / Armory / Character Creation / Settings rebuilt to
      the mockups, game renamed to VOX VEGETALLIS, Product Name VoxVegetallis (save path changed, old saves not carried over).
      Known differences from the mockups are logged in Docs/BUGS.md.
- [x] CC1 Character Creation v2: paper-doll player (Body, Armor, Head, Accessory 1 head anchor, Accessory 2 back anchor) via 2D Animation
      Sprite Library + Sprite Resolver, CosmeticPartData assets (3 placeholder variants per slot, Playersprite = Body 1), new creation screen
      (live preview, slot cycling, Randomize on Square, "To the Arena!"), profile v2, HUD portrait from head + accessory 1.
- [ ] Vertical slice art for one arena (Docs/ART_SPEC.md section 9).
- [x] M9a Armament behaviors: effect interface, variable armament slots per arm, rarity/tags/stacks,
      Homing, Auto-fire, Velocity, Pierce, Ricochet with documented interactions, generated descriptions.
- [x] M9b Shop screen: merchant + card layout, ShopPool/RarityTable random stock, crate (pick 1 of 3), reroll,
      scaling prices, tooltips with fit/comparison, buy animation + SOLD, controller-first navigation.
- [x] M9c Armory screen: gladiator + arm ring on the left with hovering 1-3 armament bubbles above the selected
      arm, tabbed inventory grid on the right, arm -> bubble -> item equip flow with before/after preview,
      dimmed incompatible items, controller-first navigation.
- [x] M9d UI foundation + bug bash: shared UI components (card, tooltip, button, panel), consistent focus
      and navigation, screen transitions, UI sound hooks; work through Docs/BUGS.md.
- [x] M10 (round 3) The Pumpking: BossData/BossBehavior/BossController/BossJump, Circle Spread / Fast Shot / Jump & Smash,
      phase 2 at 50% with transition + glow, boss health bar + named intro banner, big death sequence, Boss prefab variant +
      BossPool, debug Boss / HP buttons and debugStartRound. Painted idle hooked up (windup/attack poses pending).
- [ ] M10 (rounds 5 and 7) Bosses.
- [~] P1 Performance pass (before M11): PARTIAL. Tools built (overlay F8, stress test F10 / `-perfstress`, PerfLogger CSV, `Tools/perf_analyze.ps1`, `Scripts/Perf`),
      baseline recorded (uncapped dev build, stress test: ~165 fps avg, p99 ~14 ms, occasional 30-70 ms hitches with the cause not yet attributed, ~180 GC allocs/frame);
      fixes and the final test are tabled until content is near complete (see Docs/BUGS.md "P1 hitches"). Run: build a Development player to `Builds/Perf`, then
      `BulletHell.exe -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -perfstress -perflabel X -perfseconds 90 -perfenemies 80 -perfvsync 0`; CSVs go to
      `%USERPROFILE%\AppData\LocalLow\DefaultCompany\VoxVegetallis\PerfLogs`. Original scope:
      measure first, then fix. Toggleable performance overlay (FPS, frame-time graph, GC alloc
      per frame, active bullets/enemies/particles/pool counts), PerfLogger (frame time, GC alloc, top Profiler markers via
      ProfilerRecorder -> CSV), debug stress test (max enemies, Pumpking's heaviest patterns, Ricochet + Pierce + Homing on all
      arms), dev-build run + CSV analysis (worst spikes and causes reported before any fix), fixes for the top offenders
      (per-frame allocations, unpooled objects, flow-field / line-of-sight / physics query costs, SRP batching warning, overdraw
      and shaders, frame pacing, hitstop stutter), before/after numbers. Target: steady 60 fps, no visible hitches in the stress test.
- [ ] PF1 Case study documentation system: Docs/CaseStudy (narrative, architecture, game flow, system deep dives, dev log, roadmap, agentic workflow,
      art pipeline, metrics, reflections template), backfilled from git history, CLAUDE.md history, BUGS.md and the code; inferences marked [VERIFY].
      From now on it is updated at the end of every milestone (see the Working agreement).
- Demo readiness track (D1-D6): each milestone is its own session; none started yet.
- [ ] D1 UI polish (art-independent): spacing, alignment, text overflow (long names, 5-digit numbers), focus states, motion timings from the VoxKit
      manifest, and layouts checked at 1920x1080, 2560x1440 and a phone resolution. Screens: HUD, Settings, Round Results, Pause, Game Over, round
      banner, boss bar. The Shop merchant panel and the Character Creation preview wait for art.
- [ ] D2 Onboarding: skippable, action-driven control prompts in round 1 (move, select arm, fire, L3 lock, R2 jump, ammo swap), glyphs for the
      connected controller, completion saved in the profile, replay option in Settings.
- [ ] D3 Playtest telemetry + balance tools: local-only CSV per run (round reached, cause of death, time and damage taken per round, currency
      earned/spent, items bought, armaments equipped, boss phase reached) and a debug summary screen.
- [ ] D4 Audio: music per context with crossfades, SFX for combat, movement, pickups, UI and announcer stingers, mixer groups tied to the Settings
      volumes, CC0 placeholder sounds logged in Docs/CREDITS.md.
- [ ] D5 Settings completion: Video (resolution, fullscreen/windowed, VSync), Controls (button remapping via the Input System, arm-select
      sensitivity), Gameplay and accessibility (screen shake intensity, bullet outline thickness, high-contrast bullets, HUD scale).
- [ ] D6 Build pipeline: Windows and WebGL builds from one editor menu with version numbers, plus a WebGL test report (controller, saving, audio,
      performance) and a verdict on an itch.io browser demo.
- [ ] M11 Themed UI/visual pass: candy-colosseum style for menus, HUD, Shop, Armory, customization; final art.
- [ ] M12 Polish: touch controls (incl. jump button), button glyphs, juice, announcer/audio, performance pass
      (the performance pass also covers the 2D SRP Batcher warning on Mat_SpriteCharacter / Mat_SpriteOutline: _TexelSize / _ST properties in the Sprite Unlit graphs).
