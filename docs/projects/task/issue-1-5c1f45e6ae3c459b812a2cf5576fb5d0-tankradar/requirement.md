# Anforderungsübersetzung: Tankradar

**Aufgaben-ID:** 5c1f45e6-ae3c-459b-812a-2cf5576fb5d0  
**Projekt:** Tankradar – Preis- und Ladestationen-Tracking für Kraftstoff und Strom  
**Status:** Anforderungsanalyse  
**Erstellt:** 2026-09-28

---

## 1. Fachliche Zusammenfassung

Entwicklung einer mobilen Anwendung zur Verwaltung von Tankstellenpreisen (Kraftstoff und Strom) sowie eines digitalen Tankbuches. Die Anwendung ermöglicht Nutzern, Favoriten-Gruppen zu erstellen und zu verwalten, Tankstellen nach Standort oder Adresse zu suchen, historische Tankvorgänge zu erfassen und Verbrauchsstatistiken auszuwerten. Die Lösung muss offline-fähig sein, mit lokaler SQLite-Datenbank arbeiten und offizielle Preis-APIs nutzen. Zielplattformen sind iOS (primär) und Windows (Entwicklung/Debugging), implementiert über .NET MAUI mit MVVM-Architektur.

---

## 2. Anforderungsdekomposition und technische Objekte

### 2.1 Fachliche Kerne und Entitäten

#### **Fuel Station (Tankstelle)**
- Eindeutige Identifikation über externe API (z. B. Stationsnummer)
- Attribute: Name, Adresse, GPS-Koordinaten, Entfernung, Öffnungszeiten
- Dynamische Daten: Preise (Kraftstoff nach Sorten, Strom nach Typ), Provisorien (unbestätigte Preise, Automatenstation)
- Zeitstempel für Preisaktualität (Cache-Verwaltung)

#### **Favorite Group (Favoritengruppe)**
- Nutzer-definierte Sammlung von Tankstellen (z. B. "Arbeitsweg", "Heimat", "Urlaub")
- Attribute: Name, Erstellungsdatum, optionale Beschreibung
- Enthält Liste von Station-Referenzen mit optionalen Notizen pro Eintrag

#### **Vehicle (Fahrzeug)**
- Verwaltung mehrerer Fahrzeuge pro Nutzer
- Attribute: Name/Modell, Kraftstoffart (enum: Benzin, Diesel, Elektro, Hybrid), Start-Kilometerstand, optionaler Start-Verbrauch
- Relationen: Mehrere Fahrzeuge → mehrere Tankbuch-Einträge

#### **Fuel Entry / Charge Entry (Tankvorgang)**
- Erfassung eines Tankvorgangs oder Ladevorgangs
- Erforderliche Attribute: Datum, Uhrzeit, zugeordnetes Fahrzeug, Tankstelle (aus Liste oder manuell), Menge (Liter/kWh), Preis pro Einheit, Gesamtbetrag, Kilometerstand, optionale Notiz, optionales Fotodokument
- Berechnete Attribute: Verbrauch pro 100 km = (Menge / gefahrene Strecke) * 100
- Relationen: Fahrzeug → mehrere Einträge, Station (sofern zugewiesen)

#### **Price Cache (Preis-Cache)**
- Speicherung der letzten abgerufenen Preise mit Zeitstempel
- Attribute: Station-ID, Kraftstoffsorte/Stromtyp, Preis, Zeitstempel, Quelle (API)
- Bestimmung der Aktualität ausschließlich über Zeitstempel
- Historische Stände bleiben für Statistiken und Backups verfügbar

#### **Settings (Einstellungen)**
- Nutzer-spezifische Konfiguration
- Kraftstoffsorten: Auswahl und Reihenfolge
- Strom: Aktivierung und Typauswahl (AC/DC/HPC)
- Standort: GPS-Nutzung (enum: Immer, Nur bei Nutzung, Nie)
- Anzeige: Standard-Ansicht (Liste/Karte), Standard-Sortierung (Preis/Entfernung/Name)
- Backup & Sync: Konfiguration lokaler Sicherung und optionaler Cloud-Sync

### 2.2 Benutzerinteraktionen und Workflows

#### **Station Search (Stationssuche)**
- **Nach Standort:** GPS-Nutzung mit einstellbarem Radius (5–50 km), Filter nach Kraft- und Stromart
- **Nach Adresse:** Eingabe von Adresse/Ort/PLZ mit Radius-Definition
- **Ergebnis-Darstellung:** Listenansicht (sortierbar nach Preis/Entfernung/Name) und Kartenansicht (farbcodiert nach Preisniveau)
- **Route-Support (optional):** Anzeige von Stationen entlang einer geplanten Route

#### **Favorite Management (Favoritenverwaltung)**
- **Hinzufügen:** Nutzer wählt eine Station in der Detail-Ansicht, klickt "Zu Favoriten hinzufügen"
  - Interaction Point: Auswahl einer bestehenden Gruppe ODER Erstellung einer neuen Gruppe erforderlich
- **Entfernen:** Button "Aus Favoriten entfernen" falls Station bereits zugeordnet
- **Gruppenverwaltung:** Erstellen, Umbenennen, Löschen von Gruppen
- **Gruppen-Ansicht:** Liste der Tankstellen mit optionalen Notizen und Prioritäten

