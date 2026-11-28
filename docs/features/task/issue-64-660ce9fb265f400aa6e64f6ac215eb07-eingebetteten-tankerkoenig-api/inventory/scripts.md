# Skripte und Composite Actions — Bestand

## Aufrufgraph (wer ruft wen)

```
release.yml (Job release)
 ├─ node scripts/resolve-release-version.mjs          [GH_TOKEN]
 │   └─ determine() aus scripts/determine-next-version.mjs
 │       ├─ git tag --merged HEAD --list "v*"          (letzter stabiler Tag)
 │       ├─ git log <tag>..HEAD                        (Commits seit Tag)
 │       └─ computeNextVersion() aus scripts/versioning.mjs
 │   └─ gh api repos/…/releases[/tags/<tag>]           (Release-Existenz/-Assets)
 ├─ ./.github/actions/build-and-package
 │   └─ pwsh scripts/package-windows.ps1 → dotnet publish win-x64
 │       → artifacts/release-win-x64.zip + artifacts/update.json
 ├─ gh release create / gh release upload              (Tag implizit + Assets)
 └─ Job ios → ./.github/actions/package-ios            (signiert → TestFlight)

staging-ci.yml
 ├─ Job version → node scripts/determine-next-version.mjs --rc
 │   └─ git tag --list "v<ver>-rc.*"                   (RC-Nummernzählung)
 ├─ Job prerelease → build-and-package + gh release create --prerelease
 └─ Job ios-prerelease → package-ios

pr-staging-ci.yml
 └─ Job ios-build → package-ios (nur xcode-version; unsigned)

scripts/local-ci.ps1 (lokaler Prüflauf)
 ├─ npm test, validate-workflows.py, test_validate_workflows.py,
 │  test-ios-deployment.ps1, dotnet-Prüfungen
 └─ -Package → scripts/package-windows.ps1
```

## `scripts/resolve-release-version.mjs` (135 Zeilen)

Entscheidet im Release-Workflow: neues Release, Reparatur oder nichts. Exporte:

| Funktion/Konstante | Zweck | GitHub-/Git-Annahme |
|---|---|---|
| `EXPECTED_ASSETS` (Z. 12) | `["release-win-x64.zip", "update.json"]` — erwartete Asset-Namen | Festverdrahtet auf die Windows-Artefakte |
| `parseManualTag(tagName)` | Validiert `vX.Y.Z`(-Suffix), liefert Version ohne `v` | — |
| `classifyWorkflowRef({refType, refName})` | `tag` → `manual` (+Version/Tag), `branch`+`main` → `automatic`, sonst Fehler | `AUTOMATIC_RELEASE_BRANCHES = ["main"]` (Z. 11) |
| `releaseHasExpectedAsset(release)` | Prüft, ob ein GitHub-Release-Objekt alle Assets (`state: "uploaded"`, `size > 0`) enthält | Liest die GitHub-Release-Struktur |
| `incompleteReleases(releases)` | Filtert unvollständige Releases; **CRITICAL GUARD (Z. 42-44): Pre-Releases werden nie repariert** | Liste von `ghListReleases` |
| `resolveReleaseVersion({ref, determineVersion, getRelease, listReleases})` | Kernlogik mit injizierten Abhängigkeiten; Ausgaben `released`, `reason` (`new-release`/`repair-missing-assets`/`already-released`/`no-releasable-commits`), `version`, `tag`, `release_kind`, `release_action` (`create`/`upload-existing`/leer) | „already-released" wird über **GitHub-Release-Existenz** (`getRelease(tag)`) entschieden, nicht über den Git-Tag |
| `ghGetRelease(repository, tag)` | `gh api repos/<repo>/releases/tags/<tag>`; `null` bei 404 | GitHub Releases API |
| `ghListReleases(repository)` | `gh api --paginate --slurp repos/<repo>/releases?per_page=100` | GitHub Releases API |
| `gh(args)` (intern) | `execFileSync("gh", …)` — braucht `gh`-CLI + `GH_TOKEN` | — |

Main-Block (Z. 122-135): liest `GITHUB_REPOSITORY`, `GITHUB_REF_TYPE`, `GITHUB_REF_NAME`, schreibt alle Outputs nach `GITHUB_OUTPUT`/stdout.

## `scripts/determine-next-version.mjs` (59 Zeilen)

| Funktion | Zweck | Annahme |
|---|---|---|
| `findLatestStableTag()` | `git tag --merged HEAD --list "v*" --sort=-v:refname`, gefiltert auf `STABLE_TAG_RE` | **Benötigt vorhandene `vX.Y.Z`-Git-Tags**; ohne Tag → `null` → „first-release"-Pfad |
| `readCommitsSince(tag)` | `git log <tag>..HEAD --no-merges` | Tag muss im Verlauf erreichbar sein (Backmerge per Merge-Commit) |
| `determine({rc})` | Outputs `changed`, `version`, `reason`; bei `--rc` zusätzlich `rc_number`, `rc_tag` (`v<ver>-rc.<n>`), `rc_version` | RC-Zählung über `git tag --list "v<ver>-rc.*"` — **setzt gepushte RC-Tags voraus** |

