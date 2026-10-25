<#
.SYNOPSIS
    Erstellt einen startfaehigen Windows-Zwischenstand von Tankatlas unter review-versions/.

.DESCRIPTION
    Baut die Tankradar.MAUI-App als ungepacktes Windows-Release, kopiert die Build-Ausgabe nach
    review-versions/<Version>_<JJJJ-MM-TT>/bin/ und legt dort eine CHANGELOG.md an. Das Verzeichnis
    review-versions/ ist per .gitignore von Commits ausgeschlossen.

.PARAMETER Version
    Die Versionsnummer im Format X.Y.Z (z. B. 0.1.0).

.PARAMETER ChangelogFile
    Pflicht. Entweder der Pfad zu einer Datei mit dem Changelog-Text oder der Changelog-Text selbst.
    Ein Aufruf ohne (oder mit leerem) Changelog wird abgelehnt, bevor gebaut wird.

.EXAMPLE
    .\scripts\create-review-version.ps1 -Version 0.1.0 -ChangelogFile "Initial foundation release"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [string]$ChangelogFile
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Ungueltiges Versionsformat '$Version'. Erwartet wird das Format X.Y.Z (z. B. 0.1.0)."
    exit 1
}

# Der Changelog ist Pflicht und wird vor dem Build geprueft (kein Zwischenstand ohne Beschreibung der Aenderungen).
if ($ChangelogFile -and (Test-Path $ChangelogFile -PathType Leaf)) {
    $changelogEntry = Get-Content -Path $ChangelogFile -Raw -Encoding UTF8
}
else {
    $changelogEntry = $ChangelogFile
}

if ([string]::IsNullOrWhiteSpace($changelogEntry)) {
    Write-Error "Es wurde kein Changelog angegeben. Bitte -ChangelogFile mit einer kurzen Beschreibung der Aenderungen (Text oder Dateipfad) uebergeben."
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$targetFramework = "net10.0-windows10.0.19041.0"
$mauiProject = Join-Path $repoRoot "src\Tankradar.MAUI\Tankradar.MAUI.csproj"

if (-not (Test-Path $mauiProject)) {
    Write-Error "Hauptprojekt nicht gefunden: $mauiProject"
    exit 1
}

$dateStamp = Get-Date -Format "yyyy-MM-dd"
$versionDirName = "${Version}_${dateStamp}"
$versionDir = Join-Path $repoRoot "review-versions\$versionDirName"
$binDir = Join-Path $versionDir "bin"
$changelogPath = Join-Path $versionDir "CHANGELOG.md"

Write-Host "Erzeuge Windows-Zwischenstand '$versionDirName' ..."

if (Test-Path $versionDir) {
    Write-Host "Verzeichnis existiert bereits und wird ueberschrieben (idempotenter Aufruf): $versionDir"
    Remove-Item -Path $versionDir -Recurse -Force
}

New-Item -ItemType Directory -Path $versionDir -Force | Out-Null

# MAUI leitet die Versionsangabe der App (AppInfo.Version) aus dem zwischengespeicherten Package.appxmanifest unter obj/.../resizetizer/m ab.
# Dieses Ziel laeuft inkrementell und beruecksichtigt eine geaenderte Eigenschaft ApplicationDisplayVersion nicht; die App wuerde sonst
# weiter die Version eines frueheren Builds (z. B. 0.1.0.1) melden. Deshalb wird der Zwischenstand vor dem Publish entfernt und neu erzeugt.
$releaseObjDir = Join-Path $repoRoot "src\Tankradar.MAUI\obj\Release\$targetFramework"
if (Test-Path $releaseObjDir) {
    Get-ChildItem -Path $releaseObjDir -Recurse -Directory -Filter "resizetizer" -ErrorAction SilentlyContinue | ForEach-Object {
        $manifestDir = Join-Path $_.FullName "m"
        if (Test-Path $manifestDir) { Remove-Item -Path $manifestDir -Recurse -Force }
    }
    Get-ChildItem -Path $releaseObjDir -Recurse -File -Filter "mauimanifest.stamp" -ErrorAction SilentlyContinue | Remove-Item -Force
}

Write-Host "Fuehre Windows-Release-Build aus (dotnet publish, Framework $targetFramework) ..."
& dotnet publish $mauiProject -c Release -f $targetFramework -p:Version=$Version -p:InformationalVersion=$Version -p:ApplicationDisplayVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Remove-Item -Path $versionDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Error "Der Release-Build ist fehlgeschlagen (Exit-Code $LASTEXITCODE). Es wurde kein Zwischenstand erzeugt."
    exit $LASTEXITCODE
}