#### **Logbook Management (Tankbuch-Verwaltung)**
- **Vorgang erfassen:** Interaktive Erfassung mit Datum, Uhrzeit, Fahrzeug, Tankstelle, Menge, Preis, Kilometerstand, optionale Notiz und Foto
- **Statistik-Auswertung:** Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten, Verlaufsgrafiken
- **Export (optional):** CSV oder PDF

#### **Offline Mode (Offline-Modus)**
- **Preis-Cache:** Zwischenspeicherung der letzten bekannten Preise mit Zeitstempeln
- **Tankbuch:** Vollständig offline nutzbar
- **Synchronisation:** Bei Wiederverbindung werden Daten aktualisiert

---

## 3. Betroffene Klassen und Komponenten

### 3.1 Datenmodellklassen (Domain Model)

```
├── Station (Tankstelle)
│   ├── StationId : string
│   ├── Name : string
│   ├── Address : Address
│   ├── GpsCoordinates : GpsLocation
│   ├── Distance : double (optional, berechnete Eigenschaft)
│   ├── OpeningHours : OpeningHours[]
│   ├── PaymentMethods : PaymentMethod[]
│   └── Provisions : Provision[] (z. B. UnconfirmedPrice, AutomaticPump)
│
├── FavoriteGroup
│   ├── Id : string (GUID)
│   ├── Name : string
│   ├── CreatedDate : DateTime
│   ├── Description : string (optional)
│   └── Stations : FavoriteGroupStation[] (Join-Entität)
│
├── FavoriteGroupStation (Join-Entität für Many-to-Many)
│   ├── GroupId : string
│   ├── StationId : string
│   ├── Notes : string (optional)
│   ├── Priority : int (optional)
│   └── AddedDate : DateTime
│
├── Vehicle
│   ├── Id : string (GUID)
│   ├── Name : string
│   ├── FuelType : FuelTypeEnum
│   ├── StartOdometer : int
│   ├── StartConsumption : double (optional)
│   └── CreatedDate : DateTime
│
├── FuelEntry / ChargeEntry (polymorphes Design oder gemeinsame Basis)
│   ├── Id : string (GUID)
│   ├── Date : DateTime
│   ├── VehicleId : string (FK)
│   ├── StationId : string (optional FK)
│   ├── ManualStationName : string (optional, falls Station nicht in DB)
│   ├── Quantity : double (Liter oder kWh)
│   ├── PricePerUnit : decimal
│   ├── TotalAmount : decimal
│   ├── Odometer : int
│   ├── Notes : string (optional)
│   ├── ReceiptPhotoUri : string (optional)
│   ├── CalculatedConsumption : double (computed)
│   └── CreatedDate : DateTime
│
├── FuelTypeEnum : enum
│   ├── Petrol95
│   ├── Petrol98
│   ├── Diesel
│   ├── Electricity
│   ├── LPG
│   └── ...
│
├── PriceCache (für Offline-Modus)
│   ├── Id : string (GUID)
│   ├── StationId : string
│   ├── FuelType : FuelTypeEnum (oder Electricity Variant)
│   ├── Price : decimal
│   ├── Timestamp : DateTime
│   ├── Source : string (z. B. "FuelPriceAPI v1.0")
│   └── ExpiryEstimate : DateTime
│
├── ElectricityChargeType : enum
│   ├── AC
│   ├── DC
│   └── HPC
│
├── AppSettings
│   ├── SelectedFuelTypes : FuelTypeEnum[]
│   ├── EnableElectricityPrices : bool
│   ├── SelectedChargeTypes : ElectricityChargeType[]
│   ├── LocationUsage : LocationUsageEnum (Always, OnlyWhenNeeded, Never)
│   ├── DefaultViewType : ViewTypeEnum (List, Map)
│   ├── DefaultSortOrder : SortOrderEnum (Price, Distance, Name)
│   ├── CloudSyncEnabled : bool (optional)
│   └── LastSyncDate : DateTime
│
├── LocationUsageEnum : enum
│   ├── Always
│   ├── OnlyWhenNeeded
│   └── Never
│
├── GpsLocation
│   ├── Latitude : double
│   ├── Longitude : double
│   └── AccuracyMeters : double
│
├── Address
│   ├── Street : string
│   ├── HouseNumber : string
│   ├── PostalCode : string
│   ├── City : string
│   └── Country : string
│
└── Provision (abstrakte Basis oder Union-Type)
    ├── UnconfirmedPrice
    ├── AutomaticPump
    └── ... (weitere Provisionen)
```

### 3.2 Service-Klassen und Interfaces

