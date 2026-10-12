# Tasks: Tankradar Schritt 14 – Verbrauchs- und Kostenauswertung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Modelle | `TankbookAverageSource` Enum anlegen | Offen | — |
| 2 | Modelle | `TankbookStatisticsStatus` Enum anlegen | Offen | — |
| 3 | Modelle | `MonthlyCostSummary` anlegen | Offen | — |
| 4 | Modelle | `TrendPoint` anlegen | Offen | — |
| 5 | Modelle | `TankbookKindStatistics` anlegen | Offen | — |
| 6 | Modelle | `TankbookStatistics` anlegen | Offen | — |
| 7 | Modelle | `TankbookStatisticsLoad` anlegen | Offen | — |
| 8 | Modelle | `TankbookStatisticsLimits` (`ChartMaxPoints` = 12) anlegen | Offen | — |
| 9 | Modelle | `ChartKind`, `ChartPoint`, `ChartData` anlegen | Offen | — |
| 10 | Modelle | `TankbookEntryOverview` um `IntervalQuantity` und `IntervalCost` erweitern | Offen | — |
| 11 | Logik | `ConsumptionCalculator.Calculate` um Intervallmenge und Intervallkosten erweitern | Offen | — |
| 12 | Logik | `TankbookStatisticsCalculator` anlegen (Intervalle, Durchschnitt, Kosten/100 km) | Offen | — |
| 13 | Logik | `TankbookStatisticsCalculator`: Startverbrauch-Fallback je Art | Offen | — |
| 14 | Logik | `TankbookStatisticsCalculator`: Monatsreihe lückenlos mit Referenzdatum | Offen | — |
| 15 | Logik | `TankbookStatisticsCalculator`: Verlaufspunkte | Offen | — |
| 16 | Logik | `ITankbookStatisticsService` anlegen | Offen | — |
| 17 | Logik | `TankbookStatisticsService` anlegen | Offen | — |
| 18 | Logik | DI-Registrierung Dienst und ViewModel in `MauiProgram` | Offen | — |
| 19 | Logik | `ChartGeometry` anlegen | Offen | — |
| 20 | Texte | `TankbookStatisticsTexts` anlegen | Offen | — |
| 21 | Texte | `TankbookEntryTexts.PartialHint` ergänzen | Offen | — |
| 22 | UI | `ChartPalette` anlegen | Offen | — |
| 23 | UI | `LineChartDrawable` anlegen | Offen | — |
| 24 | UI | `BarChartDrawable` anlegen | Offen | — |
| 25 | UI | `ChartGraphicsView` anlegen | Offen | — |
| 26 | UI | `MonthlyCostRowViewModel` anlegen | Offen | — |
| 27 | UI | `TankbookKindStatisticsViewModel` anlegen | Offen | — |
| 28 | UI | `TankbookStatisticsViewModel` anlegen | Offen | — |
| 29 | UI | `TankbookEntriesViewModel`: `Statistics`, `LoadAsync`, `Show` anpassen | Offen | — |
| 30 | UI | `TankbookEntryFormViewModel`: `ShowPartialHint` ergänzen | Offen | — |
| 31 | UI | `TankbookEntriesPage.xaml`: Bereich Auswertung mit AutomationIds und Semantik | Offen | — |
| 32 | UI | `TankbookEntriesPage.xaml`: `PartialHint` im Formular | Offen | — |
| 33 | Konfiguration | `coverlet.runsettings`: Ausschluss `**/Views/Charts/**` | Offen | — |
| 34 | Konfiguration | Prüfung csproj-xmldoc-Check (`get { return …; }`, XML-Doku) | Offen | — |
| 35 | Tests Unit | `ConsumptionCalculatorTests_Interval` schreiben | Offen | — |
| 36 | Tests Unit | `TankbookStatisticsCalculatorTests_Average` schreiben | Offen | — |
| 37 | Tests Unit | `TankbookStatisticsCalculatorTests_Threshold` schreiben | Offen | — |
| 38 | Tests Unit | `TankbookStatisticsCalculatorTests_Cost` schreiben | Offen | — |
| 39 | Tests Unit | `TankbookStatisticsCalculatorTests_Months` schreiben (Monatsgrenzen) | Offen | — |
| 40 | Tests Unit | `TankbookStatisticsCalculatorTests_Trend` schreiben | Offen | — |
| 41 | Tests Unit | `TankbookStatisticsServiceTests_Read` schreiben | Offen | — |
| 42 | Tests Unit | `TankbookStatisticsViewModelTests_Load` schreiben | Offen | — |
| 43 | Tests Unit | `TankbookEntriesViewModelTests_Statistics` schreiben | Offen | — |
| 44 | Tests Unit | `TankbookEntryFormViewModelTests_PartialHint` schreiben | Offen | — |
| 45 | Tests Unit | `TankbookStatisticsTextsTests_Format` und `_Messages` schreiben | Offen | — |
| 46 | Tests Unit | `TankbookEntryTextsTests_Messages` um `PartialHint` ergänzen | Offen | — |
| 47 | Tests Unit | `ChartGeometryTests` schreiben | Offen | — |
| 48 | Tests Unit | `TankbookEntryFactory.CreateAt` ergänzen | Offen | — |
| 49 | Tests Unit | `TankbookEntriesViewModelTestBase` an neuen Konstruktor anpassen | Offen | — |
| 50 | Tests Unit | Bestehende Calculator-/Service-Tests auf `TankbookEntryOverview`-Vergleiche prüfen | Offen | — |
| 51 | Tests Unit | `ReceiptPhotoStoreTests_Detection` (`mif1`, `msf1`, unbekannte Marke, zu kurz, `heic`) schreiben | Offen | — |
| 52 | Tests Unit | `StubTankbookEntryService` anlegen | Offen | — |
| 53 | Tests Unit | `TankbookEntriesViewModelTests_DeleteResult` (Löschfehler-Zweig) schreiben | Offen | — |
| 54 | Tests Integration | `TankbookStatisticsServiceTests_RealDatabase` schreiben | Offen | — |
| 55 | E2E-Tests | `TankbookTestDatabase` um Art, Gesamtbetrag und explizite Zeitpunkte erweitern | Offen | — |
| 56 | E2E-Tests | `TankbookE2EDriver`: Lesehilfen für die Auswertung | Offen | — |
| 57 | E2E-Tests | `TankbookStatisticsE2ETests_Flow` schreiben | Offen | — |
| 58 | E2E-Tests | `TankbookStatisticsE2ETests_Update` schreiben | Offen | — |
| 59 | E2E-Tests | `TankbookStatisticsE2ETests_Fallback` schreiben | Offen | — |
| 60 | E2E-Tests | `TankbookStatisticsE2ETests_Months` schreiben | Offen | — |
| 61 | E2E-Tests | `TankbookStatisticsE2ETests_Partial` schreiben | Offen | — |
| 62 | E2E-Tests | `TankbookStatisticsE2ETests_Hybrid` schreiben | Offen | — |
| 63 | E2E-Tests | `TankbookEntriesE2ETests_PartialHint` schreiben | Offen | — |
| 64 | E2E-Tests | `TankbookStatisticsE2ETests_Persistence` schreiben | Offen | — |
| 65 | E2E-Tests | `OfflineE2ETests_Tankbook` um Auswertung ergänzen | Offen | — |
| 66 | Dokumentation | `docs/adr/0010-tankbook-statistics.md` schreiben (MAUI Graphics, iOS-Interpreter/Trimming, Lizenz, Definitionen, Abweichungen) | Offen | — |
| 67 | Dokumentation | `docs/help/Tankbuch/beschreibung.md`: Abschnitt Auswertung | Offen | — |
| 68 | Dokumentation | `docs/help/Tankbuch/beschreibung.md`: Rundungsausnahme (ADR 0009) | Offen | — |
| 69 | Dokumentation | `docs/help/Tankbuch/beschreibung.md`: präziser Teilbetankungs-Hinweis | Offen | — |
| 70 | Dokumentation | `docs/help/Tankbuch/ablauf-anwender.md` und `ablauf-technisch.md` ergänzen | Offen | — |
| 71 | Dokumentation | `docs/help/index.md` Beschreibungszeile ergänzen | Offen | — |
| 72 | Dokumentation | `changes.log` Eintrag Version 0.1.18 | Offen | — |
| 73 | Abschluss | Gesamtlauf: Build, `dotnet format`, Unit, Integration, E2E grün | Offen | — |