# Pruefung: Die vom Build erzeugten Assembly-Metadaten muessen die Zwischenstandsversion tragen (AppInfo.Version = <Version>.<ApplicationVersion>),
# denn diese Angabe zeigt die App an und schreibt sie in das Manifest einer Sicherung.
$assemblyInfoFiles = Get-ChildItem -Path $releaseObjDir -Recurse -File -Filter "Tankradar.MAUI.AssemblyInfo.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like "*win-x64*" }
$reportedVersion = $null
foreach ($file in ($assemblyInfoFiles | Sort-Object LastWriteTime)) {
    $match = Select-String -Path $file.FullName -Pattern 'Microsoft\.Maui\.ApplicationModel\.AppInfo\.Version", "([^"]+)"' | Select-Object -First 1
    if ($match) { $reportedVersion = $match.Matches[0].Groups[1].Value }
}
if (-not $reportedVersion -or -not $reportedVersion.StartsWith("$Version.")) {
    Remove-Item -Path $versionDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Error "Die App wuerde die Version '$reportedVersion' melden statt '$Version'. Es wurde kein Zwischenstand erzeugt. Bitte src\Tankradar.MAUI\obj\Release loeschen und erneut versuchen."
    exit 1
}
Write-Host "Die App meldet die Version $reportedVersion."

$publishDir = Join-Path $repoRoot "src\Tankradar.MAUI\bin\Release\$targetFramework\win-x64\publish"

if (-not (Test-Path $publishDir)) {
    Write-Error "Erwartetes Build-Ausgabeverzeichnis wurde nicht gefunden: $publishDir"
    exit 1
}

# Pruefung: Der Resizetizer muss das App-Icon (Windows-ICO, Taskleistengroessen, Kacheln) erzeugt haben; sonst startet die App mit dem Standardsymbol.
foreach ($iconFile in @("appicon.ico", "appiconLogo.targetsize-16.png", "appiconLogo.targetsize-32.png", "appiconLogo.targetsize-256.png", "appiconStoreLogo.scale-400.png")) {
    if (-not (Test-Path (Join-Path $publishDir $iconFile))) {
        Remove-Item -Path $versionDir -Recurse -Force -ErrorAction SilentlyContinue
        Write-Error "Die Icon-Ressource '$iconFile' fehlt in der Build-Ausgabe ($publishDir). Es wurde kein Zwischenstand erzeugt."
        exit 1
    }
}

Write-Host "Kopiere Build-Ausgabe nach '$binDir' ..."
Copy-Item -Path $publishDir -Destination $binDir -Recurse -Force

$changelogContent = "# Changelog - Tankatlas $Version ($dateStamp)`n`n$changelogEntry`n"
Set-Content -Path $changelogPath -Value $changelogContent -Encoding UTF8

$exePath = Join-Path $binDir "Tankradar.MAUI.exe"

Write-Host ""
Write-Host "Windows-Zwischenstand erfolgreich erstellt."
Write-Host "Verzeichnis:        $versionDir"
Write-Host "Ausfuehrbare Datei: $exePath"
Write-Host "Changelog:          $changelogPath"

exit 0
