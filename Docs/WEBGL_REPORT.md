# WebGL browser demo: test report and verdict (D6)

Tested 2026-09-30 with Unity 6000.3.25f1, WebGL build 1.0.2 / 1.0.3 (release) and 1.0.3-dev (development), Chrome (current stable) on the
development PC (Windows 11, 240 Hz display, dedicated GPU). Screenshots are in `Captures/d6`. The tool that served the build and drove
Chrome is `Tools/WebTest/WebTest.cs` (see "How this was tested").

## Verdict

**An itch.io browser demo is viable, as a desktop-browser demo with a controller or keyboard/mouse, with about a day of work left.** It is
not viable for phones or tablets (there are no touch controls yet, and a 28 MB download plus single-threaded WebGL is heavy for a phone).

What works today, measured: the build loads, the Main Menu and the whole run flow play, a gamepad drives the menus and the combat, audio plays
and the mixer volumes work, settings persist across reloads, and normal gameplay runs at the display's full refresh rate. What holds it back:
the heaviest fights (80 enemies + the boss + all arms) drop to about 22 fps, which is far beyond what the authored rounds 1-7 ask of it.

## Numbers

| Area | Result |
|---|---|
| Download | 27.9 MB zip (wasm 12.8 MB, data 14.3 MB, gzip); 224 MB for the development build (not for release) |
| Load, localhost, cold (no caches) | 1.6 s to the first game frame |
| Load, cold, 50 Mbit/s and 50 ms latency | 7.1 s |
| Load, cold, 10 Mbit/s and 50 ms latency | 25.9 s (a progress bar with "Downloading n%" is shown) |
| Load, warm (Unity's IndexedDB cache) | 1.6 s (on localhost the network is not the limit; on a real connection the cached visit skips the 26 MB) |
| Normal gameplay, release build, round 1 combat | 236 fps average, p99 6.1 ms, worst frame 13 ms (monitor is 240 Hz, so this is the cap) |
| Stress test, development build, 20 enemies + boss | 59.5 fps (capped at 60), worst frame 29 ms, none over 33 ms |
| Stress test, development build, 40 enemies + boss | 41 fps, p99 33 ms |
| Stress test, development build, 80 enemies + boss + 8 arms | 22 fps, p99 61 ms, worst 109 ms |
| For comparison, Windows development build, same 80-enemy test | about 165 fps uncapped (P1 baseline) |

The development build is slower than a release build (assertions, profiler markers, full exception stacks), and the stress test cannot run in a
release build (its switches are development-only), so the stress numbers are a pessimistic bound. The release-build number above is from real play.
WebGL here is single-threaded (no physics or job threads), which is why the same code is about 7 times slower than on Windows under load.

## Input

- **Gamepad works through the Input System's WebGL gamepad support**, tested with a stand-in standard-mapping gamepad (see the caveat below): the
  menu (d-pad, A, B), Settings (d-pad left/right on sliders, B closes), the right stick moves, the left stick selects arms, R1 fires, and the onboarding
  lessons advance from these inputs. Button prompts switch to Xbox names ("[A] Select", "[LB] Prev tab") when a pad is used.
- Keyboard and mouse work (clicks on buttons and sliders, Enter / Esc).
- **Browsers only expose a gamepad after the player presses a button on it.** The page cannot know a pad is plugged in until then, so the first menu
  screen shows keyboard prompts. Fine, but worth a line on the itch.io page ("press any button on your controller").
- Esc leaves browser fullscreen, so Pause on Esc behaves differently from the desktop game while fullscreen.
- The Quit button is hidden in the browser (`PlatformCapabilities.CanQuitApplication`): a page cannot close its tab.
- **Not verified:** a real DualSense or Xbox pad in Chrome. A scripted Chrome cannot see a physical controller without a human pressing it, so the test
  injected a standard-mapping gamepad object in place of `navigator.getGamepads()`. The Input System then read it through its real WebGL path.
  The mapping of a real DualSense in Chrome is "standard" too, so this is expected to hold, but it must be confirmed by hand (see "What it would take").
- Lock (L3) and jump (R2) inputs were sent; the L3 and R2 lessons of the tutorial were not watched to completion in the browser (the player was
  killed by the round 1 enemies while the test script was still timing them). They use the same code path as the verified buttons.

## Saving

- **Saving did not work with Unity's default WebGL page**: the settings file was written to the in-memory file system but never copied to the
  browser's IndexedDB, so a reload forgot everything (the default template leaves `autoSyncPersistentDataPath` commented out).
- **Fixed in this milestone:** a custom template (`Assets/WebGLTemplates/VoxVegetallis`) turns `autoSyncPersistentDataPath` on. Verified: changing Master
  Volume and closing Settings put `settings.json` in IndexedDB, and after a page reload Settings showed the saved value.
- Settings, the profile (onboarding done, look), the run save and the playtest log all live under `persistentDataPath`, so all of them persist. Browser
  storage is per site and per browser, can be cleared by the player, and is not shared between itch.io's own domain and a local test.
- Not verified in the browser: a full Continue across a reload (the run save is written at the Shop; the test run ended at Game Over before it).

## Audio

- No audio context exists until the player's first click or key press (browser autoplay policy): before any input the page had 0 audio contexts;
  after the first click it was `running` and the menu music measured an average level of 0.16 RMS. The game already starts music on its first menu, so it
  begins as soon as the player clicks anything.
- **The mixer works in the browser**: the Master Volume slider at 0 gave an average level of exactly 0 and back at 100 gave 0.156 again.
- Unity logs `FMOD returns error code 78 (FMOD_ERR_UNIMPLEMENTED)` and `...36` a few times per scene load: harmless messages from the web audio backend.
- Not verified by ear: music quality, the stingers, and the voice limiter under a real fight (the levels were measured, not listened to).

## Performance

See the table. Practical reading: rounds 1-2 and the boss round with its normal waves are comfortable; a swarm of 40+ enemies (the far end of round 7) would
not hold 60 fps on this PC in a development build. Two cheap levers are already known from P1 (Separation is O(n²), the per-frame GC allocations, the
2D SRP Batcher warning), and WebGL also pays for `Canvas` rebuilds and a single thread. Memory: the build was set to a 256 MB start and 1 GB cap; no
out-of-memory error appeared in any run.

## Other findings

- The page template is now responsive: the canvas takes the largest 16:9 box that fits the window (checked at 1920x1080, 1024x768 and 844x390), a themed
  loading bar shows download progress, and there is a fullscreen button.
- Two URP shader messages appear at start ("Hidden/CoreSRP/CoreCopy" and "Hidden/Universal/HDRDebugView is not supported on this GPU"): internal URP
  debug shaders that the game never uses; no visible effect.
- Browser tab in the background: `runInBackground` is on, so the game keeps running when the tab loses focus (a pause-on-blur would be kinder on a
  phone or laptop).
- Mobile browsers: not tested and not recommended (no touch controls, landscape only, heavy download).

## What it would take to publish on itch.io

1. **Confirm a real controller** (30 minutes): open `Builds/WebGL/<version>/` through the test tool or upload to itch.io as a draft and press buttons
   on the DualSense in Chrome and Firefox; check that the glyphs show the right family.
2. **Upload:** `Builds/VoxVegetallis-WebGL-<version>.zip` (the build menu makes it). On itch.io: project kind "HTML", tick "This file will be played in the
   browser", viewport 1920x1080 (the page scales down), enable "Fullscreen button", and leave "SharedArrayBuffer support" off (this build uses no threads).
3. **Compression:** the build is gzip with `decompressionFallback` on, so it works whether or not the host sends `Content-Encoding: gzip`. Brotli would
   save a few MB more but needs the host to send the right header; itch.io does for Unity Brotli builds, so switching is optional.
4. **Page text:** list controls (gamepad recommended), say that audio starts after the first click, that progress is saved in the browser, and that
   a swarm-heavy late game may be slower in a browser than in the Windows build.
5. **Decide on the heavy end** (optional, 0.5-2 days): either cap the late-round enemy counts for the web build (a data change in the wave assets), or work
   through the P1 list (flow-field and separation cost, allocations) before the demo; both help the Windows build too.
6. **Nice to have:** pause the game when the tab loses focus; a "click to start" splash so the first click also unlocks audio and the gamepad;
   a touch control scheme (M12) before advertising phones.

## How this was tested

`Tools/WebTest/WebTest.cs` is a single-file .NET tool (`dotnet run --file Tools/WebTest/WebTest.cs -- Builds/WebGL/<version> 8765 8766`). It serves
the build folder on `http://localhost:8765/`, launches Chrome with the DevTools protocol open, and offers a control API on port 8766 (launch, navigate,
evaluate JavaScript, key, click, screenshot, console log, network throttle, stand-in gamepad, audio level, viewport). Stress tests run from the page
address of the development build: `index.html?perfstress&perfseconds=60&perfenemies=80&perfvsync=0&perflabel=web` (the same switches as the Windows
command line, without the dashes; `PerfArgs` reads them from the URL in WebGL). The run ends with a `PERFSUMMARY {json}` console line.
