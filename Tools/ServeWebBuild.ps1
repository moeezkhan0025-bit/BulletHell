<#
  Serves a WebGL build folder over http://localhost:<port>/ and opens it in Chrome. A plain static server is enough: the demo build is
  gzip-compressed with Decompression Fallback on, so it needs no special headers.

  Usage (from the project root):
    powershell -ExecutionPolicy Bypass -File Tools\ServeWebBuild.ps1                      # the newest Builds\WebGL\*-demo folder
    powershell -ExecutionPolicy Bypass -File Tools\ServeWebBuild.ps1 -Folder Builds\WebGL\1.0.6-demo -Port 8080
  Stop with Ctrl+C. Use -NoBrowser to only serve.
#>
param(
    [string]$Folder = "",
    [int]$Port = 8080,
    [switch]$NoBrowser
)

$root = Split-Path -Parent $PSScriptRoot
if ($Folder -eq "") {
    $latest = Get-ChildItem (Join-Path $root "Builds\WebGL") -Directory -Filter "*-demo" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $latest) { Write-Error "No Builds\WebGL\*-demo folder found. Run BulletHell > Build > Build WebGL (Demo) first."; exit 1 }
    $Folder = $latest.FullName
}
$Folder = (Resolve-Path $Folder).Path
if (-not (Test-Path (Join-Path $Folder "index.html"))) { Write-Error "$Folder has no index.html"; exit 1 }

$mime = @{
    ".html" = "text/html"; ".js" = "application/javascript"; ".wasm" = "application/wasm"; ".json" = "application/json"; ".css" = "text/css";
    ".png" = "image/png"; ".jpg" = "image/jpeg"; ".ico" = "image/x-icon"; ".ttf" = "font/ttf"; ".txt" = "text/plain";
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "Serving $Folder"
Write-Host "Open http://localhost:$Port/   (Ctrl+C to stop)"
if (-not $NoBrowser) {
    $chrome = @("$env:ProgramFiles\Google\Chrome\Application\chrome.exe", "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($chrome) { Start-Process $chrome "http://localhost:$Port/" } else { Start-Process "http://localhost:$Port/" }
}

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $path = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart("/"))
        if ($path -eq "") { $path = "index.html" }
        $file = [System.IO.Path]::GetFullPath((Join-Path $Folder $path))
        if (-not $file.StartsWith($Folder) -or -not (Test-Path $file -PathType Leaf)) {
            $context.Response.StatusCode = 404
        } else {
            $ext = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
            $type = $mime[$ext]
            # *.unityweb files are gzip data the Unity loader decompresses itself (Decompression Fallback), so they go out as plain bytes.
            $context.Response.ContentType = if ($type) { $type } else { "application/octet-stream" }
            $context.Response.AddHeader("Cache-Control", "no-cache")
            $bytes = [System.IO.File]::ReadAllBytes($file)
            $context.Response.ContentLength64 = $bytes.Length
            $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        }
        $context.Response.Close()
    }
} finally {
    $listener.Stop()
}
