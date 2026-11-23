← [Zurück zur Übersicht](index.md)

# OS-Schnittstellen-Tests

Reguläre Tests sind deterministische Unit-, Service- und ViewModel-Tests ohne echte
Betriebssystem-Schnittstellen. Sie laufen in der **blockierenden Lane**
(`--filter "Category!=OsInterface"`).

OS-Schnittstellen-Tests prüfen genau diese Umgebungskontakte — bei Tankatlas vor allem
FlaUI/UI-Automation (das ganze E2E-Projekt startet `Tankradar.MAUI.exe` und steuert es über
UIA3), echte Prozessstarts (z. B. `dotnet build` in den `ApiKeyBuildTests_*`) und echte
Loopback-Portbindungen (`HttpListener` der Mock-Server, `TcpListener`-Portproben). Sie
bleiben erhalten, werden aber getrennt markiert und laufen in der **best-effort-Lane**
(`--filter "Category=OsInterface"`): sichtbar, aber nicht blockierend (Issue #93).

## Klassifizierungskriterium

Maßgeblich ist, ob die **Assertion** eines Tests am OS-Verhalten des Beteiligten hängt —
nicht, ob irgendwo ein `Process.Start` vorkommt:

- Als OS-Schnittstelle gelten: FlaUI/UI-Automation (`UIA3Automation`, `Application.Launch`),
  echte Prozessstarts (`Process.Start`, `ProcessStartInfo`), Loopback-Listener
  (`HttpListener`, `TcpListener`), lockanfällige Dateisystemszenarien, Clipboard.
- Als Determinismus gelten (bleiben regulär): Dateisystem- und SQLite-Tests gegen das
  Temp-Verzeichnis (`TestDataContext`, `BackupServiceTests_*`, `TempDirectory`-Nutzer) —
  bewusst **nicht** markiert, weil ihre Assertionen nicht an OS-Ressourcen-Konkurrenz
  hängen; sie sind nur bezüglich ihres Aufräumens gehärtet (`TempDirectoryCleanup`).
- Ein Test, der selbst einen Prozess startet (z. B. der `processesToKill`-Test von
  `TempDirectoryCleanupTests`), wird einzeln markiert.

Konkrete Vergabe: `E2ETestBase` trägt den Trait einmal — alle E2E-Nachfahren erben ihn.
`StartFailureCleanupE2ETests` bleibt unmarkiert (kein App-Start). In der Integration tragen
`SearchMockServerTestBase` (deckt alle Mock-Server-Nachfahren inkl. `RouteSearchMockServerTestBase`),
`PriceServiceMockServerTests_Flow`, `TileSourceMockServerTests_Flow`,
`LoopbackHttpListenerTests_Retry` und `ApiKeyBuildTests_{Log,Sources}` die Kategorie; im
Unit-Projekt `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`,
`OpenRouteServiceKeyCheckerTests` und der Prozess-Test von `TempDirectoryCleanupTests`.

## Kategorie

OS-nahe Tests tragen den nativen xUnit-v2-Trait mit der zentralen Konstante
`Tankradar.TestSupport.TestCategories` (`src/TestSupport/TestCategories.cs`, per
`<Compile Include>` in alle drei Testprojekte gelinkt):

```csharp
[Trait("Category", TestCategories.OsInterface)]
```

Auf einer Basisklasse reicht ein einziges Attribut, weil der Trait an die abgeleiteten
Testklassen vererbt wird.

## `Flaky`-Kategorie

`Flaky` (`TestCategories.Flaky`) ist eine reine Markierungs-Kategorie für nachweislich
umgebungsflaky OS-nahe Tests. Sie berechtigt zum einmaligen automatischen Retry in der
best-effort-Lane.

Vergaberegeln:

- `Flaky` wird **nur in Kombination mit `OsInterface`** vergeben — nie in der regulären
  Lane (abgesichert durch `FlakyConventionTests`, der per `<Compile Include>`-Link in allen
  drei Testprojekten läuft und jeweils die eigene Assembly per Reflexion scannt).
- `Flaky` wird nur bei einem **dokumentierten Flake-Ereignis** (konkreter CI-/Lokal-Fehlschlag
  mit Fehlerbild) oder einem Analyzer-Befund aus `Analyze-FlakyTestResults.ps1` vergeben.
- Die Markierung wird entfernt, sobald die Ursache behoben ist.
- Trägt eine Klasse einen eigenen `Category`-Trait, muss `OsInterface` am selben Element
  wiederholt werden: xUnit 2.9.3 wertet den geerbten Basisklassen-Trait nicht zusätzlich
  zum eigenen Trait aus — die Klasse fiele sonst in die blockierende Lane zurück
  (`Category!=OsInterface` würde zutreffen).

Schreibweise (Beispiel `SearchE2ETests_LargeResult`, dokumentiertes Flake-Ereignis
`COMException 0x80131505` bei UIA-Baumsuchen):

```csharp
[Trait("Category", TestCategories.OsInterface)]
[Trait("Category", TestCategories.Flaky)]
public partial class SearchE2ETests_LargeResult : SearchE2ETestBase { ... }
```

## Lokale Befehle

```powershell
# Nur reguläre (deterministische) Tests
dotnet test src/Tankradar.Tests.Unit -c Release --filter "Category!=OsInterface"

# Nur OS-Schnittstellen-Tests
dotnet test src/Tankradar.Tests.Unit -c Release --filter "Category=OsInterface"

# Auflistung der Flaky-markierten Tests
dotnet test src/Tankradar.Tests.E2E -c Release --no-build --list-tests --filter "Category=Flaky"
```

Dieselbe Konvention gilt für `Tankradar.Tests.Integration` und `Tankradar.Tests.E2E`.
Der lokale Prüflauf `scripts/local-ci.ps1` bildet beide Lanes ab: die blockierenden
Schritte laufen mit `Category!=OsInterface`, der Schritt `Tests: OS-Schnittstellen` läuft
mit `Category=OsInterface` als best-effort (`WARNUNG` statt `FEHLER`, E2E-Anteil entfällt
bei `-SkipE2E`). Der Pre-Push-Hook testet die Standard-Projekte mit
`Category!=OsInterface`; `PRE_PUSH_E2E=1` läuft ungefiltert.

## Automatischer Retry (Flaky)

Der automatische Einmal-Retry greift ausschließlich für `Flaky`-markierte Tests und
existiert nur in der best-effort-Lane — die blockierende Lane bleibt retry-frei.

**In der CI** (`os-interface tests`-Job in `pr-staging-ci.yml` und `staging-ci.yml`):
Schlägt ein Testschritt fehl, läuft der Schritt `Retry flaky tests once` (`if: failure()`)
`scripts/Retry-FlakyTests.ps1` je Testprojekt:

1. Primär-TRX parsen, fehlgeschlagene Testnamen einsammeln (Theory-Datenzeilen werden auf
   den Methoden-FQN normalisiert).
2. Flaky-Menge via `dotnet test --no-build --list-tests --filter "Category=Flaky"` ermitteln.
3. Schnittmenge leer → „keine Flaky-Fehlschläge", Exit 0 (kein Retry; bei
   Build-Fehlschlag/fehlendem TRX ebenso).
