# Logik

## `ConsumptionCalculator` (statisch)
Datei: `src/Tankradar.MAUI/Services/Tankbook/ConsumptionCalculator.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Calculate(entries, startOdometerKm, startKind)` | public static | Berechnet je Art eine eigene Kette (aufsteigend) und liefert `TankbookEntryOverview` mit Strecke, Verbrauch, Intervallmenge/-kosten, `IsFromStart`. Die Berechnung braucht die vollständige Kette; ein vorab gefilterter Zeitraum würde das erste Intervall verfälschen. |

## `TankbookStatisticsCalculator` (statisch)
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookStatisticsCalculator.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Calculate(vehicle, overviews, today)` | public static | Je Art Durchschnitt (mengengewichtet), Kosten pro 100 km, Monatsreihe, Trend. `BuildMonths` füllt lückenlos vom ersten Vorgang bis zum späteren aus letztem Vorgang und `today`. `overviews` darf eine Teilmenge sein. |

## `TankbookStatisticsService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookStatisticsService.cs` – `GetStatisticsAsync` (Fahrzeug laden, `GetEntriesAsync`, `TimeProvider`-„heute“, `Calculate`). Kein Zeitraum.

## `TankbookEntryService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookEntryService.cs` – Lesen in `GetEntriesAsync` über `ConsumptionCalculator`; `GetKnownStationsAsync` verwendet `StationNaming.GetPrimary/GetSecondary` (Anzeigename). Vorgänge speichern `StationName` als Momentaufnahme; ein Export liest diesen Namen (`TankbookEntry.StationName`). Eine Auflösung über `StationId` auf den aktuellen Anzeigenamen gibt es für Vorgänge nicht.

## `StationNaming`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationNaming.cs` – `GetPrimary(customName, brand, name)`, `GetSecondary(...)`.

## `TankbookEntryOrder`
Datei: `src/Tankradar.MAUI/Services/Tankbook/TankbookEntryOrder.cs` – Ordnung (Zeitpunkt, Kilometerstand, Kennung), aufsteigend und absteigend.

## `TankbookEntriesViewModel` / Seite
Dateien: `ViewModels/TankbookEntriesViewModel.cs` (408 Zeilen), `Views/TankbookEntriesPage.xaml` (692 Zeilen)

- Transient, `IQueryAttributable` (Parameter `vehicleId`); hält `Form` (`TankbookEntryFormViewModel`) und `Statistics` (`TankbookStatisticsViewModel`) per Konstruktor. `OnAppearing()` startet `LoadAsync()` (Fahrzeug, Vorgänge, bekannte Tankstellen, Auswertung). Fehler über `StatusMessage` (`TankbookEntries.Status`). Keine Abonnements auf Wiederherstellungsereignisse.
- Seitenaufbau: Kopf (`TankbookEntries.Vehicle`), Schaltfläche `TankbookEntries.Add`, Formular, Auswertung (`TankbookEntries.Statistics.*`), Liste. AutomationId-Schema `TankbookEntries.<Bereich>.<Element>`; Chips im Stil `FuelKindChip` (FlexLayout, `BindableLayout`, `IChoiceOption`), Karten `TankbookCard`, Schaltflächen `TankbookButton` (`Resources/TankbookStyles`). Das Datumsfeld nutzt `DatePicker`/`TimePicker` (`TankbookEntries.Form.Date/Time`).
- DI in `MauiProgram.cs`: Dienste Singleton, ViewModels und Seiten Transient.

## Teilen / Sicherung
- `BackupViewModel.ShareAsync(item)`: `GetFullPath`, dann `IBackupSharer.ShareAsync(path, BackupTexts.Heading)`; Fehler führen zu `BackupTexts.ShareFailed`, Protokoll nur mit Ausnahmetyp.
- `BackupViewModel.Restored` (Ereignis nach erfolgreicher Wiederherstellung, Zeile 255): in `src/Tankradar.MAUI` nirgends abonniert. Andere ViewModels laden bei `OnAppearing()` neu; eine bereits geöffnete Vorgangsseite wird nicht aktiv benachrichtigt (Befund H5).
- `BackupDatabasePreparer` (ab Zeile 375): Textgrenzenprüfung `SELECT 1 FROM <Tabelle> WHERE length(col) > $max OR length(CAST(col AS BLOB)) > $maxBytes` über `BackupTextLimits.Columns`. ADR 0011 nennt nur die Zeichenprüfung (Befund H2). Die Integrationstests enthalten keinen Fall mit eingebettetem NUL-Zeichen (Suche nach `char(0)` ohne Treffer in den Sicherungstests).

