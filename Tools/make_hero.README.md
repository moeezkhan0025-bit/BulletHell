# make_hero

Turns a gameplay recording into a seamless hero loop plus a GIF (milestone R1, portfolio capture kit).
Windows PowerShell 5.1, needs ffmpeg and ffprobe (`winget install --id Gyan.FFmpeg -e`).
The script finds them via `-FfmpegPath`, then PATH, then `%LOCALAPPDATA%\Microsoft\WinGet\Packages\Gyan.FFmpeg*`.

## Run

```
powershell -ExecutionPolicy Bypass -File Tools/make_hero.ps1 -In rec.mp4 -Start 12 -End 30
powershell -ExecutionPolicy Bypass -File Tools/make_hero.ps1 -In rec.mp4 -Start 0:12 -End 0:30 -Name hero -OutDir Captures/Gameplay/hero
```

Options: `-Name hero`, `-OutDir Captures/Gameplay/hero` (relative to the project root), `-Fps 30` (MP4 rate),
`-CrossfadeSec 0.75`, `-MaxMB 8`, `-KeepAudio` (off by default), `-FfmpegPath`, `-DryRun` (print the plan and ffmpeg
commands, write nothing), `-Help`. `-Start` / `-End` take seconds or mm:ss; `End - Start` must be 10.75 to 21 s.

Outputs in `-OutDir`: `<Name>_loop_1080p.mp4`, `<Name>_loop_720p.mp4`, `<Name>.gif`, then a summary table
(resolution, fps, duration, size from ffprobe). Exit code is non-zero on any failure.

## How it works

- Seamless loop: the segment is cut into head `[0,F]`, body `[F,D-F]`, tail `[D-F,D]` (F = crossfade, D = length).
  The output is `xfade(tail -> head, F)` followed by the body, so the loop is `D - F` long. Its first frames blend
  the end of the take into its start, and the last frame flows straight into the first with no jump.
  (Anything that moves in the take shows briefly as a dissolve at the loop point; pick a segment where the action
  is similar at both ends, e.g. the arena idle between waves.)
- A near-lossless 1080p master is built once, then the MP4s and GIF are derived from it.
- MP4: H.264 yuv420p, preset slow, `+faststart`, no audio. CRF starts at 20 and rises by 2 until the file is under the limit.
- GIF: 960 px wide, 15 fps, palettegen/paletteuse in one pass. If over the limit it walks down a ladder: fewer colors,
  bayer dither, fewer fps, smaller width. It fails with a message if the whole ladder is not enough (shorten the segment).
- Temp files go to the system temp folder and are removed on success.