```
├── IFuelPriceService
│   ├── GetStationsByLocationAsync(GpsLocation, radiusKm, fuelFilter) → List<Station>
│   ├── GetStationsByAddressAsync(address, radiusKm, fuelFilter) → List<Station>
│   ├── GetStationDetailsAsync(stationId) → Station
│   ├── GetStationsAlongRouteAsync(routeCoordinates, radiusKm) → List<Station>
│   └── RefreshPricesAsync() → void
│
├── IFavoriteGroupService
│   ├── CreateGroupAsync(groupName) → FavoriteGroup
│   ├── RenameGroupAsync(groupId, newName) → void
│   ├── DeleteGroupAsync(groupId) → void
│   ├── AddStationToGroupAsync(groupId, stationId, notes?) → void
│   ├── RemoveStationFromGroupAsync(groupId, stationId) → void
│   ├── GetGroupsAsync() → List<FavoriteGroup>
│   ├── GetGroupByIdAsync(groupId) → FavoriteGroup
│   └── UpdateStationNotesAsync(groupId, stationId, notes) → void
│
├── IVehicleService
│   ├── CreateVehicleAsync(vehicle) → Vehicle
│   ├── UpdateVehicleAsync(vehicle) → void
│   ├── DeleteVehicleAsync(vehicleId) → void
│   ├── GetVehiclesAsync() → List<Vehicle>
│   └── GetVehicleByIdAsync(vehicleId) → Vehicle
│
├── IFuelEntryService / IChargeEntryService
│   ├── RecordFuelEntryAsync(entry) → FuelEntry
│   ├── RecordChargeEntryAsync(entry) → ChargeEntry
│   ├── GetEntriesByVehicleAsync(vehicleId) → List<FuelEntry/ChargeEntry>
│   ├── CalculateConsumptionAsync(vehicleId, dateRange?) → ConsumptionStatistics
│   ├── GetAverageConsumptionAsync(vehicleId) → double
│   ├── GetCostsPer100KmAsync(vehicleId, dateRange?) → decimal
│   ├── GetMonthlyCostsAsync(vehicleId, year, month) → decimal
│   └── DeleteEntryAsync(entryId) → void
│
├── ILocationService
│   ├── GetCurrentLocationAsync() → GpsLocation
│   ├── RequestLocationPermissionAsync() → bool
│   └── IsLocationEnabled() → bool
│
├── IPriceCacheService
│   ├── GetCachedPriceAsync(stationId, fuelType) → (price: decimal?, timestamp: DateTime?)
│   ├── CachePriceAsync(stationId, fuelType, price, timestamp) → void
│   ├── GetCacheAgeAsync(stationId, fuelType) → TimeSpan
│   └── InvalidateCacheAsync(stationId?) → void
│
├── ISettingsService
│   ├── GetSettingsAsync() → AppSettings
│   ├── UpdateSettingsAsync(settings) → void
│   ├── GetSelectedFuelTypesAsync() → List<FuelTypeEnum>
│   ├── SetSelectedFuelTypesAsync(types) → void
│   ├── IsElectricityEnabled() → bool
│   └── GetDefaultViewTypeAsync() → ViewTypeEnum
│
├── IBackupService
│   ├── CreateBackupAsync() → BackupFile
│   ├── RestoreBackupAsync(backupPath) → void
│   ├── GetBackupHistoryAsync() → List<BackupMetadata>
│   └── DeleteBackupAsync(backupId) → void
│
├── ICloudSyncService (optional)
│   ├── SyncAsync() → void
│   ├── IsSyncEnabled() → bool
│   ├── GetLastSyncDateAsync() → DateTime?
│   └── SetSyncCredentialsAsync(credentials) → void
│
└── IStatisticsService
    ├── CalculateConsumptionStatsAsync(vehicleId, dateRange) → ConsumptionStatistics
    ├── GetMonthlyTrendAsync(vehicleId, months) → List<MonthlySummary>
    ├── ExportAsCSVAsync(vehicleId, dateRange) → Stream
    └── ExportAsPDFAsync(vehicleId, dateRange) → Stream
```

### 3.3 ViewModel-Klassen

```
├── HomeViewModel
│   ├── FavoriteGroups : ObservableCollection<FavoriteGroupViewModel>
│   ├── NearbyStations : ObservableCollection<StationViewModel> (optional)
│   ├── RefreshCommand : ICommand
│   ├── SelectGroupCommand : ICommand
│   └── OnAppearing() → Task
│
├── SearchResultsViewModel
│   ├── SearchResults : ObservableCollection<StationViewModel>
│   ├── SearchType : enum (Location, Address, Route)
│   ├── SortOrder : SortOrderEnum
│   ├── ViewType : ViewTypeEnum (List, Map)
│   ├── FilteredFuelTypes : List<FuelTypeEnum>
│   ├── ExecuteSearchCommand : ICommand
│   ├── ChangeSortCommand : ICommand
│   ├── ToggleViewCommand : ICommand
│   └── SelectStationCommand : ICommand
│
├── StationDetailViewModel
│   ├── Station : StationViewModel
│   ├── Provisions : List<ProvisionViewModel>
│   ├── OpeningHours : List<OpeningHourViewModel>
│   ├── AddToFavoritesCommand : ICommand
│   ├── RemoveFromFavoritesCommand : ICommand
│   ├── ShowGroupSelectionCommand : ICommand
│   ├── IsFavorite : bool
│   └── AvailableGroups : ObservableCollection<FavoriteGroupViewModel>
│
├── FavoriteGroupsViewModel
│   ├── Groups : ObservableCollection<FavoriteGroupViewModel>
│   ├── CreateGroupCommand : ICommand
│   ├── RenameGroupCommand : ICommand
│   ├── DeleteGroupCommand : ICommand
│   ├── SelectGroupCommand : ICommand
│   └── ViewGroupDetailsCommand : ICommand
│
├── FavoriteGroupDetailViewModel
│   ├── Group : FavoriteGroupViewModel
│   ├── Stations : ObservableCollection<GroupStationViewModel>
│   ├── RemoveStationCommand : ICommand
│   ├── UpdateNotesCommand : ICommand
│   └── SetPriorityCommand : ICommand
│
├── LogbookViewModel
│   ├── Vehicles : ObservableCollection<VehicleViewModel>
│   ├── SelectedVehicle : VehicleViewModel
│   ├── Entries : ObservableCollection<FuelEntryViewModel>
│   ├── ConsumptionStatistics : StatisticsViewModel
│   ├── RecordEntryCommand : ICommand
│   ├── SelectVehicleCommand : ICommand
│   ├── DeleteEntryCommand : ICommand
│   ├── ExportCommand : ICommand
│   └── RefreshStatisticsCommand : ICommand
│
├── VehicleManagementViewModel
│   ├── Vehicles : ObservableCollection<VehicleViewModel>
│   ├── CreateVehicleCommand : ICommand
│   ├── EditVehicleCommand : ICommand
│   ├── DeleteVehicleCommand : ICommand
│   └── SelectVehicleCommand : ICommand
│
├── SettingsViewModel
│   ├── SelectedFuelTypes : ObservableCollection<FuelTypeWithSelection>
│   ├── EnableElectricity : bool
│   ├── SelectedChargeTypes : ObservableCollection<ChargeTypeWithSelection>
│   ├── LocationUsage : LocationUsageEnum
│   ├── DefaultViewType : ViewTypeEnum
│   ├── DefaultSortOrder : SortOrderEnum
│   ├── CloudSyncEnabled : bool
│   ├── SaveSettingsCommand : ICommand
│   ├── CreateBackupCommand : ICommand
│   ├── RestoreBackupCommand : ICommand
│   └── ReorderFuelTypesCommand : ICommand
│
└── StatisticsViewModel
    ├── AverageConsumption : string
    ├── CostsPer100Km : string
    ├── MonthlyCosts : string
    ├── ConsumptionTrendData : List<ChartDataPoint>
    ├── CostTrendData : List<ChartDataPoint>
    └── GenerateChartCommand : ICommand
```

