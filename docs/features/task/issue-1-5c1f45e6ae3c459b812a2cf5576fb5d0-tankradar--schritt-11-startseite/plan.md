# Umsetzungsplan: Schritt 11 – Startseite mit Favoritengruppen und Bereich „In der Nähe“

## Übersicht

Die Seite „Favoriten“ (`FavoritesPage`/`FavoritesViewModel`) wird zur Startseite ausgebaut: je Favoritengruppe die Tankstellen mit Name, Entfernung (nur wenn ein Standort ermittelt werden darf), Preiszeilen der gewählten Sorten (Alter, Amber ab 60 Minuten) und Hinweisen; Aktualisierung beim Erscheinen und auf Wunsch, Offline-Stand mit Kopfzeilenhinweis, automatische Aktualisierung nach Wiederverbindung; Antippen öffnet die Detailansicht. Ein neuer, per Einstellung (`AppSettings.ShowNearby`) aktivierbarer Bereich „In der Nähe“ listet filterbar Tankstellen um den Standort, rein informativ. Zusätzlich öffnet die Gruppenansicht Einträge in der Detailansicht (ADR 0006 aktualisieren) und drei Restpunkte aus Schritt 10 werden behoben (`Age` und `304` im Kachel-Cache, „bereits zugeordnet“ statt „doppelter Name“). Betroffen: Favoriten, Preisdienst/Preis-Cache, Standort, Einstellungen (inkl. Migration), Kachel-Cache, Tests, Mock-Server, Dokumentation.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Preisaktualisierung der Favoriten | Neuer Abruf nach Kennungen: `ITankerkoenigClient.GetPricesAsync` (Tankerkönig `prices.php`, höchstens 10 Kennungen je Anfrage, mehrere Anfragen nacheinander über die vorhandene Drosselung) und `IFuelPriceService.RefreshStationsAsync` (Service Layer) statt je Favorit `detail.php` oder einer Umkreissuche | Eine Anfrage je 10 Favoriten schont das Kontingent (Entscheidung „Kraftstoffpreis-Quelle“: Abrufe drosseln); `detail.php` je Station würde bei 1 s Drosselung die Startseite spürbar verzögern; Umkreissuche liefert nur Stationen rund um eine Position, nicht die Favoriten |
| Lokale Daten für Favoriten | `IPriceRepository.GetStationsAsync(ids)` (Repository) liefert Stammdaten, Position und jüngste Preise; `SavePricesAsync` speichert nur Preiszeilen für lokal bekannte Tankstellen und lässt Stammdaten unverändert | `prices.php` enthält keine Stammdaten; `SaveAsync` würde sie überschreiben. Favoriten sind per Fremdschlüssel immer lokal bekannt |
| Aufbereitung der Gruppenübersicht | Reine, statische `HomeOverviewBuilder` (Transaction Script, wie `StationResultBuilder`) erzeugt `HomeGroup`/`HomeStation`; Tankstellenzeile wird über die neu öffentliche `StationResultBuilder.BuildItem` als `StationListItem` erzeugt | Wiederverwendung von Preiszeilen, Alter, Amber, Hinweisen und Entfernungsformat; gut ohne UI testbar (Anforderung: „Tests decken die Aufbereitung der Gruppenübersicht ab“) |
| Reihenfolge in der Gruppe | Wie bisher: Priorität (hoch zuerst), dann Name; keine Preissortierung | ADR 0006 legt die Reihenfolge fest; der Anwender pflegt die Priorität bewusst |
| Daten für alle Gruppen | `IFavoritesService.GetOverviewAsync` liefert alle Gruppen samt Einträgen in einem Zugriff (`FavoriteGroupOverview`) | Vermeidet N+1-Zugriffe je Gruppe und Inkonsistenzen zwischen mehreren Lesevorgängen |
| Standortermittlung | `ILocationService` wird nur aufgerufen, wenn `GpsUsage.AllowsLocation()` gilt **und** ein Bedarf besteht (mindestens eine Favoriten-Tankstelle oder „In der Nähe“ aktiv); die Position liegt nur im Speicher des ViewModels und wird in `OnDisappearing` verworfen; nie protokolliert oder gespeichert | Entscheidung „Standort nur bei Bedarf“ (kein Berechtigungsdialog auf leerer Startseite) und „Standortdaten werden nicht gespeichert“ |
| Entfernung der Favoriten | Lokal berechnet aus Standort und gespeicherter Position (`PriceRepository.DistanceKm`) über neues `StationInfo.WithDistance` | Der Abruf nach Kennungen liefert keine Entfernung; keine zusätzliche Anfrage |
| Wiederverbindung | Wie `StationDetailViewModel`: Abmeldung/Anmeldung an `IConnectionMonitor` in `OnAppearing`/`OnDisappearing`; bei `ConnectionRestored` Aktualisierung nur, wenn der letzte Abruf `OfflineFallback` war oder eine Preiszeile veraltet (`IsStale`) ist | Gleiches, abgenommenes Muster; vermeidet unnötige Abrufe |
| Aktualisierungsfrist | `RefreshStationsAsync(ids, force)` überspringt Tankstellen, die innerhalb von `PriceApiOptions.CacheLifetime` (5 min) erfolgreich aktualisiert wurden, außer bei `force` (Schaltfläche). Ein Abruf mit Fehlerfallback setzt keine Frische, daher greift nach Wiederverbindung der Abruf wieder | Beim Erscheinen der Seite nicht bei jedem Tabwechsel abrufen (Drosselung, Nutzungsbedingungen), „auf Wunsch“ aber immer |
| Bereich „In der Nähe“ | Eigenes `NearbySectionViewModel` (von `FavoritesViewModel` gehalten) mit `IFuelPriceService.SearchNearbyAsync` (fester Radius `SearchRadius.Default` = 5 km), Aufbereitung mit `StationResultBuilder.Build` (`ResultSortOrder.Distance`), höchstens 10 Einträge, Filterchips `FuelFilterOptionViewModel` („Alle“ + gewählte Sorten) wie in `MapViewModel` | Wiederverwendung der Umkreissuche samt Cache und Offline-Fallback; fester Radius/Anzahl halten den Bereich eine reine Übersicht (die Karte bietet Radius und Paging) |
| Einträge von „In der Nähe“ | Antippen öffnet die Detailansicht; keine Favoritenaktion, kein Hinzufügen-Knopf im Bereich | Information bleibt möglich; Favoritenaktionen bleiben nach ADR 0006 ausschließlich in der Detailansicht |
| Tankstellenkachel | Ein wiederverwendbares Steuerelement `StationTile` (`Views/Controls`) für Gruppen und „In der Nähe“, abgeleitet aus Stil und Preiszeilen der Ergebnisliste in `MapPage`/`StationDetailPage` (Preis groß mit Nachkommastelle, Amber bei `IsStale`, Hinweise) | Ein UI-Muster, eine Pflege; Designentwurf `startseite_favoriten_gruppen` |
| Gruppenkarten | Einklappbar (`HomeGroupViewModel.IsExpanded`, Standard aufgeklappt, Umschalter `Favorites.Group.Toggle`), Kopf mit Name, Beschreibung, Anzahl, „Gruppe öffnen“ | Designentwurf zeigt einklappbare Gruppen; Standard aufgeklappt hält vorhandene AutomationIds und Abläufe der E2E-Tests gültig |
| Öffnen der Detailansicht | Schaltfläche (`Invoke`-fähig) plus Antippen der Kachel; ein Schutz gegen Doppelöffnen wie `MapViewModel.OpenStation`. Aus der Gruppenansicht erhält `FavoriteGroupViewModel` zusätzlich `IStationNavigator`; übergeben wird ein `StationListItem` mit Kennung, Name, Adresse (Preise lädt die Detailansicht selbst) | Bestehendes Navigationsmuster (`ShellStationNavigator`); keine Eingabe interner Kennungen durch Anwender (Auswahl erfolgt durch Antippen benannter Tankstellen) |
| Restpunkt `Age` | `TileCachePolicy.Evaluate` erhält den optionalen Parameter `age` (aus `HttpResponseHeaders.Age`); Frische = Lebensdauer (`max-age` bzw. `Expires − Date` bzw. Rückfall) abzüglich `Age`, mindestens 0; bei `no-cache`/`no-store` ohne Wirkung | RFC 9111 (verbleibende Frische = Lebensdauer − aktuelles Alter) |
| Restpunkt `304` | `TileFreshness` führt `Lifetime` (Lebensdauer vor Abzug von `Age`) und `IsExplicit` (aus `Cache-Control`/`Expires` abgeleitet); `TileMetadata` speichert `Lifetime` (optional, abwärtskompatibel). Eine `304` ohne eigene Angaben verlängert um die gespeicherte `Lifetime` (abzüglich ihres `Age`), bei Altbestand ohne `Lifetime` um `TileServerOptions.CacheLifetime` | RFC 9111 4.3.4: Die gespeicherten Angaben gelten weiter, wenn die 304 keine neuen nennt; `Lifetime` statt `ExpiresUtc − Zeitpunkt`, da ein Speicherzeitpunkt sonst zusätzlich nötig wäre |
| Restpunkt Zuordnung | `FavoritesService.SaveAsync` erhält das bei Verletzung des eindeutigen Index zurückzugebende Ergebnis vom Aufrufer (`AddStationAsync` → `AlreadyMember`, Anlegen/Umbenennen → `DuplicateName`) | Eindeutig und unabhängig vom Text der SQLite-Fehlermeldung; keine Auswertung von Meldungstexten |
| Einstellung „In der Nähe“ | Ein/Aus-Schalter (`Switch`, wie die Spritsorten-Schalter `FuelTypeItemViewModel`), neue Spalte `UserSettings.ShowNearby`, Standard aus; Hinweistext, wenn `GpsUsage.Never` gewählt ist („wirkt nur, wenn die Standortnutzung nicht „Nie“ ist“) | Bestehendes Schaltermuster; Sichtbarkeit ergibt sich aus Schalter und `GpsUsage` (Anforderung), keine eigene Zusatzlogik |
| Designabweichungen | Neuer ADR 0007: ohne Tages-Empfehlung, Routenknöpfe, Tiefstpreis-/„Nah“-Kennzeichen, „Sortieren“, Strom/Ladestationen (ADR 0001), ohne Kopfzeile „Vor X Min. aktualisiert“ (Alter steht je Preis); Filterchips nur in „In der Nähe“ | Entscheidung „Designentwurf verbindlich: jede Abweichung festhalten“; Elemente ohne Entsprechung in der Anforderung sind nicht angefordert |
| Offene Fragen | Keine an den Anwender; die Entscheidungen oben sind begründet getroffen | Der Anwender ist nicht verfügbar; Annahmen stehen in ADR 0007 |

