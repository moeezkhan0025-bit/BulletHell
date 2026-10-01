# FACTS.md - verified fact sheet for the portfolio case study

Generated 2026-10-01 at repo HEAD `484781b` (working tree had one pre-existing uncommitted change: `ProjectSettings/QualitySettings.asset`).
Rules: every statement cites a source (path, command, commit hash or transcript id). Anything not directly verified is labelled ESTIMATE, ASSESSMENT or UNVERIFIED; anything undeterminable is under UNKNOWN. Facts were gathered read-only by parallel research passes; Play mode was NOT entered and the Unity CLI was NOT used (scene data comes from parsing the `.unity` YAML).

## One-page summary
Every figure below is detailed and cited in the section named in brackets.

**Scale and pace** [Section 4, 4b]
- 61 commits on `main`, 2026-09-28 13:23 to 2026-09-30 21:24 (-0500), 3 active days (29 / 15 / 17 commits). Author "Cyan".
- 317 C# files: Scripts 244 (28,322 lines), Editor 41 (12,751), Tests 32 (6,065). 337 `[Test]` + 20 `[TestCase]`, EditMode only. Last recorded full run: 344 of 355 passing, 11 known failures (`Docs/CaseStudy/05_DevLog.md:478`, not re-run).
- 49 ScriptableObject types, 168 `.asset` files in `Assets/Data` (183 in `Assets`), 25 prefabs, 3 scenes in the build (Boot, MainMenu, Game).
- Claude Code transcripts: 23 main sessions (21 counted; 1 duplicate pair counted once, the current session excluded) plus 41 subagent files. Wall-clock 30.98 h. Active time ESTIMATE: 23.6 h (10 min idle cutoff), 27.9 h (30 min), 31.0 h (60 min).
- Models (transcript `message.model`): Opus 5.5 on M1 / M1.5 and part of M0, Sonnet 5.5 on everything else, Fable 5.1 on 55 turns at the start of M10. Commit trailers: 5 Opus 5.5, 42 Sonnet 5.5, 14 none; commit `1a79436` (M10 + P1) says Sonnet but includes the Fable turns.

**Build** [Section 1d]
- Latest build 1.0.7-demo: Windows 156.8 MB in 13 s (zip 52.5 MB), WebGL 26.8 MB in 29 s (zip 27.4 MB). Earlier WebGL builds took 223-543 s; no documented cause for the speed-up.
- WebGL: gzip with decompression fallback, 256 MB initial / 1 GB max memory, custom template. 2D URP, Input System only. Windows scripting backend and graphics API: UNKNOWN.

**State** [Section 2]
- Implemented (code present, tests or recorded checks, not re-verified in Play mode): full run loop, arms/ammo/armaments, jump, arm ring, enemy AI with flow field, rounds 1-7, Pumpking boss (round 3), Shop, Armory, Settings with remapping, onboarding, telemetry, audio system, build pipeline, demo gating.
- Open: 6 entries in `Docs/BUGS.md` (14 fixed). 6 `[TBD]` in CLAUDE.md. Rounds 5 and 7 bosses, M11 and M12 not started; P1 performance is partial (baseline only, ~165 fps avg uncapped, p99 ~14 ms, hitches of 27-74 ms unattributed, ~180 GC allocs per frame; no "after").
- Placeholder: enemy and boss art other than the Chaser and Pumpking, rounds 5/7 boss (a tinted square), trap art (none), five armament icons (none), all audio (CC0), the VFX (procedural). The painted arena is one flattened image, with no foreground layer built (`ArenaScenery.cs:73-82`), which conflicts with CLAUDE.md's layered-arena rule. No touch input code, no localization.
- ASSESSMENT: content (art, audio, data) is the bulk of what remains; engineering still needed for two more bosses, touch controls, the layered arena foreground, the P1 fixes and meta-progression (a [TBD]).

**Contradictions** [Section 4.7]: `Docs/CaseStudy` is a snapshot at commit `799d55b` (50 commits); counts, spans and several statuses are stale.

## Reconciliation notes (conflicts between passes, resolved by me)
- C# file counts: recounted with `find Assets/<dir> -name '*.cs'`: Scripts 244 files / 28,322 lines, Editor 41 / 12,751, Tests 32 / 6,065, total 317 (all tracked by git). The class-table pass reported a different folder split (202/80/35); its total of 317 is right, its split is not. Use the numbers here.
- Tests: `grep` over `Assets/Tests`: 337 `[Test]` + 20 `[TestCase]` = 357 attributes. No `[UnityTest]`, no PlayMode tests. Both numbers quoted elsewhere are consistent with this.
- Section 1c and 3 are long; the tables below are the raw outputs of the passes.

## Contents
1. Section 1 - Workflow and scaffolding (1a, 1d, 1b, 1c)
2. Sections 2 and 3 - Current state, asset slots
3. Section 4 - Velocity (git, code, bugs, P1) and Claude Code session transcripts
4. Contradictions with Docs/CaseStudy

---

# SECTION 1a and 1d - Folders and build pipeline

# Sections 1a and 1d (verified facts, read-only)

## 1a. Folder hierarchy
Commands: `ls -a` in C:\Dev\BulletHell (root); `find Assets -maxdepth 3 -type d | sort` (Assets/X/Y/Z).
Root entries (ls -a): .claude, .git, .gitattributes, .gitignore, .vsconfig, ArtSource/, Assets/, Builds/, BuildVersion.json, BulletHell.sln, BulletHell_Meats_Sweets.sln, generated *.csproj, Captures/, CLAUDE.md, Docs/, Library/, Logs/, Packages/, ProjectSettings/, Temp/, Tools/ (export_art.ps1, perf_analyze.ps1, ServeWebBuild.ps1, WebTest/), UserSettings/.
(Library, Temp, Logs, UserSettings, .git excluded from the tree below.)

```
Assets/
  Art/  Arms/ Bosses/(Pumpking) Enemies/(Skirmisher) Materials/ Placeholder/(M75 Parts RigTest Vfx) Player/ ScaleTest/ Shaders/ UI/(Backdrop VoxKit)
  Audio/ Music/ Sfx/ Stingers/
  Data/  Ammo/ Arenas/(Layouts Obstacles Traps) Armaments/ Arms/ Audio/ Bosses/ Cosmetics/ Effects/ Enemies/(Patterns) Feedback/ Input/ Loadouts/ Pickups/ Player/ Settings/ Shop/ UI/ Waves/(Rounds)
  Editor/ ShaderGen/
  Fonts/
  Prefabs/ UI/ Vfx/
  Resources/
  Scenes/
  Scripts/ AI/(Behaviors) Arena/ Armory/ Audio/ Bosses/ Core/ Cosmetics/ Enemies/ Feedback/ Input/ Perf/ Pickups/ Platform/ Player/ Projectiles/ Save/ Settings/ Shop/ Telemetry/ UI/ Weapons/(Effects)
  Settings/ Scenes/
  Tests/ EditMode/
  TextMesh Pro/ Fonts/ Resources/(Fonts & Materials, Sprite Assets, Style Sheets) Shaders/ Sprites/
  ThirdParty/
  WebGLTemplates/ VoxVegetallis/(TemplateData)
```

## 1d. Build pipeline
### (i) Build menu - Assets/Editor/BuildMenu.cs (276 lines), namespace BulletHell.EditorTools
Menu items (BuildMenu.cs lines 35-61):
- `BulletHell/Build/Windows` -> Run(Windows)
- `BulletHell/Build/WebGL (release, for itch.io)` -> Run(WebGL)
- `BulletHell/Build/WebGL (development, for stress tests)` -> Run(WebGLDevelopment): BuildOptions.Development, exceptions FullWithStacktrace
- `BulletHell/Build/Windows + WebGL` -> Run(Windows, WebGL)
- `BulletHell/Build/Build Windows (Demo)`, `Build WebGL (Demo)`, `Build Windows + WebGL (Demo)` -> Run(demo=true, ...)
- `BulletHell/Build/Show Next Version` (logs next vs current version), `BulletHell/Build/Open Builds Folder`
Related (Assets/Editor/DemoSetup.cs lines 20-63): `BulletHell/Demo/Set Up Demo Config`, `Demo Mode On (Editor)`, `Demo Mode Off (Editor)`. Only BuildMenu.cs contains BuildPipeline (grep of Assets/Editor).

What the code does:
- Run(demo, targets): loads the DemoConfig asset, sets isDemo to `demo`, restores the previous value in finally (and again before the target switch).
- RunTargets: reads BuildVersion.json, next = build+1, sets PlayerSettings.bundleVersion; builds every target; if all OK writes BuildVersion.json (a failed build does not use up the number) and logs "BUILD OK"; else restores bundleVersion. Switches the Editor back to its previous build target afterwards.
- BuildOne: output folder `Builds/<Windows|WebGL>/<version>[-dev][-demo]` (deleted first if present); scenes = enabled scenes in EditorBuildSettings (Boot, MainMenu, Game per ProjectSettings/EditorBuildSettings.asset); Windows = StandaloneWindows64, exe `VoxVegetallis.exe`; WebGL fails if index.html is missing; Windows build deletes `*_BurstDebugInformation_DoNotShip` folders; every successful build is zipped (System.IO.Compression, Optimal) to `Builds/VoxVegetallis-<Platform>[-demo]-<version>.zip`; folder size = sum of file bytes; the Stopwatch times only BuildPipeline.BuildPlayer (not the zip).
- ConfigureWebGL (lines 225-238), applied on every WebGL build: template `PROJECT:VoxVegetallis`, compression Gzip, decompressionFallback true, exceptionSupport ExplicitlyThrownExceptionsOnly (dev: FullWithStacktrace), dataCaching true, initialMemorySize 256, memoryGrowthMode Geometric, maximumMemorySize 1024, runInBackground true, default web size 1920x1080.

Version scheme: `BuildVersion.json` at project root = {"major":1,"minor":0,"build":7} (current file). Version string major.minor.build via BulletHell.Core.BuildInfo.FormatVersion; written to PlayerSettings.bundleVersion (ProjectSettings.asset line 150: `bundleVersion: 1.0.7`). Shown in Main Menu (CLAUDE.md).

Builds/build_log.csv: header `time,version,platform,result,seconds,bytes,errors,warnings,folder,zip`. Rows (all 2026-09-30), fields version,platform,result,seconds,bytes,errors,warnings:
```
17:19:17 1.0.1 Windows    ok 64  164476070 1 9
17:21:42 1.0.1 WebGL      ok 145 0         2 4   (bytes 0; zip Builds\VoxVegetallis-WebGL-1.0.1.zip no longer exists in Builds/)
17:30:36 1.0.2 Windows    ok 31  164235565 0 1
17:39:29 1.0.2 WebGL      ok 532 27909102  1 1
17:51:51 1.0.3 WebGL      ok 225 27896890  1 2
18:01:01 1.0.3 WebGL-dev  ok 543 224571668 0 2
18:13:17 1.0.4 Windows and WebGL: result "Unknown", 0 s, 0 bytes (two rows)
18:16:01 1.0.4 Windows    ok 37  164476870 1 2
18:24:43 1.0.4 WebGL      ok 521 27898396  0 2
19:46:03 1.0.5 Windows(demo) ok 42  164368941 1 9
19:50:57 1.0.5 WebGL(demo)   ok 293 28075277  0 4
19:58:56 1.0.6 Windows(demo) ok 20  164368941 0 6
20:02:40 1.0.6 WebGL(demo)   ok 223 28075601  0 3
20:46:51 1.0.7 Windows(demo) ok 13  164368941 0 5
20:47:21 1.0.7 WebGL(demo)   ok 29  28075511  1 2
```

### (ii) Player settings (ProjectSettings/ProjectSettings.asset unless noted)
- Unity 6000.3.25f1 (ProjectVersion.txt). Company DefaultCompany, productName VoxVegetallis, bundleVersion 1.0.7.
- Default window: defaultScreenWidth/Height 1920x1080, defaultIsNativeResolution 1, fullscreenMode 1 (borderless fullscreen window; enum meaning is ESTIMATE from Unity docs), resizableWindow 0, allowFullscreenSwitch 1, runInBackground 1, defaultScreenOrientation 4 (auto rotation), autorotate landscape left/right only (portrait 0). Web default size 1920x1080 (defaultScreenWidthWeb/HeightWeb).
- Scripting backend: the `scriptingBackend:` map contains only `Android: 1` (IL2CPP); no Standalone entry, so Windows uses the Unity default (Mono; ESTIMATE). WebGL is always IL2CPP (Unity requirement; ESTIMATE from knowledge). il2cppCompilerConfiguration, il2cppCodeGeneration, managedStrippingLevel, apiCompatibilityLevelPerPlatform are all `{}` (defaults, not overridden). Global apiCompatibilityLevel: 6 (enum meaning ESTIMATE: .NET Standard 2.1). scriptingRuntimeVersion 1, gcIncremental 1, allowUnsafeCode 0.
- stripEngineCode: 1. Color space: m_ActiveColorSpace 1 (Linear; ESTIMATE from enum). activeInputHandler: 1 (new Input System only). useHDRDisplay 0. strictShaderVariantMatching 0.
- Graphics APIs: m_BuildTargetGraphicsAPIs `[]` (Unity defaults, no override). Batching: Standalone static batching on / dynamic off; WebGL both off.
- WebGL (lines 758-783): template PROJECT:VoxVegetallis, compressionFormat 1 (Gzip), decompressionFallback 1, memory initial 256 MB / max 1024 MB, growth mode 2 (Geometric; step 0.2, cap 96), exceptionSupport 1 (explicit only), dataCaching 1, debugSymbols 0, nameFilesAsHashes 0, threadsSupport 0, linkerTarget 1 (Wasm), legacy webGLMemorySize 32, powerPreference 2 (HighPerformance). They match ConfigureWebGL (the menu rewrites them every WebGL build).
- Quality (ProjectSettings/QualitySettings.asset): m_CurrentQuality 5 (Ultra); per-platform defaults Standalone 5, WebGL 3 (High), Android/iPhone 2. customRenderPipeline points at the UniversalRP asset (guid 681886c5...). Uncommitted working-copy change (git diff): one level's vSyncCount 1 -> 0 (the only modified file in git status). Levels: Very Low/Low/Ultra vSync 0; Medium/High/Very High vSync 1.
- URP (Assets/Settings/UniversalRP.asset): m_RendererType 1 (2D renderer), m_SupportsHDR 1, m_MSAA 1, m_RenderScale 1, m_UseSRPBatcher 1, m_ColorGradingMode 0. URP package 17.3.0 (Packages/manifest.json).
- 2D Renderer (Assets/Settings/Renderer2D.asset): m_TransparencySortMode 3 (Custom Axis; enum meaning ESTIMATE, matches CLAUDE.md), m_TransparencySortAxis (0,1,0); m_UseNativeRenderPass 0.
- URP global settings: m_StripRuntimeDebugShaders 1, m_StripUnusedVariants 1, m_StripUnusedPostProcessingVariants 0.
- Input: com.unity.inputsystem 1.20.0; actions asset Assets/Data/Input/GameInput.inputactions.
- Other packages (manifest): 2d.animation 13.0.6, 2d.psdimporter 12.0.2, 2d.aseprite 3.0.2, 2d.spriteshape 13.0.0, 2d.tilemap.extras 6.0.3.

