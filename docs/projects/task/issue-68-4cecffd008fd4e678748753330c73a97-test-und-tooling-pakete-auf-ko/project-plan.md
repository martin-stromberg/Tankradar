# Projektplan

## Übersicht

**Projekttitel:** Test-, Tooling- und Diagnose-Paket der Review-Folgeissues #68–#71

**Gesamtziel:** Vier Folge-Items des Architektur-/Sicherheitsreviews #62 umsetzen:
kompatible Patchstände der Testpakete (#68), Paket-/Actions-Governance (#69),
Opt-in-Diagnosepaket (#70) und die EF-Core-/iOS-AOT-Evaluation mit ADR (#71) —
gebündelt in einem PR gegen `staging`.

**Betroffene Bereiche:** Testprojekte, zentrale Paketverwaltung, CI-Workflows/
Dependabot, MAUI-App (Settings/Diagnose), `docs/adr`, `docs/help`.

## Grobe Vorgehensentscheidungen

- **Ein Projekt, vier Schritte — ein Schritt pro Issue.** Die Issues sind fachlich
  unabhängig, aber alle berühren Build-/Paketdateien; eine lineare Kette
  (jeder Schritt baut auf dem vorherigen auf) vermeidet Merge-Konflikte in
  `Directory.Packages.props` und den `.csproj`-Dateien.
- **Governance nach Patchupdates (Schritt 2 nach 1):** Die zentrale
  Paketverwaltung übernimmt direkt die aktualisierten Patchstände, statt erst
  alte Versionen zu zentralisieren.
- **Diagnosepaket (#70) als eigener Schritt:** Größte fachliche Neuerung
  (Opt-in-Export + Redaktion); bewusst datensparsam, an Backup-/Export-Patterns
  der App orientiert, Redaktionsregeln testgetrieben.
- **Evaluation #71 als dokumentationsgetriebener Schritt:** Ergebnis ist ein ADR
  mit begründeter Entscheidung. Eine Umstellung (z. B. Compiled Model) erfolgt
  nur, wenn sie in diesem Rahmen niedrigriskant verifizierbar ist; sonst wird die
  Bestandskonfiguration begründet beibehalten. iOS-Messungen sind in dieser
  Umgebung nicht möglich → dokumentierte Risikoabwägung + Windows-basierte
  Indikator-Messungen, soweit sinnvoll.
- **SHA-Pinning der externen Actions (aus #69):** Entscheidung wird dokumentiert.
  Vorschlag: Dependabot-Ökosystem `github-actions` deckt Update-Automation ab;
  SHA-Pinning wird als bewusst zurückgestellte Option mit Begründung im ADR/
  Kommentar dokumentiert, da Tag-Pinning + Dependabot das Risiko bereits
  deutlich reduziert und die Lesbarkeit der Workflows erhält.

## Entwicklungsschritte

### Schritt 1: Patchstände der Test- und Tooling-Pakete aktualisieren (Issue #68)

Kompatible Patch-/Minor-Updates ohne Breaking Changes in den drei Testprojekten
anwenden — mindestens `Microsoft.NET.Test.Sdk` 17.14.1 → 17.14.2. Vorab per
`dotnet list package --outdated` den tatsächlichen Update-Stand ermitteln;
Major-Updates (Test.Sdk 18.x, runner.visualstudio 4.x, coverlet.collector 10.x)
bewusst nicht anwenden und die Zurückstellung kurz dokumentieren.

Akzeptanzkriterien:
- Alle drei Testprojekte verwenden konsistente, aktualisierte Patchstände.
- `dotnet test` für Unit- und Integrationstests erfolgreich; E2E-Tests laufen
  (lokal, soweit die Umgebung es zulässt, verbindlich in der CI des PRs).
- Zurückgestellte Major-Updates sind in der Projekt-/PR-Dokumentation begründet.

Abhängigkeiten: keine.
Betroffene Bereiche: `src/Tankradar.Tests.*/*.csproj`, Projektartefakte.

### Schritt 2: Paket- und GitHub-Actions-Governance vereinheitlichen (Issue #69)

Zentrale Paketverwaltung per `Directory.Packages.props` (Central Package
Management, `ManagePackageVersionsCentrally`) einführen und alle
`PackageReference`-Einträge versionslos umstellen. `global.json` mit
`rollForward: latestPatch` auf das 10.0.x-Band einführen und CI-`dotnet-version`
darauf abstimmen (oder konsistent dokumentieren). `.github/dependabot.yml` für
`nuget` und `github-actions` einrichten. Die Entscheidung zum SHA-Pinning
externer Actions dokumentieren (Vorschlag: bewusst zurückgestellt, Begründung im
ADR oder Governance-Dokument).

Akzeptanzkriterien:
- Paketversionen zentral verwaltet; Build (`dotnet restore`/`build`) erfolgreich.
- Dependabot-Konfiguration für NuGet- und Actions-Updates liegt vor.
- `global.json` definiert das SDK-Band für lokale Builds und CI.
- Umgang mit veränderbaren Action-Tags ist entschieden und dokumentiert.

Abhängigkeiten: 1.
Betroffene Bereiche: `Directory.Packages.props`, alle `.csproj`, `global.json`,
`.github/dependabot.yml`, ggf. `.github/workflows/`, Doku/ADR.

### Schritt 3: Opt-in-Diagnosepaket für produktive Fehleranalyse (Issue #70)

Ein ausschließlich auf ausdrückliche Benutzeraktion erzeugtes, lokales
Diagnosepaket einführen: gebündelte, stark begrenzte Laufzeit-/Fehlerinfos
(App-/OS-Version, letzte Fehler-/Logeinträge aus einem kapazitätsbegrenzten
In-Memory-Ringpuffer, kein dauerhaftes Logfile) als Datei, die der Anwender
selbst teilen kann. Vor dem Teilen werden Umfang und Zweck sichtbar. Strikte
Redaktion: keine API-Schlüssel, Koordinaten, Stationsnamen, Suchbegriffe,
Dateipfade, Belegfotos. Keine Telemetrie, keine unaufgeforderte Übertragung.
Redaktionsregeln durch Unit-Tests abgesichert. Benutzerdoku unter `docs/help/`.

Akzeptanzkriterien:
- Diagnosedaten entstehen und werden geteilt nur nach ausdrücklicher
  Benutzeraktion (Settings-Einstiegspunkt mit sichtbarer Inhaltsvorschau).
- Redaktionsregeln (Schlüssel, Koordinaten, Namen, Suchbegriffe, Pfade, Fotos)
  durch Tests belegt.
- Keine unaufgeforderte Telemetrie/Übertragung; Retention/Umfang begrenzt.
- `dotnet test` (Unit + Integration) erfolgreich; kein UI-Bruch in E2E/CI.

Abhängigkeiten: 2.
Betroffene Bereiche: `src/Tankradar.MAUI` (Services/Diagnose, Settings,
Resources/Texts), `src/Tankradar.Tests.Unit`, `docs/help/`.

### Schritt 4: EF-Core-Compiled-Model und iOS-AOT-/Trimming evaluieren (Issue #71)

Evaluation als ADR: Stand der iOS-Konfiguration (`MtouchInterpreter=all`,
EF-/SQLite-Trimmer-Roots) gegen aktuelle EF-Core- und .NET-MAUI-Empfehlungen
(AOT, Trimming, Reflection, Compiled Models) prüfen. Prototypisch ein Compiled
Model erzeugen und Machbarkeit/Risiko bewerten; wo in dieser Umgebung möglich,
Indikator-Messungen (z. B. Windows-Paketgröße, DB-Init-Pfad) durchführen.
Entscheidung samt Begründung als ADR `0020` dokumentieren — auch bei
Beibehaltung der Ist-Konfiguration. Nur wenn die Umstellung nachweislich
niedrigriskant ist, wird sie umgesetzt; Migrationen/Restore/DB-Integrität
bleiben durch Tests abgesichert.

Akzeptanzkriterien:
- ADR mit begründeter Entscheidung (Messwerte oder dokumentierte
  Risikoabwägung) liegt vor.
- Bei Umstellung: Migrationen, Restore und Datenbankintegrität durch Tests
  abgesichert; bei Beibehaltung: Interpreter-/Trimmer-Konfiguration begründet
  dokumentiert.
- Tests laufen fehlerfrei.

Abhängigkeiten: 3.
Betroffene Bereiche: `src/Tankradar.MAUI` (csproj, ggf. Compiled Model),
`docs/adr/`, Tests.

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|---------------------|
| 1 | Patchstände der Test- und Tooling-Pakete (#68) | Keine | Test-csproj |
| 2 | Paket- und Actions-Governance (#69) | 1 | CPM, global.json, dependabot, Workflows |
| 3 | Opt-in-Diagnosepaket (#70) | 2 | MAUI-App, Unit-Tests, docs/help |
| 4 | EF-Compiled-Model-/iOS-AOT-Evaluation + ADR (#71) | 3 | csproj, docs/adr |

## Offene Punkte

Keine. Folgende begründete Annahmen werden behandelt (asynchroner SCM-Kontext —
keine interaktive Rückfrage möglich; Entscheidungen sind in den Issues
kommentiert bzw. im PR dokumentiert):

- *SHA-Pinning externer Actions:* zurückgestellt zugunsten Dependabot +
  Tag-Pinning; dokumentierte Entscheidung erfüllt das Akzeptanzkriterium
  „Risiko reduziert oder bewusst dokumentiert".
- *Messumfang für #71:* iOS-Geräte-/macOS-Messungen sind in dieser Umgebung
  nicht durchführbar; die Akzeptanz erlaubt eine „dokumentierte Risikoabwägung"
  statt Messwerten. Windows-basierte Indikatoren werden ergänzt, wo sinnvoll.
- *Auslieferung des Diagnosepakets:* lokale Datei (ZIP) über den vom Anwender
  ausgelösten Teilen-/Speicherdialog — kein Upload-Endpunkt, erfüllt
  „keine unaufgeforderte Übertragung".
- *E2E-Ausführung für #68:* verbindlich läuft die E2E-Suite in der PR-CI;
  lokal wird sie ausgeführt, sofern die Windows-Desktop-Session es zulässt.
