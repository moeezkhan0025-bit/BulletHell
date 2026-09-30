# UI audit (M9d, before the fixes)

Method: code read of every screen + scene grep. "Now" = state before M9d; the M9d column says what was done.

| Screen | Opens focused | Circle | Problem found | M9d |
|---|---|---|---|---|
| Main Menu | Continue (or New Game) | none (root) | Quit game had no confirm; overwrite dialog is its own panel | Quit confirm via shared ConfirmDialog |
| Settings | row 0 | back one level (saves) | no transition, no prompts | transition + prompt line |
| Character Creation | row 0 | back to menu (discards) | no transition, no prompts | transition + prompt line |
| Round Intro | (banner only) | n/a | none | - |
| Round Results | Continue | nothing | quit to menu without confirm; no prompts | confirm, prompts |
| Shop | first card | leaves (spec: "Circle leaves") | hard-coded hint text, no transition | live glyphs, transition, sounds |
| Armory | first arm slot | back one level, none at ring | hard-coded `[Start]`, `Press Cross` text, no transition | live glyphs, transition, sounds |
| Pause | Continue | NOTHING | Circle did not resume; quit to menu unconfirmed | Circle resumes, confirm |
| Game Over | Menu | none (single button) | fine | transition |
| HUD | n/a | n/a | glyphs live-switched only for gamepad family | keyboard family added |

Cross-cutting: no confirm dialog, no shared transition (only FlowPanel had one), UITheme sound slots empty and nothing
played them, glyph family read once per screen open (not live), keyboard had no prompt set.
Third-party sprites: only `Editor/UiThemeSetup.cs` touches the pack; runtime goes through UITheme. Nothing else to fix.
