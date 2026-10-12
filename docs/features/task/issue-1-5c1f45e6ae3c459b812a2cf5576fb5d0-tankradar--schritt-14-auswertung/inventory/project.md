# Dokumentation, Versionierung, Projekteinstellungen

## Anwenderhilfe `docs/help/Tankbuch/`
Dateien: `index.md` (11 Zeilen), `beschreibung.md` (84), `ablauf-anwender.md` (49), `ablauf-technisch.md` (63; Abschnitte Daten, Dienste, Oberfläche, Tests). Eingebunden in `docs/help/index.md` (Zeile 13, Beschreibung nennt Verbrauch pro 100 km, keine Auswertung).
- `beschreibung.md` enthält u. a. die Abschnitte „Tank- und Ladevorgänge“ (Tabelle der Angaben), „Verbrauchsberechnung“, „Fehlermeldungen“, „Belegfoto“, „Löschen“, „Barrierefreiheit“, „Datenschutz“.
- Zu „Menge, Preis und Gesamtbetrag“ steht: Zwei der drei Werte genügen, beim Speichern wird der dritte übernommen, bei drei Werten Abweichung > 0,02 € wird nicht gespeichert. **Die Rundungsausnahme** aus ADR 0009 (als stimmig gilt auch ein Wert, der genau dem gerundeten Quotienten der beiden anderen entspricht; Rundung Gesamtbetrag 2, Preis 3, Menge 2 Nachkommastellen) steht **nicht** in der Hilfe (Suche nach „Rundung“/„gerundet“ ohne Treffer in `docs/help/Tankbuch/`).
- Verbrauchsberechnung dort: Teilbetankungen vor dem ersten vollen Vorgang „zählen nicht“; im Formular selbst gibt es keinen Hinweis dazu.
- Keine Beschreibung einer Auswertung, von Kosten pro 100 km, Monatskosten oder Diagrammen.

## ADR 0009 `docs/adr/0009-tankbook-entries.md`
Status „Angenommen (2026-10-11)“. Wesentliche Festlegungen für Schritt 14:
- Verbrauch wird beim Lesen berechnet (`ConsumptionCalculator`), nicht gespeichert; je Art eigene Kette (Hybrid); Teilbetankungen vor dem ersten Vollvorgang fließen nicht ein; Strecke <= 0 ergibt keinen Wert.
- Zeitpunkt = lokale Wanduhrzeit (`DateTime` ohne Zeitzone, minutengenau); Ordnungsschlüssel (Zeitpunkt, Kilometerstand, Kennung) `TankbookEntryOrder`.
- Zwei-aus-drei-Regel mit Rundung Gesamtbetrag 2 / Preis 3 / Menge 2 Nachkommastellen, Toleranz 0,02 €, Sonderfall exakter gerundeter Quotient.
- Abschnitt „Abweichungen vom Designentwurf“: Monatsanalyse, Verlauf und Statistik folgen in Schritt 14, Export in Schritt 16.
- Abschnitt „Folgen“: Schritt 14 nutzt `ConsumptionCalculator` und `TankbookEntryOrder`; Schritt 15 muss `receipts/` mitsichern.
- Weitere ADRs im Ordner: 0001 bis 0008 (0008 = Fahrzeugverwaltung). Nächste freie Nummer: 0010.

