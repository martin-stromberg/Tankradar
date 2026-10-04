# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Der lokale Prüflauf prüft iOS nicht mehr, die Dokumentation behauptet das aber weiterhin.** Die Anforderung verlangt einen gleichwertigen lokalen Prüflauf, „der dieselben Prüfungen auf dem Entwicklungsrechner ausführt; seine Nutzung wird dokumentiert“. Die Pipeline enthält eine iOS-Compile-Prüfung (Job `ios build`).

  Bisher deckte `scripts/local-ci.ps1` diese Prüfung lokal ab: Der Schritt „Statische Analyse“ baute alle Zielframeworks des Betriebssystems, also auch `net10.0-ios`, sofern die Workload installiert ist. In 3.3 wurde das unter Windows nachgewiesen.

  Mit der Nachbesserung Runde 3 hat sich das geändert:
  - `local-ci.ps1` setzt vor „Restore“ in Z. 117 prozessweit `IncludeAndroidTarget`, `IncludeIosTarget` und `IncludeMacCatalystTarget` auf `false`.
  - Damit baut die statische Analyse nur noch `net10.0-windows10.0.19041.0`. Nachgeprüft: `dotnet msbuild -getProperty:TargetFrameworks` liefert mit diesen Variablen `;net10.0-windows10.0.19041.0`.
  - Der lokale Lauf enthält damit keinerlei iOS-Compile-Prüfung mehr.

  Die Benutzerdokumentation beschreibt noch das alte Verhalten:
  - `docs/help/ci-cd/lokaler-pruefung.md`, Tabelle: „Statische Analyse (Build aller Zielplattformen des lokalen Betriebssystems …)“
  - `docs/help/ci-cd/lokaler-pruefung.md`: „Der iOS-Teil wird lokal nur als Compile-Prüfung des iOS-Zielframeworks im Schritt ‚Statische Analyse‘ abgedeckt“
  - `docs/help/ci-cd/workflows.md`: „Lokal gelten die Standardwerte (alle Ziele des Betriebssystems)“

  Ebenso veraltet:
  - `docs/help/ci-cd/README.md` Z. 16 nennt für die statische Analyse noch „Windows- und Android-Ziel“, obwohl die Windows-Jobs Android jetzt ausblenden.
  - Der Kommentar im Schritt `Static analysis` in `pr-staging-ci.yml` (Z. 110, „Windows/Android“) ist ebenfalls veraltet.

  Behebung, eine von zwei Möglichkeiten:
  - Den iOS-Compile-Check lokal erhalten, z. B. als eigenen Schritt `dotnet build -f net10.0-ios` mit `IncludeIosTarget=true`, wenn die iOS-Workload vorhanden ist.
  - Oder die Dokumentation an das neue Verhalten anpassen und die fehlende lokale iOS-Prüfung ausdrücklich als Unterschied zur Pipeline nennen.

## Hinweise

**Abweichung aus 3.3 behoben (Pflichtjob `static checks`):**

- `scripts/test-ios-deployment.ps1` setzt in allen drei `getProperty`-Aufrufen alle vier `Include*Target`-Eigenschaften explizit per `-p:`. Geerbte Umgebungsvariablen wirken sich daher nicht mehr aus.
- Mit der vollständigen Job-Umgebung (`IncludeAndroidTarget`, `IncludeIosTarget` und `IncludeMacCatalystTarget` jeweils `false`) gibt `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` „iOS-Deployment-Pruefung erfolgreich.“ aus, Exit-Code 0. Ohne diese Umgebungsvariablen ist das Ergebnis ebenfalls grün.
- `local-ci.ps1` prüft das Skript jetzt zusätzlich in einem Kindprozess mit der Windows-Job-Umgebung. Das sichert die Behebung gegen ein Wiederauftreten ab.

**Windows-Jobs mit ihrer Job-Umgebung nachgestellt** (SDK 10.0.401; alle drei `Include*Target=false` als Umgebungsvariablen). Betroffen sind:
- `static checks` und `build & test` in PR und staging
- `prerelease`
- `release`
- `security-scan`