### 3.4 UI-Komponenten (XAML Views & Controls)

```
├── HomePage
│   ├── FavoriteGroupsCarousel (oder StackLayout)
│   ├── GroupStationList
│   ├── NearbyStationsSection (optional)
│   └── NavigationBar mit Suchbutton und Einstellungen
│
├── SearchPage
│   ├── SearchModeSelector (Standort, Adresse, Route)
│   ├── SearchInputSection
│   ├── RadiusSlider
│   ├── FilterCheckboxes (Spritsorten, Strom)
│   ├── SortOrderPicker
│   ├── ViewTypePicker (Liste/Karte)
│   ├── SearchResultsList / SearchResultsMap
│   └── ResultDetailPopup
│
├── StationDetailPage
│   ├── StationHeader (Name, Entfernung, Adresse)
│   ├── PricesSection (Kraftstoff + Strom)
│   ├── ProvisionsList (Provisionen)
│   ├── OpeningHoursSection
│   ├── PaymentMethodsList
│   ├── AddToFavoritesButton / RemoveFromFavoritesButton
│   ├── GroupSelectionDialog
│   └── RouteButton (optional)
│
├── FavoritesPage
│   ├── GroupTabs / GroupList
│   ├── GroupStationListView
│   ├── GroupManagementButtons (Erstellen, Umbenennen, Löschen)
│   ├── StationDetailPopup
│   └── NotesAndPrioritySection
│
├── LogbookPage
│   ├── VehicleSelector
│   ├── FuelEntryList
│   ├── RecordEntryButton
│   ├── StatisticsSummary (Verbrauch, Kosten, Verlauf)
│   ├── ExportButton
│   └── ChartsSection (Consumption & Cost Trends)
│
├── RecordEntryDialog
│   ├── DateTimePicker
│   ├── VehicleSelector
│   ├── StationSelector (Liste + Manuell)
│   ├── QuantityAndPriceInput
│   ├── OdometerInput
│   ├── NotesInput
│   ├── PhotoUploadButton
│   └── SaveButton
│
├── VehicleManagementPage
│   ├── VehicleList
│   ├── CreateVehicleButton
│   ├── EditVehicleDialog
│   ├── DeleteVehicleConfirmation
│   └── VehicleDetailView
│
├── SettingsPage
│   ├── FuelTypesSection (Checkboxes + Reordering)
│   ├── ElectricitySection (Toggle + Charge Types)
│   ├── LocationUsageSection (Picker: Immer, Bei Bedarf, Nie)
│   ├── DefaultViewSection (List / Map)
│   ├── DefaultSortSection (Preis, Entfernung, Name)
│   ├── BackupSection (Create, Restore, History)
│   ├── CloudSyncSection (optional)
│   └── SaveButton
│
├── MapView (Custom/Third-Party)
│   ├── Station Pins (farbcodiert nach Preis)
│   ├── UserLocation Marker
│   ├── TapDetection → StationDetailPopup
│   └── Zoom/Pan Controls
│
└── ChartsView (using Microcharts, SyncFusion, or Livecharts)
    ├── ConsumptionLineChart
    ├── CostLineChart
    └── Legend & Interaction
```

### 3.5 Datenbank-Schicht

```
├── TankradarDbContext (SQLite via Entity Framework Core oder andere ORM)
│   ├── DbSet<Station>
│   ├── DbSet<FavoriteGroup>
│   ├── DbSet<FavoriteGroupStation>
│   ├── DbSet<Vehicle>
│   ├── DbSet<FuelEntry>
│   ├── DbSet<ChargeEntry>
│   ├── DbSet<PriceCache>
│   ├── DbSet<AppSettings>
│   ├── Migrations
│   └── OnModelCreating() (Konfiguration von Indizes, Constraints, Relationships)
│
└── DatabaseInitializationService
    ├── EnsureDatabaseCreatedAsync() → void
    ├── MigrateAsync() → void
    ├── SeedDefaultDataAsync() (optional: Standard-Einstellungen)
    └── VerifyDatabaseIntegrityAsync() → bool
```

