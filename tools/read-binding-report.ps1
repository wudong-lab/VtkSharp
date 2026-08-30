#requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$Path,
    [string]$Class,
    [string]$Status
)

$ErrorActionPreference = "Stop"
$report = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
if ($null -eq $report.diagnostics -or $null -eq $report.ok) {
    throw "Expected a plan-bindings JSON report: $Path"
}

if ($Class -or $Status) {
    # 精确筛选，避免每次把整个成功报告送入上下文。
    $items = @($report.diagnostics | Where-Object {
        (-not $Class -or $_.class -ceq $Class -or $_.declaringClass -ceq $Class) -and
        (-not $Status -or $_.status -ceq $Status)
    })
    ConvertTo-Json -InputObject $items -Depth 12
    exit 0
}

Write-Output "Plan OK: $($report.ok)"
foreach ($group in $report.diagnostics | Group-Object status | Sort-Object Name) {
    Write-Output "$($group.Name): $($group.Count)"
}
Write-Output "Added: $(@($report.addedClasses).Count) class(es), $(@($report.added).Count) function(s)."
foreach ($item in $report.addedClasses) {
    Write-Output "  + $($item.module)/$($item.class) [$($item.reasons -join ', ')]"
}
foreach ($item in $report.added) { Write-Output "  + $item" }
foreach ($item in $report.addedEnums) { Write-Output "  + enum $item (public get/set types change)" }
foreach ($item in $report.enumDiagnostics) { Write-Output "  $item" }
foreach ($item in $report.conflicts) { Write-Output "  conflict: $item" }
foreach ($item in $report.diagnostics | Where-Object { $_.status -notin @("ready", "already-exported") }) {
    Write-Output "  $($item.class).$($item.request): $($item.status) - $($item.reason)"
}
Write-Output "Details: use -Class <VTKClass> or -Status <status>. This report describes planning time; rerun diff-whitelist before merging."
