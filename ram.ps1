$g = Get-Process GridSurf -ErrorAction SilentlyContinue
if (-not $g) { Write-Host "GridSurf not running" -ForegroundColor Red; exit }

$tree = @{}
Get-CimInstance Win32_Process | ForEach-Object {
    $tree[[int]$_.ProcessId] = [int]$_.ParentProcessId
}

$pids = New-Object System.Collections.Generic.List[int]
$pids.Add($g.Id)
$stack = New-Object System.Collections.Stack
$stack.Push($g.Id)
while ($stack.Count -gt 0) {
    $p = $stack.Pop()
    foreach ($k in $tree.Keys) {
        if ($tree[$k] -eq $p) { $pids.Add($k); $stack.Push($k) }
    }
}

$rows = foreach ($id in $pids) {
    $x = Get-Process -Id $id -ErrorAction SilentlyContinue
    if ($x) {
        [PSCustomObject]@{
            PID    = $x.Id
            Name   = $x.ProcessName
            RAM_MB = [math]::Round($x.WorkingSet64 / 1MB, 1)
            CPU_s  = [math]::Round([double]$x.CPU, 1)
        }
    }
}

$rows | Sort-Object RAM_MB -Descending | Format-Table -AutoSize
$total = ($rows | Measure-Object RAM_MB -Sum).Sum
Write-Host ("TOTAL: {0} processes, {1} MB ({2:N2} GB)" -f $rows.Count, $total, ($total / 1024)) -ForegroundColor Cyan
