#Requires -Version 7.0
<#
.SYNOPSIS
    Wertet TRX-Dateien auf Flake-Ereignisse aus und meldet Quarantäne-Kandidaten.

.DESCRIPTION
    Ein "Lauf" wird über die Dateinamens-Konvention gruppiert: <name>.trx ist der
    Primärlauf, <name>-flaky-retry.trx im selben Verzeichnis der zugehörige Retry
    (erzeugt von scripts/Retry-FlakyTests.ps1 bzw. dem CI-Schritt "Retry flaky tests once").

    Als Flake-Ereignis zählt:
      - Fehlschlag im Primär-TRX + grün im Retry-TRX desselben Laufs, oder
      - Fehlschlag im Primär-TRX von Lauf N gefolgt von grün im Primär-TRX des
        nächsten Laufs, der den Test enthält (sortiert nach dem Lauf-Zeitstempel
        aus dem TRX <Times start>-Attribut, LastWriteTimeUtc als Fallback).

    Primär-Outcomes außerhalb Passed/Failed (z. B. NotExecuted durch übersprungene
    oder nicht entdeckte Tests) gelten als fehlende Beobachtung: Sie erhöhen weder
    den Ausführungszähler noch die Fehlrate und unterbrechen die Lauf-Sequenz
    nicht (kein Phantom-"grün im Folgelauf" nach einem Skip, kein verlorenes
    fail->pass über einen Skip hinweg).

    Klassifikation pro Testname (Fenster: letzte -MaxRuns Primär-Läufe):
      - Quarantäne-Kandidat: >= QuarantineMinFlakeEvents Flake-Ereignisse ODER
        Fehlrate >= QuarantineMinFailureRate bei >= QuarantineMinExecutions Ausführungen.
      - beobachten: >= 1 Flake-Ereignis ODER Fehlrate >= WatchMinFailureRate.
      - stabil: sonst.

    Reiner Report — der Exit-Code ist immer 0. Für Quarantäne-Kandidaten wird eine
    Empfehlung ausgegeben (Quarantäne-Issue anlegen + Fehlerbild in
    docs/help/stabilitaet/os-interface-tests.md dokumentieren); in GitHub Actions
    zusätzlich als ::warning::-Annotation.