4. Sonst genau ein gefilterter Re-Run:
   `--filter "(Category=Flaky)&(FullyQualifiedName~...)"` → Retry-TRX
   `<primärname>-flaky-retry.trx`.
5. Auswertung recovered/persistent: recovered wird als `::notice::`, persistent als
   `::warning::` annotiert; persistente Fehlschläge lassen den Schritt mit Exit 1
   fehlschlagen (der Job bleibt durch `continue-on-error` nicht-blockierend).

Beide TRX-Dateien (Primär und Retry) wandern über das Artefakt-Glob
`TestResults/test-results-os-interface*.trx` ins Upload-Artefakt — der Erstfehlschlag
bleibt sichtbar.

**Lokal** manuell nach einem OS-Lauf:

```powershell
dotnet test src/Tankradar.Tests.E2E -c Release --filter "Category=OsInterface" --results-directory TestResults --logger "trx;LogFileName=lauf.trx"
./scripts/Retry-FlakyTests.ps1 -TestProject src/Tankradar.Tests.E2E/Tankradar.Tests.E2E.csproj -PrimaryTrx TestResults/lauf.trx -Configuration Release
```

## Flake-Statistik

`scripts/Analyze-FlakyTestResults.ps1` wertet gesammelte TRX-Verzeichnisse aus:
Lauf-Gruppierung über die Namenskonvention `<name>.trx` (Primär) /
`<name>-flaky-retry.trx` (Retry desselben Laufs). Als Flake-Ereignis zählt
Fehlschlag-im-Primär + grün-im-Retry desselben Laufs oder Fehlschlag→grün in
aufeinanderfolgenden Primär-Läufen (Lauf-Reihenfolge aus dem TRX
`<Times start>`-Attribut). Primär-Outcomes außerhalb Passed/Failed (`NotExecuted` u. a.)
gelten als fehlende Beobachtung: Sie zählen weder als Ausführung noch als grüner Folgelauf
und unterbrechen die Fehlschlag→grün-Sequenz nicht.