## Programmabläufe

### Startseite öffnen (`FavoritesViewModel.OnAppearing`)

1. `OnAppearing` meldet sich an `IConnectionMonitor.ConnectionChanged` und `ConnectionRestored` an (einmalig) und setzt `IsOffline` aus `IConnectionMonitor.IsOnline`.
2. `LoadAsync` lädt `AppSettings` (`ISettingsService.LoadAsync`) und `IFavoritesService.GetOverviewAsync`. Gibt es keine Gruppen, erscheint der Hinweis `FavoritesTexts.NoGroups`; es folgt kein Preis- und kein Standortabruf (Ausnahme „In der Nähe“, siehe unten).
3. Sofortanzeige: `IFuelPriceService.GetKnownStationsAsync(ids)` liefert die lokal bekannten Tankstellen mit zuletzt bekannten Preisen; `HomeOverviewBuilder.Build` erzeugt `Groups` (ohne Entfernung, solange keine Position vorliegt).
4. `RefreshAsync(force: false)`: bricht einen laufenden Abruf ab (`CancellationTokenSource` je Abruf), bestimmt die Position (siehe „Standort bestimmen“), ruft `IFuelPriceService.RefreshStationsAsync(ids, force, token)`, baut `Groups` mit `HomeOverviewBuilder` neu auf, setzt `IsOffline` aus `Source == OfflineFallback` oder `!IsOnline`, `StatusMessage` aus `SearchTexts.GetFailureMessage(failure, hasData)`.
5. Ist „In der Nähe“ sichtbar (`AppSettings.ShowNearby` und `GpsUsage.AllowsLocation()`), lädt `NearbySectionViewModel.LoadAsync(position)` den Bereich.
6. `OnDisappearing` meldet die Ereignisse ab, bricht Abrufe ab und verwirft die Position.
7. `IFavoritesService.Changed` (Zuordnungen, Umbenennen, Löschen aus anderen Ansichten) löst weiterhin `LoadAsync` aus (vorhandene `FavoritesChangeSubscription`); ein Neuaufbau ruft den Abruf nicht erneut auf, sondern verwendet die lokalen Preise.

Beteiligte Klassen/Komponenten: `FavoritesViewModel`, `FavoritesPage`, `HomeOverviewBuilder`, `IFavoritesService`, `IFuelPriceService`, `ISettingsService`, `IConnectionMonitor`, `NearbySectionViewModel`.

### Standort bestimmen (nur bei Bedarf)

1. Nur wenn `settings.GpsUsage.AllowsLocation()` und (mindestens eine Favoriten-Tankstelle vorhanden oder „In der Nähe“ aktiv).
2. `ILocationService.GetCurrentLocationAsync(settings.GpsUsage)` genau einmal je Abruf; `LocationStatus.Available` liefert die `GeoPosition`, sonst keine Entfernungen (keine Fehlermeldung auf der Startseite; „In der Nähe“ zeigt die Meldung `SearchTexts.GetLocationMessage`).
3. Bei `GpsUsage.Never` kein Aufruf und keine Entfernungsangaben; „In der Nähe“ ist unsichtbar.
4. Die Position wird weder gespeichert noch protokolliert (Protokolle nennen höchstens den Ausnahmetyp).

Beteiligte Klassen/Komponenten: `FavoritesViewModel`, `ILocationService`, `AppSettings`, `GpsUsageExtensions`.

### Preise nach Kennungen aktualisieren (`FuelPriceService.RefreshStationsAsync`)