.PARAMETER Path
    Ein oder mehrere Verzeichnisse, die rekursiv nach TRX-Dateien durchsucht werden —
    z. B. lokale Läufe (src/*/TestResults) oder per
    "gh run download <run-id> -n test-results-os-interface-<pr|staging>" geholte
    CI-Artefakte.

.PARAMETER MaxRuns
    Auswertungsfenster: nur die letzten N Primär-Läufe (nach dem Lauf-Zeitstempel
    im TRX, LastWriteTimeUtc als Fallback) zählen. Standard: 20.

.PARAMETER QuarantineMinFlakeEvents
    Ab so vielen Flake-Ereignissen im Fenster gilt ein Test als Quarantäne-Kandidat.
    Standard: 2 (Startwert, kalibrierbar).

.PARAMETER QuarantineMinFailureRate
    Fehlraten-Schwelle für die Quarantäne (zusammen mit QuarantineMinExecutions).
    Standard: 0.2 (Startwert, kalibrierbar).

.PARAMETER QuarantineMinExecutions
    Mindestanzahl Primär-Ausführungen, bevor die Fehlraten-Quarantäne greift.
    Standard: 5 (Startwert, kalibrierbar).

.PARAMETER WatchMinFailureRate
    Fehlraten-Schwelle für die Beobachtungsstufe. Standard: 0.1 (Startwert, kalibrierbar).

.EXAMPLE
    ./scripts/Analyze-FlakyTestResults.ps1 -Path src/Tankradar.Tests.E2E/TestResults

.EXAMPLE
    gh run download 123456789 -n test-results-os-interface-pr -D artifacts/os-pr
    ./scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/os-pr
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Path,
    [int]$MaxRuns = 20,
    [int]$QuarantineMinFlakeEvents = 2,
    [double]$QuarantineMinFailureRate = 0.2,
    [int]$QuarantineMinExecutions = 5,
    [double]$WatchMinFailureRate = 0.1
)

$ErrorActionPreference = "Stop"
$inGitHubActions = $env:GITHUB_ACTIONS -eq "true"

. (Join-Path $PSScriptRoot "lib/Trx.ps1")

# 1. TRX-Dateien einsammeln und in Primär/Retry aufteilen
$trxFiles = @()
foreach ($dir in $Path) {
    if (Test-Path $dir) {
        $trxFiles += @(Get-ChildItem -Path $dir -Filter "*.trx" -Recurse -File)
    }
}

$primaries = [System.Collections.Generic.List[object]]::new()
$retries = [System.Collections.Generic.List[object]]::new()
foreach ($file in $trxFiles) {
    if ($file.BaseName -like "*-flaky-retry") {
        $retries.Add($file)
    }
    else {
        $primaries.Add($file)
    }
}
# Lauf-Reihenfolge primär über den Lauf-Zeitstempel im TRX (<Times start="..."):
# LastWriteTimeUtc spiegelt bei per "gh run download" geholten Artefakten nur den
# Download-Zeitpunkt wider und ist daher kein zuverlässiges Ordnungskriterium.
$primaries = @($primaries | Sort-Object {
    $runStart = Get-TrxRunStartTime -TrxPath $_.FullName
    if ($null -ne $runStart) { $runStart } else { $_.LastWriteTimeUtc }
} | Select-Object -Last $MaxRuns)

Write-Host "==> $($primaries.Count) Primär-Lauf/Läufe und $($retries.Count) Retry-TRX im Fenster (MaxRuns=$MaxRuns)."

if ($primaries.Count -eq 0) {
    Write-Host "==> Keine TRX-Dateien gefunden — leerer Report."
    exit 0
}

# 2. Läufe parsen (Primär + gepaarter Retry im selben Verzeichnis)
$runs = [System.Collections.Generic.List[object]]::new()
foreach ($primary in $primaries) {
    $primaryResults = Get-TrxTestResults -TrxPath $primary.FullName
    if ($null -eq $primaryResults) { continue }
    $retryPath = Join-Path $primary.DirectoryName "$($primary.BaseName)-flaky-retry.trx"
    $retryResults = $null
    if (Test-Path $retryPath) {
        $retryResults = Get-TrxTestResults -TrxPath $retryPath
    }
    $runs.Add([PSCustomObject]@{
        Name    = $primary.BaseName
        Primary = $primaryResults
        Retry   = $retryResults
    })
}

# 3. Pro Testname: Ausführungen, Fehlschläge, Flake-Ereignisse
$stats = @{}
foreach ($run in $runs) {
    foreach ($entry in $run.Primary.GetEnumerator()) {
        $name = $entry.Key
        # Nicht-Passed/Failed-Primär-Outcomes (z. B. NotExecuted durch einen Skip oder
        # einen Discovery-Ausfall) zählen als fehlende Beobachtung: keine Ausführung,
        # keine Sequenz-Transition, kein LastOutcome-Update. Sonst erzeugt fail->skip ein
        # Phantom-"grün im Folgelauf", verwässert Executions/Fehlrate und löscht ein
        # gemerktes LastOutcome=Failed (echte fail->skip->pass-Sequenz ginge verloren).
        if ($entry.Value -ne "Passed" -and $entry.Value -ne "Failed") { continue }
        $failed = $entry.Value -eq "Failed"
        if (-not $stats.ContainsKey($name)) {
            $stats[$name] = [PSCustomObject]@{
                Executions   = 0
                Failures     = 0
                FlakeEvents  = 0
                LastOutcome  = $null
            }
        }
        $stat = $stats[$name]
        $stat.Executions++
        if ($failed) { $stat.Failures++ }
        $retried = $null -ne $run.Retry -and $run.Retry.Contains($name)
        $retryOutcome = if ($retried) { $run.Retry[$name] } else { $null }
        # Nur "Passed" gilt als grün — andere Retry-Outcomes (z. B. NotExecuted oder ein
        # Discovery-Ausfall im gefilterten Re-Run) sind kein Bestandsnachweis und dürfen
        # kein Flake-Ereignis erzeugen (analog Retry-FlakyTests.ps1).
        $recovered = $failed -and $retried -and $retryOutcome -eq "Passed"
        if ($recovered) {
            # Fehlschlag im Primär + grün im Retry desselben Laufs.
            $stat.FlakeEvents++
        }
        # Für die Primär-zu-Primär-Sequenz zählt der effektive Lauf-Ausgang: Ein durch
        # den Retry geretteter Lauf gilt als grün, damit ein Flake-Ereignis nicht doppelt
        # (Retry-grün + Folgelauf-grün) gezählt wird. Nur Passed/Failed-Retry-Outcomes
        # werden übernommen — ein nicht ausgeführter Re-Run verändert den Primär-Ausgang
        # nicht.
        $effectiveOutcome = if ($retryOutcome -eq "Passed" -or $retryOutcome -eq "Failed") { $retryOutcome } else { $entry.Value }
        if ($effectiveOutcome -ne "Failed" -and $stat.LastOutcome -eq "Failed") {
            # Fehlschlag im Primär von Lauf N, grün im Primär des nächsten Laufs.
            $stat.FlakeEvents++
        }
        $stat.LastOutcome = $effectiveOutcome
    }
}

# 4. Klassifikation und Report
$report = [System.Collections.Generic.List[object]]::new()
foreach ($entry in $stats.GetEnumerator()) {
    $stat = $entry.Value
    $failureRate = if ($stat.Executions -gt 0) { $stat.Failures / $stat.Executions } else { 0 }
    $classification = "stabil"
    if ($stat.FlakeEvents -ge $QuarantineMinFlakeEvents -or
        ($stat.Executions -ge $QuarantineMinExecutions -and $failureRate -ge $QuarantineMinFailureRate)) {
        $classification = "Quarantäne-Kandidat"
    }
    elseif ($stat.FlakeEvents -ge 1 -or $failureRate -ge $WatchMinFailureRate) {
        $classification = "beobachten"
    }
    $report.Add([PSCustomObject]@{
        Test           = $entry.Key
        Executions     = $stat.Executions
        Failures       = $stat.Failures
        FlakeEvents    = $stat.FlakeEvents
        FailureRate    = "{0:P0}" -f $failureRate
        Classification = $classification
    })
}

$severity = @{ "Quarantäne-Kandidat" = 0; "beobachten" = 1; "stabil" = 2 }
$sorted = @($report | Sort-Object { $severity[$_.Classification] }, Test)

Write-Host ""
Write-Host "| Test | Ausführungen | Fehlschläge | Flake-Ereignisse | Fehlrate | Klassifikation |"
Write-Host "|---|---|---|---|---|---|"
foreach ($row in $sorted) {
    Write-Host "| $($row.Test) | $($row.Executions) | $($row.Failures) | $($row.FlakeEvents) | $($row.FailureRate) | $($row.Classification) |"
}
Write-Host ""

$candidates = @($report | Where-Object { $_.Classification -eq "Quarantäne-Kandidat" })
$watched = @($report | Where-Object { $_.Classification -eq "beobachten" })
Write-Host "==> $($report.Count) Tests ausgewertet: $($candidates.Count) Quarantäne-Kandidat(en), $($watched.Count) beobachten, $($report.Count - $candidates.Count - $watched.Count) stabil."

foreach ($candidate in $candidates) {
    $message = "Quarantäne-Kandidat: $($candidate.Test) ($($candidate.FlakeEvents) Flake-Ereignisse, Fehlrate $($candidate.FailureRate)) — Empfehlung: Quarantäne-Issue anlegen + Fehlerbild in docs/help/stabilitaet/os-interface-tests.md dokumentieren."
    if ($inGitHubActions) { Write-Host "::warning::$message" }
    else { Write-Host "[warning] $message" -ForegroundColor Yellow }
}

exit 0
