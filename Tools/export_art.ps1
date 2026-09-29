<#
  export_art.ps1 - downscale 4x master PNGs from ArtSource/ into Assets/Art/ as 2x game PNGs.

  ArtSource/<sub>/<name>.png  ->  Assets/Art/<sub>/<name>.png   (50%, high-quality bicubic, alpha kept)
  Files whose output already exists and is newer than the master are skipped.

  Usage (from the project root, Git Bash or PowerShell):
    powershell -ExecutionPolicy Bypass -File Tools/export_art.ps1                 # everything under ArtSource
    powershell -ExecutionPolicy Bypass -File Tools/export_art.ps1 ScaleTest       # one subfolder
    powershell -ExecutionPolicy Bypass -File Tools/export_art.ps1 -Force          # ignore the timestamp check
#>
param(
    [string]$Sub = "",
    [switch]$Force
)

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$srcRoot = Join-Path $root "ArtSource"
$dstRoot = Join-Path $root "Assets/Art"
$scanRoot = if ($Sub) { Join-Path $srcRoot $Sub } else { $srcRoot }

if (-not (Test-Path $scanRoot)) { Write-Error "Not found: $scanRoot"; exit 1 }

$done = 0; $skipped = 0
foreach ($file in Get-ChildItem -Path $scanRoot -Recurse -Filter *.png) {
    $rel = $file.FullName.Substring($srcRoot.Length).TrimStart('\', '/')
    $dst = Join-Path $dstRoot $rel

    if (-not $Force -and (Test-Path $dst) -and ((Get-Item $dst).LastWriteTimeUtc -gt $file.LastWriteTimeUtc)) {
        Write-Host "skip   $rel (unchanged)"
        $skipped++
        continue
    }

    $src = [System.Drawing.Image]::FromFile($file.FullName)
    try {
        $w = [int][Math]::Round($src.Width / 2.0)
        $h = [int][Math]::Round($src.Height / 2.0)
        $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $bmp.SetResolution(72, 72)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            # The wrap mode stops the edge pixels from bleeding transparent/black into the border.
            $attr = New-Object System.Drawing.Imaging.ImageAttributes
            $attr.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
            $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0, 0, $w, $h)), 0, 0, $src.Width, $src.Height, [System.Drawing.GraphicsUnit]::Pixel, $attr)
        } finally { $g.Dispose() }

        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
        $bmp.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host ("export {0}  {1}x{2} -> {3}x{4}" -f $rel, $src.Width, $src.Height, $w, $h)
        $done++
    } finally { $src.Dispose() }
}
Write-Host "done: $done exported, $skipped skipped"
