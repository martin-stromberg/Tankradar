# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### MapViewModel.cs (MapViewModel)

- **Fehlerbehandlung / Vorbedingung gegen veralteten Stand** — `AutoSearchOnceAsync` (Zeile 530) prüft die `GpsUsage`-Vorbedingung über `GetSettingsOrReportAsync`, das bei bereits geladenen Einstellungen den Cache `_settings` zurückgibt (`_settings ?? await LoadSettingsAsync()`, Zeile 916) statt den in demselben `OnAppearing` gestarteten frischen Lauf (`LastSettingsTask = LoadSettingsAsync()`, Zeile 449) abzuwarten. Da `SettingsService.LoadAsync` in Produktion echt asynchron auf die Datenbank geht, ist `_settings` beim Vorlauf eines erneuten Erscheinens regelmäßig noch der **veraltete** Stand; die Deduplizierung über `_settingsLoad` wird vom `??`-Kurzschluss umgangen. Zwei Fehlrichtungen:
  1. **WhileInUse → Never:** Stand etwa nach einem abgebrochenen Startsuchlauf (`_lastResult is null`, `_autoSearchDone == false`) der Cache noch `WhileInUse`, während der Anwender die Ortung in den Einstellungen gerade auf „Nie“ gestellt hat, passiert die `AllowsLocation()`-Prüfung (Zeile 536) mit dem alten Wert und `SearchAsync` startet eine echte Standort- und Preisabfrage — obwohl die aktuelle Einstellung „Nie“ lautet. Je nachdem, ob die frische Laden-Continuation vor `RunSearchAsync` landet, kommt alternativ eine Standort-Statusmeldung (`DisabledBySetting`) ohne Nutzeraktion auf die Karte. Beides verletzt den Vertrag „bei deaktivierter Standortnutzung unterbleibt die Startsuche still — kein Abruf, keine Meldung“.
  2. **Never → WhileInUse:** Der im Plan vereinbarte Nachhol-Mechanismus („Wechselt die Einstellung in derselben Sitzung auf `Always`/`WhileInUse`, startet das nächste `OnAppearing` die normale Startsuche“) verstreicht eine Erscheinen-Runde: Der Vorlauf liest das zwischengespeicherte `Never` und endet still, obwohl der parallel laufende Ladevorgang bereits `WhileInUse` liefern würde.

  In den Unit-Tests bleibt das verborgen, weil `FakeSettingsService.LoadAsync` ohne `LoadGate` synchron durchläuft und `_settings` daher vor `MaybeStartAutoSearch` frisch ist; dort ist die Richtung WhileInUse → Never zusätzlich ungetestet.

  Empfehlung: Der Vorlauf soll den für dieses Erscheinen laufenden Ladevorgang tatsächlich abwarten statt den `_settings`-Kurzschluss zu nutzen — z. B. `var settings = await LoadSettingsAsync().ConfigureAwait(true);` (dedupliziert über `_settingsLoad`, liefert den frischen Stand) gefolgt von `cancellation.Token.ThrowIfCancellationRequested()`; der `null`-Fall (Ladefehler) ist bereits über `StatusMessage`/`LoadSettingsCoreAsync` abgedeckt. Alternativ `GetSettingsOrReportAsync` um einen Modus „immer den anstehenden Lauf abwarten“ erweitern. Einen Test ergänzen, der den Wechsel WhileInUse → Never vor dem zweiten Erscheinen mit geöffnetem `LoadGate` bzw. asynchroner Einstellungsladung nachstellt (kein `Location.Calls`-/ `Prices.Queries`-Eintrag, keine Meldung).

## Verifizierte Fixes der Vorbeurteilung (review-code.2.md)

Beide Befunde der letzten Runde wurden korrekt und vollständig behoben:

