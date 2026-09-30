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

## Audio (all CC0 placeholders)

Every sound and track below is released under **CC0 1.0** (public domain dedication): no attribution is required, it is given anyway. Files
were copied into `Assets/Audio` under the names in the tables and imported by `BulletHell/D4/Build Audio` (effects: mono, decoded at load;
music: streamed). The packs' own `License.txt` / `INFO.txt` state CC0 (Kenney: "Creative Commons Zero, CC0"; Juhani Junkala: "released under
CC0 creative commons license. You can do anything you want with these tunes"). Downloaded 2026-09-30. All picks are placeholders chosen without
listening tests: swap any of them by changing the clips on the `SfxData` / `AudioLibrary` assets in `Assets/Data/Audio`.

### Music (Juhani Junkala, CC0)

| Used for | File in project | Original file | Source pack |
|---|---|---|---|
| Main Menu | `Music/music_menu.wav` | `Juhani Junkala [Retro Game Music Pack] Title Screen.wav` | "5 Chiptunes (Action)", https://opengameart.org/content/5-chiptunes-action |
| Combat, odd rounds | `Music/music_combat_1.ogg` | `Juhani Junkala [Chiptune Adventures] 1. Stage 1.ogg` | "4 Chiptunes (Adventure)", https://opengameart.org/content/4-chiptunes-adventure |
| Combat, even rounds | `Music/music_combat_2.ogg` | `Juhani Junkala [Chiptune Adventures] 2. Stage 2.ogg` | same pack |
| Boss rounds | `Music/music_boss.ogg` | `Juhani Junkala [Chiptune Adventures] 3. Boss Fight.ogg` | same pack |
| Round Results, Shop, Armory | `Music/music_shop.ogg` | `Juhani Junkala [Chiptune Adventures] 4. Stage Select.ogg` | same pack |

Author homepage: https://juhanijunkala.com/

### Sound effects and stingers (Kenney, CC0)

Packs: Interface Sounds (https://kenney.nl/assets/interface-sounds), Impact Sounds (https://kenney.nl/assets/impact-sounds), Digital Audio
(https://kenney.nl/assets/digital-audio), Sci-fi Sounds (https://kenney.nl/assets/sci-fi-sounds), RPG Audio (https://kenney.nl/assets/rpg-audio),
Music Jingles (https://kenney.nl/assets/music-jingles). Author: Kenney (https://kenney.nl).

| Used for | File in project | Original file (pack) |
|---|---|---|
| Basic ammo shot (3 variations) | `Sfx/shot_basic_1..3.ogg` | `laserSmall_000..002` (Sci-fi Sounds) |
| Shotgun ammo shot (2) | `Sfx/shot_shotgun_1..2.ogg` | `laserLarge_001..002` (Sci-fi Sounds) |
| Laser ammo (beam tick, 2) | `Sfx/shot_laser_1..2.ogg` | `zap2`, `zap1` (Digital Audio) |
| Gatling ammo shot (3) | `Sfx/shot_gatling_1..3.ogg` | `laserRetro_000..002` (Sci-fi Sounds) |
| Enemy hit (3) | `Sfx/enemy_hit_1..3.ogg` | `impactSoft_medium_000..002` (Impact Sounds) |
| Enemy death (2) | `Sfx/enemy_death_1..2.ogg` | `slime_000..001` (Sci-fi Sounds) |
| Player damage (2) | `Sfx/player_hit_1..2.ogg` | `impactPunch_heavy_000..001` (Impact Sounds) |
| Jump | `Sfx/jump.ogg` | `phaseJump1` (Digital Audio) |
| Landing | `Sfx/land.ogg` | `impactSoft_heavy_000` (Impact Sounds) |
| Ammo pickup | `Sfx/pickup_ammo.ogg` | `powerUp2` (Digital Audio) |
| Coin pickup (2) | `Sfx/pickup_coin_1..2.ogg` | `handleCoins`, `handleCoins2` (RPG Audio) |
| UI focus (2) | `Sfx/ui_focus_1..2.ogg` | `tick_001`, `tick_002` (Interface Sounds) |
| UI confirm | `Sfx/ui_confirm.ogg` | `select_002` (Interface Sounds) |
| UI back | `Sfx/ui_back.ogg` | `back_001` (Interface Sounds) |
| UI buy | `Sfx/ui_buy.ogg` | `confirmation_002` (Interface Sounds) |
| UI equip | `Sfx/ui_equip.ogg` | `metalLatch` (RPG Audio) |
| UI error | `Sfx/ui_error.ogg` | `error_004` (Interface Sounds) |
| Round intro countdown tick | `Sfx/countdown.ogg` | `tick_004` (Interface Sounds) |
| "Begin!" | `Sfx/countdown_go.ogg` | `powerUp7` (Digital Audio) |
| Round intro stinger | `Stingers/stinger_round.ogg` | `Hit jingles/jingles_HIT04` (Music Jingles) |
| Boss intro stinger | `Stingers/stinger_boss.ogg` | `Steel jingles/jingles_STEEL08` (Music Jingles) |
| Round cleared stinger | `Stingers/stinger_clear.ogg` | `8-Bit jingles/jingles_NES06` (Music Jingles) |
| Game over stinger | `Stingers/stinger_gameover.ogg` | `Sax jingles/jingles_SAX04` (Music Jingles) |