### 3.6 Test-Artefakte

```
├── UnitTests
│   ├── ConsumptionCalculationTests
│   ├── StatisticsServiceTests
│   ├── FavoriteGroupServiceTests
│   ├── PriceCacheServiceTests
│   ├── ValidationTests (für Eingaben)
│   ├── OfflineModeTests
│   └── BackupRestoreTests
│
├── IntegrationTests
│   ├── FuelPriceApiIntegrationTests
│   ├── ElectricityChargeApiIntegrationTests
│   ├── DatabaseIntegrationTests
│   ├── LocationServiceIntegrationTests
│   └── BackupRestoreIntegrationTests
│
├── E2E-Tests (FlaUI auf Windows)
│   ├── SearchFlowTests
│   ├── FavoritesManagementTests
│   ├── LogbookRecordingTests
│   ├── StatisticsExportTests
│   └── OfflineModeScenarioTests
│
└── TestData
    ├── MockStationData
    ├── MockPriceData
    ├── SampleVehicles
    ├── SampleEntries
    └── TestDatabase Fixtures
```

---

## 4. Implementierungsansatz

### 4.1 Projektstruktur und Schichten

**Schichtenmodell:**

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│   (XAML Views, ViewModels, Commands)    │
├─────────────────────────────────────────┤
│       Application Layer (Services)      │
│  (Orchestration, Business Logic)        │
├─────────────────────────────────────────┤
│        Domain Layer (Models)            │
│    (Entities, Value Objects, Rules)     │
├─────────────────────────────────────────┤
│      Infrastructure Layer               │
│  (Database, APIs, Caching, Location)    │
├─────────────────────────────────────────┤
│      Cross-Cutting Concerns             │
│  (Logging, DI, Configuration, Security) │
└─────────────────────────────────────────┘
```

**Projektorganisation:**

```
Tankradar/
├── Tankradar.App/                    (MAUI Shell, Main App)
├── Tankradar.App.iOS/                (iOS-spezifische Plattform)
├── Tankradar.App.Windows/            (Windows-spezifische Plattform)
├── Tankradar.Core/                   (Domain Models, Enums, Interfaces)
├── Tankradar.Services/               (Application Services)
├── Tankradar.Data/                   (EF Core DbContext, Migrations)
├── Tankradar.Api/                    (HTTP Client, API DTOs)
├── Tankradar.UI/                     (Shared XAML Components, Converters)
├── Tankradar.Security/               (Keychain/Credential Locker, Encryption)
├── Tankradar.Tests/                  (Unit Tests)
├── Tankradar.IntegrationTests/       (Integration Tests)
├── Tankradar.E2ETests/               (FlaUI E2E Tests – nur Windows)
└── Tankradar.Infrastructure/         (DI Setup, Configuration, Logging)
```

### 4.2 Architektur-Muster und Erweiterungspunkte

#### **MVVM-Muster (Model-View-ViewModel)**

- **Views:** XAML-Seiten und Steuerelemente ohne Code-Behind (oder minimal)
- **ViewModels:** Bindbare Eigenschaften, Commands für Benutzerinteraktionen
- **Services:** Injiziert in ViewModels über Constructor Injection
- **Bindings:** TwoWay, OneWay, OneWayToSource je nach Anforderung

#### **Dependency Injection (DI)**

- Zentrale Registrierung in `MauiProgram.CreateMauiApp()`
- Services als Interfaces definiert, konkrete Implementierungen registriert
- Ermöglicht einfaches Mocking in Tests

#### **Event-Driven Updates**

- `PropertyChanged` Events (INotifyPropertyChanged) für ViewModel-Properties
- `CollectionChanged` Events (INotifyCollectionChanged) für ObservableCollections
- Optional: Custom Events oder Messaging für Cross-ViewModel-Kommunikation

#### **Repository-Pattern (optional aber empfohlen)**

- Abstraktion der Datenzugriffslogik
- `IRepository<T>` mit CRUD-Operationen
- Ermöglicht Testbarkeit und Austauschbarkeit von Persistierungs-Strategien

### 4.3 Datenfluss und Synchronisationslogik

#### **Offline-First-Ansatz**

1. **Lokales Laden:** Daten aus SQLite Database zuerst laden und darstellen
2. **Hintergrund-Synchronisation:** API-Aufrufe im Hintergrund
3. **Preis-Cache:** Letzte bekannte Preise mit Zeitstempel zeigen Aktualitäts-Zustand an
4. **Fehlerbehandlung:** UI zeigt "Offline" oder "Daten veraltet" Indikatoren

#### **Preis-Aktualisierung**

```
User startet App
  ↓
Lokale Stationen + gepufferte Preise laden
  ↓
UI mit lokalen Daten rendern + "Loading..."
  ↓
Im Hintergrund: FuelPriceService.RefreshPricesAsync()
  ↓
API-Aufrufe mit Retry-Strategie
  ↓
Neue Preise in PriceCache speichern
  ↓
UI aktualisieren via PropertyChanged Events
  ↓