Tests: `scripts/versioning.test.mjs` (11 Tests) — `determine-next-version.mjs` selbst hat keine eigene Testdatei (I/O-Schale).

## `scripts/versioning.mjs` (126 Zeilen)

Reine Logik ohne I/O: `STABLE_TAG_RE`, `FIRST_VERSION = "0.1.0"`, `parseCommit`, `requiredBump`, `parseVersion`, `applyBump` (0.x-Regel: nie automatisch 1.0), `computeNextVersion`, `nextRcNumber`, `displayVersion`. Laut Anforderung unverändert zu lassen.

## `scripts/package-windows.ps1` (101 Zeilen)

- Parameter: `-Version` (Pflicht), `-Tag` (Pflicht, bestimmt Download-URL), `-Repository` (Standard `GITHUB_REPOSITORY`), `-OutputDirectory` (Standard `artifacts`).
- `dotnet publish` `net11.0-windows10.0.19041.0`, `WindowsPackageType=None`, `SelfContained`, `win-x64` (Z. 55-66).
- **Schlüsselhärtung (Z. 50-57):** löscht `Env:TANKRADAR_FUEL_PRICE_API_KEY` und `Env:FUEL_PRICE_API_KEY` und setzt `-p:TankerkoenigLocalPropsFile` auf eine nicht existente Temp-Datei → das Paket enthält garantiert keinen Schlüssel.
- Erzeugt `release-win-x64.zip` (Compress-Archive) und `update.json` (Z. 83-98): `version`, `releaseNotes`, `publishedAt`, `assets[]` mit `assetUrl = https://github.com/<repo>/releases/download/<Tag>/release-win-x64.zip` (Z. 78), `sha256`, `sizeBytes`.
- **Consumer von `update.json`:** keiner — kein Treffer für `update.json`/`releases/download`/Auto-Updater in `src/`; das Manifest wird nur erzeugt und hochgeladen.
- Rufer: `build-and-package`-Action (CI) und `local-ci.ps1 -Package` (lokal).

## `.github/actions/build-and-package/action.yml` (46 Zeilen)

Composite Action: Inputs `release-version`, `release-tag`; Outputs `zip-path`, `manifest-path`. Schritte: Setup .NET `11.0.x`, `dotnet workload restore`, Aufruf `package-windows.ps1` (shell `pwsh`) mit explizit geleertem `TANKRADAR_FUEL_PRICE_API_KEY`/`FUEL_PRICE_API_KEY` (Z. 38-40). Kommentar Z. 34-35: Asset-Namen sind fest, weil `resolve-release-version.mjs` genau diese erwartet.

## `.github/actions/package-ios/action.yml` (364 Zeilen)

Einziger iOS-Auslieferungspfad. Inputs u. a. `release-version`, `signing-enabled`, `bundle-id`, Signierungsdaten (`certificate-p12-base64` usw.), App-Store-Connect-API (`api-key-id`, `api-issuer-id`, `api-key-p8`), `ios-workload-version` (Default `27.2.11039`), `xcode-version`, **`fuel-price-api-key`** (Z. 62-65). Output: `signed`.

Schritte: Xcode-Auswahl → `Determine signing mode` (`signed=false` ohne Fehlschlag bei `IOS_SIGNING_ENABLED != 'true'` oder fehlenden Secrets; fehlende API-Keys → nur `::notice::`, Z. 84-117) → SDK-Pin (`global.json` im Workspace, `11.0.100`) → Workload-Pin per Rollback-File → Keychain-Import (nur signiert) → **Build iOS** (`dotnet publish -f net11.0-ios`, `TANKRADAR_FUEL_PRICE_API_KEY` als Env, Z. 203-234; signiert: `ios-arm64` + `ArchiveOnBuild` + Codesign, unsigniert: `iossimulator-arm64`) → `Collect iOS package` (`release-ios.ipa`, nur Runner-lokal) → `Validate iOS package` (Privacy-Manifest, `UIDeviceFamily` iPhone, `ITSAppUsesNonExemptEncryption`, `MinimumOSVersion ≥ 16`, Marketing-Icon 1024 px) → **Upload to TestFlight** per iTMSTransporter (`-m upload -apiKey -apiIssuer`, nur bei `signed=='true'` + allen drei API-Key-Inputs, Z. 301-350) → `Remove signing material` (`if: always()`).

