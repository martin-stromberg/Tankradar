# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### MapViewModel.cs (MapViewModel)

- **Fehlerbehandlung / Kopplung** — `AutoSearchOnceAsync` (Zeilen 522–541): Die Auslösebedingungen werden nur synchron in `MaybeStartAutoSearch` (Zeilen 512–520) geprüft und nach dem asynchronen `GetSettingsOrReportAsync` nicht erneut. Die Vorlaufphase läuft mit `CancellationToken.None` und außerhalb von `_searchCancellation`, sodass `CancelSearch` (`OnDisappearing`, `ChangeMode`, neue Suche) sie nicht erreicht. Konkrete Folgen in dem (realen, da `SettingsService.LoadAsync` auf die Datenbank geht) Fenster zwischen Guard und `await SearchAsync()`:
  1. **Moduswechsel:** Wählt der Anwender im Suchdialog „Adresse“, läuft `ChangeMode` → `CancelSearch` (wirkungslos, kein Token) → die Startsuche ruft `SearchAsync` unter `SearchMode.Address` auf → `AddressInput.Validate("")` setzt `StatusMessage` (`AddressEmpty`) ohne Nutzeraktion; `_autoSearchDone` wird anschließend `true`, ohne dass je am Standort gesucht wurde.
  2. **Verschwinden der Seite:** `OnDisappearing` während des Einstellungsladens bricht die Startsuche nicht ab (entgegen dem sonst gültigen Abbruchvertrag); `SearchAsync` läuft danach verdeckt weiter und setzt `_autoSearchDone`.
  3. **Doppelstart:** `OnDisappearing` + `OnAppearing` innerhalb des Ladefensters startet einen zweiten `AutoSearchOnceAsync`-Lauf, weil der Guard nur „erfolgt“ (`_autoSearchDone`), nicht „läuft“ prüft → zwei parallele Instanzen; die zweite verdrängt die erste erst innerhalb von `SearchAsync` (zusätzlicher Standortabruf, ggf. eine noch abgebrochene Preisanfrage).

  Empfehlung: In `AutoSearchOnceAsync` nach dem Laden der Einstellungen die Bedingungen erneut prüfen (mindestens `_searchMode == SearchMode.CurrentLocation` und sichtbarer Zustand, z. B. `_subscribed`) sowie die Vorlaufphase abbrechbar machen (eigenes `CancellationTokenSource` in `_searchCancellation` registrieren bzw. einen echten Token an `GetSettingsOrReportAsync` übergeben und `ThrowIfCancellationRequested` vor `SearchAsync`). Zusätzlich im Guard von `MaybeStartAutoSearch` einen laufenden Lauf berücksichtigen (z. B. `LastSearchTask is { IsCompleted: false }`), damit kein zweiter Startsuch-Lauf entsteht.

- **Doppelter Code** — `AutoSearchOnceAsync` (Zeilen 527 und 540): `_autoSearchDone = _lastResult is not null || HasStatusMessage;` steht identisch in beiden Pfaden. Im `settings is null`-Zweig ist die Bedingung zudem stets wahr, weil `GetSettingsOrReportAsync` bei `null` die `StatusMessage` setzt.

  Empfehlung: Die Auswertung einmal am Methodenende vornehmen (Early-Return nur für `!settings.GpsUsage.AllowsLocation()`), oder im Fehlerzweig explizit `_autoSearchDone = true` schreiben, wenn die generische Form der Symmetrie wegen beibehalten werden soll.

### MapViewModelTests_Panel.cs (MapViewModelTests_Panel)

- **Testqualität** — `DefaultView_List_OpensPanelExpanded` (Zeile 42): Das Arrange stellt unkommentiert `GpsUsage = GpsUsage.Never` ein, ausschließlich um die Startsuche zu unterdrücken und `HasMapContent == false` zu erhalten. Für den Leser ist nicht ersichtlich, warum eine Standort-Einstellung für einen Panel-Test relevant ist — anders als die übrigen Unterdrückungsstellen (`SelectAddressModeAsync`, `PrepareRouteAsync`, `CreateRouteViewModelAsync`), die alle einen erklärenden Kommentar tragen.

  Empfehlung: Entweder denselben erklärenden Kommentar wie an den anderen Stellen ergänzen („`GpsUsage.Never` unterdrückt die Startsuche, damit der Ausgangszustand unberührt bleibt") oder die Startsuche über `AppearAndAutoSearchAsync()` abwarten und die Erwartung `HasMapContent` an den gefüllten Zustand anpassen.

### MapViewModelTests_Address.cs / MapViewModelRouteTestBase.cs

- **Doppelter Code** — Identischer Block `Location.Calls.Clear(); Prices.Queries.Clear();` mit identischem Kommentar in `MapViewModelTests_Address.SelectAddressModeAsync` (Zeilen 20–21) und `MapViewModelRouteTestBase.PrepareRouteAsync` (Zeilen 33–34). Beide Klassen erben von `MapViewModelTestBase`.

  Empfehlung: Eine geschützte Hilfsmethode (z. B. `ResetSearchCounters()`) auf `MapViewModelTestBase` auslagern und an beiden Stellen aufrufen.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelRouteTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AutoLoad.cs` (neu)
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Address.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReloadBlock.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Chips.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Concurrency.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_FilterSort.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_MapView.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Panel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Route.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_RouteAvailability.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Search.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_SearchOverlay.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Settings.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Sort.cs`
- `src/Tankradar.Tests.Integration/Integration/AutoSearchMockServerTests_Cache.cs` (neu)
- `src/Tankradar.Tests.Integration/Integration/AddressSearchMockServerTests_Flow.cs`
- `src/Tankradar.Tests.Integration/Integration/AddressSearchPrivacyTests_Persistence.cs`
- `src/Tankradar.Tests.Integration/Integration/AreaSearchMockServerTests_Flow.cs`
- `src/Tankradar.Tests.Integration/Integration/HomePrivacyTests_Persistence.cs`
- `src/Tankradar.Tests.Integration/Integration/RoutePrivacyTests_Persistence.cs`
- `src/Tankradar.Tests.Integration/Integration/SearchMockServerTests_Flow.cs`
- `src/Tankradar.Tests.Integration/Integration/SearchMockServerTests_Offline.cs`
- `src/Tankradar.Tests.Integration/Integration/SearchMockServerTests_RealFormat.cs`
- `src/Tankradar.Tests.Integration/Integration/SearchPrivacyTests_Logging.cs`
- `src/Tankradar.Tests.Integration/Integration/SearchPrivacyTests_Persistence.cs`
- `src/Tankradar.Tests.Integration/Integration/Support/RouteSearchMockServerTestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_AutoLoad.cs` (neu)
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_Layout.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_SearchOverlay.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_View.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_Basic.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_Cancel.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_Distance.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_Errors.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_FilterSort.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_GpsNever.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_Address.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_AddressErrors.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_Basic.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_Errors.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_LargeResult.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_Radius.cs`
