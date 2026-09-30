# 07 Agentic workflow

How this project was run with Claude Code, what the structure was, and where it helped or failed. Claims I could not confirm from
the repository are marked [VERIFY]. Session transcripts were not read (they live outside the project; see the note in `06_Roadmap.md`).

## 1. CLAUDE.md as a living spec
- `CLAUDE.md` (about 500 lines) is the single source of truth: theme, perspective rules, controls, systems, architecture rules, folder
  layout, working agreement and the milestone list. The agent re-reads it at the start of every session, so anything that matters must
  be written there, not said in chat.
- It evolved with the game: `git log -p -- CLAUDE.md` shows design-first commits ("Design: game flow, save system, shop and armory",
  "Design: elliptical arm ring and bullet height", "Docs: hovering armament bubbles (M9c)") landing *before* the code commits that build them.
- It holds hard rules that steer the agent: all tunables in ScriptableObjects, all projectiles pooled, input only through the generated
  Input Actions class, no platform code outside `Scripts/Platform`, never commit or push without asking, never touch `Library/`.
- `[TBD]` and `[DEFAULT]` markers make unresolved decisions visible so the agent does not silently invent answers (all collected in `06_Roadmap.md`).
- Since 2026-09-30 the agent also maintains the docs (Working agreement: "Claude maintains the project docs ... end the task with a short
  'Docs updated' list"), and from PF1 on, `Docs/CaseStudy` too.

## 2. Plan mode and the milestone loop
1. One milestone per session. The human names it (often by pasting a design block into the prompt).
2. The agent proposes a plan first; the human approves before large changes (Working agreement). Example: for UI2 the agent surveyed the kit, the theme and
   the 44 files that used legacy `Text`, then proposed a plan and asked two questions (convert to TMP? one commit or several?) before touching anything.
3. The agent implements, checks the Unity console for compile errors, runs the EditMode tests, and drives the real editor through the `unity` CLI (section 7).
4. The human playtests ("I playtest every change myself"), reports bugs, and the agent logs them in `Docs/BUGS.md` (reproduce, fix, verify, mark fixed with a
   one-line cause).
5. Commit only on approval, with a message like "M8: ..." and the agent's `Co-Authored-By` trailer.

Milestones were small and named (`M7.6 Jump`, `M8.6 Animation toolkit`), which kept each session inside one context window and each commit reviewable.

## 3. Model selection
Taken from the commit trailers (`git log --format='%h %(trailers:key=Co-Authored-By)'`):

| Milestones | Model |
|---|---|
| M0, M1, M1.5 (skeleton, movement, arm loadout) | Claude Opus 5.5 |
| M3a onward (ammo, armaments, game flow, combat, arena, AI, UI, boss, performance tools, UI2, PF1) | Claude Sonnet 5.5 |
| Art uploads, some design-note commits | human (no trailer) |

The switch to Sonnet after M1.5 is visible in the history; the reason is not recorded there [VERIFY: presumably speed and cost once the architecture was set].

## 4. How the docs were maintained
- Design docs first (`Docs/ARMAMENTS.md`, `SHOP.md`, `ARMORY.md`, `UI_AUDIT.md`), then code, then a "tick the milestone in CLAUDE.md" commit (several are separate commits: "M1: mark milestone complete").
- `Docs/ART_SPEC.md` and `ART_CHECKLIST.md` track art sizes and needs; the agent edits them when a decision changes (for example P=660 to P=506 after the scale lock).
- `Docs/BUGS.md` keeps open bugs with steps, expected, actual, frequency and severity; fixed bugs are removed, so `git log -p -- Docs/BUGS.md` is the history.
- `Docs/CREDITS.md` logs every third-party asset the moment it enters the project.

## 5. Where human judgment was essential
- **Design and feel:** what the game is (candy gladiator in a vegetable colosseum), the aim scheme (two-stick soft select and lock), jump rules, the shop and armory flow.
- **Scale and look:** the character scale was playtested at 0.75x to 1.5x and locked at 1.15x by eye; the agent built the test, the human chose.
- **Art:** all painted art (player, arms, enemies, backdrop, Pumpking, the UI kit and mockups) is human work; the agent hooked it up and never replaced it.
- **Scope control:** deciding what to defer (rigging bosses, performance fixes until content is near complete, touch controls to M12).
- **Permissions:** commits and pushes always needed approval.
- **Playtest verdicts:** only the human can say a jump "feels" wrong or that bullets do not pop against the floor.

## 6. What the agent got wrong, and how it was caught

| What went wrong | How it was caught | Fix / lesson |
|---|---|---|
| Enemy bullets in the same red family as the painted floor (scale test) | human looked at the painted backdrop | bullet palette pass in M9d; rule in CLAUDE.md ("never floor/splatter red", reserved magenta/purple) |
| Ricochet bullets passed through low walls | playtest | logged and fixed; interaction rules documented in `Docs/ARMAMENTS.md` |
| Yellow circle drawn over the player (jump marker); opaque yellow disc hiding the arm in the Armory | playtest / screenshots | fixed in M7.6 / M9d |
| `FeedbackHub` added a new `Application.quitting` handler every Play session; PrimeTween "endValue equals current value" warnings | console noise during testing | logged in BUGS.md and fixed |
| Edit-mode tests break when `Obstacle.ApplyContactShadow` calls `GameServices.Ensure()` (11 failing tests) | agent ran the whole suite during M10 and logged it instead of hiding it | still open; shows the value of running the full suite and of logging unrelated bugs instead of fixing them silently |
| Phone portrait shrinks the UI (found in M9d) | the agent's own UI audit | decision recorded: landscape only |
| TextMeshPro outline set per label created a material instance that went stale when the font changed, drawing garbled text (UI2) | the agent rendered its own screenshot of the wave banner and saw broken glyphs | shared outline material per font in `ThemedText`; lesson in `04_Systems/07_UIThemeSystem.md` |
| A scripted edit of `CLAUDE.md` failed and left the file empty (UI2 session); the restore command was then refused by the permission guard | the agent noticed `git diff --stat` showing 471 deletions | the agent stopped, told the human, prepared the updated file outside the repo, and wrote it only after the human said to; lesson: edit spec files with the Edit tool, never `script > file && cp` |
| Stale test state while driving the editor (a saved run made New Game open a confirm dialog instead of Character Creation) | the output made no sense; the agent inspected state | test helpers now delete the run save first |
| Input System `NullReferenceException` seen once in the Editor | console | could not reproduce; diagnostics added, monitoring |

[VERIFY] the exact sessions in which the early items were introduced; the dates come from the BUGS.md history and commit messages.

## 7. Testing and verification loop (how the agent checks its own work)
- Compile: `unity command recompile`, then check the console for errors before reporting done.
- Tests: `unity command run_tests --mode EditMode` (305 tests; 294 pass, the 11 failures are the known open bug).
- Play through the real flow: Boot, Main Menu, New Game, Character Creation, Combat, Results, Shop, Armory, by invoking button `onClick`s and stepping frames
  from an editor eval (Unity only ticks frames when focused, so the agent pauses and calls `EditorApplication.Step()`).
- Screenshots: `VoxShots` renders the open scene plus overlay canvases to a PNG without window focus, and the agent compares it with the mockup.
- Back up and restore the user's save, settings and profile files before any Play test; delete test artifacts afterwards.

## 8. Practices that worked / did not
Worked: small milestones; design-first docs; hard architecture rules; logging bugs instead of silently fixing; asking before large or outward-facing actions;
screenshot-driven UI work; ScriptableObjects as the single source of truth so numbers can be tuned without touching code.

Did not (or cost time): one-shot regex edits on large files; an editor that only ticks when focused; long runs without a human look (the UI needed several
screenshot rounds); keeping old one-shot setup scripts around (they still build the old look, now logged as a bug).