## Dateiablage
`IAppDataPathProvider.GetCacheDirectory()` (Zwischenspeicher); Sicherungen liegen unter `<Datenverzeichnis>/backups`, geteilte Testkopien unter `<Datenverzeichnis>/test-shared` (`BackupFormat.TestSharedDirectoryName`), Fotos unter `receipts/`.

## PDF-Möglichkeiten im Bestand (Beleg)

| Prüfung | Befund |
|---------|--------|
| `Tankradar.MAUI.csproj` | `PackageReference` nur auf `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug`, `Microsoft.EntityFrameworkCore.Sqlite`, `SQLitePCLRaw.bundle_e_sqlite3`, `Microsoft.EntityFrameworkCore.Design`. Kein PDF-, SkiaSharp- oder SkiaSharp.Views-Paket. |
| `obj/project.assets.json` | Aufgelöst sind `Microsoft.Maui.Graphics 10.0.20` und (Windows) `Microsoft.Maui.Graphics.Win2D.WinUI.Desktop 10.0.20`. **Kein** `SkiaSharp`, kein `Microsoft.Maui.Graphics.Skia`. `SKDocument.CreatePdf` steht deshalb ohne neues Paket nicht zur Verfügung. |
| `Microsoft.Maui.Graphics 10.0.20` (XML-Doku aller Zielframeworks, iOS, Android, Windows) | PDF-Typen: `IPdfPage` (`Width`, `Height`, `PageNumber`, `Save`, `SaveAsync`), `IPdfRenderService.CreatePage(Stream, int)`, `PdfPageExtensions`, `PictureCommand.DrawPdfPage`. Das ist das Rendern vorhandener PDFs, kein Schreiben eines Zeichenkontexts als PDF. |
| Lokaler NuGet-Cache (Fremdprojekte, nicht referenziert) | `skiasharp` 3.116.1 bis 4.153.1 (Lizenz MIT), `questpdf` (Community-Lizenz, nicht MIT/Apache/BSD), `pdfpig` (Leser, Apache-2.0). |
| Repository-Suche nach `skiasharp`, `questpdf`, `pdfsharp`, `.pdf` | Nur ADR 0010, das SkiaSharp-basierte Pakete unter iOS-Interpreter/Trimming als Risiko nennt. |
| iOS-Konfiguration | `MtouchInterpreter=all`, `TrimmerRootAssembly` für `Tankradar.MAUI`, EF Core, `Microsoft.Data.Sqlite`; Kommentar nennt das 60-Minuten-Limit des iOS-CI-Builds. SkiaSharp bräuchte native iOS-Assets und gegebenenfalls Trimmer-Ausnahmen; Lauffähigkeit ist hier nicht belegt, eine Änderung der iOS-Einstellungen ist untersagt (nur melden). |
| .NET-Basisbibliothek | Kein PDF-Schreiber. `System.IO.Compression` (für Sicherungen genutzt) bietet Flate, wird für ein unkomprimiertes oder minimal komprimiertes PDF nicht benötigt. |

Folgerung: Ohne neue Abhängigkeit ist ein PDF nur mit einem eigenen, rein verwalteten PDF-Schreiber möglich; MAUI/Skia liefert ihn im aktuellen Stand nicht.

## Teilen/Speichern je Plattform
- `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(ShareFileRequest)` wird in `MauiBackupSharer` bereits genutzt (Windows: System-Teilen-Dialog, iOS: Teilen-Blatt, Android: Chooser). Eine eigene Windows-Speichern-Logik und `FileSaver` (CommunityToolkit.Maui) gibt es nicht und ist nicht referenziert.
- Zu teilende Dateien müssen im lesbaren App-Bereich liegen (Cache- oder Datenverzeichnis).
