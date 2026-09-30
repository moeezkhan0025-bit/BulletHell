# Bug list

How to log a bug (one entry each, newest at the top of "Open"):
- **Title:** short description
- **Steps:** exactly what to do to make it happen (round, loadout, what you pressed)
- **Expected:** what should happen
- **Actual:** what happens instead (paste Console errors if any)
- **How often:** always / sometimes / once
- **Severity:** crash/blocker, major (breaks a feature), minor (looks/feels wrong)

## Open

### UI2 mockup differences that could not be matched (VOX VEGETALLIS hot swap) - WORK THROUGH OVER TIME
Screenshots of the built screens: `Docs/Screenshots/VoxUI` (1920x1080). Mockups: `Docs/Reference/UI`. Everything below is minor (looks only).
- **Backdrop:** the mockups show the blurred arena with bokeh. Ours is a generated blur of `ScaleTest/arena01_backdrop.png` (`Assets/Art/UI/Backdrop/backdrop_blur.png`, rebuilt by `BulletHell/Vox/5`): softer, no detail, a little brighter and more orange at the edges than the mock. Main Menu, Settings and Character Creation draw it as a world sprite (`MenuBackdrop`), Shop and Armory as a UI image.
- **Main Menu:** the focused button is the same size as the others (mock: slightly larger); the version label shows `v` + Application.version ("v1.0"); the hint pill uses text prompts ("[Cross] Select"), not glyph icons.
- **HUD:** the heat bar is one tint that blends leaf -> carrot -> tomato as it fills (mock shows three fixed colour zones; kept on purpose, it matches the spec "tinted as it rises"); the hold-to-replace ring on an ammo slot is still the old placeholder ring (tinted gold, checked in D1: readable, not matched to a mock); button glyph badges on the ammo slots are the existing glyph discs, not the mock's round badges.
- **Shop:** the detail panel shows the item text as a title plus plain lines. The mock's rarity / tag chips, the "Fits" box and the aligned stat rows (before -> after in green) are not built (the tooltip data is free text from `ShopDescriber`). The merchant is the old placeholder olive instead of the dashed "[MERCHANT ART]" arch. Cards are 0.92x the mock size so both rows fit without scrolling. Reroll / Leave / Main Menu / bag buttons are extra (mouse and touch); the mock only has the prompt pill.
- **Armory:** the ring is smaller than the mock (radius 0.85 u x 285 px = 242 px vs about 380 px; the doll size is tied to the same scale). Filled slots show the big arm sprite beside a faint medallion instead of the arm inside the medallion. The stat strip is one text panel, not four cells with a highlighted change. Tabs show counts ("Arms (1)"). The card grid is 4 columns of portrait cards (mock: landscape cards in 3 columns). The "Slot 2 - choose an armament" line is the generic message label. The dashed ring is a generated texture (`Assets/Art/UI/Backdrop/ring_dashed.png`).
- **Character Creation:** the preview keeps the eight placeholder arms around the doll (mock: one arm); the front arms overlap the Randomize button. A Main Menu button sits under the title (mock has only the hint). The spotlight is a soft glow (the kit's arch sprite at 75%).
- **Settings:** (slider click-by-position fixed in D1); the Video tab only exists on PC (hidden elsewhere, like the settings it holds); a Back button sits top-left (mock has only the hint). The "Changes apply instantly" note is a fixed text.
- **Fonts:** static atlases (no runtime glyph adding); arrows and other missing glyphs fall back to Liberation Sans. Nunito's static 600 / 800 come from the `@expo-google-fonts/nunito` package (the Google Fonts repo only has the variable font).
- **Old setup scripts:** `M4Setup..M9dSetup`, `Cc1Setup`, `M75Setup` still contain the old skeleton screen builders (TMP-converted but the old look). The screens are now built by `BulletHell/Vox/5..9`; do not re-run the old screen builders.
- **Retired art in docs:** `Docs/Screenshots/UI1` and `UI1/theme_sheet.png` still show the removed Mega Cozy pack. Delete the folder when convenient.

### "Destroy may not be called from edit mode" logged when leaving Play mode (seen while working on UI2)
- **Steps:** enter Play mode (any scene that spawned enemies), then stop it
- **Expected:** a clean Console
- **Actual:** about 30 errors `Destroy may not be called from edit mode! Use DestroyImmediate instead.` from `UnityEngine.Object:Destroy` <- `ObjectPool<BulletHell.Enemies.Enemy>.Clear` <- `UnityEngine.Pool.PoolManager:Reset` <- `UnityEditor.ObjectPool.PoolManager:OnEditorStateChange` (Unity's pool manager clearing pools on the play-mode change). Not related to the UI swap; the Editor.log shows it at every Play stop in this session.
- **How often:** always on stopping Play with live enemy pools
- **Severity:** minor (Console noise). Idea: the pool clean-up should not call Destroy from edit mode: check the Enemy pool's `actionOnDestroy` / how the pool is reset on domain reload.

### P1 hitches: occasional 30-70 ms frames in the stress test, cause not yet attributed - TABLED FOR THE FINAL PERFORMANCE TEST
- **Steps:** Development build, `-perfstress -perfvsync 0` (80-enemy swarm + Pumpking phase 2 + 8 arms firing, 90 s), analyse with `Tools/perf_analyze.ps1 -Csv <run.csv> -From 10`
- **Expected:** steady 60 fps, no visible hitches
- **Actual:** average ~165 fps uncapped (p99 13.5-14.2 ms), but 3-37 frames per 80 s run take over 20 ms (worst 27-74 ms); GPU < 1 ms. The BH profiler markers explain only ~1-3 ms per frame
  (Enemy.Brain ~1.0, Nav.Separation ~0.5, Bullet.Update ~0.35, Physics2D ~0.17), so the hitch work is in main-thread phases not yet timed. About 180 GC allocations (~8 KB) happen in every frame (source unknown).
  NOTE: the analyzer lists `wait_gfx_ms` as the top marker of the worst frames, but that stat is the render thread idling for the main thread, a symptom, not the cause.
- **How often:** run 1: 37 frames over 20 ms; runs 2 and 3: 3-4 (unexplained variance)
- **Severity:** minor (the user's own playtests feel fine); recheck at the final performance test
- **Resume checklist:** (1) time the PlayerLoop phases (Update / LateUpdate / FixedUpdate / renderer update / canvas) and find the per-frame allocations; (2) then the suspects from the P1 survey: DebugOverlay 4 Hz throttle, bullet pool and 5-renderer bullets, `Separation` O(n^2) and per-frame LOS / path sweeps, homing scans, the SRP-batcher `_MainTex` property, frame pacing and the 70 ms player hitstop (design decisions: ask); (3) re-run 3x and compare with `-Compare`.
- Minor tool bug: the PerfLogger `canvas_overlay_ms` column reports garbage (negative counter); ignore it.

### 11 EditMode tests fail outside Play mode: Obstacle.ApplyContactShadow calls GameServices.Ensure()
- **Steps:** run the EditMode tests from a fresh Editor (no Play session since the domain loaded), e.g. `unity command run_tests --mode EditMode`
- **Expected:** all tests pass
- **Actual:** `ArenaTests` (2), `LayoutTests` (6), `PerspectiveTests` (3) throw `InvalidOperationException: The following game object is invoking the DontDestroyOnLoad method: Services ... cannot be part of an editor script` from `GameServices.Ensure()` (GameServices.cs:38) via `Obstacle.ApplyContactShadow` (Obstacle.cs:188) <- `ApplyLook` <- `Restore` <- `Setup`. The tests build obstacles in edit mode and the contact-shadow look reaches for the live services (M9d added the shadow). Found while running the suite for M10; the failing code is untouched by M10.
- **How often:** always in edit mode
- **Severity:** minor (tests only; the game is unaffected). Fix idea: `Obstacle` takes its `PerspectiveTuning` from `Setup` (the ArenaController passes it) instead of `GameServices.Ensure()`, or `GameServices.Ensure()` skips `DontDestroyOnLoad` when `!Application.isPlaying`.

### Input System NullReferenceException in InputEvent.get_handled (Editor update) - CAN'T REPRODUCE - MONITORING
- **Steps:** seen once (Console 2026-09-29 20:15:33 UTC, Input System 1.20.0). The Editor had been sitting in a PAUSED Play session for about 43 minutes with nothing else logged; the user then looked at the Editor. Enter Play Mode option "Reload Domain" is disabled.
- **Expected:** no exception
- **Actual:** `NullReferenceException at InputEvent.get_handled (InputEvent.cs:212)` from `InputManager.ProcessEventBuffer` / `NativeInputRuntime.set_onUpdate`, then "Exception ... during event processing of Editor update; resetting event buffer". The whole stack is inside the package (a null native event pointer in the buffer); no project frame is in it. The package recovered by resetting the buffer.
- **How often:** once (1 hit in Editor.log, 1 in the live Console)
- **Severity:** minor (Editor-only, self-recovering, no gameplay effect seen)
- **Investigation (2026-09-29):** no project code uses `InputSystem.onEvent/onDeviceChange/onAfterUpdate`, `QueueStateEvent`, `InputState`, `InputUser` or `PlayerInput`. `GameplayInputReader` and `MenuInputReader` enable in OnEnable, disable in OnDisable, unsubscribe and Dispose in OnDestroy. M8.6 hitstop only writes `Time.timeScale` (GameClock). No `.inputsettings` asset exists (package defaults). Tried to reproduce, no error each time: (1) Play, pause, 20x `QueueStateEvent` on the real DualSense plus `Step()`, waited 20s; (2) EditMode `TheControllerLayoutPicksTheGlyphFamily` (adds fake gamepads to the live editor input system); (3) paused Play with `simulate_key`/`simulate_pointer` then `editor_focus`. Not tried: the idle-for-40-minutes case and DualSense unplug/replug.
- **Status:** no source-level fix made because no cause was found (no settings changed on a guess). Suspects left: package bug in 1.20.0 while the Editor idles in paused Play, or device/focus events after a long pause. Reopen if it comes back: note what you did just before, and whether the Editor was paused in Play.
- **Update (2026-09-30, M9d):** decision: keep monitoring, do not change input behavior. Our input subscribers now log context if they ever throw: `InputDiagnostics.Raise` (used by `MenuInputReader` and `GameplayInputReader` for every event, plus their OnEnable / OnDisable) logs the script, object name, active/enabled state, scene, frame, time scale, and the action / phase / control / device being processed, then rethrows as before. In the Editor, if the package exception (`InputEvent.get_handled`) shows up, a warning is added with the last input our readers handled, the frame, whether the Editor was paused and whether the app was focused. If it comes back: paste those `[InputDiagnostics]` lines here.

### Sprite_Character materials disable 2D SRP batching (console warning) - DEFERRED TO M12
- **Steps:** enter Play mode; the Console shows "Material Mat_SpriteCharacter (and Mat_SpriteOutline) has _TexelSize / _ST texture properties which are not supported by 2D SRP Batcher"
- **Expected:** no warning
- **Actual:** warning once per material; those renderers (player, enemies, arms) are drawn without SRP batching. Comes from the _MainTex property Unity's own Sprite Unlit graph template declares, so it is harmless for now; revisit in the M12 performance pass if draw calls matter.
- **How often:** always
- **Severity:** minor

- **Status (2026-09-30):** deferred to M12; the M12 line in CLAUDE.md lists it under the performance pass.

## Fixed
<!-- Claude moves entries here with a one-line note of the cause and the fix -->
### D1 UI polish (HUD, Settings, Round Results, Pause, Game Over, banners, boss bar)
- **Steps:** capture each screen at 1920x1080, 2560x1440 and 2340x1080 (phone landscape) with long names and 5-digit numbers (`VoxD1Shots`, before/after in `Captures/`)
- **Fixed (2026-09-30, D1):** (1) round pill: "ROUND 12" collided with the divider and the coin pill cut off "99,999 +12,345": pills are wider and their text autosizes (`VoxUi.Fit`), earnings use thousands separators. (2) Boss name plate: a long name wrapped to two lines over the round pill and the bar: one line, autosized, ellipsis. (3) The HUD (hearts, heat, ammo, pills) and the boss bar stayed on screen under Pause and Settings-from-Pause, so the prompt pill and "Changes apply instantly" sat on top of them: `CombatHud` hides in Pause, `RunHud` only shows in RoundIntro and Combat, `BossHealthBar` steps aside while paused and returns on resume. (4) Flow panel prompt pills (Round Results, Pause, Game Over) were an older fixed-width pill with the text off-centre: rebuilt with the self-sizing pill, and `PromptHint` rebuilds the layout when the text changes. (5) Settings sliders only stepped: pressing or dragging on the track now sets the value by position (`SettingRow` `IPointerDown/Drag`, snapped to the setting's step; a click that set a value does not also step it). (6) In-game scrims: Settings over the paused arena dims to 0.8 (was 0.55) and the flow panels to 0.7 so the title and rows read. (7) Motion timings: screen transition, round banner pop and wave banner pop now read the theme (manifest: fade/slide 0.2 s, pop 0.25 s) instead of their own constants.
- **Checked, no change:** Round Results / Pause / Game Over focus (the focused button turns corn, the other stays marble), 5-digit currency on Round Results, long boss banner text (wraps to two lines at all three sizes), phone landscape (HUD stays inside the 16:9 safe rect, bars are dark). Not done on purpose: focus lift on flow-panel buttons and settings rows (they sit in layout groups and the focus sprite already reads; the manifest lift stays on cards, bubbles and menu buttons).
- **Still open (minor):** the Pause debug row (dev builds) clips the "Boss" button text on narrow layouts; a very long boss name is cut with an ellipsis at the 20 px minimum size (the real names are short).

### Phone portrait: the whole UI shrinks into a small 16:9 strip and the gameplay camera crops the arena (found in M9d)
- **Steps:** capture or run the Game view at 1080x2340 (portrait)
- **Expected:** readable, usable layout on a phone
- **Actual:** nothing is clipped or outside the 16:9 safe area (checked Pause, Shop, Armory, quit dialog), but all screens are drawn in a 16:9 strip in the middle of the screen, so text is tiny; the camera shows a tall crop of the arena.
- **How often:** always (portrait only; 21:9 landscape and 1440p are fine)
- **Severity:** minor for now (needs a design decision: landscape-only on phones, or a portrait layout, decided at M12 with the touch controls)
- **Fixed (2026-09-30, M9d): design decision, the game is landscape only on phones and tablets.** Player Settings now auto-rotate between Landscape Left and Landscape Right only (portrait and upside-down portrait off, default orientation Auto Rotation) for iOS and Android; 4:3 tablets keep the existing letterboxing. The decision is in CLAUDE.md, Target platforms.

### Scale test: enemy bullets are the same red family as the painted floor
- **Steps:** Game scene with `ScaleTestArt` backdrop on; enemies fire over the red carpet strips and crest ring
- **Expected:** enemy bullets pop against the floor, also in grayscale (ART_SPEC section 7)
- **Actual:** bullets are rgb(255,89,89), luma 139; floor luma 167, carpet strips and crest ring luma ~103 with a similar red hue. No dark outline. Contrast is low over the red parts. Placeholder bullets also draw ~0.30-0.34 P wide (spec: enemy 0.2-0.3 P, player 0.15-0.25 P).
- **How often:** always
- **Severity:** minor (art pass: reserved bullet colours + outline, then retune sizes)

- **Fixed (2026-09-30, M9d):** enemy bullets took a per-pattern red tint and had no outline. They now come from the `EnemyBulletPalette` asset (`Assets/Data/Enemies/`): Electric Violet (white core #FFFFFF, body #B44BFF, outline #1B0730) for all ordinary shots, Hot Magenta (body #FF3DCB) for boss and special shots (`Pattern_BossBurst`, `Pattern_SniperShot`); outline 2 px at 1080p, 25% normal-blend glow. `AttackPattern.bulletColor` was replaced by `bulletStyle`; `Projectile.LaunchHostile` draws outline, fill, core and glow layers (built with the pool). Colours that clashed with the two hues were moved: Purple/Piercer arm ID colours (now gold / mint), Epic rarity (now emerald), the test-spare ammo tint and pickup (orange), Spiraler and boss placeholder body colours, the DANGER telegraph colour (#FF4D33). Floor art has 0% of pixels in either hue band. Checked by `EnemyBulletPaletteTests` and the Audit Reserved Hues menu. Screenshots in `Docs/Screenshots/EnemyBullets` (color + grayscale). Bullet size retune (0.2-0.3 P) is still open with the art pass.


### Scale test: magenta square and white shape next to the pillars
- **Steps:** Game scene, round 1; look at the top corners of the two pillars
- **Expected:** no stray shapes; every shader compiles for URP 2D
- **Actual:** a bright magenta square right of the right pillar and a white circle left of the left pillar. Not a broken material: no shader errors, all 8 Shader Graphs are URP Sprite Unlit. They are the test ammo pickups (`Pickup_TestSpare` = magenta-tinted Square, `Pickup_Basic` = white Circle) placed beside the pillars.
- **How often:** always
- **Severity:** minor
- **Fixed (2026-09-29):** pickups and coins now get a dark outline, a contact shadow and muted colours (`PlaceholderLook`, values in `PerspectiveTuning`). The test pickups still sit near the pillars; move them in the Game scene if that bothers you.

### Scale test: painted backdrop framed with its dark bars, left/right HUD cut off in a small Game view
- **Steps:** Game scene at 1920x1080, or any other window shape
- **Expected:** the whole 16:9 painted arena, centred, and the whole HUD inside it
- **Actual:** the imported image (3840x2160) has dark bars around the painted area (3269x1840 at x 273, y 54), which was framed too, so the arena sat off-centre with a big empty band on top. The HUD was anchored to the screen edges, not to the picture. On this machine Windows scaling is 150% (2560x1440), so a fixed 1920x1080 Game view is larger than its pane and Unity crops it unless the Game view's Scale slider is lowered.
- **How often:** always
- **Severity:** major for the art pass
- **Fixed (2026-09-29):** `ScaleTestArt` crops the backdrop to the painted area; `CameraLetterbox` keeps the camera at the arena's aspect (bars cleared by a second camera); `SafeAreaFitter` keeps the HUD inside that rectangle. The Game view Scale slider is an editor setting: set it to fit (or use Free Aspect / a 1280x720 size) when the pane is small.

### Scale test: ArenaData bounds (16 x 9) are larger than the painted floor
- **Steps:** Game scene with the backdrop on; walk to the bottom or top edge, watch bullets
- **Expected:** walls and gates line up with the painted arena
- **Actual:** painted walkable floor is about 13.3 x 6.0 units (x +-6.6, y +-3.0); the play bounds reach y -4.5, so the player spawn (0, -3.2) stands on the painted front railing, and bullets fly out into the crowd and the dark margin. Side gates at (+-7.2, 0.5) have no painted door; the painted arches are at about x +-4.3 on the back wall.
- **How often:** always
- **Severity:** major for the art pass
- **Fixed (2026-09-29):** ArenaData size is now 13.2 x 6.0 (x +-6.6, y +-3.0). All 7 layouts: spawn (0,-2.4), back gates (+-4.3, 2.3) under the painted arches, side gates (+-5.7, 0.3), obstacle/trap positions remapped x*0.9, y*0.75+0.6 and rounded to 0.1. Gates sit 0.6 in from the border because the layout tests need a 0.6-radius lane. Retune each layout by eye later.

### FeedbackHub adds a new Application.quitting handler every Play session
- **Steps:** enter and exit Play mode repeatedly with Reload Domain disabled
- **Expected:** one handler
- **Actual:** `FeedbackHub.ResetStatics` (FeedbackHub.cs:38-44) does `Application.quitting += () => quitting = true;` on every Play session and never removes it, so lambdas pile up. Found while investigating the input exception; unrelated to it and harmless so far.
- **How often:** always
- **Severity:** minor

- **Fixed (2026-09-30, M9d):** the lambda was created on every Play session; it is now a static method subscribed once (`-=` then `+=`). `FeedbackHub.cs`

### PrimeTween "endValue equals current animated value" warnings from menu rows
- **Steps:** navigate Main Menu / ammo HUD slots (MenuRow.cs:42-49, Slot3/Slot4 scale tweens)
- **Expected:** no warning
- **Actual:** warning per tween when the target scale/alpha already equals the end value. Harmless.
- **How often:** often
- **Severity:** minor

- **Fixed (2026-09-30, M9d):** `MenuRow.Lift` and `HudAmmoSlot` started scale tweens whose end value equalled the current scale; they now skip the tween in that case.

### Example: Ricochet bullets pass through low walls
- **Steps:** Round 2, equip Ricochet on the red arm, fire at the low wall near the left gate
- **Expected:** bullet bounces off the wall
- **Actual:** bullet goes straight through
- **How often:** always
- **Severity:** major
- **Fixed (2026-09-30, M9d): could not reproduce, works.** Round 2 layout, a Low wall at (0, 1.6), bullet with Bounces=3 fired upward from (0,-0.5): it reached the wall's near edge at y=0.95 and came back down (y 0.95 -> 0.23 -> -0.49 -> -1.21). Ricochet off Low walls is correct. This entry looked like the format example from the top of this file.

### Armory ring slot focus showed as an opaque yellow disc covering the arm (found in M9d)
- **Steps:** Armory, focus any ring slot
- **Expected:** a visible focus ring around the arm
- **Actual:** a filled yellow circle hid the arm's base
- **How often:** always
- **Severity:** minor
- **Fixed (2026-09-30, M9d):** the focus ring used the solid Circle sprite; it now uses a ring-shaped sprite (`M9cSetup.BuildSlotButton`).

<!-- Claude moves entries here with a one-line note of the cause and the fix -->

### Yellow circle drawn over the player sprite (jump marker)
- **Steps:** play any round; look at the player standing, then jump (R2)
- **Expected:** shadow under the feet, nothing covering the sprite
- **Actual:** a yellow disc with a dark ring sat on the lower body, and stayed on the ground while the body rose in a jump
- **How often:** always
- **Severity:** minor
- **Fixed (2026-09-29):** it was the player's damage-core marker (`Player/Core/Marker` + `Outline`), sorted at order 10/9 (above the body at 0) and drawn as a round disc. It is now a flat ellipse (same flatness as the shadow) at order -5/-6, above the shadow and behind the body: hidden by the body while standing, visible on the ground under the body during a jump. `PlayerVisualRig.cs`, `Player.prefab`, `M77Setup.cs`. Note: the marker colour (1, 0.95, 0.45) is almost the yellow of player bullets (1, 0.95, 0.4).
