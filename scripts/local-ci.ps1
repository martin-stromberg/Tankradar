<#
    local-ci.ps1

    Lokaler Prüflauf, der dieselben Prüfungen wie die CI-Pipeline (.github/workflows/pr-staging-ci.yml und
    staging-ci.yml) auf dem Entwicklungsrechner ausführt. Gedacht für den Fall, dass GitHub Actions wegen
    des Billing-Limits des privaten Repositories nicht läuft, und als schnelle Vorab-Prüfung vor einem PR.

    Geprüft wird (Reihenfolge wie in der Pipeline):
        1. Pipeline-Skripte: Node-Tests (npm test), Workflow-Validierung (validate-workflows.py samt Tests; erkennt u. a. .ipa-Uploads) und
                              Pruefung von scripts/iOS-Deployment.ps1 (Syntax, Hilfe, sauberer Abbruch ohne Mac)
        2. Restore
        3. Formatprüfung      dotnet format --verify-no-changes --severity error
        4. Sicherheitsprüfung dotnet list package --vulnerable --include-transitive --format json, sprachunabhängig
                              ausgewertet von scripts/check-vulnerabilities.mjs (dieselbe Logik wie die CI-Action)
        5. Statische Analyse  dotnet build -p:TreatWarningsAsErrors=true (Windows-Job-Simulation: wie die Windows-Jobs
                              der CI mit IncludeAndroidTarget/IncludeIosTarget/IncludeMacCatalystTarget=false; die
                              Variablen werden am Ende wiederhergestellt)
        6. Unit- und Integrationstests mit Coverage, Mindestabdeckung (Standard 70 %)
        7. FlaUI-E2E-Tests (blockierend wie in der Pipeline; die App läuft im Testmodus außerhalb des sichtbaren
                              Bildschirms. Rückfall auf den Vordergrundbetrieb: -E2EForeground. Diagnosedaten
                              fehlgeschlagener Tests liegen unter e2e-diagnostics\)
        8. Optional (-Package): Windows-Paket release-win-x64.zip + update.json
        9. iOS-Compile-Prüfung: net10.0-ios (Simulator-RID, Warnungen als Fehler, ohne Signierung); wird ohne
                              lokale iOS-Workload mit Hinweis übersprungen. Signierte Pakete/TestFlight nur auf dem Mac bzw. in der CI.

    Beispiele:
        .\scripts\local-ci.ps1
        .\scripts\local-ci.ps1 -SkipE2E -SkipSecurityScan     # schneller / offline
        .\scripts\local-ci.ps1 -Package -PackageVersion 0.1.0

    Exit-Code 0 = alle blockierenden Prüfungen bestanden, 1 = mindestens eine fehlgeschlagen.