## `changes.log` und Versionierung
- `changes.log` (Repository-Root, UTF-8) beginnt mit dem neuesten Eintrag; Format `## <Datum> — <Titel> (Tankradar Schritt <n>, Version 0.1.<n+4>)`, darunter Stichpunkte, teils „Restpunkte aus Schritt …“, „Tests:“ und „Dokumentation:“-Zeilen. Oberster Eintrag: `## 2026-10-11 — Tank- und Ladevorgänge erfassen (Tankradar Schritt 13, Version 0.1.17)`; davor Schritt 12 = Version 0.1.16, Schritt 11 = Version 0.1.15 (Zählung der Versionsnummer = Schrittnummer + 4 in diesen Einträgen). Weitere Einträge ohne Schritt/Version (z. B. „Test: E2E-Eingaben vor dem Speichern abgesichert“, „Hotfix: Datenbank im iOS-Release-Build (TestFlight)“).
- Die Version 0.1.17 steht nur im `changes.log`-Text und ggf. in Release-Namen, nicht in einer Datei: `src/Tankradar.MAUI/Tankradar.MAUI.csproj` enthält fest `Version`/`InformationalVersion`/`ApplicationDisplayVersion` = `0.1.0` und `ApplicationVersion` = `1`. Die eigentliche Version setzt die Pipeline (`-p:Version=...` in `scripts/package-windows.ps1` und `scripts/create-review-version.ps1`; `scripts/iOS-Deployment.ps1` schreibt `ApplicationDisplayVersion` bei `-Version`). Git-Tags im Repository: nur `v0.1.0-rc.*` (neuester `v0.1.0-rc.7`).
- Regeln der CI-Versionierung: `scripts/versioning.mjs` und `scripts/determine-next-version.mjs`: erste Version 0.1.0; Conventional-Commits (`feat` -> minor, `fix`/`perf` -> patch, Breaking in 0.x nur minor, nie automatisch 1.0.0; `docs`, `chore`, `ci`, `test` lösen keine Version aus). RC-Versionen auf `staging` mit Suffix `-rc.N`.
- Commit-Stil der letzten Schritte: `plan:`, `feat:`, `test:`, `docs:`, `fix:`, `merge:`, `chore:` jeweils mit „Schritt <n> - …“.

## Branch und Arbeitsverzeichnis
Aktueller Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-14-auswertung`; Verzeichnis `docs/features/task/<Branch>/` enthält `requirement.md` und `todo.md` (nicht committet). Der Vorgängerschritt 13 hat sein Feature-Verzeichnis nach der Abnahme entfernt (Commit `799efb3`).

## iOS-Einstellungen `src/Tankradar.MAUI/Tankradar.MAUI.csproj`
- `PropertyGroup` nur für Ziel `ios`: `<MtouchInterpreter>all</MtouchInterpreter>` (Kommentar: iOS-Release läuft ohne JIT und mit Linker, EF Core braucht Laufzeitcode; die Variante `-all` überschritt das 60-Minuten-Limit des CI-Builds).
- `ItemGroup` nur für `ios`: `TrimmerRootAssembly` für `Tankradar.MAUI`, `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Data.Sqlite`.
- `SupportedOSPlatformVersion` iOS 16.0, MacCatalyst 15.0, Android 21.0, Windows 10.0.19041.0. `TargetFrameworks`: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst` (außer Linux), `net10.0-windows10.0.19041.0` (nur Windows), je per `IncludeAndroidTarget`/`IncludeIosTarget`/`IncludeMacCatalystTarget`/`IncludeWindowsTarget` ausblendbar. `UseMaui` true, `Nullable` enable, `GenerateDocumentationFile` true mit `WarningsAsErrors` CS1591 (alle öffentlichen Member brauchen XML-Dokumentation); `ApplicationTitle` „Tankatlas“, `ApplicationId` `de.martinstromberg.tankradar`.
- Hinweis: Eine zusätzliche Bibliothek müsste unter iOS mit Interpreter und Linker funktionieren; im Projekt sind keine Diagramm-Pakete vorhanden. Vorlage für iOS/CI: `drafts/iOS-Deployment.ps1`, `drafts/github/`; produktiv `scripts/iOS-Deployment.ps1`, `scripts/test-ios-deployment.ps1`.

## Test- und Build-Konfiguration
- `coverlet.runsettings`: Coverage nur für `[Tankradar.MAUI]*`, ausgeschlossen `**/*.xaml.cs`, `**/Platforms/**`, `**/MauiProgram.cs`, `**/Views/TankradarContentPage.cs` u. a.; Mindestabdeckung 70 % (`scripts/check-coverage.mjs`, `COVERAGE_THRESHOLD` in der CI; lokal `scripts/local-ci.ps1`).
- `.editorconfig` und `dotnet format --verify-no-changes --severity error` sowie `TreatWarningsAsErrors=true` (statische Analyse) sind Teil von `scripts/local-ci.ps1`.
- CI (`.github/workflows/staging-ci.yml`, `pr-staging-ci.yml`): `dotnet test` für `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration` (je mit Coverage) und `src/Tankradar.Tests.E2E` (blockierend, `TANKRADAR_E2E_WINDOW` = `offscreen` Standard).
