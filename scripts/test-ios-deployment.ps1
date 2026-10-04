<#
.SYNOPSIS
    Plattformunabhaengige Pruefungen fuer scripts/iOS-Deployment.ps1 (ohne Mac, ohne Netzwerk).

.DESCRIPTION
    Was ohne Mac pruefbar ist, wird hier geprueft:
      - PowerShell-Parser findet keine Syntaxfehler,
      - Comment-Based-Help (Get-Help) ist vorhanden,
      - keine Reste der Vorlage (Reporter, REPORTER_IOS_, Lizenzzeile),
      - Aktionen, die einen Mac benoetigen, brechen unter Windows sauber mit verstaendlicher Meldung
        und Exit-Code 1 ab (list: Hinweis, Exit-Code 0),
      - reine Hilfsfunktionen (Buildnummer erhoehen, Transporter-Suchskript) liefern das erwartete Ergebnis,
      - die Pipeline referenziert den ersetzten Baustein build-ios nicht mehr.
    Nicht pruefbar ohne Mac: Pair-to-Mac, SSH/scp, Keychain, Signierung, iTMSTransporter, Upload.

    Exit-Code 0 = alle Pruefungen bestanden, 1 = mindestens eine fehlgeschlagen.
#>
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $PSScriptRoot "iOS-Deployment.ps1"
$failures = New-Object System.Collections.Generic.List[string]
$tempDir = Join-Path ([IO.Path]::GetTempPath()) ("ios-deploy-test-" + [Guid]::NewGuid().ToString("n"))
New-Item -ItemType Directory -Path $tempDir | Out-Null

function Assert-That {
    param([string]$Name, [bool]$Condition, [string]$Detail = "")
    if ($Condition) {
        Write-Host "  OK      $Name"
    }
    else {
        Write-Host "  FEHLER  $Name $Detail" -ForegroundColor Red
        $failures.Add($Name) | Out-Null
    }
}

function Get-FunctionSource {
    param($Ast, [string]$Name)
    $fn = $Ast.Find({ param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq $Name }, $true)
    if (-not $fn) { throw "Funktion $Name nicht gefunden." }
    return $fn.Extent.Text
}