#>
param(
    [Parameter(HelpMessage = "FlaUI-E2E-Tests überspringen (benötigen eine interaktive Desktop-Sitzung).")]
    [switch]$SkipE2E,

    [Parameter(HelpMessage = "FlaUI-E2E-Tests im Vordergrund statt außerhalb des Bildschirms ausführen (Rückfall; setzt TANKRADAR_E2E_WINDOW=foreground).")]
    [switch]$E2EForeground,

    [Parameter(HelpMessage = "Sicherheitsprüfung überspringen (benötigt Zugriff auf nuget.org).")]
    [switch]$SkipSecurityScan,

    [Parameter(HelpMessage = "Erzeugt zusätzlich das Windows-Release-Paket (release-win-x64.zip + update.json) unter artifacts/.")]
    [switch]$Package,

    [Parameter(HelpMessage = "Version für -Package (ohne führendes v).")]
    [string]$PackageVersion = "0.0.0-local",

    [Parameter(HelpMessage = "Mindest-Zeilenabdeckung in Prozent.")]
    [int]$CoverageThreshold = 70
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$solution = "Tankradar.sln"
$windowsJobEnv = [ordered]@{ IncludeAndroidTarget = 'false'; IncludeIosTarget = 'false'; IncludeMacCatalystTarget = 'false' }
# Gesamtzustand der Windows-Job-Simulation: zusätzlich IncludeWindowsTarget=true, damit eine vorbelegte Sitzung
# (z. B. IncludeWindowsTarget=false) nicht zu einem Projekt ohne Zielframework führt.
$windowsSimEnv = [ordered]@{ IncludeAndroidTarget = 'false'; IncludeIosTarget = 'false'; IncludeMacCatalystTarget = 'false'; IncludeWindowsTarget = 'true' }
$results = New-Object System.Collections.Generic.List[object]

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [switch]$BestEffort
    )
    Write-Host ""
    Write-Host "=== $Name ===" -ForegroundColor Cyan
    $started = Get-Date
    $ok = $true
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) { $ok = $false }
    }
    catch {
        Write-Host $_.Exception.Message -ForegroundColor Red
        $ok = $false
    }
    $seconds = [int]((Get-Date) - $started).TotalSeconds
    $status = if ($ok) { "OK" } elseif ($BestEffort) { "WARNUNG" } else { "FEHLER" }
    $results.Add([pscustomobject]@{ Schritt = $Name; Status = $status; Sekunden = $seconds }) | Out-Null
    if (-not $ok) {
        $color = if ($BestEffort) { "Yellow" } else { "Red" }
        Write-Host "$Name -> $status" -ForegroundColor $color
    }
    $global:LASTEXITCODE = 0
}

# Setzt eine Prozess-Umgebungsvariable; $null entfernt sie wirklich (PowerShell würde $null sonst als leere Zeichenfolge übergeben).
function Set-ProcessEnv([string]$Name, $Value) {
    if ($null -eq $Value) { [Environment]::SetEnvironmentVariable($Name, [NullString]::Value) }
    else { [Environment]::SetEnvironmentVariable($Name, [string]$Value) }
}

function Skip-Step([string]$Name, [string]$Reason) {
    Write-Host ""
    Write-Host "=== $Name === übersprungen ($Reason)" -ForegroundColor DarkGray
    $results.Add([pscustomobject]@{ Schritt = $Name; Status = "ÜBERSPRUNGEN"; Sekunden = 0 }) | Out-Null
}

$testResults = Join-Path $repoRoot "TestResults"
$coverageReport = Join-Path $repoRoot "coverage-report"
$e2eDiagnostics = Join-Path $repoRoot "e2e-diagnostics"
foreach ($dir in @($testResults, $coverageReport, $e2eDiagnostics)) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

