<#
  make_hero.ps1 - turn a gameplay recording into a seamless hero loop (MP4 1080p + 720p) and a GIF.

  Usage (from the project root, PowerShell or Git Bash):
    powershell -ExecutionPolicy Bypass -File Tools/make_hero.ps1 -In rec.mp4 -Start 12 -End 30
    powershell -ExecutionPolicy Bypass -File Tools/make_hero.ps1 -In rec.mp4 -Start 0:12 -End 0:30 -Name hero -OutDir Captures/Gameplay/hero
    powershell -ExecutionPolicy Bypass -File Tools/make_hero.ps1 -Help

  Parameters:
    -In <file>        source recording (any format ffmpeg reads)
    -Start / -End     seconds (12.5) or mm:ss (1:05.5); End-Start must be 10.75 .. 21 s
    -Name <name>      output base name (default hero)
    -OutDir <dir>     output folder (default Captures/Gameplay/hero, relative to the project root)
    -Fps <n>          MP4 frame rate (default 30)
    -CrossfadeSec <s> loop crossfade length (default 0.75)
    -MaxMB <n>        size limit per file (default 8)
    -KeepAudio        keep the audio track in the MP4s (default: no audio)
    -FfmpegPath <p>   ffmpeg.exe or its folder (default: PATH, then the WinGet Gyan.FFmpeg folder)
    -DryRun           validate and print the plan and ffmpeg commands, run nothing
    -Help

  Outputs: <Name>_loop_1080p.mp4, <Name>_loop_720p.mp4, <Name>.gif  (see Tools/make_hero.README.md)
