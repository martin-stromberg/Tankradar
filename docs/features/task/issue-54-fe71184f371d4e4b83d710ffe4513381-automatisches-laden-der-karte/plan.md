# Umsetzungsplan: Automatisches Laden der Karte (Issue #54)

## Übersicht

Die `MapPage` soll beim erstmaligen Anzeigen innerhalb einer App-Sitzung selbständig eine Umkreissuche am aktuellen Standort (`SearchMode.CurrentLocation`, `SearchRadius.Default` = 5 km) ausführen. Bei deaktivierter Standortnutzung (`GpsUsage.Never`) unterbleibt die Startsuche still — keine Standort- oder Preisabfrage und keine Hinweismeldung. Bei jedem weiteren Anzeigen bleibt das zuletzt gezeigte Ergebnis vollständig erhalten, ohne erneuten Suchlauf. Betroffen ist ausschließlich `MapViewModel` (`OnAppearing` als Auslösepunkt plus ein sitzungsbezogenes Flag); `MapPage`, `TankradarContentPage`, Datenmodell, Enums, Persistenz und Konfiguration bleiben unverändert. Der Erhalt des Ergebnisses über `OnDisappearing` hinweg ist bereits gegeben und darf durch die neue Logik nicht zerstört werden.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Auslösepunkt der Startsuche | `MapViewModel.OnAppearing()` ruft am Ende neue private Methode `MaybeStartAutoSearch()` auf (kein Konstruktor, keine `MapPage`-Änderung, kein Shell-Eingriff) | Der Lebenszyklus-Hook existiert bereits, `TankradarContentPage.OnAppearing` leitet bestätigt weiter, und das ViewModel lebt über die `ShellContent`-Instanz faktisch sitzungslang — ein Feld im ViewModel ist damit sitzungsbezogen. Direkt testbar über `OnAppearing`/`LastSearchTask`. |
| Wiederholungsschutz | Explizites Flag `_autoSearchDone` statt Ableitung aus dem sichtbaren Zustand („pristine check") | Ein Flag unterscheidet zuverlässig „Startsuche noch nie ausgelöst" von „Ergebnis wurde bewusst verworfen" (z. B. Moduswechsel → `ClearResult`). Ein reiner Zustandscheck würde nach manuellem Leeren des Ergebnisses erneut automatisch suchen. |
| Zeitpunkt des Flag-Setzens | `_autoSearchDone` wird erst gesetzt, wenn die Startsuche einen sichtbaren Ausgang hinterlassen hat (`_lastResult is not null` oder `HasStatusMessage`), ausgewertet nach `await SearchAsync()` in einer Wrapper-Methode | Ein Abbruch durch `OnDisappearing` (Tab-Wechsel während der laufenden ersten Suche) hinterlässt weder Ergebnis noch Meldung — die Startsuche läuft dann beim nächsten Erscheinen erneut, damit der Anwender nicht dauerhaft eine leere Karte sieht (vom Anwender entschieden). Erfolg, Leerergebnis und Fehlermeldung zählen als „erfolgt" und verhindern Wiederholungen. |
| `GpsUsage.Never` (Standortnutzung „Nie") | Vorab-Prüfung der Einstellung in `AutoSearchOnceAsync` vor `SearchAsync`: `GetSettingsOrReportAsync` → `settings.GpsUsage.AllowsLocation()`; bei `false` kein Suchlauf, keine Meldung, `_autoSearchDone` bleibt `false` | Anwenderentscheidung: Bei deaktivierter Standortnutzung soll schlicht keine automatische Suche ausgelöst werden — kein `DisabledBySetting`-Hinweis nötig. Da die Prüfung vor `ILocationService` liegt, entsteht keinerlei Abfrage. Weil `_autoSearchDone` nicht gesetzt wird, prüft jedes `OnAppearing` erneut: Aktiviert der Anwender die Ortung noch in derselben Sitzung, lädt die Karte beim nächsten Erscheinen nach. |
| Ausführung der eigentlichen Suche | Unverändertes `SearchAsync()` (`LastSearchTask = AutoSearchOnceAsync()`, Wrapper um `SearchAsync`) | Der komplette Ablauf (Validierung, `RunGuardedSearchAsync`, `RunSearchAsync`, `ApplyResult`, `MapFitMode.FitToResult`) ist vorhanden und durch Tests abgesichert; ein eigener Suchpfad würde Verhalten duplizieren. |
| Teilergebnis-Halter für `LocationResult` (Kombination mit „In der Nähe") | Kein neuer Dienst — der bestehende Such-Zwischenspeicher `_searchCache` des `FuelPriceService` (Singleton, `CacheLifetime` 5 min, Schlüssel aus gerundeten Koordinaten + Radius) dient als Kombinationsmechanismus | `NearbySectionViewModel.LoadAsync` stellt eine identische `StationSearchQuery` (5 km); bei gleichen gerundeten Koordinaten liefert die Startsuche `PriceDataSource.Cache` ohne zweiten API-Abruf. Die zusätzliche Standortermittlung per `ILocationService` wird akzeptiert (vom Anwender entschieden — kein geteilter `LocationResult`-Halter). |

## Programmabläufe

### Startsuche beim erstmaligen Anzeigen

1. `TankradarContentPage.OnAppearing` leitet an `MapViewModel.OnAppearing()` weiter (bestehend).
2. `OnAppearing` führt die bisherigen Schritte aus: einmaliges Anmelden von `ConnectionChanged`/`IRouteSearchAccess.Changed` (`_subscribed`), `UpdateOfflineState()`, `ResumeAreaReloadTimer()`, `LastSettingsTask = LoadSettingsAsync()`, `LastRouteAccessTask = RefreshRouteAvailabilityAsync()`.
3. Neu am Ende: `MaybeStartAutoSearch()`. Guard: Abbruch, wenn `_autoSearchDone` gesetzt, `_lastResult is not null` oder `_searchMode != SearchMode.CurrentLocation`. (`_radiusKm` ist im Ausgangszustand `SearchRadius.Default` = 5; ein Wechsel vor dem ersten Erscheinen ist über die UI nicht möglich, der Guard ist defensiv.)
4. Bei erfülltem Guard: `LastSearchTask = AutoSearchOnceAsync()`.
5. `AutoSearchOnceAsync` lädt zuerst die Einstellungen über `GetSettingsOrReportAsync(clearResult: false, token)` — dedupliziert mit dem parallel laufenden `LoadSettingsAsync` über `_settingsLoad`. Schlägt das Laden fehl, ist die Meldung bereits gesetzt: `_autoSearchDone = _lastResult is not null || HasStatusMessage` auswerten und enden.
6. `GpsUsage`-Vorprüfung: `!settings.GpsUsage.AllowsLocation()` (z. B. `GpsUsage.Never`) → `AutoSearchOnceAsync` endet ohne Suchlauf, ohne Standortabfrage, ohne Meldung; `_autoSearchDone` bleibt `false` (siehe Ablauf „Deaktivierte Standortnutzung").
7. Sonst `await SearchAsync()` — unveränderter Ablauf: `CancelSearch()`, `MapFitMode = FitToResult`, `SearchRadius.IsValid`-Prüfung, `RunGuardedSearchAsync` → `RunSearchAsync` → `GetSettingsOrReportAsync` (bereits geladen, keine Doppelladung) → `ResolveCurrentLocationAsync` (`ILocationService.GetCurrentLocationAsync` mit `AppSettings.GpsUsage`) → `StationSearchQuery` (5 km, gewählte `FuelTypes`) + `MapOrigin(MapOriginKind.CurrentLocation)` → `IFuelPriceService.SearchNearbyAsync` → `ApplyResult` (befüllt `_lastResult`, `Origin`, `MapMarkers`, `Stations`, Status-/Offline-Meldungen).
8. Nach dem `await`: `_autoSearchDone = _lastResult is not null || HasStatusMessage`. Ein leeres `StationSearchResult` zählt als Ergebnis (`ShowEmptyState`), ein `PermissionDenied`-/`Unavailable`-/`SearchFailed`-Hinweis als Meldung — beides verhindert Wiederholungen. `DisabledBySetting` kann im Startsuchpfad nicht mehr auftreten, da `GpsUsage.Never` bereits in Schritt 6 abgefangen wird; bei der manuellen Suche bleibt die Meldung unverändert.

Beteiligte Klassen/Komponenten: `MapViewModel`, `TankradarContentPage`, `ILocationService`, `IFuelPriceService`, `ISettingsService`, `StationResultBuilder`, `MapMarkerBuilder`.

### Wiederholtes Anzeigen (Erhalt des Ergebnisses)

1. `OnDisappearing` (bestehend, unverändert): meldet Events ab, `CancelSearch()`, `StopAreaReloadTimer()`, leert `RouteProgressText`, `IsBusy = false` — verwirft kein Ergebnis.
2. Erneutes `OnAppearing`: `MaybeStartAutoSearch` greift in den Guard (`_autoSearchDone` bzw. `_lastResult is not null`) → kein Suchlauf. `_lastResult`, `_routeResult`, `MapMarkers`, `Origin`, Filter, Sortierung, Panel- und Suchdialog-Zustand bleiben unverändert bestehen. Gilt auch für Routenergebnis (`_routeResult`) und per `ReloadVisibleAreaAsync` nachgeladene Ausschnittsergebnisse.

Beteiligte Klassen/Komponenten: `MapViewModel` (alle Partials).

### Abgebrochene Startsuche

1. `OnDisappearing` während laufender Startsuche → `CancelSearch()`; der abgelöste Lauf wird von `RunGuardedSearchAsync` (`IsCurrent`) verworfen, ohne Ergebnis oder Meldung zu hinterlassen.
2. `AutoSearchOnceAsync` endet; `_autoSearchDone` bleibt `false`, da weder `_lastResult` noch `HasStatusMessage` gesetzt sind.
3. Nächstes `OnAppearing` startet die Startsuche erneut (gleicher Ablauf wie „Startsuche beim erstmaligen Anzeigen"; vom Anwender entschieden: Abbruch gilt nicht als „erfolgt").

Beteiligte Klassen/Komponenten: `MapViewModel`.

### Deaktivierte Standortnutzung (`GpsUsage.Never`)

1. `OnAppearing` → `MaybeStartAutoSearch`: der Guard besteht (`_autoSearchDone` false, kein Ergebnis, Modus `CurrentLocation`) → `LastSearchTask = AutoSearchOnceAsync()`.
2. `AutoSearchOnceAsync` lädt die Einstellungen über `GetSettingsOrReportAsync` und prüft `settings.GpsUsage.AllowsLocation()` → `false`.
3. Die Methode endet: kein Aufruf von `ILocationService` oder `IFuelPriceService`, keine `StatusMessage`, kein `ClearResult` — die Karte bleibt im unberührten Ausgangszustand. Es wird bewusst **kein** `DisabledBySetting`-Hinweis gezeigt (Anwenderentscheidung: „KEINE Suche starten").
4. `_autoSearchDone` bleibt `false`: Die `GpsUsage`-Prüfung läuft bei jedem `OnAppearing` erneut (die Einstellungen werden ohnehin je Erscheinen geladen). Wechselt die Einstellung in derselben Sitzung auf `Always`/`WhileInUse`, startet das nächste `OnAppearing` die normale Startsuche.
5. Die manuelle Suche ist unverändert: Betätigt der Anwender „Suchen" bei `GpsUsage.Never`, erscheint weiterhin die bestehende `LocationDisabledBySetting`-Meldung über `ResolveCurrentLocationAsync`.

Beteiligte Klassen/Komponenten: `MapViewModel`, `ISettingsService` (`GetSettingsOrReportAsync`/`_settingsLoad`), `GpsUsageExtensions.AllowsLocation`.

### Kombination mit „In der Nähe" (ohne eigenen Code)

1. `FavoritesViewModel.RefreshAsync`/`NearbySectionViewModel.LoadAsync` fragen bei erlaubter Standortnutzung den Standort ab und rufen `SearchNearbyAsync` mit `SearchRadius.Default` (5 km) auf — identische Abfrageform wie die Startsuche.
2. Trifft die Startsuche denselben Cache-Schlüssel (`Round(lat,3)|Round(lng,3)|RadiusKm`) innerhalb von `CacheLifetime` (5 min), liefert `FuelPriceService.SearchNearbyAsync` `PriceDataSource.Cache` ohne zweiten API-Abruf.
3. Der Standort wird je Seite separat über `ILocationService` ermittelt (kein geteilter `LocationResult`-Halter — siehe Designentscheidungen; vom Anwender so entschieden).

Beteiligte Klassen/Komponenten: `NearbySectionViewModel`, `FuelPriceService` (`_searchCache`, `IsSearchCached`), `MapViewModel`.

## Neue Klassen

Keine — die Umsetzung erweitert nur `MapViewModel`.

## Änderungen an bestehenden Klassen

### `MapViewModel` (ViewModel, partial — `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`)

- **Neue Felder:** `_autoSearchDone` (`bool`, privat) — sitzungsbezogener Zustand „Startsuche hat einen sichtbaren Ausgang hinterlassen". Lebt implizit sitzungslang, da die `ShellContent`-Instanz der `MapPage` im `TabBar` bestehen bleibt.
- **Neue Methoden:**
  - `MaybeStartAutoSearch()` (privat, `void`) — Guard (`_autoSearchDone`, `_lastResult is not null`, `_searchMode != SearchMode.CurrentLocation`) und Start der Startsuche über `LastSearchTask = AutoSearchOnceAsync()`; wird am Ende von `OnAppearing()` aufgerufen.
  - `AutoSearchOnceAsync()` (privat, `Task`) — zuerst `GetSettingsOrReportAsync(clearResult: false, token)` (Einstellungs-Vorprüfung, dedupliziert über `_settingsLoad`); bei `!settings.GpsUsage.AllowsLocation()` still enden (kein Suchlauf, keine Meldung, `_autoSearchDone` bleibt `false`); sonst `await SearchAsync()`, danach `_autoSearchDone = _lastResult is not null || HasStatusMessage` setzen (Auswertung der Ergebnislage; ermöglicht Wiederholung nach Abbruch ohne Ausgang).
- **Geänderte Methoden:** `OnAppearing()` — zusätzlicher Aufruf `MaybeStartAutoSearch()` am Ende der Methode; alle bisherigen Schritte bleiben unverändert. Methodenkommentar aktualisieren („ein Standort wird dabei nicht abgefragt" entfällt bzw. wird durch die Startsuche-Beschreibung ersetzt).
- **Unverändert:** `SearchAsync`, `SearchCommand`, `LastSearchTask`, `ClearResult`, `ApplyResult`, `MapFitMode`, `OnDisappearing`.

### `MapViewModelTestBase` (Test-Basisklasse — `src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`)

- **Neue Methoden:** `AppearAndAutoSearchAsync()` — `OnAppearing()` aufrufen und zusätzlich `LastSearchTask` abwarten (deterministischer Endzustand für Startsuche-Tests; `LastSearchTask` ist initial `Task.CompletedTask`, das Abwarten ist immer sicher).
- **Hinweis:** `AppearAsync()` bleibt unverändert (kein `LastSearchTask`-Abwarten), damit Gate-gesteuerte Tests (`Location.Gate`, `Prices.SearchGate`) keinen Deadlock erzeugen; bei ungated Fakes läuft die Startsuche synchron während `OnAppearing` durch.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es werden keine neuen Eingaben eingeführt; die bestehenden Prüfungen von `SearchAsync` (`SearchRadius.IsValid`, `AddressInput.Validate`) bleiben unverändert wirksam.

## Konfigurationsänderungen

Keine — die Anforderung verlangt keine Konfigurierbarkeit; das Verhalten folgt `AppSettings.GpsUsage` (vom Anwender entschieden: keine neue Einstellung zum Abschalten der Startsuche).

## Seiteneffekte und Risiken

- **Bisheriges Nicht-Suchen ist testvertraglich abgesichert:** `MapViewModelTests_Search.Appearing_DoesNotQueryLocationOrPrices` (Unit) und `SearchE2ETests_Basic.OpeningSearch_DoesNotQueryBeforeSubmit` (E2E) prüfen explizit, dass beim Erscheinen nichts abgefragt wird — beide brechen beabsichtigt und werden umgeschrieben.
- **Aufrufzähler in Unit-Tests:** Jeder `AppearAsync`/`AppearAndViewportAsync`-Aufruf führt jetzt einen `ILocationService`- und einen `IFuelPriceService`-Aufruf aus. `Assert.Empty`/`Assert.Single` auf `Location.Calls`/`Prices.Queries` in zahlreichen `MapViewModelTests_*`-Dateien müssen angepasst werden (empfohlenes Muster: Zähler vor der eigentlichen Aktion merken bzw. `Prices.Queries[^1]` auswerten).
- **Anfangszustand nach dem Erscheinen:** Die Startsuche hinterlässt je nach Fake-Ergebnis `ShowEmptyState`, `Stations`/`MapMarkers` oder eine `StatusMessage` — Tests, die einen unberührten Anfangszustand annehmen (u. a. Settings-Failure-Pfad, Filter/Sortierung, Panel), sind zu prüfen.
- **Gate-/Concurrency-Tests:** `Location.Gate`/`Prices.SearchGate` werden jetzt zuerst von der Startsuche getroffen; `OnDisappearing_CancelsRunningSearch` und `SupersededSearch_DoesNotOverwriteNewerResult` sind entsprechend umzubauen (die Startsuche kann selbst die „laufende Suche" sein).
- **E2E-Request-Zähler:** `Assert.Equal(0, Server.TotalRequests)` bzw. `ListRequests`-Zähler in `MapE2ETests_SearchOverlay`, `RouteE2ETests_Cancel`, `MapE2ETests_*` und `SearchE2ETests_*` verschieben sich um die Startsuche; identische Anschlusssuchen werden durch den 5-Minuten-Cache dedupliziert, abweichende Radius-/Ortsabfragen nicht.
- **Berechtigungsdialog:** Bei `GpsUsage.WhileInUse` kann beim erstmaligen Öffnen der Karte der System-Berechtigungsdialog erscheinen, ohne dass der Anwender „Suchen" betätigt hat — vom Anwender akzeptiert (identisch zur „In der Nähe"-Logik der Favoriten und zur manuellen Suche).
- **Deaktivierte Standortnutzung (`GpsUsage.Never`):** Die Startsuche unterbleibt vollständig — keine Standort-/Preisabfrage und keine `LocationDisabledBySetting`-Meldung beim ersten Öffnen (Anwenderentscheidung). Die Karte bleibt unberührt; da `_autoSearchDone` nicht gesetzt wird, holt ein späteres Aktivieren der Ortung in derselben Sitzung die Startsuche beim nächsten `OnAppearing` nach. Die manuelle Suche zeigt weiterhin die bestehende Meldung.
- **Sofortiger Ladezustand:** `IsBusy`/Fortschritt und `MapFitMode.FitToResult` (Einpassen auf Standort und Ergebnis) sind beim ersten Tab-Wechsel auf „Karte" sofort aktiv — gewünschtes Verhalten, wirkt sich aber auf E2E-Wartezeiten aus (Warten auf Stationsnamen statt statischer Assertions direkt nach `OpenSearch`).
- **Parallelität mit `LoadSettingsAsync`:** `AutoSearchOnceAsync` ruft `GetSettingsOrReportAsync` für die `GpsUsage`-Vorprüfung parallel zum `OnAppearing`-Ladelauf auf; die Deduplizierung über `_settingsLoad` verhindert Doppelladungen. Ein Einstellungs-Ladefehler meldet über diesen Pfad (`clearResult: false` — es gibt noch nichts zu räumen) und gilt als sichtbarer Ausgang (`HasStatusMessage` → `_autoSearchDone`).

## Umsetzungsreihenfolge

1. **`MapViewModel`: Startsuche implementieren**
   - Voraussetzungen: Keine.
   - Beschreibung: Feld `_autoSearchDone`, Methoden `MaybeStartAutoSearch()` und `AutoSearchOnceAsync()` in `MapViewModel.cs` einführen; Aufruf von `MaybeStartAutoSearch()` am Ende von `OnAppearing()`; Methodenkommentar von `OnAppearing` anpassen.

2. **`MapViewModelTestBase`: Hilfsmethode `AppearAndAutoSearchAsync()` ergänzen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Erscheinen plus Abwarten von `LastSettingsTask`, `LastRouteAccessTask` und `LastSearchTask` für deterministische Startsuche-Tests.

3. **Neue Unit-Tests `MapViewModelTests_AutoLoad`**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: Neue Testdatei für Startsuche, Wiederholungsschutz, Abbruch-Retry und das stille Überspringen bei `GpsUsage.Never` (siehe Tests).

4. **Bestehende Unit-Tests anpassen**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `Appearing_DoesNotQueryLocationOrPrices` umwidmen; Aufrufzähler-, Anfangszustands- und Gate-Assertions in den betroffenen `MapViewModelTests_*`-Dateien an die Startsuche anpassen.

5. **Integrationstest: Cache-Synergie mit „In der Nähe"**
   - Voraussetzungen: Schritt 1; vorhandene Infrastruktur `SearchMockServerTestBase`/`MockTankerkoenigServer`/`TestLocationService`.
   - Beschreibung: Identische `StationSearchQuery` vorab an den echten `FuelPriceService` (Cache füllen), dann `MapViewModel` erscheinen lassen und Cache-Nutzung sowie fehlenden zweiten `ListRequest` nachweisen.

6. **E2E-Tests**
   - Voraussetzungen: Schritt 1; vorhandene Infrastruktur `SearchE2ETestBase`/`MapE2ETestBase` mit `TANKATLAS_TEST_LOCATION` und `MockTankerkoenigServer`.
   - Beschreibung: Neue Datei `MapE2ETests_AutoLoad` (erstmaliges Laden, Erhalt bei Tab-Wechsel und Detail-Rückkehr, `GpsUsage.Never`); `OpeningSearch_DoesNotQueryBeforeSubmit` entfernen/invertieren; Request-Zähler-Assertions der bestehenden E2E-Tests prüfen und anpassen.

7. **Verifikation**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: Build sowie Unit-, Integrations- und betroffene E2E-Tests ausführen; verbleibende Testbrüche nachziehen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `AppearAndAutoSearchAsync()` | `MapViewModelTestBase` | Erscheinen inkl. Abwarten der Startsuche (`LastSearchTask`). |
| `FirstAppearing_SearchesAtCurrentLocationWithDefaultRadius` | `MapViewModelTests_AutoLoad` (neu) | Ein `Location.Calls`-Eintrag mit `GpsUsage.WhileInUse`, eine `Prices.Queries`-Anfrage mit `RadiusKm` = 5 und den Fake-Koordinaten, `Origin.Kind` = `CurrentLocation`, `MapFitMode.FitToResult`, Stations/Markierungen befüllt. |
| `Reappearing_KeepsResultWithoutNewQueries` | `MapViewModelTests_AutoLoad` | Nach Verschwinden und erneutem Erscheinen bleiben `Location.Calls`/`Prices.Queries` einzeln; `_lastResult`, `Origin`, `MapMarkers`, Liste, Filter/Sortierung unverändert. |
| `Reappearing_AfterEmptyResult_DoesNotRepeat` | `MapViewModelTests_AutoLoad` | Leeres `StationSearchResult` (`ShowEmptyState`) zählt als ausgelöst — kein erneuter Abruf beim nächsten Erscheinen. |
| `Reappearing_AfterFailureMessage_DoesNotRepeat` | `MapViewModelTests_AutoLoad` | Fehlerhafte Startsuche (`SearchFailed` bzw. Standortmeldung `PermissionDenied`/`Unavailable`) wird nicht wiederholt. |
| `DisappearingDuringAutoSearch_ReappearingRetries` | `MapViewModelTests_AutoLoad` | `Location.Gate` blockiert die Startsuche, `OnDisappearing` bricht ab (kein Ergebnis/Meldung), Gate öffnen, erneutes Erscheinen startet die Suche erneut (entschieden: Abbruch gilt nicht als „erfolgt"). |
| `GpsNever_SkipsAutoSearchSilently` | `MapViewModelTests_AutoLoad` | `GpsUsage.Never` (`Settings.Stored`): nach dem Erscheinen sind `Location.Calls` und `Prices.Queries` leer, keine `StatusMessage`, kein `ShowEmptyState` — die Startsuche löst keine Abfrage und keinen Hinweis aus (Anwenderentscheidung). |
| `GpsNever_EnabledLater_AutoSearchRunsOnReappear` | `MapViewModelTests_AutoLoad` | `GpsUsage.Never` → Erscheinen (kein Abruf), dann `Settings.Stored` auf `WhileInUse` umstellen und erneut erscheinen → Startsuche läuft nach (`_autoSearchDone` wurde im Never-Pfad nicht gesetzt). |
| `AppearingInAddressMode_DoesNotAutoSearch` | `MapViewModelTests_AutoLoad` | Guard `_searchMode`: vor dem Erscheinen per `ModeOptions` auf `Address` gewechselt → kein Abruf. |
| `AutoSearch_UsesSearchCache_AfterIdenticalQuery` | neue Integrations-Testklasse auf `SearchMockServerTestBase` | Vorab `SearchNearbyAsync(Query(5))` am echten `FuelPriceService` (Cache füllen, `IsSearchCached` = true); dann `CreateViewModel(service)` erscheinen lassen, `LastSearchTask` abwarten: `Server.ListRequests` bleibt 1, Ergebnis-`Source` = `PriceDataSource.Cache` (Nachweis der Kombinationsvariante — Such-Cache genügt, kein geteilter Positionshalter). |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `MapViewModelTests_Search.Appearing_DoesNotQueryLocationOrPrices` | Prüft das alte Verhalten (kein Abruf beim Erscheinen); wird umgeschrieben/entfernt — neue Positive-Abdeckung in `MapViewModelTests_AutoLoad`. |
| `MapViewModelTests_Search.InvalidRadius_CallsNeitherLocationNorPrices` | `Assert.Empty(Location.Calls)`/`Assert.Empty(Prices.Queries)` scheitern an der Startsuche; Zähler relativ zur Startsuche prüfen. |
| `MapViewModelTests_Concurrency.OnDisappearing_CancelsRunningSearch` | `Location.Gate` wird jetzt von der Startsuche getroffen — Umbau auf „Verschwinden bricht die Startsuche" bzw. Gate-Verwaltung für die manuelle Suche. |
| `MapViewModelTests_Concurrency.SupersededSearch_DoesNotOverwriteNewerResult` | Die Startsuche ist ein zusätzlicher Lauf; Reihenfolge/Ablösung muss sie berücksichtigen. |
| `MapViewModelTests_Address`, `MapViewModelTests_AreaReload`, `MapViewModelTests_AreaReloadBlock`, `MapViewModelTests_Chips`, `MapViewModelTests_FilterSort`, `MapViewModelTests_MapView`, `MapViewModelTests_Panel`, `MapViewModelTests_Route`, `MapViewModelTests_RouteAvailability`, `MapViewModelTests_RouteValidation` | Aufrufzähler (`Assert.Empty`/`Assert.Single`/`Assert.Equal(n, …)` auf `Location.Calls`/`Prices.Queries`) verschieben sich um die Startsuche (+1 Standort-/Preisaufruf je erstmaligem Erscheinen); empfohlenes Muster: Zählerstand vor der eigentlichen Aktion merken oder `Queries[^1]` auswerten. |
| `MapViewModelTests_Settings` (u. a. `SettingsLoadFailure_BlocksSearchWithMessage`), `MapViewModelTests_Offline`, `MapViewModelTests_AreaReloadTimer`, `MapViewModelTests_Failures`, `MapViewModelTests_Sort`, `MapViewModelTests_SearchOverlay`, `MapViewModelTests_OpenStation`, `MapViewModelTests_Privacy`, `MapViewModelTests_Route*` (übrige) | Anfangszustand nach dem Erscheinen ändert sich (`_lastResult`/`ShowEmptyState`/`StatusMessage`/`Stations` können bereits befüllt sein) — beim Testlauf prüfen und gegebenenfalls anpassen. |

### E2E-Tests (Pflicht)

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Erstmaliges Öffnen der Karte lädt Stationen am Teststandort | `MapE2ETests_AutoLoad` (neu): `FirstOpen_LoadsStationsAtTestLocation` | Nach `NavigateToTab("Karte")` erscheinen Alpha/Beta (`WaitForStationNames`), `Server.ListRequests == 1`, `LastListRadius == 5`. |
| Ergebnis bleibt beim Hin- und Hernavigieren erhalten | `MapE2ETests_AutoLoad`: `TabSwitch_PreservesResultWithoutNewRequest` | Wechsel „Karte" → „Favoriten" → „Karte": Stationsnamen/Markierungen unverändert, `Server.ListRequests` unverändert. |
| Ergebnis bleibt bei Rückkehr aus der Detailansicht erhalten | `MapE2ETests_AutoLoad`: `ReturnFromDetail_PreservesResult` | Detail öffnen und zurück: Liste/Karte unverändert, kein neuer `ListRequest`. |
| `GpsUsage.Never`: Startsuche unterbleibt still | `MapE2ETests_AutoLoad`: `GpsNever_FirstOpen_SkipsAutoSearch` | Bei „Nie" bleibt die Karte beim ersten Öffnen unberührt: `Server.TotalRequests == 0`, keine Standort-Statusmeldung, kein Leerzustand (entschieden: keine Suche, kein Hinweis). |
| Leeres Gebiet: Leerzustand statt Wiederholung | `MapE2ETests_AutoLoad`: `FirstOpen_WithoutStations_ShowsEmptyState` | Server ohne Stationen am Teststandort → `Search.EmptyState` sichtbar, kein zweiter `ListRequest` beim erneuten Erscheinen. |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SearchE2ETests_Basic.OpeningSearch_DoesNotQueryBeforeSubmit` | Prüft `TotalRequests == 0` nach dem Öffnen — durch die Startsuche unhaltbar; wird durch `FirstOpen_LoadsStationsAtTestLocation` ersetzt bzw. invertiert. |
| `SearchE2ETests_Basic.Search_DefaultRadius_ShowsNearStationsWithPricesAgeAndHints` | Nach `OpenSearch` existieren bereits Ergebnisse; `Submit` bedient denselben Cache-Schlüssel (`ListRequests` bleibt 1) — Assertions auf Reihenfolge/Zähler prüfen. |
| `MapE2ETests_SearchOverlay` (`Assert.Equal(0, Server.TotalRequests)`) | Öffnen der Karte sendet bereits eine Anfrage. |
| `RouteE2ETests_Cancel` (`Assert.Equal(0, Server.ListRequests)`) | Die Startsuche sendet bereits einen `ListRequest`, bevor die Routensuche abgebrochen wird. |
| `MapE2ETests_AreaReload`, `MapE2ETests_Markers`, `MapE2ETests_Navigation`, `MapE2ETests_Panel`, `MapE2ETests_View`, `RouteE2ETests_Basic`, `RouteE2ETests_Distance` | `ListRequests`/`TotalRequests`-Zähler bzw. der Zeitpunkt sichtbarer Meldungen verschieben sich um die Startsuche; gleiche Schlüssel werden vom Cache dedupliziert, abweichende Abfragen (anderer Radius/Ort) erhöhen den Zähler — je Test prüfen. |
| `SearchE2ETests_GpsNever` | Voraussichtlich unverändert: Die Tests stellen `GpsUsage.Never` ein, sodass die Startsuche gar nicht erst ausgelöst wird (keine zusätzlichen Requests, keine vorzeitige Meldung); trotzdem beim Testlauf verifizieren. |

## Offene Punkte

Keine — alle zuvor offenen Punkte wurden vom Anwender entschieden und sind in den Plan eingearbeitet:

1. `GpsUsage.Never`: **keine** Startsuche — sie unterbleibt still ohne `DisabledBySetting`-Hinweis (Ablauf „Deaktivierte Standortnutzung", Designentscheidung „`GpsUsage.Never`").
2. Berechtigungsdialog beim erstmaligen Öffnen: akzeptiert.
3. Kombination mit „In der Nähe": der Such-Cache des `FuelPriceService` genügt; kein geteilter `LocationResult`-Halter.
4. Abgebrochene Startsuche: läuft beim nächsten Erscheinen erneut (`_autoSearchDone` erst nach sichtbarem Ausgang).
5. Radius der Startsuche: `SearchRadius.Default` (5 km).
6. Abschaltbarkeit: keine neue Einstellung.
7. Sichtbare Kennzeichnung: keine — die Startsuche ist von einer manuellen Suche ununterscheidbar.
