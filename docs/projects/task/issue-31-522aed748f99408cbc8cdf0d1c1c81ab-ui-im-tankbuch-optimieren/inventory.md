# Bestandsaufnahme – UI im Tankbuch optimieren

## Ausgangslage

- .NET-MAUI-App mit MVVM; der Tab `tankbook` lädt `TankbookEntriesPage` über `ContentTemplate`.
- `TankradarContentPage.OnAppearing` ruft synchron `TankbookEntriesViewModel.OnAppearing`; diese setzt `LastTask = LoadAndAnnounceAsync()`.
- `LoadAsync` beginnt ohne initialen UI-Yield mit `_vehicleService.GetVehiclesAsync()`. Danach lädt `LoadEntriesAsync` Vorgänge, bekannte Tankstellen und anschließend die Statistik.
- Die Datenbankinitialisierung ist gecacht und wird beim App-Start bereits angestoßen; trotzdem können EF-/SQLite- und Berechnungsarbeiten synchron auf dem UI-Thread fortsetzen, bevor der erste wirklich asynchrone Teil erreicht ist.
- `TankbookStatisticsCalculator.BuildMonths` erzeugt eine lückenlose Monatsreihe vom ersten Vorgang bis heute inklusive `EntryCount = 0`.
- `TankbookKindStatisticsViewModel` übernimmt alle Monate in `Months`; `MonthlyCostRowViewModel` rendert jede Zeile.
- `TankbookEntriesPage.xaml` zeigt Bearbeiten und Löschen als normale Text-Buttons mit `TankbookEntry.Item.Edit` und `TankbookEntry.Item.Delete`.
- Der Exportbereich ist eine immer sichtbare Karte mit allen Optionen; `TankbookExportViewModel` besitzt noch keinen Auf-/Zuklappzustand.
- Wiederverwendbare Konventionen: `TankbookOutlineButton`, `FuelKindChip`, `MinimumTouchTarget`, `SemanticProperties.*`, Text-Glyphen wie `SelectVehicleGlyph`, `MapTexts.PanelExpandGlyph`/`PanelCollapseGlyph`.

## Tests und Konventionen

- Unit-Tests liegen für Ladeverhalten, Statistik, Eintragsliste und Export vor.
- E2E-Tests bedienen die App über FlaUI-AutomationIds; die aktuellen Exporttests erwarten sichtbare Exportelemente, die Monats-E2E-Tests erwarten bisher auch `0,00 €`-Monate.
- C#-Projektdateien erzeugen XML-Dokumentationswarnungen als Fehler; neue öffentliche Member brauchen XML-Dokumentation.
- Keine Schema- oder Dependency-Änderung erforderlich.

## Risiken und Einschränkungen

- Ein reines `Task.Run` wäre riskant, weil ViewModel-Eigenschaften und Bindings UI-threadgebunden sind. Die Entkopplung muss über einen UI-Yield bzw. dispatcherfreundliches Fortsetzen erfolgen.
- Monatsfilter nur in der sichtbaren Liste anwenden, damit Monatskarte und Kostenverlauf nicht ungewollt ihre bisherige Zeitachse verlieren.
- Exporttests müssen den Bereich vor Bedienung aufklappen oder der Treffer muss zentral erweitert werden.

## Detailnachweise

- `src/Tankradar.MAUI/Views/TankbookEntriesPage.xaml`
- `src/Tankradar.MAUI/ViewModels/TankbookEntriesViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/TankbookKindStatisticsViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/TankbookExportViewModel.cs`
- `src/Tankradar.MAUI/Services/Tankbook/TankbookStatisticsCalculator.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookExportE2ETestBase.cs`