#>
param(
    [string]$In = "",
    [string]$Start = "",
    [string]$End = "",
    [string]$Name = "hero",
    [string]$OutDir = "Captures/Gameplay/hero",
    [int]$Fps = 30,
    [double]$CrossfadeSec = 0.75,
    [double]$MaxMB = 8,
    [switch]$KeepAudio,
    [string]$FfmpegPath = "",
    [switch]$DryRun,
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$inv = [Globalization.CultureInfo]::InvariantCulture

if ($Help -or -not $In) {
    Get-Content $PSCommandPath -TotalCount 28 | ForEach-Object { $_ }
    if ($Help) { exit 0 } else { Write-Host "`nERROR: -In, -Start and -End are required."; exit 1 }
}

function Fail($msg) { Write-Host "ERROR: $msg" -ForegroundColor Red; exit 1 }

function Parse-Time([string]$s, [string]$label) {
    $s = $s.Trim()
    if (-not $s) { Fail "$label is required." }
    $parts = $s.Split(':')
    $total = 0.0
    foreach ($p in $parts) {
        $v = 0.0
        if (-not [double]::TryParse($p, [Globalization.NumberStyles]::Float, $inv, [ref]$v)) { Fail "$label '$s' is not seconds or mm:ss." }
        $total = $total * 60 + $v
    }
    return $total
}

function Find-Tool([string]$exe) {
    if ($FfmpegPath) {
        $p = $FfmpegPath
        if (Test-Path $p -PathType Container) { $c = Join-Path $p $exe; if (Test-Path $c) { return $c } }
        elseif (Test-Path $p) {
            $c = Join-Path (Split-Path -Parent $p) $exe
            if (Test-Path $c) { return $c }
        }
        Fail "$exe not found via -FfmpegPath '$FfmpegPath'."
    }
    $cmd = Get-Command $exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $wg = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
    if (Test-Path $wg) {
        $hit = Get-ChildItem -Path $wg -Directory -Filter "Gyan.FFmpeg*" -ErrorAction SilentlyContinue |
            ForEach-Object { Get-ChildItem -Path $_.FullName -Recurse -Filter $exe -ErrorAction SilentlyContinue } |
            Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    Fail "$exe not found. Install: winget install --id Gyan.FFmpeg -e   (or pass -FfmpegPath)."
}

# Runs a native exe, returns @{Code; Out; Err}. Avoids PowerShell 5.1 stderr-as-error behaviour.
function Run-Native([string]$exe, [string[]]$arguments) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.Arguments = (($arguments | ForEach-Object {
        if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join ' ')
    $pr = [Diagnostics.Process]::Start($psi)
    $errTask = $pr.StandardError.ReadToEndAsync()
    $out = $pr.StandardOutput.ReadToEnd()
    $pr.WaitForExit()
    return @{ Code = $pr.ExitCode; Out = $out; Err = $errTask.Result }
}

$script:ffmpeg = $null
function FF([string[]]$a, [string]$what) {
    $full = @("-hide_banner", "-loglevel", "error", "-y") + $a
    if ($DryRun) { Write-Host ("  [dry] ffmpeg " + ($full -join ' ')); return }
    $r = Run-Native $script:ffmpeg $full
    if ($r.Code -ne 0) { Fail "ffmpeg failed ($what):`n$($r.Err)" }
}

function Probe([string]$file) {
    $r = Run-Native $script:ffprobe @("-v", "error", "-show_streams", "-show_format", "-of", "json", $file)
    if ($r.Code -ne 0) { Fail "ffprobe failed on ${file}: $($r.Err)" }
    $j = $r.Out | ConvertFrom-Json
    $v = $j.streams | Where-Object { $_.codec_type -eq "video" } | Select-Object -First 1
    $fps = 0.0
    if ($v.r_frame_rate -match '^(\d+)/(\d+)$' -and [double]$Matches[2] -gt 0) { $fps = [double]$Matches[1] / [double]$Matches[2] }
    $dur = 0.0
    [void][double]::TryParse([string]$j.format.duration, [Globalization.NumberStyles]::Float, $inv, [ref]$dur)
    return [pscustomobject]@{
        File = Split-Path -Leaf $file; Res = "$($v.width)x$($v.height)"; Fps = [math]::Round($fps, 2)
        Dur = [math]::Round($dur, 2); SizeMB = [math]::Round((Get-Item $file).Length / 1MB, 2)
        Codec = $v.codec_name; Pix = $v.pix_fmt
        Audio = [bool]($j.streams | Where-Object { $_.codec_type -eq "audio" })
    }
}

# ---- validate ----
$s = Parse-Time $Start "-Start"
$e = Parse-Time $End "-End"
$D = $e - $s
if ($D -lt 10.75 -or $D -gt 21.0) { Fail ("End-Start is {0:N2} s; it must be between 10.75 and 21 s." -f $D) }
if ($CrossfadeSec -lt 0.1 -or $CrossfadeSec * 2 -ge $D) { Fail "-CrossfadeSec must be >= 0.1 and less than half the segment." }
if (-not (Test-Path $In)) { Fail "Input not found: $In" }
$In = (Resolve-Path $In).Path
$script:ffmpeg = Find-Tool "ffmpeg.exe"
$script:ffprobe = Find-Tool "ffprobe.exe"

$root = Split-Path -Parent $PSScriptRoot
$out = if ([IO.Path]::IsPathRooted($OutDir)) { $OutDir } else { Join-Path $root $OutDir }
if (-not $DryRun) { New-Item -ItemType Directory -Force $out | Out-Null }
$limit = [long]($MaxMB * 1MB)
$F = $CrossfadeSec
$loopLen = $D - $F
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("make_hero_" + [guid]::NewGuid().ToString("N").Substring(0, 8))
if (-not $DryRun) { New-Item -ItemType Directory -Force $tmp | Out-Null }

$ver = (Run-Native $script:ffmpeg @("-version")).Out.Split("`n")[0]
Write-Host "make_hero: $ver"
Write-Host ("Input  : {0}`nSegment: {1:N2} .. {2:N2} s ({3:N2} s), crossfade {4} s -> loop length {5:N2} s" -f $In, $s, $e, $D, $F, $loopLen)
Write-Host "Output : $out"

$fs = $F.ToString("0.###", $inv)
$ds = $D.ToString("0.###", $inv)
$bs = ($D - $F).ToString("0.###", $inv)

# ---- 1. master loop (1080p, near lossless intermediate) ----
# Source segment (relative time 0..D) is cut into head [0,F], body [F,D-F], tail [D-F,D].
# Output = xfade(tail -> head, F) + body.  o(t) = c(t) for t in [F, D-F), and for t in [0,F) it
# blends c(D-F+t) (fading out) into c(t) (fading in): the last frame is followed directly by the
# first with no visible jump.
$master = Join-Path $tmp "master.mkv"
$vf = "[0:v]fps=$Fps,scale=1920:1080:flags=lanczos:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,setsar=1,format=yuv420p,split=3[v1][v2][v3];" +
      "[v1]trim=start=0:end=${fs},setpts=PTS-STARTPTS[head];" +
      "[v2]trim=start=${bs}:end=${ds},setpts=PTS-STARTPTS[tail];" +
      "[v3]trim=start=${fs}:end=${bs},setpts=PTS-STARTPTS[body];" +
      "[tail][head]xfade=transition=fade:duration=${fs}:offset=0[xf];[xf][body]concat=n=2:v=1:a=0[v]"
$mapArgs = @("-map", "[v]")
$af = ""
if ($KeepAudio) {
    $af = ";[0:a]asplit=3[a1][a2][a3];[a1]atrim=start=0:end=${fs},asetpts=PTS-STARTPTS[ah];[a2]atrim=start=${bs}:end=${ds},asetpts=PTS-STARTPTS[at];" +
          "[a3]atrim=start=${fs}:end=${bs},asetpts=PTS-STARTPTS[ab];[at][ah]acrossfade=d=${fs}[ax];[ax][ab]concat=n=2:v=0:a=1[a]"
    $mapArgs += @("-map", "[a]")
}
Write-Host "`n[1/3] Building master loop..."
FF (@("-ss", $s.ToString("0.###", $inv), "-t", $ds, "-i", $In, "-filter_complex", ($vf + $af)) + $mapArgs +
   @("-c:v", "libx264", "-preset", "veryfast", "-crf", "10", "-pix_fmt", "yuv420p") + $(if ($KeepAudio) { @("-c:a", "flac") } else { @("-an") }) + @($master)) "master loop"

# ---- 2. MP4s: raise CRF until under the limit ----
function Make-Mp4([int]$w, [int]$h, [string]$file) {
    $crf = 20
    while ($true) {
        $a = @("-i", $master, "-vf", "scale=${w}:${h}:flags=lanczos,format=yuv420p", "-c:v", "libx264", "-preset", "slow",
               "-crf", "$crf", "-pix_fmt", "yuv420p", "-movflags", "+faststart") +
             $(if ($KeepAudio) { @("-c:a", "aac", "-b:a", "128k") } else { @("-an") }) + @($file)
        FF $a "mp4 ${w}x${h} crf $crf"
        if ($DryRun) { return }
        $size = (Get-Item $file).Length
        Write-Host ("  {0}x{1} CRF {2}: {3:N2} MB" -f $w, $h, $crf, ($size / 1MB))
        if ($size -le $limit) { return }
        $crf += 2
        if ($crf -gt 44) { Fail "Could not get $file under $MaxMB MB even at CRF 44." }
    }
}
Write-Host "`n[2/3] MP4s..."
$f1080 = Join-Path $out "${Name}_loop_1080p.mp4"
$f720 = Join-Path $out "${Name}_loop_720p.mp4"
Make-Mp4 1920 1080 $f1080
Make-Mp4 1280 720 $f720

# ---- 3. GIF: palettegen/paletteuse, retry ladder until under the limit ----
# (width, fps, colors, dither, stats_mode)
$ladder = @(
    @(960, 15, 256, "sierra2_4a", "full"),
    @(960, 15, 192, "sierra2_4a", "diff"),
    @(960, 15, 128, "bayer:bayer_scale=4", "diff"),
    @(960, 15, 96,  "bayer:bayer_scale=5", "diff"),
    @(960, 12, 128, "bayer:bayer_scale=4", "diff"),
    @(960, 10, 96,  "bayer:bayer_scale=5", "diff"),
    @(800, 10, 96,  "bayer:bayer_scale=5", "diff"),
    @(640, 10, 64,  "bayer:bayer_scale=5", "diff"),
    @(640, 8,  48,  "bayer:bayer_scale=5", "diff")
)
Write-Host "`n[3/3] GIF..."
$gif = Join-Path $out "${Name}.gif"
$gifOk = $false
$gifNote = ""
foreach ($t in $ladder) {
    $gw = $t[0]; $gf = $t[1]; $gc = $t[2]; $gd = $t[3]; $gm = $t[4]
    $g = "[0:v]fps=$gf,scale=${gw}:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=${gc}:stats_mode=${gm}[p];[b][p]paletteuse=dither=${gd}:diff_mode=rectangle"
    FF @("-i", $master, "-filter_complex", $g, "-loop", "0", $gif) "gif ${gw}px ${gf}fps ${gc}c"
    if ($DryRun) { $gifOk = $true; break }
    $size = (Get-Item $gif).Length
    Write-Host ("  GIF {0}px {1}fps {2} colors {3}: {4:N2} MB" -f $gw, $gf, $gc, $gd, ($size / 1MB))
    $gifNote = "{0}px, {1} fps, {2} colors, {3}" -f $gw, $gf, $gc, $gd
    if ($size -le $limit) { $gifOk = $true; break }
}
if (-not $gifOk) { Fail "Could not get the GIF under $MaxMB MB with the retry ladder; trim the segment (shorter End-Start) and rerun." }

if ($DryRun) { Write-Host "`nDry run complete (nothing written)."; exit 0 }

# ---- summary ----
Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
$rows = @($f1080, $f720, $gif) | ForEach-Object { Probe $_ }
Write-Host "`nSummary"
$rows | Format-Table @{n = "File"; e = { $_.File } }, @{n = "Resolution"; e = { $_.Res } }, @{n = "FPS"; e = { $_.Fps } },
    @{n = "Duration(s)"; e = { $_.Dur } }, @{n = "Size(MB)"; e = { $_.SizeMB } }, @{n = "Codec"; e = { $_.Codec } }, @{n = "Audio"; e = { $_.Audio } } -AutoSize | Out-String | Write-Host
Write-Host "GIF settings used: $gifNote"
$bad = $rows | Where-Object { $_.SizeMB -gt $MaxMB }
if ($bad) { Fail "Over the size limit: $($bad.File -join ', ')" }
Write-Host "OK" -ForegroundColor Green
exit 0