1. Kennungen prüfen (UUID, höchstens 100 verschieden); ungültige Kennungen werden ausgelassen.
2. Ist nicht `force`, werden Tankstellen übersprungen, deren erfolgreicher Abruf innerhalb von `PriceApiOptions.CacheLifetime` liegt (Speicher im Dienst, `ConcurrentDictionary`). Bleiben keine übrig: Ergebnis aus `IPriceRepository.GetStationsAsync` mit `PriceDataSource.Cache`.
3. Ist `IConnectionMonitor.IsOnline` falsch: lokaler Stand mit `PriceDataSource.OfflineFallback`, `PriceFailure.Offline`.
4. Sonst `ITankerkoenigClient.GetPricesAsync(restIds)` in Gruppen zu 10 Kennungen (Drosselung, Wiederholung wie die übrigen Abrufe). Antwort: je Kennung Sorten (`e5`, `e10`, `diesel`, nur Zahlen > 0); Kennungen mit Status „no prices“ oder ohne Preise liefern keine Zeilen (der alte Preis bleibt mit seinem Alter stehen).
5. `IPriceRepository.SavePricesAsync` speichert die Preiszeilen für lokal bekannte Tankstellen (Speicherfehler werden wie in `TrySaveAsync` protokolliert und nicht weitergereicht); Frischezeitpunkte werden gesetzt.
6. Ergebnis: Tankstellen aus `GetStationsAsync` mit `PriceDataSource.Live`. Bei `PriceApiException`: lokaler Stand mit `OfflineFallback` und `ex.Failure`; Teilerfolge (erste Gruppe ok, zweite fehlgeschlagen) liefern `Live`-Daten der erfolgreichen Gruppen und den Fehler als `Failure`.

Beteiligte Klassen/Komponenten: `FuelPriceService`, `ITankerkoenigClient`/`TankerkoenigClient`, `IPriceRepository`/`PriceRepository`, `IConnectionMonitor`, `PriceApiOptions`.

### Aufbereitung der Gruppenübersicht (`HomeOverviewBuilder.Build`)

1. Eingaben: `IReadOnlyList<FavoriteGroupOverview>` (Gruppe plus Einträge in der Reihenfolge des Dienstes), `IReadOnlyDictionary<string, StationInfo>`, `IReadOnlyList<FuelTypeSelection>`, `GeoPosition?`, `nowUtc`.
2. Je Eintrag: `StationInfo` zur Kennung suchen; mit Position `WithDistance(PriceRepository.DistanceKm(...))`, sonst ohne Entfernung (auch bei Position (0, 0) ohne Angabe).
3. `StationResultBuilder.BuildItem(station, fuelTypes, nowUtc)` erzeugt das `StationListItem` (Preiszeilen der gewählten Sorten in der Einstellungs-Reihenfolge, Alter „vor X Min.“, `IsStale` ab 60 Minuten, `HasUnconfirmedPrice`, `IsAutomatedStation`).
4. `HomeStation` bündelt `StationListItem`, Notiz und Priorität; hat die Tankstelle keine Preise der gewählten Sorten, setzt `HasPrices` falsch (Anzeige „Keine Preise bekannt“).
5. `HomeGroup` trägt Gruppenkennung, Name, Beschreibung, Anzahl (`FavoritesTexts.FormatStationCount`) und `Stations`; leere Gruppe zeigt „Noch keine Tankstellen“.

Beteiligte Klassen/Komponenten: `HomeOverviewBuilder`, `HomeGroup`, `HomeStation`, `StationResultBuilder`, `StationInfo`, `PriceRepository.DistanceKm`.

### Wiederverbindung

1. `ConnectionRestored` (nur während die Seite angezeigt wird): `UpdateOfflineState`.
2. Hat der letzte Abruf `PriceDataSource.OfflineFallback` geliefert oder enthält eine Gruppentankstelle eine veraltete Preiszeile (`HasStalePrice`), startet `RefreshAsync(force: false)`; sonst kein Abruf.
3. Nach erfolgreichem Abruf entfällt der Offline-Hinweis (`IsOffline` falsch), die Preise zeigen neue Zeiten.
4. Ist „In der Nähe“ sichtbar und das letzte Ergebnis ein Offline-Stand, wird es ebenfalls neu geladen.
5. `ConnectionChanged` (offline) zeigt den Hinweis sofort.

Beteiligte Klassen/Komponenten: `FavoritesViewModel`, `NearbySectionViewModel`, `IConnectionMonitor`.

### Aktualisierung auf Wunsch

1. Schaltfläche „Preise aktualisieren“ (`Favorites.Refresh`, nur bedienbar, wenn online und nicht beschäftigt: `CanRefresh`) startet `RefreshAsync(force: true)` und lädt „In der Nähe“ neu.

### Bereich „In der Nähe“ (`NearbySectionViewModel`)

1. Sichtbar (`IsVisible`) nur wenn `AppSettings.ShowNearby` und `GpsUsage.AllowsLocation()`.
2. Ist die Position nicht verfügbar, zeigt der Bereich die passende Meldung (`SearchTexts.GetLocationMessage`) statt Einträgen.
3. Sonst `IFuelPriceService.SearchNearbyAsync(new StationSearchQuery(lat, lon, SearchRadius.Default, gewählte Sorten))`; bei Fehler/Offline die zuletzt bekannten Stationen (Fallback des Dienstes) und Offline-Hinweis in der Kopfzeile der Seite.
4. Filterchips „Alle“ und je gewählte Sorte (`FuelFilterOptionViewModel`); Auswahl ändert die Liste ohne neuen Abruf (`StationResultBuilder.Build` mit Filter, `ResultSortOrder.Distance`, `Take(10)`).
5. Einträge sind `HomeStation` ohne Favoritenaktion; Antippen öffnet die Detailansicht (`IStationNavigator`). Ohne Treffer: Hinweis „Keine Tankstellen in der Nähe“.

Beteiligte Klassen/Komponenten: `NearbySectionViewModel`, `FuelFilterOptionViewModel`, `StationResultBuilder`, `IFuelPriceService`, `IStationNavigator`.

### Einstellung „In der Nähe“

1. `SettingsViewModel` führt `ShowNearby` (bool, Schalter `Settings.Nearby.Switch`); `LoadAsync` setzt den Wert still (`_isLoading`), eine Änderung speichert sofort (`SaveAsync` bildet `AppSettings` mit dem neuen Feld).
2. `SettingsService` liest/schreibt `UserSettingsEntity.ShowNearby`; fehlender Wert ergibt `false`.
3. Hinweis `Settings.Nearby.Hint` erscheint zusätzlich, wenn `GpsUsage.Never` gewählt ist.

### Gruppenansicht → Detailansicht

1. `FavoriteEntryItemViewModel.OpenCommand` ruft den von `FavoriteGroupViewModel` übergebenen Rückruf.
2. `FavoriteGroupViewModel.OpenStation` erzeugt ein `StationListItem` (Kennung, Name, Adresse; ohne Preise/Entfernung) und ruft `IStationNavigator.OpenDetailAsync`; Doppelöffnen wird verhindert, ein Fehler zeigt `DetailTexts.NotAvailable` in der Statusmeldung.
3. Die Detailansicht lädt Preise und Details selbst (`StationDetailViewModel`).

### Restpunkte aus Schritt 10