Benutzer sieht aktuelle Preise
```

#### **Favoriten-Management**

```
User klickt "Zu Favoriten hinzufügen" auf Station Detail
  ↓
FavoriteGroupService.AddStationToGroupAsync() aufgerufen
  ↓
Existierende Gruppen laden (ggf. Gruppe wählen oder neu erstellen)
  ↓
FavoriteGroupStation Entity erstellen und in DB einfügen
  ↓
StationDetailViewModel.IsFavorite Property aktualisieren
  ↓
Button-Label wechselt zu "Aus Favoriten entfernen"
  ↓
HomeViewModel.FavoriteGroups Collection aktualisieren
  ↓
Startseite zeigt Station in Gruppe
```

### 4.4 API-Integration

#### **Kraftstoff-Preis-API**

- **Endpoint:** `GET /stations?lat={lat}&lon={lon}&radius={radius}&types={fuelTypes}`
- **Response:** JSON mit Stationen und aktuellen Preisen
- **Authentifizierung:** API-Key (aus Settings via Keychain/Credential Locker)
- **Fehlerbehandlung:** Exponential Backoff, Timeout 10s, Max Retries 3

#### **Strompreis-API**

- **Endpoint:** `GET /charging-stations?lat={lat}&lon={lon}&radius={radius}`
- **Response:** Ladestation mit AC/DC/HPC-Preisen
- **Integrierung:** Optional in Station-Details oder als separater Service

#### **HttpClient-Konfiguration**

```csharp
// In MauiProgram.cs oder Infrastructure-Setup
services.AddHttpClient<IFuelPriceService, FuelPriceService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("User-Agent", "Tankradar/1.0");
    })
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, retryCount =>
            TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
```

### 4.5 Sicherheits-Integration

#### **API-Schlüssel-Verwaltung**

- **iOS:** Keychain (`SecureStorage`)
- **Windows (Dev):** Windows Credential Locker oder appsettings.json (Dev-only)
- **Implementierung:** `ISecureStorageService` Abstraction

#### **Verschlüsselte lokale Speicherung**

- **Sensible Daten:** API-Keys, Cloud-Sync-Tokens
- **Implementierung:** Encrypted SQLite (optional via SQLCipher) oder selective encryption

#### **Standort-Datenschutz**

- **Berechtigungen:** LocationPermission Prompt beim ersten Zugriff
- **Nutzung:** Nur bei Bedarf (Search, "In der Nähe")
- **Speicherung:** Keine persistenten GPS-Rohdaten

### 4.6 Backup- und Restore-Logik

```
Backup erstellen:
  ↓
Gesamte SQLite Database (oder SQL-Export) erstellen
  ↓
Zeitstempel + Metadaten hinzufügen
  ↓
Optional verschlüsseln
  ↓
Auf Gerät speichern (optional: zu Cloud)

Backup wiederherstellen:
  ↓
Backup-Datei auswählen
  ↓
Database zurückschreiben
  ↓
App neustarten
  ↓
Zeitstempel prüfen: veraltete Preise erkennen
  ↓
Bei Bedarf: Preis-Aktualisierung triggern
```

### 4.7 Testing-Strategie

#### **Unit Tests**

- **Ziel:** Isolierte Logik-Tests (Berechnungen, Validierungen, Services ohne I/O)
- **Framework:** NUnit oder xUnit
- **Beispiele:**
  - `ConsumptionCalculationTests`: (Menge / Strecke) * 100
  - `PriceCacheValidationTests`: Zeitstempel-basierte Gültigkeit
  - `FavoriteGroupServiceTests`: Gruppen-Operationen mit Mock-Repository

#### **Integration Tests**

- **Ziel:** Services mit echtem oder Mock-Database/API
- **Fixtures:** Test-Database mit Seed-Daten
- **Beispiele:**
  - `FuelPriceApiIntegrationTests`: Http-Aufrufe mit Mock HttpMessageHandler
  - `DatabaseIntegrationTests`: EF Core mit Test-SQLite

#### **E2E Tests (FlaUI – Windows only)**

- **Ziel:** Vollständige User-Flows auf MAUI-Windows-App
- **Framework:** FlaUI
- **Szenarien:**
  - Suche durchführen, Station zu Favoriten hinzufügen
  - Tankvorgang erfassen, Statistiken prüfen
  - Backup erstellen und wiederherstellen
  - Offline-Modus: Daten anzeigen, ohne dass API verfügbar ist

---

## 5. Konfiguration

### 5.1 Anwendungsebenen-Konfiguration

#### **AppSettings (Nutzer-Einstellungen)**

```csharp
public class AppSettings
{
    // Kraftstoff-Sorten und Anzeigereihenfolge
    public List<FuelTypeEnum> SelectedFuelTypes { get; set; }
    public int[] FuelTypeOrder { get; set; }

    // Strom
    public bool EnableElectricityPrices { get; set; }
    public List<ElectricityChargeType> SelectedChargeTypes { get; set; }

    // Standort
    public LocationUsageEnum LocationUsage { get; set; } // Always, OnlyWhenNeeded, Never

    // Anzeige
    public ViewTypeEnum DefaultViewType { get; set; } // List or Map
    public SortOrderEnum DefaultSortOrder { get; set; } // Price, Distance, Name

