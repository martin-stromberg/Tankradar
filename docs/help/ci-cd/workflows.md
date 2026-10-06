← [Zurück zur Übersicht](index.md)

# Workflows und gemeinsame Bausteine

Alle Dateien liegen unter [`.github/`](../../../.github/). Grundlage ist die Vorlage
„CI Workflows for a Staging-Based .NET Release Pipeline“, angepasst an die MAUI-App.

## Die sieben Workflows

| Datei | Anzeigename | Trigger | Aufgabe |
|---|---|---|---|
| `verify-pr-source.yml` | Verify PR Source | PR → `main` | Lehnt PRs nach `main` ab, die nicht von `staging` kommen. |
| `pr-staging-ci.yml` | PR CI for Staging | PR → `staging` | Qualitätsprüfungen (`static checks`, `build & test`, `ios build`). Reine Rückführungs-PRs (`main` → `staging`) überspringen die Prüfungen (Erkennung siehe unten). |
| `staging-ci.yml` | Pre-Release | Push → `staging` | Dieselben Prüfungen, dann `version`, `prerelease` (Windows-ZIP als GitHub-Pre-Release) und `ios-prerelease`. |
| `staging-to-main-promotion.yml` | Staging to Main Promotion | nach erfolgreichem `Pre-Release` | Eröffnet den Entwurfs-PR `staging` → `main` (Label `automated-promotion`). |
| `sync-staging-with-main.yml` | Backmerge Main to Staging | Push → `main` | Eröffnet den PR `main` → `staging` (Label `automated-backmerge`). |
| `release.yml` | Release | Push → `main` oder Tag `v*.*.*` | Ermittelt die Version, prüft/repariert vorhandene Releases, veröffentlicht `release-win-x64.zip` + `update.json`, baut iOS. |
| `security-scan.yml` | Security Scan | wöchentlich (Mo 04:00 UTC), manuell | Schwachstellenprüfung unabhängig von Codeänderungen. |

Wichtig: `staging-to-main-promotion.yml` referenziert den **Anzeigenamen** `Pre-Release`. Wer den Namen
in `staging-ci.yml` ändert, muss den Eintrag im selben Commit anpassen (`validate-workflows.py` prüft das).

## Gemeinsame Bausteine

| Baustein | Zweck |
|---|---|
| `.github/actions/security-scan` | `dotnet list package --vulnerable --include-transitive --format json`, ausgewertet sprachunabhängig über `scripts/check-vulnerabilities.mjs` (derselbe Code wie im lokalen Prüflauf; Bericht `vulnerable-packages.json`); lässt den Schritt bei Funden **fehlschlagen**. Von `static checks` und vom wöchentlichen Scan genutzt. |
| `.github/actions/build-and-package` | Veröffentlicht die Windows-App und erzeugt ZIP und `update.json` (ruft `scripts/package-windows.ps1`). Das Paket ist öffentlich herunterladbar und wird deshalb **ohne Tankerkönig-Schlüssel** gebaut: kein Schlüssel-Input, die Schlüsselquellen werden explizit geleert. `scripts/validate-workflows.py` prüft das. |
| `.github/actions/package-ios` | Baut iOS (iOS 16). Pinnt SDK-Band (`10.0.1xx`) und iOS-Workload auf eine zu Release-Xcode passende Version, setzt `MauiXamlInflator=XamlC`, die Buildnummer (`ApplicationVersion` = Commit-Anzahl + `run_attempt` − 1) und die Anzeigeversion (Pre-Release-Suffix gekürzt). Mit `IOS_SIGNING_ENABLED=true` und vollständigen Secrets: Import von Zertifikat und Profil in eine temporäre Keychain, signierte `release-ios.ipa` (nur im Runner; sie wird **nicht** als Workflow-Artefakt hochgeladen, weil sie den Tankerkönig-Schlüssel enthält und das Repository öffentlich ist), optional TestFlight-Upload per iTMSTransporter, Aufräumen der Keychain. Sonst unsignierter Simulator-Build als Compile-Prüfung (kein Fehlschlag). Blendet die übrigen Zielframeworks per Eigenschaft aus (`IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false`), weil der Restore alle Zielframeworks der `.csproj` auswertet und bei fehlender Workload mit `NETSDK1147` scheitern würde; die Windows-Jobs blenden umgekehrt die Apple-Ziele aus (`IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`). Der lokale Prüflauf (`scripts/local-ci.ps1`) simuliert die Windows-Jobs mit diesen `false`-Werten, stellt die Variablen danach wieder her und prüft iOS in einem eigenen Schritt „iOS-Compile-Prüfung“ (Apple-Ziel aktiv, Android aus; ohne iOS-Workload übersprungen). Einziger iOS-Pfad der Pipeline; der frühere Baustein `build-ios` entfällt. |