- **`Age`:** `HttpTileSource.EvaluateFreshness` liest `response.Headers.Age` und übergibt es an `TileCachePolicy.Evaluate`; die Gültigkeit wird um `Age` gekürzt (mindestens 0), z. B. `max-age=600` und `Age: 600` ergibt „sofort abgelaufen“.
- **`304`:** Bei `304` ohne eigene Caching-Angaben (`IsExplicit` falsch) gilt `Lifetime` aus den gespeicherten `TileMetadata` (abzüglich `Age` der 304), sonst `TileServerOptions.CacheLifetime`; mit eigenen Angaben gelten diese (bisheriges Verhalten). Beim Speichern einer Antwort wird `Lifetime` in die Metadaten geschrieben und bei einer 304 mit eigenen Angaben aktualisiert.
- **Zuordnung:** Bei gleichzeitigem `AddStationAsync` derselben Tankstelle zur selben Gruppe liefert die zweite Speicherung `FavoriteResult.AlreadyMember`; Änderungen an Namen liefern weiterhin `DuplicateName`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `HomeOverviewBuilder` | statische Klasse (`Services/Search` oder `Services/Favorites`) | Aufbereitung Gruppen + Tankstellen + Preise zu `HomeGroup` |
| `HomeGroup`, `HomeStation` | Datenmodellklassen (records, `Models/Favorites`) | Anzeigemodell einer Gruppe bzw. Tankstelle auf der Startseite (`HomeStation`: `Item` (`StationListItem`), `Note`, `Priority`, `HasPrices`, `HasNote`) |
| `FavoriteGroupOverview` | Datenmodellklasse (record) | Gruppe (`FavoriteGroup`) mit ihren `FavoriteEntry`-Einträgen |
| `StationRefreshResult` | Datenmodellklasse (record, `Models/Pricing`) | `Stations`, `Source` (`PriceDataSource`), `Failure` (`PriceFailure`) für den Abruf nach Kennungen |
| `HomeGroupViewModel` | ViewModel | Gruppenkarte: Name, Beschreibung, Anzahl, `IsExpanded`, `ToggleCommand`, `OpenCommand`, `Stations` |
| `HomeStationViewModel` | ViewModel | Tankstellenkachel: `Item`, `OpenCommand`, Notiz/Priorität (nur Gruppen) |
| `NearbySectionViewModel` | ViewModel | Bereich „In der Nähe“: Sichtbarkeit, Filter, Einträge, Meldungen |
| `StationTile` | Steuerelement (`Views/Controls/StationTile.xaml(.cs)`) | Wiederverwendbare Kachel mit Name, Entfernung, Preiszeilen, Hinweisen, Öffnen-Schaltfläche |
| ADR `docs/adr/0007-home-page-decisions.md` | Dokument | Entscheidungen und Designabweichungen der Startseite |
| `docs/help/Startseite/` (`index.md`, `beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md`) | Dokumentation | Anwender- und Technikdokumentation |
| Migration `AddNearbySetting` (+ Designer) | Migration | Neue Spalte, siehe Datenbankmigrationen |

## Änderungen an bestehenden Klassen

### `AppSettings` (Datenmodellklasse)

- **Neue Eigenschaften:** `ShowNearby` (`bool`) — Bereich „In der Nähe“ aktiv.
- **Geänderte Methoden:** Konstruktor erhält den optionalen fünften Parameter `showNearby = false` (bestehende Aufrufe mit vier Argumenten bleiben gültig); `CreateDefault` setzt `false`.

### `UserSettingsEntity` (Entität)

- **Neue Eigenschaften:** `ShowNearby` (`bool`, Standard `false`).
- **Geänderte Methoden:** `Update` schreibt `ShowNearby`.

### `SettingsService` (Dienst)

- **Geänderte Methoden:** `LoadAsync` übernimmt `user?.ShowNearby ?? false`; `SaveAsync` über `Update`.

### `SettingsViewModel` / `SettingsPage` (ViewModel/View)

- **Neue Eigenschaften:** `ShowNearby` (`bool`, Schalter), `NearbyHeading`, `NearbyHint`, `NearbyGpsHint`, `ShowNearbyGpsHint`.
- **Geänderte Methoden:** `LoadAsync`, `SaveAsync`; Änderung der Standortwahl aktualisiert `ShowNearbyGpsHint`.
- **View:** Abschnitt „In der Nähe“ mit `Switch` (`Settings.Nearby.Switch`), Hinweis (`Settings.Nearby.Hint`, `Settings.Nearby.GpsHint`), Screenreader-Beschreibung.

### `SettingsTexts` / `FavoritesTexts` (Textklassen)

- **Neue Texte:** Einstellungen: Überschrift, Beschreibung des Schalters, Hinweis bei „Nie“. Startseite: Schaltfläche „Preise aktualisieren“, „Keine Preise bekannt“, „Noch keine Tankstellen“, „Gruppe ein-/ausklappen“, „Details öffnen“, Überschrift „In der Nähe“, „Keine Tankstellen in der Nähe“, Filtertexte „Alle“, Beschreibungen; `NoGroups` bleibt (Hinweis, wie man Favoriten anlegt).

### `IFavoritesService` / `FavoritesService` (Dienst)

- **Neue Methoden:** `GetOverviewAsync` — alle Gruppen mit Einträgen (`IReadOnlyList<FavoriteGroupOverview>`, Gruppen nach Namen, Einträge nach Priorität/Name), ein Datenbankzugriff.
- **Geänderte Methoden:** `SaveAsync` erhält den Parameter `duplicateResult`; `AddStationAsync` übergibt `AlreadyMember`, `CreateAsync`/`UpdateGroupAsync` `DuplicateName`.

### `ITankerkoenigClient` / `TankerkoenigClient`

- **Neue Methoden:** `GetPricesAsync(IReadOnlyCollection<string> stationIds, CancellationToken)` — `prices.php?ids=...` (höchstens 10, UUID-Prüfung vor der Anfrage), liefert `IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>>`; `InvalidResponse`, wenn `prices` fehlt; gleiche Fehlerabbildung wie `SearchAsync`.

### `IPriceRepository` / `PriceRepository`

- **Neue Methoden:** `GetStationsAsync(IReadOnlyCollection<string>)` — `StationInfo` mit Stammdaten, Position, Detailangaben und jüngsten Preisen (Reihenfolge der Eingabe); `SavePricesAsync(IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>>)` — hängt Preiszeilen für bekannte Tankstellen an, ignoriert unbekannte Kennungen.

### `IFuelPriceService` / `FuelPriceService`

- **Neue Methoden:** `RefreshStationsAsync(IReadOnlyCollection<string> stationIds, bool force, CancellationToken)` → `StationRefreshResult` (siehe Ablauf); `GetKnownStationsAsync(IReadOnlyCollection<string>, CancellationToken)` → `IReadOnlyList<StationInfo>` (nur lokal).

### `StationInfo` (Datenmodellklasse)

- **Neue Methoden:** `WithDistance(double? distanceKm)` — Kopie mit Entfernung.

### `StationResultBuilder` (statische Klasse)

- **Neue Methoden:** öffentliche `BuildItem(StationInfo, IReadOnlyList<FuelTypeSelection>, DateTime nowUtc)` (bisher privat `CreateItem`); `Build` nutzt sie. Verhalten unverändert.

### `FavoritesViewModel` / `FavoritesPage` (ViewModel/View, Startseite)

- **Neue Abhängigkeiten:** `ISettingsService`, `IFuelPriceService`, `ILocationService`, `IConnectionMonitor`, `IStationNavigator`, `TimeProvider`; `NearbySectionViewModel` wird als Eigenschaft `Nearby` bereitgestellt.
- **Neue Eigenschaften:** `Groups` (nun `IReadOnlyList<HomeGroupViewModel>`), `IsOffline`, `OfflineMessage`, `CanRefresh`, `RefreshCommand`, `Nearby`, `LastTask` bleibt.
- **Geänderte Methoden:** `OnAppearing`/`LoadAsync` (siehe Ablauf), neu `OnDisappearing`, `RefreshAsync`, `OnConnectionRestored`, `OnConnectionChanged`. Gruppenanlegen (`ShowCreateCommand` …) und `FavoritesChangeSubscription` bleiben.
- **View:** Kopfzeile mit Offline-Hinweis (`Favorites.OfflineBanner`), Schaltfläche `Favorites.Refresh`, Gruppenkarten mit `StationTile` (AutomationIds: `Favorites.Group.Name|Description|Count|Open|Toggle` bleiben bzw. kommen hinzu; `Favorites.Station.Name|Distance|Open|Hint.Unconfirmed|Hint.Automated|NoPrices`, `Favorites.Station.Price.<Sorte>` und `Favorites.Station.Age.<Sorte>`), Bereich `Nearby.Section` mit Filterchips `Nearby.Filter.All|<Sorte>`, Einträgen `Nearby.Station.*`, Meldung `Nearby.Status`. Eine `ScrollView` bleibt.