    // Sicherung & Sync
    public bool CloudSyncEnabled { get; set; }
    public DateTime? LastCloudSyncDate { get; set; }
    public string? CloudSyncUserEmail { get; set; } // (optional)
}
```

#### **Persistierung von Einstellungen**

- **Speicherort:** SQLite AppSettings Table oder native Preferences (MAUI `Preferences`)
- **Service:** `ISettingsService` mit Get/Update-Methoden
- **UI:** `SettingsPage` mit Bindings zu `SettingsViewModel`

### 5.2 Build-Konfiguration und Secrets

#### **API-Keys und Authentifizierung**

- **Development:** `appsettings.Development.json` (Git-ignored)
- **Production:** GitHub Secrets oder Cloud-KMS
- **Zugriff:** Via `IConfiguration` oder `ISecureStorageService`

#### **CI/CD-Umgebungsvariablen**

- `FUEL_PRICE_API_KEY`
- `ELECTRICITY_PRICE_API_KEY`
- `APPLE_DEVELOPER_ACCOUNT` (für iOS-Signing)
- `APPLE_DEVELOPER_PASSWORD`
- `CERTIFICATE_P12_PATH` und `-PASSWORD`

### 5.3 Datenbank-Konfiguration

#### **Connection Strings**

```csharp
// iOS
string iosDbPath = Path.Combine(
    FileSystem.AppDataDirectory, 
    "tankradar.db");

// Windows
string winDbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Tankradar",
    "tankradar.db");

// Im DbContext:
optionsBuilder.UseSqlite($"Filename={dbPath}");
```

#### **Migrations**

- Versionierung über EF Core Migrations
- `dotnet ef migrations add` bei Schema-Änderungen
- Automatische Anwendung beim App-Start (`EnsureDatabaseCreated`, `Migrate`)

### 5.4 Konfigurierbare Feature-Flags

```csharp
public enum FeatureFlags
{
    CloudSyncEnabled,
    RouteSearchEnabled,
    ElectricityChargingEnabled,
    OfflineModeEnforced,
    E2ETestMode
}

