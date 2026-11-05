<#
    package-windows.ps1

    Baut die Windows-Variante von Tankradar (unpackaged, self-contained) und erzeugt daraus das
    Release-Artefakt `release-win-x64.zip` sowie das Update-Manifest `update.json`.

    Das ZIP enthält direkt im Wurzelverzeichnis `Tankradar.MAUI.exe` samt .NET-Laufzeit (self-contained,
    CoreCLR statt Mono: für MAUI/Windows gibt es kein Mono-Runtime-Pack); nach dem Entpacken startet die
    App ohne Installation. Es wird bewusst KEIN Installer (MSIX/Setup) erzeugt.

    Wird sowohl von der Composite-Action .github/actions/build-and-package als auch vom lokalen
    Prüflauf (scripts/local-ci.ps1 -Package) verwendet, damit beide dasselbe Artefakt erzeugen.

    Beispiel:
        .\scripts\package-windows.ps1 -Version 0.1.0-rc.1 -Tag v0.1.0-rc.1 -OutputDirectory artifacts
#>
param(
    [Parameter(Mandatory = $true, HelpMessage = "Version ohne führendes v, ggf. mit Pre-Release-Suffix (z. B. 0.2.0-rc.1).")]
    [string]$Version,

    [Parameter(Mandatory = $true, HelpMessage = "Release-Tag (z. B. v0.2.0-rc.1); bestimmt die Download-URL im Manifest.")]
    [string]$Tag,

    [Parameter(HelpMessage = "Repository im Format owner/name für die Download-URL. Standard: GITHUB_REPOSITORY.")]
    [string]$Repository = $env:GITHUB_REPOSITORY,

    [Parameter(HelpMessage = "Zielverzeichnis für ZIP, update.json und das Publish-Verzeichnis.")]
    [string]$OutputDirectory = "artifacts"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/Tankradar.MAUI/Tankradar.MAUI.csproj"
$framework = "net10.0-windows10.0.19041.0"
$displayVersion = ($Version -split "-")[0]

if (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot $OutputDirectory
}
$publishDir = Join-Path $OutputDirectory "publish-win-x64"
$zipPath = Join-Path $OutputDirectory "release-win-x64.zip"
$manifestPath = Join-Path $OutputDirectory "update.json"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

Write-Host "Veröffentliche Windows-App $Version (unpackaged, self-contained, win-x64, ohne Installer) ..."
# Das Windows-Paket ist öffentlich herunterladbar und enthält deshalb nie den Tankerkönig-Schlüssel:
# Umgebungsvariablen leeren und die lokale props-Datei ausblenden.
Remove-Item Env:TANKRADAR_FUEL_PRICE_API_KEY -ErrorAction SilentlyContinue
Remove-Item Env:FUEL_PRICE_API_KEY -ErrorAction SilentlyContinue
$noLocalProps = Join-Path ([System.IO.Path]::GetTempPath()) "tankradar-no-local-props.props"
& dotnet publish $project `
    --configuration Release `
    -p:TankerkoenigLocalPropsFile=$noLocalProps `
    --framework $framework `
    -p:WindowsPackageType=None `
    -p:SelfContained=true `
    -p:RuntimeIdentifier=win-x64 `
    -p:UseMonoRuntime=false `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:ApplicationDisplayVersion=$displayVersion `
    --output $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ist fehlgeschlagen (Exit-Code $LASTEXITCODE)." }

$exe = Join-Path $publishDir "Tankradar.MAUI.exe"
if (-not (Test-Path $exe)) { throw "Tankradar.MAUI.exe fehlt im Publish-Verzeichnis: $publishDir" }

Write-Host "Erzeuge $zipPath ..."
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$sha = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item $zipPath).Length
$assetUrl = if ($Repository) {
    "https://github.com/$Repository/releases/download/$Tag/release-win-x64.zip"
} else {
    "release-win-x64.zip"
}

$manifest = [ordered]@{
    version      = $Version
    releaseNotes = "Tankatlas release $Tag"
    publishedAt  = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    assets       = @(
        [ordered]@{
            platform          = "windows"
            runtimeIdentifier = "win-x64"
            assetName         = "release-win-x64.zip"
            assetUrl          = $assetUrl
            sha256            = $sha
            sizeBytes         = $size
        }
    )
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding utf8

Write-Host "Fertig: $zipPath ($size Bytes, SHA256 $sha)" -ForegroundColor Green
Write-Host "Manifest: $manifestPath"
