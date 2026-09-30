# 09 - Audio System

Code: `Assets/Scripts/Audio` (`SfxId`, `SfxData`, `AudioLibrary`, `SfxVoiceLimiter`, `AudioDirector`, `AudioHost`), `Assets/Scripts/Core/AudioService.cs`. Data: `Assets/Data/Audio`. Clips: `Assets/Audio`. Tests: `Assets/Tests/EditMode/AudioTests.cs`. Introduced in D4 (the earlier `AudioService` was a stub).

**One entry point.** Gameplay and UI call `GameServices.Ensure().Audio.Play(SfxId)` or `PlayMusic(MusicContext)`. The `AudioLibrary` on `GameConfig` maps each id to an `SfxData` asset (clips, volume, pitch range, bus, limits) and each music context to tracks. Ammo types hold their own `SfxData` (`AmmoTypeData.FireSound`). Nothing in code names a clip.

**Why the limits exist.** A swarm fight fires hundreds of hits, deaths and coin pickups per minute. Each `SfxData` says how many copies may play at once (`maxVoices`), how soon it may repeat (`minInterval`) and its `priority`. `SfxVoiceLimiter.Decide` turns those into Play, Drop or StealOldest; the oldest copy is cut only after 0.06 s so a fast weapon does not chop its own sound. The pool itself is a fixed array of `AudioSource`s built at startup (size on the library); when every voice is busy a sound steals the oldest voice of equal or lower priority or is dropped. `AudioService.PlayedCount` and `DroppedCount` show the effect.

**Music.** Two looping sources crossfade (1.5 s by default, linear gains). `AudioDirector` picks the context from the run state: combat or boss music at the round intro (Combat indexes its tracks by round number), the Shop track from Round Results through the Armory, silence at Game Over; the Main Menu asks for the Menu track itself. A stinger's `SfxData` can duck the music to a fraction for a few seconds, and the pause state dips it to a configurable level.

**Mixer and settings.** `VoxMixer.mixer`: Master > Music, Sfx > Ui, Announcer. The three Settings sliders set the exposed `MasterVolume`, `MusicVolume`, `SfxVolume` parameters in dB (`20 log10(v)`, silence = -80 dB). Without a mixer (tests, a missing library) master falls back to `AudioListener.volume` and the others scale the source volumes. Unity has no public API to build a mixer, so `Editor/AudioSetup.cs` creates it by reflection on the internal `AudioMixerController`.

**UI sounds.** `UiSound.Play(kind)` keeps its old hooks (focus, confirm, back, buy, equip, error); it plays the library sound on the Ui bus, unless the `UITheme` has a clip assigned for that kind, which overrides it.