Annahmen: Build-Nummer = `git rev-list --count HEAD` + `run_attempt` − 1 (braucht `fetch-depth: 0`); iTMSTransporter-Suche in Transporter.app/`/usr/local/itms` mit Apple-CDN-Fallback. Upload ist **optional** — fehlen die API-Key-Secrets, endet der Job mit einer signierten, nicht hochgeladenen `.ipa` (nur Notice).

## `scripts/validate-workflows.py` (256 Zeilen) + `scripts/test_validate_workflows.py` (110 Zeilen)

Relevante Regeln:

| Element | Zeilen | Inhalt |
|---|---|---|
| `SAFE_UPLOAD_DIRS` | 41 | `coverage-report`, `TestResults`, `e2e-diagnostics`, `e2e-dumps` |
| `SAFE_FILE_EXTENSIONS` | 43 | `.json`, `.trx`, `.xml`, `.html`, `.txt`, `.md`, `.log`, `.zip`, `.sha256` |
| `GH_VALUE_OPTIONS` | 45-48 | Optionen von `gh release create/upload` mit eigenem Wert |
| `classify_upload_path(entry)` | 73-93 | Verwirft `.ipa`, Variablen, Verzeichnisse, Muster, unbekannte Endungen — für `upload-artifact`-Pfade **und** `gh release`-Assets |
| `gh_release_assets(cmd)` | 96-123 | Extrahiert Asset-Argumente aus `gh release create/upload`-Zeilen |
| `check_release_commands(...)` | 126-139 | Fehler bei `.ipa` als Release-Asset oder nicht klassifizierbarem Asset |
| `check_key_policy(...)` | 153-166 | `build-and-package`-Schritte dürfen keine Schlüssel-Inputs erhalten (`fuel|price|api[-_]?key`) |
| `check_action` (build-and-package-Sonderregeln) | 205-212 | Die Action darf keine `fuel|price`-Inputs haben; `FUEL_PRICE`-Env-Werte müssen leer sein |

Tests: `ReleaseCommandTests.test_current_release_commands_are_allowed` trägt die **heutigen Aufrufe wörtlich** als erlaubt ein (`gh release create "$RELEASE_TAG" artifacts/release-win-x64.zip artifacts/update.json …`, Z. 54-65) — wird bei Entfernung der Schritte obsolet/anzupassen. `WindowsPackageKeyPolicyTests` prüft die Schlüsselfreiheit des Windows-Pakets. `artifacts/update.json` steht in `test_known_safe_uploads_are_allowed` (Z. 28) als erlaubter Upload-Pfad. Hinweis zur Anforderung: Die genannte „check-coverage.mjs-nahe Artefaktliste" existiert nicht in `check-coverage.mjs`; die `update.json`-Erwähnung liegt in `test_validate_workflows.py:28`.

## `scripts/local-ci.ps1` (288 Zeilen)

Lokaler Spiegel der Pipeline. Release-relevant: Schritt 9 `-Package` (Z. 232-239) ruft `package-windows.ps1 -Version <v> -Tag v<v> -OutputDirectory artifacts` → erzeugt lokal `release-win-x64.zip` + `update.json` unter `artifacts/` (gitignoriert, `.gitignore:88`). Schritt 1 ruft `npm test`, `validate-workflows.py` + Tests, `test-ios-deployment.ps1`.

## `scripts/iOS-Deployment.ps1` (1505 Zeilen) + `test-ios-deployment.ps1` (280 Zeilen)

Lokales iOS-Deployment (Mac/Pair-to-Mac/SSH): Aktionen `build`, `simulator`, `device`, `store` (signierter Build + IPA-Validierung + TestFlight-Upload via iTMSTransporter, Buildnummer-Bump), `upload` (vorhandene `.ipa` hochladen), `list`, `menu`. Enthält `Invoke-IpaValidation` mit identischen Fehlertexten wie der CI-Schritt „Validate iOS package" und dieselbe iTMSTransporter-Kandidatenliste. **Kein GitHub-Release-Bezug.** `test-ios-deployment.ps1` prüft ohne Mac: Syntax, Hilfe, Abbruchverhalten, Hilfsfunktionen, Gleichlauf der CI-Fehlertexte und dass die Pipeline den früheren Baustein `build-ios` nicht mehr referenziert.

## `scripts/create-review-version.ps1` (181 Zeilen)

Erzeugt lokale Windows-Zwischenstände unter `review-versions/` (gitignoriert) für manuelle Reviews — eigenständiger Pfad ohne Release-/Tag-Bezug, kein Aufruf aus Workflows.

## Weitere Skripte (kein Release-Bezug)

`detect-backmerge.mjs`, `check-coverage.mjs`, `check-vulnerabilities.mjs` (je mit `*.test.mjs`), `Retry-FlakyTests.ps1`, `Analyze-FlakyTestResults.ps1`, `scripts/lib/{Trx,TestDiscovery}.ps1`.
