# Art Spec: Candy Gladiator / Vegetable Colosseum

Everything drawn for the game follows this sheet so it fits on the first try.
Values marked (start) are starting points: confirm them in the scale test (section 9), then lock them.
Keep this file at `Docs/ART_SPEC.md`. Claude reads it when hooking up art.
Templates: `procreate_templates_4x_P660.zip` (4x masters, P = 660). Older packs are retired.

---

## 1. Style decision (pick one, then never mix)

| | Option A: Painted HD (matches the concept) | Option B: Pixel art |
|---|---|---|
| Look | Inked comic outlines, painted shading | Crisp pixel grid |
| Reference screen | 1920 x 1080 | 480 x 270 canvas, shown at x4 |
| Pixels per unit (PPU) | 100 | 32 |
| Player body height | ~110 px on screen (start) | ~28 px (start) |
| Draw at | 2x final size, export at 2x | 1x, never scale in the art program |
| Unity filter | Bilinear, compression High Quality | Point (no filter), no compression |

Chosen style: **A (painted HD)**. **Character scale locked at 1.15x after the scale test (playtested 0.75x-1.5x):**
player body height in game: **126.5 px at 1920x1080**. The arena keeps its original scale.

**Resolution pipeline (Option A):**
| | Scale | P (player height) | Arena / screen | Used for |
|---|---|---|---|---|
| Master (you draw here) | 4x | **660 px** | 7680 x 4320 | Procreate source art, kept in `ArtSource/` |
| Game export | 2x | 330 px | 3840 x 2160 | PNGs in `Assets/Art/` (made from masters by a script) |
| On screen at 1080p | 1x | 165 px | 1920 x 1080 | What players see; 4K screens show the 2x detail |

Use the **P660 template pack (`procreate_templates_4x_P660.zip`)** for masters. Older packs are retired.
Character art already drawn at P = 440 still works (the game scales it via import settings), but redraw
final versions at P = 660 so they stay sharp on 4K screens. Export PNGs at master size into
`ArtSource/<same folders as Assets/Art>`; Claude's `Tools/export_art` script downscales them by 50% into
`Assets/Art/`. Never paint directly at 2x once masters exist.

Everything below uses **P = the player body's height** (feet to top of head, not counting headgear),
so sizes stay correct whichever style you pick.

---

## 2. The camera and perspective rules

The game uses a **3/4 top-down (oblique) view**: the camera looks down at the arena at an angle.
It is NOT true isometric (no diamond grid) and NOT true perspective (no vanishing points).

**Rule 1: Vertical lines stay vertical.** Walls, pillars, torches and characters stand straight up.
Their sides never lean or converge. This is what keeps everything tileable and sortable.

