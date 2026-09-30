# Credits

Third-party assets used in this project. Add a row here whenever a pack or font goes into the project.

## UI art

The VOX VEGETALLIS UI kit (`Assets/Art/UI/VoxKit`, described by `vox_ui_kit_manifest.json`) is project art, not a third-party
pack. There is no third-party UI sprite pack in the project any more: the earlier "Mega Cozy UI Pack" (dobo_ui) stand-in was
removed in UI2 together with its `Assets/UI/DoboCozy` copies.

## Fonts (all SIL Open Font License 1.1)

Font files and the TextMesh Pro font assets built from them are in `Assets/Fonts/`. The license texts sit beside them
(`OFL_cinzeldecorative.txt`, `OFL_lilitaone.txt`, `OFL_nunito.txt`). All three families are free to use in commercial games and to
embed; they may not be sold on their own.

| Family | Used for | Weights | Author / copyright | Source |
|---|---|---|---|---|
| Cinzel Decorative | screen titles (700), the game logo (900) | Bold, Black | (c) 2012 Natanael Gama | Google Fonts (`google/fonts`, `ofl/cinzeldecorative`) |
| Lilita One | buttons and numbers | Regular | (c) 2011 Juan Montoreano | Google Fonts (`google/fonts`, `ofl/lilitaone`) |
| Nunito | body text (600), small labels (800) | SemiBold, ExtraBold | (c) 2014 The Nunito Project Authors | Google Fonts |

Notes:
- The Google Fonts repository ships Nunito only as a variable font (`Nunito[wght].ttf`), which TextMesh Pro cannot pick weights
  from. The two static weights used here (`Nunito_600SemiBold.ttf`, `Nunito_800ExtraBold.ttf`) come from the `@expo-google-fonts/nunito`
  npm package (static instances of the same OFL family, downloaded 2026-09-30).
- TextMesh Pro font assets: `CinzelDecorative-Bold SDF`, `CinzelDecorative-Black SDF`, `LilitaOne SDF`, `Nunito-SemiBold SDF`,
  `Nunito-ExtraBold SDF` (static atlases, ASCII + Latin-1 + a few symbols). Each falls back to TMP's bundled Liberation Sans SDF
  for glyphs the family lacks (for example the arrow used in stat comparisons).
- `Assets/TextMesh Pro` holds Unity's own TMP Essential Resources (Liberation Sans, shaders, default styles).
