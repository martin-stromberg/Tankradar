# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basisbranch: `origin/staging`, Merge-Base `bfca445`. Das steht so im Reflog („Created from origin/staging“) und in `review-code.2.md`. Geprüft wurden die uncommitteten Änderungen und die neuen, noch nicht versionierten Dateien. Der einzige Commit auf dem Branch (`221e0d4`) enthält nur den Plan. Die Unit-Tests zu Nachladen, Sperre und `MapAreaQuery` (29 Tests, Filter `AreaReload|MapAreaQuery`) laufen grün.

### Stand der Befunde aus `review-code.1.md` und `review-code.2.md`

| Befund | Stand |
|---|---|
| Durchlauf 1, alle Befunde | Behoben (bereits in `review-code.2.md` bestätigt) |
| Die Stile `MapOverlayButton`/`MapOverlayBadge` in `MapPage.xaml` doppeln `MapControlButton`/`MapBadge` | Behoben. `MapControlButton` und `MapBadge` liegen jetzt in `Resources/MapStyles.xaml`. `MapPage.xaml` und `StationMapView.xaml` verwenden nur noch diese Stile, lokale Kopien gibt es keine mehr. |
| Zwei XML-Präfixe (`resources`, `palette`) für denselben Namespace in `MapPage.xaml` | Behoben. Es gibt nur noch `resources:`. |
| `OnViewportChanged_DisablesButtonWithHint_WhenTooLarge` prüft auch das Heranzoomen | Behoben. Der Fall steht jetzt im eigenen Test `OnViewportChanged_ClearsTooLargeHint_AfterZoomIn`. |
| `MapAreaQueryTests_Radius`: mehrere Datenfälle je `[Fact]` | Behoben. `From_AllowsMaximumRadius` ist ein eigener Test, `From_ReturnsNull_WhenRadiusExceeds25Km` und `From_UsesMinimumOneKilometer` sind jetzt `[Theory]`. |

### Neue Teile in diesem Durchlauf, ohne Befund

- **Zeitgeber in `MapViewModel.AreaReload.cs`, Threading und Race Conditions:** `StartAreaReloadTimer` merkt sich beim Start den `SynchronizationContext`. Das passiert nach `await … ConfigureAwait(true)`, also auf dem UI-Thread. Jeder Tick wird über `Post` auf diesen Kontext zurückgeführt. Danach laufen alle Zugriffe auf `_areaReloadTimer`, `_lastAreaReloadAt` und `ChangeCanExecute` auf dem UI-Thread. Zwei Fälle sind harmlos:
  - Ein Tick eines alten Zeitgebers, der nach `Dispose` noch eingereiht ist, aktualisiert nur den Hinweis.
  - `RefreshAreaReloadBlock` gibt einen neueren Zeitgeber nur frei, wenn keine Sperre mehr besteht. Ein neuer Zeitgeber entsteht aber immer zusammen mit einer neuen Sperre.

  Die Sperre wird gesetzt, bevor die Anfrage gesendet wird. Auch eine abgebrochene Abfrage zählt also mit. Bei „höchstens eine Abfrage je Minute“ ist das die sichere Seite.
- **`ManualTimeProvider.CreateTimer`:** Die Zeitgeber feuern in `Advance` synchron. Weil über eine Kopie der Liste (`ToList()`) iteriert wird, darf sich ein Zeitgeber in seinem eigenen Rückruf freigeben. `FireDue` unterscheidet einmalige und periodische Zeitgeber richtig.
- **Animation in `StationBottomSheet.xaml.cs`:** Kommen Umschaltungen schnell hintereinander, verwirft der Zähler `_toggleVersion` veraltete Läufe. Zu Beginn und am Ende eines Laufs ruft der Code `CancelAnimations` auf. `Task.WhenAny` mit `ToggleTimeout` stellt sicher, dass der Zielzustand auch ohne sichtbares Fenster erreicht wird.
- **`Resources/MapStyles.xaml`:** Die Stile sind ohne Doppelungen zusammengeführt. Die Abstände sind benannte Ressourcen, ihre Herleitung steht jeweils daneben.

## Befunde

### src/Tankradar.MAUI/ViewModels/MapViewModel.AreaReload.cs (MapViewModel)

- **Fehlerbehandlung / Lebensdauer des Zeitgebers** — `OnDisappearing` in `MapViewModel.cs` (ab Z. 453) meldet die Ereignisse ab und bricht die Suche ab. Den Zeitgeber `_areaReloadTimer` gibt es dort nicht frei, und `MapViewModel` hat auch kein `Dispose`. Nach dem Verlassen der Seite tickt der Zeitgeber noch bis zu 60 s lang jede Sekunde weiter. Er hält das ViewModel über den Rückruf am Leben und löst auf der unsichtbaren Seite `PropertyChanged` (`AreaReloadHint`) und `CanExecuteChanged` aus. Der Fehler bleibt zwar zeitlich begrenzt. Die Lebensdauer des Zeitgebers hängt damit aber an keinem Lebenszyklus der Seite, obwohl `OnDisappearing` sonst alle laufenden Vorgänge aufräumt.

  Empfehlung: In `MapViewModel.AreaReload.cs` eine Methode `StopAreaReloadTimer()` anlegen (`_areaReloadTimer?.Dispose(); _areaReloadTimer = null;`). `StartAreaReloadTimer` (Z. 206) und `RefreshAreaReloadBlock` (Z. 228–229) rufen sie statt des eigenen Codes auf, `OnDisappearing` ruft sie zusätzlich auf. In `OnAppearing` den Zeitgeber über `StartAreaReloadTimer()` neu starten, wenn `IsAreaReloadBlocked` gilt, und danach `UpdateAreaReloadHint()` aufrufen. Dazu einen Unit-Test schreiben: Nach `OnDisappearing` und `Clock.Advance(…)` ändert sich `AreaReloadHint` nicht mehr, und nach erneutem `OnAppearing` stimmt die Restzeit wieder.

