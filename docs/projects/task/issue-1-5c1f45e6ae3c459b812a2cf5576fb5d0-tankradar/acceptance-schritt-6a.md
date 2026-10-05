# Abnahmeprüfung – Entwicklungsschritt 6a

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

### Geprüfte Punkte (Code, `git diff task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar...HEAD`)

- **E2E blockierend in der PR-CI nach `staging`:** In `pr-staging-ci.yml` ist der Schritt
  `Test E2E with FlaUI (blocking)` ohne `continue-on-error`; der frühere Warn-Schritt ist entfernt. Die
  Statusprüfung `build & test` (laut `einrichtung.md` für `staging` erforderlich) scheitert damit bei einem
  E2E-Fehlschlag. Die Abweichung von der CI-Vorlage ist in `docs/help/ci-cd/workflows.md` und `README.md`
  (ci-cd) dokumentiert.
- **Pre-Release auf `staging` (`staging-ci.yml`):** E2E ist dort ebenfalls blockierend. `version` hängt per
  `needs` an `static-checks` und `build-and-test`; die `if`-Bedingung enthält keine Statusfunktion, sodass
  implizit `success()` gilt. `prerelease` und `ios-prerelease` (signierter Build/TestFlight-Upload) hängen
  an `version` und laufen deshalb nur nach grünen Unit-, Integrations-, Coverage- und E2E-Prüfungen. „Re-run
  failed jobs“ führt `build-and-test` erneut aus, bevor abhängige Jobs starten; ein Umgehungspfad besteht
  nicht. Der Back-Merge-Pfad (`is_backmerge == 'true'`) überspringt alle Prüfungen, erzeugt aber auch kein
  Pre-Release.
- **Release auf `main` (`release.yml`):** Der Release-Job führt selbst nur Unit- und Integrationstests aus,
  keine E2E-Tests. Der automatische Weg ist trotzdem abgesichert: `main` nimmt nur PRs von `staging` an
  (`verify-pr-source.yml` + Branch-Schutz), und jeder `staging`-Stand hat die blockierende PR-CI inkl. E2E
  durchlaufen (Branch muss aktuell sein). Das entspricht der Festlegung in der Vorgehensentscheidung
  (blockierend in der PR-CI nach `staging`) und ist in `workflows.md` so beschrieben („Das Release auf `main`
  entsteht aus einem bereits geprüften `staging`-Stand“). Der Reparaturpfad (`upload-existing`) baut nur einen
  bereits veröffentlichten Tag neu und lädt Assets nach; ein neues Release entsteht dort nicht.
- **Off-Screen-Betrieb in der CI:** Beide E2E-Schritte setzen
  `TANKRADAR_E2E_WINDOW: ${{ vars.TANKRADAR_E2E_WINDOW || 'offscreen' }}`. `E2ETestBase.ResolveWindowMode()`
  übersetzt `foreground` (Groß-/Kleinschreibung und Leerraum egal) in `TANKATLAS_TEST_WINDOW=foreground`,
  alles andere in `offscreen`, und setzt den Wert immer explizit beim App-Start. Der Rückfall über die
  Repository-Variable `TANKRADAR_E2E_WINDOW=foreground` funktioniert damit ohne Codeänderung. Lokal gilt
  dasselbe über `local-ci.ps1 -E2EForeground` (stellt die Variable danach wieder her).
- **Fail-safe außerhalb des Testmodus:** `TestWindowMode.ShouldHideWindow` liefert nur `true`, wenn
  `TANKATLAS_TEST_DATA_PATH` nicht leer ist **und** `TANKATLAS_TEST_WINDOW=offscreen` gesetzt ist. Die
  Lifecycle-Registrierung in `MauiProgram.cs` ist zusätzlich auf `#if WINDOWS` beschränkt. Eine reguläre App
  (ohne Testdatenpfad) kann den Modus nicht übernehmen. Unit-Tests decken das ab
  (`TestWindowModeTests_Selection`, inkl. leerem bzw. nur aus Leerraum bestehendem Testdatenpfad).
- **Bedienung ohne Maus:** Die E2E-Tests enthalten keine `Click()`-, Maus- oder Tastaturaufrufe mehr. Verwendet
  werden nur die Muster Invoke, SelectionItem und Toggle. `TransientRetry` und die Diagnose-Artefakte
  (`if: always()`) bleiben erhalten. Screenshots entstehen per `PrintWindow` direkt vom App-Fenster.
  `DiagnosticsCaptureE2ETests` prüft, dass der Screenshot Inhalt zeigt (Größe und nicht einfarbig).
- **pre-push:** `test-execution-check.py` testet standardmäßig nur Projekte mit `Tests` und ohne `E2E` im
  Namen. Am realen Repository ergibt das `Tankradar.Tests.Integration` und `Tankradar.Tests.Unit`. Mit
  `PRE_PUSH_E2E=1` wird die gesamte Solution getestet. Durch drei neue Python-Tests abgedeckt; die
  Dokumentation in `checks.md` ist angepasst.
