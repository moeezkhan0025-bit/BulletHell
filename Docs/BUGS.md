# Bug list

How to log a bug (one entry each, newest at the top of "Open"):
- **Title:** short description
- **Steps:** exactly what to do to make it happen (round, loadout, what you pressed)
- **Expected:** what should happen
- **Actual:** what happens instead (paste Console errors if any)
- **How often:** always / sometimes / once
- **Severity:** crash/blocker, major (breaks a feature), minor (looks/feels wrong)

## Open

### Scale test: enemy bullets are the same red family as the painted floor
- **Steps:** Game scene with `ScaleTestArt` backdrop on; enemies fire over the red carpet strips and crest ring
- **Expected:** enemy bullets pop against the floor, also in grayscale (ART_SPEC section 7)
- **Actual:** bullets are rgb(255,89,89), luma 139; floor luma 167, carpet strips and crest ring luma ~103 with a similar red hue. No dark outline. Contrast is low over the red parts. Placeholder bullets also draw ~0.30-0.34 P wide (spec: enemy 0.2-0.3 P, player 0.15-0.25 P).
- **How often:** always
- **Severity:** minor (art pass: reserved bullet colours + outline, then retune sizes)

### Scale test: ArenaData bounds (16 x 9) are larger than the painted floor
- **Steps:** Game scene with the backdrop on; walk to the bottom or top edge, watch bullets
- **Expected:** walls and gates line up with the painted arena
- **Actual:** painted walkable floor is about 13.3 x 6.0 units (x +-6.6, y +-3.0); the play bounds reach y -4.5, so the player spawn (0, -3.2) stands on the painted front railing, and bullets fly out into the crowd and the dark margin. Side gates at (+-7.2, 0.5) have no painted door; the painted arches are at about x +-4.3 on the back wall.
- **How often:** always
- **Severity:** major for the art pass
- **Fixed (2026-09-29):** ArenaData size is now 13.2 x 6.0 (x +-6.6, y +-3.0). All 7 layouts: spawn (0,-2.4), back gates (+-4.3, 2.3) under the painted arches, side gates (+-5.7, 0.3), obstacle/trap positions remapped x*0.9, y*0.75+0.6 and rounded to 0.1. Gates sit 0.6 in from the border because the layout tests need a 0.6-radius lane. Retune each layout by eye later.

### Input System NullReferenceException in InputEvent.get_handled (Editor update) - NOT REPRODUCED
- **Steps:** seen once (Console 2026-09-29 20:15:33 UTC, Input System 1.20.0). The Editor had been sitting in a PAUSED Play session for about 43 minutes with nothing else logged; the user then looked at the Editor. Enter Play Mode option "Reload Domain" is disabled.
- **Expected:** no exception
- **Actual:** `NullReferenceException at InputEvent.get_handled (InputEvent.cs:212)` from `InputManager.ProcessEventBuffer` / `NativeInputRuntime.set_onUpdate`, then "Exception ... during event processing of Editor update; resetting event buffer". The whole stack is inside the package (a null native event pointer in the buffer); no project frame is in it. The package recovered by resetting the buffer.
- **How often:** once (1 hit in Editor.log, 1 in the live Console)
- **Severity:** minor (Editor-only, self-recovering, no gameplay effect seen)
- **Investigation (2026-09-29):** no project code uses `InputSystem.onEvent/onDeviceChange/onAfterUpdate`, `QueueStateEvent`, `InputState`, `InputUser` or `PlayerInput`. `GameplayInputReader` and `MenuInputReader` enable in OnEnable, disable in OnDisable, unsubscribe and Dispose in OnDestroy. M8.6 hitstop only writes `Time.timeScale` (GameClock). No `.inputsettings` asset exists (package defaults). Tried to reproduce, no error each time: (1) Play, pause, 20x `QueueStateEvent` on the real DualSense plus `Step()`, waited 20s; (2) EditMode `TheControllerLayoutPicksTheGlyphFamily` (adds fake gamepads to the live editor input system); (3) paused Play with `simulate_key`/`simulate_pointer` then `editor_focus`. Not tried: the idle-for-40-minutes case and DualSense unplug/replug.
- **Status:** no source-level fix made because no cause was found (no settings changed on a guess). Suspects left: package bug in 1.20.0 while the Editor idles in paused Play, or device/focus events after a long pause. Reopen if it comes back: note what you did just before, and whether the Editor was paused in Play.

### FeedbackHub adds a new Application.quitting handler every Play session
- **Steps:** enter and exit Play mode repeatedly with Reload Domain disabled
- **Expected:** one handler
- **Actual:** `FeedbackHub.ResetStatics` (FeedbackHub.cs:38-44) does `Application.quitting += () => quitting = true;` on every Play session and never removes it, so lambdas pile up. Found while investigating the input exception; unrelated to it and harmless so far.
- **How often:** always
- **Severity:** minor

### PrimeTween "endValue equals current animated value" warnings from menu rows
- **Steps:** navigate Main Menu / ammo HUD slots (MenuRow.cs:42-49, Slot3/Slot4 scale tweens)
- **Expected:** no warning
- **Actual:** warning per tween when the target scale/alpha already equals the end value. Harmless.
- **How often:** often
- **Severity:** minor

### Sprite_Character materials disable 2D SRP batching (console warning)
- **Steps:** enter Play mode; the Console shows "Material Mat_SpriteCharacter (and Mat_SpriteOutline) has _TexelSize / _ST texture properties which are not supported by 2D SRP Batcher"
- **Expected:** no warning
- **Actual:** warning once per material; those renderers (player, enemies, arms) are drawn without SRP batching. Comes from the _MainTex property Unity's own Sprite Unlit graph template declares, so it is harmless for now; revisit in the M12 performance pass if draw calls matter.
- **How often:** always
- **Severity:** minor

### Example: Ricochet bullets pass through low walls
- **Steps:** Round 2, equip Ricochet on the red arm, fire at the low wall near the left gate
- **Expected:** bullet bounces off the wall
- **Actual:** bullet goes straight through
- **How often:** always
- **Severity:** major

## Fixed
<!-- Claude moves entries here with a one-line note of the cause and the fix -->

### Yellow circle drawn over the player sprite (jump marker)
- **Steps:** play any round; look at the player standing, then jump (R2)
- **Expected:** shadow under the feet, nothing covering the sprite
- **Actual:** a yellow disc with a dark ring sat on the lower body, and stayed on the ground while the body rose in a jump
- **How often:** always
- **Severity:** minor
- **Fixed (2026-09-29):** it was the player's damage-core marker (`Player/Core/Marker` + `Outline`), sorted at order 10/9 (above the body at 0) and drawn as a round disc. It is now a flat ellipse (same flatness as the shadow) at order -5/-6, above the shadow and behind the body: hidden by the body while standing, visible on the ground under the body during a jump. `PlayerVisualRig.cs`, `Player.prefab`, `M77Setup.cs`. Note: the marker colour (1, 0.95, 0.45) is almost the yellow of player bullets (1, 0.95, 0.4).
