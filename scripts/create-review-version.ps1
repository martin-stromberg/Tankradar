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
    Optional. Entweder der Pfad zu einer Datei mit dem Changelog-Text oder der Changelog-Text selbst.

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

Write-Host "Fuehre Windows-Release-Build aus (dotnet publish, Framework $targetFramework) ..."
& dotnet publish $mauiProject -c Release -f $targetFramework -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Remove-Item -Path $versionDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Error "Der Release-Build ist fehlgeschlagen (Exit-Code $LASTEXITCODE). Es wurde kein Zwischenstand erzeugt."
    exit $LASTEXITCODE
}

$publishDir = Join-Path $repoRoot "src\Tankradar.MAUI\bin\Release\$targetFramework\win-x64\publish"

if (-not (Test-Path $publishDir)) {
    Write-Error "Erwartetes Build-Ausgabeverzeichnis wurde nicht gefunden: $publishDir"
    exit 1
}

Write-Host "Kopiere Build-Ausgabe nach '$binDir' ..."
Copy-Item -Path $publishDir -Destination $binDir -Recurse -Force

if ($ChangelogFile -and (Test-Path $ChangelogFile -PathType Leaf)) {
    $changelogEntry = Get-Content -Path $ChangelogFile -Raw
}
elseif ($ChangelogFile) {
    $changelogEntry = $ChangelogFile
}
else {
    $changelogEntry = "Keine Changelog-Beschreibung angegeben."
}

$changelogContent = "# Changelog - Tankatlas $Version ($dateStamp)`n`n$changelogEntry`n"
Set-Content -Path $changelogPath -Value $changelogContent -Encoding UTF8

$exePath = Join-Path $binDir "Tankradar.MAUI.exe"

Write-Host ""
Write-Host "Windows-Zwischenstand erfolgreich erstellt."
Write-Host "Verzeichnis:        $versionDir"
Write-Host "Ausfuehrbare Datei: $exePath"
Write-Host "Changelog:          $changelogPath"

exit 0
