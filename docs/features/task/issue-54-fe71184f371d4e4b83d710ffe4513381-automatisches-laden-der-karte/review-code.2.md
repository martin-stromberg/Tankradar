# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### MapViewModel.cs (MapViewModel)

- **Fehlerbehandlung / Kopplung** — `MaybeStartAutoSearch` (Zeilen 512–521): Der neue Wiederholungsschutz `LastSearchTask is { IsCompleted: false }` verhindert zwar den Doppelstart, unterdrückt aber auch die vom Plan geforderte Wiederholung nach Abbruch, wenn `OnDisappearing` und `OnAppearing` innerhalb des Einstellungs-Ladefensters liegen. Ablauf: `OnAppearing` startet `AutoSearchOnceAsync` → `OnDisappearing` → `CancelSearch` setzt `_searchCancellation = null` und bricht das Token ab, der Lauf hängt aber weiter am deduplizierten `LoadSettingsAsync` → `OnAppearing` sieht `LastSearchTask` noch nicht abgeschlossen und startet **keinen** neuen Lauf → nach Ende des Ladens wirft `token.ThrowIfCancellationRequested()` (`GetSettingsOrReportAsync`, Zeile 903) die `OperationCanceledException`, der Lauf endet ohne Ausgang (`_autoSearchDone` bleibt `false`). Ergebnis: Die Seite ist sichtbar, die Karte bleibt leer, und kein weiterer Auslöser existiert — erst ein drittes `OnAppearing` holt die Startsuche nach. Der Plan-Ablauf „Abgebrochene Startsuche: Das nächste `OnAppearing` startet die Startsuche erneut" ist damit genau in dem Fenster verletzt, das der Guard adressiert. Der neue Test `MapViewModelTests_AutoLoad.ReappearingDuringSettingsLoad_DoesNotStartSecondAutoSearch` (Zeilen 224–243) zementiert das Verhalten sogar: Er endet mit `Assert.Empty(Location.Calls)`/`Assert.Empty(Prices.Queries)`, obwohl die Seite zuletzt erschienen ist — auf eine Wiederholung wird nicht geprüft.

  Empfehlung: Im Guard nur noch einen tatsächlich aktiven (nicht abgebrochenen) Lauf sperren — `LastSearchTask is { IsCompleted: false }` durch `_searchCancellation is not null` ersetzen. `CancelSearch` leert das Feld synchron; ein abgebrochener, noch abwickelnder Lauf blockiert dann nicht mehr, während eine laufende Suche (Startsuche, `SearchCommand`, `RunAreaReloadAsync` — alle setzen `_searchCancellation` synchron vor dem ersten `await`) weiterhin geschützt bleibt. Der Test ist anzupassen: Nach `await autoSearch` und einem erneuten Erscheinen (oder direkt beim Erscheinen im Ladefenster) muss ein neuer Startsuch-Lauf mit genau einem `Location.Calls`-/`Prices.Queries`-Eintrag nachgewiesen werden.

- **Doppelter Code** — `AutoSearchOnceAsync` (Zeilen 526–527 und 551–558) wiederholt das Abbruch-Grundgerüst von `RunGuardedSearchAsync` (Zeilen 563–564 und 586–596): `new CancellationTokenSource()` mit Zuweisung an `_searchCancellation`, `catch (OperationCanceledException) when (cancellation.IsCancellationRequested)` und im `finally` `if (IsCurrent(cancellation)) { _searchCancellation = null; cancellation.Dispose(); }`. Das Muster steht jetzt zweimal nahezu identisch in derselben Klasse.

  Empfehlung: Das Grundgerüst in kleine Hilfsmethoden auslagern (z. B. `BeginSearchRun()` für Erzeugung und Registrierung sowie `EndSearchRun(CancellationTokenSource)` für die `IsCurrent`-Prüfung, das Leeren und das Dispose) und in beiden Methoden verwenden; die spezifischen Teile (`IsBusy`, `IsSearchOverlayOpen`, `onFinished`, Statuszeile) bleiben in `RunGuardedSearchAsync`.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.AreaReload.cs` (Wechselwirkung `LastSearchTask`/`_searchCancellation`)
- `src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs` (Wechselwirkung `CancelSearch`/`RunGuardedSearchAsync`)
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelRouteTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeSettingsService.cs`
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