### (iii) Tools/export_art.ps1
Downscales every ArtSource/<sub>/<name>.png to 50% (rounded) into Assets/Art/<sub>/<name>.png (4x masters -> 2x game PNGs) using System.Drawing: HighQualityBicubic, 32bppArgb (alpha kept), 72 dpi, TileFlipXY wrap mode to avoid edge bleed. Skips files whose output exists and is newer than the master unless -Force. Root = parent of Tools/. Run from project root: `powershell -ExecutionPolicy Bypass -File Tools/export_art.ps1` (all), `... export_art.ps1 ScaleTest` (one subfolder, param $Sub), `... -Force`. Dependencies: Windows PowerShell + .NET System.Drawing (Windows-only), nothing else. Prints "export"/"skip" lines and "done: N exported, M skipped". Other tools: Tools/perf_analyze.ps1, Tools/ServeWebBuild.ps1 (serves newest Builds\WebGL\*-demo on http://localhost:8080/ and opens Chrome, per WEBGL_REPORT.md line 126), Tools/WebTest/WebTest.cs.

### (iv) Latest build sizes and times
Latest = 1.0.7-demo (build_log.csv; BuildVersion.json 1.0.7).
- Windows 1.0.7-demo: 13 s, 164,368,941 bytes = 156.8 MB (Editor-prev.log line 94562 "BUILD Windows 1.0.7: Succeeded in 13 s, 156.8 MB"); zip 52,529,596 bytes (ls -l). Largest files: resources.assets.resS 39.8 MB, UnityPlayer.dll 36.8 MB, resources.assets 12.8 MB.
- WebGL 1.0.7-demo: 29 s, 28,075,511 bytes = 26.8 MB (Editor-prev.log line 100622); zip 27,387,804 bytes. Files: Build/1.0.7-demo.data.unityweb 14,668,885; .wasm.unityweb 13,113,775; .framework.js.unityweb 91,139; loader.js 47,867.
- The 13 s / 29 s of 1.0.7 vs 223-543 s earlier WebGL builds: cache effect is an ESTIMATE (no log states a cause).
- Other WebGL times: release 1.0.2 532 s, 1.0.3 225 s, 1.0.4 521 s, 1.0.5-demo 293 s, 1.0.6-demo 223 s. WebGL dev 1.0.3-dev: 543 s, 224,571,668 bytes (wasm 199.7 MB, data 23.9 MB; zip 47,020,338). Windows range 13-64 s.
- Unity Build Report (Editor-prev.log; Windows block starts line 91616): complete build size 156.8 MB; user assets 65.0 MB: Textures 57.8 MB (89.0%), Sounds 4.3 MB (6.6%), Shaders 985.5 KB, Other 1.1 MB, Levels 417.3 KB. WebGL block (line 97708): complete size 26.8 MB; textures 57.8 MB uncompressed (90.8%), sounds 3.5 MB. Top assets: Assets/Art/UI/VoxKit/spotlight_arch.png 8.2 MB, Assets/Art/ScaleTest/arena01_backdrop.png 7.9 MB, CinzelDecorative-Bold SDF and -Black SDF 4.0 MB each, Unity splash logo 2.7 MB, UI/Backdrop/ring_dashed.png 2.4 MB, boss_pumpking_idle.png 1.6 MB. Log line 97650: "Total compressed size 9.2 MB. Total uncompressed size 61.2 MB." (what it measures: ESTIMATE, texture compression summary).
- Current Editor.log (last written Oct 1 13:33, 52 KB) has no build report; the numbers above come from Editor-prev.log (8.8 MB, last written Sep 30 22:16).
- Builds/ disk usage (du -sh): Windows 944M (6 version folders), WebGL 375M, Dev 186M, Perf 191M (older BulletHell.exe builds, dated Sep 29), zips: WebGL 27M x6, WebGL dev 45M, Windows demo 51M x3.
- Folders present: Windows 1.0.1, 1.0.2, 1.0.4, 1.0.5-demo, 1.0.6-demo, 1.0.7-demo; WebGL 1.0.2, 1.0.3, 1.0.3-dev, 1.0.4, 1.0.5-demo, 1.0.6-demo, 1.0.7-demo.
- Docs/WEBGL_REPORT.md: download 27.9 MB zip (wasm 12.8 MB, data 14.3 MB, gzip), dev build 224 MB (line 20); cold load localhost 1.6 s to first frame; 50 Mbit/s + 50 ms = 7.1 s; 10 Mbit/s = 25.9 s; warm 1.6 s; release round 1 combat 236 fps avg, p99 6.1 ms (monitor 240 Hz cap); WebGL single-threaded, about 7x slower than Windows under stress. Demo section (lines 133-137): 27.4 MB zip (wasm 13.1 + data 14.7 MB), load 1.6-2.2 s, Windows demo zip 52.5 MB, 227-240 fps in menus/round 1. Brotli would save a few MB but needs host headers (line 93). Memory 256 MB start / 1 GB cap.

## UNKNOWN / not determined
- Windows Standalone scripting backend is not stored explicitly (Mono default is an ESTIMATE); actual graphics API list for Windows (defaults not overridden; Builds contain a D3D12 folder, but API used not verified).
- Managed stripping level in effect (default, not overridden).
- Cause of the fast 1.0.7 build times.
- Zip step duration (log times BuildPlayer only).
- Meaning of the two "Unknown" 1.0.4 rows (likely an aborted attempt, ESTIMATE).
- Any builds after 1.0.7 (none in logs).

---

# SECTION 1b - Scene hierarchies

# Section 1b: Scene hierarchies

## Method (stated clearly)
FALLBACK method: static YAML parsing. The Unity CLI/live Editor was NOT used (avoids any risk of dirtying scenes). I wrote scratchpad/dump.ps1 (PowerShell; no python/node on machine) which reads Assets/Scenes/*.unity, links GameObject/Transform/RectTransform parents (m_Father), maps MonoBehaviour m_Script GUIDs to scripts via Assets/**/*.cs.meta (project scripts shown by name) and Library/PackageCache *.cs.meta (shown as "[pkg]Name", read-only), and reads PrefabInstance m_SourcePrefab GUIDs. Native components shown with "~" prefix (e.g. ~Canvas, ~SpriteRenderer). "(inactive)" = m_IsActive 0 in the file. "* group 'X' xN" = repetitive siblings collapsed (first one shown). Depth limit 7. Prefab instance internals are NOT expanded (only "[PrefabInstance of X.prefab] name=..."). Runtime-created objects are NOT in these dumps (see lists below).
Caveats: scene-level overrides on prefab instances are not shown. Component lists omit Transform/RectTransform.

## Scenes in build (Source: ProjectSettings/EditorBuildSettings.asset, all enabled: 1)
0. Assets/Scenes/Boot.unity
1. Assets/Scenes/MainMenu.unity
2. Assets/Scenes/Game.unity
Not in build: Assets/Scenes/RigTest.unity (exists in folder; 11 YAML docs, not dumped).
Object counts (from dump): Boot 2 GameObjects; MainMenu 105 GameObjects / 2 prefab instances; Game 341 GameObjects / 19 prefab instances (16 root objects).

## Boot.unity (2 GameObjects)
- Bootstrapper  <Bootstrapper>
- Main Camera  <~AudioListener, ~Camera>

## MainMenu.unity (6 root objects)
- EventSystem  <UIFocusGuard, [pkg]InputSystemUIInputModule, [pkg]EventSystem>
- Main Camera  <~AudioListener, ~Camera>
- MainMenu  <MainMenuController, MenuPrimaryRouter, MenuInputReader>
- MenuCanvas [Rect]  <[pkg]GraphicRaycaster, [pkg]CanvasScaler, ~Canvas>
  - SafeArea [Rect]  <SafeAreaFitter>
    - SettingsScreen (inactive) [Rect]  <SettingsScreen, [pkg]Image, ~CanvasRenderer>
      - Panel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - BadgeL1 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - SectionPrefab (inactive) [Rect]  <[pkg]LayoutElement, ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Viewport [Rect]  <ScrollFollowSelection, [pkg]ScrollRect, [pkg]RectMask2D>
          - Rows [Rect]  <[pkg]VerticalLayoutGroup, [pkg]ContentSizeFitter>
        - BadgeR1 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Tabs [Rect]  <[pkg]HorizontalLayoutGroup>
          - TabAudio [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabControls [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabGameplay [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabVideo [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - Strip [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
      - Note [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Back [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
    - Buttons [Rect]  <[pkg]VerticalLayoutGroup>
      - Quit [Rect]  <[pkg]LayoutElement, [pkg]Image, ~CanvasRenderer, [pkg]Button, ThemedButton, FocusDecor>
        - FocusTrimR (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - FocusTrimL (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - LaurelR (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - LaurelL (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
      - NewGame [Rect]  <[pkg]LayoutElement, [pkg]Image, ~CanvasRenderer, [pkg]Button, ThemedButton, FocusDecor>
        - FocusTrimL (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - FocusTrimR (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - LaurelL (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - LaurelR (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
      - Continue [Rect]  <[pkg]LayoutElement, [pkg]Image, ~CanvasRenderer, [pkg]Button, ThemedButton, FocusDecor>
        - FocusTrimL (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - LaurelL (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - FocusTrimR (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - LaurelR (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
      - Settings [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton, FocusDecor>
        - LaurelR (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - FocusTrimL (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - LaurelL (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - FocusTrimR (inactive) [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
    - Title [Rect]  <>
      - Logo [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - LogoSoil [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Version [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Tagline [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - TopStrip [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - LogoLeaf [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
    - CustomizationScreen (inactive) [Rect]  <CustomizationScreen>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Back [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - TopStrip [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
      - Confirm [Rect]  <ThemedButton, CancelRelay, [pkg]Image, ~CanvasRenderer, [pkg]Button>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Randomize [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Panel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Rows [Rect]  <[pkg]VerticalLayoutGroup>
      - PortraitChip [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Text [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Portrait [Rect]  <HudPortrait>
          - Window [Rect]  <[pkg]Mask, [pkg]Image, ~CanvasRenderer>
            - Accessory [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Head [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Ring [Rect]  <[pkg]Image, ~CanvasRenderer>
    - Message [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
    - [PrefabInstance of ConfirmDialog.prefab] name='ConfirmDialog'
- GladiatorPreview (inactive)  <GladiatorPreviewMotion, ProceduralMotion>
  - Motion  <>
    * group 'Arm' x4 (first shown):
      - Arm3  <~SpriteRenderer>
    - [PrefabInstance of GladiatorDoll.prefab] name='GladiatorDoll'
  - Pedestal  <>
    - Spotlight  <~SpriteRenderer>
    - PedestalBase  <~SpriteRenderer>
- MenuBackdrop  <~SpriteRenderer>

## Game.unity (16 root objects)
- WaveSpawner  <WaveSpawner>
- Navigation  <AiDebugView, NavigationService>
- CoinField  <CoinField>
- Arena  <ArenaController, ArenaScenery, TallObstacleFader>
  - Walls  <~BoxCollider2D>
- BossPool  <EnemyPool>
- Main Camera  <~AudioListener, [pkg]UniversalAdditionalCameraData, ~Camera, CameraShake>
- EnemyPool  <EnemyPool>
- DebugOverlay [Rect]  <DebugOverlay, [pkg]CanvasScaler, ~Canvas>
  - SafeArea [Rect]  <SafeAreaFitter>
    - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
- DebugOverlayToggle  <DebugOverlayToggle>
- EventSystem  <UIFocusGuard, [pkg]InputSystemUIInputModule, [pkg]EventSystem>
- Global Light 2D  <[pkg]Light2D>
- TestPickups  <>
  - [PrefabInstance of AmmoPickup.prefab] name='Pickup_TestSpare'
  - [PrefabInstance of AmmoPickup.prefab] name='Pickup_Shotgun'
  - [PrefabInstance of AmmoPickup.prefab] name='Pickup_Laser'
  - [PrefabInstance of AmmoPickup.prefab] name='Pickup_Gatling'
  - [PrefabInstance of AmmoPickup.prefab] name='Pickup_Basic'
- ProjectilePool  <ProjectilePool>
- FlowUI [Rect]  <[pkg]GraphicRaycaster, [pkg]CanvasScaler, ~Canvas>
  - SafeArea [Rect]  <SafeAreaFitter>
    - WaveBanner [Rect]  <WaveBanner, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
    - RoundResultsPanel [Rect]  <FlowPanel, [pkg]Image, ~CanvasRenderer>
      - Box [Rect]  <[pkg]VerticalLayoutGroup, [pkg]Image, ~CanvasRenderer, ThemedImage>
        - Body [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - Continue [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - Title [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - MainMenu [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - Trim [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
    - ShopScreen (inactive) [Rect]  <ShopTooltip, ShopScreen>
      - MerchantPanel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Speech [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
          - Text [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Merchant [Rect]  <>
          - Nameplate [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - EyeL [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Laurel [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Pimento [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Belly [Rect]  <[pkg]Image, ~CanvasRenderer>
          - EyeR [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Body [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Head [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
      - Detail [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Body [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Bar [Rect]  <[pkg]Image, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Reroll [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Message [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - CrateOverlay (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - [PrefabInstance of ShopCard.prefab] name='Choice3'
        - [PrefabInstance of ShopCard.prefab] name='Choice2'
        - [PrefabInstance of ShopCard.prefab] name='Choice1'
      - Backdrop [Rect]  <[pkg]AspectRatioFitter, [pkg]Image, ~CanvasRenderer>
        - Dark [Rect]  <[pkg]Image, ~CanvasRenderer>
      - MainMenu [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Inventory [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Icon [Rect]  <[pkg]Image, ~CanvasRenderer>
      - Leave [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - InventoryLabel [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Currency [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Value [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Coin [Rect]  <[pkg]Image, ~CanvasRenderer>
      - Stall [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - ArmamentsLabel [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - ArmsLabel [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - [PrefabInstance of ShopCard.prefab] name='ArmamentCard2'
        - [PrefabInstance of ShopCard.prefab] name='ArmamentCard1'
        - [PrefabInstance of ShopCard.prefab] name='ArmamentCard3'
        - [PrefabInstance of ShopCard.prefab] name='CrateCard'
        - [PrefabInstance of ShopCard.prefab] name='ArmCard2'
        - [PrefabInstance of ShopCard.prefab] name='ArmCard1'
    - BossHud [Rect]  <BossHealthBar, ~CanvasGroup, HudScale>
      - Bar [Rect]  <>
        - Fill [Rect]  <[pkg]Image, ~CanvasRenderer>
        - PhaseTick [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Back [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Frame (inactive) [Rect]  <[pkg]Image, ~CanvasRenderer>
        - Trail [Rect]  <[pkg]Image, ~CanvasRenderer>
      - NamePlate [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
    - SettingsScreen (inactive) [Rect]  <SettingsScreen, [pkg]Image, ~CanvasRenderer>
      - Note [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Back [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Panel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Viewport [Rect]  <ScrollFollowSelection, [pkg]ScrollRect, [pkg]RectMask2D>
          - Rows [Rect]  <[pkg]VerticalLayoutGroup, [pkg]ContentSizeFitter>
        - BadgeL1 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Tabs [Rect]  <[pkg]HorizontalLayoutGroup>
          - Strip [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
          - TabVideo [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabAudio [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabGameplay [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - TabControls [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - SectionPrefab (inactive) [Rect]  <[pkg]LayoutElement, ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - BadgeR1 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
    - CombatHud [Rect]  <CombatHud>
      - Content [Rect]  <>
        - BottomRight [Rect]  <HudScale>
          - Tray [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
          * group 'Slot' x4 (first shown):
            - Slot2 [Rect]  <HudAmmoSlot>
              - Icon [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Highlight [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Frame [Rect]  <[pkg]Image, ~CanvasRenderer>
              - HoldRing [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Glyph [Rect]  <>
                ... (2 children not expanded, depth limit)
        - BottomLeft [Rect]  <HudScale>
          - Panel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
            - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
          - Hearts [Rect]  <HudHearts>
            * group 'Heart' x8 (first shown):
              - Heart4 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - HeatLabel [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - Portrait [Rect]  <HudPortrait>
            - Ring [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Window [Rect]  <[pkg]Mask, [pkg]Image, ~CanvasRenderer>
              - Accessory [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Head [Rect]  <[pkg]Image, ~CanvasRenderer>
          - HeatBar [Rect]  <HudHeatBar, ~CanvasGroup>
            - Fill [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Track [Rect]  <[pkg]Image, ~CanvasRenderer>
    - RoundIntro [Rect]  <RoundIntroBanner>
      - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
    - GameOverPanel (inactive) [Rect]  <FlowPanel, [pkg]Image, ~CanvasRenderer>
      - Box [Rect]  <[pkg]VerticalLayoutGroup, [pkg]Image, ~CanvasRenderer, ThemedImage>
        - Continue [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - Title [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - Trim [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Body [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - MainMenu [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
    - PausePanel (inactive) [Rect]  <FlowPanel, [pkg]Image, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Box [Rect]  <[pkg]VerticalLayoutGroup, [pkg]Image, ~CanvasRenderer, ThemedImage>
        - Title [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - MainMenu [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - Settings [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - Body [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - Continue [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
          - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI>
        - DebugRoundRow [Rect]  <DebugRoundPicker, [pkg]LayoutElement, [pkg]HorizontalLayoutGroup>
          - Label [Rect]  <[pkg]LayoutElement, ~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - Boss [Rect]  <ThemedButton, [pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - HpUp [Rect]  <ThemedButton, [pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - Go [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - HpDown [Rect]  <ThemedButton, [pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - Preview [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - StatsButton [Rect]  <ThemedButton, [pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - Lower [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
          - Raise [Rect]  <[pkg]LayoutElement, [pkg]Button, [pkg]Image, ~CanvasRenderer, ThemedButton>
            - Label [Rect]  <~CanvasRenderer, [pkg]TextMeshProUGUI, ThemedText>
        - Trim [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
    - RunHud [Rect]  <RunHud>
      - RoundPill [Rect]  <HudScale, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Wave [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - TrimR [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Divider [Rect]  <[pkg]Image, ~CanvasRenderer>
        - TrimL [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Round [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - CoinPill [Rect]  <HudScale, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Coins [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Coin [Rect]  <[pkg]Image, ~CanvasRenderer>
    - ArmoryScreen (inactive) [Rect]  <ArmoryScreen>
      - MainMenu [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Backdrop [Rect]  <[pkg]AspectRatioFitter, [pkg]Image, ~CanvasRenderer>
        - Dark [Rect]  <[pkg]Image, ~CanvasRenderer>
      - Fight [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - HintPill [Rect]  <[pkg]ContentSizeFitter, [pkg]HorizontalLayoutGroup, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Hint [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Inventory [Rect]  <ScrollFollowSelection, [pkg]ScrollRect, ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Viewport [Rect]  <[pkg]RectMask2D>
          - Content [Rect]  <[pkg]ContentSizeFitter, [pkg]GridLayoutGroup>
        - Empty [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Feet [Rect]  <ArmoryRing>
        * group 'Anchor' x8 (first shown):
          - Anchor7 [Rect]  <>
        - Doll [Rect]  <>
          - Accessory1 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Accessory2 [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Body [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Armor [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Head [Rect]  <[pkg]Image, ~CanvasRenderer>
        * group 'Slot' x8 (first shown):
          - Slot4 [Rect]  <ArmorySlotButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
            - Body [Rect]  <>
              - FocusRing [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Frame [Rect]  <[pkg]Image, ~CanvasRenderer>
              - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Pedestal [Rect]  <>
          - PedestalBase [Rect]  <[pkg]Image, ~CanvasRenderer>
        - BackArms [Rect]  <>
        - Bubbles [Rect]  <>
          - Tether2 (inactive) [Rect]  <ArmoryTether>
            - Strand [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Glow [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Tether1 (inactive) [Rect]  <ArmoryTether>
            - Glow [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Strand [Rect]  <[pkg]Image, ~CanvasRenderer>
          - Tether3 (inactive) [Rect]  <ArmoryTether>
            - Glow [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Strand [Rect]  <[pkg]Image, ~CanvasRenderer>
          - [PrefabInstance of ArmoryBubble.prefab] name='Bubble2'
          - [PrefabInstance of ArmoryBubble.prefab] name='Bubble3'
          - [PrefabInstance of ArmoryBubble.prefab] name='Bubble1'
        - DashedRing [Rect]  <[pkg]Image, ~CanvasRenderer>
        - FrontArms [Rect]  <>
      - Tabs [Rect]  <[pkg]HorizontalLayoutGroup, ArmoryTabs>
        - ArmamentsTab [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - BadgeR1 [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Strip [Rect]  <[pkg]LayoutElement, ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - ArmsTab [Rect]  <CancelRelay, [pkg]Button, [pkg]LayoutElement, ThemedImage, [pkg]Image, ~CanvasRenderer>
          - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
          - BadgeL1 [Rect]  <[pkg]Image, ~CanvasRenderer>
            - Label [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Title [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Remove [Rect]  <ThemedButton, CancelRelay, [pkg]Button, [pkg]Image, ~CanvasRenderer>
        - Label [Rect]  <[pkg]TextMeshProUGUI, ~CanvasRenderer>
      - Message [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
      - InfoPanel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Info [Rect]  <ThemedText, [pkg]TextMeshProUGUI, ~CanvasRenderer>
        - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
    - TutorialPanel [Rect]  <TutorialPanel, ~CanvasGroup, HudScale>
      - Card [Rect]  <>
        - Pips [Rect]  <[pkg]HorizontalLayoutGroup>
          * group 'Pip' x6 (first shown):
            - Pip4 [Rect]  <~CanvasRenderer, [pkg]Image>
        - Trim [Rect]  <ThemedTrim, [pkg]Image, ~CanvasRenderer>
        - Instruction [Rect]  <[pkg]TextMeshProUGUI, ThemedText, ~CanvasRenderer>
        - Panel [Rect]  <ThemedImage, [pkg]Image, ~CanvasRenderer>
        - Counter [Rect]  <[pkg]TextMeshProUGUI, ThemedText, ~CanvasRenderer>
        - Skip [Rect]  <[pkg]TextMeshProUGUI, ThemedText, ~CanvasRenderer>
    - TutorialController  <TutorialController>
    - [PrefabInstance of ConfirmDialog.prefab] name='ConfirmDialog'
- GameFlow  <GameFlowUI, GameSceneController, MenuPrimaryRouter, MenuInputReader>
- [PrefabInstance of Player.prefab] name='Player'

## Key prefabs referenced from Game/MainMenu (same method, Assets/Prefabs)
### Player.prefab (8 GameObjects + nested GladiatorDoll prefab instance)
- Player  <GameplayInputReader, PlayerMover, ArmSelectionController, AmmoSlots, AmmoPickupCollector, DebugArmamentControls, PlayerInventory, Health, PlayerHealth, ~SortingGroup, PlayerVisualRig, ~CircleCollider2D, JumpController, SpriteFx, ProceduralMotion, HitFeedback, PlayerFeedbackBinder>
  - ArmRing  <>
  - Shadow  <~SpriteRenderer>
  - Visuals  <>
    - Motion  <>
      - [PrefabInstance of GladiatorDoll.prefab] name='GladiatorDoll'
  - Core  <>
    - Marker  <~SpriteRenderer>
      - Outline  <~SpriteRenderer>

### Enemy.prefab (9 GameObjects; Boss.prefab is a variant of it per CLAUDE.md)
- Enemy  <~Rigidbody2D, Health, PatrolMover, Enemy, StatusEffects, EnemyAttacker, ~CapsuleCollider2D, ~SortingGroup, EnemyBrain, SpriteFx, ProceduralMotion, HitFeedback, TelegraphFx>
  - Rig  <>
    - HealthBar  <HealthBar>
      - Visuals  <>
        - Back  <~SpriteRenderer>
        - Fill  <~SpriteRenderer>
    - Motion  <>
      - Body  <~SpriteRenderer>
  - Shadow  <~SpriteRenderer>

## Runtime-created objects (NOT in scene files; from code grep, Assets/Scripts)
- "Services" GameObject with GameServices (DontDestroyOnLoad), created by GameServices.Ensure() (Core/GameServices.cs:49-53), called by Bootstrapper and by other scenes (self-bootstrap in the Editor). Plain C# objects held by it (Init, lines 60-79): LocalFileSaveSystem (ISaveSystem), RunManager, SceneLoader, AudioService (+AudioDirector), SettingsService, InputBindingService, ProfileService, TelemetryService. Config loaded from Resources/GameConfig.asset.
- AudioService creates children of the Services host: "SfxVoices" pool of AudioSources, "Music" root with two sources, plus an AudioHost component (Core/AudioService.cs:65-88).
- "PerfTools" (DontDestroyOnLoad, Perf/PerfHost.cs:36-37, AfterSceneLoad); PerfOverlayCanvas created by PerfOverlay.cs.
- "TelemetryOverlay" (Telemetry/TelemetryOverlay.cs:59, DontDestroyOnLoad at :281).
- "FeedbackHub" (Feedback/FeedbackHub.cs:61, lazily created), plus DeathGhost, Shockwave objects.
- Arena: ArenaController builds ObjectPool<Obstacle> and ObjectPool<Trap> (Arena/ArenaController.cs:88-89) instantiating obstacle/trap prefabs under the Arena object; ArenaScenery creates a "Scenery" root with sprite groups (ArenaScenery.cs:59,225,243); CameraLetterbox creates "LetterboxBars"; PlaceholderLook creates objects.
- Pools: EnemyPool / BossPool (scene objects, pre-existing in Game) fill with Enemy.prefab / Boss.prefab instances; ProjectilePool fills Projectile.prefab; CoinField uses Coin.prefab (pool contents are runtime; not verified individually).
- Player arms are spawned from the loadout at runtime (CLAUDE.md "arms are spawned from the loadout at runtime"; Arm.prefab exists; Player/ArmRing is empty in prefab). Beam_{slot} beam objects (Weapons/ArmFireController.cs:220), DustPuffs, WarningLine, SmashTelegraph, DangerGlow, PickupGlow, Occluded arm silhouettes, ArmoryRing/ShopCard ghost UI objects, FocusRing (ButtonFocusFx) are also created in code.
- Debug/diagnostic: BulletPathDebug view, AiDebugView child (names via AiDebugView.cs:57).

## Verification
git status --short before and after my work: only " M ProjectSettings/QualitySettings.asset" (pre-existing). No project files written; scripts/outputs are only in the scratchpad.

## UNKNOWN
- Scene-level property overrides on prefab instances, serialized field values, and per-component enabled state (not parsed).
- Exact runtime object counts (pool sizes, obstacles per layout) - runtime only.
- Package scripts shown as "[pkg]" are mapped from Library/PackageCache by GUID; names like TextMeshProUGUI, Button, Image, EventSystem are as resolved; any "?xxxxxxxx" would be unresolved (none observed in the dumps).
- RigTest.unity contents not examined.

---

# SECTION 1c - C# classes and ScriptableObject types

# Section 1c: C# type inventory and ScriptableObject asset counts

Generated 2026-10-01 by scripts in the scratchpad (first_add.sh, decls.awk, gen.sh, so.sh, assemble.sh). Read-only on the project.

## Method and scope
- Scope: every `*.cs` under `Assets/` = 317 files, ALL tracked by git (`git ls-files 'Assets/*.cs'` = 317; no untracked .cs; `git status` shows only ProjectSettings/QualitySettings.asset modified). By top folder: Scripts/ = 244, Editor/ = 41, Tests/ = 32.
- Excluded: nothing had to be excluded. `find Assets -name '*.cs'` returns no file outside Assets/Scripts, Assets/Editor, Assets/Tests (no .cs under Assets/ThirdParty, Assets/TextMesh Pro, Assets/Fonts, Assets/WebGLTemplates, Assets/Settings, Assets/Resources). Packages/ (UPM) was not scanned. Tools/WebTest/WebTest.cs lives outside Assets/ and is not included.
- Declarations were found with an awk regex over class/struct/enum/interface lines (335 classes, 41 structs, 53 enums, 8 interfaces matched, minus 1 false positive in AudioSetup.cs where a code comment contained the words 'struct would'). Result: 436 rows. Every one of the 317 files yields at least one row (checked).
- Purpose column: taken from the type's own XML-doc or // comment directly above the declaration (first sentence, or two when the first is very short), which I read as part of the code. For the 87 types with no such comment (small enums/structs/private helpers, and most test classes) I wrote the purpose after reading the declaration and members (overrides.tsv). Where a purpose repeats a comment, it is the author's description, not an independent behavioural audit. Test classes were described from their test-method names.
- Milestone column: first-add commit of the file from `git log --diff-filter=A --name-only` (first_add.sh), mapped to the milestone named in that commit subject, short hash in parentheses. ArenaArt.cs (first added as ScaleTestArt.cs, 15b44f0) and ArmamentData.cs (first added as UpgradeData.cs, 9d5fef6) were resolved via rename (git reports the later rename, so they do not appear as added under their current names).
- Commit-label caveats (the milestone is the commit label, which is not always the milestone the code belongs to):
  - 9d5fef6 is titled 'Design: game flow, save system, shop and armory' but contains M3b code (ArmInstance, ArmStats, StatCalculator, UpgradeData -> ArmamentData, M3bSetup, DebugUpgradeControls, 2 test files); the 'M3b' commit (4b0398d) came after. Labelled 'M3b code (committed early in Design commit)'.
  - 5b356f7 'WIP M1' holds the first M1 code (10 files); c52770a 'M1: ...' added no new .cs file.
  - 8cefcd0 'M2 done; M3 design' holds the 14 M2 files.
  - 0e117c7 is titled 'M5: waves, rounds...' (the M5b work; M5a is 295524f).
  - f74d79b bundles 'Scale lock 1.15x, UI1 UI theme, CC1 paper-doll character creation' in one commit; its 8 new files cannot be separated by commit.
  - 1a79436 bundles M10 (Pumpking) and P1 partial (performance tools); I split its files by folder: Scripts/Perf and Perf-named files = P1 partial, rest = M10 (ESTIMATE by folder, not by commit).
  - 15b44f0 / c393055 are 'Scale test' commits (no numbered milestone; CLAUDE.md calls it 'Art scale test').
  - Files added and later deleted (10, not in the tables): Editor/M5aSetup.cs, Editor/UiThemeCapture.cs, Editor/UiThemeSetup.cs, Scripts/Arena/ScaleTestArt.cs (renamed to ArenaArt.cs), Scripts/Core/CharacterScale.cs, Scripts/Cosmetics/CosmeticData.cs, Scripts/Enemies/CombatController.cs, Scripts/Enemies/HitFlash.cs, Scripts/UI/DebugUpgradeControls.cs, Scripts/Weapons/UpgradeData.cs (renamed to ArmamentData.cs). Source: first_add.tsv minus current file list.
- Nested/private types are listed as their own rows (short name; the file column says where they live). The Type column shows 'Name : bases' for classes.

## Counts

| Group | Rows (types) | Files |
|---|---|---|
| Editor | 56 | 41 |
| Scripts/AI | 19 | 15 |
| Scripts/Arena | 27 | 19 |
| Scripts/Armory | 13 | 9 |
| Scripts/Audio | 11 | 6 |
| Scripts/Bosses | 11 | 5 |
| Scripts/Core | 19 | 17 |
| Scripts/Cosmetics | 8 | 5 |
| Scripts/Enemies | 29 | 18 |
| Scripts/Feedback | 18 | 14 |
| Scripts/Input | 15 | 8 |
| Scripts/Perf | 8 | 7 |
| Scripts/Pickups | 6 | 6 |
| Scripts/Platform | 4 | 2 |
| Scripts/Player | 17 | 15 |
| Scripts/Projectiles | 8 | 4 |
| Scripts/Save | 7 | 6 |
| Scripts/Settings | 3 | 3 |
| Scripts/Shop | 16 | 11 |
| Scripts/Telemetry | 11 | 6 |
| Scripts/UI | 62 | 44 |
| Scripts/Weapons | 31 | 24 |
| Tests | 37 | 32 |
| TOTAL | 436 | 317 |

Kinds across all rows: class 335, enum 53, interface 8, struct 40, 

## Scripts/AI

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/AI/AiDebugView.cs | AiDebugView : MonoBehaviour | class | Debug view of the enemy AI, toggled by the "Show AI debug" setting (development builds only): the flow field as small arrows (every few cells) coloured from near the player (warm) to far (cool), cells the enemies can't walk in ... | M8 (6fa1892) |
| Scripts/AI/Behaviors/BossBehavior.cs | BossBehavior : IEnemyBehavior | class | The boss's movement and attack choices: it holds a distance band from the player between attacks, then picks one of the current phase's attacks by weight (never the same kind twice in a row), winds it up with a readable telegra... | M10 (1a79436) |
| Scripts/AI/Behaviors/BossBehavior.cs | State | enum | Private enum of the boss attack cycle: Enter, Reposition, Windup, Fire, Jump, PostSmash, Recover. | M10 (1a79436) |
| Scripts/AI/Behaviors/ChargerBehavior.cs | ChargerBehavior : IEnemyBehavior | class | Approaches along the flow field until it has a clear straight run at the player, stops and telegraphs (warning line, pulsing tint), then dashes along the locked line. | M8 (6fa1892) |
| Scripts/AI/Behaviors/ChargerBehavior.cs | State | enum | Private enum: Approach, Telegraph, Dash, Recover. | M8 (6fa1892) |
| Scripts/AI/Behaviors/ChaserBehavior.cs | ChaserBehavior : IEnemyBehavior | class | Pursues the player's feet (their shadow while jumping) along the flow field and hurts on contact. | M8 (6fa1892) |
| Scripts/AI/Behaviors/RangedPositioner.cs | RangedPositioner | class | Where a ranged enemy wants to go: without a clear line, or too far, it advances along the flow field; too close, it backs off (sliding sideways if something is behind it); in the comfortable band it strafes and flips direction ... | M8 (6fa1892) |
| Scripts/AI/Behaviors/SentryBehavior.cs | SentryBehavior : IEnemyBehavior | class | Moves to a firing spot (a clear line, inside its range band), plants, streams bullets until the shared HeatComponent overheats, then stops firing and crawls while it cools: that is its vulnerable window. | M8 (6fa1892) |
| Scripts/AI/Behaviors/SkirmisherBehavior.cs | SkirmisherBehavior : IEnemyBehavior | class | Ranged harasser: holds its preferred distance, strafes, backs off when approached and fires its patterns, but only with a clear line to the player; without one it repositions. | M8 (6fa1892) |
| Scripts/AI/Behaviors/SniperBehavior.cs | SniperBehavior : IEnemyBehavior | class | Keeps far from the player with a clear line (repositioning when it has none), then stops, shows a warning line that tracks the player and locks shortly before the shot, and fires one fast bullet along it. | M8 (6fa1892) |
| Scripts/AI/Behaviors/SniperBehavior.cs | State | enum | Private enum: Position, Aim. | M8 (6fa1892) |
| Scripts/AI/BossAttackPicker.cs | BossAttackPicker | class | Weighted choice among a phase's attacks that never repeats the previous kind when another kind is available. | M10 (1a79436) |
| Scripts/AI/EnemyAgent.cs | IEnemyBehavior | interface | One behaviour of an enemy (chase, skirmish, sentry...). | M8 (6fa1892) |
| Scripts/AI/EnemyAgent.cs | EnemyAgent | class | What a behaviour works with: where the enemy and the player are, how to steer along the ground plane, line of sight, contact hits. | M8 (6fa1892) |
| Scripts/AI/EnemyAiTuning.cs | EnemyAiTuning : ScriptableObject | class | Shared numbers for enemy navigation, separation and line of sight. | M8 (6fa1892) |
| Scripts/AI/FlowField.cs | FlowField | class | A grid flow field over the arena: every free cell knows the direction of the cheapest walk to a target (the player's feet). | M8 (6fa1892) |
| Scripts/AI/NavigationService.cs | NavigationService : MonoBehaviour | class | Enemy navigation for the arena: owns the flow field towards the player's FEET (so it follows the shadow during a jump), keeps it up to date (rebuilt when a breakable breaks, when the arena is rebuilt for a round, and as the pla... | M8 (6fa1892) |
| Scripts/AI/Steering.cs | Steering | class | Grounded steering: an enemy has a velocity with a heading it can only turn at a limited rate, accelerates up to its top speed, brakes when it should stop, and slows down while it is still turning towards where it wants to go. | M8 (6fa1892) |
| Scripts/AI/WarningLine.cs | WarningLine | class | A telegraph line on the ground (charger dash, sniper shot). | M8 (6fa1892) |

## Scripts/Arena

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Arena/ArenaArt.cs | ArenaArt : ScriptableObject | class | The painted arena backdrop (replaces the placeholder floor, walls, stands and torches) and where it sits. | Art scale test (15b44f0) |
| Scripts/Arena/ArenaController.cs | ArenaController : MonoBehaviour | class | Builds and runs the colosseum of the current round: the layered scenery, obstacles, traps, the collision grids, the fixed camera and the player's starting spot. | M7 (28a5d2c) |
| Scripts/Arena/ArenaData.cs | ArenaData : ScriptableObject | class | The colosseum shell: the walls (a rectangle centred on the origin) and the placeholder colours. | M7 (28a5d2c) |
| Scripts/Arena/ArenaGrid.cs | ArenaGrid | class | The arena's collision picture: a uniform grid over the playable rectangle where every cell is free or owned by an obstacle. | M7 (28a5d2c) |
| Scripts/Arena/ArenaLayoutData.cs | ObstaclePlacement | struct | Serializable struct: one placed obstacle (ObstacleData, position, optional size override). | M8.5 (34bbe88) |
| Scripts/Arena/ArenaLayoutData.cs | TrapPlacement | struct | Serializable struct: one placed trap (TrapData, position, rotation, extra start delay). | M8.5 (34bbe88) |
| Scripts/Arena/ArenaLayoutData.cs | ArenaLayoutData : ScriptableObject | class | What stands in a colosseum for a round: where the player starts, the gates enemies come out of, and the placed obstacles and traps, on top of an  shell. | M8.5 (34bbe88) |
| Scripts/Arena/ArenaScenery.cs | ArenaScenery : MonoBehaviour | class | The layered look of the colosseum, built from an ArenaData: floor with decals, back wall with the gates the enemies come out of, side walls, the crowd, and a FOREGROUND layer (front railing, drapes, torches, front crowd) that d... | M7.5 (ce21bf8) |
| Scripts/Arena/CameraLetterbox.cs | CameraLetterbox : MonoBehaviour | class | Keeps the fixed camera on exactly one aspect ratio (the painted arena's, 16:9): the camera's viewport is the largest centred rectangle of that aspect, and a second camera clears the bars around it. | Art scale test (fixes) (c393055) |
| Scripts/Arena/HazardBudget.cs | HazardBudget | struct | How much hazard and clutter a round's layout may hold: a maximum count per trap kind and per obstacle class. | M8.5 (34bbe88) |
| Scripts/Arena/LayoutValidator.cs | LayoutValidator | class | Checks an arena layout against the rules every layout must keep: things inside the walls, a clear area around the player's spawn, a walkable lane from every enemy gate to the spawn, no trap on top of the spawn or a gate, and th... | M8.5 (34bbe88) |
| Scripts/Arena/Obstacle.cs | Obstacle : MonoBehaviour, IDamageable | class | A placed obstacle. The object sits at the centre of its flat footprint, which is what blocks movement (through the arena grid). | M7 (28a5d2c) |
| Scripts/Arena/ObstacleData.cs | ObstacleKind | enum | Enum: Solid (permanent) or Breakable. | M7 (28a5d2c) |
| Scripts/Arena/ObstacleData.cs | ObstacleShape | enum | Shape of the footprint on the floor. Circle is an ellipse when the footprint is wider than deep. | M7 (28a5d2c) |
| Scripts/Arena/ObstacleData.cs | ObstacleHeightClass | enum | How tall an obstacle stands (ART_SPEC section 3). | M7 (28a5d2c) |
| Scripts/Arena/ObstacleData.cs | ObstacleData : ScriptableObject | class | One kind of obstacle. Both kinds block movement and every bullet. | M7 (28a5d2c) |
| Scripts/Arena/ObstacleHealthStages.cs | ObstacleHealthStages | class | Which damage stage a breakable obstacle shows for its remaining health. | M7 (28a5d2c) |
| Scripts/Arena/PerspectiveTuning.cs | PerspectiveTuning : ScriptableObject | class | Numbers that tie the 3/4 art to the flat 2D gameplay plane: how far bullets reach above an obstacle's footprint, how big an enemy's footprint and hurtbox are compared to its sprite, and how the sprite sits over its feet. | M7.5 (ce21bf8) |
| Scripts/Arena/PlaceholderLook.cs | PlaceholderLook | class | Stand-in styling for placeholder sprites (flat circles and squares) so they sit in the painted backdrop: a soft contact shadow on the floor and a dark outline behind the sprite. | Art scale test (fixes) (c393055) |
| Scripts/Arena/TallObstacleFader.cs | FadeRule | class | The "is something behind this obstacle" test, plain math. | M8.5 (34bbe88) |
| Scripts/Arena/TallObstacleFader.cs | TallObstacleFader : MonoBehaviour | class | Fades every standing Tall obstacle to ~40% while the player or an enemy stands behind it, so nothing is hidden. | M8.5 (34bbe88) |
| Scripts/Arena/Trap.cs | Trap : MonoBehaviour | class | A placed trap. It cycles Idle -> Telegraph (a visible warning) -> Active (strikes) -> Cooldown, but only while the run is in Combat, so it does nothing during the round intro and freezes with pause. | M7 (28a5d2c) |
| Scripts/Arena/TrapData.cs | TrapKind | enum | Enum of trap shapes: Vent (burst), Skewer (line strike), Zone (damage ticks). | M7 (28a5d2c) |
| Scripts/Arena/TrapData.cs | TrapData : ScriptableObject | class | One kind of trap. It hurts anything inside its area, the player and enemies alike, so luring enemies into it works. | M7 (28a5d2c) |
| Scripts/Arena/TrapShape.cs | TrapShape | class | Overlap tests between a trap's area and a circle (the player's hitbox). | M7 (28a5d2c) |
| Scripts/Arena/TrapTimer.cs | TrapPhase | enum | Enum of the trap clock phases: Idle, Telegraph, Active, Cooldown. | M7 (28a5d2c) |
| Scripts/Arena/TrapTimer.cs | TrapTimer | class | The phase clock of one trap: Idle (until started, then for the start delay) -> Telegraph -> Active -> Cooldown -> Telegraph -> ... | M7 (28a5d2c) |

## Scripts/Armory

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Armory/ArmoryActions.cs | ArmoryResult | enum | Enum result of arm-slot actions: Done, SlotOccupied, SlotEmpty, NotInInventory, LastArm. | M4 (ced81c5) |
| Scripts/Armory/ArmoryActions.cs | ArmoryActions | class | The arm-slot half of the Armory (armament equipping lives on ArmInstance). | M4 (ced81c5) |
| Scripts/Armory/ArmoryBubble.cs | ArmoryBubble : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler | class | A floating armament slot: pops up above the selected arm, bobs gently, and is tied to the arm by a tether. | M9c (4711107) |
| Scripts/Armory/ArmoryInventoryView.cs | ArmamentStack | struct | One armament in the inventory with how many copies the player holds. | M9c (4711107) |
| Scripts/Armory/ArmoryInventoryView.cs | ArmoryInventoryView | class | The inventory as the Armory's card grid shows it. | M9c (4711107) |
| Scripts/Armory/ArmoryPreview.cs | ArmoryChange | struct | What equipping (or removing) an armament in a chosen slot of an arm would do. | M9c (4711107) |
| Scripts/Armory/ArmoryPreview.cs | ArmoryPreview | class | The before -> after preview shown in the Armory before an armament is confirmed. | M9c (4711107) |
| Scripts/Armory/ArmoryRing.cs | ArmoryRing : MonoBehaviour | class | The left half of the Armory: the gladiator on a pedestal with the 8 arm slots on the flattened ellipse around its FEET (the same ArmRingTuning / ArmRingMath the game uses), drawn with uGUI Images. | M9c (4711107) |
| Scripts/Armory/ArmoryScreen.cs | ArmoryScreen : MonoBehaviour | class | The Armory (between the Shop and the next round): the gladiator with the 8 arm slots on the ring on the left, an inventory of cards (Arms / Armaments tabs) on the right. | M4 (ced81c5) |
| Scripts/Armory/ArmoryScreen.cs | Level | enum | Private enum of the Armory focus depth: Ring, Bubbles, Picking. | M4 (ced81c5) |
| Scripts/Armory/ArmorySlotButton.cs | ArmorySlotButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler | class | One of the 8 arm slots on the ring: a transparent hit area with a small frame that shows whether the slot is empty or holds an arm. | M9c (4711107) |
| Scripts/Armory/ArmoryTabs.cs | ArmoryTabs : MonoBehaviour | class | The Arms / Armaments tabs above the inventory grid. | M9c (4711107) |
| Scripts/Armory/ArmoryTether.cs | ArmoryTether : MonoBehaviour | class | A thin glowing line from an arm to its bubble. | M9c (4711107) |

## Scripts/Audio

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Audio/AudioDirector.cs | AudioDirector : IDisposable | class | Decides which music plays and when the stingers fire, from the run state alone (so no scene has to be wired): combat music (a different track per round) or the boss track from the round intro, the Shop track from Round Results ... | D4 (c379716) |
| Scripts/Audio/AudioHost.cs | AudioHost : MonoBehaviour | class | Gives the AudioService a frame tick (crossfades, ducking). | D4 (c379716) |
| Scripts/Audio/AudioLibrary.cs | AudioLibrary : ScriptableObject | class | Everything the AudioService needs from assets: which  answers each , which tracks play in each , the mixer and its groups, and the tuning (crossfade length, voice pool size). | D4 (c379716) |
| Scripts/Audio/AudioLibrary.cs | SfxEntry | struct | Serializable struct pairing an SfxId with its SfxData. | D4 (c379716) |
| Scripts/Audio/AudioLibrary.cs | MusicEntry | struct | Serializable struct: a MusicContext with its clips and volume. | D4 (c379716) |
| Scripts/Audio/SfxData.cs | SfxData : ScriptableObject | class | One sound cue: its clips (a random one plays), volume and pitch range, and the limits that keep a heavy fight from turning into noise: how many copies of it may play at once and how soon it may repeat. | D4 (c379716) |
| Scripts/Audio/SfxId.cs | SfxId | enum | Every sound the game asks for by name. The AudioLibrary maps each to an  (clips, volume, limits). | D4 (c379716) |
| Scripts/Audio/SfxId.cs | MusicContext | enum | The music that plays in each part of the game. | D4 (c379716) |
| Scripts/Audio/SfxId.cs | AudioBus | enum | Which mixer group a sound plays through (all three sit under the SFX volume slider; Music has its own). | D4 (c379716) |
| Scripts/Audio/SfxVoiceLimiter.cs | VoiceDecision | enum | What to do with a sound that wants to play. | D4 (c379716) |
| Scripts/Audio/SfxVoiceLimiter.cs | SfxVoiceLimiter | class | The rules that keep identical sounds from piling up, as plain maths so they can be tested: a sound repeats no faster than its minimum interval, no more than its maximum number of copies play at once, and when the limit is hit t... | D4 (c379716) |

## Scripts/Bosses

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Bosses/BossController.cs | BossController : MonoBehaviour, IDeathSequence | class | The boss-only layer on top of a pooled Enemy: phases and the transition between them (roar, flash, shake, glow), the Jump &amp; Smash (jump, landing ring, radial ground damage) and the long death sequence. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | BossAttackKind | enum | The three things a boss can do. Each attack entry names one and the pattern preset it fires. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | BossAttack | class | Serializable boss attack definition: kind, pattern preset, windup, volleys, interval, angle step, cooldown, weight. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | BossPhase | class | Serializable boss phase: HP threshold, speed multiplier, attack list, glow settings. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | SmashSettings | class | Serializable settings for the boss Jump & Smash landing: radius, damage to player/enemies, telegraph colour/ring, follow-up rings. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | TransitionSettings | class | Serializable settings for the phase-change transition: duration, invulnerability, roar inflate, flashes, shake, hitstop. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | DeathSettings | class | Serializable settings for the multi-stage boss death: stagger, squash, dissolve, flashes, debris bursts. | M10 (1a79436) |
| Scripts/Bosses/BossData.cs | BossData : ScriptableObject | class | Everything tunable about one boss: health, its phases (each with its own attack list, speed and glow), the jump, the smash, the phase transition and the death sequence. | M10 (1a79436) |
| Scripts/Bosses/BossEvents.cs | BossEvents | class | Static hooks for the one boss on the field: the boss health bar, the debug options and the overlay listen here instead of searching the scene. | M10 (1a79436) |
| Scripts/Bosses/BossJump.cs | BossJump : MonoBehaviour | class | A boss's jump with fake height, built from the player's jump pieces (JumpTimeline, JumpTuning, LandingResolver, DustPuffs). | M10 (1a79436) |
| Scripts/Bosses/SmashTelegraph.cs | SmashTelegraph | class | The landing ring of a Jump &amp; Smash: a flat ellipse on the floor at the landing spot that grows and pulses in the DANGER colour while the boss is in the air, flashes on impact and fades. | M10 (1a79436) |

## Scripts/Core

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Core/AudioService.cs | AudioService | class | The game's audio: a pool of pre-built audio sources for sound effects (with limits on identical sounds), two sources that crossfade the music, ducking for stingers and pause, and the volume settings routed to the mixer groups. | M4 (ced81c5) |
| Scripts/Core/Bootstrapper.cs | Bootstrapper : MonoBehaviour | class | The only object in the Boot scene: creates the persistent services, then loads the Main Menu. | M4 (ced81c5) |
| Scripts/Core/BuildInfo.cs | BuildInfo | class | What the running game knows about its own build. | D6 (3f643bc) |
| Scripts/Core/DemoConfig.cs | DemoConfig : ScriptableObject | class | The one switch behind the demo build (S1). With  on, the game hides what the demo must not show: Sentry enemies and traps / hazard zones before the first boss, and Character Creation's editing. | S1 (484781b) |
| Scripts/Core/FireTimer.cs | FireTimer | class | Frame-rate independent fire cadence. Time left over inside a frame carries into the next one, but an idle pause never stores up a burst. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Core/GameClock.cs | HitstopClock | class | The one owner of Time.timeScale during a run. | M8.6 (64b7eee) |
| Scripts/Core/GameClock.cs | GameClock | class | Static access to the run's HitstopClock, and the only place that writes Time.timeScale for it. | M8.6 (64b7eee) |
| Scripts/Core/GameConfig.cs | GameConfig : ScriptableObject | class | Global game settings and the start-of-run setup. | M4 (ced81c5) |
| Scripts/Core/GameSceneController.cs | GameSceneController : MonoBehaviour | class | MonoBehaviour in the Game scene that wires the run manager to the gameplay input reader and reacts to state changes. | M4 (ced81c5) |
| Scripts/Core/GameServices.cs | GameServices : MonoBehaviour | class | The persistent services (save system, settings, profile, run manager, scene loader, audio stub), created once on a DontDestroyOnLoad object. | M4 (ced81c5) |
| Scripts/Core/GameStateMachine.cs | GameState | enum | Enum of run states: None, RoundIntro, Combat, RoundResults, Shop, Armory, Pause, GameOver. | M4 (ced81c5) |
| Scripts/Core/GameStateMachine.cs | GameStateMachine | class | The single state machine for the run loop: RoundIntro -> Combat -> RoundResults -> Shop -> Armory -> RoundIntro (next round). | M4 (ced81c5) |
| Scripts/Core/Health.cs | Health : MonoBehaviour, IDamageable | class | Hit points for any damageable thing. Its owner sets the maximum from data via Initialize. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Core/IDamageable.cs | IDamageable | interface | Anything a projectile can hurt: enemies now, bosses and the player later. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Core/RunManager.cs | RunManager | class | Owns the current run: its RunState, the GameStateMachine, autosave and the round rewards. | M4 (ced81c5) |
| Scripts/Core/RunState.cs | RunState | class | Everything about the current run that must survive between rounds and be saved: round, currency, the 8-slot loadout of arm instances, both inventories and the 4 ammo slots. | M4 (ced81c5) |
| Scripts/Core/SceneLoader.cs | SceneLoader | class | Loads scenes by name. Kept behind one class so async loading and transitions can be added later. | M4 (ced81c5) |
| Scripts/Core/SortingLayers.cs | SortingLayers | class | The sorting layers of the 3/4 view, back to front. | M7.5 (ce21bf8) |
| Scripts/Core/StatusEffects.cs | StatusEffects : MonoBehaviour | class | Timed status effects on anything with Health (burn = damage over time, stun = can't move). | M3b (4b0398d) |

## Scripts/Cosmetics

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Cosmetics/CosmeticPartData.cs | CosmeticSlot | enum | The paper-doll slots, listed back to front. The order is the profile file's layout: append, never reorder. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/Cosmetics/CosmeticPartData.cs | CosmeticSlots | class | Static helper: slot count (5) and display labels for each CosmeticSlot. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/Cosmetics/CosmeticPartData.cs | CosmeticPartData : ScriptableObject | class | One variant of one paper-doll slot. Purely visual, never changes stats. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/Cosmetics/GladiatorCosmetics.cs | GladiatorCosmetics : MonoBehaviour | class | The gladiator paper doll: one SpriteRenderer + SpriteResolver per slot (Body, Armor, Head, Accessory 1, Accessory 2), all drawn on the same character template so they line up with no per-part offsets. | M6 (9864589) |
| Scripts/Cosmetics/GladiatorCosmetics.cs | Layer | struct | Serializable struct linking a CosmeticSlot to its SpriteRenderer and SpriteResolver. | M6 (9864589) |
| Scripts/Cosmetics/GladiatorPreviewMotion.cs | GladiatorPreviewMotion : MonoBehaviour | class | Gives the character creation preview the player's idle motion (breathing) from the toolkit, so the assembled doll is seen moving exactly as in the game. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/Cosmetics/ProfileData.cs | ProfileData | class | What the profile file stores: the chosen look (one cosmetic ID per slot). | M6 (9864589) |
| Scripts/Cosmetics/ProfileService.cs | ProfileService | class | The player's chosen look. Edits are live (the customization preview reads them) but only Save writes the profile file; Revert drops unsaved edits. | M6 (9864589) |

## Scripts/Enemies

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Enemies/AttackPattern.cs | AttackShape | enum | Enum of bullet pattern shapes: Aimed, Spread, Ring, Spiral (as read from its member docs). | M5a (295524f) |
| Scripts/Enemies/AttackPattern.cs | AttackPattern : ScriptableObject | class | One repeating enemy attack: its shape, timing and bullets. | M5a (295524f) |
| Scripts/Enemies/AttackPatternMath.cs | AttackPatternMath | class | Where the bullets of one volley go. Plain math (no Unity objects) so it can be unit tested and never allocates. | M5a (295524f) |
| Scripts/Enemies/CombatTuning.cs | CombatTuning : ScriptableObject | class | Tuning for where enemies spawn and how the wave messages behave. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/DifficultyCurve.cs | RoundDifficulty | struct | What a round multiplies: how many enemies, their health, how often they fire and how fast their bullets fly. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/DifficultyCurve.cs | DifficultyCurve : ScriptableObject | class | How hard each round is. Every stat is an AnimationCurve over the round number (x = round). | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/DifficultyCurve.cs | Axis | struct | Serializable struct: a per-round multiplier curve with growth beyond the last key and min/max clamp. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/Enemy.cs | Enemy : MonoBehaviour | class | A pooled enemy. The wave spawner takes one from the EnemyPool and calls Initialize with the enemy type and the round's difficulty; this binds the EnemyData to the shared Health / hit flash / health bar / patrol / attacker compo... | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Enemies/EnemyAttacker.cs | EnemyAttacker : MonoBehaviour | class | Fires an enemy's attack patterns. Each pattern keeps its own timer (and spiral turn). | M5a (295524f) |
| Scripts/Enemies/EnemyBrain.cs | EnemyBrain : MonoBehaviour | class | Runs an enemy's movement AI: picks the behaviour named by its EnemyData (Chaser, Skirmisher, Sentry, Charger, Sniper) and ticks it every frame. | M8 (6fa1892) |
| Scripts/Enemies/EnemyBulletPalette.cs | BulletStyle | enum | Which colour family an enemy bullet uses. | M9d (a3a8740) |
| Scripts/Enemies/EnemyBulletPalette.cs | EnemyBulletPalette : ScriptableObject | class | The colours of every enemy bullet, in one place (ART_SPEC section 7: enemy bullets pop against the floor, also in grayscale). | M9d (a3a8740) |
| Scripts/Enemies/EnemyBulletPalette.cs | Look | struct | Serializable struct: core, body and outline colours of one bullet family. | M9d (a3a8740) |
| Scripts/Enemies/EnemyData.cs | EnemyBehavior | enum | How an enemy moves and picks its moments to attack. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Enemies/EnemyData.cs | EnemyData : ScriptableObject | class | Tunable data for one enemy type. Real enemies (M4) extend this; movement fields are optional. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Enemies/EnemyPool.cs | EnemyPool : MonoBehaviour | class | Pool for every enemy. Warns when it has to grow past its prewarmed size. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/HealthBar.cs | HealthBar : MonoBehaviour | class | World-space health bar (two sprites under "visuals"). | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Enemies/IDeathSequence.cs | IDeathSequence | interface | An optional component on an enemy that plays its own death (a boss). | M10 (1a79436) |
| Scripts/Enemies/PatrolMover.cs | PatrolMover : MonoBehaviour | class | Moves a kinematic body back and forth along an axis at constant speed. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Enemies/RoundData.cs | RoundData : ScriptableObject | class | A round's combat: its waves in order, and whether it is a boss round (a tougher placeholder until M7). | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/SpawnPlacement.cs | SpawnPlacement | class | Where a spawned enemy goes. Plain math so it can be tested; the arena is the playable rectangle inset by the edge margin. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/SpawnScheduler.cs | SpawnRequest | struct | One enemy that is due to appear. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/SpawnScheduler.cs | SpawnScheduler | class | Turns a wave into a timed list of spawns. Group sizes are scaled by the round's count multiplier (rounded, never below one enemy per group). | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/SpawnScheduler.cs | Entry | struct | Private struct: a spawn request with its scheduled time. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/WaveData.cs | SpawnPattern | enum | Where a group of enemies appears. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/WaveData.cs | SpawnGroup | struct | A batch of one enemy type inside a wave: how many, where, and how fast they arrive. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/WaveData.cs | WaveData : ScriptableObject | class | One wave of a round. It is cleared when every enemy in it has appeared and been killed. | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/WaveSpawner.cs | WaveSpawner : MonoBehaviour | class | Runs a round's combat. At the start of each round it looks up the round's waves and the difficulty for that round number, then plays the waves in order: enemies appear on their schedule, a wave is cleared when all of its enemie... | M5b (commit "M5") (0e117c7) |
| Scripts/Enemies/WaveSpawner.cs | Phase | enum | Private enum: Idle, Fighting, Breather. | M5b (commit "M5") (0e117c7) |

## Scripts/Feedback

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Feedback/CameraShake.cs | CameraShake : MonoBehaviour | class | Trauma-based screen shake for the fixed orthographic camera. | M8.6 (64b7eee) |
| Scripts/Feedback/FeedbackHub.cs | FeedbackHub : MonoBehaviour | class | Runtime home of the pooled feedback effects: particle presets (a fixed ring of ParticleSystems per preset, the oldest is recycled, so nothing is allocated or grown during play) and the death ghosts (a fixed ring of sprite copie... | M8.6 (64b7eee) |
| Scripts/Feedback/FeedbackHub.cs | Ghost | class | Private pooled record of a dying-enemy visual clone (transform, renderer, fx, timers) animated after the enemy returns to its pool. | M8.6 (64b7eee) |
| Scripts/Feedback/FeedbackTuning.cs | VfxKind | enum | Enum of particle preset kinds: Spark, Dust, Smoke, Steam, Debris, Coin, Confetti, MuzzleFlash, SpawnPuff. | M8.6 (64b7eee) |
| Scripts/Feedback/FeedbackTuning.cs | FeedbackTuning : ScriptableObject | class | Global feedback settings and the default tunings for the player and enemies. | M8.6 (64b7eee) |
| Scripts/Feedback/HitFeedback.cs | IHitReceiver | interface | Something that wants to know which way a hit was travelling (set just before the damage is applied). | M8.6 (64b7eee) |
| Scripts/Feedback/HitFeedback.cs | HitFeedback : MonoBehaviour, IHitReceiver | class | Reaction to taking damage, for the player and every enemy: white flash, and for direct bullet hits also a scale punch, a visual-only knockback nudge, sparks, a short hitstop and a camera shake (values in HitFeedbackTuning). | M8.6 (64b7eee) |
| Scripts/Feedback/HitFeedbackTuning.cs | HitFeedbackTuning : ScriptableObject | class | What happens when something takes a hit: white flash, knockback nudge, scale punch, hitstop, camera shake, sparks. | M8.6 (64b7eee) |
| Scripts/Feedback/LifeCycleTuning.cs | LifeCycleTuning : ScriptableObject | class | Spawn pop-in and the squash / flash / dissolve death. | M8.6 (64b7eee) |
| Scripts/Feedback/MotionTuning.cs | MotionTuning : ScriptableObject | class | How a character moves procedurally: breathing, hop-walk bob and tilt, lean, squash/stretch, facing flip, windup. | M8.6 (64b7eee) |
| Scripts/Feedback/PickupGlow.cs | PickupGlow : MonoBehaviour | class | A soft glow under a coin or an ammo pickup, so loot reads on the busy floor: a procedural placeholder sprite (from FeedbackTuning) tinted in the VoxKit palette, slowly pulsing. | S1 (484781b) |
| Scripts/Feedback/PlayerFeedbackBinder.cs | PlayerFeedbackBinder : MonoBehaviour | class | Gives the player's motion and hit-feedback components their tuning from the FeedbackTuning in GameConfig (enemies get theirs in Enemy.Initialize). | M8.6 (64b7eee) |
| Scripts/Feedback/ProceduralMotion.cs | ProceduralMotion : MonoBehaviour | class | The single writer of a character's Motion transform (the child that holds its body sprites): idle breathing, hop-walk bob and tilt, lean into movement, squash/stretch on start/stop, facing flip, plus the one-shot layers other f... | M8.6 (64b7eee) |
| Scripts/Feedback/RigTestAnim.cs | RigTestAnim : MonoBehaviour | class | Demo animation for the rigged test character (Assets/Scenes/RigTest.unity): finds the skeleton's bones by name and drives them procedurally: an idle sway and breathing, or a walk cycle with swinging arms and legs. | M8.6 (64b7eee) |
| Scripts/Feedback/Spring.cs | Spring | struct | Damped spring toward zero (semi-implicit Euler, sub-stepped so a big dt can't blow it up). | M8.6 (64b7eee) |
| Scripts/Feedback/Spring.cs | Spring2 | struct | Two springs for a 2D offset. | M8.6 (64b7eee) |
| Scripts/Feedback/SpriteFx.cs | SpriteFx : MonoBehaviour | class | The one channel for shader feedback on a character's sprites: white hit flash, DANGER wind-up pulse, a tint overlay (e.g. | M8.6 (64b7eee) |
| Scripts/Feedback/TelegraphFx.cs | TelegraphFx : MonoBehaviour | class | Makes enemy attacks readable: while an attack winds up the enemy inflates, trembles and pulses the reserved DANGER colour (shader), growing stronger as the attack gets closer. | M8.6 (64b7eee) |

## Scripts/Input

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Input/AimThresholds.cs | AimThresholds | class | The InputTuning thresholds scaled by the player's aim sensitivity setting, within safe limits. | M6 (9864589) |
| Scripts/Input/ControlLabels.cs | ControlLabels | class | What a gamepad control is called on each controller family ("rightShoulder" is R1 on PlayStation, RB on Xbox, R on Switch). | D5 (1835475) |
| Scripts/Input/GameInput.cs | GameplayActions | struct | Provides access to input actions defined in input action map "Gameplay". | M0 (480dc6a) |
| Scripts/Input/GameInput.cs | DebugActions | struct | Provides access to input actions defined in input action map "Debug". | M0 (480dc6a) |
| Scripts/Input/GameInput.cs | MenuActions | struct | Provides access to input actions defined in input action map "Menu". | M0 (480dc6a) |
| Scripts/Input/GameInput.cs | IGameplayActions | interface | Interface to implement callback methods for all input action callbacks associated with input actions defined by "Gameplay" which allows adding and removing callbacks. | M0 (480dc6a) |
| Scripts/Input/GameInput.cs | IDebugActions | interface | Interface to implement callback methods for all input action callbacks associated with input actions defined by "Debug" which allows adding and removing callbacks. | M0 (480dc6a) |
| Scripts/Input/GameInput.cs | IMenuActions | interface | Interface to implement callback methods for all input action callbacks associated with input actions defined by "Menu" which allows adding and removing callbacks. | M0 (480dc6a) |
| Scripts/Input/GameplayInputReader.cs | GameplayInputReader : MonoBehaviour | class | The single owner of the generated GameInput class. | M1 (WIP commit) (5b356f7) |
| Scripts/Input/InputBindingService.cs | RebindAction | enum | The gameplay buttons the player can remap (sticks stay where they are). | D5 (1835475) |
| Scripts/Input/InputBindingService.cs | RebindOutcome | enum | Enum: Changed, Swapped, Cancelled. | D5 (1835475) |
| Scripts/Input/InputBindingService.cs | InputBindingService : IDisposable | class | Button remapping with the Input System's rebinding API. | D5 (1835475) |
| Scripts/Input/InputDiagnostics.cs | InputDiagnostics | class | Diagnostics for the input readers; it never changes what input does. | M9d (a3a8740) |
| Scripts/Input/InputTuning.cs | InputTuning : ScriptableObject | class | Tuning for left-stick arm selection. Defaults match CLAUDE.md. | M1 (WIP commit) (5b356f7) |
| Scripts/Input/MenuInputReader.cs | MenuInputReader : MonoBehaviour | class | Owner of the "Menu" input map: the controller's Start / Options button as the menu's primary action, plus the extra face/shoulder buttons some screens use. | M5a (295524f) |

## Scripts/Perf

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Perf/PerfArgs.cs | PerfArgs | class | Command-line switches of the performance tools (development builds and the editor only; release builds see none). | P1 partial (1a79436) |
| Scripts/Perf/PerfHost.cs | PerfHost : MonoBehaviour | class | Owner of the performance tools: one persistent object in development builds and the editor (never in release builds). | P1 partial (1a79436) |
| Scripts/Perf/PerfLogger.cs | PerfLogger : IDisposable | class | Records one row per frame while running: real frame time, main and render thread CPU time, garbage allocated in the frame, batches / set-pass / draw calls, the milliseconds spent in each BulletHell profiler marker, and the obje... | P1 partial (1a79436) |
| Scripts/Perf/PerfMarkers.cs | PerfMarkers | class | Profiler markers around the main gameplay systems. | P1 partial (1a79436) |
| Scripts/Perf/PerfOverlay.cs | PerfOverlay : MonoBehaviour | class | The performance overlay: FPS, frame time, p99, max, garbage per frame, object counts, and a frame-time graph (the last 128 frames, green up to 60 fps, yellow to 50, red beyond, guide lines at 60 and 30 fps). | P1 partial (1a79436) |
| Scripts/Perf/PerfStats.cs | PerfStats : IDisposable | class | Live numbers for the performance overlay: frame time (a ring of the last 256 frames), average, p99, max, fps, garbage allocated per frame and the object counts (bullets, enemies, particles, pools). | P1 partial (1a79436) |
| Scripts/Perf/StressTest.cs | StressTest : MonoBehaviour | class | A fixed, seeded stress script for comparing builds: from the start of Combat it runs the player on a scripted figure-8 with all eight arms firing at once (Homing, Ricochet and Pierce equipped as far as the slots allow), keeps a... | P1 partial (1a79436) |
| Scripts/Perf/StressTest.cs | Stage | enum | Private enum: WaitingForCombat, Running, Finished. | P1 partial (1a79436) |

## Scripts/Pickups

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Pickups/AmmoPickup.cs | AmmoPickup : MonoBehaviour | class | An ammo type lying in the world. Registers itself in a static list so the collector can find the nearest one without FindObjectOfType. | M3a (578c096) |
| Scripts/Pickups/AmmoPickupCollector.cs | AmmoPickupCollector : MonoBehaviour | class | On the player. Walking over a pickup fills the first empty ammo slot. | M3a (578c096) |
| Scripts/Pickups/CoinField.cs | CoinField : MonoBehaviour | class | All the currency coins of the round. Enemies drop coins here; coins near the player are pulled in by a magnet and collected, adding to the round's earnings. | M5b (commit "M5") (0e117c7) |
| Scripts/Pickups/CoinPickup.cs | CoinPickup : MonoBehaviour | class | A currency coin's state. The CoinField moves and collects every coin from one loop, so this has no Update of its own. | M5b (commit "M5") (0e117c7) |
| Scripts/Pickups/CoinTuning.cs | CoinTuning : ScriptableObject | class | Tuning for currency coins: how they pop out, the magnet that pulls them to the player, and their pool. | M5b (commit "M5") (0e117c7) |
| Scripts/Pickups/PickupTuning.cs | PickupTuning : ScriptableObject | class | Tuning for ammo pickups: pickup ranges, hold-to-replace time and dropped-ammo behaviour. | M3a (578c096) |

## Scripts/Platform

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Platform/GlyphFamily.cs | GlyphFamily | enum | Which set of button pictures to show: the family of the device in the player's hands. | M7.5 (ce21bf8) |
| Scripts/Platform/GlyphFamily.cs | GlyphFamilyDetector | class | Works out the glyph family of the active device. | M7.5 (ce21bf8) |
| Scripts/Platform/GlyphFamily.cs | InputDeviceWatcher | class | Tracks which kind of device the player pressed something on last: a gamepad (its family) or the keyboard / mouse. | M7.5 (ce21bf8) |
| Scripts/Platform/PlatformCapabilities.cs | PlatformCapabilities | class | The only place that asks "which platform is this". | M4 (ced81c5) |

## Scripts/Player

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Player/ArmRingMath.cs | ArmRingMath | class | Pure maths of the arm ring. Angles are compass degrees like ArmSelector: 0 = N (+Y, the back of the ring on screen), 90 = E, 180 = S (the front), increasing clockwise. | M7.7 (2ba5edb) |
| Scripts/Player/ArmRingTuning.cs | ArmRingTuning : ScriptableObject | class | The arm ring: the 8 arm slots sit on a flattened ellipse around the player's FEET (3/4 view), like a ring spinning around the base. | M7.7 (2ba5edb) |
| Scripts/Player/ArmSelectionController.cs | ArmSelectionController : MonoBehaviour | class | Spawns the arms from the run state's loadout, feeds the left stick into ArmSelector, handles L3 lock, and drives the arm visuals. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/ArmSelector.cs | ArmSelectionState | enum | Enum: None, Soft, Locked. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/ArmSelector.cs | ArmSelector | class | Pure selection logic for the 8 weapon arm slots. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/ArmVisual.cs | ArmVisual : MonoBehaviour | class | Look of one spawned arm: hidden, soft-selected (thin dim outline) or locked (thick solid pulsing outline), the outline (shader) in the arm's ID colour; widths and alphas live in ArmRingTuning. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/ArmVisual.cs | State | enum | Public enum of an arm's display state: Hidden, Selected, Locked. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/DustPuffs.cs | DustPuffs | class | A fixed set of dust puff sprites, created once and reused (no allocations while playing). | M7.6 (767e3ca) |
| Scripts/Player/JumpController.cs | JumpController : MonoBehaviour | class | R2 / Space jump with FAKE height. The player root never leaves the ground plane; only the Visuals child rises on the height curve, the ground shadow shrinks and fades, and the body squashes on takeoff and landing. | M7.6 (767e3ca) |
| Scripts/Player/JumpTimeline.cs | JumpTimeline | class | The timing of a jump, without any engine objects: ground, airborne for the airtime, cooldown, ground. | M7.6 (767e3ca) |
| Scripts/Player/JumpTuning.cs | JumpTuning : ScriptableObject | class | Every number of the jump. The jump is fake height: the player's root stays on the ground plane and only the visuals rise, so all of this is looks plus the few rules that change while airborne. | M7.6 (767e3ca) |
| Scripts/Player/LandingResolver.cs | LandingResolver | class | Finds the spot a landing player is moved to when the landing spot is taken. | M7.6 (767e3ca) |
| Scripts/Player/PlayerData.cs | PlayerData : ScriptableObject | class | ScriptableObject with the player tunables: move speed, body radius, health, hitbox core, i-frames and similar values. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/PlayerHealth.cs | PlayerHealth : MonoBehaviour | class | The player's health and the target enemy bullets test against. | M5a (295524f) |
| Scripts/Player/PlayerInventory.cs | PlayerInventory : MonoBehaviour | class | Player-side access to the run's inventories (armaments and spare arms that aren't equipped). | M3b (4b0398d) |
| Scripts/Player/PlayerMover.cs | PlayerMover : MonoBehaviour | class | Moves the player with the right stick. Inside an arena the body (a circle) slides along the walls and obstacles through the arena grid; with no arena built it stays inside the camera view like before. | M1 (WIP commit) (5b356f7) |
| Scripts/Player/PlayerVisualRig.cs | PlayerVisualRig : MonoBehaviour | class | The player's layered structure. The root sits on the ground at the FEET and is what moves, collides (footprint), sorts and stands on traps. | M7.5 (ce21bf8) |

## Scripts/Projectiles

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Projectiles/BulletPathDebug.cs | BulletPathDebug | class | Debug visualisation of bullet paths, toggled with the debug key. | M9a (ee19099) |
| Scripts/Projectiles/BulletPathDebug.cs | MarkerKind | enum | Enum of debug path markers: Pierce, Ricochet, Stop. | M9a (ee19099) |
| Scripts/Projectiles/BulletPathDebug.cs | BulletPathView : MonoBehaviour | class | Private MonoBehaviour that draws the recorded bullet path lines with GL for the debug view. | M9a (ee19099) |
| Scripts/Projectiles/Projectile.cs | Projectile : MonoBehaviour | class | A pooled bullet. Moves in a straight line and sweeps a circle along each step, so fast bullets can't tunnel through targets. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Projectiles/ProjectilePool.cs | ProjectilePool : MonoBehaviour | class | Pool for every projectile (player and, later, enemy). | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Projectiles/ProjectileRules.cs | HitOutcome | enum | What a bullet does after touching something. | M9a (ee19099) |
| Scripts/Projectiles/ProjectileRules.cs | ShotState | struct | The counters that decide what a bullet survives. | M9a (ee19099) |
| Scripts/Projectiles/ProjectileRules.cs | ProjectileRules | class | The armament interaction rules as plain functions (Docs/ARMAMENTS.md), so they are testable without physics: - Pierce is used up before a bullet stops: an enemy hit with pierce left keeps the bullet flying. | M9a (ee19099) |

## Scripts/Save

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Save/AssetRegistry.cs | AssetRegistry : ScriptableObject | class | Maps the stable string IDs stored in saves to the WeaponArmData / ArmamentData / AmmoTypeData assets and back. | M4 (ced81c5) |
| Scripts/Save/ISaveSystem.cs | ISaveSystem | interface | Single-slot save storage. The local file version lives in Scripts/Save; platform save APIs (Steam Cloud, iCloud, console save data) plug in later as other implementations, under Scripts/Platform. | M4 (ced81c5) |
| Scripts/Save/JsonFileStore.cs | JsonFileStore | class | One JSON file holding one plain serializable object (the settings file, the profile file). | M6 (9864589) |
| Scripts/Save/LocalFileSaveSystem.cs | LocalFileSaveSystem : ISaveSystem | class | The one save slot as a JSON file. Writes go to a temp file first and are then swapped in, so a crash mid-write leaves the previous save intact. | M4 (ced81c5) |
| Scripts/Save/RunSaveMapper.cs | RunSaveMapper | class | Converts between the live RunState and the plain SaveData, going through the AssetRegistry for IDs. | M4 (ced81c5) |
| Scripts/Save/SaveData.cs | SaveData | class | What is written to the save file. Plain serializable classes and asset IDs only, never ScriptableObject references (the AssetRegistry maps IDs back to assets). | M4 (ced81c5) |
| Scripts/Save/SaveData.cs | ArmSave | class | One arm instance: its arm type and the armaments in its 3 slots. | M4 (ced81c5) |

## Scripts/Settings

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Settings/SettingsData.cs | SettingsData | class | What the settings file stores. Plain data; defaults and limits live on SettingsDefaults. | M6 (9864589) |
| Scripts/Settings/SettingsDefaults.cs | SettingsDefaults : ScriptableObject | class | Default values, steps and safe limits for the player-facing settings. | M6 (9864589) |
| Scripts/Settings/SettingsService.cs | SettingsService | class | The player-facing settings: loaded from the settings file at startup, applied immediately whenever something changes (Commit), and written back with Save (the settings screen saves when it closes, and the app saves on quit/pause). | M6 (9864589) |

## Scripts/Shop

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Shop/RarityTable.cs | RarityEntry | struct | How one rarity behaves in the Shop. | M9b (5ef5e62) |
| Scripts/Shop/RarityTable.cs | RarityTable : ScriptableObject | class | Weights (by rarity and round), prices (by rarity, price tier and round) and frame colours for Shop stock. | M9b (5ef5e62) |
| Scripts/Shop/ShopCard.cs | ShopCard : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler | class | One card in the Shop (also used for the crate choices): icon, name, rarity frame and price tag. | M9b (5ef5e62) |
| Scripts/Shop/ShopDescriber.cs | ShopDescriber | class | The text of a Shop card tooltip, built from the item data (nothing here is written by hand): the M9a generated description, tags, stack limit, which of the player's arms it fits and the stats before and after. | M9b (5ef5e62) |
| Scripts/Shop/ShopFit.cs | ArmFit | struct | How an armament would sit on one of the player's arms. | M9b (5ef5e62) |
| Scripts/Shop/ShopFit.cs | ShopFit | class | Which of the player's arms an armament fits, and the stats before and after. | M9b (5ef5e62) |
| Scripts/Shop/ShopPool.cs | ShopItemKind | enum | Enum: Arm, Armament, Crate. | M4 (ced81c5) |
| Scripts/Shop/ShopPool.cs | ShopEntry | struct | One thing for sale: an arm, an armament or the crate, and its price. | M4 (ced81c5) |
| Scripts/Shop/ShopPool.cs | ShopPool : ScriptableObject | class | The candidates the Shop draws its random stock from (arms and armaments). | M4 (ced81c5) |
| Scripts/Shop/ShopScreen.cs | ShopScreen : MonoBehaviour | class | The Shop: a merchant and a stall with cards (2 arms on top, 3 armaments below, a crate that opens a pick-1-of-3 armament choice), currency top-right, Reroll and Leave. | M4 (ced81c5) |
| Scripts/Shop/ShopService.cs | PurchaseResult | enum | Enum: Bought, NotEnoughCurrency, InvalidEntry, AlreadySold. | M4 (ced81c5) |
| Scripts/Shop/ShopService.cs | ShopService | class | Buying: spends currency and puts the item in the arm or armament inventory. | M4 (ced81c5) |
| Scripts/Shop/ShopStock.cs | ShopStock | class | The stock of one Shop visit: arm offers, armament offers and the crate. | M9b (5ef5e62) |
| Scripts/Shop/ShopTooltip.cs | ShopTooltip : MonoBehaviour | class | The tooltip next to the focused Shop card: a themed panel (UITheme Tooltip role) with the item text. | M9b (5ef5e62) |
| Scripts/Shop/ShopTuning.cs | ShopTuning : ScriptableObject | class | The Shop's layout counts, crate, reroll and debug numbers. | M9b (5ef5e62) |
| Scripts/Shop/ShopVisit.cs | ShopVisit | class | One visit to the Shop: which stock the player sees and what has been bought. | M9b (5ef5e62) |

## Scripts/Telemetry

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Telemetry/RunRecord.cs | RunEndReason | enum | How a logged run ended. | D3 (59b33c7) |
| Scripts/Telemetry/RunRecord.cs | RunRecord | class | One finished run, as written to a row of the playtest CSV. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryCsv.cs | TelemetryCsv | class | The playtest CSV: one header line, then one row per finished run. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryEvents.cs | SpendKind | enum | Enum of currency spend types: Item, Reroll, Crate. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryEvents.cs | ItemKind | enum | Enum: Arm, Armament. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryEvents.cs | TelemetryEvents | class | What the playtest telemetry listens to. The game raises these where things happen (a hit on the player, a purchase); the turns them into the run's numbers. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryOverlay.cs | TelemetryOverlay : MonoBehaviour | class | Debug summary screen for the playtest log: averages and distributions over every logged run (round reached, combat time, damage, currency, purchases, cause of death, boss phase, time and damage per round). | D3 (59b33c7) |
| Scripts/Telemetry/TelemetryService.cs | TelemetryService : IDisposable | class | Local-only playtest telemetry. Watches the run (rounds, combat time, damage, shop spending, boss phases) and, when a run ends (the player dies, or leaves it / closes the game), appends one row to a CSV in the save folder. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetrySummary.cs | Stat | struct | Count, mean, median and range of one number across runs. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetrySummary.cs | RoundAverage | struct | Average time and damage of one round across the runs that played it. | D3 (59b33c7) |
| Scripts/Telemetry/TelemetrySummary.cs | TelemetrySummary | class | Averages and distributions over the logged runs, for the debug summary screen. | D3 (59b33c7) |

## Scripts/UI

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/UI/BossHealthBar.cs | BossHealthBar : MonoBehaviour | class | Screen-space boss bar at the top of the combat HUD: name plate, fill, a slower trail behind the fill, and a tick at the phase 2 threshold that pops when the phase changes. | M10 (1a79436) |
| Scripts/UI/ButtonFocusFx.cs | ButtonFocusFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, | class | The VoxKit focus treatment for every themed button, on controller focus and on mouse hover: a gold ring around the button and a lift of the theme's focus height (12 px @1080p, 0.12 s ease-out), and a small press-down while pres... | L1 (1a1a93e) |
| Scripts/UI/ButtonGlyphLibrary.cs | GlyphButton | enum | The gamepad buttons the UI shows prompts for (by position, so they stay right on every controller family). | M7.5 (ce21bf8) |
| Scripts/UI/ButtonGlyphLibrary.cs | ButtonGlyph | struct | One button picture: an icon, and an optional letter drawn over it (for sets whose icons are blank discs). | M7.5 (ce21bf8) |
| Scripts/UI/ButtonGlyphLibrary.cs | ButtonGlyphLibrary : ScriptableObject | class | Button prompts for every controller family (PlayStation, Xbox, Nintendo, Touch). | M7.5 (ce21bf8) |
| Scripts/UI/ButtonGlyphLibrary.cs | FamilySet | class | Serializable per-device-family set of face-button glyphs and prompt label texts per UiAction. | M7.5 (ce21bf8) |
| Scripts/UI/CancelRelay.cs | CancelRelay : MonoBehaviour, ICancelHandler | class | Turns the UI's Cancel action (B / Circle on a gamepad, Esc on a keyboard) into an event. | M6 (9864589) |
| Scripts/UI/CombatHud.cs | CombatHud : MonoBehaviour | class | The combat HUD, drawn over the crowd and railing so it never covers play space. | M7.5 (ce21bf8) |
| Scripts/UI/ConfirmDialog.cs | ConfirmDialog : MonoBehaviour | class | The shared yes / no dialog for destructive actions (start over a save, quit a run, quit the game). | M9d (a3a8740) |
| Scripts/UI/CustomizationScreen.cs | CustomizationScreen : MonoBehaviour | class | Character Creation: one row per paper-doll slot (left/right cycles its variants), a live preview of the gladiator, Randomize (Square) and "To the Arena!" (Confirm). | M6 (9864589) |
| Scripts/UI/DebugArmamentControls.cs | DebugArmamentControls : MonoBehaviour | class | Debug-only: give yourself ANY armament and equip it on the selected arm during play. | M3b (4b0398d) |
| Scripts/UI/DebugOverlay.cs | DebugOverlay : MonoBehaviour | class | On-screen readout of move stick, aim magnitude/angle, selected arm (slot and name), selection state and arm aim. | M1 (WIP commit) (5b356f7) |
| Scripts/UI/DebugOverlayToggle.cs | DebugOverlayToggle : MonoBehaviour | class | Shows or hides the debug objects (the overlay) from the "Show debug overlay" setting. | M6 (9864589) |
| Scripts/UI/DebugRoundPicker.cs | DebugRoundPicker : MonoBehaviour | class | Debug row on the Pause screen: pick a round with - / + and press Go to restart combat there, Boss to jump to the first boss round, and HP- / HP+ to set the living boss's health in steps of 10%. | M5b (commit "M5") (0e117c7) |
| Scripts/UI/FlowPanel.cs | FlowPanel : MonoBehaviour | class | One full-screen between-rounds screen (Round Results, Pause, Game Over): a title, some text, a Continue button and a Menu button. | M4 (ced81c5) |
| Scripts/UI/FocusDecor.cs | FocusDecor : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler | class | Focus decoration for a Selectable: shows its decor objects (checker trims at the ends of a button, laurels beside it, a gold focus ring) while it is selected or hovered, and lifts it by the theme's focus lift. | UI2 (799d55b) |
| Scripts/UI/GameFlowUI.cs | GameFlowUI : MonoBehaviour | class | Shows the screen for the current run state (Round Results, Shop, Armory) and hides them during Combat. | M4 (ced81c5) |
| Scripts/UI/HudAmmoSlot.cs | HudAmmoSlot : MonoBehaviour | class | One ammo slot: icon, face-button glyph, active highlight, empty frame, and the hold-to-replace ring. | M7.5 (ce21bf8) |
| Scripts/UI/HudHearts.cs | HudHearts : MonoBehaviour | class | One heart per hit point (1 heart = 1 HP). Only redraws when the health changes. | M7.5 (ce21bf8) |
| Scripts/UI/HudHeatBar.cs | HudHeatBar : MonoBehaviour | class | Heat of the selected arm. Fills while firing heat ammo, tints leaf -> carrot -> tomato as it rises (UITheme heat colors), flashes tomato and marble when overheated, drains as it cools. | M7.5 (ce21bf8) |
| Scripts/UI/HudPortrait.cs | HudPortrait : MonoBehaviour | class | The gladiator's portrait. With the customizer parts on (GameConfig.ShowCustomizationInGame) it is built from the same parts as the player: the chosen head and Accessory 1 over the portrait frame. | M7.5 (ce21bf8) |
| Scripts/UI/HudScale.cs | HudScale : MonoBehaviour | class | Scales a piece of the combat HUD by Settings > Gameplay > HUD Scale. | D5 (1835475) |
| Scripts/UI/MainMenuController.cs | MainMenuController : MonoBehaviour | class | Main Menu: New Game, Continue (disabled without a run save), Settings and Quit. | M4 (ced81c5) |
| Scripts/UI/MenuPrimaryRouter.cs | MenuPrimaryRouter : MonoBehaviour | class | Makes the controller's Start / Options button press the main button of whichever screen is showing. | M5a (295524f) |
| Scripts/UI/MenuPrimaryRouter.cs | Route | struct | Serializable struct: a scope object paired with the button that Start presses while that scope is active. | M5a (295524f) |
| Scripts/UI/MenuRow.cs | MenuRow : MonoBehaviour, ICancelHandler, ISelectHandler, IDeselectHandler | class | One selectable row of a skeleton menu list. Submit (A / Cross) runs the select action; Cancel (B / Circle) runs the cancel action while this row is focused. | M4 (ced81c5) |
| Scripts/UI/PromptHint.cs | PromptHint : MonoBehaviour | class | A line of button prompts ("[Cross] Buy   [Triangle] Reroll") that follows the device in use: it is rebuilt from the ButtonGlyphLibrary whenever the player switches between PlayStation / Xbox / Nintendo / keyboard. | M9d (a3a8740) |
| Scripts/UI/PromptHint.cs | Prompt | struct | Readonly struct pairing a UiAction with its label for the prompt bar. | M9d (a3a8740) |
| Scripts/UI/RoundIntroBanner.cs | RoundIntroBanner : MonoBehaviour | class | The announcer moment that opens every round: a "Round N" banner (a special one for boss rounds), a 3-2-1 countdown, then "Begin!" as combat starts. | M6 (9864589) |
| Scripts/UI/RoundIntroBanner.cs | Phase | enum | Private enum: Idle, Title, Countdown, Begin. | M6 (9864589) |
| Scripts/UI/RunHud.cs | RunHud : MonoBehaviour | class | The combat HUD's top readouts: the round / wave pill (top center) and the currency pill (top right). | M4 (ced81c5) |
| Scripts/UI/SafeAreaFitter.cs | SafeAreaFitter : MonoBehaviour | class | Fits a full-screen RectTransform to Screen.safeArea (phone notches, rounded corners), intersected with the letterboxed arena viewport so the HUD always sits inside the 16:9 picture and never over the bars. | M1 (WIP commit) (5b356f7) |
| Scripts/UI/ScreenTransition.cs | ScreenTransition : MonoBehaviour | class | The one transition every screen uses when it appears: a short fade plus a small upward slide (PrimeTween, unscaled time because the game is often paused). | M9d (a3a8740) |
| Scripts/UI/ScrollFollowSelection.cs | ScrollFollowSelection : MonoBehaviour | class | Scrolls a ScrollRect so the controller-selected row stays visible (the EventSystem moves selection but never scrolls). | M4 (ced81c5) |
| Scripts/UI/SettingRow.cs | SettingKind | enum | What a settings row looks like and how it reads its value. | M6 (9864589) |
| Scripts/UI/SettingRow.cs | SettingRow : MonoBehaviour, IMoveHandler, ICancelHandler, ISelectHandler, IDeselectHandler, IPointerEnterHandler, | class | One row of the Settings screen (and of the Character Creation part list): a label and a value. | M6 (9864589) |
| Scripts/UI/SettingsScreen.cs | SettingsScreen : MonoBehaviour | class | The Settings screen: master / music / SFX volume, screen shake, controller vibration, aim sensitivity, debug overlay, and on PC fullscreen + resolution, spread over tabs (Audio, Video, Controls, Gameplay; L1 / R1 switch). | M6 (9864589) |
| Scripts/UI/SettingsScreen.cs | Entry | class | Private record: tab index, row GameObject and SettingRow for one settings list entry. | M6 (9864589) |
| Scripts/UI/ThemedButton.cs | ButtonKind | enum | Which kit button a ThemedButton wears. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/ThemedButton.cs | ThemedButton : MonoBehaviour | class | Themes a Button: sprite swap (normal / focused / pressed / disabled) from UITheme, plus the font and color of every TMP_Text under it. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/ThemedImage.cs | ThemedImage : MonoBehaviour | class | Puts a UITheme role's sprite and color on the Image next to it (9-sliced, borders drawn at the theme's scale). | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/ThemedText.cs | TextFont | enum | Which theme font a label uses. | UI2 (799d55b) |
| Scripts/UI/ThemedText.cs | TextTone | enum | Which theme color a label uses. Custom leaves the color already on the label. | UI2 (799d55b) |
| Scripts/UI/ThemedText.cs | ThemedText : MonoBehaviour | class | Themes a TextMeshPro label: font asset and color from UITheme. | UI2 (799d55b) |
| Scripts/UI/ThemedTrim.cs | ThemedTrim : MonoBehaviour | class | A checker trim strip: the kit's tileable checker drawn tiled on the Image next to it (panel tops, pills, button ends). | UI2 (799d55b) |
| Scripts/UI/TutorialController.cs | TutorialController : MonoBehaviour | class | The round 1 onboarding. On the first combat of a profile that has not finished it, it walks the player through the controls one at a time (move, select an arm, fire, lock, jump, swap ammo). | D2 (4368fd4) |
| Scripts/UI/TutorialData.cs | TutorialStepKind | enum | What the player has to do to finish a tutorial step. | D2 (4368fd4) |
| Scripts/UI/TutorialData.cs | TutorialData : ScriptableObject | class | The round 1 onboarding: which steps to show, in which order, with what text, and the timings. | D2 (4368fd4) |
| Scripts/UI/TutorialData.cs | Step | class | Serializable tutorial lesson: kind, title, text, control keys, waiting text. | D2 (4368fd4) |
| Scripts/UI/TutorialPanel.cs | TutorialPanel : MonoBehaviour | class | The card the round 1 onboarding talks through: a small "LESSON 2 OF 6" line, the instruction, progress pips and the skip prompt. | D2 (4368fd4) |
| Scripts/UI/TutorialText.cs | TutorialText | class | Turns a tutorial template ("Push {0} to move") into the text for the device in use. | D2 (4368fd4) |
| Scripts/UI/UIFocusGuard.cs | UIFocusGuard : MonoBehaviour | class | Keeps a menu controller-navigable: if nothing is selected (a mouse click on empty space, the selected button got disabled) the last valid selection is restored, so the stick or D-pad always has somewhere to start. | M4 (ced81c5) |
| Scripts/UI/UIList.cs | UIList : MonoBehaviour | class | A vertical list of MenuRows under one container. | M4 (ced81c5) |
| Scripts/UI/UITheme.cs | ThemeRole | enum | Where a themed image gets its sprite from. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/UITheme.cs | TrimColor | enum | The four checker trim colors of the kit. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/UITheme.cs | UITheme : ScriptableObject | class | The one and only UI theme: VOX VEGETALLIS (Garden Colosseum). | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/UITheme.cs | ButtonSprites | struct | Serializable struct of button state sprites (normal, highlighted, selected, pressed, disabled). | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/UITheme.cs | Palette | struct | Serializable struct of the theme colour tokens (ink soil, marble, gold, tomato, leaf, carrot, ...). | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Scripts/UI/UiAction.cs | UiAction | enum | What a prompt tells the player to press, by meaning rather than by button (the glyph library maps it per device). | M9d (a3a8740) |
| Scripts/UI/UiSound.cs | UiSoundKind | enum | The UI events that have a sound hook. Clips are assigned on the UITheme; unassigned hooks are silent. | M9d (a3a8740) |
| Scripts/UI/UiSound.cs | UiSound | class | Plays UI sounds. Focus, confirm (buttons) and back (Circle) are automatic through the shared components; the Shop and Armory call Buy / Equip / Error. | M9d (a3a8740) |
| Scripts/UI/WaveBanner.cs | WaveBanner : MonoBehaviour | class | Big centred message such as "Wave 2/4". Pops in, shows for a set time, then fades out. | M5b (commit "M5") (0e117c7) |

## Scripts/Weapons

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Scripts/Weapons/AmmoSlotSet.cs | AmmoSlotSet | class | The player's 4 ammo slots (Cross / Circle / Square / Triangle) and which one is active. | M3a (578c096) |
| Scripts/Weapons/AmmoSlots.cs | AmmoSlots : MonoBehaviour | class | Player-side owner of the 4 ammo slots. Tapping a face button makes that slot's ammo active for every arm. | M3a (578c096) |
| Scripts/Weapons/AmmoTypeData.cs | AmmoBehavior | enum | Enum: Projectile or Beam. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Weapons/AmmoTypeData.cs | AmmoTypeData : ScriptableObject | class | Ammo type: defines how an arm shoots (projectile shape and count, or a continuous beam), plus heat and spin-up. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Weapons/ArmEffect.cs | ArmEffect : ScriptableObject | class | A special effect an arm or armament can carry (pierce, burn, ricochet, homing, ...). | M3b (4b0398d) |
| Scripts/Weapons/ArmFireController.cs | ArmFireController : MonoBehaviour | class | MonoBehaviour that fires the selected arm on R1: fire timing, ammo behaviour (projectiles / beam), heat, spin-up, projectile spawning. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Scripts/Weapons/ArmInstance.cs | ArmInstance | class | Run-time state of one equipped arm: its WeaponArmData plus its armament slots (1-3, set on the WeaponArmData). | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmInventory.cs | ArmInventory | class | Arms the player owns but has not placed in a loadout slot. | M3b (4b0398d) |
| Scripts/Weapons/ArmLoadout.cs | ArmLoadout : ScriptableObject | class | Which arm type sits in each of the 8 slots. Slot 0 = N, then clockwise. | M1.5 (4a707ff) |
| Scripts/Weapons/ArmStats.cs | StatType | enum | Enum of arm stats: Damage, FireRate, ProjectileSpeed, ProjectilesPerShot, Spread. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmStats.cs | ArmStats | struct | The tunable stats of one arm. Immutable value; ammo multipliers are applied on top of it when firing. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmamentData.cs | ModifierMode | enum | Enum: Flat or Percent stat modification. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmamentData.cs | StatModifier | struct | One stat change. Flat: value is added to the stat (+1 projectile, +2 shots/s). | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmamentData.cs | ArmamentData : ScriptableObject | class | An armament an arm instance can hold in one of its armament slots: stat modifiers plus special effects, with a rarity, tags and a stack limit. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/ArmamentInventory.cs | ArmamentInventory | class | Armaments the player owns but has not equipped. | M3b (4b0398d) |
| Scripts/Weapons/ArmamentRarity.cs | ArmamentRarity | enum | Enum: Common, Rare, Epic, Legendary. | M9a (ee19099) |
| Scripts/Weapons/ArmamentRarity.cs | ArmamentTags | enum | What an armament does, for filtering in the shop/armory and for the generated descriptions. | M9a (ee19099) |
| Scripts/Weapons/Effects/AutoFireEffect.cs | AutoFireEffect : ArmEffect | class | The arm also fires on its own at enemies inside its slot arc while it is NOT selected, at a fraction of its fire rate. | M9a (ee19099) |
| Scripts/Weapons/Effects/BurnEffect.cs | BurnEffect : ArmEffect | class | Sets targets on fire: damage over time. Hitting a burning target refreshes the duration (no stacking). | M3b (4b0398d) |
| Scripts/Weapons/Effects/HomingEffect.cs | HomingEffect : ArmEffect | class | Bullets steer towards the nearest enemy inside a forward cone. | M9a (ee19099) |
| Scripts/Weapons/Effects/PierceEffect.cs | PierceEffect : ArmEffect | class | Projectiles pass through extra enemies before stopping. | M3b (4b0398d) |
| Scripts/Weapons/Effects/RicochetEffect.cs | RicochetEffect : ArmEffect | class | Bullets bounce off walls and obstacles instead of stopping. | M3b (4b0398d) |
| Scripts/Weapons/Effects/StunEffect.cs | StunEffect : ArmEffect | class | A hit has a chance to stun the target (it stops moving) for a short time. | M3b (4b0398d) |
| Scripts/Weapons/HeatComponent.cs | HeatSettings | struct | Heat numbers for one ammo type. All values are fractions of the heat bar (1 = full), so an arm's heat stays meaningful when the active ammo changes. | M3a (578c096) |
| Scripts/Weapons/HeatComponent.cs | HeatComponent | class | Shared heat model, one instance per arm. Heat builds while firing; at full heat the arm overheats and can't fire until it has cooled to the restart threshold. | M3a (578c096) |
| Scripts/Weapons/ItemDescriber.cs | ItemDescriber | class | Descriptions of arms, armaments and stats for the Shop/Armory UI and the debug overlay. | M4 (ced81c5) |
| Scripts/Weapons/ShotProperties.cs | ShotProperties | struct | Per-projectile behaviour granted by an arm's effects (its own plus its armaments'). | M3b (4b0398d) |
| Scripts/Weapons/SpinUp.cs | SpinSettings | struct | Spin-up numbers for one ammo type. A spin-up time of 0 means no spin-up (always at the max rate multiplier). | M3a (578c096) |
| Scripts/Weapons/SpinUp.cs | SpinUp | class | Gatling-style spin-up, one instance per arm: builds while firing, winds down when not. | M3a (578c096) |
| Scripts/Weapons/StatCalculator.cs | StatCalculator | class | Final arm stats. Order is fixed: start from the base value, add every Flat modifier, then multiply by every Percent modifier (each one is x(1 + value/100), so +50% and +50% give x2.25, not x2). | M3b code (committed early in "Design" commit) (9d5fef6) |
| Scripts/Weapons/WeaponArmData.cs | WeaponArmData : ScriptableObject | class | One weapon arm type: its art, identity and stats. | M1.5 (4a707ff) |

## Editor

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Editor/AudioSetup.cs | AudioSetup | class | D4 setup: import settings for the audio clips, the AudioMixer (Master > Music, Sfx > Ui, Announcer, with exposed MasterVolume / MusicVolume / SfxVolume), the SfxData assets with their voice limits, the AudioLibrary on GameConfi... | D4 (c379716) |
| Editor/BuildMenu.cs | BuildMenu | class | D6: one menu for Windows and WebGL builds. Every run bumps the build number (version = major.minor.build, kept in BuildVersion.json at the project root and written to Player Settings, which the Main Menu shows), builds into Bui... | D6 (3f643bc) |
| Editor/BuildMenu.cs | VersionData | class | Private JSON model (major/minor/build) of BuildVersion.json, the project-root build counter. | D6 (3f643bc) |
| Editor/BuildMenu.cs | Target | enum | Enum of build targets offered by the menu: Windows, WebGL, WebGLDevelopment. | D6 (3f643bc) |
| Editor/Cc1Setup.cs | Cc1Setup | class | CC1 setup: generates the placeholder paper-doll parts (3 clearly different variants per slot, all on the player's character canvas), the Sprite Library that holds them, the CosmeticPartData assets and their registry entries, th... | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Editor/Cc1Setup.cs | Painter | class | Draws flat placeholder shapes (fill + dark outline) onto a transparent canvas the size of the player sprite. | Scale lock + UI1 + CC1 (one commit) (f74d79b) |
| Editor/DemoSetup.cs | DemoSetup | class | S1 setup: creates the DemoConfig asset (Assets/Data/DemoConfig.asset) and puts it on the GameConfig, and wires the notice dialog of the Character Creation screen in the Main Menu scene. | S1 (484781b) |
| Editor/EnemyBulletPaletteSetup.cs | EnemyBulletPaletteSetup | class | Creates the EnemyBulletPalette asset, puts it on GameConfig, marks boss / special patterns as Special (Hot Magenta), and moves the few colours that clashed with the two bullet hues (arm ID colours, the Epic rarity, a test ammo ... | M9d (a3a8740) |
| Editor/M10Setup.cs | M10Setup | class | M10: the round 3 boss, the Pumpking. Imports its painted art, creates its patterns, feedback, jump and BossData, its EnemyData, rewires round 3 to the boss alone, builds the Boss prefab variant, the BossPool, the boss health ba... | M10 (1a79436) |
| Editor/M3aSetup.cs | M3aSetup | class | One-shot M3a setup: creates/updates the ammo and pickup-tuning assets, the pickup prefab, adds the new components to the Player prefab, and wires the Game scene (fire controller, overlay, test pickups). | M3a (578c096) |
| Editor/M3bSetup.cs | M3bSetup | class | One-shot M3b setup: creates/updates the effect assets and test armaments, adds PlayerInventory and DebugArmamentControls to the Player prefab, StatusEffects to the test enemy prefab, and wires the debug overlay in the Game scene. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Editor/M4Setup.cs | M4Setup | class | One-shot M4 (part 1) setup: save IDs and the AssetRegistry, the GameConfig, the Boot and MainMenu scenes, the flow UI and controllers in the Game scene, and the build scene order. | M4 (ced81c5) |
| Editor/M4bSetup.cs | M4bSetup | class | One-shot M4 part 2 setup: two test arm types with effects, the ShopPool, GameConfig changes (starting currency, the real one-arm starting loadout, no free test stock), the MenuRow prefab, and the Shop / Armory screens in the Ga... | M4 (ced81c5) |
| Editor/M5bSetup.cs | M5bSetup | class | One-shot M5b setup: the enemy roster (Grunt, Weaver, Ringer, Spiraler + a boss placeholder), waves and rounds 1-7, the difficulty curve, combat and coin tuning, the coin prefab, and the Game scene wiring that replaces the stub ... | M5b (commit "M5") (0e117c7) |
| Editor/M6Setup.cs | M6Setup | class | One-shot M6 setup: the settings defaults, the placeholder cosmetics (and their shape sprites) with the registry entries, the GameConfig references, the Player prefab layers, the SettingRow prefab, and the Main Menu / Game scene... | M6 (9864589) |
| Editor/M75Art.cs | M75Art | class | Draws the M7.5 placeholder art (tiles, obstacles, HUD icons, button glyphs) into PNG files and sets their import settings. | M7.5 (ce21bf8) |
| Editor/M75Art.cs | Canvas | class | Private in-memory pixel canvas used to paint the procedural placeholder arena art PNGs. | M7.5 (ce21bf8) |
| Editor/M75Art.cs | ImportSpec | struct | Private struct: PPU, pivot, tiling and 9-slice border to apply when importing one placeholder PNG. | M7.5 (ce21bf8) |
| Editor/M75Art.cs | GlyphCanvas | class | A canvas whose drawing calls with the special colour "Ink()" use the glyph's shape colour. | M7.5 (ce21bf8) |
| Editor/M75Setup.cs | M75Setup | class | One-shot M7.5 setup: sorting layers and Custom Axis (0,1,0) sorting, the placeholder art, the perspective tuning asset, footprints and feet-pivoted structure for the player / enemy / obstacle prefabs, the layered arena scenery,... | M7.5 (ce21bf8) |
| Editor/M76Setup.cs | M76Setup | class | One-shot M7.6 setup: the "Player" / "PlayerAirborne" physics layers (the airborne one does not collide with enemies), the "Airborne" sorting layer between the characters and the bullets, the JumpTuning asset, the JumpController... | M7.6 (767e3ca) |
| Editor/M77Setup.cs | M77Setup | class | One-shot M7.7 setup: the ArmRingTuning asset, the Player prefab's ArmRing anchor (at the feet, outside the lifted Visuals) and its damage Core + marker (on the ground plane, low), the lifted Body + ground Shadow children of the... | M7.7 (2ba5edb) |
| Editor/M7Setup.cs | M7Setup | class | One-shot M7 setup: the "Obstacle" physics layer, the obstacle / trap / arena assets (one test colosseum), the Obstacle and Trap prefabs, the Arena object in the Game scene wired into the projectile pool, player mover and wave s... | M7 (28a5d2c) |
| Editor/M85Setup.cs | M85Setup | class | One-shot M8.5 setup: sets the height class of every obstacle, creates one ArenaLayoutData per round (1-7, with separate open layouts for the boss rounds 3, 5 and 7) and each round's HazardBudget, points the rounds and the GameC... | M8.5 (34bbe88) |
| Editor/M85Setup.cs | Obstacles | struct | Private struct bundling the five ObstacleData assets (Pillar, LowWall, Crate, Pumpkin, Cabbage) the setup creates/edits. | M8.5 (34bbe88) |
| Editor/M85Setup.cs | Traps | struct | Private struct bundling the three TrapData assets (Vent, Skewer, Zone). | M8.5 (34bbe88) |
| Editor/M86Rig.cs | M86Rig | class | Builds the M8.6 rigged test character from nothing: writes a small layered PSD (one layer per body part, named like the Procreate layers the real bosses will use: head, torso, arm_L, arm_R, leg_L, leg_R), lets the 2D PSD Import... | M8.6 (64b7eee) |
| Editor/M86Rig.cs | Part | struct | Private struct describing one body part of the generated rig test PSB: name, pixel rect, color, ellipse flag, bone. | M8.6 (64b7eee) |
| Editor/M86Rig.cs | BoneDef | struct | Skeleton in character space: pixels, origin bottom-left of the canvas, positions relative to the parent bone. | M8.6 (64b7eee) |
| Editor/M86Rig.cs | BigEndianWriter : IDisposable | class | Private helper that writes big-endian 16/32-bit values to a stream (used to hand-write a PSB file). | M8.6 (64b7eee) |
| Editor/M86Setup.cs | M86Setup | class | One-shot M8.6 setup: creates the feedback tuning assets, the character / outline materials, the particle textures and the seven particle presets, wires the Motion child + feedback components into the Player, Enemy and Arm prefa... | M8.6 (64b7eee) |
| Editor/M8Setup.cs | M8Setup | class | One-shot M8 setup: the enemy AI tuning asset, the new attack patterns, the enemy roster converted to the new behaviours (Grunt = Chaser, Weaver = Skirmisher, Ringer / Spiraler = Sentries, plus the new Charger and Sniper), waves... | M8 (6fa1892) |
| Editor/M9aSetup.cs | M9aSetup | class | One-shot M9a setup: the effect assets (Pierce, Ricochet, Homing, Auto-fire), the armament catalog with rarity, tags, price tier and max stacks (the five starters plus the earlier test armaments), the armament slot count of each... | M9a (ee19099) |
| Editor/M9bSetup.cs | M9bSetup | class | One-shot M9b setup: RarityTable, ShopTuning and the reworked ShopPool assets, arm rarities and price tiers, the ShopCard prefab, and the new card-based Shop screen in the Game scene (replacing the old list screen). | M9b (5ef5e62) |
| Editor/M9cSetup.cs | M9cSetup | class | One-shot M9c setup: builds the new Armory screen (gladiator ring, hovering bubbles, tabbed inventory) in the Game scene, replacing the old list screen. | M9c (4711107) |
| Editor/M9dSetup.cs | M9dSetup | class | One-shot M9d setup: fills the ButtonGlyphLibrary with prompt text per device (plus a Keyboard set), puts it on GameConfig, builds the shared ConfirmDialog prefab, drops it into the Game and Main Menu scenes (replacing the Main ... | M9d (a3a8740) |
| Editor/ScaleTestImport.cs | ScaleTestImport : AssetPostprocessor | class | Import settings for painted art (Docs/ART_SPEC.md, painted HD style). | Art scale test (15b44f0) |
| Editor/ShaderGen/GraphBuilder.cs | GraphBuilder | class | Builds a Sprite Unlit shader graph from code. | M8.6 (64b7eee) |
| Editor/ShaderGen/ShaderDefinitions.cs | ShaderDefinitions | class | The M8.6 shader library as real Shader Graph assets, all "Sprite Unlit" 2D graphs that read the SpriteRenderer's _MainTex and colour: Sprite_Character (everything a fighter needs at once), plus the single-purpose HitFlash, Diss... | M8.6 (64b7eee) |
| Editor/TelemetrySetup.cs | TelemetrySetup | class | D3 setup: adds a "Stats" button to the Pause screen's debug row (next to Boss / HP- / HP+) that opens the playtest summary. | D3 (59b33c7) |
| Editor/TutorialSetup.cs | TutorialSetup | class | D2 setup: adds the gameplay control names to the button glyph asset, creates the Tutorial data asset, and builds the tutorial card plus its controller in the Game scene (VoxVegetallis look). | D2 (4368fd4) |
| Editor/UiBuilder.cs | UiBuilder | class | Helpers that build plain skeleton uGUI (legacy TMP_Text, no art) from editor setup scripts. | M4 (ced81c5) |
| Editor/UiBuilder.cs | ListScreenParts | class | The pieces of a list screen (Shop, Armory) that its component needs wired. | M4 (ced81c5) |
| Editor/VoxD1Shots.cs | VoxD1Shots | class | D1 capture run: in Play mode (Game scene), puts each in-scope screen (HUD, banners, boss bar, Round Results, Pause, Game Over, Settings) into a stress state (long names, 5-digit numbers) and renders it with  at 1920x1080, 2560x... | D1 (b76b5ee) |
| Editor/VoxD1Shots.cs | Case | class | Private screenshot case: name, scene setup action, frames to wait, before-shot action. | D1 (b76b5ee) |
| Editor/VoxGameScreens.cs | VoxGameScreens | class | VOX VEGETALLIS look of the Game scene UI: the combat HUD (tomato hearts, heat bar, portrait ring, ammo slots, round and currency pills), banners, the flow panels, Settings (from Pause) and the confirm dialog. | UI2 (799d55b) |
| Editor/VoxKitSetup.cs | VoxKitSetup | class | VOX VEGETALLIS UI kit import. Reads Assets/Art/UI/VoxKit/vox_ui_kit_manifest.json and applies the 2x UI import settings (PPU 200 = half size on the 1080p canvas), 9-slice borders, and Repeat wrap for the checker trims. | UI2 (799d55b) |
| Editor/VoxMenuScreens.cs | VoxMenuScreens | class | Builds the VOX VEGETALLIS look of the front-end screens (Main Menu, Settings, Character Creation, confirm dialog, flow panels) in the MainMenu and Game scenes. | UI2 (799d55b) |
| Editor/VoxShopArmory.cs | VoxShopArmory | class | VOX VEGETALLIS Shop ("The Mercator's Stall": merchant panel, wood stall with the cards, detail panel) and Armory (gladiator ring with the hovering bubbles, tabbed card panel, stat strip, Fight!) in the Game scene. | UI2 (799d55b) |
| Editor/VoxShots.cs | VoxShots | class | Renders the open scene's main camera plus every overlay canvas into a PNG at a chosen resolution, without needing the Game window to be focused (overlay canvases are temporarily switched to the capture camera and restored). | UI2 (799d55b) |
| Editor/VoxTextMigrate.cs | VoxTextMigrate | class | One-shot Text -> TextMeshPro migration for every scene and prefab. | UI2 (799d55b) |
| Editor/VoxTextMigrate.cs | Ref | class | Private record of one serialized reference to a legacy Text component (asset, owner path, component, property, target) so it can be re-pointed to TMP. | UI2 (799d55b) |
| Editor/VoxTextMigrate.cs | RefList | class | Private JSON wrapper holding the list of Ref records saved during the Text-to-TMP migration. | UI2 (799d55b) |
| Editor/VoxThemeSetup.cs | VoxThemeSetup | class | Builds the single UI theme (Assets/Data/UI/VoxVegetallis.asset) from the kit manifest tokens and sprites, assigns it to GameConfig, sets the TMP defaults and fallbacks, and removes the retired Mega Cozy theme asset. | UI2 (799d55b) |
| Editor/VoxUi.cs | VoxUi | class | Builder helpers for the VOX VEGETALLIS screens (editor only). | UI2 (799d55b) |
| Editor/VoxVfx.cs | VoxVfx | class | S1: procedural placeholder effects in the VOX VEGETALLIS palette, so the demo does not wait for hand-drawn effect art. | S1 (484781b) |

## Tests

| File (under Assets/) | Type | Kind | Purpose | Milestone (first-add commit) |
|---|---|---|---|---|
| Tests/EditMode/AmmoSlotSetTests.cs | AmmoSlotSetTests | class | Tests: ammo slot auto-fill, active slot, selecting, replacing. | M3a (578c096) |
| Tests/EditMode/ArenaTests.cs | ArenaTests | class | Tests: obstacle grid blocking, movement sliding and tunnelling, nearest-free push-out, trap timer phases and strikes. | M7 (28a5d2c) |
| Tests/EditMode/ArmInstanceTests.cs | ArmInstanceTests | class | Tests: ArmInstance base stats, adding/removing armaments, independence between instances, change events. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Tests/EditMode/ArmRingTests.cs | ArmRingTests | class | M7.7: arm ring ellipse, front/back halves, spin easing. | M7.7 (2ba5edb) |
| Tests/EditMode/ArmRingTests.cs | Vector2EqualityComparer : System.Collections.Generic.IEqualityComparer<Vector2> | class | Test helper: approximate Vector2 comparer for NUnit. | M7.7 (2ba5edb) |
| Tests/EditMode/ArmSelectorTests.cs | ArmSelectorTests | class | Tests: soft select thresholds, hysteresis, lock/unlock and free aim of ArmSelector. | M1 (WIP commit) (5b356f7) |
| Tests/EditMode/ArmamentInventoryTests.cs | ArmamentInventoryTests | class | Tests: equip/unequip moving armaments between inventory and arms, effect aggregation (pierce, bounces). | M3b (4b0398d) |
| Tests/EditMode/AudioTests.cs | AudioTests | class | D4 audio: the limits on identical sounds, the decibel conversion, and that the shipped library answers every sound and track the game asks for. | D4 (c379716) |
| Tests/EditMode/BossTests.cs | BossTests | class | Tests: boss enum, phase thresholds, attack picker, round 3 = Pumpking. | M10 (1a79436) |
| Tests/EditMode/BuildInfoTests.cs | BuildInfoTests | class | D6: the version line shown in the Main Menu and the build menu's version format. | D6 (3f643bc) |
| Tests/EditMode/CombatTests.cs | CombatTests | class | Tests: attack pattern shapes (aimed, spread, ring, spiral), volley clamp, Game Over and pause rules. | M5a (295524f) |
| Tests/EditMode/CombatTests.cs | FakeSave : ISaveSystem | class | Test double implementing ISaveSystem. | M5a (295524f) |
| Tests/EditMode/DemoTests.cs | DemoTests | class | S1 demo scope: the DemoConfig rules, the shipped round data, the locked profile, and the procedural effects. | L1 (1a1a93e) |
| Tests/EditMode/EnemyAiTests.cs | EnemyAiTests | class | M8: flow field, steering limits, grid ray, the move speed difficulty axis. | M8 (6fa1892) |
| Tests/EditMode/EnemyBulletPaletteTests.cs | EnemyBulletPaletteTests | class | Tests: enemy bullet palette colours and that no other game colour uses the reserved bullet hues. | M9d (a3a8740) |
| Tests/EditMode/FeedbackTests.cs | FeedbackTests | class | Tests: time scale per state, hitstop rules, spring settling, tuning assets, all Shader Graphs compile. | M8.6 (64b7eee) |
| Tests/EditMode/FireTimerTests.cs | FireTimerTests | class | Tests: fire timer shot rate, no burst storing, rapid re-press cannot beat fire rate. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Tests/EditMode/GameFlowTests.cs | GameFlowTests | class | Tests: run loop order, illegal transitions, save file round trip/corruption, asset registry. | M4 (ced81c5) |
| Tests/EditMode/HealthTests.cs | HealthTests | class | Tests: damage, clamping, Died raised once, revive. | M2 (commit "M2 done; M3 design") (8cefcd0) |
| Tests/EditMode/HeatComponentTests.cs | HeatComponentTests | class | Tests: heat gain, overheat, restart threshold, decay, spin-up. | M3a (578c096) |
| Tests/EditMode/JumpTests.cs | JumpTests | class | M7.6: jump timing, height arc, landing spot search. | M7.6 (767e3ca) |
| Tests/EditMode/LayoutTests.cs | LayoutTests | class | M8.5: height classes (Low / Tall), jumping over Low, the Tall fade rule, layouts and their hazard budgets. | M8.5 (34bbe88) |
| Tests/EditMode/M6Tests.cs | M6Tests | class | Tests: round intro flow, aim sensitivity scaling, settings clamping and file round trip, cosmetics fallback and cycling. | M6 (9864589) |
| Tests/EditMode/M6Tests.cs | FakeSave : ISaveSystem | class | Test double implementing ISaveSystem. | M6 (9864589) |
| Tests/EditMode/M9aTests.cs | M9aTests | class | M9a: armament interaction rules, variable slots, stacks, generated descriptions and saving. | M9a (ee19099) |
| Tests/EditMode/M9bTests.cs | M9bTests | class | M9b: Shop stock, prices, rerolls, crate, fit/tooltip text and saving of the shop visit. | M9b (5ef5e62) |
| Tests/EditMode/M9cTests.cs | M9cTests | class | Tests: Armory preview, swap-on-equip, inventory grouping/sort, removing arms. | M9c (4711107) |
| Tests/EditMode/M9dTests.cs | M9dTests | class | Tests: prompt labels per device family, device switching event, UI sound hooks, confirm dialog. | M9d (a3a8740) |
| Tests/EditMode/PerspectiveTests.cs | PerspectiveTests | class | M7.5: footprints and bullet reach, perspective numbers, button glyphs. | M7.5 (ce21bf8) |
| Tests/EditMode/SettingsD5Tests.cs | SettingsD5Tests | class | D5 settings: the new values and their limits, old settings files, gamepad button remapping and how it reaches the prompts. | D5 (1835475) |
| Tests/EditMode/ShopArmoryTests.cs | ShopArmoryTests | class | Tests: buying, arm placement/removal rules, persistence through save and Continue. | M4 (ced81c5) |
| Tests/EditMode/StatCalculatorTests.cs | StatCalculatorTests | class | Tests: stat calculation order (flat before percent), clamping. | M3b code (committed early in "Design" commit) (9d5fef6) |
| Tests/EditMode/TelemetryTests.cs | TelemetryTests | class | D3 playtest telemetry: the CSV format, the summary maths, and the service that turns a run into a row. | D3 (59b33c7) |
| Tests/EditMode/TelemetryTests.cs | FakeSave : ISaveSystem | class | Test double implementing ISaveSystem. | D3 (59b33c7) |
| Tests/EditMode/TutorialTests.cs | TutorialTests | class | D2 onboarding: the text built from the device's button names, the glyph asset's gameplay labels, the tutorial data, and the profile flag. | D2 (4368fd4) |
| Tests/EditMode/WaveTests.cs | WaveTests | class | Tests: SpawnScheduler, spawn formations, difficulty scaling, round table looping. | M5b (commit "M5") (0e117c7) |
| Tests/EditMode/WaveTests.cs | FakeSave : ISaveSystem | class | Test double implementing ISaveSystem. | M5b (commit "M5") (0e117c7) |

## ScriptableObject types (classes deriving ScriptableObject directly or via ArmEffect)

Method (so.sh): build the transitive subclass set from the parsed class declarations (base = first item after ':'), read each file's guid from its `.cs.meta`, and count `m_Script: {fileID: 11500000, guid: <guid>}` occurrences in `Assets/**/*.asset` (grep). Only .asset files are counted (ScriptableObject instances embedded in scenes/prefabs would not show; none known). ArmLoadout assets include the Starting and Debug loadouts.

| ScriptableObject type | Base | File | Assets | Notes |
|---|---|---|---|---|
| AmmoTypeData | ScriptableObject | Scripts/Weapons/AmmoTypeData.cs | 5 |  |
| ArenaArt | ScriptableObject | Scripts/Arena/ArenaArt.cs | 1 |  |
| ArenaData | ScriptableObject | Scripts/Arena/ArenaData.cs | 1 |  |
| ArenaLayoutData | ScriptableObject | Scripts/Arena/ArenaLayoutData.cs | 7 |  |
| ArmEffect | ScriptableObject | Scripts/Weapons/ArmEffect.cs | 0 | abstract base (instances are its subclasses below) |
| ArmLoadout | ScriptableObject | Scripts/Weapons/ArmLoadout.cs | 2 |  |
| ArmRingTuning | ScriptableObject | Scripts/Player/ArmRingTuning.cs | 1 |  |
| ArmamentData | ScriptableObject | Scripts/Weapons/ArmamentData.cs | 10 |  |
| AssetRegistry | ScriptableObject | Scripts/Save/AssetRegistry.cs | 1 |  |
| AttackPattern | ScriptableObject | Scripts/Enemies/AttackPattern.cs | 10 |  |
| AudioLibrary | ScriptableObject | Scripts/Audio/AudioLibrary.cs | 1 |  |
| AutoFireEffect | ArmEffect | Scripts/Weapons/Effects/AutoFireEffect.cs | 1 |  |
| BossData | ScriptableObject | Scripts/Bosses/BossData.cs | 1 |  |
| BurnEffect | ArmEffect | Scripts/Weapons/Effects/BurnEffect.cs | 1 |  |
| ButtonGlyphLibrary | ScriptableObject | Scripts/UI/ButtonGlyphLibrary.cs | 1 |  |
| CoinTuning | ScriptableObject | Scripts/Pickups/CoinTuning.cs | 1 |  |
| CombatTuning | ScriptableObject | Scripts/Enemies/CombatTuning.cs | 1 |  |
| CosmeticPartData | ScriptableObject | Scripts/Cosmetics/CosmeticPartData.cs | 15 |  |
| DemoConfig | ScriptableObject | Scripts/Core/DemoConfig.cs | 1 |  |
| DifficultyCurve | ScriptableObject | Scripts/Enemies/DifficultyCurve.cs | 1 |  |
| EnemyAiTuning | ScriptableObject | Scripts/AI/EnemyAiTuning.cs | 1 |  |
| EnemyBulletPalette | ScriptableObject | Scripts/Enemies/EnemyBulletPalette.cs | 1 |  |
| EnemyData | ScriptableObject | Scripts/Enemies/EnemyData.cs | 12 |  |
| FeedbackTuning | ScriptableObject | Scripts/Feedback/FeedbackTuning.cs | 1 |  |
| GameConfig | ScriptableObject | Scripts/Core/GameConfig.cs | 1 |  |
| HitFeedbackTuning | ScriptableObject | Scripts/Feedback/HitFeedbackTuning.cs | 3 |  |
| HomingEffect | ArmEffect | Scripts/Weapons/Effects/HomingEffect.cs | 1 |  |
| InputTuning | ScriptableObject | Scripts/Input/InputTuning.cs | 1 |  |
| JumpTuning | ScriptableObject | Scripts/Player/JumpTuning.cs | 2 |  |
| LifeCycleTuning | ScriptableObject | Scripts/Feedback/LifeCycleTuning.cs | 2 |  |
| MotionTuning | ScriptableObject | Scripts/Feedback/MotionTuning.cs | 3 |  |
| ObstacleData | ScriptableObject | Scripts/Arena/ObstacleData.cs | 5 |  |
| PerspectiveTuning | ScriptableObject | Scripts/Arena/PerspectiveTuning.cs | 1 |  |
| PickupTuning | ScriptableObject | Scripts/Pickups/PickupTuning.cs | 1 |  |
| PierceEffect | ArmEffect | Scripts/Weapons/Effects/PierceEffect.cs | 1 |  |
| PlayerData | ScriptableObject | Scripts/Player/PlayerData.cs | 1 |  |
| RarityTable | ScriptableObject | Scripts/Shop/RarityTable.cs | 1 |  |
| RicochetEffect | ArmEffect | Scripts/Weapons/Effects/RicochetEffect.cs | 1 |  |
| RoundData | ScriptableObject | Scripts/Enemies/RoundData.cs | 7 |  |
| SettingsDefaults | ScriptableObject | Scripts/Settings/SettingsDefaults.cs | 1 |  |
| SfxData | ScriptableObject | Scripts/Audio/SfxData.cs | 23 |  |
| ShopPool | ScriptableObject | Scripts/Shop/ShopPool.cs | 1 |  |
| ShopTuning | ScriptableObject | Scripts/Shop/ShopTuning.cs | 1 |  |
| StunEffect | ArmEffect | Scripts/Weapons/Effects/StunEffect.cs | 1 |  |
| TrapData | ScriptableObject | Scripts/Arena/TrapData.cs | 3 |  |
| TutorialData | ScriptableObject | Scripts/UI/TutorialData.cs | 1 |  |
| UITheme | ScriptableObject | Scripts/UI/UITheme.cs | 1 |  |
| WaveData | ScriptableObject | Scripts/Enemies/WaveData.cs | 22 |  |
| WeaponArmData | ScriptableObject | Scripts/Weapons/WeaponArmData.cs | 6 |  |

Totals: 49 ScriptableObject types, 1 with zero assets: ArmEffect . Asset references mapped to our types: 168. Total .asset files under Assets/: 183. m_Script references in .asset files in total: 202; the remainder belong to package scripts (TextMesh Pro, URP and similar) whose guids match no .cs.meta under Assets/.

### Our ScriptableObject assets by folder

| Folder | Assets |
|---|---|
| Assets/Data | 2 |
| Assets/Data/Ammo | 5 |
| Assets/Data/Arenas | 2 |
| Assets/Data/Arenas/Layouts | 7 |
| Assets/Data/Arenas/Obstacles | 5 |
| Assets/Data/Arenas/Traps | 3 |
| Assets/Data/Armaments | 10 |
| Assets/Data/Arms | 6 |
| Assets/Data/Audio | 24 |
| Assets/Data/Bosses | 2 |
| Assets/Data/Cosmetics | 15 |
| Assets/Data/Effects | 6 |
| Assets/Data/Enemies | 14 |
| Assets/Data/Enemies/Patterns | 10 |
| Assets/Data/Feedback | 9 |
| Assets/Data/Input | 1 |
| Assets/Data/Loadouts | 2 |
| Assets/Data/Pickups | 2 |
| Assets/Data/Player | 3 |
| Assets/Data/Settings | 2 |
| Assets/Data/Shop | 3 |
| Assets/Data/UI | 3 |
| Assets/Data/Waves | 24 |
| Assets/Data/Waves/Rounds | 7 |
| Assets/Resources | 1 |

## UNKNOWN / limitations
- UNKNOWN: whether any ScriptableObject instances are stored inside scenes/prefabs rather than .asset files (count covers .asset files only).
- UNKNOWN: precise milestone for files from the bundled commits f74d79b (UI1 vs CC1) and 1a79436 (M10 vs P1); the P1 split is an ESTIMATE by folder.
- UNKNOWN: whether every purpose sentence taken from author comments is still accurate (comments, not behaviour, were read for most types).
- Types possibly missed by the regex: declarations split across lines or exotic generics; a separate grep reconciliation of ScriptableObject files found no missing SO class.
- Library/, Temp/, Logs/, UserSettings/ were not touched; no project file was modified.

---

# SECTIONS 2 and 3 - Current state and asset slots

# Fact sheet: Sections 2 and 3 (VOX VEGETALLIS, C:\Dev\BulletHell)

Gathered 2026-10-01, read-only. Method labels: **[code]** = verified by code/asset reading; **[docs]** = stated in project docs, not independently verified; **[tests-doc]** = test result recorded in Docs/CaseStudy/05_DevLog.md (I did NOT run Unity or any test); **ESTIMATE / ASSESSMENT** = my inference.
Path shorthand: `A/` = `Assets/`. Texture/audio numbers were extracted by scripts (PNG/OGG/WAV headers + `.meta` YAML), kept in the scratchpad (`png.txt`, `aud.txt`, `refs.txt`, `refs_vox.txt`). "Referenced by" = GUID found in the YAML of .asset/.prefab/.unity/.mat files (code-side `Resources.Load`/runtime generation not traced, so "referenced by nothing" means "no YAML reference").

---------------------------------------------------------------------
# SECTION 2 - CURRENT STATE
---------------------------------------------------------------------

## 2.0 Evidence base
- Source size: ~28,322 lines of C# in `A/Scripts` (21 subfolders; UI 44 files, Weapons 24, Arena 19, Enemies 18, Core 17) [code, `find Scripts -name *.cs | cat | wc -l`].
- Tests: 32 EditMode test files, 357 `[Test]`/`[TestCase]` attributes in the tree today [code, grep]. No PlayMode tests (`Docs/CaseStudy/09_Metrics.md:81`).
- Last RECORDED full EditMode run: "Full run 344 of 355 (the 11 known Obstacle contact-shadow failures)" at S1 (`Docs/CaseStudy/05_DevLog.md:478`). L1 added 2 tests (`05_DevLog.md:489`: "full EditMode run below in the final report"; no result recorded in the repo for L1). So the recorded state is 344 pass / 11 fail; the 11 failures are an Editor-only test setup issue (BUGS.md open item). There is no TestResults xml in the repo. Test status at each milestone is "NOT recorded in the repo" except those lines (`05_DevLog.md:25`).
- Git: branch main; only `ProjectSettings/QualitySettings.asset` modified at session start; last commit "S1: demo scope" (484781b) preceded by "L1: local web build" (1a1a93e).

## 2.1 Implemented systems (code present; "verified" qualifiers per row)
Verification column: T = covered by EditMode tests (names of test files given), P = Play-mode check recorded in DevLog, none = code present, not verified in play by me.

| System | Key files | Evidence |
|---|---|---|
| Input (generic gamepad paths, soft-select/lock aim, rebinding, glyph lookup) | `A/Scripts/Input/` (GameplayInputReader, InputBindingService, AimThresholds, InputTuning), `A/Scripts/Platform/GlyphFamily.cs` | T: ArmSelectorTests (34 attrs), SettingsD5Tests, TutorialTests. P: remap/tutorial per DevLog D2/D5 [tests-doc]. Real DualSense play is the developer's own playtest (CLAUDE.md "I playtest every change") [docs]. |
| Player: movement, health/i-frames, jump (fake height), arm ring (ellipse) | `A/Scripts/Player/` (JumpController, JumpTimeline, LandingResolver, ArmRingMath, PlayerHealth, ArmVisual) | T: JumpTests, ArmRingTests, HealthTests, PerspectiveTests (3 of its tests are in the 11 known failures). |
| Weapons: arms, loadout, ammo slots (4), heat/overheat, armament stat calc + effects (Homing, Auto-fire, Velocity, Pierce, Ricochet, Burn, Stun, etc.) | `A/Scripts/Weapons/` (ArmInstance, StatCalculator, HeatComponent, AmmoSlotSet, Effects/), data `A/Data/Arms` (6), `A/Data/Armaments` (10), `A/Data/Ammo` (5 incl. TestSpare) | T: ArmInstanceTests, StatCalculatorTests, HeatComponentTests, AmmoSlotSetTests, ArmamentInventoryTests, M9aTests (20 attrs), FireTimerTests. |
| Projectiles (pooled, hostile styled bullets with outline/fill/core/glow layers) | `A/Scripts/Projectiles/` (Projectile, ProjectilePool, ProjectileRules), `A/Data/Enemies/EnemyBulletPalette.asset` | T: EnemyBulletPaletteTests (reserved-hue audit), CombatTests. |
| Enemies + AI (flow field nav, separation, LOS; Chaser/Skirmisher/Sentry/Charger/Sniper behaviours) | `A/Scripts/Enemies/`, `A/Scripts/AI/` (FlowField, NavigationService, Steering, Behaviors/) | T: EnemyAiTests (16), WaveTests (16), CombatTests. P: stress test only (P1). |
| Waves/rounds/difficulty, coins/magnet | `A/Scripts/Enemies/` (WaveSpawner, RoundData, DifficultyCurve), `A/Scripts/Pickups/`, `A/Data/Waves/` (7 Round assets, 24 Wave assets) | T: WaveTests, DemoTests. Rounds 1-7 authored [code]. |
| Bosses: Pumpking (round 3) with 2 phases, jump/smash, transition, death sequence, boss bar | `A/Scripts/Bosses/` (BossController, BossJump, BossData, SmashTelegraph), `A/Scripts/AI/BossAttackPicker.cs`, `A/Data/Bosses/Boss_Pumpking.asset`, `A/Prefabs/Boss.prefab` | T: BossTests (only 4). Play: described in DevLog M10 [tests-doc]. Only ONE boss exists. |
| Arena: layouts, obstacles (Low/Tall, breakable), traps (vent/skewer/zone), perspective, tall-obstacle fade | `A/Scripts/Arena/` (ArenaController, ArenaGrid, Obstacle, Trap, LayoutValidator), `A/Data/Arenas/` (7 layouts, 5 obstacles, 3 traps) | T: ArenaTests, LayoutTests, PerspectiveTests (11 of these fail outside Play mode, see 2.3). |
| Game flow (Boot -> Menu -> Game; RoundIntro/Combat/Results/Shop/Armory/Pause/GameOver), single-slot save, profile, settings | `A/Scripts/Core/` (GameStateMachine, RunManager, Bootstrapper, SceneLoader), `A/Scripts/Save/` (ISaveSystem, LocalFileSaveSystem, RunSaveMapper, AssetRegistry), `A/Scripts/Settings/`, `A/Scenes/{Boot,MainMenu,Game}.unity` | T: GameFlowTests (12), M6Tests (17), SettingsD5Tests (13). |
| Shop (cards, reroll, crate pick-1-of-3, tooltips) and Armory (ring + bubbles + inventory tabs) | `A/Scripts/Shop/` (11 files), `A/Scripts/Armory/` (9 files), `Docs/SHOP.md`, `Docs/ARMORY.md` | T: M9bTests (20), M9cTests (9), ShopArmoryTests (11). |
| UI theme "VoxVegetallis" (UITheme, Themed* components, FocusDecor, ButtonFocusFx), screens built by editor tools | `A/Scripts/UI/` (44 files), `A/Data/UI/VoxVegetallis.asset`, `A/Editor/Vox*.cs` | T: M9dTests (5, light). Visual checks via screenshots `Docs/Screenshots/VoxUI`, `Captures/` [docs]. |
| Onboarding / tutorial (6 lessons, saved flag) | `A/Scripts/UI/` TutorialController, `A/Data/UI/Tutorial.asset` | T: TutorialTests (6). P: walk-through in DevLog D2 [tests-doc]. |
| Audio (pooled SFX with voice limits, music crossfade, mixer routing) | `A/Scripts/Audio/` (6 files), `A/Data/Audio/` (AudioLibrary + 23 SfxData), `A/Audio/VoxMixer.mixer` | T: AudioTests (10). P: dB values + crossfade checked in Play per DevLog D4 [tests-doc]. All clips are placeholders (2.2). |
| Telemetry (local CSV per run, debug summary F9) | `A/Scripts/Telemetry/` (6 files) | T: TelemetryTests (8). P: a real death wrote a row (DevLog D3) [tests-doc]. |
| Perf tooling (overlay F8, stress test F10, PerfLogger CSV) | `A/Scripts/Perf/` (7 files), `Tools/perf_analyze.ps1` | Tools only; fixes tabled (P1, BUGS.md). |
| Feedback toolkit (hit flash, shake, telegraph, procedural motion) | `A/Scripts/Feedback/` (14 files), `A/Art/Shaders` | T: FeedbackTests (14). |
| Demo gating (`DemoConfig`) | `A/Scripts/Core/DemoConfig.cs`, `A/Data/DemoConfig.asset` (shipped `isDemo: 0`, `showPartOverlays: 0`), `A/Editor/DemoSetup.cs`, `A/Editor/BuildMenu.cs` | T: DemoTests (12). P: scripted demo run in Play mode (DevLog S1) [tests-doc]. |
| Build pipeline (Windows, WebGL, version numbers, WebGL template) | `A/Editor/BuildMenu.cs`, `A/WebGLTemplates/VoxVegetallis`, `BuildVersion.json`, `Docs/WEBGL_REPORT.md`, `Tools/WebTest` | T: BuildInfoTests (3). Browser numbers in WEBGL_REPORT.md [docs]. |
| Cosmetics / Character Creation (paper doll, Sprite Library/Resolver, profile v2) | `A/Scripts/Cosmetics/`, `A/Data/Cosmetics/` (GladiatorParts + 13 Part_*), `A/Prefabs/GladiatorDoll.prefab`, `A/Scripts/UI/CustomizationScreen.cs` | Implemented; locked in demo. Art is placeholder (Section 3). |

"Working" claim policy: I list a system as working only where there is BOTH code and a test/DevLog play check above; anything with only "Implemented" is code-present.

## 2.2 Placeholder inventory (summary; per-slot detail in Section 3)
- **Art placeholders (code-drawn or flat shapes)**: all non-painted enemies (Charger, Sniper, Ringer, Spiraler, boss placeholder + 4 Test* enemies) draw `Square.png` tinted by `EnemyData.color` and scaled by `size` (`Enemy.cs:111-115`, `EnemyData.cs:33-37`); obstacles (5 sprites in `Art/Placeholder/M75`); traps (no sprite at all: `Trap_*.asset sprite: {fileID: 0}`); bullets/coins/pickups (generic `Circle.png`/`Square.png` + code outline/shadow, `PlaceholderLook.cs`); merchant (built from circles in an editor tool, `A/Editor/M9bSetup.cs:384-409`, "A placeholder mercator (an olive)"); cosmetic parts (13 flat PNGs); HUD ammo glyphs (`Glyph_PS_*` 64x64 + `Glyph_Disc`, PlayStation set only in `ButtonGlyphs.asset` references).
- **Painted/final-looking art in the project (user's own)**: Player, 4 arm sprites, Chaser, Skirmisher (mushroom), Pumpking idle, arena backdrop (see 3.1/3.2). User states the player and arm sprites are his own and never to be replaced (CLAUDE.md). The painted enemies/boss/backdrop are marked "[x]" in `Docs/ART_CHECKLIST.md:17-23` but Chaser's file lives in `ScaleTest/` (scale-test art) [code].
- **Painted arena is ONE flattened image**: `ArenaScenery.Rebuild` returns right after adding a single `Backdrop` sprite when `ArenaArt.UseBackdrop` (`A/Scripts/Arena/ArenaScenery.cs:73-82`); that branch builds NO foreground layer, no separate crowd/torches/gates. So the layered arena the spec demands (ART_SPEC section 6) exists only as the old placeholder tile-built scenery (used when no backdrop). `GameConfig.asset` has `arenaArt` assigned (`A/Resources/GameConfig.asset:46`) so the shipped config uses the single backdrop [code].
- **Audio**: 100% CC0 placeholders (Kenney + Juhani Junkala), "chosen without listening tests" (`Docs/CREDITS.md`). No announcer VOICE exists (only stinger jingles on the Announcer bus).
- **Procedural stand-ins**: VoxVfx (`A/Editor/VoxVfx.cs`): masks glow/ring/muzzle star/puff/spark/shard generated into `A/Art/Placeholder/Vfx/Vox`, 9 particle presets in `A/Prefabs/Vfx`, 11 `Mat_Vfx_*`/Sprite materials in `A/Art/Materials`; DANGER glow (`TelegraphFx`), `PickupGlow`, Pumpking smash shockwave (`FeedbackTuning.asset` ring sprite). Backdrop blur for menus is a generated downscale (`Art/UI/Backdrop/backdrop_blur.png`, `ring_dashed.png`).
- **UI kit**: `Assets/Art/UI/VoxKit` (61 PNGs + manifest) is "project art, not a third-party pack" (`Docs/CREDITS.md`) and treated as done in `ART_CHECKLIST.md` (UI2 marks), but BUGS.md "UI2 mockup differences" lists remaining visual mismatches; the Main Menu logo is live font text, not drawn logo art (`ART_CHECKLIST.md:163`). ASSESSMENT: functionally final for the demo, not signed off as final art.
- **Fonts**: three OFL families (Cinzel Decorative, Lilita One, Nunito) as static TMP atlases (`A/Fonts`).
- **DemoConfig-gated features** (`A/Data/DemoConfig.asset`, shipped OFF; demo build menu turns ON for the build): Sentry enemies (Ringer, Spiraler) only from round 4; traps/hazard zones only from round 4; Character Creation viewable but locked with a notice; part overlays hidden (`showPartOverlays`) (CLAUDE.md "Demo scope"; `DemoConfig.cs`). Tests: DemoTests.
- **Orphaned/legacy assets**: `Wave_R3_2`, `Wave_R3_3` are not referenced by `Round_3` (Round 3 = `Wave_R3_1` only, the Pumpking) [code, `Round_3.asset`]. 4 test enemies (`Enemy_Test*`) and `Ammo_TestSpare` remain as dev data. Legacy sprites with no YAML reference: `Placeholder/Diamond.png`, `Triangle.png`, `M75/HeartFull|HeartEmpty.png`, `M75/Icon_Basic|Gatling|Laser|Shotgun.png`, `M75/UIRoundRect.png`, `Placeholder/Vfx/{Shard,SmokePuff,SoftDot,SparkStreak}.png` (replaced by VoxKit/Vox VFX).

## 2.3 Docs/BUGS.md: counts and every open item
Counts [code: read of `Docs/BUGS.md`, 185 lines]: **6 open sections, 14 fixed entries.** (The "Open" section holds 6 `###` entries; the "Fixed" section holds 14 `###` entries, including the D1 polish entry, which itself carries a "Still open (minor)" note, and the format-example entry "Ricochet bullets pass through low walls" which was closed as "could not reproduce".)

OPEN (verbatim titles, BUGS.md lines):
1. (l.13) "UI2 mockup differences that could not be matched (VOX VEGETALLIS hot swap) - WORK THROUGH OVER TIME". Everything minor (looks). Sub-items: Backdrop (generated blur, no bokeh); Main Menu (focused button same size, "v"+Application.version, text hint prompts); HUD (heat bar one blended tint, hold-to-replace ring still placeholder, glyph badges not mock's); Shop (no rarity/tag chips, no "Fits" box, no aligned stat rows; merchant is the old placeholder olive instead of "[MERCHANT ART]" arch; cards 0.92x mock; extra buttons); Armory (ring smaller than mock ~242 px vs ~380 px, filled slots layout, stat strip single panel, 4-col portrait cards vs 3-col landscape, generated dashed ring); Character Creation (eight placeholder arms stay around the doll, front arms overlap Randomize, extra Main Menu button, soft-glow spotlight); Settings (Video tab PC only, scrolling masked list, extra Back button, fixed "Changes apply instantly" note); Fonts (static atlases, missing glyphs fall back to Liberation Sans, Nunito statics from npm package); WebGL (heaviest stress test ~22 fps in browser dev build vs ~165 fps Windows; DualSense in Chrome and Continue-across-reload not tested by hand; Esc leaves fullscreen; no audio until first click); Old setup scripts (M4..M9d, Cc1, M75 still hold old-look screen builders, do not re-run); Retired art in docs (`Docs/Screenshots/UI1` still shows removed Mega Cozy pack).
2. (l.27) '"Destroy may not be called from edit mode" logged when leaving Play mode (seen while working on UI2)' - ~30 errors from `ObjectPool<Enemy>.Clear` via Unity's PoolManager on play-mode change; always when stopping Play with live enemy pools; minor (Console noise).
3. (l.34) "P1 hitches: occasional 30-70 ms frames in the stress test, cause not yet attributed - TABLED FOR THE FINAL PERFORMANCE TEST" - avg ~165 fps uncapped, p99 13.5-14.2 ms, 3-37 frames/80 s over 20 ms (worst 27-74 ms), GPU <1 ms, ~180 GC allocs (~8 KB) every frame, source unknown; minor; includes sub-note "PerfLogger `canvas_overlay_ms` column reports garbage".
4. (l.45) "11 EditMode tests fail outside Play mode: Obstacle.ApplyContactShadow calls GameServices.Ensure()" - ArenaTests 2, LayoutTests 6, PerspectiveTests 3; minor (tests only; game unaffected).
5. (l.52) "Input System NullReferenceException in InputEvent.get_handled (Editor update) - CAN'T REPRODUCE - MONITORING" - seen once, Input System 1.20.0, Editor-only, self-recovering; diagnostics added (`InputDiagnostics`); no fix made.
6. (l.62) "Sprite_Character materials disable 2D SRP batching (console warning) - DEFERRED TO M12" - Mat_SpriteCharacter / Mat_SpriteOutline `_TexelSize/_ST`; minor.
Also noted inside Fixed D1 entry (l.99) "Still open (minor)": Pause debug row clips the "Boss" button text on narrow layouts; very long boss name cut with ellipsis at 20 px min size.
Also open in a Fixed entry: "Bullet size retune (0.2-0.3 P) is still open with the art pass" (BUGS.md l.116) - bullets currently 0.3 world units (`Pattern_Ring.asset bulletSize: 0.3`).

FIXED (14 entries, titles): Demo Character Creation/portrait drew placeholder overlays (L1); Primary (red) buttons showed no focus/hover (L1); WebGL settings/profile/saves forgotten on reload (D6); D1 UI polish (HUD, Settings, Results, Pause, Game Over, banners, boss bar); Phone portrait UI shrink (M9d, resolved by landscape-only decision); Scale test enemy bullets same red family as floor (M9d, EnemyBulletPalette violet/magenta); Scale test magenta square/white shape by pillars (test pickups); painted backdrop framed with dark bars / HUD cut off; ArenaData bounds larger than painted floor; FeedbackHub Application.quitting handler leak; PrimeTween "endValue equals current" warnings; "Ricochet bullets pass through low walls" (could not reproduce, works); Armory ring slot focus opaque yellow disc; Yellow circle over player sprite (jump marker/damage core marker).

## 2.4 Every [TBD] in CLAUDE.md (grep -n "TBD" CLAUDE.md; current line numbers)
- l.107: "A separate Touch control scheme for mobile: [TBD - designed at M12, e.g. virtual left stick for aim, virtual right stick for movement, on-screen fire/lock/ammo buttons]."
- l.152: "Boss rounds: 3, 5, 7. After round 7: [TBD - e.g. boss every 2 rounds / endless scaling / game ends]."
- l.155: "[TBD: also allow the Armory between waves inside a round?]"
- l.165: "[TBD: how new cosmetics are unlocked - all unlocked for now]"
- l.213: "Game Over deletes the run save (roguelike). [TBD: any permanent meta-progression, saved separately]"
- l.429: "Rounds 5 and 7: still the placeholder boss wave. [TBD: their bosses.]"
(Note: `Docs/CaseStudy/06_Roadmap.md` quotes older line numbers 106/133/136/145/155/369; those have shifted. Total 6 TBDs. Also checked the CLAUDE.md Milestone list line "[ ] M10 (rounds 5 and 7) Bosses".)

## 2.5 Honest assessment of remaining work  (ASSESSMENT / ESTIMATE, based on evidence above)

Milestone state per CLAUDE.md (unchecked): "Vertical slice art for one arena", "M10 (rounds 5 and 7) Bosses", "P1 Performance pass" ([~] partial), "PF1 Case study documentation system" ([ ] though 10 docs exist in `Docs/CaseStudy`), "M11 Themed UI/visual pass", "M12 Polish". Demo track D1-D6, S1, L1 all [x]. W1 (itch.io deployment) postponed (`Docs/CaseStudy/06_Roadmap.md`).

A. PURE CONTENT (data/art/audio, little or no new engineering) - ASSESSMENT:
- Replace all placeholder art in Section 3 (obstacles, traps' 4-state art, bullets, coins/pickups, enemy sprites for Charger/Sniper/Sentry types, merchant parts, layered arena, VFX textures, glyph sets Xbox/Nintendo/Touch, armament icons for Burn/Damage/ExtraProjectile/FireRate/Stun which currently have NO icon). Slots already exist (`EnemyData.paintedSprite`, `ObstacleData.sprite`, `TrapData.sprite`, `AttackPattern.bulletSprite`, `AmmoTypeData.projectileSprite/icon`, `ArmamentData.icon`, `CosmeticPartData`), so swapping is mostly import + assignment. Exception: trap art needs 4 states (idle/telegraph/active/cooldown) per ART_SPEC but `TrapData` has a single `sprite` field and the trap code draws shapes/colours (`Trap_*.asset telegraphColor/activeColor`), so animated trap sprites need small code/data work [ASSESSMENT from `TrapData.cs:26`].
- Replace placeholder audio (music, SFX, a real announcer if wanted): slots are data-only (`SfxData`, `AudioLibrary`).
- Boss poses (windup/attack fall back to idle), enemy windup/attack poses, player run cycle/jump poses (ART_CHECKLIST lists these as unchecked; player currently one sprite + procedural motion).
- Tuning/balance of rounds 4-7 (authored but only lightly playtested; telemetry exists).
- More armaments/arms/ammo ("Later: Tracking, Automatic, more") are new assets on existing systems.

B. STILL NEEDS ENGINEERING for a full game - ASSESSMENT:
1. Bosses for rounds 5 and 7: only the Pumpking exists; R5 (`Wave_R5_3`) and R7 (`Wave_R7_4`) spawn `Enemy_BossPlaceholder` (a tinted square, "Warden"). The boss framework (BossData/phases/attack list) is data driven, so each boss is mostly data + art, but unique mechanics would need new code (Pumpking's jump/smash is bespoke: `BossJump`, `SmashTelegraph`). Effort: ESTIMATE per boss = new BossData + patterns + art + some mechanic code.
2. Post-round-7 loop undefined (TBD l.152) and no meta-progression (TBD l.213): needs a design decision, then engineering (endless scaling or boss cycle; a meta save beyond the profile file). Profile file already exists (`ProfileService`, `ProfileData.tutorialDone`), so a meta layer can reuse it.
3. Touch controls (M12): no touch/virtual-stick/OnScreen code anywhere in `A/Scripts` (grep for touchscreen/virtual stick/OnScreen: no match); only `GlyphFamily.Touch` enum + fallback. Landscape-only is set; `Application.isMobilePlatform` handling exists for glyphs only. Real engineering task (new control scheme, UI, jump button, aim model on touch).
4. Localization: none (grep "localiz" only matches two UI files incidentally; Unity Localization package not used; strings are hard-coded English in code/editor builders, e.g. ShopScreen `"THE MERCATOR'S STALL"`). Static TMP font atlases (ASCII + Latin-1 + some symbols) would also need rebuilding for other scripts. Engineering + content.
5. Platform builds: Windows + WebGL builds exist (BuildMenu); WebGL perf ~22 fps at 80 enemies in a dev build (WEBGL_REPORT, BUGS.md). iOS/Android/Switch/Xbox: no platform code under `Scripts/Platform` beyond glyph/capabilities; consoles also need Pro/platform approval (CLAUDE.md). Mac/Linux untested. No evidence of any mobile build ever made.
6. Performance (P1): unattributed hitches (30-74 ms), ~8 KB/frame GC allocations, SRP-batcher warning on character materials, mobile/Switch budget never measured. Needs profiling-driven engineering after content is near complete (already tabled by the developer).
7. Art-pipeline features the code already expects but art does not yet exercise: layered arena (current painted backdrop is a single image with no foreground layer, so the "foreground draws over gameplay" rule is unmet in the painted path), rigging for merchant/bosses (deferred), Character Creation part art + unlock rules (TBD l.165).
8. Shop/Armory remaining items: optional services (sell/remove/upgrade) marked "later"; mock-match differences (BUGS.md item 1).
9. Test debt: 11 failing tests outside Play mode need a small fix; no PlayMode tests; the boss/telegraph/UI layers have thin coverage (BossTests 4, M9dTests 5).
10. Housekeeping: stale old setup scripts that rebuild the old look (BUGS.md) and a leftover `Docs/CLAUDE.new.md` and `Docs/Screenshots/UI1` of retired art.

Roughly (ESTIMATE, not measured): the engine/loop for rounds 1-7 + shop + armory + settings + save is built; what's missing for a "full game" is more content breadth (2 bosses, post-7 loop, art/audio), platform ports (touch), localization, and the performance pass.

---------------------------------------------------------------------
# SECTION 3 - ASSET SLOTS
---------------------------------------------------------------------

Import-value legend (read from `.meta`): `textureType 8` = Sprite (2D and UI); `0` = Default texture. `spriteMode 1` = Single, `2` = Multiple. `filterMode 1` = Bilinear. Mip = `enableMipMap`. Compression enum (Unity TextureImporterCompression): `0` Uncompressed, `1` Compressed (normal), `2` CompressedHQ. All values are the DEFAULT-platform settings; I did not enumerate per-platform overrides (the WebGL/Android override blocks, if any, were not read). PPU = `spritePixelsToUnits`. Max size is 2048 everywhere. Dimension = actual PNG IHDR pixels. "px on screen at 1080p" factor: ESTIMATE only (the cropped backdrop is 3269 px / 220 PPU = 14.86 world units across 1920 px, ~129 screen px per world unit if the camera frames it exactly, `ArenaArt.cs`, `ArenaScenery.cs:77-78`; camera letterboxing can change this).

## 3.0 The spec a final asset must match (from Docs/ART_SPEC.md and actual import)
- Style: painted HD, bilinear, High Quality compression (ART_SPEC section 1 table), PNG RGBA sRGB transparent, no baked background, names `category_name_state_##.png` lowercase (section 8).
- Pipeline: draw 4x masters at P=506 px into `ArtSource/<mirror of Assets/Art>`, `Tools/export_art.ps1` downscales 50% to 2x game PNGs (section 1). 2x canvases: character 768x768 (master `tpl_character_1536`), boss 1280x1280 (master 2560), bullet 192x192 (384 template), icon 384 (768 template), obstacles Low 256x128 (512x256), Tall 256x384 (512x768), floor trap 768x576 (1536x1152), arena 3840x2160 (7680x4320) per ART_CHECKLIST.
- Pivot at footprint centre/feet ("real base" of the art), 4 px padding, feet on same row for frames (section 4). Floor-flat things squashed by F=0.6. Light from top-left. Dark outline. Low <= 0.5 P, Tall >= 1.5 P (section 3). Sizes in P (section 5).
- Import actually in use for painted characters: PPU 191.30435 (= 220 / 1.15, character scale lock), Single sprite, bilinear, no mips, CompressedHQ (`c2`), pivot per art (Pumpking 0.534,0.2125; Skirmisher 0.513,0.219; Chaser 0.5,0.1406), alignment Custom. Player/arms: PPU 273.91 / 347.83.
- Reserved colours: enemy bullets Electric Violet (core #FFFFFF, body #B44BFF, outline #1B0730), Hot Magenta #FF3DCB for boss/special; DANGER #FF4D33; no decorative use within ~22 degrees of those hues (`A/Data/Enemies/EnemyBulletPalette.asset`, ART_SPEC section 7).

## 3.1 Character and boss art (painted / user art)

| Slot (asset + field) | Current file | Size px | Import now | Final-spec note |
|---|---|---|---|---|
| Player body: `Cosmetics/GladiatorParts.asset`, `Part_body_original`; `Prefabs/GladiatorDoll.prefab`, `Player.prefab` | `A/Art/Player/Playersprite.png` (master `ArtSource/Player/Playersprite.png` 604x630) | 302x315 | Sprite, Multiple, PPU 273.91304, bilinear, mips off, CompressedHQ, pivot (0.5,0.5) | USER'S OWN, treat as final (CLAUDE.md). Single pose; spec wants down/side/up x idle/run(4)/jump takeoff/land (ART_CHECKLIST A, unchecked). |
| Arms x4: `Arms/Arm_{Red,Blue,Green,Purple}.asset .sprite` (Arm_Ember reuses green, Arm_Piercer reuses purple) | `A/Art/Arms/arm_red|blue|green|purple.png` | 256x256 each | Sprite, Single, PPU 347.82608, bilinear, no mips, CompressedHQ, pivots red (0.148,0.746) blue (0.109,0.723) green (0.129,0.75) purple (0.168,0.785) | USER'S OWN, final (CLAUDE.md "never replace"). No ArtSource master for arms in repo. Drawn pointing right, pivot at attach point. 6 WeaponArmData share 4 sprites. |
| Chaser (Grunt): `Enemies/Enemy_Grunt.asset .paintedSprite` | `A/Art/ScaleTest/enemy_chaser_idle_side.png` (master 1024x1024) | 512x512 | Sprite, Single, PPU 191.30434, bilinear, no mips, CompressedHQ, pivot (0.5,0.1406) | Painted, from the scale-test folder (not yet moved to `Enemies/Chaser`). Idle only; windup/lunge unchecked. Footprint r 0.345, hurtbox 1.15x0.8625 in data. |
| Skirmisher (Weaver, mushroom): `Enemies/Enemy_Weaver.asset .paintedSprite` | `A/Art/Enemies/Skirmisher/enemy_skirmisher_idle_side.png` (master 1024x1024) | 512x512 | Sprite, Single, PPU 191.30435, bilinear, no mips, CompressedHQ, pivot (0.5127,0.21875) | Painted idle; windup/shoot not drawn (toolkit pulse substitutes). Footprint r 0.30. |
| Pumpking boss: `Enemies/Enemy_Pumpking.asset .paintedSprite` (BossData `Boss_Pumpking`) | `A/Art/Bosses/Pumpking/boss_pumpking_idle.png` (master 2560x2560) | 1280x1280 | Sprite, Single, PPU 191.30435, bilinear, no mips, CompressedHQ, pivot (0.534,0.2125) | Painted idle (~2.7 P). windup/attack poses missing, fall back to idle. Footprint r 0.7, hurtbox 3.0x2.6. Boss template per spec is 2560 master / 1280 export: matches. |
| Round 5 and 7 bosses: `Enemies/Enemy_BossPlaceholder.asset` (color 0.75,0.45,0.15; size 2.76; no paintedSprite), used by `Wave_R5_3`, `Wave_R7_4` (and orphan `Wave_R3_3`) | none: draws `Square.png` tinted/scaled (`Enemy.prefab` body sprite) | `Square.png` 32x32 | see 3.2 | PLACEHOLDER. Needs 1280x1280 export per boss (2560 master), key poses idle/windup/attack. |
| Charger (`Enemy_Charger`, size 1.265, behaviour 4), Sniper (`Enemy_Sniper`, 1.035, 5), Ringer (`Enemy_Ringer`, 1.495, 3), Spiraler (`Enemy_Spiraler`, 1.265, 3, `paintedSprite` explicitly empty) | none: tinted `Square.png` | 32x32 source | see 3.2 | PLACEHOLDER shapes. Used in waves: Charger R3_2,R4_3,R5_1,R6_2,R6_4,R7_1-3; Sniper R5_2,R6_1,R6_3,R7_2,R7_3; Ringer R4_2,R5_1,R5_3,R6_3,R7_1,R7_3,R7_4; Spiraler R6_2,R6_4,R7_1,R7_3,R7_4 [code]. Spec sizes: Charger ~1.1 P, Sniper ~1 P, Sentry ~1 P tall x 1.2 P wide. Character canvas 768x768 (2x) at PPU 191.3. Demo hides Sentry (Ringer, Spiraler) before round 4. |
| Dev-only enemies `Enemy_TestMover|TestRing|TestSpiral|TestStatic` | tinted `Square.png` | - | - | Test data, not shipping content. |

## 3.2 Placeholder sprites (Assets/Art/Placeholder), by group

Common import for all rows below unless noted: Sprite, bilinear, no mips, max 2048, compression Compressed (`c1`), alignment Custom.

| Slot / used by | File(s) | Pixels | PPU | Pivot | Notes / spec for final |
|---|---|---|---|---|---|
| Generic shapes: bullets (all `AttackPattern.bulletSprite`, 10 Pattern_* assets), player projectile (`Ammo_Basic|Shotgun|Laser|Gatling .projectileSprite`), coin (`Coin.prefab`), `Arm.prefab`, `Player.prefab`, `Projectile.prefab`, `ArmoryBubble.prefab`, `PerspectiveTuning.asset` (shadow/ellipse sprite), `JumpTuning.asset`, `Jump_Pumpking.asset`, scenes | `Placeholder/Circle.png` | 128x128 | 128 | (0.5,0.5) | One white circle drives every bullet/coin/shadow ellipse; look is composed in code (`Projectile.LaunchHostile`: outline, fill, core, glow layers; `PlaceholderLook` outline + contact shadow). Final bullets: 192x192 canvas (384 master), white for player bullets (tinted by arm), reserved violet/magenta with outline for enemy bullets, size 0.15-0.25 P player / 0.2-0.3 P enemy (currently bulletSize 0.3 in Pattern_Ring). Laser needs start cap/middle tile/end cap/impact. |
| Enemy placeholder body (`Enemy.prefab`), `Ammo_TestSpare`, Game.unity strips | `Placeholder/Square.png` | 32x32 | 32 | (0.5,0.5) | See 3.1 final sizes. |
| Unreferenced shapes | `Placeholder/Diamond.png` 64x64 (PPU 64), `Triangle.png` 64x64 (PPU 64) | | | | No YAML references (legacy). |
| Obstacles: `Arenas/Obstacles/Obstacle_LowWall.asset` (size 1x0.5, artSize 1x0.85, heightClass 0 Low) | `Placeholder/M75/LowWall.png` | 64x64 | 64 | (0.5,0) bottom | Final: Low class <= 0.5 P; spec canvas 256x128 at 2x (512x256 master); pieces straight/end_left/end_right/corner + `_shadow`; consistent "jumpable" cap colour. |
| `Obstacle_Crate.asset` (size 0.85x0.55, artSize 0.95x1, Low) | `M75/Crate.png` | 64x64 | 64 | (0.5,0) | Breakable crate row: intact/dmg1/dmg2/debris (spec section 6). |
| `Obstacle_Cabbage.asset` (size 0.8x0.5, artSize 0.95x0.9, heightClass 0) | `M75/Cabbage.png` | 64x64 | 64 | (0.5,0) | Low breakable. |
| `Obstacle_Pillar.asset` (size 0.9x0.55, artSize 1.1x2.3, Tall) | `M75/Pillar.png` | 64x160 | 64 | (0.5,0) | Final: Tall >= 1.5 P, 0.8-1.2 P wide x 1.8-2.5 P tall; canvas 256x384 at 2x (512x768 master); `_shadow` separate. Pivot at footprint centre. |
| `Obstacle_Pumpkin.asset` (size 1.3x0.8, artSize 1.6x1.6, Tall) | `M75/Pumpkin.png` | 64x64 | 64 | (0.5,0) | Breakable giant pumpkin: dmg1/dmg2/debris + chunks. |
| Traps: `Arenas/Traps/Trap_Vent|Skewer|Zone.asset` (`sprite: {fileID: 0}`; sizes 1x1, 0.5x4, 1.3x1.3; telegraphColor (1,0.85,0.2), activeColor (1,0.25,0.15)) | NONE (drawn by code from colour) | - | - | - | Note: the Trap data telegraph colour (1,0.85,0.2) differs from the spec's reserved DANGER #FF4D33. Final trap art: flat (xF 0.6), 768x576 at 2x, 4 states each (idle, telegraph 2-4 frames, active 3-4, cooldown 2-3). Single `sprite` field exists. Hidden in demo before round 4. |
| Placeholder arena (used only when `ArenaArt.backdrop` empty): `ArenaScenery` fields in `Game.unity` | `M75/FloorTile.png` 128x128 PPU 85.33; `StoneTile.png` 128x128 PPU 128; `CrowdTile.png` 128x128 PPU 64; `CheckerTile.png` 32x32 PPU 64; `Ring.png` 256x256 PPU 256 (also `Boss_Pumpking.asset` shockwave ring slot? - referenced there) | | | (0.5,0.5) | Superseded by backdrop (3.3). If the layered path is used for real, spec asks floor tile 1 P x 0.6 P, tileable. |
| HUD glyph sprites: `UI/ButtonGlyphs.asset` | `M75/Glyph_Disc.png`, `Glyph_PS_Circle|Cross|Square|Triangle.png` | 64x64 each | 64 | (0.5,0.5) | PlayStation-only sprites referenced; Xbox/Nintendo/Touch/Keyboard families exist as enum (`GlyphFamily.cs:9`) but ART_CHECKLIST lists those sets as undrawn. Spec: sets for PlayStation/Xbox/Nintendo/touch; "Free: Kenney input prompts" suggestion. |
| Legacy, unreferenced | `M75/HeartFull|HeartEmpty.png`, `M75/Icon_Basic|Gatling|Laser|Shotgun.png` (64x64), `M75/UIRoundRect*.png` (64x64, 9-slice 18; `UIRoundRectOutline` referenced from Game.unity), `Icon_Spare.png` (referenced by `Ammo_TestSpare.icon`) | | 64 | | Replaced by VoxKit; candidates for deletion. |
| Ammo pickups: `Prefabs/AmmoPickup.prefab` | sprite comes from `AmmoTypeData.projectileSprite` (Circle / Square) | | | | Final spec: 0.3-0.4 P, floor glow ellipse xF, "amphora or crate + icon floating". Coin: spec 6-frame spin (128 canvas); currently `Coin.prefab` + Circle + `Mat_Vfx_Coin.mat`; UI coin icon is `VoxKit/icon_coin_seed.png`. |

## 3.3 Arena backdrop and menu backdrops

| Slot | Current file | Pixels | Import | Notes |
|---|---|---|---|---|
| `Settings/ArenaArt.asset .backdrop` (assigned on `Resources/GameConfig.asset` `arenaArt`), crop RectInt (273,54,3269,1840), position (-0.037,0.168), sorting -100 | `A/Art/ScaleTest/arena01_backdrop.png` (master `ArtSource/ScaleTest/arena01_backdrop.png` 7680x4320) | 3840x2160 (painted area 3269x1840 inside dark bars) | Sprite, Single, PPU 220, bilinear, no mips, CompressedHQ, pivot (0.5,0.5) | Single flattened painting incl. floor, back wall/crowd, gates, side walls, railing. Foreground layer NOT built in this path (`ArenaScenery.cs:73-82`). Final spec: separate layers (floor tiles+decals, backwall, side walls, foreground railing/front crowd drawn over gameplay, animated props), 3840x2160 export, arena PPU 220 (arena scale unchanged by the 1.15x character scale). 16:9 only; wider/taller aspect needs filler art ("Outer filler" ART_CHECKLIST F [3]). Gates in art at about x +-4.3 (BUGS.md fixed entry). |
| Menu/Shop/Armory backdrop (`MenuBackdrop` in MainMenu.unity and Game.unity) | `A/Art/UI/Backdrop/backdrop_blur.png` | 512x288 | Sprite, Single, PPU 28.8, bilinear, no mips, UNCOMPRESSED (`c0`) | Generated blur of the arena backdrop by `BulletHell/Vox/5`; mocks show bokeh (BUGS.md item 1). Replace with key art or a wide arena shot. |
| Armory ring: `Game.unity` | `A/Art/UI/Backdrop/ring_dashed.png` | 1024x614 | Sprite, Single, PPU 100, bilinear, Compressed | Generated dashed ellipse. |
| WebGL page | `A/WebGLTemplates/VoxVegetallis/TemplateData/cover.jpg` (1280x720), `fullscreen-button.png` (38x38) | | no .meta import (template files) | Browser cover art; placeholder title card. |

## 3.4 Cosmetic parts (Character Creation paper doll)

| Slot | Files (13) | Pixels | Import | Notes |
|---|---|---|---|---|
| `Cosmetics/GladiatorParts.asset` (Sprite Library) and the `Part_*.asset` CosmeticPartData; `GladiatorDoll.prefab` uses `part_head_round`, `part_armor_plate`, `part_accessory1_crown`, `part_accessory2_crimson_cape`, `Playersprite` | `A/Art/Placeholder/Parts/`: body: `part_body_gumdrop`, `part_body_mint_bean` (+ `Playersprite` as "Body 1/original"); armor: `part_armor_belt`, `_plate`, `_stripes`; head: `part_head_pointy`, `_round`, `_square`; accessory1: `part_accessory1_bow`, `_crown`, `_horns`; accessory2: `part_accessory2_backpack`, `_banner`, `_crimson_cape` | all 302x315 | Sprite, Single, PPU 273.91304, bilinear, no mips, UNCOMPRESSED (`c0`), pivot (0.5,0.5) | Placeholder flat shapes. Spec (ART_SPEC 6b): all parts on the same `tpl_character_1536` canvas/pose (2x = 768x768 at spec; note current parts are 302x315, same canvas as Playersprite, so finals must match that canvas or the player must be re-exported), layer order body < armor < head < accessory1 (< 1.3 P line) < accessory2 (or behind body), identical neck/shoulder joins, names `player_<part>_<variant>_<facing>.png`, side facing first. Hidden in demo (`showPartOverlays` off); Character Creation locked in demo. ART_CHECKLIST: body x3, armor x3, head x3, acc1 x3, acc2 x3 unchecked (post-demo). |

## 3.5 UI kit (VoxKit): 61 PNGs, one import profile

Common import for ALL 61 files: Sprite, Single, **PPU 200**, bilinear, mips off, max 2048, **Uncompressed (`c0`)**, pivot (0.5,0.5), manifest `A/Art/UI/VoxKit/vox_ui_kit_manifest.json` (2x art, "Mockup px x3 = these px"; drawn half-size at 1080p; applied by `BulletHell/Vox/1 Import Kit Sprites`). Referenced via `Data/UI/VoxVegetallis.asset` (theme) plus prefabs ShopCard, ConfirmDialog, MenuRow, SettingRow, CosmeticRow, ArmoryBubble and the Game/MainMenu scenes. Status: project-made art, treated as done in ART_CHECKLIST (UI2) - ASSESSMENT: functionally final for the demo; logo (live font text), merchant panel art and hold-progress ring still flagged as placeholder (BUGS.md item 1, ART_CHECKLIST l.143, l.163).

| Group | Files (count) | Pixels | 9-slice border L,B,R,T (as stored in meta: x,y,z,w) |
|---|---|---|---|
| Panels | `panel_marble|shade|wood` (3) 256x256 | 256x256 | 60,78,60,60 |
| Panel corn | `panel_corn` (1) | 256x256 | 54,72,54,54 |
| Pills | `pill_marble` 240x138 (60,78,60,60); `pill_hintbar` 200x84 (42,42,42,42) | | |
| Buttons | `button_normal|focused|disabled|primary` (4) | 256x200 | 54,72,54,54; `button_pressed` 54,54,54,72 |
| Arrow buttons | `button_arrow_left|right` (2) | 132x144 | none |
| Cards | `card_common|rare|epic|legendary` (4) 414x588; `card_focus_ring` 450x606 | | none |
| Hearts / heat bar | `heart_tomato_full|empty` 90x90; `heatbar_track` 240x48 (24 all); `heatbar_fill_white` 120x30 (15 all) | | |
| Ammo slots | `slot_ammo` 198x216, `slot_ammo_empty` 198x198, `slot_active_ring` 234x234 | | none |
| Ammo icons | `icon_ammo_basic|shotgun|laser|gatling` (4) | 144x144 | none; referenced by `Ammo_Basic|Shotgun|Laser|Gatling.icon` |
| Armament icons | `icon_armament_autofire|homing|pierce|ricochet|velocity` (5) | 144x144 | none; referenced by Armament_AutoFire, Homing, Pierce, Ricochet, BulletSpeed. **No icon assigned** for Armament_Burn, Damage, ExtraProjectile, FireRate, Stun (all 5 are in the ShopPool) |
| Misc icons | `icon_coin_seed` 78x78, `icon_harvest_crate` 144x144, `stamp_sold` 384x224 | | |
| Armory | `armslot_filled` 150x168, `armslot_selected` 216x234, `bubble_empty` 198x216, `bubble_filled_rim_white` 222x240, `bubble_focused` 264x282, `tether_vine_segment` 60x24, `pedestal_marble` 1140x306, `spotlight_arch` 1380x1560 | | none |
| Controls | `slider_track` 300x66 (33 all), `slider_fill_white` 200x42 (21 all), `slider_handle` & `_focused` 102x114, `toggle_track_on|off` 228x120, `toggle_knob` 84x84, `row_focus_ring` 256x256 (69 all), `tab_normal|selected` 160x120 (36,10,36,36) | | |
| Trim / frame | `trim_checker_leaf|corn|carrot|tomato` 42x42 (tiled), `laurel_right` 162x120 (the left laurel is a flip in code; no `laurel_left.png` exists), `portrait_ring_leaf` 348x366 | | none |

Spec for replacements: keep PPU 200, uncompressed, the manifest's border tokens and sprite names; tokens in manifest (palette: ink soil #2E1F14, marble #F4EEDC, gold #E9B63A, tomato #D8443A, leaf #5E9F3E, carrot #E57A24, corn #F4CE4A) mirrored in `VoxVfx.cs:25-35`. Fonts: Cinzel Decorative Bold/Black, Lilita One, Nunito 600/800 (static TMP SDF atlases, ASCII + Latin-1 + symbols).

## 3.6 Merchant (Shop) and portraits
- Merchant: no sprite asset. Built from circles/shapes as scene objects by `A/Editor/M9bSetup.cs:384-409` ("A placeholder mercator (an olive): body, head, eyes, laurel. Final art replaces this whole group") inside the `MerchantPanel` made by `A/Editor/VoxShopArmory.cs:349-355` (panel 382x766 at 1080p reference, nameplate "Mercator Oliva"). Final spec: drawn in PARTS and rigged (ART_SPEC section 6, 6 "Rigged characters": layers head/torso/arm_L/arm_R etc, PSD export), neutral + happy-on-purchase face swaps (ART_CHECKLIST J). No import settings yet (no file).
- Portrait: `A/Scripts/UI/HudPortrait.cs` composes head + accessory 1 of the cosmetics (no separate portrait drawing, per spec 6b); frame is `VoxKit/portrait_ring_leaf.png` 348x366. In the demo the portrait shows the base player sprite only.
- Armory doll: `Prefabs/GladiatorDoll.prefab` (same parts).

## 3.7 VFX placeholders (VoxVfx, procedural)

All generated by `A/Editor/VoxVfx.cs` (`BulletHell/Vox/10 Build VFX Placeholders`), folder `A/Art/Placeholder/Vfx/Vox/`. Import: Sprite, Single, bilinear, no mips, Compressed (`c1`), pivot centre, PPU = pixel width (so each is 1 world unit across).

| Texture | Pixels | Used by | Final spec |
|---|---|---|---|
| `glow.png` | 128x128 | `Mat_Vfx_Steam.mat`, `FeedbackTuning.asset` (telegraph/pickup glow), PickupGlow | flat floor glow (xF), white, tinted DANGER #FF4D33 for telegraphs |
| `shockwave_ring.png` | 256x256 | `FeedbackTuning.asset` (Pumpking smash ring) | spec: flat on floor (x0.6), 4-6 frames expanding |
| `muzzle_star.png` | 128x128 | `Mat_Vfx_MuzzleFlash.mat` | white muzzle flash, tinted in code |
| `puff.png` | 64x64 | `Mat_Vfx_Dust`, `_Smoke`, `_SpawnPuff` | smoke puff texture |
| `shard.png` | 32x32 | `Mat_Vfx_Confetti`, `_Debris` | shard texture / debris chunks |
| `spark.png` | 64x16 | `Mat_Vfx_Spark` | spark streak |
| `dot.png` | 64x64 | `Mat_Vfx_Coin` | soft dot |
Prefabs (`A/Prefabs/Vfx`): Vfx_Coin, Confetti, Debris, Dust, MuzzleFlash, Smoke, Spark, SpawnPuff, Steam (9 particle presets). Materials `A/Art/Materials`: Mat_SpriteCharacter, Mat_SpriteOutline + 9 Mat_Vfx_*. Older unreferenced textures `Placeholder/Vfx/{SoftDot 64x64, SmokePuff 64x64, SparkStreak 64x16, Shard 32x32}` use textureType Default(0), spriteMode 2 (Multiple), PPU 100, Compressed. ART_CHECKLIST D lists remaining drawn effects: hit spark, death splat + floor decal, impact spark, overheat steam, boss summon/phase burst/death explosion, ricochet/pierce/homing effects, juice droplet.

## 3.8 Icons summary (ammo / armament)
See 3.5. Ammo: 4 final-looking VoxKit icons + `Ammo_TestSpare` -> `M75/Icon_Spare.png` (64x64 legacy test). Armament: 5 of 10 have icons. Spec: icon canvas 384 at 2x (768 master), rarity rim drawn white and tinted by code.

## 3.9 Audio (A/Audio; 40 clips + mixer; corrected 2026-10-01 from "41 files": 5 music + 31 Sfx + 4 Stingers = 40)

Import for music (5 files): LoadType 2 = **Streaming**, compression Vorbis (`compressionFormat 1`), quality 0.5, preloadAudioData 0, forceToMono 0, loadInBackground 1, PreserveSampleRate. Import for all 35 SFX/stingers (31 Sfx + 4 Stingers; corrected from 36): LoadType 0 = **Decompress On Load**, Vorbis, quality 0.6, preload 1, **forceToMono 1**, loadInBackground 0, PreserveSampleRate. (`AudioSetup.cs` "effects: mono, decoded at load; music: streamed", CREDITS.md.) Lengths from file headers (OGG last-granule / WAV data chunk); "src ch" is the source file's channel count (imported mono when forceToMono=1).

### Music (AudioLibrary.music, `A/Data/Audio/AudioLibrary.asset`)
| File | Format | src ch | Rate | Length | Size | Slot (MusicContext, volume) |
|---|---|---|---|---|---|---|
| `Music/music_menu.wav` | WAV PCM16 | 2 | 44100 | 11.29 s | 1,946 KB | Context 1 Menu, vol 0.6 (also WAV: 11 s only, loop assumed) |
| `Music/music_combat_1.ogg` | OGG Vorbis | 2 | 44100 | 41.14 s | 1,652 KB | Context 2 Combat (odd rounds), vol 0.55 |
| `Music/music_combat_2.ogg` | OGG | 2 | 44100 | 56.10 s | 2,412 KB | Combat (even rounds) |
| `Music/music_boss.ogg` | OGG | 2 | 44100 | 71.72 s | 2,954 KB | Context 3 Boss, vol 0.6 |
| `Music/music_shop.ogg` | OGG | 2 | 44100 | 21.33 s | 917 KB | Context 4 Shop (Results/Shop/Armory), vol 0.5 |
Library params: crossfade 1.5 s, voicePool 24, pausedMusicVolume 0.35. Source: Juhani Junkala, CC0. Final: any ogg/wav; match loop points; keep streaming import.

### SFX and stingers (`A/Data/Audio/Sfx_*.asset` -> `AudioLibrary.sfx` ids 1-19; ammo shots via `AmmoTypeData.fireSound`)
| SfxData asset (SfxId) | Clips (files, src ch/rate, length) | volume / pitch range / maxVoices / minInterval / priority | Bus (mixer group) |
|---|---|---|---|
| Sfx_EnemyHit (1) | `Sfx/enemy_hit_1|2|3.ogg` (stereo src 44.1k; 0.12 / 0.18 / 0.14 s) | 0.5 / 0.9-1.15 / 3 / 0.05 / 0 | Sfx (bus 0) |
| Sfx_EnemyDeath (2) | `enemy_death_1.ogg` (mono, 0.50 s), `enemy_death_2.ogg` (mono, **4.71 s**, 155 KB) | 0.7 / 0.9-1.1 / 3 / 0.05 / 1 | Sfx |
| Sfx_PlayerHit (3) | `player_hit_1|2.ogg` (stereo, 0.65 / 0.54 s) | 0.9 / 0.95-1 / 2 / 0.2 / 3 | Sfx |
| Sfx_Jump (4) | `jump.ogg` (stereo, 0.47 s) | 0.6 / 1 / 1 / 0.1 / 2 | Sfx |
| Sfx_Land (5) | `land.ogg` (stereo, 0.51 s) | 0.6 / 0.95-1.05 / 1 / 0.1 / 2 | Sfx |
| Sfx_PickupAmmo (6) | `pickup_ammo.ogg` (mono, 0.47 s) | 0.8 / 1 / 2 / 0.1 / 2 | Sfx |
| Sfx_PickupCoin (7) | `pickup_coin_1.ogg` (stereo **48 kHz**, 0.85 s), `pickup_coin_2.ogg` (48 kHz, 0.34 s) | 0.5 / 0.95-1.25 / 4 / 0.04 / 0 | Sfx |
| Sfx_UiFocus (8) | `ui_focus_1|2.ogg` (mono, 0.02 s each) | 0.35 / 1 / 2 / 0.03 / 1 | Ui (bus 1) |
| Sfx_UiConfirm (9) | `ui_confirm.ogg` (stereo, 0.04 s) | 0.6 / 1 / 2 / 0.05 / 2 | Ui |
| Sfx_UiBack (10) | `ui_back.ogg` (mono, 0.06 s) | 0.6 / 1 / 2 / 0.05 / 2 | Ui |
| Sfx_UiBuy (11) | `ui_buy.ogg` (mono, 0.54 s) | 0.7 / 1 / 2 / 0.1 / 2 | Ui |
| Sfx_UiEquip (12) | `ui_equip.ogg` (stereo 48 kHz, 0.26 s) | 0.7 / 1 / 2 / 0.1 / 2 | Ui |
| Sfx_UiError (13) | `ui_error.ogg` (stereo, 0.10 s) | 0.6 / 1 / 2 / 0.1 / 2 | Ui |
| Sfx_StingerRound (14) | `Stingers/stinger_round.ogg` (stereo, 0.64 s) | 0.8 / 1 / 1 / 0 / 3 | Announcer (bus 2) |
| Sfx_StingerBoss (15) | `stinger_boss.ogg` (stereo, 0.80 s) | 0.85 / 1 / 1 / 0 / 3 | Announcer |
| Sfx_StingerClear (16) | `stinger_clear.ogg` (stereo, 0.80 s) | 0.8 / 1 / 1 / 0 / 3 | Announcer |
| Sfx_StingerGameOver (17) | `stinger_gameover.ogg` (stereo, 0.39 s) | 0.8 / 1 / 1 / 0 / 3 | Announcer |
| Sfx_Countdown (18) | `countdown.ogg` (mono, 0.05 s) | 0.6 / 1 / 1 / 0.2 / 2 | Announcer |
| Sfx_CountdownGo (19) | `countdown_go.ogg` (mono, 0.52 s) | 0.7 / 1 / 1 / 0.2 / 2 | Announcer |
| Sfx_ShotBasic (via `Ammo_Basic` and `Ammo_TestSpare` .fireSound) | `shot_basic_1|2|3.ogg` (stereo, 0.24 / 0.25 / 0.34 s) | 0.45 / 0.95-1.05 / 4 / 0.05 / 1 | Sfx |
| Sfx_ShotShotgun (`Ammo_Shotgun`) | `shot_shotgun_1|2.ogg` (mono, 0.72 / 0.74 s) | 0.6 / 0.9-1 / 3 / 0.08 / 1 | Sfx |
| Sfx_ShotLaser (`Ammo_Laser`) | `shot_laser_1|2.ogg` (mono, 1.23 / 1.02 s) | 0.35 / 1 / 2 / 0.14 / 1 | Sfx |
| Sfx_ShotGatling (`Ammo_Gatling`) | `shot_gatling_1|2|3.ogg` (mono, 0.24 / 0.24 / 0.26 s) | 0.3 / 0.95-1.1 / 4 / 0.04 / 1 | Sfx |
Totals: 23 SfxData assets: 11 on bus Sfx (0), 6 on Ui (1), 6 on Announcer (2) [grep of `bus:`]. Observation: `enemy_death_2.ogg` is 4.71 s long (larger than any other effect, probably includes a tail) [measured]. No ambient/crowd loop, no voice/announcer lines, no footsteps/hit-confirm variations beyond the above.

### Mixer `A/Audio/VoxMixer.mixer` [code]
Groups: **Master** (children: Music, Sfx); **Music**; **Sfx** (children: Ui, Announcer); **Ui**; **Announcer**. Exposed parameters (3): `MasterVolume`, `MusicVolume`, `SfxVolume` (dB; Settings sliders drive them; `AudioLibrary` holds the Music/Sfx/Ui/Announcer group refs). No effects/snapshots beyond the default `Snapshot`. Ducking is done in code (`AudioDirector`), not by mixer snapshots.
Spec for finals: SFX mono Vorbis q0.6 decompress-on-load preload; music stereo Vorbis q0.5 streamed; keep clip lengths short for rapid-fire cues (voice limits assume short shots); keep file names or reassign clips on the `SfxData`/`AudioLibrary` assets (no sound is named in code).

---------------------------------------------------------------------
# UNKNOWN / NOT DETERMINED
---------------------------------------------------------------------
- Current pass/fail of the full EditMode suite today: no result file in the repo; last recorded 344/355 (S1). L1 result not recorded. I did not run Unity.
- Whether the 11 failing tests also fail inside Unity when Play mode has been entered (BUGS.md says only "from a fresh Editor").
- Per-platform texture/audio import overrides (WebGL, Android, iOS): only default-platform keys were read.
- Whether any asset is referenced through `Resources.Load`/Addressables/code paths not visible as GUIDs (so "unreferenced" files might still be used); `Resources/` holds only `GameConfig.asset`.
- Exact on-screen pixel sizes of the characters and the arena at 1080p (camera/letterbox math not traced; only the ~129 px/unit derivation above, labelled ESTIMATE); the spec's 126.5 px player height claim is [docs].
- Whether VoxKit art is considered final by the developer (docs imply done for the demo; mock mismatches listed).
- Source of the arm sprites' master files (no `ArtSource/Arms`), and the original Procreate masters for UI kit/Parts (not in repo).
- Whether the shipped sound clips actually sound right (CREDITS says picks were made without listening tests); `music_menu.wav` loop seamlessness unknown.
- Playtest status of rounds 4-7 balance and of Sentry/Charger/Sniper behaviour in real play (telemetry CSVs are not in the repo; only code + tests).
- Effort numbers for the remaining engineering items are ESTIMATES with no repo evidence beyond the code shape.

---

# SECTION 4 - Velocity

# SECTION 4 - Velocity (verified 2026-10-01 at HEAD 484781b, working tree: only ProjectSettings/QualitySettings.asset modified)

## 4.1 Git facts
- Branches: only `main` (+ `origin/main`). `git rev-list --all --count` = 61; `git rev-list --count HEAD` = 61. Total commits = **61** (all branches = main). Source: `git rev-list`, `git branch -a`.
- Authors: single author "Cyan" (`git shortlog -s`). Trailers (`git log --format='%(trailers:key=Co-Authored-By,valueonly,separator=)'`): 5x "Claude Opus 5.5", 42x "Claude Sonnet 5.5", 14 commits with no trailer (c581c13 bececa0 8800221 5b356f7 6d96071 8cefcd0 9d5fef6 921ffae 4bfa35d 6c30daa 7bfb75e c3fc59a dbd3019 4300774; the list printed by awk). 5+42+14 = 61.
- First commit: c581c13 "Initial Unity 6.3 project", author date = commit date = 2026-09-28 13:23:24 -0500.
- Last commit: 484781b "S1: demo scope ...", author date = commit date = 2026-09-30 21:24:16 -0500 (L1 1a1a93e has the same minute 21:24).
- Author date == commit date for every commit (`git log --format='%ad|%cd'` compared by day; per-day counts by %ad and %cd are identical). No rebases/amends visible.
- Timezone: every commit carries offset -0500 (US Central, CDT). All times below are local -0500. A commit dated 2026-09-30 00:05 (a3a8740, M9d) belongs to the late-night tail of the 09-29 working session by wall clock but counts as 09-30 in git.
- Wall-clock span: 2026-09-28 13:23 -> 2026-09-30 21:24 = 56 h 01 min (calendar span, including nights).
- Active days (distinct author dates): **3** (09-28, 09-29, 09-30). Distinct (day, hour) buckets with a commit: 28.
- Commits per day: 09-28 = 29; 09-29 = 15; 09-30 = 17 (sum 61). (`git log --format=%ad --date=format:%Y-%m-%d | sort | uniq -c`)
- Day 1 (09-28): first commit 13:23, M0..M8 all landed 14:20..22:33 (about 9 h). Busiest hour-cluster: 09-30 16:16 had 2 commits (D2 4368fd4, D3 59b33c7); 09-30 21:24 had 2 (L1, S1); 09-28 14:51-14:52 had 3.
- Lines (`git log --shortstat --format=`, summed): 3,726 file-changes (not unique files), **+453,094 / -121,200** lines. INCLUDES generated YAML (scenes/prefabs/assets), third-party UI pack (dbd3019: 332 files +24,626), fonts, packages, art. Not a measure of hand/agent-written code.
- Tracked files at HEAD: 1,928 (`git ls-files | wc -l`); tracked .cs under Assets: 317.

## 4.2 Commits per milestone prefix (subject-line parse)
Prefix counts (`git log --format=%s` first token): M0 3, M1 4, M1.5 1, M2 1, M3a 1, M3b 1, M4 1, M5a 1, "M5" 2 (0e117c7 "M5: waves..." = M5b work; 921ffae "M5 done; design"), M6 1, M7 1, M7.5 1, M7.6 1, M7.7 2, M8 2, M8.5 1, M8.6 1, "Scale" 3 (15b44f0 "Scale test:", c393055 "Scale test fixes", f74d79b "Scale lock 1.15x, UI1, CC1"), C1 1 (245a1f0), M9a 1, M9b 1, M9c 1, M9d 1, M10 1 (1a79436, includes P1 partial), UI2 1, PF1 2 (3043af8, 25bd415), D1..D6 1 each, L1 1, S1 1. Non-milestone prefixes: Docs 4, Art 4, Design 2, plus unprefixed: Initial/require/add/Updated/Refresh.

## 4.3 CLAUDE.md [x] milestones mapped to commits (author date, -0500)
| CLAUDE.md item | State | Commit(s) | Date/time |
|---|---|---|---|
| Setup | [x] | c581c13, bececa0 | 09-28 13:23, 13:46 |
| M0 | [x] | f92fefd, 480dc6a, tick 150865a | 09-28 14:11, 14:20, 14:52 |
| M1 | [x] | 5b356f7 (WIP), c52770a, tick 2addb41, 6d96071 | 09-28 14:37-15:04 |
| M1.5 | [x] | 4a707ff | 09-28 15:36 |
| M2 | [x] | NO commit with an "M2:" prefix. Only 8cefcd0 "M2 done; M3 design: ammo slots, pickups, etc" (09-28 16:05, no trailer); it adds FireTimerTests/HealthTests per DevLog (UNVERIFIED by me) | FLAG |
| M3a | [x] | 578c096 | 09-28 16:27 |
| M3b | [x] | 4b0398d (and 9d5fef6 "Design: game flow..." 16:41) | 09-28 16:54 |
| M4 | [x] | ced81c5 | 09-28 17:44 |
| M5a | [x] | 295524f | 09-28 18:07 |
| M5b | [x] | 0e117c7 ("M5:" prefix, not "M5b"), 921ffae | 18:23, 18:32 |
| M6 | [x] | 9864589 | 09-28 19:00 |
| M7 | [x] | 28a5d2c (+4bfa35d concept art) | 09-28 19:37 |
| M7.5 | [x] | ce21bf8 | 09-28 21:07 |
| M7.6 | [x] | 767e3ca | 09-28 21:25 |
| M7.7 | [x] | 2ba5edb, d1a53f0 (design 6c30daa) | 09-28 21:51 |
| M8 | [x] | 6fa1892, tick 52acd7b | 09-28 22:32-22:33 |
| M8.5 | [x] | 34bbe88 | 09-29 12:46 |
| M8.6 | [x] | 64b7eee | 09-29 14:34 |
| Art scale test | [x] | c3fc59a, 15b44f0, c393055, f366fc7, 637d92b (no single "Art scale test" subject) | 09-29 15:14-18:24 |
| C1 | [x] | 245a1f0 | 09-29 18:47 |
| Scale lock | [x] | f74d79b | 09-29 20:02 |
| UI1 | [x] | f74d79b (same commit as Scale lock and CC1; no UI1-only commit) | 09-29 20:02 |
| UI2 | [x] | 799d55b | 09-30 14:17 |
| CC1 | [x] | f74d79b (shared) | 09-29 20:02 |
| M9a | [x] | ee19099 | 09-29 21:27 |
| M9b | [x] | 5ef5e62 | 09-29 21:55 |
| M9c | [x] | 4711107 | 09-29 22:49 |
| M9d | [x] | a3a8740 | 09-30 00:05 |
| M10 (round 3) | [x] | 1a79436 (also contains P1 partial) | 09-30 12:04 |
| D1 | [x] | b76b5ee | 09-30 15:37 |
| D2 | [x] | 4368fd4 | 09-30 16:16 |
| D3 | [x] | 59b33c7 | 09-30 16:16 |
| D4 | [x] | c379716 | 09-30 16:52 |
| D5 | [x] | 1835475 | 09-30 17:11 |
| D6 | [x] | 3f643bc | 09-30 18:52 |
| S1 | [x] | 484781b (HEAD) | 09-30 21:24 |
| L1 | [x] | 1a1a93e | 09-30 21:24 (same minute as S1; L1 committed first although CLAUDE.md lists S1 before L1) |
Not [x]: Vertical slice art [ ]; M10 rounds 5/7 [ ]; P1 [~] partial (committed in 1a79436 with M10); PF1 [ ] in CLAUDE.md BUT has 2 commits (3043af8 "PF1: case study documentation system" 09-30 14:49; 25bd415 "PF1: model attribution..." 14:55) - CLAUDE.md state looks stale/inconsistent with git; M11 [ ]; M12 [ ].
Flags: [x] with no exact-prefix commit: M2 (only 8cefcd0). [x] sharing one commit: Scale lock+UI1+CC1 (f74d79b); M10+P1 (1a79436); L1/S1 same minute. Commits without a milestone prefix (non-tick): c581c13, bececa0, 8800221 "Updated controls", 4bfa35d, 6c30daa (Design), 9d5fef6 (Design), 7bfb75e, c3fc59a (Art), f366fc7 (Docs), 637d92b (Art), dbd3019 (UI pack), 2c8407f (Docs), 4300774 (Art), fbdc702 (Docs), d8e48d5 (Art), 637a720 (Docs: D1-D6 track), 921ffae ("M5 done; design", counted under M5), 6d96071/8cefcd0 ("M1 done"/"M2 done" human-style).

## 4.4 Code size (HEAD; `find ... -name '*.cs'` + `xargs cat | wc -l`; raw lines incl. blanks/comments; .meta excluded; no third-party .cs under these folders)
Assets/Scripts: 244 files, 28,322 lines. By folder (files/lines): AI 15/1675; Arena 19/2278; Armory 9/1669; Audio 6/303; Bosses 5/940; Core 17/1440; Cosmetics 5/391; Enemies 18/1759; Feedback 14/1374; Input 8/2525; Perf 7/1117; Pickups 6/467; Platform 2/158; Player 15/1528; Projectiles 4/766; Save 6/467; Settings 3/241; Shop 11/1555; Telemetry 6/1057; UI 44/4968; Weapons 24/1644. (None directly in Assets/Scripts.)
Assets/Editor: 41 files, 12,751 lines. Assets/Tests: 32 files, 6,065 lines (all in Assets/Tests/EditMode).
Tests: `[Test]` = 337, `[TestCase` = 20, `[UnityTest]` = 0 (grep -rE on Assets/Tests). No PlayMode tests. Largest files by attribute count: ArmSelectorTests 34, ArenaTests 21, M9aTests 20, M9bTests 20, M6Tests 17.
ScriptableObject classes: `grep -rhE 'class \w+ *: *ScriptableObject' Assets --include=*.cs` = 43 direct subclasses (does not count indirect subclasses like ArmEffect children; a looser grep found 56 files, so true count is 43-56: UNKNOWN exact, other agent covers).
.asset files: Assets/Data = 168 (Waves 31, Enemies 24, Audio 24, Arenas 17, Cosmetics 16, Armaments 10, Feedback 9, Effects 6, Arms 6, Ammo 5, UI 3, Shop 3, Player 3, Settings 2, Pickups 2, Loadouts 2, Bosses 2, Input 1, + DemoConfig.asset, AssetRegistry.asset at top level); all of Assets = 183.
Prefabs: 25 (all Assets, `find Assets -name '*.prefab'`). Scenes: 5 .unity files = Boot, MainMenu, Game, RigTest, Settings/Scenes/URP2DSceneTemplate (3 in build flow per CLAUDE.md). Shader graphs: 12.

## 4.5 Docs/BUGS.md
- Statuses are not a field; they are marked by SECTION: `## Open` and `## Fixed`, plus per-entry `**Fixed (date, milestone): note**` lines and `**Status:**` lines on open items.
- Open: 6 entries (UI2 mockup differences; "Destroy may not be called from edit mode"; P1 hitches (TABLED); 11 EditMode tests fail outside Play mode; Input System NRE (can't reproduce, monitoring); Sprite_Character materials disable 2D SRP batching (deferred to M12)).
- Fixed: 14 entries (one is "Example: Ricochet bullets pass through low walls" = the format example, closed "could not reproduce, works"). `grep -c '^### '` = 20 total = 6 + 14.
- Last touched in commit 484781b (2026-09-30).

## 4.6 P1 performance
- CLAUDE.md: P1 = `[~]` PARTIAL. Tools built (overlay F8, stress test F10 / `-perfstress`, PerfLogger CSV, Tools/perf_analyze.ps1, Assets/Scripts/Perf = 7 files/1117 lines). Fixes and final test TABLED. Committed in 1a79436.
- BASELINE ("BEFORE") only, from Docs/BUGS.md "P1 hitches" and CLAUDE.md: Development build, `-perfstress -perfvsync 0`, 80-enemy swarm + Pumpking phase 2 + 8 arms, 90 s, analysed with `-From 10`: avg ~165 fps uncapped; p99 13.5-14.2 ms; 3-37 frames >20 ms per 80 s run (run 1: 37; runs 2-3: 3-4); worst frame 27-74 ms; GPU <1 ms; BH profiler markers ~1-3 ms/frame (Enemy.Brain ~1.0, Nav.Separation ~0.5, Bullet.Update ~0.35, Physics2D ~0.17); ~180 GC allocs (~8 KB) per frame; hitch cause unattributed.
- AFTER: **NONE.** No post-fix numbers exist; no P1 fixes were made (BUGS.md checklist "Resume checklist" still lists them as to-do). Docs/CaseStudy/09_Metrics.md:271-283 has an explicitly EMPTY after column.
- Raw CSVs: PerfLogs folder `%USERPROFILE%\AppData\LocalLow\DefaultCompany\VoxVegetallis\PerfLogs` does NOT exist on this machine (checked), so the baseline numbers cannot be re-verified from CSV (UNVERIFIED beyond doc text).
- Related (Docs/WEBGL_REPORT.md:25-29, not P1 before/after): WebGL dev build 80 enemies + boss + 8 arms = 22 fps, p99 61 ms, worst 109 ms; Windows same test ~165 fps (the P1 baseline); release round 1 combat 236 fps avg, p99 6.1 ms (240 Hz cap).
- Captures/before and Captures/after exist but are D1 UI polish screenshots (banner_*), not performance.

## 4.7 CONTRADICTIONS in Docs/CaseStudy (folder exists: 01_CaseStudy, 02_Architecture, 03_GameFlow, 04_Systems/01-09, 05_DevLog, 06_Roadmap, 07_AgenticWorkflow, 08_ArtPipeline, 09_Metrics, 10_Reflections). Main cause: docs are a snapshot at commit 799d55b (50 commits); HEAD has 61. I only read 09_Metrics fully; grep-scanned the others.
- 01_CaseStudy.md:13 "50 commits" -> verified 61 (span 09-28..09-30 correct).
- 05_DevLog.md:3 "50 commits" -> 61. 05_DevLog.md:29-35 trailer counts "5 Opus, 31 Sonnet" (50 total) -> now 5 / 42 / 14 untagged (61).
- 09_Metrics.md:3 snapshot commit 799d55b (stale by 11 commits: PF1x2, docs 637a720, D1-D6, L1, S1).
- 09_Metrics.md:15-34 runtime table (221 files / 24,527 lines) -> 244 / 28,322; UI 38/3,927 -> 44/4,968; Input 6/2,083 -> 8/2,525; Core 15/955 -> 17/1,440; Feedback 13/1,170 -> 14/1,374; Enemies 1,730 -> 1,759; Cosmetics 335 -> 391; Settings 195 -> 241; plus new folders omitted: Audio (6/303), Telemetry (6/1,057).
- 09_Metrics.md:66-67 Editor 34 files/11,259 -> 41/12,751 (the "Editor" find there used -path '*Editor*', different method); Tests 26 files/4,991 -> 32/6,065.
- 09_Metrics.md:75-76 test files 26, [Test] 285 -> 32 files, 337 (TestCase 20 unchanged). 09_Metrics.md:81 "ArmSelectorTests 21" -> 34 (also ArenaTests 21 ok, "FireTimerTests 3" -> 7 by attribute count incl. TestCase; UNCERTAIN, my count includes [TestCase]).
- 09_Metrics.md:83 test ratio 0.20 -> 6,065/28,322 = 0.21 (my arithmetic).
- 09_Metrics.md:95-117 Assets/Data 142 -> 168; whole-project .asset 157 -> 183 (new Audio folder 24 assets; Data/Audio absent from the table; UI 2 -> 3; DemoConfig new). Waves 31 / Enemies 24 / Arenas 17 / Cosmetics 16 unchanged.
- 09_Metrics.md:125 prefabs 23 -> 25. (Scenes 3+2, shader graphs 12 confirmed.)
- 09_Metrics.md:138-143 per-day table 09-30 = 6, total 50 -> 17, total 61. 09_Metrics.md:148-150 chart bar [29,15,6] -> [29,15,17].
- 09_Metrics.md:153 "5 Opus, 31 Sonnet, 14 untagged" -> 5 / 42 / 14.
- 09_Metrics.md:187 "PF1 0 commits, uncommitted" -> 2 PF1 commits (3043af8, 25bd415).
- 09_Metrics.md:205 "last commit 2026-09-30 14:17, about 49 hours" -> last commit 21:24 (484781b), 56 h.
- 09_Metrics.md:213 Fixed entries 10 -> 14 now (new: S1 overlays, L1 buttons, D6 WebGL saves, D1 polish). Open 6 unchanged.
- 09_Metrics.md:240-243 CLAUDE.md 497 lines -> 585; BUGS.md 156 -> 185; ART_CHECKLIST 203 -> 246; CREDITS 30 -> 82; new Docs/WEBGL_REPORT.md 157; sum 1,742 -> 2,111; 09_Metrics.md:252 CLAUDE.md "changed in 33 commits" -> 42.
- 09_Metrics.md:267 (and 04_Systems/08_SaveSystem.md:136 acknowledges) perf CSV folder `BulletHell_Meats&Sweets` -> current product name folder is `VoxVegetallis` (per CLAUDE.md and BUGS.md).
- 05_DevLog.md:333 (under "M9c - Armory screen") the line "Model: commit trailer says Claude Sonnet 5.5, but M10 planning ... Fable 5.1" is an M10 note misplaced in the M9c entry; also lines ~57-58 and 68-72 appear to be misplaced fragments inside/after the transcript table (formatting, I only saw grep output).
- 06_Roadmap.md:146 heading "D1 done 2026-09-30" -> D1-D6, S1 and L1 are all [x] and committed 09-30.
- 05_DevLog has no entries for D1-D6, L1, S1 (not a contradiction; stale/missing; DevLog headings end at M10 per grep head -120 truncated; UNVERIFIED for entries after line 368).
- UNVERIFIED (not checked): 05_DevLog.md:38-72 session transcript/model/time claims (Fable 5.1 window 09:28-10:44, session ids, 1167 turns, record counts); I was told not to read transcripts. 09_Metrics.md:193-199 largest-commit file/insertion counts; 09_Metrics.md:209-228 bugs-per-commit history; 09_Metrics.md:252 first CLAUDE.md = 103 lines.
- Checked OK: 09_Metrics.md:140-141 per-day counts 29 and 15 (09-28/09-29); 09_Metrics.md:79 and 217 open-bug facts; DevLog times for M0-M10 commits (all match git log above); 12 shader graphs; 3+2 scenes; 20 TestCase.

## 4.8 UNKNOWN / not determined
- Exact ScriptableObject class count (43 direct vs indirect subclasses); another agent covers.
- Hands-on hours / session count / which model wrote what: not derivable from git (only trailers); transcripts not read per instruction.
- Any post-fix performance numbers (none exist); raw PerfLogs CSVs (folder absent).
- Lines of hand-written vs generated code; shortstat totals include generated files and double-count files across commits.
- Whether commits with no trailer (14) were agent-made or hand-made (DevLog claims some are; not provable from git).
- Net lines at HEAD for whole repo (not computed; cheap with `git ls-files | xargs wc -l` but includes assets/YAML).

---

# SECTION 4b - Claude Code session transcripts (approved by the user in this session)

# Section 4: Session transcripts (read-only analysis)

Source: `C:\Users\moeez\.claude\projects\C--Dev-BulletHell\*.jsonl` (+ `<sessionid>\subagents\agent-*.jsonl`). Parsed with a PowerShell regex extractor (`scratchpad\extract.ps1`; intermediates ts.tsv, prompts.tsv, asst.tsv). Local time = UTC-5 (matches the -0500 offsets in git log). All timestamps are the record field `timestamp`.

## 1. File counts
- 64 `.jsonl` files total: **23 main sessions** (top-level, filename = sessionId) + **41 subagent sidechain files** (in `<sessionId>\subagents\agent-*.jsonl`). Distinguished by directory (subagents folder); the `isSidechain` field was not needed. Subagent files per parent: 25305cab 6, 9a558f0e 8, 3cb55dc3 7, 7ff0c1b1 6, 745ad93a 3, 1af0887c 2, 58b753f1 2, a38336c3 2, abae63f2 2, 10dfe826 1, 53252171 1, bb035825 1.
- **Duplicate:** 9a558f0e and 760a494d are near-identical transcripts (1472 of ~1475 record uuids shared, same prompts, same 09-30 14:00Z start). Treated as ONE session (M10/P1); 9a558f0e excluded from all totals below. Which is the "original" is UNKNOWN (the 8 subagent files sit under 9a558f0e, none under 760a494d).
- Two sessions contain no assistant turns (ee1537f6: only /exit; 8958630f: only /clear). 25305cab = current session (in progress, shown separately).
- 3 more empty-ish: none. Transcripts also contain `/clear`, `/exit`, `/model` command records and "session continued" summaries; these are counted as "other user records", not prompts.

## 2. Per-session table (main sessions, chronological)
User prompts = `type:user` records with `origin.kind == "human"` (excludes tool results, task notifications, meta, command records). Assistant turns = unique `message.id` (records repeat per content block). Tokens = sum over unique message ids of `usage` fields (output = max seen per id). "Other" = non-human user text records (slash commands, continuation summaries).

| id | start local (UTC) | end local (UTC) | wall h | prompts | other | asst turns | models (turns) |
|---|---|---|---|---|---|---|---|
| ee1537f6 | 09-28 13:44 (18:44) | 13:44 (18:44) | 0 | 0 | 2 | 0 | none |
| 489b827c | 09-28 13:47 (18:47) | 14:25 (19:25) | 0.63 | 10 | 12 | 54 | sonnet-5-5 38, opus-5-5 16 |
| 7c9ff7db | 09-28 14:38 (19:38) | 14:52 (19:52) | 0.24 | 5 | 2 | 35 | opus-5-5 35 |
| 8958630f | 09-28 14:53 (19:53) | 14:53 | 0 | 0 | 1 | 0 | none |
| 4466eaf4 | 09-28 15:10 (20:10) | 15:36 (20:36) | 0.44 | 5 | 0 | 45 | opus-5-5 45 |
| 53252171 | 09-28 15:37 (20:37) | 16:01 (21:01) | 0.41 | 2 | 3 | 45 | sonnet-5-5 45 |
| 10dfe826 | 09-28 16:05 (21:05) | 16:22 (21:22) | 0.28 | 2 | 0 | 48 | sonnet-5-5 48 |
| 235077d1 | 09-28 16:26 (21:26) | 16:32 (21:32) | 0.10 | 2 | 1 | 26 | sonnet-5-5 26 |
| 9c330260 | 09-28 16:42 (21:42) | 18:23 (23:23) | 1.68 | 11 | 0 | 186 | sonnet-5-5 186 |
| abae63f2 | 09-28 18:33 (23:33) | 19:37 (00:37 +1d) | 1.07 | 5 | 0 | 177 | sonnet-5-5 177 |
| 47ba1452 | 09-28 19:46 (00:46 09-29) | 21:29 (02:29) | 1.72 | 6 | 4 | 171 | sonnet-5-5 171 |
| 58b753f1 | 09-28 21:31 (02:31 09-29) | 22:33 (03:33) | 1.03 | 9 | 0 | 125 | sonnet-5-5 125 |
| a38336c3 | 09-29 12:23 (17:23) | 13:05 (18:05) | 0.70 | 3 | 0 | 60 | sonnet-5-5 60 |
| bb035825 | 09-29 13:11 (18:11) | 15:06 (20:06) | 1.91 | 3 | 0 | 237 | sonnet-5-5 237 |
| 1af0887c | 09-29 15:19 (20:19) | 16:22 (21:22) | 1.05 | 4 | 0 | 141 | sonnet-5-5 141 |
| bd0d9026 | 09-29 16:23 (21:23) | 16:24 (21:24) | 0.01 | 1 | 0 | 4 | sonnet-5-5 4 |
| 745ad93a | 09-29 16:25 (21:25) | 18:25 (23:25) | 1.99 | 8 | 1 | 152 | sonnet-5-5 152 |
| 3cb55dc3 | 09-29 18:34 (23:34) | 09-30 00:05 (05:05) | 5.52 | 28 | 1 | 590 | sonnet-5-5 590 |
| 760a494d | 09-30 09:00 (14:00) | 12:06 (17:06) | 3.10 | 10 | 6 | 167 | sonnet-5-5 112, fable-5-1 55 |
| (9a558f0e) | duplicate of 760a494d, excluded | | | | | | |
| 7ff0c1b1 | 09-30 12:15 (17:15) | 15:09 (20:09) | 2.90 | 11 | 0 | 309 | sonnet-5-5 309 |
| c90053ce | 09-30 15:13 (20:13) | 21:25 (02:25 10-01) | 6.20 | 20 | 2 | 552 | sonnet-5-5 552 |
| **25305cab (current, in progress)** | 10-01 13:33 (18:33) | 13:37 (18:37) | 0.07 | 4 | 0 | 7 | sonnet-5-5 7 |

Totals excluding the duplicate and the current session (21 main files, 2 of them empty): wall (sum of first-to-last) 30.98 h; 145 human prompts; 3124 main-session assistant turns. Subagent transcripts: 362 assistant turns (excludes 9a558f0e's 8 files and the current session's 6); across all 41 subagent files incl. those, 485 sub turns (sonnet 433, opus 40, fable 10, haiku 2).

Tokens (sum of `usage` fields, unique message ids), main sessions excl. duplicate/current: input 7,782; cache_creation 9,864,010; cache_read 1,120,886,125; output 3,642,122. Subagent files (same exclusions): input 740; cache_creation 3,134,523; cache_read 27,911,970; output 98,526. Per-session (input / cache_creation / cache_read / output): 489b827c 108/179,166/4,999,640/48,925; 7c9ff7db 70/91,086/3,274,641/26,460; 4466eaf4 90/119,912/5,136,807/40,512; 53252171 92/119,518/5,314,737/46,957; 10dfe826 102/126,932/5,724,025/54,743; 235077d1 52/100,606/2,460,055/30,076; 9c330260 372/522,434/58,444,435/345,762; abae63f2 356/431,281/51,970,360/219,829; 47ba1452 342/455,221/55,496,971/254,688; 58b753f1 256/376,519/31,984,689/175,649; a38336c3 124/213,322/10,592,816/69,559; bb035825 474/570,010/58,938,768/196,686; 1af0887c 286/262,861/26,716,480/99,162; bd0d9026 8/47,037/242,394/2,840; 745ad93a 312/254,108/29,923,175/90,551; 3cb55dc3 1,194/1,353,853/245,441,360/669,794; 760a494d 1,796/1,244,342/59,466,583/317,503; 7ff0c1b1 628/720,404/146,083,417/338,935; c90053ce 1,120/2,675,398/318,674,772/613,491; current 25305cab 16/52,684/508,340/7,426. ("Input" is tiny because nearly everything is cache; do not read it as total context.) Output tokens may include thinking; cost not computed (UNKNOWN pricing).

## 3. ACTIVE time (ESTIMATE)
Definition: sort all `user`/`assistant` record timestamps within a session; sum the gaps between consecutive records that are <= T minutes; gaps > T are treated as idle. Subagent files not added (they run inside the main session's gaps). Duplicate and current session excluded. Includes time waiting on tool runs/the agent (not only the user typing) so it is "session busy or conversational" time, not hands-on-keyboard time.

| threshold T | sum of per-session active h | merged-timeline active h (all sessions on one timeline) |
|---|---|---|
| 10 min | 23.58 | 24.47 |
| 30 min | 27.86 | 29.80 |
| 60 min | 30.99 | 32.93 |

Merged is higher because gaps between consecutive sessions <= T (e.g. /clear then new session) are counted; sessions did not overlap otherwise (except the 760a/9a55 duplicate).

Per-day (local date, gap attributed to the day of its start), per-session-sum method:
- T=10: 09-28 6.59 h, 09-29 8.25 h, 09-30 8.74 h (total 23.58)
- T=30: 09-28 6.93 h, 09-29 9.95 h, 09-30 10.99 h (total 27.86)
- T=60: 09-28 7.60 h, 09-29 11.17 h, 09-30 12.22 h (total 30.99)
Merged-timeline per-day at T=30: 8.14 / 10.47 / 11.19. Calendar span: 09-28 13:44 to 09-30 21:25 local (~2.3 days); no activity 09-28 22:33 to 09-29 12:23 and 09-30 00:05 to 09:00 (overnight gaps).
Current session 25305cab: 4 prompts, 7 turns, ~0.07 h, in progress, excluded.

## 4. Milestone to session map
Basis codes: C = git commit timestamp falls inside the session's assistant-turn range (assistant turns in the window between the previous commit and this commit were counted per session); P = the session's first/early human prompt names the milestone ("We're doing Xn from CLAUDE.md"); K = milestone tokens in human prompts (counted by regex). Commit times are local.

| milestone | session(s) | commit | basis / flag |
|---|---|---|---|
| Setup / M0 | 489b827c (then 7c9ff7db for tick) | f92fefd 14:11, 480dc6a 14:20; tick 150865a 14:52 | C+P; clear |
| M1 | 489b827c (build), 7c9ff7db (rework to new Controls spec) | c52770a 14:51 (+WIP 5b356f7) | C+P+K; M1 straddles two sessions |
| M1.5 | 4466eaf4 | 4a707ff 15:36 | C+P; clear |
| M2 | 53252171 | no own commit; included in 8cefcd0 16:05 ("M2 done; M3 design") | P+C window; no M2-labelled commit |
| M3a | 10dfe826 (first), 9c330260 (prompt repeated 21:42Z) | 578c096 16:27 | C+P; AMBIGUOUS: M3a prompt reissued in 9c330260 |
| M3b | 235077d1 (started), 9c330260 (M3b x2 in prompts) | 4b0398d 16:54 | C+K; AMBIGUOUS between the two |
| M4 | 9c330260 (two halves, 2 prompts) | ced81c5 17:44 | C+P+K |
| M5a, M5b | 9c330260 | 295524f 18:07, 0e117c7 18:23 | C+K |
| M6 | abae63f2 | 9864589 19:00 | C+P |
| M7 | abae63f2 | 28a5d2c 19:37 | C+P (prompt "next up is M7") |
| M7.5, M7.6 | 47ba1452 | ce21bf8 21:07, 767e3ca 21:25 | C+P+K |
| M7.7 | 47ba1452 (design commit 6c30daa), 58b753f1 | 2ba5edb 21:51 | C+P |
| M8 | 58b753f1 | 6fa1892 22:32 | C+P |
| M8.5 | a38336c3 | 34bbe88 12:46 (09-29) | C+P |
| M8.6 | bb035825 (+ tail of a38336c3 3 turns) | 64b7eee 14:34 | C+P; K also shows M8.6 in 745ad93a (follow-up fixes) |
| Art scale test | 1af0887c (backdrop, Chaser), 745ad93a (fixes), bd0d9026 (CLAUDE.new.md merge, 1 min) | 15b44f0 16:21, c393055 17:49 | C; no milestone word in prompts; match by commit message |
| C1 | 3cb55dc3 | 245a1f0 18:47 | C+P |
| Scale lock, UI1, CC1 | 3cb55dc3 | f74d79b 20:02 (one commit for all three) | C+P+K; three milestones share one commit (cannot split by commit) |
| M9a, M9b, M9c, M9d | 3cb55dc3 (all four, one 5.5 h session) | ee19099 21:27, 5ef5e62 21:55, 4711107 22:49, a3a8740 00:05 | C+P; clear per prompt |
| M10 (Pumpking) | 760a494d / 9a558f0e (duplicate) | 1a79436 12:04 (09-30) | C+P (prompt "We're doing M10" at 14:28Z) |
| P1 (partial) | 760a494d / 9a558f0e | 1a79436 (same commit as M10) | P (prompt 15:45Z); shares commit with M10 |
| UI2 hot swap | 7ff0c1b1 | 799d55b 14:17 | C+P |
| PF1 | 7ff0c1b1 | 3043af8 14:49, 25bd415 14:55 | C+P |
| Demo track definition (D1-D6 text) | 7ff0c1b1 | 637a720 15:08 | C+P |
| D1 | c90053ce | b76b5ee 15:37 | C+P (7ff0c1b1 contributed 3 turns before) |
| D2, D3 | c90053ce | 4368fd4 16:16:11, 59b33c7 16:16:16 | C+P; committed 5 s apart so turn-window attribution of D3 is 1 turn (ambiguous commit split; prompts clear: D2 20:41Z, D3 20:57Z) |
| D4 | c90053ce | c379716 16:52 | C+P |
| D5 | c90053ce | 1835475 17:11 | C+P |
| D6 | c90053ce | 3f643bc 18:52 | C+P |
| L1 | c90053ce | 1a1a93e 21:24:13 | C+P |
| S1 | c90053ce (prompt 23:53Z, before L1's 00:34Z) | 484781b 21:24:16 | C+P; AMBIGUOUS: S1 and L1 committed 3 s apart, 0 assistant turns attributed to S1 window |
| W1, M10 rounds 5/7, M11, M12, vertical slice art | none | none | not built (CLAUDE.md unchecked; only mentioned in prompts, e.g. M11 as "before M11") |
Non-milestone commits (docs/art, user-made): bececa0, 8800221, 6d96071, 921ffae, 4bfa35d, 7bfb75e, c3fc59a, 637d92b, dbd3019, 4300774, fbdc702, d8e48d5 (Skirmisher art hookup, 760a494d), 2c8407f, f366fc7.

## 5. Model per milestone vs Co-Authored-By trailers
Transcript model = `message.model` of main-session assistant turns in the commit window (counts). Trailer = `Co-Authored-By` on the commit.

| milestone / commit | transcript model | trailer | result |
|---|---|---|---|
| M0 f92fefd (Pipeline pkg) | sonnet-5-5 18 | Sonnet 5.5 | match |
| M0 480dc6a | sonnet 20, opus 3 (rest of the M0 session's opus turns fall in the next window) | Opus 5.5 | PARTIAL: session 489b827c is 38 sonnet / 16 opus; trailer reflects only the later opus part |
| M1 c52770a, ticks 2addb41/150865a | opus 28/3/3 (7c9ff7db) | Opus 5.5 | match (M1 first build in 489b827c was mixed sonnet/opus) |
| M1.5 4a707ff | opus 44 | Opus 5.5 | match |
| M2 (8cefcd0, user commit) | sonnet 45 (53252171) | none | no trailer; transcript = Sonnet |
| M3a 578c096 | sonnet 52 | Sonnet 5.5 | match |
| M3b..M9d and S1 commits | sonnet only | Sonnet 5.5 | match |
| M10 + P1 1a79436 | sonnet 59, **fable-5-1 55** (via /model switch at 09-30 14:28Z, back to Sonnet at 15:44Z) | Sonnet 5.5 | **MISMATCH: no commit carries a Fable trailer** |
| UI2, PF1, D1-D6, L1 | sonnet only | Sonnet 5.5 | match |
| S1 484781b | no turns in its window (shares time with L1) | Sonnet 5.5 | UNKNOWN independent check; the session was Sonnet-only |
Also: user-made commits have no trailer (bececa0, 8800221, 5b356f7, 6d96071, 8cefcd0, 9d5fef6, 921ffae, 4bfa35d, 7bfb75e, c3fc59a, 4300774, ...). Subagents: sonnet 433, opus 40, fable 10, haiku 2 (haiku-4-5 appears only in 2 subagent turns in 7ff0c1b1's PF1 work); no trailer reflects subagent models. Commit 25bd415 message already claims "Opus M1/M1.5, Fable 5.1 on M10 start", consistent with the transcripts.

## 6. Things the agent got wrong / user corrections
Explicit "no, wrong, revert" style corrections are rare (regex found only 2 in 145 prompts); most prompts are long pasted milestone specs. Candidates (quotes shortened; timestamps UTC):
1. 489b827c 09-28 19:14Z: "no, let's build with the contolls being button/controller based only. leave off mouse/WASD for now" (agent had planned KB/mouse inputs).
2. 7c9ff7db 09-28 19:43Z: "in the instance of only 1 arm, any directional selection will activate it" (clarifying soft-select spec after M1 build).
3. 4466eaf4 09-28 20:21Z: "re-exported/made edits to the file in the file explored it's why i got the issue" (orphan .meta warnings; user-caused, but agent had to add step 0).
4. 10dfe826 09-28 21:20Z: "pressing a key on the input pad is not actually selecting between different ammot type" + MissingReferenceException on Projectile (M3a bug found in playtest).
5. 9c330260 09-28 22:01Z: "had to recompile/click in unity. can you continue from where you last left off" (agent stalled mid-task).
6. abae63f2 09-29 00:24Z: "13 errors - most recent error (NullReferenceException ... MenuInputReader.OnDestroy" (M7 regression).
7. 1af0887c 09-29 20:19Z: "TASK 1: Fix this Console error before anything else" (InputEvent.get_handled NullReference).
8. 745ad93a 09-29 21:25Z: "Broken material: a bright magenta square renders at the top-right of one pillar" (M8.6 shaders not compiling for URP 2D).
9. 745ad93a 09-29 23:20Z: "discard the build settings changes (what does this mean?)" (agent's wording unclear).
10. 3cb55dc3 09-30 00:39Z: "The 1.5x character scale from C1 feels too big" (design reversal; temp scale setting re-added, later locked at 1.15x). Not an agent error: playtest outcome.
11. 3cb55dc3 09-30 01:04Z: "Delete the stray Assets/Docs folder" (ESTIMATE: leftover created by an earlier step; cause not verified).
12. 3cb55dc3 09-30 04:26Z: "Decision for the enemy bullet bug: enemy bullets use Electric Violet" (bullet readability bug from M9d bug bash; the Hot Magenta palette appears later in CLAUDE.md, so the colour was changed again: UNKNOWN when).
13. 760a494d 09-30 16:46Z: "it's working well enough when i test, let's table the rest" (P1 performance fixes left partial; usage limit cited).
14. c90053ce 10-01 00:34Z (L1 prompt): "FIX 1 - Character Creation, no part overlays: in the demo, the preview shows only the base player sprite" (plus button focus ring and Armory controller navigation fixes listed; defects in earlier-built UI found during demo testing).
15. c90053ce 09-30 20:13Z (D1 prompt): "Fix the matching issues in Docs/BUGS.md first" (known UI2 mismatches carried into D1).

## UNKNOWN / caveats
- Which of 760a494d / 9a558f0e is the true original: UNKNOWN.
- True hands-on time: UNKNOWN; active time above is an ESTIMATE from record gaps (agent working time included).
- Model per milestone is derived from the commit time window, not from explicit milestone tags in the transcripts; M3a/M3b, D2/D3, S1/L1, UI1/CC1/Scale lock are ambiguous at commit granularity.
- Subagent model use only partially counted in commit windows (9a558f0e's 8 subagent files were excluded as duplicate-owned).
- Cost, context-window size and thinking-token share: UNKNOWN (not derived).
- "Other" user records and prompt counts rely on `origin.kind == "human"`; pasted prompts count as 1.
- Items 11 and 12 in section 6 are inferences labelled ESTIMATE/UNKNOWN; the user's corrections were identified from prompt text only, not from assistant replies.