### `FavoriteGroupViewModel` / `FavoriteEntryItemViewModel` / `FavoriteGroupPage`

- **Neue Abhängigkeit:** `IStationNavigator` im Konstruktor von `FavoriteGroupViewModel`.
- **Neue Eigenschaften/Methoden:** `FavoriteEntryItemViewModel.OpenCommand`; `FavoriteGroupViewModel.OpenStation`.
- **View:** Schaltfläche `FavoriteGroup.Entry.Open` („Details öffnen“) je Eintrag; Name tippbar.

### `MauiProgram` (Konfiguration)

- Keine neuen Registrierungen außer `NearbySectionViewModel` (transient) falls per DI bezogen; `FavoritesViewModel` bekommt neue Abhängigkeiten über DI (bereits registriert).

### `TileCachePolicy` / `TileFreshness` / `TileMetadata` / `HttpTileSource`

- **Neue Eigenschaften:** `TileFreshness.Lifetime` (`TimeSpan?`), `TileFreshness.IsExplicit` (`bool`); `TileMetadata.Lifetime` (`TimeSpan?`, optional, Standard `null`).
- **Geänderte Methoden:** `TileCachePolicy.Evaluate` erhält `TimeSpan? age = null`; `HttpTileSource.EvaluateFreshness` übergibt `response.Headers.Age`; `DownloadAsync`/`GetTileAsync` nutzen `Lifetime` für `304` ohne Angaben; `CreateMetadata` speichert `Lifetime`.

### `MockTankerkoenigServer` (Testunterstützung, `src/TestSupport`)

- **Neue Eigenschaften/Methoden:** Endpunkt `prices.php` (Format wie das echte: `{"ok":true,"prices":{"<id>":{"status":"open","e5":…,"e10":…,"diesel":…}}}`, `false` für fehlende Sorten, unbekannte Kennung `"status":"no prices"`, mehr als 10 Kennungen oder fehlendes `ids` → `ok:false`), `PricesRequests`, `LastPricesIds`, `SetPrices(stationId, e5, e10, diesel)` (überschreibt nur `prices.php`); `TotalRequests` zählt `prices.php` mit.

### Testhilfen (Unit)