- **Guard in `MaybeStartAutoSearch` (Zeilen 512–521):** `_searchCancellation is not null` sperrt nur tatsächlich aktive Läufe. `CancelSearch` (Zeilen 845–859) leert das Feld synchron; ein abgebrochener, noch abwickelnder Lauf blockiert die Wiederholung nicht mehr. Alle Laufarten (Startsuche über `AutoSearchOnceAsync`, manuelle Suche und Routensuche über `SearchAsync`, Nachladen über `RunAreaReloadAsync`) registrieren ihre `CancellationTokenSource` synchron vor dem ersten `await` — zwischen `CancelSearch` und `BeginSearchRun` liegt kein `await`, auf dem UI-Thread also atomar. Der umbenannte Test `ReappearingDuringSettingsLoad_RestartsAbortedAutoSearch` (`MapViewModelTests_AutoLoad.cs:224–250`) weist den Neustart jetzt nach (`Assert.NotSame(autoSearch, retry)`, `Settings.LoadCount == 1` für die Deduplizierung, genau ein `Location.Calls`-/`Prices.Queries`-Eintrag nach dem Öffnen des Gates); der abgebrochene Lauf kann `SearchAsync` nicht mehr erreichen, weil `GetSettingsOrReportAsync` vorher `ThrowIfCancellationRequested` ausführt.
- **Extraktion `BeginSearchRun`/`EndSearchRun` (Zeilen 820–838):** Die Abbruch-Semantik von `RunGuardedSearchAsync` ist unverändert — `IsCurrent`-Prüfung, `onFinished`, `IsBusy` und Dispose liegen in derselben Reihenfolge wie zuvor; die `onFinished`-Callbacks (`RouteProgressText = null`, `IsAreaReloading = false`) und die `IsBusy`-`ChangeCanExecute`-Kette lesen `_searchCancellation` nicht, sodass die verschobene Feld-Leerung folgenlos ist. Keine Doppel-Dispose- oder Leak-Pfade: Ein abgebrochener Lauf wird von `CancelSearch` disposed und sein `EndSearchRun` ist wegen `!IsCurrent` ein No-Op; ein superseded Lauf darf die Registrierung des Nachfolgers nicht anfassen (ReferenceEquals-Vergleich). Zugriffe auf die bereits disposed Quelle in den `catch`-Filtern (`cancellation.IsCancellationRequested`, `IsCurrent`) sind zulässig; das Token wurde vor der Entsorgung als `CancellationToken`-Wert erfasst und bleibt für `ThrowIfCancellationRequested` nutzbar.

## Hinweise zur Prüfung

- Release-Build `Tankradar.Tests.Unit` (Windows-Target): 0 Fehler, 0 Warnungen.
- `dotnet test --filter MapViewModelTests_AutoLoad|MapViewModelTests_Concurrency`: 15/15 bestanden.
- Übriger Diff (angepasste Zähler-Assertions, `ResetSearchCounters`, `AppearAndAutoSearchAsync`, `WaitForAutoSearch`, `RouteSearchMockServerTestBase`-Adressmodus-Unterdrückung, neue E2E-/Integrationstests) stichprobenartig geprüft — konsistent, keine weiteren Befunde.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.AreaReload.cs` (Wechselwirkung `RunGuardedSearchAsync`/`CancelSearch`)
- `src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs` (Wechselwirkung `RunGuardedSearchAsync`/`IsCurrent`/`CancelSearch`)
- `src/Tankradar.MAUI/Services/SettingsService.cs` (Asynchronizität des Einstellungsladens)
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelRouteTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeSettingsService.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeLocationService.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeFuelPriceService.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeRouteSearchAccess.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AutoLoad.cs` (neu)
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Concurrency.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Search.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_SearchOverlay.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_MapView.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Panel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Settings.cs`
- `src/Tankradar.Tests.Integration/Integration/AutoSearchMockServerTests_Cache.cs` (neu)
- `src/Tankradar.Tests.Integration/Integration/Support/SearchMockServerTestBase.cs`
- `src/Tankradar.Tests.Integration/Integration/Support/RouteSearchMockServerTestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_AutoLoad.cs` (neu)
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETestBase.cs`
