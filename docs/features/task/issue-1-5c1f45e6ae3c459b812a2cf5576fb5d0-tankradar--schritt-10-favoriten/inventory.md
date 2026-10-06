# Bestandsaufnahme

- Datenbank: `TankradarDbContext` (SQLite, EF-Core-Migrationen `InitialCreate`, `AddFuelTypeSettings`, `AddPriceCache`); Enums werden als Name gespeichert; Tabelle `Stations` hält Stammdaten aller gesehenen Tankstellen (Suche und Detail speichern sie über `PriceRepository`).
- Detailansicht: `StationDetailPage` / `StationDetailViewModel` (5 Konstruktorparameter, Tests in `StationDetailViewModelTestBase`); Karten-Stil `DetailCard`; Designentwurf zeigt unten die Karte „Favoritengruppen“ (Chips, „Zu weiterer Gruppe hinzufügen“, „Aus Favoriten entfernen“).
- Favoriten-Tab: `FavoritesPage`/`FavoritesViewModel` sind Platzhalter (Schritt 11 baut die Startseite); Navigation über `Shell` mit `Routing.RegisterRoute` (Detail).
- Kacheln: `HttpTileSource` (feste Frist 7 Tage über Dateizeit, Kennung `Tankatlas/0.1 <BundleId>`, Cache unter `<AppData>/tiles`), `TileServerOptions`, `MockTileServer` (ohne Cache-Header), `StationMapView` (Pan/Pinch nur per Gestenerkenner, Quellenangabe als Label).
- Tests: Unit (`Tankradar.Tests.Unit`, Fakes unter `Unit/Support`), Integration (EF mit Testverzeichnis, Migrations-Tests `MigrationsTests_ModelSync`, `PriceCacheMigrationTests_Upgrade`), E2E (FlaUI, `SearchE2ETestBase`, Mock-Server).
- Werkzeuge: `dotnet ef` 10.0.10 verfügbar; Hooks (Enum-Abdeckung, XML-Doku, Format).