## Skripte

| Skript | Zweck |
|---|---|
| `scripts/versioning.mjs` | Reine Versionslogik (0.x-Regeln, RC-Nummer). |
| `scripts/determine-next-version.mjs` | Liest die Git-Historie und liefert `changed`, `version`, `rc_tag`, `rc_version`. |
| `scripts/resolve-release-version.mjs` | Entscheidet im Release-Workflow: neues Release, vorhandenes Release reparieren oder nichts tun. Pre-Releases werden nie repariert. |
| `scripts/check-coverage.mjs` | Prüft die Zeilenabdeckung gegen die Schwelle. |
| `scripts/package-windows.ps1` | Windows-Publish, ZIP und `update.json` (auch lokal nutzbar). |
| `scripts/validate-workflows.py` | Syntaktische und strukturelle Prüfung der Workflows; verbietet außerdem .ipa-Veröffentlichungen, auch über Verzeichnis-/Muster-Uploads (`actions/upload-artifact`) und `gh release create/upload` mit Verzeichnissen, Mustern oder Variablen als Asset (Tests: `scripts/test_validate_workflows.py`). |
| `scripts/local-ci.ps1` | [Lokaler Prüflauf](lokaler-pruefung.md). |
| `scripts/iOS-Deployment.ps1` | [iOS-Build, Simulator, Gerät und TestFlight-Upload](ios-deployment.md) (lokal, mit Mac). |
| `scripts/test-ios-deployment.ps1` | Prüfung des iOS-Skripts ohne Mac (Syntax, Hilfe, Abbruchverhalten, Hilfsfunktionen). |

Tests der Node-Skripte: `npm test` (`scripts/*.test.mjs`, keine Abhängigkeiten nötig).

## Runner

Windows-spezifische Schritte (Format, Analyse, Build, Tests, Paket) laufen auf `windows-latest`, weil die
MAUI-Windows-App und die FlaUI-Tests Windows benötigen. iOS läuft auf `macos-latest`. Leichtgewichtige
Schritte (PR-Quellenprüfung, Versionsermittlung, Promotion) laufen auf `ubuntu-latest`.

## Abweichungen von der Vorlage

- **Versionsermittlung:** `semantic-release` entfällt. Es würde die erste Version als 1.0.0 vergeben und
  bei Breaking Changes automatisch auf 1.0 anheben, was der Projektvorgabe (0.1.0, keine automatische 1.0)
  widerspricht. Stattdessen ermittelt `determine-next-version.mjs` die Version nach denselben
  Conventional-Commits-Regeln. Folglich entfallen `release.config.js` und die npm-Abhängigkeiten.
- **Release-Erstellung:** Auch der automatische Weg nutzt die `gh` CLI (Vorlage, Abschnitt 11.3).
- **Linux-Variante und `release-metadata.json`** entfallen: Die App ist Windows/iOS-only und nutzt
  `msTools.Updater` nicht.
