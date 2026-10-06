# Umsetzungsplan

## A. Kachelrestpunkte
1. `HttpTileSource`: neben jeder Kachel eine `.meta`-Datei (ETag, Last-Modified, Ablaufzeit UTC). Ablauf aus `Cache-Control` (`max-age`, `no-store`, `no-cache`), sonst `Expires`, sonst 7 Tage (Rückfall `CacheLifetime`). Abgelaufene Kacheln: bedingte Anfrage (`If-None-Match`/`If-Modified-Since`); 304 verlängert die Gültigkeit, Fehler → veraltete Kachel weiterverwenden. `no-store` → nicht auf Datenträger. Arbeitsspeicher-Treffer tragen ebenfalls die Ablaufzeit.
2. `IAppDataPathProvider.GetCacheDirectory()` (Testmodus: Testverzeichnis; sonst `FileSystem.CacheDirectory`); Kachelspeicher dort.
3. `TileServerOptions.BuildUserAgent(version)` = `Tankatlas/<Version> (+https://github.com/martin-stromberg/Tankradar; <BundleId>)`; Version aus `AppInfo.VersionString`. Gleiches für Nominatim und Preisdienst-Client. Quellenangabe der Karte als Schaltfläche im Linkstil, öffnet `https://www.openstreetmap.org/copyright`.
4. Windows: `MapPointerTracker` (plattformunabhängig, unit-getestet: Ziehen → Verschiebung, Mausrad → Zoomschritte) und Verdrahtung über die Zeigerereignisse des nativen Panels (`#if WINDOWS`, nur Maus).
5. `MockTileServer` um Cache-Header/ETag/304 erweitern; ADR 0005 präzisieren.

## B. Favoriten
1. Datenmodell/Migration `AddFavorites`: `FavoriteGroups` (Id, Name eindeutig ohne Groß-/Kleinschreibung, Description, CreatedUtc), `FavoriteEntries` (Id, GroupId → Gruppe Cascade, StationId → Stations Restrict, Note, Priority als Enum-Name, eindeutig je Gruppe+Station). Bestehende Daten bleiben erhalten (nur neue Tabellen).
2. `IFavoritesService`/`FavoritesService` (EF, `IDbContextFactory`, `IDatabaseInitializer`): Gruppen anlegen/umbenennen/Beschreibung/löschen, Gruppen einer Tankstelle, Hinzufügen (bestehend/neu in einem Schritt), Entfernen aus einer oder mehreren Gruppen, Einträge je Gruppe geordnet nach Priorität (Hoch bis Keine) dann Name, Notiz/Priorität ändern. Ergebniswerte `FavoriteResult`. Validierung: Name getrimmt, 1–40 Zeichen; Beschreibung höchstens 200; Notiz höchstens 500.
3. Detailansicht: `StationFavoritesViewModel` (Karte „Favoritengruppen“ unter den Öffnungszeiten): zugeordnete Gruppen, „Zu Favoriten hinzufügen“ (Inline-Auswahl bestehender Gruppen plus „Neue Gruppe“ mit Namensfeld), „Aus Favoriten entfernen“ (eine Gruppe: direkt; mehrere: Mehrfachauswahl mit Bestätigung). Inline-Bedienung statt nativer Dialoge (UI-Automation).
4. Favoriten-Tab: Gruppenliste (Name, Beschreibung, Anzahl), „Neue Gruppe“, Öffnen → `FavoriteGroupPage` (Route): Umbenennen/Beschreibung bearbeiten, Löschen mit Rückfrage, Einträge mit Notiz/Priorität bearbeiten. Zuordnung nur über die Detailansicht.
5. Texte in `FavoritesTexts`, DI-Registrierung, Shell-Route, ADR 0006, Hilfe `docs/help/Favoriten`, README.

## C. Tests
Unit: Tile-Cache-Header, Pointer-Tracker, UserAgent, Favoriten-Service, ViewModels. Integration: Migration erhält Daten, Persistenz über Neustart, Offline. E2E (FlaUI): Suchen – Details – neue Gruppe – umbenennen – entfernen.

## Offene Punkte
(keine)