- `FakeFuelPriceService`: Ergebnisse und Anfragen für `RefreshStationsAsync`/`GetKnownStationsAsync`; `StubTankerkoenigClient`: `GetPricesAsync`; `InMemoryDbContextFactory`: optionale `ISaveChangesInterceptor`; `FakeFavoriteGroupNavigator`/`FakeStationNavigator` unverändert nutzbar; `FakeSettingsService` unterstützt `ShowNearby` über `AppSettings`.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddNearbySetting` | `UserSettings.ShowNearby` | Neue Spalte `INTEGER NOT NULL DEFAULT 0`; bestehende Zeilen erhalten `false`. Modell-Snapshot aktualisieren. Upgrade-Test auf einer echten Datei (Skill `entityframework-database`): Datenbank im Stand von `AddFavorites` mit Einstellungen, Tankstellen, Preisen und Favoriten bleibt vollständig erhalten |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `GetPricesAsync(stationIds)` | Jede Kennung muss eine UUID sein, 1 bis 10 Kennungen je Anfrage | `ArgumentException` vor der Anfrage |
| `RefreshStationsAsync(stationIds)` | Ungültige/doppelte Kennungen werden ausgelassen, leere Liste ergibt leeres Ergebnis ohne Abruf | — |
| `AppSettings.Validate` | `ShowNearby` ist ein `bool`, keine zusätzliche Regel | — |
| Preise aus `prices.php` | Nur Zahlen > 0 werden übernommen (wie `AddPrice`) | Wert ignoriert |
| `Age`-Header | Negative oder fehlende Werte gelten als 0 | — |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `AppSettings.ShowNearby` / `UserSettings.ShowNearby` | `bool` | `false` | Bereich „In der Nähe“ aktivieren |
| `NearbySectionViewModel.MaxItems` (Konstante) | `int` | 10 | Anzahl Einträge, kein Benutzerwert |
| Radius „In der Nähe“ | `SearchRadius.Default` | 5 km | fester Wert, kein Benutzerwert |

## Seiteneffekte und Risiken

- **Konstruktoren:** `FavoritesViewModel` und `FavoriteGroupViewModel` erhalten neue Abhängigkeiten; alle Tests und Testbasen, die sie erzeugen, werden angepasst.
- **Schnittstellen:** `IFuelPriceService`, `IPriceRepository`, `ITankerkoenigClient`, `IFavoritesService` wachsen; alle Fakes/Stubs (Unit/Integration) müssen die neuen Mitglieder implementieren, sonst Kompilierfehler.
- **`TileFreshness`/`TileMetadata`:** erweiterte Datensätze; bestehende `.meta`-Dateien ohne `Lifetime` müssen weiter gelesen werden (JSON ohne Feld ergibt `null`).
- **Preis-Cache:** `SavePricesAsync` hängt je Abruf Zeilen an (wie bisher `SaveAsync`); bei vielen Favoriten wächst `PriceEntries` stärker. Bestehendes Muster, kein neues Aufräumen in diesem Schritt.
- **Kontingent/Drosselung:** Aktualisierung beim Erscheinen wird durch die 5-Minuten-Frische begrenzt; Schaltfläche erzwingt.
- **Standort/Datenschutz:** Position nur im Speicher, nie in Protokollen; Berechtigungsdialog nur bei Bedarf. Datenschutztests (`SearchPrivacyTests_*`) um die Startseite erweitern.
- **Teststabilität:** E2E-Tests der Favoriten nutzen `Favorites.Group.Name` usw.; die Ids bleiben, die Gruppen sind standardmäßig aufgeklappt. Eine neue Statusmeldung oder Schaltflächen können Reihenfolgen von `FindAllOrdered` beeinflussen.
- **Leistung:** Eine Gruppenübersicht mit vielen Favoriten baut `BindableLayout` ohne Virtualisierung (wie die bisherige Liste); für den erwarteten Umfang (einstellige bis niedrige zweistellige Zahl) ausreichend.
- **Keine produktiven Endpunkte in Tests**, API-Schlüssel wird nie ausgelesen oder protokolliert.
- **iOS:** reine MAUI-Mittel (Switch, BindableLayout, Border); iOS-Compile-Prüfung der CI (`local-ci.ps1`) bleibt Teil des Gates.

## Umsetzungsreihenfolge

1. **Restpunkt Zuordnung (`FavoritesService.SaveAsync`)**
   - Voraussetzungen: Keine.
   - Beschreibung: Parameter `duplicateResult` einführen, Aufrufer anpassen; `InMemoryDbContextFactory` um `ISaveChangesInterceptor` erweitern; Tests `FavoritesServiceTests_Concurrency`.
2. **Restpunkte Kachel-Cache (`Age`, `304`)**
   - Voraussetzungen: Keine.
   - Beschreibung: `TileFreshness`/`TileMetadata`/`TileCachePolicy.Evaluate`/`HttpTileSource` ändern; Tests `TileCachePolicyTests_Age`, `HttpTileSourceTests_AgeAndRevalidation`.
3. **Einstellung „In der Nähe“: Datenmodell**
   - Voraussetzungen: Keine.
   - Beschreibung: `AppSettings.ShowNearby`, `UserSettingsEntity.ShowNearby`, `SettingsService`, Migration `AddNearbySetting` (mit Snapshot), Tests (Unit, Integration-Upgrade).
4. **Einstellung „In der Nähe“: Oberfläche**
   - Voraussetzungen: Schritt 3.
   - Beschreibung: `SettingsTexts`, `SettingsViewModel`, `SettingsPage`; Unit- und E2E-Tests der Einstellungen.
5. **Preisabruf nach Kennungen: Mock-Server und Client**
   - Voraussetzungen: Keine.
   - Beschreibung: `MockTankerkoenigServer` um `prices.php` erweitern; `ITankerkoenigClient.GetPricesAsync`, Parser, Tests `TankerkoenigClientTests_Prices`; `StubTankerkoenigClient` ergänzen.
6. **Preisabruf nach Kennungen: Repository und Dienst**
   - Voraussetzungen: Schritt 5.
   - Beschreibung: `StationInfo.WithDistance`, `StationRefreshResult`, `IPriceRepository.GetStationsAsync`/`SavePricesAsync`, `IFuelPriceService.RefreshStationsAsync`/`GetKnownStationsAsync`; Tests (Unit, Integration gegen Mock-Server); `FakeFuelPriceService` ergänzen.
7. **Favoriten-Übersicht und Aufbereitung**
   - Voraussetzungen: Schritt 6 (`StationInfo.WithDistance`, `StationRefreshResult`).
   - Beschreibung: `FavoriteGroupOverview`, `IFavoritesService.GetOverviewAsync`, `StationResultBuilder.BuildItem` öffentlich, `HomeGroup`/`HomeStation`/`HomeOverviewBuilder`; Tests.
8. **Gruppenansicht → Detailansicht**
   - Voraussetzungen: Keine (nutzt vorhandenes `IStationNavigator`).
   - Beschreibung: `FavoriteEntryItemViewModel.OpenCommand`, `FavoriteGroupViewModel` mit `IStationNavigator`, `FavoriteGroupPage`; Tests (Unit, E2E).
9. **Startseite: ViewModels und Oberfläche**
   - Voraussetzungen: Schritte 3, 4, 6, 7.
   - Beschreibung: `HomeGroupViewModel`, `HomeStationViewModel`, `NearbySectionViewModel`, `StationTile`, `FavoritesViewModel`/`FavoritesPage` (Aktualisierung, Offline, Wiederverbindung, Standort, Leerzustand), `FavoritesTexts`; Unit-Tests.
10. **E2E-Tests der Startseite**
    - Voraussetzungen: Schritte 5 bis 9.
    - Beschreibung: `HomeE2ETestBase`, Testklassen (siehe unten), Anpassung der bestehenden Favoriten-/Einstellungs-E2E-Tests.
11. **Dokumentation**
    - Voraussetzungen: Schritte 1 bis 10.
    - Beschreibung: ADR 0006 aktualisieren (Gruppenansicht öffnet Detailansicht, „Folgen“, Status-Vermerk, Restpunkte `AlreadyMember`), ADR 0007 anlegen, `docs/help/Startseite/` anlegen, `docs/help/index.md`, `Favoriten`, `Einstellungen`, `Preisdaten`, `Suche`/Karte (Kachel-Cache `Age`/`304`), `Navigation` anpassen; `README.md` und `changes.log` gemäß Projektkonvention.
12. **Prüfung und Windows-Zwischenstand**
    - Voraussetzungen: Schritte 1 bis 11.
    - Beschreibung: `scripts/local-ci.ps1` (Format, Analyse, Unit, Integration, E2E grün); Zwischenstand mit `scripts/create-review-version.ps1 -Version 0.1.15 -ChangelogFile …` unter `review-versions/0.1.15_<Datum>/` erstellen (nicht committen).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `AddStation_ConcurrentSameGroup_ReturnsAlreadyMember`, `AddStation_ConcurrentSameGroup_DoesNotCountTwice`, `CreateGroup_ConcurrentSameName_StillDuplicateName`, `UpdateGroup_ConcurrentSameName_StillDuplicateName` | `FavoritesServiceTests_Concurrency` (Unit) | Interceptor fügt vor dem Speichern die rivalisierende Zeile ein; Ergebnis `AlreadyMember` statt `DuplicateName`, Namenskonflikte bleiben `DuplicateName`, Zahl der Einträge 1, `Changed` nicht ausgelöst |
| `Evaluate_MaxAgeMinusAge`, `Evaluate_AgeGreaterThanLifetime_ExpiresImmediately`, `Evaluate_ExpiresMinusAge`, `Evaluate_FallbackMinusAge`, `Evaluate_NoCacheAndNoStore_IgnoreAge`, `Evaluate_LifetimeAndExplicitFlags` | `TileCachePolicyTests_Age` (Unit) | `Age` kürzt die Frische, Untergrenze 0, `IsExplicit`/`Lifetime` korrekt |
| `GetTile_AgeHeader_ReducesFreshness`, `GetTile_NotModifiedWithoutCachingHeaders_KeepsPreviousLifetime`, `GetTile_NotModifiedWithOwnHeaders_UsesNewLifetime`, `GetTile_NotModifiedLegacyMetadata_UsesFallback`, `GetTile_NotModifiedAfterNoCache_RevalidatesAgain`, `GetTile_NotModifiedWithAge_ReducesPreviousLifetime`, `Metadata_WithoutLifetime_IsReadable` | `HttpTileSourceTests_AgeAndRevalidation` (Unit, `FakeHttpMessageHandler`, `ManualTimeProvider`) | `Age`-Header und `304` ohne Angaben (statt pauschal 7 Tage); z. B. erste Antwort `max-age=600`, 304 ohne Header nach 11 Minuten: nach weiteren 9 Minuten noch gültig, nach 11 abgelaufen |
| `Defaults_ShowNearbyIsFalse`, `Validate_ShowNearby_IsAccepted` | `AppSettingsTests_Defaults`/`_Validation` (erweitert, Unit) | Standard `false` |
| `SaveThenLoad_RoundTrips_ShowNearby`, `Load_WithoutStoredValue_ShowNearbyFalse` | `SettingsServiceTests_LoadSave` (erweitert, Unit) | Persistenz der Einstellung |
| `ShowNearby_Toggle_SavesImmediately`, `ShowNearby_Load_AppliesWithoutSaving`, `GpsNever_ShowsNearbyHint`, `Save_Fails_ShowsStatusMessage_ForNearby` | `SettingsViewModelTests_Nearby` (neu, Unit) | Schalter speichert sofort, Laden löst kein Speichern aus, Hinweis bei „Nie“, Fehler |
| `Initialize_FromSchemaBeforeNearbySetting_KeepsExistingDataAndAddsColumn`, `NearbySetting_SurvivesRestartOnRealDatabaseFile` | `NearbySettingMigrationTests_Upgrade` (neu, Integration) | Migration auf echter Datei erhält Einstellungen, Tankstellen, Preise, Favoriten; neue Spalte standardmäßig `false` |
| `Save_ShowNearby_RoundTripsThroughFile` | `SettingsServiceTests_RestartPersistence` (erweitert, Integration) | Neustartbeständigkeit |
| `GetPricesAsync_ParsesPricesAndFalseValues`, `GetPricesAsync_BuildsRequestWithIdsAndInvariantFormat`, `GetPricesAsync_NoPricesStatus_YieldsNoLines`, `GetPricesAsync_InvalidIdOrMoreThanTen_ThrowsBeforeRequest`, `GetPricesAsync_BadBodies_AreClassified` | `TankerkoenigClientTests_Prices` (neu, Unit) | Parser und Anfrage für `prices.php`, Fehlerabbildung wie `SearchAsync` |
| `GetStationsAsync_ReturnsLatestPricesAndPositions`, `SavePricesAsync_AppendsOnlyForKnownStations`, `SavePricesAsync_KeepsMasterData` | `PriceRepositoryTests_Stations` (neu, Integration) | Lokale Daten für Favoriten |
| `Refresh_FetchesPricesInChunksOfTen`, `Refresh_SkipsFreshStationsUnlessForced`, `Refresh_Offline_ReturnsLocalWithOfflineFallback`, `Refresh_ApiFailure_FallsBackWithFailure`, `Refresh_PartialFailure_KeepsSuccessfulChunk`, `Refresh_FailureDoesNotMarkFresh`, `Refresh_InvalidIds_AreIgnored`, `Refresh_StorageFailure_StillReturnsFreshPrices`, `GetKnownStations_NeverCallsClient` | `FuelPriceServiceTests_RefreshStations` (neu, Unit) | Abruf nach Kennungen, Fristen, Fallback, Teilerfolg |
| `Refresh_AgainstMock_UpdatesPricesAndAges`, `Refresh_MoreThanTenStations_UsesSeveralRequests`, `Refresh_ServerGone_ReturnsLastKnownPrices`, `Refresh_WrongKey_ReportsFailureWithoutThrowing`, `Refresh_ConnectionRestored_FetchesAgain` | `PriceRefreshMockServerTests_Flow` (neu, Integration) | Echter Client und Repository gegen `MockTankerkoenigServer` (nie produktiv) |
| `GetOverview_ReturnsAllGroupsWithEntriesInOrder`, `GetOverview_EmptyGroup_HasNoEntries`, `GetOverview_StationInTwoGroups_AppearsInBoth` | `FavoritesServiceTests_Overview` (neu, Unit) | Neue Dienstmethode |
| `Build_ShowsPricesInSettingsOrderWithAge`, `Build_MarksPricesFromSixtyMinutesAsStale`, `Build_HidesDistanceWithoutPosition`, `Build_ShowsDistanceWithPosition`, `Build_HintsUnconfirmedAndAutomated`, `Build_StationWithoutSelectedPrices_HasNoPrices`, `Build_KeepsEntryOrderAndMultipleGroups`, `Build_EmptyGroup`, `Build_UnselectedFuelTypeNotShown` | `HomeOverviewBuilderTests_Content` (neu, Unit) | Aufbereitung der Gruppenübersicht |
| `Load_ShowsLocalDataThenRefreshes`, `Load_WithoutGroups_ShowsHintAndDoesNotQueryPricesOrLocation`, `Load_GpsNever_NeverCallsLocationService`, `Load_GpsAllowed_ShowsDistances`, `Load_LocationUnavailable_ShowsNoDistances`, `Refresh_Command_ForcesRequest`, `Refresh_NotRunningWhenOffline`, `Disappearing_CancelsAndDropsPosition`, `FavoritesChanged_ReloadsWithoutNewRequest`, `OpenStation_NavigatesWithStationAndGuardsDoubleOpen`, `Load_StorageFailure_ShowsMessage`, `Refresh_Failure_ShowsMessageAndKeepsLastKnown`, `Position_IsNeverLogged` | `FavoritesViewModelTests_Home` (neu, Unit) | Ablauf der Startseite, Standort nur bei Bedarf, Fehlerfälle, Datenschutz (`ListLogger`) |
| `Offline_ShowsLastKnownPricesAndBanner`, `ConnectionRestored_RefreshesStalePricesAndClearsBanner`, `ConnectionRestored_FreshPrices_DoesNotQueryAgain`, `ConnectionRestored_AfterOfflineFallback_Refreshes`, `ConnectionRestored_AfterDisappearing_IsIgnored`, `ConnectionLost_ShowsBannerImmediately`, `ConnectionRestored_ReloadsNearbyWhenOfflineResult` | `FavoritesViewModelTests_Reconnect` (neu, Unit, `FakeConnectionMonitor`) | Automatische Aktualisierung nach Wiederverbindung, Offline-Hinweis entfällt |
| `Hidden_WhenSettingOff`, `Hidden_WhenGpsNever`, `Visible_WhenEnabledAndAllowed`, `Load_BuildsEntriesSortedByDistanceMaxTen`, `Filter_ByFuelType_ChangesListWithoutNewRequest`, `NoLocation_ShowsLocationMessage`, `Offline_ShowsLastKnownEntries`, `Entries_OpenDetailAndOfferNoFavoriteActions`, `NoResults_ShowsHint`, `Disabled_DoesNotRequestLocation` | `NearbySectionViewModelTests_Section` (neu, Unit) | Bereich „In der Nähe“ |
| `OpenEntry_NavigatesToStationDetail`, `OpenEntry_NavigationFailure_ShowsMessage`, `OpenEntry_DoubleTap_OpensOnce` | `FavoriteGroupViewModelTests_Group` (erweitert, Unit) | Zusatz 1 |
| `ViewModelTests_PageTitles`: unverändert Titel „Favoriten“ (Konstruktoren angepasst) | `ViewModelTests_PageTitles` (angepasst) | Titel bleiben |
| Hilfsmethoden: `HomeTestData` (Gruppen, Tankstellen, Preise), `FavoritesViewModelTestBase` (Aufbau mit Fakes) | `Unit/Support` bzw. Testbasis | Gemeinsame Testdaten |
| Hilfsmethoden E2E: `HomeE2ETestBase` (`OpenHome`, `CreateGroupWithStation`, `EnableNearby`, `ReadStationNames(groupIndex)`) | `HomeE2ETestBase : FavoritesE2ETestBase` | Aufbau der Testdaten über Suche → Detail → „Zu Favoriten hinzufügen“ (wie `FavoritesE2ETestBase`) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FavoritesViewModelTests_Groups`, `FavoritesChangeTests_Notification` | Neuer Konstruktor von `FavoritesViewModel` (zusätzliche Abhängigkeiten); `Groups` enthält `HomeGroupViewModel` |
| `FavoriteGroupViewModelTests_Group`, `StationFavoritesViewModelTests_Flow` (falls `FavoriteGroupViewModel` erzeugt) | Konstruktor mit `IStationNavigator` |
| `ViewModelTests_PageTitles` | Konstruktoren |
| `FavoritesServiceTests_Membership`, `_Groups` | Fehlerabbildung bleibt; Nachweis, dass bestehende `AlreadyMember`/`DuplicateName`-Fälle unverändert sind |
| Alle Klassen mit eigenen `IFuelPriceService`/`IPriceRepository`/`ITankerkoenigClient`/`IFavoritesService`-Doubles (`FakeFuelPriceService`, `StubTankerkoenigClient`, Doubles in `FuelPriceServiceTestBase`, Integration-Support) | Neue Interface-Mitglieder |
| `HttpTileSourceTests_HttpCaching.Evaluate_EdgeCases`, `HttpTileSourceTests_NoStoreAndExpires` | Neuer optionaler Parameter von `Evaluate`, erweitertes `TileFreshness`; Ergebnisse unverändert |
| `SettingsViewModelTests_Loading`, `_Changes`, `SettingsServiceTests_LoadSave`, `SettingsEnumTests_Persistence`, `FakeSettingsService` | `AppSettings` mit neuem Feld, Speichern enthält `ShowNearby` |
| `MigrationsTests_ModelSync`, `DatabaseInitializerTests_*` | Neue Migration muss mit dem Modell übereinstimmen |
| `SettingsE2ETestBase`/`SettingsE2ETests_Defaults` (`Settings_ProvideAllControls_WithoutElectricOptions`: Schalter „In der Nähe“ als neues Steuerelement; `FirstStart_ShowsDefaultValues`: Schalter aus), `SettingsE2ETests_Persistence` (Schalter überlebt Neustart) | Neue Einstellung |