Invoke-Step "Pipeline-Skripte: Node-Tests" { npm test }
Invoke-Step "Pipeline-Skripte: Workflow-Validierung" {
    python scripts/validate-workflows.py
    if ($LASTEXITCODE -ne 0) { throw "Workflow-Validierung fehlgeschlagen." }
    python scripts/test_validate_workflows.py
    if ($LASTEXITCODE -ne 0) { throw "Tests der Workflow-Validierung fehlgeschlagen." }
}
# Das Pruefskript muss unabhaengig von geerbten Include*Target-Variablen gruen sein: einmal ohne, einmal mit den
# Variablen der Windows-Jobs (inkl. Android) in einem Kindprozess.
Invoke-Step "iOS-Deployment-Skript (Syntax, Hilfe, Abbruch ohne Mac)" {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot "test-ios-deployment.ps1")
    if ($LASTEXITCODE -ne 0) { throw "iOS-Deployment-Pruefung fehlgeschlagen." }
}
Invoke-Step "iOS-Deployment-Skript mit Windows-Job-Umgebung (Include*Target=false)" {
    $saved = @{}
    foreach ($n in $windowsJobEnv.Keys) { $saved[$n] = [Environment]::GetEnvironmentVariable($n) }
    try {
        foreach ($n in $windowsJobEnv.Keys) { [Environment]::SetEnvironmentVariable($n, 'false') }
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot "test-ios-deployment.ps1")
        if ($LASTEXITCODE -ne 0) { throw "iOS-Deployment-Pruefung mit Windows-Job-Umgebung fehlgeschlagen." }
    }
    finally {
        foreach ($n in $windowsJobEnv.Keys) { Set-ProcessEnv $n $saved[$n] }
    }
}
# Ab hier wie in den Windows-Jobs der Pipeline: nur das Windows-Zielframework der MAUI-App (kein Android/iOS/MacCatalyst;
# Tankradar hat kein Android-Ziel, die Apple-Ziele baut nur der macOS-Job).
# Die Include*Target-Variablen werden für die Windows-Job-Simulation prozessweit gesetzt und am Ende in jedem Fall
# (auch bei Fehler/Abbruch) auf die ursprünglichen Werte zurückgesetzt, damit eine aufrufende PowerShell-Sitzung
# nicht dauerhaft nur noch Windows baut.
$originalEnv = @{}
foreach ($n in $windowsSimEnv.Keys) { $originalEnv[$n] = [Environment]::GetEnvironmentVariable($n) }
try {
    foreach ($n in $windowsSimEnv.Keys) { Set-ProcessEnv $n $windowsSimEnv[$n] }
    Invoke-Step "Restore" { dotnet restore $solution -p:Configuration=Release }
    Invoke-Step "Formatprüfung" { dotnet format $solution --verify-no-changes --no-restore --severity error }

    if ($SkipSecurityScan) {
        Skip-Step "Sicherheitsprüfung (Abhängigkeiten)" "-SkipSecurityScan"
    }
    else {
        Invoke-Step "Sicherheitsprüfung (Abhängigkeiten)" {
            # Sprachunabhängige JSON-Auswertung (die Textausgabe von dotnet list ist lokalisiert); Exit-Code 1 = Funde,
            # 2 = Aufruf-/Ausgabefehler. Bericht liegt unter vulnerable-packages.json (gitignoriert).
            node scripts/check-vulnerabilities.mjs --run $solution vulnerable-packages.json
            if ($LASTEXITCODE -ne 0) { throw "Sicherheitsprüfung fehlgeschlagen (anfällige Pakete oder Prüffehler, Exit-Code $LASTEXITCODE)." }
        }
    }

    Invoke-Step "Statische Analyse (Build mit Warnungen als Fehler)" {
        dotnet build $solution --configuration Release --no-restore -p:TreatWarningsAsErrors=true
    }

    foreach ($project in @("Unit", "Integration")) {
        Invoke-Step "Tests: $project" {
            dotnet test "src/Tankradar.Tests.$project" --configuration Release --no-build `
                --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory $testResults `
                --logger "trx;LogFileName=test-results-$($project.ToLower()).trx"
        }
    }

    Invoke-Step "Coverage-Bericht" {
        if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
            dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.11
        }
        reportgenerator "-reports:$testResults/**/coverage.cobertura.xml" "-targetdir:$coverageReport" "-reporttypes:TextSummary"
    }
    Invoke-Step "Mindest-Testabdeckung ($CoverageThreshold %)" {
        node scripts/check-coverage.mjs (Join-Path $coverageReport "Summary.txt") $CoverageThreshold
    }

    if ($SkipE2E) {
        Skip-Step "Tests: FlaUI-E2E" "-SkipE2E"
    }
    else {
        Invoke-Step "Tests: FlaUI-E2E" {
            $savedWindow = [Environment]::GetEnvironmentVariable('TANKRADAR_E2E_WINDOW')
            try {
                if ($E2EForeground) { Set-ProcessEnv 'TANKRADAR_E2E_WINDOW' 'foreground' }
                dotnet test "src/Tankradar.Tests.E2E" --configuration Release --no-build --results-directory $testResults `
                    --logger "trx;LogFileName=test-results-e2e.trx"
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "Diagnosedaten fehlgeschlagener E2E-Tests (Screenshot, UI-Baum, Fehlertext): $e2eDiagnostics" -ForegroundColor Yellow
                }
            }
            finally {
                Set-ProcessEnv 'TANKRADAR_E2E_WINDOW' $savedWindow
            }
        }
    }

    if ($Package) {
        Invoke-Step "Windows-Paket (release-win-x64.zip)" {
            & (Join-Path $PSScriptRoot "package-windows.ps1") -Version $PackageVersion -Tag "v$PackageVersion" -OutputDirectory "artifacts"
        }
    }
    else {
        Skip-Step "Windows-Paket (release-win-x64.zip)" "mit -Package aktivierbar"
    }

    # Eigener Schritt: iOS-Compile-Prüfung (Apple-Ziel aktiv, Android aus), da die Windows-Job-Simulation oben die
    # Apple-Ziele ausblendet. Baut ohne Signierung für den Simulator (wie der unsignierte Build in der CI).
    # Ohne lokale iOS-Workload wird der Schritt mit Hinweis übersprungen (kein Fehlschlag, keine Installation).
    $iosWorkload = $false
    try { $iosWorkload = [bool]((dotnet workload list 2>$null | Out-String) -match '(?m)^\s*ios\s') } catch { }
    if (-not $iosWorkload) {
        Skip-Step "iOS-Compile-Prüfung (net10.0-ios)" "iOS-Workload nicht installiert (dotnet workload restore Tankradar.sln)"
    }
    else {
        Invoke-Step "iOS-Compile-Prüfung (net10.0-ios)" {
            [Environment]::SetEnvironmentVariable('IncludeAndroidTarget', 'false')
            [Environment]::SetEnvironmentVariable('IncludeIosTarget', 'true')
            [Environment]::SetEnvironmentVariable('IncludeMacCatalystTarget', 'false')
            [Environment]::SetEnvironmentVariable('IncludeWindowsTarget', 'false')
            $buildExit = 1
            try {
                # Eigener Restore (impliziert im Build), weil der Restore oben ohne Apple-Ziele erfolgte.
                dotnet build "src/Tankradar.MAUI/Tankradar.MAUI.csproj" --configuration Release --framework net10.0-ios `
                    -p:RuntimeIdentifier=iossimulator-arm64 -p:TreatWarningsAsErrors=true -p:MauiXamlInflator=XamlC
                $buildExit = $LASTEXITCODE
            }
            finally {
                # Der iOS-Restore überschreibt obj/project.assets.json der MAUI-App (ohne Windows-Ziel). Damit spätere
                # --no-restore-Builds/-Tests (auch nach diesem Lauf) nicht darauf stoßen, wird der Windows-Zustand
                # wiederhergestellt: Umgebung der Windows-Job-Simulation setzen und erneut restoren.
                foreach ($n in $windowsSimEnv.Keys) { Set-ProcessEnv $n $windowsSimEnv[$n] }
                dotnet restore $solution -p:Configuration=Release
                if ($LASTEXITCODE -ne 0 -and $buildExit -eq 0) { $buildExit = $LASTEXITCODE }
            }
            if ($buildExit -ne 0) { throw "iOS-Compile-Prüfung fehlgeschlagen (Exit-Code $buildExit)." }
        }
    }

}
finally {
    foreach ($n in $windowsSimEnv.Keys) { Set-ProcessEnv $n $originalEnv[$n] }
}

Write-Host ""
Write-Host "=== Zusammenfassung ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize | Out-String | Write-Host
$failed = @($results | Where-Object { $_.Status -eq "FEHLER" })
if ($failed.Count -gt 0) {
    Write-Host "Lokaler Prüflauf FEHLGESCHLAGEN ($($failed.Count) blockierende Prüfung(en))." -ForegroundColor Red
    exit 1
}
Write-Host "Lokaler Prüflauf erfolgreich." -ForegroundColor Green
exit 0