- **Nachweis und Entscheidung dokumentiert:** `workflows.md` hält fest: sechs grüne lokale Läufe im
  Off-Screen-Betrieb, CI-Nachweis (5 grüne Läufe) steht aus und folgt im PR, bei Instabilität Rückfall über
  die Repository-Variable. Wie vereinbart ist das keine Abweichung.

### Praktische Verifikation (2026-10-05, Windows 11)

- `scripts/local-ci.ps1` vollständig (inkl. Sicherheitsprüfung, nuget.org erreichbar): **Exit 0**. Alle
  Schritte OK: Node-Tests, Workflow-Validierung, iOS-Deployment-Skript, Restore, Format, Sicherheitsprüfung,
  statische Analyse, Unit 365/365, Integration 58/58, Abdeckung 94,4 % (Schwelle 70 %), **FlaUI-E2E 33/33
  (1 min 51 s)** und iOS-Compile-Prüfung. Das Windows-Paket wurde ohne `-Package` übersprungen.
- **Fensterbeobachtung während des Laufs** (Abtastung alle 250 ms per `EnumWindows` aller sichtbaren
  Fenster der Prozesse `Tankradar.MAUI` plus `GetForegroundWindow`): 420 Abtastungen bei laufender App,
  282 Fenstertreffer. Davon lagen **0 im sichtbaren Bereich**: alle Fenster bei (-32000, -32000), ExStyle
  `0x8000180` = `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`. Die App war **0-mal Vordergrundfenster**. Der Anwender
  wurde also nicht gestört.
- Python-Hook-Tests: 74/74 OK. `npm test`: 32/32 OK. `scripts/validate-workflows.py`: OK (7 Workflows;
  `actionlint` nicht im PATH, nur eingebaute Prüfungen).
- Nach dem Lauf waren keine `Tankradar.MAUI`-Prozesse übrig. Es wurde kein echter Tankerkönig-Endpunkt
  aufgerufen.

### Beobachtungen und Empfehlungen

1. **Restlücken der Auslieferungskette außerhalb der Festlegung von 6a:**
   - Der manuelle Release-Weg per Tag `v*.*.*` (`release.yml`) veröffentlicht jeden getaggten Commit nur
     nach Unit- und Integrationstests, ohne E2E. Ein Administrator könnte also einen Commit taggen, der nie
     die PR-CI durchlaufen hat.
   - Der Promotion-PR `staging` → `main` verlangt nur `verify-source`. Wird er gemergt, nachdem ein späterer
     Pre-Release-Lauf auf `staging` rot war (z. B. E2E im zweiten Lauf flaky), entsteht trotzdem ein Release.
     Der Inhalt hatte E2E in der PR-CI bestanden.

   Beides ist kein Verstoß gegen die festgelegte Umsetzung (E2E blockierend in der PR-CI nach `staging`).
   Für ein lückenloses Gate wäre denkbar: E2E zusätzlich im Release-Gate von `release.yml`, oder ein
   Tag-Schutz bzw. eine Prüfung, dass der getaggte Commit auf `main`/`staging` liegt und einen grünen
   `Pre-Release`-Lauf hat.
2. **Uneinheitliche Variablennamen:** Testseitig gilt `TANKRADAR_E2E_WINDOW` (wie `TANKRADAR_E2E_DIAGNOSTICS_DIR`),
   App-seitig `TANKATLAS_TEST_WINDOW` (wie `TANKATLAS_TEST_DATA_PATH`). Die Testbasis überschreibt
   `TANKATLAS_TEST_WINDOW` immer. Wer lokal versehentlich `TANKATLAS_TEST_WINDOW=foreground` setzt, sieht daher
   kommentarlos keine Wirkung. Das Risiko ist gering: Die Dokumentation nennt für den Rückfall konsistent
   `TANKRADAR_E2E_WINDOW` bzw. `-E2EForeground`. Ein Satz in `workflows.md`, dass `TANKATLAS_TEST_WINDOW` nur
   intern von der Testbasis gesetzt wird, würde Fehlbedienung vorbeugen.
3. `PRE_PUSH_E2E` wirkt nur beim exakten Wert `1`. `true` oder `yes` werden still ignoriert. So ist es
   dokumentiert, die Hook-Ausgabe weist aber nur auf `=1` hin.
4. Der CI-Stabilitätsnachweis (5 grüne Läufe auf gehosteten Runnern) steht noch aus und ist vom Projektleiter
   im PR nachzuholen. Danach sollte das Ergebnis in `workflows.md` ergänzt werden (Abschnitt „Entscheidung
   und Nachweis“).
