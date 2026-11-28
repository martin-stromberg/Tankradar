# Tankerkönig-API-Schlüssel — Bestand

Für die Risikoentscheidung von Issue #64 (gemeinsamer Schlüssel bleibt im iOS-Binary) ist der heutige Schlüsselweg maßgeblich. Laut Anforderung bleiben `TankerkoenigApiKey.targets` und `ApiKeys.cs` unverändert — dokumentiert wird der Ist-Zustand.

## Build-Zeit-Einbettung: `src/Tankradar.MAUI/MSBuild/TankerkoenigApiKey.targets` (89 Zeilen)

Importiert **unbedingt** (für alle Zielframeworks) in `Tankradar.MAUI.csproj:112` — im Gegensatz zu `WindowsAppIcon.targets` (Z. 115, Windows-konditioniert). Jeder Build jeder Plattform führt das Target aus.

| Element | Beschreibung |
|---|---|
| `TankerkoenigLocalPropsFile` (Z. 14) | Pfad zu `tankerkoenig.local.props` im Repo-Root; überschreibbar per MSBuild-Eigenschaft (`package-windows.ps1` setzt eine nicht existente Temp-Datei) |
| `UsingTask WriteTankerkoenigKeyAttribute` (Z. 17-76) | `RoslynCodeTaskFactory`-Inline-Task: liest den Schlüssel selbst aus der Umgebung/Datei — **bewusst keine MSBuild-Eigenschaft/Item-Metadaten/Task-Parameter**, damit der Wert nie in `-v:detailed`/`-v:diag`/Binlogs landet |
| Quellenreihenfolge (Z. 32-49) | 1. Env `TANKRADAR_FUEL_PRICE_API_KEY` → 2. Env `FUEL_PRICE_API_KEY` (Name des GitHub-Secrets) → 3. `<TankerkoenigApiKey>` aus `tankerkoenig.local.props` (per `XDocument` gelesen, **nicht** per `<Import>`) |
| Ausgabe (Z. 65-69) | Schreibt `obj/…/TankerkoenigApiKey.g.cs` mit `[assembly: AssemblyMetadata("TankerkoenigApiKey", "<key>")]`; Nicht-Alnum-Zeichen als `\uXXXX` escaped |
| Logging (Z. 73) | Nur „Tankerkönig-Schlüssel beim Build vorhanden: ja/nein" |
| `Target EmbedTankerkoenigApiKey` (Z. 78-87) | `BeforeTargets="CoreCompile"`; fügt die generierte Datei als `<Compile>` ein |

## Laufzeitauflösung: `src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs` (182 Zeilen)

| Typ | Rolle |
|---|---|
| `IApiKeyStore` (Z. 10-30) | Sichere Ablage: `GetAsync`/`SetAsync`/`RemoveAsync` (iOS: Keychain, Windows: Credential Locker) |
| `IApiKeyProvider` (Z. 35-42) | `GetApiKeyAsync()` |
| `ApiKeyProvider` (Z. 48-132) | Auflösung: **Testmodus** (`TANKATLAS_TEST_DATA_PATH` gesetzt) → nur Env `TANKRADAR_PRICE_API_KEY` (`PriceApiOptions.ApiKeyEnvironmentVariable`), nie Build-/Ablage-Schlüssel (Z. 94-99). **Normalbetrieb:** gespeicherter Schlüssel wird gelesen; ist ein **Build-Schlüssel** vorhanden, ist er maßgeblich und wird bei Abweichung in die sichere Ablage geschrieben (`_store.SetAsync`, Z. 117-128); sonst gilt der gespeicherte |
| `ReadBuildTimeKey()` (Z. 83-89) | Liest `AssemblyMetadataAttribute` mit Key `BuildKeyMetadataName = "TankerkoenigApiKey"` (Z. 53) aus der eigenen Assembly |
| `InMemoryApiKeyStore` (Z. 137-160) | Speicher-Ablage für den Testmodus |
| `ApiKeyStoreSelector` (Z. 166-181) | Wählt im Testmodus die In-Memory-Ablage, sonst die Plattform-Ablage |

**Rotationsmechanismus (bereits vorhanden):** Neuer Build-Schlüssel ≠ gespeicherter Schlüssel → Ablage wird beim nächsten App-Start automatisch überschrieben. Ein Schlüsseltausch erfordert also nur: neuen Wert im Secret `FUEL_PRICE_API_KEY` + neuer signierter iOS-Build. (Dokumentiert auch in `docs/help/Preisdaten/api-schluessel.md:34` und der Sicherheitshinweis Z. 56.)

## Schlüsselweg je Artefakt/Kanal

| Artefakt/Weg | Schlüssel enthalten? | Absicherung |
|---|---|---|
| Signierte iOS-`.ipa` (TestFlight/App Store) | **Ja** — `package-ios` erhält `fuel-price-api-key` (`release.yml:174`, `staging-ci.yml:446`) → Env `TANKRADAR_FUEL_PRICE_API_KEY` beim `dotnet publish` (`package-ios/action.yml:211`) | `.ipa` wird **nie** als Workflow-Artefakt oder Release-Asset hochgeladen; `validate-workflows.py` verbietet jede `.ipa`-Ablage (IPA_RE, Upload-/Release-Prüfung) |
| Windows-Paket `release-win-x64.zip` (GitHub-Release) | **Nein** — Action leert beide Env-Vars (`build-and-package/action.yml:38-40`), Skript löscht sie und blendet die props-Datei aus (`package-windows.ps1:50-57`) | `validate-workflows.py` + `test_validate_workflows.py` (`WindowsPackageKeyPolicyTests`): keine Schlüssel-Inputs, Env muss leer sein |
| PR-iOS-Build (`pr-staging-ci.yml:370`) | Nein — kein `fuel-price-api-key`-Input übergeben | — |
| Lokale Builds / `review-versions/` (`create-review-version.ps1`) | Ja, wenn Env/props gesetzt — bleibt auf dem Entwicklerrechner | Hinweis in `api-schluessel.md:56` |
| Quellcode, Logs, Tests | Nein | Pre-Commit-Hook blockiert Zertifikatsdateien; GitHub maskiert Secrets; Tests nutzen `TANKRADAR_PRICE_API_KEY`-Mock-Schlüssel |

## Zugehörige Tests

- `src/Tankradar.Tests.Integration/Integration/ApiKeyBuildTests_Log.cs` — baut mit `-v:diag` + Binlog und Platzhalterschlüssel: der Wert darf nicht im Log stehen, muss aber in der Assembly sein.
- `src/Tankradar.Tests.Integration/Integration/ApiKeyBuildTests_Sources.cs` — Reihenfolge der drei Schlüsselquellen.
- `src/Tankradar.Tests.Unit/Unit/ApiKeyProviderTests_Resolution.cs` — Auflösungslogik des Providers.