// Implementierung: Feature Service mit Config-Provider
services.AddScoped<IFeatureFlagService, ConfigBasedFeatureFlagService>();
```

---

## 6. Offene Fragen und Klärungspunkte

### 6.1 Datenmodell und Persistierung

1. **Externe Station-ID:** Welches Format/Länge für `StationId`? (z. B. eindeutig pro API, oder appweite UUID?)
2. **Preis-Historisierung:** Sollen historische Preise (nicht nur aktuelle + Cache) persistiert werden für Trend-Auswertung?
3. **Photo-Speicherung:** Wo werden Receipt-Fotos gespeichert? (In App-Directory, in DB als BLOB, Cloud?)
4. **Vehicle-Archivierung:** Was passiert mit Einträgen eines gelöschten Fahrzeugs?

### 6.2 API-Integration

5. **Preis-API-Spezifikation:** Welche exakte API (z. B. Tankstelle.de, aral.de, externe Aggregatoren)? Authentifizierungsmethode?
6. **Strompreis-API:** Existiert bereits ein API-Contract, oder muss dieser definiert werden?
7. **Fehlerbehandlung:** Sollen abgelaufene Preise mit rot/orange gekennzeichnet werden? Schwelle (z. B. >24h alt)?
8. **Rate Limiting:** API-Aufrufe pro Minute/Tag? Caching-Strategien?

### 6.3 Benutzerinteraktion und UX

9. **Favoritenverwaltung:** Kann ein Nutzer eine Station mehrfach in verschiedenen Gruppen haben? (Annahme: Ja, via FavoriteGroupStation Join-Tabelle)
10. **Tankstellen-Auswahl:** Werden Stationen in RecordEntry-Dialog via autocomplete (Suche) oder Dropdown angeboten?
11. **Offline-Indikator:** Wo wird der Offline-Status dem Nutzer angezeigt? (z. B. Banner, Icon in Header?)
12. **Route-Suche (optional):** Über welche Library/Service erfolgt Routenplanung? (Google Maps, Apple Maps, Mapbox?)

### 6.4 Sicherheit und Datenschutz

13. **Backup-Verschlüsselung:** Ist eine verschlüsselte Backup-Funktion erforderlich, oder genügt die systemseitige Verschlüsselung?
14. **Cloud-Sync (optional):** Falls implementiert, welcher Service/Provider (z. B. Azure, AWS, eigener Server)?
15. **Daten-Retention:** Wie lange sollen alte Tankbuch-Einträge und Preis-Cache-Daten aufbewahrt werden?

### 6.5 Technische Architektur

16. **MAUI-Version:** Welche MAUI-Version als Minimum (z. B. 8.0+)?
17. **iOS-Deployment-Target:** iOS 14+, 15+, 16+?
18. **Datenbank-Library:** EF Core, SQLiteNet, oder andere?
19. **Kartenbibliothek:** Google Maps, Apple Maps via MKMapView, oder Mapbox?

### 6.6 Testing und Quality Assurance

20. **Test-Abdeckung:** Angestrebte Code-Coverage? (z. B. >70% Core-Logic, >50% overall)
21. **E2E-Testdaten:** Wo werden Test-Daten für FlaUI-Tests verwaltet? (JSON, Test-Database, Fixtures)
22. **Lokale Test-API:** Wird ein Mock/Stub-Server für lokale Integration Tests benötigt?

### 6.7 Deployment und Versioning

23. **Version-Schema:** Semantic Versioning (Major.Minor.Patch)? Beispiele aus CI-Workflows-Dokumentation klären?
24. **TestFlight-Verwaltung:** Wer verwaltet TestFlight-Betaversionen? Automatisch über CI?
25. **App Store Review:** Dokumentation für App Store Review (Privacy, Locations, etc.)?

### 6.8 Lokale Windows-Builds

26. **Artefakt-Speicherung:** Wo werden buildierte Windows-Versionen abgelegt? (Lokaler Pfad, Network Share, Cloud?)
27. **Versionierung von Artefakten:** Benennung (z. B. `TankradarSetup_0.1.0_20260928.exe`)?

---

## 7. Zusammenfassung der Implementierungsschritte (Phasierung)

### Phase 1: Grundstruktur & Datenmodell
- [ ] MAUI-Projekt einrichten (iOS + Windows)
- [ ] Domänen-Modelle definieren (Station, FavoriteGroup, Vehicle, FuelEntry)
- [ ] EF Core DbContext und Migrations setup
- [ ] IServices Interfaces definieren
- [ ] DI-Container konfigurieren

### Phase 2: Basis-Services und API-Integration
- [ ] LocationService (GPS)
- [ ] FuelPriceService (API-Integration)
- [ ] FavoriteGroupService
- [ ] VehicleService
- [ ] SettingsService
- [ ] PriceCacheService (Offline-Unterstützung)

### Phase 3: UI und ViewModels
- [ ] HomePage + FavoriteGroupsViewModel
- [ ] SearchPage + SearchResultsViewModel
- [ ] StationDetailPage + StationDetailViewModel
- [ ] FavoritesPage + Gruppen-Management
- [ ] SettingsPage

### Phase 4: Tankbuch und Statistiken
- [ ] LogbookPage + LogbookViewModel
- [ ] FuelEntryService
- [ ] StatisticsService (Berechnungen)
- [ ] Chart-Integration

### Phase 5: Sicherheit und Offline-Modus
- [ ] SecureStorageService (Keychain/Credential Locker)
- [ ] BackupService
- [ ] Offline-Mode Testing
- [ ] Encryption Layer (optional)

### Phase 6: Testing und Qualitätssicherung
- [ ] Unit Tests (Core-Logic)
- [ ] Integration Tests (Services + Database)
- [ ] E2E Tests (FlaUI – Windows)
- [ ] Lokale Test-Daten

### Phase 7: Deployment und CI/CD
- [ ] GitHub Actions Workflows einrichten
- [ ] iOS Build & TestFlight Setup
- [ ] Windows Installer (MSIX oder Setup.exe)
- [ ] Release-Prozess dokumentieren

---

## 8. Anhang: Glossar technischer Begriffe

| Begriff | Definition |
|---------|-----------|
| **MVVM** | Model-View-ViewModel Architektur-Pattern für UI-basierte Anwendungen |
| **ViewModel** | Präsentations-Logik-Klasse, die Daten und Befehle für die View bereitstellt |
| **Binding** | Deklarative Datenverknüpfung zwischen ViewModel-Properties und UI-Controls |
| **Command** | Benutzeraktionen (Klicks, Gesten) als ausführbare Befehle |
| **Observable Collection** | Collection, die UI benachrichtigt, wenn Items hinzugefügt/entfernt werden |
| **Property Changed** | Event, das ausgelöst wird, wenn eine ViewModel-Property ändert |
| **Dependency Injection** | Architektur-Pattern zum Bereitstellen von Abhängigkeiten (Services) |
| **Repository** | Abstraktions-Pattern für Datenzugriff (CRUD) |
| **DbContext** | EF Core Klasse, die Datenbankzugriff und Migrations verwaltet |
| **Migration** | EF Core Mechanismus zur Versionierung von Database Schema-Änderungen |
| **Retry-Strategie** | Automatisches Wiederholen fehlgeschlagener HTTP-Requests mit Backoff |
| **Preis-Cache** | Lokale Speicherung von letzten abgerufenen Preisen zur Offline-Unterstützung |
| **Offline-First** | Architektur, die zuerst lokale Daten nutzt, dann ggf. synchronisiert |
| **FlaUI** | UI-Automation Framework für Windows Anwendungen (für E2E-Tests) |
| **Keychain** | iOS-Sicherheitsmechanismus zur verschlüsselten Speicherung von Secrets |
| **Credential Locker** | Windows-Sicherheitsmechanismus (nur für Dev; nicht für iOS) |
| **HTTPS** | Sichere HTTP-Kommunikation mit TLS-Verschlüsselung |
| **API-Key** | Authentifizierungstoken für API-Zugriff |
| **Route-Suche** | Optionale Funktion: Anzeige von Tankstellen entlang einer geplanten Route |
| **Provision** | Zusatzinformation zu Station (z. B. "Preis unbestätigt", "Automaten-Tankstelle") |

---

**Ende der Anforderungsübersetzung**

Dokument erstellt: 2026-09-28  
Basierend auf: `issue.md` Tankradar-Projekt  
Status: Vollständig, prüfungsbereit

## Stakeholder-Entscheidungen

- **Ablage funktionierender Windows-Zwischenstände (2026-09-28):** Die Zwischenstände werden strukturiert im Unterordner `review-versions/` des Repository-Roots abgelegt (je Version ein klar benannter und datierter Unterordner, z. B. `review-versions/0.1.0_2026-09-28/`, startfähig, optional mit Changelog). Das Verzeichnis darf nicht in Commits landen und ist daher in `.gitignore` eingetragen.