Ergebnisse:
- `TargetFrameworks` = `;net10.0-windows10.0.19041.0`. Das führende Semikolon ist unschädlich, wie in 3.3 belegt.
- `dotnet restore Tankradar.sln -p:Configuration=Release`: OK.
- `dotnet format … --verify-no-changes --no-restore --severity error`: OK.
- `dotnet list … package --vulnerable --include-transitive --no-restore`: keine anfälligen Pakete in allen 4 Projekten. Das Grep-Muster der Action schlägt nicht an.
- `dotnet build … -c Release --no-restore -p:TreatWarningsAsErrors=true`: 0 Warnungen, 0 Fehler.
- Unit- und Integrationstests (`--no-build`, `coverlet.runsettings`, XPlat Code Coverage): grün.
- ReportGenerator + `check-coverage.mjs`: Zeilenabdeckung 73,2 %, Schwelle 70 %, OK.
- `scripts/package-windows.ps1 -Version 0.1.0-rc.1 -Tag v0.1.0-rc.1` (Ausgabe in den Scratchpad):
  - erzeugt `release-win-x64.zip` (ca. 92 MB, `Tankradar.MAUI.exe` self-contained)
  - erzeugt eine korrekte `update.json` (Version, URL, SHA-256, Größe)
  - ProductVersion der EXE `0.1.0-rc.1+<sha>`
- Nicht ausgeführt: `dotnet workload restore`, um keine Workloads zu verändern; in 3.3 löste der Befehl einen Installationsversuch aus. Ebenfalls nicht ausgeführt sind die E2E-Tests, alle macOS- bzw. GitHub-gebundenen Teile (iOS-Build, Signierung, Upload) sowie `gh release`.

**Weitere Läufe:**
- `scripts/local-ci.ps1 -SkipE2E`: erfolgreich, Exit-Code 0, alle Schritte OK, inklusive des neuen Schritts „iOS-Deployment-Skript mit Windows-Job-Umgebung“.
- `npm test`: 24/24.
- `python scripts/validate-workflows.py`: OK für 7 Workflows und alle lokalen Actions. `actionlint` ist lokal nicht vorhanden.

**Ausblenden des Android-Ziels in allen Windows-Jobs:** Das entspricht der Vorlage und der Anforderung („Android-Bestandteile der Vorlage entfallen“). Damit ist die Beobachtung aus 3.3 erledigt. Zu `package-ios`: Die Abweichung von der Vorlage beim Entfernen fremder iOS-Packs (nur das Versionsverzeichnis wird gelöscht) ist jetzt begründet kommentiert. Auf macOS ist das weiterhin unbelegt.

**Nebenwirkung von `local-ci.ps1`:** Das Skript setzt die `Include*Target`-Variablen in Z. 117 auf Prozessebene und stellt sie am Ende nicht wieder her. Ein Aufruf als `.\scripts\local-ci.ps1` in einer interaktiven PowerShell-Sitzung (laut Doku die übliche Form) läuft im selben Prozess. Danach bauen weitere `dotnet`-Befehle in dieser Sitzung nur noch das Windows-Ziel. Das ist für das Prüfergebnis unschädlich, für Entwickler aber überraschend.

**Übrige Anforderungspunkte unverändert erfüllt, wie in 3.2/3.3 bewertet:**
- Branch-Modell mit `verify-pr-source`
- Promotion- und Backmerge-Workflows
- Versionierung: Start bei 0.1.0, keine automatische Anhebung auf 1.0, RC-Suffix
- Windows-ZIP ohne Installer
- Secrets und Variablen ausschließlich über GitHub
- iOS unsigniert ohne Fehlschlag, gesteuert über `IOS_SIGNING_ENABLED` und `IOS_*`
- `scripts/iOS-Deployment.ps1` mit allen Aktionen und `TANKRADAR_IOS_*`
- `package-ios` nach Vorlage
- Einrichtungs-Checkliste
- Bundle-ID `de.martinstromberg.tankradar` an allen Stellen
- `drafts/` nicht versioniert