- **Plattformen:** `windows-latest`/`macos-latest` statt `ubuntu-latest` für Build und Tests.
- **Testprojekte:** Unit, Integration und E2E laufen getrennt; nur Unit und Integration fließen in die
  Coverage ein. E2E ist abweichend von der Vorlage („best-effort test category“) **blockierend**, siehe
  [E2E-Tests als Auslieferungs-Gate](#e2e-tests-als-auslieferungs-gate).
- **Restore:** `dotnet restore` läuft mit `-p:Configuration=Release`, weil die MacCatalyst-Laufzeiten
  konfigurationsabhängig sind; `dotnet list package` läuft deshalb mit `--no-restore`.

## E2E-Tests als Auslieferungs-Gate

**Abweichung von der CI-Vorlage:** Die Vorlage führt UI-Tests als *best-effort* (Fehlschlag nur Warnung).
Tankatlas liefert nur mit vollständig grünen Tests aus. Deshalb ist der Schritt `Test E2E with FlaUI (blocking)`
in `pr-staging-ci.yml` (PR nach `staging`) **und** in `staging-ci.yml` (vor Version, Pre-Release und iOS-Upload;
`version` hängt per `needs` an `build-and-test`) blockierend: kein `continue-on-error`, ein Fehlschlag lässt den
Job und damit PR-Merge bzw. Pre-Release scheitern. Der Schritt steht hinter der Coverage-Prüfung, damit deren
Ergebnis auch bei einem E2E-Fehlschlag vorliegt; Diagnose-Artefakte (`if: always()`) und die begrenzte
Wiederholung bei UI-Automation-Timeouts (`TransientRetry`) bleiben erhalten. Das Release auf `main` entsteht aus
einem bereits geprüften `staging`-Stand.

`TransientRetry` wiederholt (höchstens drei Versuche) nur UIA-Timeouts: `TimeoutException`, `COMException` 0x80131505
und `Win32Exception` mit ERROR_TIMEOUT (1460 bzw. 0x800705B4, so aus `UIA3Automation.FromHandle` beim Abfragen des
Hauptfensters auf dem Runner beobachtet). Alle anderen Fehler werden sofort und unverändert gemeldet. Das Aufräumen
nach einem fehlgeschlagenen App-Start (`Dispose`/`Cleanup` der Testbasen) ist null-sicher, damit die ursprüngliche
Startausnahme im Testergebnis sichtbar bleibt und nicht von einer `NullReferenceException` verdeckt wird.

**Lokal stören die Tests den Anwender nicht:**

- `pre-push` führt die E2E-Tests standardmäßig **nicht** mehr aus (Unit- und Integrationstests weiterhin);
  `PRE_PUSH_E2E=1 git push ...` schaltet sie ein (siehe [`checks.md`](../git-hooks/checks.md#test-execution-checkpy)).
- `scripts/local-ci.ps1` führt sie weiterhin aus (blockierend).
- **Off-Screen-Betrieb:** Im Testmodus (`TANKATLAS_TEST_DATA_PATH`) startet die App ihr Fenster bei
  `TANKATLAS_TEST_WINDOW=offscreen` außerhalb des Bildschirms (Position -32000/-32000), als nicht aktivierbares
  Werkzeugfenster ohne Taskleisteneintrag und ohne sich in den Vordergrund zu holen
  (`TestWindowMode`, `Platforms/Windows/OffscreenWindow.cs`). Außerhalb des Testmodus hat die Variable nie eine
  Wirkung. Die Testbasis setzt sie standardmäßig; die Tests bedienen die App ausschließlich über
  UI-Automation-Muster (Invoke, Toggle, SelectionItem), nie per Mausklick. Diagnose-Screenshots nimmt
  `E2EDiagnostics` direkt vom App-Fenster auf (`PrintWindow`), sodass sie auch off-screen Inhalt zeigen.
- **Rückfall auf den Vordergrundbetrieb ohne Codeänderung:** lokal `TANKRADAR_E2E_WINDOW=foreground` bzw.
  `local-ci.ps1 -E2EForeground`; in der CI die Repository-Variable `TANKRADAR_E2E_WINDOW=foreground`.

**Entscheidung und Nachweis (Schritt 6a):** Der Off-Screen-Betrieb wurde übernommen, weil er lokal nachweislich genauso stabil ist wie der Vordergrundbetrieb:
sechs vollständig grüne Läufe hintereinander (je 33 von 33 E2E-Tests, 1 min 43 s bis 1 min 58 s) im Off-Screen-Betrieb
auf dem Entwicklungsrechner (Windows 11), während der Anwender parallel arbeitete; zusätzlich ein grüner Lauf über
`scripts/local-ci.ps1`. Eine Prüfung bestätigte, dass das Fenster bei (-32000, -32000) liegt und der Vordergrund
unverändert bleibt.

**Stand in der CI:** Die CI-Oberflächentests laufen per Repository-Variable `TANKRADAR_E2E_WINDOW=foreground`
im Vordergrund. Der Off-Screen-Betrieb war auf den GitHub-Runnern nicht zuverlässig: Im Staging-Lauf trat ein
UI-Automation-Timeout (`0x800705B4`) auf, obwohl zuvor fünf von fünf PR-Läufe grün waren. Lokal bleibt Off-Screen der
Standard (`local-ci.ps1 -E2EForeground` schaltet lokal auf den Vordergrund um). Die übrigen Punkte (blockierende E2E
in der PR-CI und in `staging-ci.yml`, `pre-push` ohne E2E) bleiben bestehen.

## Robuster App-Start der E2E-Tests (UIA-Timeouts)

Auf den GitHub-Windows-Runnern scheiterten einzelne FlaUI-Tests im Konstruktor mit
`Win32Exception 0x800705B4` (`UIA3Automation.FromHandle`, Timeout), obwohl lokal alles grün war (PR #5, Run
37424919256: 5 von 44 Tests; im Fenstermodus `foreground`). Dasselbe Fehlerbild gab es zuvor im Off-Screen-Betrieb:
**Der Fenstermodus war nicht die Ursache.** Ursache ist der langsame UIA-Verbindungsaufbau zum frisch gestarteten
WinUI-Fenster, der die FlaUI-Standard-Timeouts und die bisherigen drei kurzen Wiederholungen überschritt.

Die Testbasis (`E2ETestBase`, `E2EStartupPolicy`, `TransientRetry` in `src/TestSupport`) behandelt das jetzt so:

- **Großzügigere UIA-Timeouts:** `ConnectionTimeout` und `TransactionTimeout` der `UIA3Automation` sind lokal 15 s,
  in der CI (`GITHUB_ACTIONS=true` bzw. `CI=true`) 60 s. Überschreibbar mit `TANKRADAR_E2E_UIA_TIMEOUT_SECONDS`
  (1 bis 600; ungültige Werte werden ignoriert).
- **Warten auf Bereitschaft:** Vor der ersten UIA-Abfrage wartet der Test (höchstens bis zum UIA-Timeout), bis der Prozess
  ein Hauptfenster-Handle hat und auf Eingaben wartet (`Process.WaitForInputIdle`).
- **Backoff statt kurzer Pausen:** Die Abfrage des Hauptfensters wird bei UIA-Timeouts bis zu viermal wiederholt, mit
  Pausen 2 s, 4 s, 8 s (höchstens 15 s je Pause) und einem Gesamtzeitlimit (mindestens 60 s, sonst das Doppelte des UIA-Timeouts).
- **Ein begrenzter Neustart:** Scheitert der Start danach weiterhin an einem UIA-Timeout, wird der eigene (halb gestartete)
  App-Prozess beendet, eine neue Automation-Instanz erzeugt und die App genau einmal neu gestartet (höchstens zwei Starts).
- **Keine verdeckten Fehler:** Nur UIA-Timeouts (`TimeoutException`, `0x80131505`, `0x800705B4`) lösen Wiederholungen oder
  einen Neustart aus; ein fehlendes Fenster, Assertions und andere Fehler bleiben sofort sichtbar. Die Meldung nennt Zahl der
  Versuche bzw. Starts und die Wartezeit; die Diagnosedaten (siehe unten) werden wie bisher erfasst.

Abgesichert ist die Logik durch Unit-Tests (`E2EStartupPolicyTests_Backoff`, `TransientRetryTests_Behavior`) ohne echte App.

## E2E-Diagnosedaten

Zur Nachvollziehbarkeit eines Fehlschlags erfasst die
Testbasis (`E2ETestBase.RunWithDiagnostics`, `E2EDiagnostics`) bei einem fehlgeschlagenen Test pro Test:

- `<Testklasse>.<Test>.png` – Screenshot, direkt vom App-Fenster aufgenommen (auch im Off-Screen-Betrieb; bei Startfehlern des gesamten Bildschirms),
- `<Testklasse>.<Test>.uitree.txt` – Dump des UI-Automation-Baums (Typ, Name, AutomationId, Klasse, Position),
- `<Testklasse>.<Test>.error.txt` – Fehlermeldung samt Stacktrace und Zustand des App-Prozesses.

Die Dateien liegen im Verzeichnis `e2e-diagnostics/` im Repository-Root (überschreibbar über die
Umgebungsvariable `TANKRADAR_E2E_DIAGNOSTICS_DIR`); es ist nicht versioniert. Beide CI-Workflows
(`pr-staging-ci.yml`, `staging-ci.yml`) laden es im Schritt `Upload E2E diagnostics` mit `if: always()` als
Artefakt `e2e-diagnostics-pr` bzw. `e2e-diagnostics-staging` hoch, also auch bei Fehlschlägen.
Neue E2E-Tests kapseln ihren Testkörper dafür in `RunWithDiagnostics(...)`.

## Erkennung von Rückführungen (Back-Merge)

`pr-staging-ci.yml` und `staging-ci.yml` entscheiden im Job `detect-backmerge` mit
`scripts/detect-backmerge.mjs <head-sha> <main-sha>`, ob die Prüfungen übersprungen werden. Ein Back-Merge liegt vor,
wenn der **Head-Commit** (PR: `github.event.pull_request.head.sha`, Push: `github.sha`) ein Merge-Commit ist, dessen
zweiter oder weiterer Parent die Spitze von `main` ist, oder wenn sein Inhalt identisch zu `main` ist. Der erste Parent
zählt bewusst nicht: Bei PR-Events ist `HEAD` der von GitHub erzeugte Merge-Commit, dessen erster Parent die Spitze von
`staging` ist (== `main`, solange noch nichts auf `staging` liegt oder nach einer Beförderung). Normale Feature-PRs
durchlaufen daher immer die vollen Prüfungen. Die Logik ist über `npm test` (`scripts/detect-backmerge.test.mjs`) abgesichert.