try {
    $content = [IO.File]::ReadAllText($scriptPath)

    Write-Host "Syntax"
    $tokens = $null; $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
    Assert-That "Parser ohne Fehler" ($errors.Count -eq 0) ($errors | ForEach-Object { $_.Message } | Out-String)

    Write-Host "Hilfe"
    $help = Get-Help $scriptPath
    Assert-That "Get-Help liefert Synopsis" (-not [string]::IsNullOrWhiteSpace($help.Synopsis) -and $help.Synopsis -notlike "*[[]-Action*")
    $paramNames = @($help.parameters.parameter | ForEach-Object { $_.name })
    foreach ($p in @("Action", "BundleId", "ServerAddress", "ApiKeyPath", "IpaPath", "NoBumpBuildNumber")) {
        Assert-That "Parameter -$p dokumentiert" ($paramNames -contains $p)
    }

    Write-Host "Anpassung an Tankradar"
    Assert-That "keine Reporter-Reste" ($content -notmatch "(?i)reporter")
    Assert-That "keine Lizenzzeile der Vorlage" ($content -notmatch "PolyForm")
    Assert-That "Projektpfad Tankradar.MAUI" ($content -match "src/Tankradar\.MAUI/Tankradar\.MAUI\.csproj")
    Assert-That "Umgebungsvariablen TANKRADAR_IOS_*" ($content -match "TANKRADAR_IOS_CODESIGN_KEY" -and $content -match "TANKRADAR_IOS_MAC_SERVER_ADDRESS")
    foreach ($legacy in @(".github/actions/build-ios", "actions/build-ios")) {
        $hits = @(Get-ChildItem -Path (Join-Path $repoRoot ".github") -Recurse -File | Select-String -SimpleMatch $legacy)
        Assert-That "keine Referenz auf $legacy" ($hits.Count -eq 0)
    }

    Write-Host "Bundle-ID"
    $expectedBundleId = "de.martinstromberg.tankradar"
    $csprojText = [IO.File]::ReadAllText((Join-Path $repoRoot "src/Tankradar.MAUI/Tankradar.MAUI.csproj"))
    $configText = [IO.File]::ReadAllText((Join-Path $repoRoot "src/Tankradar.MAUI/Services/AppConfiguration.cs"))
    $settingsText = [IO.File]::ReadAllText((Join-Path $repoRoot "src/Tankradar.MAUI/Resources/Raw/appsettings.json"))
    Assert-That "csproj ApplicationId = $expectedBundleId" ($csprojText -match "<ApplicationId>$([regex]::Escape($expectedBundleId))</ApplicationId>")
    Assert-That "AppConfiguration.DefaultBundleId = $expectedBundleId" ($configText.Contains("DefaultBundleId = `"$expectedBundleId`""))
    Assert-That "appsettings.json BundleId = $expectedBundleId" ($settingsText.Contains("`"$expectedBundleId`""))

    Write-Host "Anzeigename"
    Assert-That "csproj ApplicationTitle = Tankatlas (iOS-Anzeigename)" ($csprojText -match "<ApplicationTitle>Tankatlas</ApplicationTitle>")
    Assert-That "AppConfiguration.AppDisplayName = Tankatlas" ($configText.Contains("AppDisplayName = `"Tankatlas`""))
    Assert-That "Bundle-ID bleibt de.martinstromberg.tankradar" ($csprojText -match "<ApplicationId>de\.martinstromberg\.tankradar</ApplicationId>")

    Write-Host "Sauberer Abbruch ohne Mac"
    $hostPath = (Get-Process -Id $PID).Path
    $envNames = @(Get-ChildItem Env: | Where-Object { $_.Name -like "TANKRADAR_IOS_*" } | ForEach-Object { $_.Name })
    $savedEnv = @{}
    foreach ($n in $envNames) { $savedEnv[$n] = [Environment]::GetEnvironmentVariable($n); [Environment]::SetEnvironmentVariable($n, $null) }
    try {
        $onWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
        if ($onWindowsHost) {
            $cases = @(
                @{ Action = "build"; Exit = 1; Pattern = "Pair-to-Mac" },
                @{ Action = "simulator"; Exit = 1; Pattern = "Pair-to-Mac" },
                @{ Action = "device"; Exit = 1; Pattern = "Pair-to-Mac" },
                @{ Action = "store"; Exit = 1; Pattern = "Pair-to-Mac" },
                @{ Action = "upload"; Exit = 1; Pattern = "Pflichtparameter" },
                @{ Action = "list"; Exit = 0; Pattern = "nur auf macOS" }
            )
            foreach ($case in $cases) {
                $out = & $hostPath -NoProfile -File $scriptPath -Action $case.Action -NoPrompt -LogDir $tempDir 2>&1 | Out-String
                $code = $LASTEXITCODE
                Assert-That "-Action $($case.Action): Exit-Code $($case.Exit)" ($code -eq $case.Exit) "(war $code)"
                Assert-That "-Action $($case.Action): Meldung enthaelt '$($case.Pattern)'" ($out -match [regex]::Escape($case.Pattern)) $out
            }
        }
        else {
            Write-Host "  uebersprungen (nur unter Windows sinnvoll)"
        }
    }
    finally {
        foreach ($n in $savedEnv.Keys) { [Environment]::SetEnvironmentVariable($n, $savedEnv[$n]) }
    }

    Write-Host "Hilfsfunktionen"
    # Funktionen aus dem Skript herausloesen, ohne das Skript selbst auszufuehren.
    $csproj = Join-Path $tempDir "Test.csproj"
    $csprojTemplate = "<Project><PropertyGroup><ApplicationDisplayVersion>0.1.0</ApplicationDisplayVersion><ApplicationVersion>41</ApplicationVersion></PropertyGroup></Project>"
    [IO.File]::WriteAllText($csproj, $csprojTemplate)
    $projectPath = $csproj
    $Version = ""
    Invoke-Expression (Get-FunctionSource $ast "Update-BuildNumber")
    Update-BuildNumber 6>&1 | Out-Null
    Assert-That "Update-BuildNumber erhoeht ApplicationVersion 41 -> 42" ([IO.File]::ReadAllText($csproj) -match "<ApplicationVersion>42</ApplicationVersion>")
    $Version = "0.2.0"
    Update-BuildNumber 6>&1 | Out-Null
    $text = [IO.File]::ReadAllText($csproj)
    Assert-That "Update-BuildNumber -Version setzt DisplayVersion und Buildnummer 1" ($text -match "<ApplicationDisplayVersion>0\.2\.0</ApplicationDisplayVersion>" -and $text -match "<ApplicationVersion>1</ApplicationVersion>")

    $TransporterPath = ""
    Invoke-Expression (Get-FunctionSource $ast "Get-TransporterFindScript")
    $find = Get-TransporterFindScript
    Assert-That "Transporter-Suche kennt /usr/local/itms und Transporter.app" ($find -match "/usr/local/itms/bin/iTMSTransporter" -and $find -match "Transporter\.app")
    $TransporterPath = "/opt/it's/iTMSTransporter"
    $find = Get-TransporterFindScript
    Assert-That "Transporter-Override wird fuer die Shell maskiert" ($find.Contains("/opt/it'\''s/iTMSTransporter"))

    Write-Host "Store-Validierung (Invarianten)"
    $validation = Get-FunctionSource $ast "Invoke-IpaValidation"
    Assert-That "Fehlendes PrivacyInfo.xcprivacy ist ein Fehler (exit 1)" ($validation -match 'FEHLER: PrivacyInfo\.xcprivacy fehlt im Bundle-Root"; exit 1')
    Assert-That "Ungueltiges PrivacyInfo.xcprivacy ist ein Fehler" ($validation -match 'FEHLER: PrivacyInfo\.xcprivacy ist kein gueltiges plist')
    Assert-That "ITSAppUsesNonExemptEncryption ungleich false ist ein Fehler" ($validation -match 'FEHLER: ITSAppUsesNonExemptEncryption nicht false')
    Assert-That "Keine Warnung-Abschwaechung fuer Privacy-Manifest/Verschluesselung" ($validation -notmatch 'WARNUNG: (PrivacyInfo|ITSAppUsesNonExemptEncryption)')

    Write-Host "Projektdateien zur Store-Validierung"
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $mauiDir = Join-Path $repoRoot "src/Tankradar.MAUI"
    Assert-That "PrivacyInfo.xcprivacy liegt im iOS-Ressourcenordner" (Test-Path (Join-Path $mauiDir "Platforms/iOS/Resources/PrivacyInfo.xcprivacy"))
    $infoPlist = [IO.File]::ReadAllText((Join-Path $mauiDir "Platforms/iOS/Info.plist"))
    Assert-That "Info.plist setzt ITSAppUsesNonExemptEncryption auf false" ($infoPlist -match '<key>ITSAppUsesNonExemptEncryption</key>\s*<false\s*/>')

    Write-Host "Zielframeworks per Eigenschaft ausblendbar"
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $csprojPath = Join-Path $mauiDir "Tankradar.MAUI.csproj"
        # Deterministisch unabhaengig von geerbten Include*Target-Umgebungsvariablen (CI-Jobs setzen sie): alle vier
        # Eigenschaften werden in jedem Aufruf explizit per -p: gesetzt (globale Eigenschaften schlagen Umgebungsvariablen).
        $allOn = @("-p:IncludeAndroidTarget=true", "-p:IncludeIosTarget=true", "-p:IncludeMacCatalystTarget=true", "-p:IncludeWindowsTarget=true")
        $all = (& dotnet msbuild $csprojPath -getProperty:TargetFrameworks @allOn 2>&1 | Out-String).Trim()
        Assert-That "Standard: iOS-Zielframework enthalten" ($all -match 'net10\.0-ios') $all
        $iosOnly = (& dotnet msbuild $csprojPath -getProperty:TargetFrameworks -p:IncludeAndroidTarget=false -p:IncludeIosTarget=true -p:IncludeMacCatalystTarget=false -p:IncludeWindowsTarget=false 2>&1 | Out-String).Trim().Trim(';')
        Assert-That "Ausgeblendet: nur net10.0-ios (macOS-Job)" ($iosOnly -eq 'net10.0-ios') $iosOnly
        $noApple = (& dotnet msbuild $csprojPath -getProperty:TargetFrameworks -p:IncludeAndroidTarget=true -p:IncludeIosTarget=false -p:IncludeMacCatalystTarget=false -p:IncludeWindowsTarget=true 2>&1 | Out-String).Trim()
        Assert-That "Ausgeblendet: keine Apple-Ziele (Windows-Jobs)" ($noApple -notmatch 'ios|maccatalyst') $noApple
    }
}
finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    Write-Host "iOS-Deployment-Pruefung FEHLGESCHLAGEN ($($failures.Count))." -ForegroundColor Red
    exit 1
}
Write-Host "iOS-Deployment-Pruefung erfolgreich." -ForegroundColor Green
exit 0
