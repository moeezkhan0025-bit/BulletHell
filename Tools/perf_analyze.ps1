<#
  perf_analyze.ps1 - analyse the CSV written by the in-game PerfLogger (PerfLogs folder in the persistent data path).

  Usage (from the project root, PowerShell or Git Bash):
    powershell -ExecutionPolicy Bypass -File Tools/perf_analyze.ps1 -Csv <run.csv>                   # one run: percentiles, per-marker cost, worst frames
    powershell -ExecutionPolicy Bypass -File Tools/perf_analyze.ps1 -Csv <before.csv> -Compare <after.csv>   # before / after table
    ... -Top 20      number of worst frames to list (default 15)
    ... -From 30 -To 90   only rows between these seconds of the run (e.g. skip the swarm ramp-up)
    ... -Out file.md  also write the report to a file

  Columns: frame, t_s, dt_ms (real frame time), main_ms, render_ms, gc_bytes, batches, setpass, draws, one column per
  BH.* profiler marker (ms spent in it that frame), bullets, enemies, particles, timescale (1 running, 0 hitstop/frozen), state.
  Each row is the frame that just finished.
#>
param(
    [Parameter(Mandatory = $true)][string]$Csv,
    [string]$Compare = "",
    [int]$Top = 15,
    [string]$Out = "",
    [double]$From = 0,
    [double]$To = 1e9
)

$ErrorActionPreference = "Stop"
$inv = [System.Globalization.CultureInfo]::InvariantCulture

function Load-Run([string]$path) {
    $rows = @(Import-Csv -Path $path | Where-Object { $t = [double]::Parse($_.t_s, $inv); $t -ge $From -and $t -le $To })
    if ($rows.Count -eq 0) { throw "No rows in $path between $From s and $To s" }
    $names = $rows[0].PSObject.Properties.Name
    $markers = @($names | Where-Object { $_ -like "BH.*" })
    [pscustomobject]@{ Path = $path; Rows = $rows; Markers = $markers }
}

function Col($run, [string]$name) { [double[]]($run.Rows | ForEach-Object { [double]::Parse($_.$name, $inv) }) }

function Pct([double[]]$sorted, [double]$p) {
    if ($sorted.Length -eq 0) { return 0 }
    $i = [math]::Min($sorted.Length - 1, [math]::Max(0, [int][math]::Ceiling($p * $sorted.Length) - 1))
    $sorted[$i]
}

function Summarize($run) {
    $dt = Col $run "dt_ms"
    $sorted = $dt | Sort-Object
    $gc = Col $run "gc_bytes"
    $main = Col $run "main_ms"
    $render = Col $run "render_ms"
    $n = $dt.Length
    $seconds = ($dt | Measure-Object -Sum).Sum / 1000.0
    $s = [ordered]@{}
    $s["frames"] = $n
    $s["seconds"] = [math]::Round($seconds, 1)
    $s["avg fps"] = [math]::Round($n / $seconds, 1)
    $s["avg frame ms"] = [math]::Round(($dt | Measure-Object -Average).Average, 2)
    $s["p50 ms"] = [math]::Round((Pct $sorted 0.50), 2)
    $s["p95 ms"] = [math]::Round((Pct $sorted 0.95), 2)
    $s["p99 ms"] = [math]::Round((Pct $sorted 0.99), 2)
    $s["p99.9 ms"] = [math]::Round((Pct $sorted 0.999), 2)
    $s["max ms"] = [math]::Round($sorted[$n - 1], 2)
    $s["frames > 16.7 ms"] = @($dt | Where-Object { $_ -gt 16.7 }).Count
    $s["frames > 20 ms"] = @($dt | Where-Object { $_ -gt 20 }).Count
    $s["frames > 33.3 ms"] = @($dt | Where-Object { $_ -gt 33.3 }).Count
    $s["main thread avg ms"] = [math]::Round(($main | Measure-Object -Average).Average, 2)
    $s["main thread p99 ms"] = [math]::Round((Pct ($main | Sort-Object) 0.99), 2)
    $s["render thread avg ms"] = [math]::Round(($render | Measure-Object -Average).Average, 2)
    $s["GPU frame avg ms"] = [math]::Round((Col $run "gpu_ms" | Measure-Object -Average).Average, 2)
    $s["present wait avg ms"] = [math]::Round((Col $run "wait_present_ms" | Measure-Object -Average).Average, 2)
    $s["GC.Collect total ms"] = [math]::Round((Col $run "gc_collect_ms" | Measure-Object -Sum).Sum, 1)
    $s["Instantiate total ms"] = [math]::Round((Col $run "instantiate_ms" | Measure-Object -Sum).Sum, 1)
    $s["GC allocs / frame avg"] = [math]::Round((Col $run "gc_allocs" | Measure-Object -Average).Average, 1)
    $s["GC bytes / frame avg"] = [math]::Round(($gc | Measure-Object -Average).Average, 0)
    $s["GC bytes max frame"] = [math]::Round(($gc | Measure-Object -Maximum).Maximum, 0)
    $s["frames with GC alloc %"] = [math]::Round(100.0 * @($gc | Where-Object { $_ -gt 0 }).Count / $n, 1)
    $s["batches avg"] = [math]::Round((Col $run "batches" | Measure-Object -Average).Average, 0)
    $s["setpass avg"] = [math]::Round((Col $run "setpass" | Measure-Object -Average).Average, 0)
    $s["bullets avg / max"] = "{0} / {1}" -f [math]::Round((Col $run "bullets" | Measure-Object -Average).Average, 0), (Col $run "bullets" | Measure-Object -Maximum).Maximum
    $s["enemies avg / max"] = "{0} / {1}" -f [math]::Round((Col $run "enemies" | Measure-Object -Average).Average, 0), (Col $run "enemies" | Measure-Object -Maximum).Maximum
    $s
}

