# Build-/Test-/Release-Pipeline

11 Workflows unter `.github/workflows/` (Übersichtstabelle auch in
`docs/help/ci-cd/workflows.md`). Für diese Aufgabe relevant:

## `pr-staging-ci.yml` — „PR CI for Staging" (PR → `staging`)

Trigger: `pull_request` auf `staging` (opened/synchronize/reopened). Der Update-PR
muss gegen `staging` gehen (`verify-pr-source.yml` erlaubt nur `staging` → `main`).

Jobs (alle übersprungen bei reinem Back-merge `main` → `staging`,
Erkennung `scripts/detect-backmerge.mjs`):

| Job | Runner | Wesentliche Schritte |
|---|---|---|
| `static checks` | windows-latest | setup-dotnet `10.0.x`, Node 24, `dotnet workload restore Tankradar.sln`, `dotnet restore -p:Configuration=Release`, `npm test` (Skript-Tests), `scripts/test-ios-deployment.ps1`, `dotnet format --verify-no-changes --severity error`, **Security scan** (s. u.), `dotnet build … -p:TreatWarningsAsErrors=true` |
| `build & test` | windows-latest | Restore + Build (Release), `dotnet test` Unit + Integration mit Coverage (`coverlet.runsettings`), Coverage-Gate **70 %** (`scripts/check-coverage.mjs`), **FlaUI-E2E** (blockierend, Off-Screen via `TANKRADAR_E2E_WINDOW`, Crash-Dumps unter `e2e-dumps/`) |
| `ios build` | macos-latest | unsignierter iOS-Compile via `.github/actions/package-ios` (prüft `net10.0-ios` mit gepinnter Workload) |

Env der Windows-Jobs: `IncludeAndroidTarget/IosTarget/MacCatalystTarget=false`.

## `staging-ci.yml` — „Pre-Release" (Push → `staging`)

Gleiche `static checks` + `build & test` wie oben, danach:

- `version`: `scripts/determine-next-version.mjs --rc` ermittelt aus
  Conventional-Commits, ob sich die Version ändert (`changed`), samt `rc_tag`/`rc_version`.
- `prerelease` (nur bei `changed == 'true'`): `.github/actions/build-and-package`
  → `scripts/package-windows.ps1` → `artifacts/release-win-x64.zip` + `update.json`,
  dann `gh release create <rc_tag> --prerelease`.
- `ios-prerelease`: iOS-Build auf macOS (signiert nur mit `IOS_SIGNING_ENABLED`
  + Secrets, sonst unsignierte Compile-Prüfung, kein Fehlschlag).

**Wichtig für dieses Issue:** ein Paket-Bump ohne `feat:`/`fix:`-Commit erzeugt
ggf. **keinen** neuen RC (`changed=false`) — Commit-Typ bei der Umsetzung beachten
(`fix:` löst Patch-Version aus; Regeln in `scripts/versioning.mjs`).

## `release.yml` — „Release" (Push → `main` oder Tag `v*.*.*`)

- `scripts/resolve-release-version.mjs` entscheidet: neues Release, Reparatur
  (`upload-existing`) oder Skip.
- Release-Gate: `dotnet test` Unit + Integration (Release-Konfiguration).
- `.github/actions/build-and-package` → `dotnet publish … --framework
  net10.0-windows10.0.19041.0 -p:SelfContained=true -p:RuntimeIdentifier=win-x64
  -p:WindowsPackageType=None` (`scripts/package-windows.ps1`) →
  `release-win-x64.zip` + `update.json`; API-Schlüssel wird explizit geleert.
- `gh release create` (automatischer Weg) bzw. `gh release upload --clobber`
  (Tag-Weg), danach `ios`-Job (signiert/unsigniert).

## Promotion `staging` → `main`

- `staging-to-main-promotion.yml`: nach erfolgreichem `Pre-Release`-Lauf
  (Anzeigename im `workflow_run`-Trigger!) wird automatisch ein Draft-PR
  `staging` → `main` erstellt; Merge (manuell) triggert `release.yml`.
- `sync-staging-with-main.yml`: nach Push auf `main` PR `main` → `staging`
  (Back-merge; mit „Create a merge commit" mergen).

## Security-Scan

- Composite-Action `.github/actions/security-scan`: `node
  scripts/check-vulnerabilities.mjs --run <sln> vulnerable-packages.json` →
  intern `dotnet list <sln> package --vulnerable --include-transitive
  --no-restore --format json`; Exit ≠ 0 bei Funden **oder** Aufruffehlern
  (Exit 2 zählt als Fehlschlag). Bericht als Artefakt.
- Einsatz: `static checks` in PR-CI und Staging-CI (PR-blockierend) sowie
  `security-scan.yml` (wöchentlich Mo 04:00 UTC + `workflow_dispatch`, nicht
  PR-getriggert).
- Akzeptanzkriterium des Issues ist exakt dieser Lauf (lokal:
  `node scripts/check-vulnerabilities.mjs --run Tankradar.sln` — ohne `--no-restore`-Problem,
  da das Skript restoren voraussetzt).

## Review-Workflows (nur Issue-Erzeugung, keine Analyse)

- `review-nuget-packages.yml`: wöchentlich (Di 05:00 UTC) + manuell — erzeugt
  Issue „NuGet-Paketprüfung" für `@softwarewerkstatt` (Dedup über offenes Issue
  mit Label `review-nuget-packages`). Dieses Issue #63 stammt aus dem monatlichen
  `review-architecture-security.yml`-Lauf (#62).
- `review-compliance.yml`: jährlich; `review-architecture-security.yml`: monatlich.

## Lokaler Prüflauf

`scripts/local-ci.ps1` spiegelt die CI: npm-Tests, Workflow-Validierung
(`validate-workflows.py`), Format, Security-Scan (gleiches Skript), Build mit
Warnungen als Fehler, Unit/Integration + Coverage 70 %, E2E (blockierend;
`-SkipE2E` möglich, `-E2EForeground` statt Off-Screen), optional `-Package`
(ruft `package-windows.ps1`), iOS-Compile-Prüfung falls iOS-Workload lokal
vorhanden (hier: ja).

## Git-Hooks

`.githooks/pre-push` (installierbar via `install-hooks.sh/.cmd`): Format-Check,
XML-Doku-Check, `test-execution-check.py` → `dotnet build` + `dotnet test` für
Unit+Integration (E2E nur mit `PRE_PUSH_E2E=1`).
