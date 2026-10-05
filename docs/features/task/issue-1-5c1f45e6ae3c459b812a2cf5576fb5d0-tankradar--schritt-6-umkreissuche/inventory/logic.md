## `MapViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Konstruktor (parameterlos) | public | Setzt `Title = "Karte"`; keine weitere Logik, keine Abhängigkeiten |

`MapPage` (`src/Tankradar.MAUI/Views/MapPage.xaml`) enthält nur ein `VerticalStackLayout` mit `MapPage.Headline` und Platzhaltertext. `MapViewModel` ist transient in `MauiProgram` registriert. `BaseViewModel` bietet `Title`, `IsBusy`, `OnAppearing`; `TankradarContentPage` leitet `OnAppearing` weiter.

## `FuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchNearbyAsync` | public | Validiert (Breite/Länge, Radius 1..`MaxRadiusKm`, Sorten), nutzt Such-Cache (Schlüssel auf 3 Nachkommastellen gerundet), offline/Fehler: Rückfall auf `IPriceRepository.FindNearbyAsync`; filtert Preise auf die angefragten Sorten und entfernt Stationen ohne Preis; Live-Ergebnis nach `DistanceKm` sortiert |
| `GetStationDetailAsync` | public | Details mit Cache und Rückfall |
| `CheckAvailabilityAsync` | public | Probe-Suche bei Berlin, Radius 1 |

Events: keine.

## `PriceFreshness` / `StationHints` (statisch)
Dateien: `src/Tankradar.MAUI/Models/Pricing/PriceFreshness.cs`, `StationHints.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `PriceFreshness.GetAge/IsStale/FormatAge` | Alter mindestens 0, veraltet ab 60 Min., „vor X Min.“ |
| `StationHints.IsAutomatedStation` | durchgehend geöffnet laut `WholeDay` bzw. Öffnungszeiten |
| `StationHints.HasUnconfirmedPrice` | mindestens ein Preis veraltet |

## `PriceRepository`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DistanceKm` (statisch) | public | Haversine |
| `FindNearbyAsync` | public | Stationen im Radius aus dem Cache inkl. berechneter `DistanceKm`, jüngste Preise je Sorte; speichert keine Suchposition |
| `SaveAsync`, `GetStationAsync` | public | Speichern mit Zeitstempel, Einzelstation |

## `ConnectionMonitor`, `MauiNetworkStatusSource`
Datei: `src/Tankradar.MAUI/Services/Pricing/ConnectionMonitor.cs`

Publiziert `ConnectionChanged` und `ConnectionRestored`; `IsOnline` aus `INetworkStatusSource`. Genutzt von `FuelPriceService`; kein ViewModel abonniert die Events bisher.

## `AppDataPathProvider`, `PriceApiOptions.FromEnvironment`, `ApiKeyStoreSelector`
Dateien: `src/Tankradar.MAUI/Services/AppDataPathProvider.cs`, `Services/Pricing/PriceApiOptions.cs`, `Services/Pricing/ApiKeys.cs`

Alle lesen `TestDataPaths.TestDataPathEnvironmentVariable`; nicht leer = Testmodus (isoliertes Datenverzeichnis, Endpunkt-Override nur Loopback-HTTP mit kurzen Wartezeiten, Schlüssel im Speicher, ohne `TANKRADAR_PRICE_API_URL` Abruf verweigert).

## `SettingsViewModel`, `SettingsService`, `PriceTexts`, `SettingsTexts`
`SettingsService.LoadAsync/SaveAsync` laden/speichern `AppSettings`; `SettingsViewModel` speichert jede Änderung sofort (Muster: `ChoiceOptionViewModel<T>`-Radioauswahl, Karten mit Stil `SettingsCard`). `PriceTexts` enthält `PriceUnconfirmed`, `AutomatedStation`, Attribution, `GetCheckMessage(PriceFailure)`; `SettingsTexts.GetLabel` liefert Anzeigenamen für Sorten und Enums.

## Plattform- und Projektdateien
- `src/Tankradar.MAUI/Platforms/iOS/Info.plist` und `Platforms/MacCatalyst/Info.plist`: keine Standort-Schlüssel.
- `src/Tankradar.MAUI/Platforms/Windows/Package.appxmanifest`: nur `rescap:Capability runFullTrust`.
- `src/Tankradar.MAUI/Tankradar.MAUI.csproj`: `WindowsPackageType=None`, Version 0.1.0.
- `src/Tankradar.MAUI/MauiProgram.cs`: `AddPriceServices`; kein Standortdienst registriert.
- `scripts/create-review-version.ps1`: Parameter `-Version`, `-ChangelogFile`.
