# Art Checklist: Vertical Slice (Rounds 1-3 + Pumpking)

Goal: everything needed so one full run from the Main Menu through the round 3 boss uses final art.
Sizes use P = player body height (**506 px on 4x master canvases**, 1.15x character scale). Templates: `procreate_templates_4x_P506.zip`.
Animation is HYBRID: draw key poses only; code does breathing, bobbing, squash, hit flash, windup tremble and deaths.
Priority: **[1]** scale test / blocks gameplay feel, **[2]** vertical slice core, **[3]** slice polish.
Tip marked "free" = fine to use a CC0/licensed pack instead of drawing it yourself.

---

## A. Player (you have the body + 4 arms: check these are complete)

- [x] Player body (base look)
- [x] Arms x4 (red, blue, green, yellow), pointing right, attach point on the pivot
- [ ] [2] Player poses, facings down / side / up (template: character 1536):
  - [ ] idle (1)  - [ ] run cycle (4)  - [ ] jump takeoff (1)  - [ ] jump land (1)
  (hit, death, breathing, bounce: code)
- [ ] [2] Damage-core marker: small glowing dot shown low on the body (bullet 384 template)
- [ ] [2] Soft-select outline and Locked outline for arms (or let code tint them: ask Claude first)

## B. Enemies (side-facing only for the slice; flipped in code)

**Chaser** (0.8P, character 1536)
- [ ] [1] idle (1)  - [ ] [2] windup/crouch (1)  - [ ] [2] lunge (1-2)