function MarkerTable($run) {
    # Unity's own phases (same unit, ms per frame) first, then the BH.* script markers.
    $unity = @("physics2d_ms", "physics_sync_ms", "camera_render_ms", "canvas_batch_ms", "canvas_overlay_ms", "wait_gfx_ms", "wait_vsync_ms", "gc_collect_ms", "instantiate_ms")
    foreach ($m in (@($unity | Where-Object { $run.Rows[0].PSObject.Properties.Name -contains $_ }) + $run.Markers)) {
        $v = Col $run $m
        $sorted = $v | Sort-Object
        [pscustomobject]@{
            Marker = $m
            "mean ms" = [math]::Round(($v | Measure-Object -Average).Average, 3)
            "p99 ms" = [math]::Round((Pct $sorted 0.99), 3)
            "max ms" = [math]::Round($sorted[$sorted.Length - 1], 3)
        }
    }
}

function WorstFrames($run, [int]$count) {
    $rows = $run.Rows
    $list = for ($i = 0; $i -lt $rows.Count; $i++) {
        $r = $rows[$i]
        $best = ""
        $bestMs = -1.0
        foreach ($m in (@("physics2d_ms", "physics_sync_ms", "camera_render_ms", "canvas_batch_ms", "canvas_overlay_ms", "wait_gfx_ms") + $run.Markers)) {
            if ($r.PSObject.Properties.Name -notcontains $m) { continue }
            $v = [double]::Parse($r.$m, $inv)
            if ($v -gt $bestMs) { $bestMs = $v; $best = $m }
        }
        [pscustomobject]@{
            "t s" = [math]::Round([double]::Parse($r.t_s, $inv), 2)
            "frame ms" = [math]::Round([double]::Parse($r.dt_ms, $inv), 1)
            "main ms" = [math]::Round([double]::Parse($r.main_ms, $inv), 1)
            "render ms" = [math]::Round([double]::Parse($r.render_ms, $inv), 1)
            "gpu ms" = [math]::Round([double]::Parse($r.gpu_ms, $inv), 1)
            "GC B" = [double]::Parse($r.gc_bytes, $inv)
            "GC ms" = [math]::Round([double]::Parse($r.gc_collect_ms, $inv), 1)
            "inst ms" = [math]::Round([double]::Parse($r.instantiate_ms, $inv), 1)
            "top marker" = "$best $([math]::Round($bestMs, 2))"
            bullets = $r.bullets
            enemies = $r.enemies
            timescale = $r.timescale
        }
    }
    $list | Sort-Object { $_."frame ms" } -Descending | Select-Object -First $count
}

function Table($objects) { ($objects | Format-Table -AutoSize | Out-String).TrimEnd() }

$a = Load-Run $Csv
$report = New-Object System.Text.StringBuilder

if ($Compare -eq "") {
    $sum = Summarize $a
    [void]$report.AppendLine("# Perf report: $([System.IO.Path]::GetFileName($Csv))")
    [void]$report.AppendLine("")
    [void]$report.AppendLine((Table ($sum.GetEnumerator() | ForEach-Object { [pscustomobject]@{ Metric = $_.Key; Value = $_.Value } })))
    [void]$report.AppendLine("")
    [void]$report.AppendLine("## Cost per marker (ms per frame)")
    [void]$report.AppendLine((Table (MarkerTable $a | Sort-Object { $_."mean ms" } -Descending)))
    [void]$report.AppendLine("")
    [void]$report.AppendLine("## Worst $Top frames")
    [void]$report.AppendLine((Table (WorstFrames $a $Top)))
}
else {
    $b = Load-Run $Compare
    $sa = Summarize $a
    $sb = Summarize $b
    [void]$report.AppendLine("# Perf compare: before = $([System.IO.Path]::GetFileName($Csv))   after = $([System.IO.Path]::GetFileName($Compare))")
    [void]$report.AppendLine("")
    [void]$report.AppendLine((Table ($sa.Keys | ForEach-Object { [pscustomobject]@{ Metric = $_; Before = $sa[$_]; After = $sb[$_] } })))
    [void]$report.AppendLine("")
    [void]$report.AppendLine("## Marker mean ms per frame (before -> after)")
    $ma = MarkerTable $a
    $mb = MarkerTable $b
    [void]$report.AppendLine((Table ($ma | ForEach-Object {
        $name = $_.Marker
        $after = $mb | Where-Object { $_.Marker -eq $name }
        [pscustomobject]@{ Marker = $name; "before mean" = $_."mean ms"; "after mean" = $after."mean ms"; "before p99" = $_."p99 ms"; "after p99" = $after."p99 ms" }
    })))
}

$text = $report.ToString()
Write-Output $text
if ($Out -ne "") { Set-Content -Path $Out -Value $text -Encoding utf8 }