### E2E-Tests (primärer Funktionsnachweis)

Testdaten: `MockTankerkoenigServer` (Alpha, Beta, …), `MockNominatimServer`, `MockTileServer`, Testposition `52.5200,13.4050` (`TANKATLAS_TEST_LOCATION`), isolierte Testdatenbank; Favoriten werden wie im Bestand über Suche → Detail → „Zu Favoriten hinzufügen“ angelegt. Neu: `HomeE2ETestBase` (Basis `FavoritesE2ETestBase`).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Erster Start ohne Gruppen: Startseite zeigt den Hinweis zum Anlegen von Favoriten (`Favorites.Empty`), keine Preise, kein „In der Nähe“, kein Standortzugriff | `HomeE2ETests_Overview` | Hinweis ohne Gruppen | Sichtbarkeit und Text der echten Seite |
| Pflicht | Zwei Gruppen (Tankstelle in beiden und eine weitere): Startseite zeigt Gruppennamen und je Gruppe die Tankstellen, Preise der gewählten Sorten (Preiszeilen, Alter „vor 0 Min.“-Format), Entfernung (Testposition), Hinweis „Automatentankstelle“ für Alpha (nach geöffneter Detailansicht, `wholeDay`) | `HomeE2ETests_Overview` | Gruppen mit Name, Entfernung, Preisen, Alter, Hinweisen | Zusammenspiel Datenbank, Preisdienst, Standort und Darstellung |
| Pflicht | Reihenfolge der Sorten folgt den Einstellungen: Diesel in „Optionen“ nach oben, abgewählte Sorte verschwindet | `HomeE2ETests_FuelOrder` | Preise in Einstellungsreihenfolge | Einstellungen → Startseite über echte Navigation |
| Pflicht | Aktualisierung: Beim Öffnen wird `prices.php` abgerufen (`PricesRequests` ≥ 1); `SetPrices` im Mock, Schaltfläche `Favorites.Refresh` zeigt den neuen Preis und erhöht den Zähler | `HomeE2ETests_Refresh` | Aktualisierung beim Öffnen und auf Wunsch | Netzwerkpfad und Anzeige |
| Pflicht | Tankstelle antippen (`Favorites.Station.Open`) öffnet die Detailansicht (`Detail.Name`) | `HomeE2ETests_OpenDetail` | Detailansicht öffnen | Navigation |
| Pflicht | Gruppenansicht: Eintrag öffnen (`FavoriteGroup.Entry.Open`) zeigt die Detailansicht der Tankstelle (Name, Preise) | `FavoritesE2ETests_Flow` (erweitert) | Zusatz 1 | Navigation aus der Gruppenansicht |
| Pflicht | Offline: Preisdienst unerreichbar (Neustart mit `ClosedPortUrl`): zuletzt bekannte Preise mit Alter, Kopfzeilenhinweis „Offline: keine Verbindung zum Preisdienst.“ (`Favorites.OfflineBanner`) | `HomeE2ETests_Offline` | Offline-Anzeige | Der gesamte Offline-Pfad inkl. Cache |
| Pflicht | „In der Nähe“: standardmäßig nicht sichtbar; Schalter in „Optionen“ (`Settings.Nearby.Switch`) aktivieren → Bereich zeigt Alpha und Beta (Radius 5 km, nach Entfernung), Filterchip Diesel zeigt nur Alpha (Beta hat keinen Diesel), „Alle“ zeigt beide; keine Favoritenschaltflächen im Bereich; Antippen öffnet Detailansicht | `HomeE2ETests_Nearby` | Bereich nur bei Aktivierung, filterbar, ohne Favoritenaktionen | Sichtbarkeitsregeln und Filter über die echte UI |
| Pflicht | „In der Nähe“ aktiviert, aber `GpsUsage` „Nie“ (`Settings.Gps.Never`): Bereich unsichtbar, keine Entfernungen, kein Umkreis-Abruf am Mock (`ListRequests` unverändert) | `HomeE2ETests_NearbyGpsNever` | Bereich nur bei erlaubter GPS-Nutzung; Standort nur bei Bedarf | Zusammenspiel zweier Einstellungen |
| Pflicht | Schalter und Wirkung überstehen einen Neustart; Standort wird nicht gespeichert (Testdatenbank enthält keine Position, geprüft im Integrationstest `SearchPrivacyTests_Persistence` erweitert) | `SettingsE2ETests_Persistence` (erweitert) | Persistenz der Einstellung | Neustart der echten App |
| Optional | Gruppe einklappen/ausklappen (`Favorites.Group.Toggle`) blendet Tankstellen aus/ein | `HomeE2ETests_Overview` | Designentwurf | Sichtbarkeitsverhalten |

