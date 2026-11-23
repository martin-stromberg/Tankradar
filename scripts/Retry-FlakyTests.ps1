#Requires -Version 7.0
<#
.SYNOPSIS
    Wertet das Primär-TRX eines Testlaufs aus und wiederholt fehlgeschlagene, als
    "Flaky" markierte Tests genau einmal gefiltert.

.DESCRIPTION
    Ablauf:
      1. Primär-TRX parsen: Namen aller "Failed"-Ergebnisse einsammeln (Theory-
         Datenzeilen werden auf den Methoden-FQN normalisiert).
      2. Flaky-Menge des Projekts via "dotnet test --no-build --list-tests
         --filter Category=Flaky" ermitteln.
      3. Schnittmenge leer  -> "keine Flaky-Fehlschläge", Exit 0 (kein Retry).
         Sonst genau ein Re-Run: --filter "(Category=Flaky)&(FullyQualifiedName~...)".
      4. Retry-TRX auswerten: recovered (fehlgeschlagen -> bestanden) vs. persistent.
         Persistente Fehlschläge -> Exit 1 (ehrliches Signal; in der CI bleibt der
         Job durch continue-on-error trotzdem nicht-blockierend).

    Fehlendes oder unauswertbares Primär-TRX gilt als "keine Flaky-Fehlschläge"
    (Exit 0), damit ein als if: failure() laufender CI-Schritt bei Build-Fehlschlag
    oder fehlendem TRX nichts verschleiert.

.PARAMETER TestProject
    Pfad zum Testprojekt (.csproj), dessen Primärlauf ausgewertet wird.

.PARAMETER PrimaryTrx
    Pfad zur TRX-Datei des Primärlaufs.

.PARAMETER Configuration
    Build-Konfiguration (Standard: Debug).

.PARAMETER RetryTrxFileName
    Dateiname des Retry-TRX im selben TestResults-Verzeichnis.
    Standard: <primärname>-flaky-retry.trx

.EXAMPLE
    ./scripts/Retry-FlakyTests.ps1 -TestProject src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj -PrimaryTrx src/Tankradar.Tests.E2E/TestResults/test-results-os-interface-e2e.trx -Configuration Release
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TestProject,
    [Parameter(Mandatory = $true)]
    [string]$PrimaryTrx,
    [string]$Configuration = "Debug",
    [string]$RetryTrxFileName = ""
)

$ErrorActionPreference = "Stop"
$inGitHubActions = $env:GITHUB_ACTIONS -eq "true"

. (Join-Path $PSScriptRoot "lib/Trx.ps1")
. (Join-Path $PSScriptRoot "lib/TestDiscovery.ps1")

function Write-Annotation {
    param([string]$Kind, [string]$Message)
    if ($inGitHubActions) { Write-Host "::$Kind`::$Message" }
    else { Write-Host "[$Kind] $Message" }
}

function ConvertTo-MethodName {
    # Theory-Datenzeilen ("Ns.Class.Method(arg: 1)") auf den Methoden-FQN normalisieren.
    param([string]$TestName)
    if ($TestName -match '^([A-Za-z0-9_]+(\.[A-Za-z0-9_]+)+)\(.*\)$') { return $matches[1] }
    return $TestName
}

# 1. Primär-TRX auswerten
$primaryResults = Get-TrxTestResults -TrxPath $PrimaryTrx
if ($null -eq $primaryResults) {
    Write-Host "Keine Flaky-Fehlschläge: Primär-TRX '$PrimaryTrx' fehlt oder ist unauswertbar."
    exit 0
}

$failedMethods = @($primaryResults.GetEnumerator() |
    Where-Object { $_.Value -eq "Failed" } |
    ForEach-Object { ConvertTo-MethodName $_.Key } |
    Sort-Object -Unique)
if ($failedMethods.Count -eq 0) {
    Write-Host "Keine Flaky-Fehlschläge: Der Primärlauf hat keine fehlgeschlagenen Tests."
    exit 0
}

# 2. Flaky-Menge des Projekts ermitteln (Helper aus lib/TestDiscovery.ps1)
$flakyMethods = Get-FlakyTestNames -ProjectPath $TestProject -Configuration $Configuration