```powershell
# Lokale Läufe
./scripts/Analyze-FlakyTestResults.ps1 -Path TestResults

# CI-Artefakte
gh run download <run-id> -n test-results-os-interface-pr -D artifacts/os-pr
./scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/os-pr
```

Klassifikation pro Test (Fenster: letzte `-MaxRuns 20` Primär-Läufe):

- `Quarantäne-Kandidat`: ≥ 2 Flake-Ereignisse ODER Fehlrate ≥ 20 % bei ≥ 5 Ausführungen →
  Empfehlung: Quarantäne-Issue anlegen + Fehlerbild unten dokumentieren.
- `beobachten`: ≥ 1 Flake-Ereignis ODER Fehlrate ≥ 10 %.
- `stabil`: sonst.

Die Schwellwerte sind Startwerte und kalibrierbar (Skript-Parameter
`-QuarantineMinFlakeEvents`, `-QuarantineMinFailureRate`, `-QuarantineMinExecutions`,
`-WatchMinFailureRate`). Der Analyzer legt keine Issues automatisch an — er meldet
Kandidaten mit Empfehlung. In der CI läuft er als `if: always()`-Report-Schritt
`Analyze flaky test results` über dem `TestResults`-Verzeichnis des Laufs (Exit immer 0).

## Quarantäne-Prozess (manuell)

Überschreitet ein Test die Schwellwerte:

1. Dedup: `gh issue list --state open --label flaky-quarantine`.
2. Issue anlegen: `gh issue create --label flaky-quarantine` mit Test-FQN, Fehlerbild und
   Verweis auf Lauf/Artefakt bzw. Analyzer-Befund.
3. Fehlerbild zusätzlich unter „Bekannte Fehlerbilder" in dieser Datei dokumentieren.

**Job-Rerun-Konvention** (komplementär zum Trait-Retry): Schlägt der `os-interface tests`-Job
ohne auswertbares TRX fehl (Runner-Abbruch, Testhost-Crash — der Retry-Schritt findet dann
keine Fehlschläge und endet neutral), ist genau ein `gh run rerun --job <job-id>` nur für
diesen Job erlaubt — niemals `gh run rerun --failed` (das würde blockierende Jobs mit
wiederholen). Fehler in der regulären Lane werden niemals wiederholt — sie gelten immer
als Regression.

## CI-Verhalten

Die Workflows `.github/workflows/pr-staging-ci.yml` (PR CI for Staging) und
`.github/workflows/staging-ci.yml` (Pre-Release) testen in getrennten Jobs:

- Job `build & test`: blockierend; `Test regular (unit)`, `Test regular (integration)` und
  `Test regular (E2E)` laufen mit `Category!=OsInterface`; nur diese Lane liefert die
  Coverage für das 70-%-Gate.
- Job `os-interface tests`: best-effort (`continue-on-error: true`, 60 min) mit eigenem
  Build; `Test OS interface (unit|integration|E2E)` laufen mit `Category=OsInterface`,
  gefolgt von `Retry flaky tests once` (`if: failure()`) und `Analyze flaky test results`
  (`if: always()`). In `staging-ci.yml` ist der Job **nicht** Teil der `needs`-Kette von
  `version`.