**Skirmisher** (1P, character 1536)
- [x] [2] idle (1)  - [ ] aim windup (1)  - [ ] shoot (1-2)
  (idle hooked up on `Enemy_Weaver` from `ArtSource/Enemies/Skirmisher`: mushroom, pivot at its base. Windup/shoot poses not drawn yet:
  the toolkit's windup pulse plays on the idle pose in the last 0.3 s before each shot, visual only.)

**Mobile Sentry** (1P tall, 1.2P wide, character 1536)
- [ ] [2] moving (1)  - [ ] planted (1)  - [ ] firing (1-2)  - [ ] overheat (1, vents glowing)

**Pumpking, round 3 boss** (~2.7P as drawn, boss 2560 template, key poses like the enemies)
- [x] [2] idle (1)  - [ ] [2] windup (1, a clear "about to attack" silhouette)  - [ ] [2] attack (1-2)
  (idle hooked up in M10 from `ArtSource/Bosses/Pumpking`, pivot at the art's base; the jump, smash, phase 2 glow,
  transition and death are code; missing poses fall back to idle)
- Later (deferred): body in PART layers for rigging, phase 2 replacement parts, face swaps

Later (not in the slice): Charger, Sniper, more enemy types, bosses for rounds 5 and 7.

## C. Projectiles (bullet 384 template)

Player bullets: draw in WHITE / light gray so the game can tint them with the arm's color.
- [ ] [1] Basic round
- [ ] [2] Shotgun pellet
- [ ] [2] Laser beam: start cap, middle (tileable), end cap, impact (3 frames)
- [ ] [2] Gatling round (smaller, elongated)

Enemy bullets: reserved enemy colors, bright core + dark outline, 2-3 frame pulse.
- [ ] [1] Pea (Skirmisher)
- [ ] [2] Kernel (Sentry, smaller than the pea)
- [ ] [2] Boss large slow orb
- [ ] [2] Boss small fast shard
- [ ] [2] Ground-slam shockwave ring: FLAT on the floor (squashed x0.6), 4-6 frames expanding

## D. VFX (mostly PARTICLES in code; you draw a few small textures)

Particle textures (white, small, reused everywhere):
- [ ] [2] soft dot  - [ ] spark streak  - [ ] smoke puff  - [ ] shard  - [ ] juice droplet
Drawn effects:
- [ ] [2] Muzzle flash (white, tinted in code)
- [ ] [2] Bullet impact spark (on walls/obstacles)
- [ ] [2] Enemy hit spark
- [ ] [2] Enemy death burst (juice splat) + [ ] floor splat decal (low contrast, flat)
- [ ] [2] Enemy spawn puff (coming out of a gate)
- [ ] [2] Dust puff (run, jump landing)
- [ ] [2] Danger telegraph: floor glow circle (flat, white, tinted DANGER color) + pulse
- [ ] [2] Overheat steam (player arm + Sentry)
- [ ] [3] Ricochet spark, pierce flash, homing trail (for armaments)
- [ ] [2] Breakable debris chunks (3-4 small pieces)
- [ ] [3] Boss summon effect, boss phase burst, boss death explosion
- [ ] [3] Coin sparkle, pickup glow
Free: generic sparks/dust/smoke packs work fine here if you want to save time.

## E. Pickups

- [ ] [2] Coin (spin, 6 frames) (bullet 128)
- [ ] [2] Ammo pickup: one base (e.g. amphora or crate) + the ammo icon floating above it
- [ ] [2] Floor glow ring under pickups (flat, x0.6)

## F. The stadium (vegetable colosseum)

Full arena canvas: **7680 x 4320** (4x master; use tpl_arena_7680x4320 for layout). Draw each part on its own canvas or layer
group and export them separately. Match your concept_arena.png layout.

- [ ] [1] Floor tiles: 2-3 variants, tileable (1P x 0.6P per tile, flat)
- [ ] [2] Floor decals (flat, lower contrast): central crest, graffiti x3, cracks x3, splatter x3
- [ ] [2] Back wall: stands with crowd, 2 enemy gates, royal box (the Pumpking's throne)
- [ ] [2] Gates: closed / opening (3) / open (enemies spawn here)
- [ ] [2] Side walls left + right, plus corner pieces
- [ ] [2] Foreground: front railing + front crowd heads (keep it SHORT, it covers the play area)
- [ ] [3] Crowd animation: 2-3 frame cheer loop per crowd section
- [ ] [3] Torches (4-frame flame loop), banners (3-frame sway)
- [ ] [3] Outer filler: extra crowd/wall art for wider (21:9) and taller (4:3, phones) screens

## G. Obstacles (height classes from ART_SPEC section 3)

Low (jumpable, <= 0.5P, low wall 512x256 template):
- [ ] [1] Low wall set: straight, end-left, end-right, corner (with the "jumpable" cap color)
- [ ] [2] Breakable low crate row: intact, dmg1, dmg2, debris (flat)

Tall (>= 1.5P, tall 512x768 template):
- [ ] [1] Pillar (solid) + [ ] broken-pillar variant (solid)
- [ ] [2] Breakable giant pumpkin/tomato: intact, dmg1, dmg2, debris + chunks
- [ ] [3] Statue (solid, e.g. a vegetable legionnaire)

- [ ] [2] Contact shadow sprite for each obstacle (separate `_shadow` file)

## H. Traps (floor trap 1536x1152 template; flat, x0.6)

Each needs: idle, telegraph (2-4 frames, DANGER color), active (3-4), cooldown (2-3).
- [ ] [1] Floor vent (e.g. steam grate)
- [ ] [2] Spike line (e.g. kebab skewers popping up: upright parts drawn vertical)
- [ ] [2] Hazard zone (e.g. bubbling hot sauce puddle that ticks damage)

## I. Combat HUD (icon 768 template unless noted)

- [x] [2] Portrait frame (the spiky badge from the concept) + gladiator portrait (base; cosmetics layered)  (UI2: VoxKit checkered leaf ring, not the spiky badge)
- [~] [2] Heart: full, empty, lose-heart animation (3)  (UI2: tomato hearts full/empty done; lose-heart pop is code)
- [x] [2] Heat bar: frame, fill, overheat fill/flash (9-slice friendly: even borders)  (UI2: track + white fill tinted by code)
- [~] [2] Ammo slot frame: normal, active, empty + hold-progress ring  (UI2: frames + active ring done; hold-progress ring still a placeholder)
- [x] [2] Ammo icons x4: Basic, Shotgun, Laser, Gatling  (UI2: VoxKit)
- [x] [2] Coin counter icon  (UI2: icon_coin_seed)
- [ ] [2] Boss health bar frame + boss name plate
- [ ] [2] Round banner frame ("Round 1 - Begin!") + Boss Round variant
- [ ] [3] "Wave X/Y" banner
- [ ] [2] Button glyphs: PlayStation, Xbox, Nintendo sets (cross/circle/square/triangle, L1/R1/R2, L3, Options)
  Free: use a CC0 input-prompt pack (e.g. Kenney input prompts) instead of drawing these.

## J. Menus and screens

General UI kit (a FREE UI PACK can cover most of this; log it in Docs/CREDITS.md. If drawing: 9-slice, even borders):
- [x] [2] Panel frame (large + small)  (UI2: panel_marble / shade / wood / corn, pills)
- [x] [2] Button: normal / focused / pressed / disabled  (UI2: + primary button)
- [x] [2] Slider: track, fill, handle  - [x] Toggle: on/off  - [x] Selector arrows (left/right)
- [x] [2] Tab: normal / selected
- [x] [2] Tooltip panel  (UI2: panel_marble; the Shop detail panel)
- [x] [2] Fonts: 1 display font (Roman/candy title style) + 1 very readable UI font. Buy/license, don't draw.  (UI2: Cinzel Decorative, Lilita One, Nunito, see Docs/CREDITS.md)

Main Menu:
- [~] [2] Game logo / title art  (UI2: logo is live Cinzel Decorative text with corn/soil/leaf layers, no drawn art)
- [~] [2] Menu background (key art or a wide arena shot)  (UI2: generated blur of the arena backdrop as a placeholder)

Character Creation (modular paper doll, see ART_SPEC 6b; side facing first):
- [ ] [2] Mannequin canvas (base body + plain head) on tpl_character_1536
- [ ] [2] Body x3  - [ ] Armor/body kit x3  - [ ] Head x3  - [ ] Accessory 1 (head area) x3  - [ ] Accessory 2 (back/torso) x3
- [x] [2] Preview pedestal/spotlight  - [x] slot selector arrows (or from the UI pack)  (UI2: pedestal_marble + spotlight_arch, arrow buttons)
- [ ] [3] Screen background (e.g. a gladiator locker room under the stands)

Round Results:
- [ ] [3] Results panel art (can reuse the UI kit panel)

Shop (Balatro / Slay the Spire style):
- [ ] [2] Merchant NPC (e.g. Roman fig/olive trader): body in PARTS for rigging + face swaps (neutral, happy on purchase)
- [ ] [2] Stall/table background
- [ ] [2] Item card frame x4 rarities: Common, Rare, Epic, Legendary (card canvas ~1024 x 1536)
- [ ] [2] Card back (for the crate pick) + crate closed/open
- [ ] [2] Price tag, SOLD stamp, reroll icon
- [ ] [2] Armament icons x5: Homing, Auto-fire, Velocity, Pierce, Ricochet
- [ ] [2] Arm card art x4 (can reuse your arm sprites, larger)

Armory:
- [ ] [2] Armory background (e.g. a weapons tent or barracks)
- [ ] [2] Gladiator pedestal
- [ ] [2] Arm slot frame: empty / filled / selected
- [ ] [2] Hovering armament slot BUBBLE (floats above the selected arm): empty (with "+"), filled
  (holds an armament icon; rim tinted by rarity in code, so draw the rim white), focused (outline)
- [ ] [2] Bubble tether: thin glowing line/strand linking bubble to arm (white, tinted in code)
- [ ] [2] Inventory grid cell, stat up/down arrows (green/red), "Fight!" button

Pause and Game Over:
- [ ] [3] Pause panel (UI kit)
- [ ] [3] Game Over title art (e.g. the crowd giving a thumbs-down)

---

## Rough totals for the slice (hybrid animation)

| Group | Approx. sprites/frames |
|---|---|
| Player poses (3 facings) | ~20 |
| Enemies (side only, key poses) | ~12 |
| Boss (Pumpking key poses) | ~4 |
| Projectiles | ~15 |
| VFX (particle textures + a few drawn effects) | ~25 |
| Pickups | ~8 |
| Stadium | ~35 |
| Obstacles | ~20 |
| Traps | ~25 |
| HUD | ~35 |
| Menus + Shop (merchant in parts) + Armory | ~55 |

About **250-300 sprites** in total (down from ~500 with frame-by-frame animation). Free packs for glyphs,
fonts and some VFX cut it further.

## Suggested order

1. **Scale test** (all [1] items, rough): player, Chaser idle, low wall, pillar, floor vent, floor tile, basic bullet, pea.
2. **Combat look:** stadium floor + walls + foreground, obstacles, traps, Chaser/Skirmisher/Sentry, bullets, core VFX, HUD.
3. **Boss:** Pumpking windup/attack poses, shockwave, boss HUD.
4. **Screens:** UI kit first (panels, buttons, cards), then Shop, Armory, Main Menu, customization.
5. **Polish:** crowd/torch/banner animation, extra VFX, Game Over art.