$retryMethods = @($failedMethods | Where-Object { $flakyMethods.Contains($_) })
if ($retryMethods.Count -eq 0) {
    Write-Host "Keine Flaky-Fehlschläge: $($failedMethods.Count) Fehlschlag/Fehlschläge im Primärlauf, davon keiner Flaky-markiert."
    exit 0
}

Write-Host "==> $($retryMethods.Count) Flaky-Fehlschlag/Fehlschläge werden genau einmal wiederholt:" -ForegroundColor Cyan
$retryMethods | ForEach-Object { Write-Host "    $_" }

# 3. Genau ein gefilterter Re-Run
$resultsDirectory = Split-Path -Parent $PrimaryTrx
if ([string]::IsNullOrEmpty($resultsDirectory)) {
    # Reiner Dateiname ohne Verzeichnisanteil (lokaler Aufruf aus dem TRX-Verzeichnis):
    # "" würde --results-directory "" und ein unbrauchbares Join-Path erzeugen.
    $resultsDirectory = "."
}
if ([string]::IsNullOrWhiteSpace($RetryTrxFileName)) {
    $RetryTrxFileName = [IO.Path]::GetFileNameWithoutExtension($PrimaryTrx) + "-flaky-retry.trx"
}
$nameFilter = ($retryMethods | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
$retryOutput = & dotnet test $TestProject -c $Configuration --no-build `
    --filter "(Category=Flaky)&($nameFilter)" `
    --logger "trx;LogFileName=$RetryTrxFileName" `
    --results-directory $resultsDirectory `
    --logger "console;verbosity=normal" 2>&1
$retryOutput | ForEach-Object { Write-Host $_ }

# 4. Retry-TRX auswerten: Nur "Passed" gilt als recovered, "Failed" als persistent.
# Alles andere — kein auswertbares Retry-Ergebnis (Retry-TRX fehlt/unauswertbar oder
# der Test lief im gefilterten Re-Run nicht) sowie Nicht-Failed-Outcomes (z. B.
# NotExecuted durch ein Skip-Flag oder Discovery-Ausfall) — ist kein Bestandsnachweis
# und wird als eigene, nicht-recovered Kategorie gemeldet (ebenfalls Exit 1).
$retryResults = Get-TrxTestResults -TrxPath (Join-Path $resultsDirectory $RetryTrxFileName)
$persistent = [System.Collections.Generic.List[string]]::new()
$indeterminate = [System.Collections.Generic.List[string]]::new()
foreach ($method in $retryMethods) {
    $retryOutcomes = @()
    if ($null -ne $retryResults) {
        $retryOutcomes = @($retryResults.GetEnumerator() |
            Where-Object { (ConvertTo-MethodName $_.Key) -eq $method } |
            ForEach-Object { $_.Value })
    }
    if ($retryOutcomes.Count -gt 0 -and @($retryOutcomes | Where-Object { $_ -ne "Passed" }).Count -eq 0) {
        Write-Annotation -Kind "notice" -Message "Flaky-Retry recovered: $method (fehlgeschlagen im Primärlauf, bestanden im Retry)"
    }
    elseif ($retryOutcomes.Count -gt 0 -and @($retryOutcomes | Where-Object { $_ -eq "Failed" }).Count -gt 0) {
        $persistent.Add($method)
    }
    elseif ($retryOutcomes.Count -eq 0) {
        $indeterminate.Add("$method (kein auswertbares Retry-Ergebnis — Infrastruktur-/Discovery-Ausfall möglich)")
    }
    else {
        $indeterminate.Add("$method (kein Passed-Ergebnis im Retry; Retry-Outcome: $(($retryOutcomes | Sort-Object -Unique) -join ', '))")
    }
}

foreach ($method in $persistent) {
    Write-Annotation -Kind "warning" -Message "Flaky-Retry persistent: $method (fehlgeschlagen im Primärlauf UND im Retry)"
}
foreach ($method in $indeterminate) {
    Write-Annotation -Kind "warning" -Message "Flaky-Retry nicht recovered: $method"
}

if ($persistent.Count -gt 0 -or $indeterminate.Count -gt 0) {
    Write-Host "==> $($persistent.Count) Flaky-Test(s) auch im Retry fehlgeschlagen (persistent), $($indeterminate.Count) ohne Passed-Ergebnis (nicht recovered)." -ForegroundColor Red
    exit 1
}
Write-Host "==> Alle Flaky-Fehlschläge im Retry bestanden (recovered)." -ForegroundColor Green
exit 0