- Artefakte: regulär `test-results-pr`/`test-results-staging` (`TestResults/*.trx`);
  OS-Lane `test-results-os-interface-pr`/`test-results-os-interface-staging`
  (`TestResults/test-results-os-interface*.trx` inkl. `*-flaky-retry.trx`), plus
  `e2e-diagnostics-os-{pr|staging}` und `e2e-dumps-os-{pr|staging}`.

Im Release-Workflow (`release.yml`, Anzeigename Release) läuft das Gate als Schrittfolge:
`Release gate - regular tests (unit|integration)` mit `Category!=OsInterface` blockierend,
`Release gate - OS interface tests (unit|integration)` mit `Category=OsInterface` und
`continue-on-error: true` auf Schrittebene; `Upload release-gate test results` lädt
`TestResults/*.trx` als `test-results-release`.

Reguläre Fehler gelten als Regressionsfehler. OS-Schnittstellen-Fehler werden sichtbar
gemacht, blockieren PRs und Pre-Releases aber nicht automatisch, solange die
Runner-Umgebung für Desktop-Automation und Port-Bindungen nicht stabil genug ist.

## Deadline-Angleichung (E2E)

Die Host-Poll-Frist der E2E-Basis wird über
`E2EStartupPolicy.ResolveHostPollTimeout` bestimmt: `max(30 s, 2 × UIA-Timeout)`. Der
UIA-Timeout (`TANKRADAR_E2E_UIA_TIMEOUT_SECONDS`, 1–600 s) beträgt lokal 15 s und in der
CI 60 s — die Host-Frist liegt damit lokal bei 30 s und in der CI bei 120 s und damit
immer über dem Budget einer einzelnen UIA-Anfrage. Die Frist gilt für die Poll-Warteschleifen
des Test-Hosts (`NavigateToTab` mit höchstens drei Versuchen, `WaitForPageTitle`,
Sichtbarkeits- und Ein-/Ausblendewarten in `SkipSetup`, `SettingsE2ETestBase.WaitUntil`).

## Bekannte Fehlerbilder

- UIA-Verbindungsaufbau zum frisch gestarteten Fenster kann auf GitHub-Runnern das
  FlaUI-Standard-Timeout überschreiten (`Win32Exception 0x800705B4`,
  `COMException 0x80131505`); Gegenmaßnahmen siehe
  [Robuster App-Start der E2E-Tests](../ci-cd/workflows.md#robuster-app-start-der-e2e-tests-uia-timeouts).
- UIA-Baumsuchen über große Ergebnismengen können in Timeouts laufen (dokumentiertes
  Flake-Ereignis von `SearchE2ETests_LargeResult` → `Flaky`-markiert).
- Loopback-`HttpListener` können durch Port-Kollisionen/Firewall transient scheitern;
  `LoopbackHttpListener` wählt einen freien Port und versucht bis zu zehn Neubindungen.
- Kindprozess-Builds (`ApiKeyBuildTests_*`) hängen am SDK und an der CI-Last; das
  5-Minuten-Kindbudget darf ein Schritt-Timeout nicht vorziehen.
- Dateisystem-Cleanup kann durch Locks oder Read-only-Attribute scheitern —
  `TempDirectoryCleanup.DeleteBestEffort` räumt mit Prozess-Kill, Attribut-Normalisierung
  und Wiederholungen auf, ohne den Assert zu überdecken (Restfehler nur in `Console.Error`).
- Testhost- oder Build-Artefakt-Rennen sind Infrastrukturfehler und werden separat von
  Assertion-Fehlern bewertet.

## Audit-Heuristik

Wiederholbarer Suchbefehl für die Klassifikation ungetaggter Tests (Heuristik, Ergebnis
fachlich bewerten — Kriterium „Assertion hängt am OS-Verhalten"):

```bash
grep -rnE 'Process\.Start|ProcessStartInfo|FlaUI|UIA3|HttpListener|TcpListener|Clipboard' src/Tankradar.Tests.Unit src/Tankradar.Tests.Integration src/Tankradar.Tests.E2E --include='*.cs'
```

Treffer ohne `OsInterface`-Markierung prüfen: Helper/Fixtures ohne eigene Testmethoden,
Fake-Launcher und reine Dateisystem-/SQLite-Fixture-Arbeit sind Fehl-Positive und bleiben
regulär.