Nicht per E2E prüfbar, deshalb ausschließlich Unit-/Integrationstests: automatische Aktualisierung nach echter Wiederverbindung (die Betriebssystem-Netzwerkverbindung lässt sich in FlaUI nicht kontrolliert wechseln; Nachweis über `FavoritesViewModelTests_Reconnect` mit `FakeConnectionMonitor` und `PriceRefreshMockServerTests_Flow.Refresh_ConnectionRestored_FetchesAgain`) und Amber-Markierung ab 60 Minuten (keine Zeitsteuerung in der laufenden App; Nachweis über `HomeOverviewBuilderTests_Content`). Im E2E wird zusätzlich geprüft, dass Preiszeilen das Alter zeigen.

Bestehende E2E-Tests, die angepasst werden müssen:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FavoritesE2ETestBase.OpenFavorites`, `FavoritesE2ETests_Groups`, `FavoritesE2ETests_Flow` | Startseite statt Gruppenliste; die AutomationIds `Favorites.Group.*`, `Favorites.NewGroup*` bleiben, Prüfungen auf Gruppenzahl/-namen bleiben gültig; Tankstellenzeilen kommen hinzu (eigene Ids `Favorites.Station.*`) |
| `NavigationE2ETests` | Prüft pro Tab das Seitenelement `FavoritesPage.Headline` (bleibt erhalten); zusätzliche Laufzeiten bei Standortabruf beachten |
| `SettingsE2ETests_Defaults`, `SettingsE2ETests_Persistence` | Neuer Schalter |
| `SearchE2ETestBase`-Ableitungen | Keine Änderung, solange `Search.*`-Ids unverändert; Startseite fragt `prices.php` ab, daher dürfen Tests, die `TotalRequests` des Mock-Servers auswerten, nicht vorher die Startseite öffnen (Prüfen: `SearchE2ETests_GpsNever`, `PriceServiceE2ETests_*`) |

## Offene Punkte

Keine.