**Rule 2: The floor is squashed vertically by one ratio: F = 0.6 (start).**
Anything lying flat on the ground is drawn at full width but only F times as tall:
- A circle on the floor becomes an ellipse **1.0 wide x 0.6 tall**.
- A square floor tile becomes a rectangle **1.0 wide x 0.6 tall**.
- The **same F** is used for: trap areas, shadows, footprints, the arm ring around the feet,
  pickup glows, the flow of crest/graffiti decals. (The arm ring's ellipse ratio in ArmRingTuning must equal F.)

**Rule 3: You see two faces of every solid object: the TOP and the FRONT (south-facing) side.**
Never the back, never the underside. The top face is squashed by F; the front face is drawn at full height.

```
   Tall pillar            Low wall                 Floor trap
    ______                 ________________
   /______\  <- top (xF)  /________________\ <- top (xF)     .-""""-.
   |      |               |________________| <- front       (  vent  )  <- flat, whole thing xF
   |      |  <- front      (short front face)                 `-....-'
   |      |     (full height)
   |______|
   ( ____ )  <- contact shadow / footprint (xF)
       ^ pivot: center of the footprint's front edge... see section 4
```

**Rule 4: Light comes from the top-left, always.** Highlights on top-left edges, shading on the
bottom-right. Cast shadows fall slightly down-right, short and soft (it's midday in the colosseum).

**Rule 5: Height reads as "up the screen."** Something 1 P tall occupies 1 P of screen height above its
footprint. The jump works the same way: the body moves up the screen, the shadow stays put.

---

## 3. Height classes (this decides gameplay, so it must read instantly)

| Class | Visual height | Jumpable? | Blocks bullets? | Examples |
|---|---|---|---|---|
| Flat | 0 (on the floor) | Yes (it's under you) | No | Traps, hazard zones, decals, debris |
| Low | up to **0.5 P** | **Yes** | **Yes** (cover) | Low walls, veggie crate rows, fences, bread-loaf barricades |
| Tall | **1.5 P and up** | No | Yes | Pillars, statues, big pumpkins, towers |
| Boundary | Arena walls | No | Yes | Colosseum walls, gates, stands |

- Keep a **clear gap** between Low (max 0.5 P) and Tall (min 1.5 P). Nothing in between, or players
  can't tell at a glance whether they can jump it.
- The jump apex (JumpTuning) must visibly clear Low objects: body lifts at least **0.7 P** at the top.
- Give Low obstacles a consistent visual language (e.g. always a flat top with a colored cap or rim)
  so "I can hop this" becomes instinct.
- Tall obstacles hide things behind them. In game they fade to ~40% when a character is behind them
  (handled in code), so draw them as solid shapes; don't try to leave see-through gaps.

---

## 4. Pivots, footprints and canvas setup

Every standing sprite (player, enemies, obstacles, rising traps) is set up the same way:

- **Pivot = the center of the object's footprint on the ground** (where it touches the floor).
  Unity sorts by this point, so a wrong pivot = wrong overlap.
- **Footprint** = the object's base shape on the floor, squashed by F. It becomes the collider.
  Draw it on a separate "guide" layer while working (a light ellipse or rectangle at the base), then hide it on export.
- Leave **4 px transparent padding** on every side of every frame.
- For animation frames, the **feet must stay on the same pixel row in every frame** and the canvas size must be
  identical for all frames of that character. Otherwise the character jitters.
- Draw a **soft contact shadow** under static objects (pillars, walls) on its own layer and export it
  as a separate `_shadow` sprite. Characters get their shadow from code (it moves during jumps), so don't paint one on them.

---

## 5. Size guide (relative to P)

| Asset | Size (width x height) | Notes |
|---|---|---|
| Player body | ~0.7 P x 1 P | Headgear cosmetics may add up to 0.3 P on top |
| Arms (each) | ~0.5 P long | Drawn pointing RIGHT, pivot at the attach point (shoulder/mount end) |
| Chaser | 0.7 - 0.9 P tall | Smaller and faster reads as "rushes you" |
| Skirmisher | ~1 P tall | Lean, readable weapon |
| Mobile Sentry | ~1 P tall, 1.2 P wide | Wheels/base, visible barrel, cooling vents that glow when overheated |
| Charger | ~1.1 P tall | Heavy, forward-leaning silhouette |
| Sniper | ~1 P tall | Long barrel, clear aiming pose |
| Bosses | 2.5 - 5 P tall | Designed per boss |
| Player bullets | 0.15 - 0.25 P | Tinted by arm ID color |
| Enemy bullets | 0.2 - 0.3 P | Bright core + dark outline, reserved colors (section 7) |
| Pickups (ammo, coins) | 0.3 - 0.4 P | Gentle bob + glow on the floor (glow ellipse xF) |
| Low wall segment | 1 - 2 P wide, 0.4 - 0.5 P tall | Tileable left/right; end caps; corner pieces |
| Tall pillar | 0.8 - 1.2 P wide, 1.8 - 2.5 P tall | |
| Breakable crate/pumpkin | 0.8 P wide, Low or Tall class | Decide its class and draw to that height |
| Floor trap footprint | 1 - 2 P wide (xF tall) | Flat |
| Floor tile | 1 P wide x 0.6 P tall (start) | Tileable edges |

---

## 6. How to draw each asset type

**Arena (always layered, never one flat image):**
1. `floor`: tileable tiles plus separate decal sprites (crest, graffiti, cracks, splatter). Decals are
   lower contrast than anything interactive.
2. `backwall`: top edge of the arena: stands, crowd, royal box, gates. Front faces fully visible.
3. `sidewalls_left` / `sidewalls_right`: drawn as vertical walls seen at the 3/4 angle (top face xF, inner face visible).
4. `foreground`: bottom edge: railing and front crowd, drawn over gameplay. Keep it SHORT (it covers the play area).
5. Props with animation (torches, crowd waves, banners) as separate small sprites so they can move.

**Low walls:** draw as modular pieces: `straight`, `end_left`, `end_right`, `corner`. Top face xF,
short front face, consistent "jumpable" rim/cap color. Separate `_shadow`.

**Tall obstacles:** full front face, top xF, strong silhouette. If breakable: 3 damage stages
(`_dmg0`, `_dmg1`, `_dmg2`) + `_debris` (flat, non-blocking) + a few chunk sprites for the break burst.

**Floor traps and hazard zones:** drawn completely flat (whole thing squashed by F). Every trap needs these states:
- `idle`: visible but calm (the player must always be able to spot a trap)
- `telegraph`: warning (glow/pulse in the reserved DANGER color, 2-4 frames)
- `active`: the damage moment (burst, flames, spikes)
- `cooldown`: settling back
Parts that rise out of the floor (spikes, skewers) are drawn upright (Rule 1) on top of the flat base.

**Characters (player + enemies): hybrid animation.** Draw KEY POSES; code adds the motion
(breathing, hop-walk bob, lean, squash/stretch, hit flash, knockback, windup tremble, death splat/dissolve).
See CLAUDE.md "Animation approach".
- Player (facings `down`, `side` flipped in code, `up`): `idle` (1 pose), `run` (4-frame cycle),
  `jump_takeoff` (1), `jump_land` (1). No hit/death frames needed (code handles them).
- Regular enemies (side-facing only for now): `idle` (1), `windup` (1, a clear telegraph silhouette),
  `attack` (1-2). Sentry adds `planted` (1) and `overheat` (1, vents glowing).
- Bosses and the merchant: drawn in PARTS for rigging (see below), plus alternate parts for phase changes.
- Readable poses beat smooth motion in a bullet hell.

**Rigged characters (bosses, merchant):** one body part per layer, named `head`, `torso`, `arm_L`, `arm_R`,
`leg_L`, `leg_R`, `weapon`, etc. Draw a little extra where parts overlap (e.g. the top of an arm continues
under the shoulder) so no gaps show when parts rotate. Export with Procreate Share -> PSD into `ArtSource/`.

**Bullets:** simple, bold shapes. Player bullets can vary per ammo type. Enemy bullets: round or
diamond shapes with a bright core and dark outline, 2-3 frame pulse.

**HUD and icons:** heart (full/empty), heat bar frame + fill + overheat state, ammo slot frame (normal/active/empty),
ammo icons (one per ammo type), face-button glyph sets (PlayStation / Xbox / Nintendo / touch), portrait frame.

---

## 6b. Modular player (Character Creation parts)

The player is a paper doll of swappable parts, all drawn on **the same `tpl_character_1536` canvas** and in
the same pose, so they stack perfectly with no offsets:

| Part | Layer order (back to front) | Notes |
|---|---|---|
| Body | 1 | The base body. Every other part must fit over every body variant. |
| Armor / body kit | 2 | Worn over the body; keep within the body's silhouette plus a little bulk. |
| Head | 3 | Neck joins the body at the same spot on every variant. |
| Accessory 1 | 4 | Head area (hats, horns, crests). Stay under the 1.3P line. |
| Accessory 2 | 5 (or behind the body for capes) | Back/torso (capes, banners, badges). |

- Make one "mannequin" canvas: the base body + a plain head. Draw every new part on its own layer over it,
  hide the mannequin, and export each part as its own PNG (full 1536 canvas, transparency around it).
- Names: `player_<part>_<variant>_<facing>.png`, e.g. `player_head_gummy_side.png`, `player_acc1_crown_side.png`.
- Keep the **neck and shoulder joins identical** across variants so any head fits any body.
- For the portrait, the same head + accessory 1 are reused, so no separate portrait drawings are needed.
- Start with the side facing only; add down/up later.

## 7. Color and readability rules

- **Reserved enemy-bullet colors:** pick 2 colors that appear NOWHERE in the floor, walls or decals:
  **[color 1] [color 2]**. Enemy bullets use only these.
- **Reserved DANGER color** for trap telegraphs and enemy wind-ups: **[color]**. Not used decoratively.
- **Player-owned colors:** the arm ID colors (red / blue / green / yellow). Enemies don't use them.
- Floor and decals: lower saturation and contrast than characters, bullets and obstacles.
  Check: convert a screenshot to grayscale; characters and bullets should still pop.
- Every character, obstacle and bullet has a dark outline so it separates from the busy floor.

---

## 8. Export and file rules

- PNG, transparent background, sRGB. No baked-in background color.
- Names: `category_name_state_##.png`, lowercase, underscores, no spaces or symbols.
  Examples: `enemy_chaser_run_down_01.png`, `trap_vent_telegraph_02.png`, `obstacle_pillar_dmg1.png`,
  `arena01_floor_tile_a.png`.
- Animations: either numbered single frames or one sprite sheet per animation with a uniform grid
  (note the grid size in the file name: `enemy_chaser_run_down_64x64.png`).
- Folders (bring NEW files in with File Explorer; move/rename existing ones inside Unity only):

```
Assets/Art/
  Player/        Arms/        Cosmetics/
  Enemies/<EnemyName>/
  Bosses/<BossName>/
  Arena/<ArenaName>/ (Floor, Walls, Foreground, Props)
  Obstacles/     Traps/       Pickups/
  Projectiles/   UI/ (HUD, Icons, Glyphs, Menus)   VFX/
```

---

## 9. Scale test (do this before drawing final art)

1. Rough sketches only: player, 1 enemy, 1 low wall, 1 pillar, 1 floor trap, 1 floor tile, 1 enemy bullet.
2. Drop them in, ask Claude to hook them up (prompt below), and play.
3. Check:
   - [ ] The player feels the right size in the arena (not lost, not cramped).
   - [ ] Low wall is obviously jumpable; pillar obviously isn't.
   - [ ] The jump visibly clears the low wall.
   - [ ] Enemy bullets pop against the floor, including in grayscale.
   - [ ] The floor ellipses (trap, shadows, arm ring) all look like they lie on the same ground.
   - [ ] Readable on a phone screen (or the Game view set to a small phone resolution).
4. Adjust P, PPU or F in this file, not the art. Then lock the values and start final art.

**Hook-up prompt for Claude** (masters exported at 4x to `ArtSource/ScaleTest/`):
```
Read Docs/ART_SPEC.md. Run Tools/export_art for ArtSource/ScaleTest (create the script first if it doesn't
exist: 50% high-quality downscale of 4x masters into the matching Assets/Art folder). Then apply import
settings for our style, set pivots at the footprint centers, size colliders to the footprints, set the arm
ring and shadow ellipse ratio to F, and temporarily swap the sketches in for the matching placeholders
(keep the placeholders). Take screenshots at 1920x1080 and at a phone resolution with enemies firing, and
report how the on-screen sizes compare to the spec.
```

---

## 10. Per-asset checklist

- [ ] Drawn to the right height class and size (sections 3, 5)
- [ ] Vertical lines vertical; flat things squashed by F (section 2)
- [ ] Light from top-left
- [ ] Pivot point clear at the footprint center; feet on the same row in every frame
- [ ] Dark outline; no reserved colors used decoratively
- [ ] All required states/frames present (section 6)
- [ ] Named and foldered correctly; 4 px padding; PNG with transparency
