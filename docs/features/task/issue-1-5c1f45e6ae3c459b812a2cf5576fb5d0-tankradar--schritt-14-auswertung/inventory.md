# Bestandsaufnahme: Tankradar Schritt 14 – Verbrauchs- und Kostenauswertung

Analysiert wurde der Tankbuch-Bereich in `src/` (Modelle, `ConsumptionCalculator`, Dienste, ViewModels, Seite `TankbookEntriesPage`, Texte, Tests) sowie Dokumentation, Versionierung, iOS-Einstellungen und der Designentwurf im Ordner `design-draft/`, bezogen auf `requirement.md` dieses Verzeichnisses. Es wurde nichts geändert und nichts committet.

## Zusammenfassung

- **Vorhanden (Berechnung):** `ConsumptionCalculator.Calculate` liefert je Vorgang Strecke und Verbrauch pro 100 km, je Art (`Fuel`/`Electricity`) in eigener Kette, Teilbetankungen vor dem nächsten Vollvorgang aufsummiert, Teilbetankungen vor dem ersten Vollvorgang ignoriert, Strecke <= 0 ohne Wert; die Ordnung liefert `TankbookEntryOrder` (Zeitpunkt, Kilometerstand, Kennung). Aufruf nur aus `TankbookEntryService.GetEntriesAsync` (Ergebnis neueste zuerst als `TankbookEntryOverview`).
- **Fehlt:** Kosten pro 100 km, Monatskosten, Durchschnittsverbrauch über mehrere Vorgänge, Verlaufspunkte, jede Auswertungsklasse (`ITankbookStatisticsService`, `TankbookStatistics`, `MonthlyCostSummary`, `TrendPoint` existieren nicht), Auswertungstexte, AutomationIds und UI-Bereich für die Auswertung. `TankbookEntry.TotalAmount` und `OccurredAt` stehen bereit (lokale Wanduhrzeit ohne Zeitzone), werden aber nicht ausgewertet.
- **Startverbrauch:** `Vehicle.StartConsumption` (`decimal?`, > 0 und < 100, 2 Nachkommastellen, Einheit L/100 km bzw. kWh/100 km bei `VehicleFuelKind.Electric`) existiert bereits in Modell, Entität (`VehicleEntity`) und Formular, wird aber nur angezeigt (`TankbookTexts.FormatVehicleDetails`), nicht verrechnet. Keine Schemaänderung nötig.
- **Seite/ViewModels:** `TankbookEntriesPage` (Shell-Route `tankbookentries`, Parameter `vehicleId`) ist ein einziger `ScrollView` aus Kopf, Statusmeldung, „Vorgang erfassen“, Formular, Liste. `TankbookEntriesViewModel.LoadAsync` lädt nach Öffnen, Speichern (`Form.Saved`) und Löschen neu und setzt `Entries`; daran würde eine Auswertung hängen. Es gibt keine Auswertungsansicht.
- **Grafik:** Weder `Microsoft.Maui.Graphics`/`GraphicsView`/`IDrawable` noch ein Diagrammpaket sind im Projekt vorhanden; Pakete: `Microsoft.Maui.Controls`, EF Core/SQLite, Logging. Vorhanden sind nur `Color`-Paletten (`MapPalette`, `DesignSystem.xaml`).
- **Texte und AutomationIds:** Alle Texte stehen in `Resources/Texts/*.cs` (deutsch, `const`/statische Formatierer); AutomationIds folgen `Bereich.Element[.Unterelement]` (`TankbookEntries.*`, `TankbookEntries.Form.*`, `TankbookEntry.Item.*`, `Tankbook.*`). Der Teilbetankungs-Hinweis im Formular besteht nur aus Beschriftung `PartialLabel`; die Hilfe nennt nur „Teilbetankungen vor dem ersten vollen Vorgang zählen nicht“. Die Rundungsausnahme (ADR 0009) fehlt in der Anwenderhilfe.
- **Tests:** FlaUI-Basisklassen und Treiber (`TankbookE2ETestBase`, `TankbookE2EDriver`, `SettingsE2ETestBase.SetFieldValue`, `TankbookTestDatabase`, `TANKATLAS_TEST_DATA_PATH`) sind vorhanden und wiederverwendbar. Unit-Tests für `ConsumptionCalculator` (10 Tests) decken Vollvorgänge, Teilbetankungen, Elektro, Hybrid. Nicht getestet: `DetectExtension` mit `mif1`/`msf1`, Löschfehler-Zweig mit Ergebnis ungleich `Ok`/`EntryNotFound` in `TankbookEntriesViewModel`.
- **Dokumentation/Version:** `docs/help/Tankbuch/` (4 Dateien), ADR 0009 (nächste ADR-Nummer 0010), `changes.log` (oberster Eintrag Schritt 13, Version 0.1.17; `Tankradar.MAUI.csproj` führt fest 0.1.0, die Pipeline setzt die Version). iOS: `MtouchInterpreter=all` und `TrimmerRootAssembly` für `Tankradar.MAUI`, `Microsoft.EntityFrameworkCore`, `...Relational`, `...Sqlite`, `Microsoft.Data.Sqlite`.
- **Designentwurf:** Der Ordner `design-draft/` enthält `stitch_smart_fuel_charge_tracker.zip` (im Scratchpad entpackt, nur gelesen). Der Bildschirm `tankbuch_verbrauchsauswertung` zeigt Monatsanalyse (Ø Verbrauch, Kosten/100 km, Gesamtkosten Monat), Trendzeilen gegenüber dem Vormonat, ein Balkendiagramm der letzten 6 Betankungen mit Zielmarke und Plakette „Effizient“; die `screen.png`-Dateien sind defekt (Text „<FIFE Image failed to fetch>“), Quelle ist nur das HTML. Keine Darstellung für zu wenige Daten, Hybrid oder Startverbrauch. Einzelheiten: [Designentwurf](inventory/design.md).

Test-Ausgangszustand: Vor der Bestandsaufnahme wurden Build (Release, Windows-Ziel, 0 Warnungen) und alle drei Suiten ausgeführt: Unit 1028/1028, Integration 120/120, FlaUI-E2E 90/90 bestanden, 0 Fehlschläge, 0 übersprungen (Stand: Commit `799efb3`, nur untracked `docs/features/`). Keine bekannten Fehlschläge. Testlücken: Coverage-Schwelle, `dotnet format`, Paket-Sicherheitsprüfung, `TreatWarningsAsErrors`-Build, Node-/Python-/iOS-Skripttests und alle nicht-Windows-Ziele wurden nicht ausgeführt. Nachweis: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik und ViewModels](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [UI, Texte, AutomationIds, Grafik](inventory/ui.md)
- [Designentwurf Statistik-Darstellung](inventory/design.md)
- [Dokumentation, Versionierung, iOS-Einstellungen, CI](inventory/project.md)
- [Tests (inkl. Ausgangslauf, Testprojekte, Hilfsmethoden)](inventory/tests.md)