- **Speculative Generality / öffentliche Eigenschaft nur für Tests** — `AreaReloadRemainingSeconds` (Z. 76–80) ist öffentlich und meldet `PropertyChanged`. Gebunden ist sie aber nirgends, weder in `MapPage.xaml` noch in einem Steuerelement. Lesend greifen nur `UpdateAreaReloadHint` und die Tests in `MapViewModelTests_AreaReloadBlock.cs` (Z. 38, 55, 75) darauf zu. Für die Oberfläche maßgeblich ist allein `AreaReloadHint`, das die Restzeit schon enthält.

  Empfehlung: `AreaReloadRemainingSeconds` als öffentliche Eigenschaft entfernen. In `UpdateAreaReloadHint` die Sekunden als lokale Variable berechnen und in `ResetAreaReload` die Zuweisung (Z. 156) streichen. Die Tests prüfen die Restzeit dann über den Hinweistext, z. B. `Assert.Equal(MapTexts.ReloadAreaWait(60), ViewModel.AreaReloadHint)` und `Assert.Null(ViewModel.AreaReloadHint)`. Bei Z. 55–56 ist das schon so.

### src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReloadBlock.cs (MapViewModelTests_AreaReloadBlock)

- **Testqualität (Weitergabe an den UI-Thread nicht abgedeckt)** — Bei echten Zeitgebern läuft in der App immer der Pfad `_areaReloadContext.Post(…)` in `OnAreaReloadTimerTick` (Z. 220). Die Tests decken diesen Pfad nicht ab. Sie verlassen sich darauf, dass xUnit in async-Tests seinen eigenen `AsyncTestSyncContext` setzt und dass dieselbe Instanz bei `Clock.Advance` noch `SynchronizationContext.Current` ist. Nur deshalb wird der Rückruf direkt ausgeführt und die Assertions direkt nach `Advance` greifen. Ändert sich das Verhalten des Testframeworks (z. B. mit xUnit v3) oder ruft ein Test `Advance` aus einem anderen Kontext auf, schlagen die Tests fehl. Ob ein Tick wirklich über den beim Start gemerkten Kontext läuft, prüft kein Test.

  Empfehlung: In `Unit/Support` einen kleinen Test-`SynchronizationContext` anlegen, der `Post`-Aufrufe in eine Warteschlange stellt und sie per `RunPending()` ausführt. Einen Test ergänzen, der die Nachladeabfrage unter diesem Kontext startet und dann `Advance` außerhalb des Kontexts aufruft (`SynchronizationContext.SetSynchronizationContext(null)`). Vor `RunPending()` muss der Hinweis unverändert sein, danach muss er die neue Restzeit zeigen. Die bestehenden Tests sollten den Kontext ausdrücklich setzen, statt sich auf den Kontext von xUnit zu verlassen.

## Geprüfte Dateien

- `src/Tankradar.MAUI/Models/Map/MapBounds.cs`
- `src/Tankradar.MAUI/Models/Map/MapFitMode.cs`
- `src/Tankradar.MAUI/Resources/MapStyles.xaml`
- `src/Tankradar.MAUI/Resources/MapStyles.xaml.cs`
- `src/Tankradar.MAUI/Resources/Texts/MapTexts.cs`
- `src/Tankradar.MAUI/Resources/Texts/SearchTexts.cs`
- `src/Tankradar.MAUI/Services/Map/MapAreaQuery.cs`
- `src/Tankradar.MAUI/Services/Map/MapViewport.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.AreaReload.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.Panels.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs`
- `src/Tankradar.MAUI/Views/Controls/MapSearchOverlay.xaml`
- `src/Tankradar.MAUI/Views/Controls/MapSearchOverlay.xaml.cs`
- `src/Tankradar.MAUI/Views/Controls/StationBottomSheet.xaml`
- `src/Tankradar.MAUI/Views/Controls/StationBottomSheet.xaml.cs`
- `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml`
- `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml.cs`
- `src/Tankradar.MAUI/Views/Controls/ViewportChangedEventArgs.cs`
- `src/Tankradar.MAUI/Views/MapPage.xaml`
- `src/Tankradar.MAUI/Views/MapPage.xaml.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_AreaReload.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_Layout.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_Markers.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_Navigation.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_Panel.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_SearchOverlay.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/MapE2ETests_View.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_FilterSort.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/RouteE2ETests_Settings.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETestBase.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_Address.cs`
- `src/Tankradar.Tests.Integration/Integration/AreaSearchMockServerTests_Flow.cs`
- `src/Tankradar.Tests.Unit/Unit/MapAreaQueryTests_Radius.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTestBase.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReloadBlock.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_MapView.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Panel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_SearchOverlay.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Settings.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Sort.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Bounds.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Math.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeFuelPriceService.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/ManualTimeProvider.cs`
