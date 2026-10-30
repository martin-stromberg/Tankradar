# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basisbranch: `origin/staging` (laut Reflog „Created from origin/staging“). Geprüft wurden die uncommitteten Änderungen und die neuen Dateien auf dem Branch (der einzige Branch-Commit `221e0d4` enthält nur den Plan).

## Befunde

### src/Tankradar.MAUI/ViewModels/MapViewModel.cs (MapViewModel)

- **God-Klasse** — Die Datei hat jetzt rund 1.200 Zeilen. Das Nachladen des sichtbaren Ausschnitts kommt als eigener, klar abgrenzbarer Bereich dazu: die Felder `_currentViewport`, `_searchedViewport`, `_lastAreaReloadAt`, `_isAreaTooLarge`, `_areaReloadRemainingSeconds`, `_reloadVisibleAreaCommand`, die Eigenschaften `ShowAreaReloadButton`, `AreaReloadHint`, `HasAreaReloadHint`, `IsAreaReloading`, `AreaReloadRemainingSeconds`, `MapFitMode`, `ReloadVisibleAreaCommand` und die Methoden `OnViewportChanged`, `IsAreaReloadMode`, `CanReloadVisibleArea`, `IsAreaReloadBlocked`, `GetAreaReloadRemaining`, `UpdateAreaReloadState`, `IsSameView`, `ReloadVisibleArea`, `RunAreaReloadAsync`, `ReloadVisibleAreaAsync`. Die Routensuche liegt dagegen schon in einem eigenen Partial (`MapViewModel.Route.cs`).

  Empfehlung: Den Nachlade-Bereich in eine neue Partial-Datei `MapViewModel.AreaReload.cs` verschieben (Felder, Eigenschaften, Methoden), so wie `MapViewModel.Route.cs` aufgebaut ist. Dasselbe gilt für Panel und Overlay (`IsBottomSheetExpanded`, `IsSearchOverlayOpen`, Toggle/Open/Close-Befehle): Sie können in `MapViewModel.Panels.cs` oder in dieselbe neue Datei.

- **Doppelter Code** — `ReloadVisibleAreaAsync` wiederholt zum vierten Mal dieselben Zeilen zum Laden der Einstellungen und Auswählen der Spritsorten: `_settings ?? await LoadSettingsAsync()`, dann bei `null` `StatusMessage = SettingsTexts.LoadFailed`, dann `settings.FuelTypes.Where(selection => selection.IsSelected).Select(selection => selection.FuelType).ToList()`. Dieselben Zeilen stehen auch in `RunSearchAsync` (Z. ~772/790), in der Methode bei Z. ~977 und in `MapViewModel.Route.cs` (Z. 423/449).

  Empfehlung: Eine private Hilfsmethode `static IReadOnlyList<FuelType> SelectedFuelTypes(AppSettings settings)` einführen, idealerweise auch `Task<AppSettings?> GetSettingsOrReportAsync()` (lädt die Einstellungen und setzt die Statusmeldung, wenn das fehlschlägt), und alle vier Stellen darauf umstellen.

- **Temporäres Feld / versteckte Steuerung** — `ApplyResult` entscheidet über den Einpassmodus und darüber, ob der Nachlade-Zustand zurückgesetzt wird, anhand der UI-Zustandseigenschaft `IsAreaReloading`. Diese wird in `RunAreaReloadAsync` vor dem Aufruf gesetzt und im `onFinished`-Callback bzw. in `CancelSearch` zurückgesetzt. Damit hängt das Verhalten von `ApplyResult` von einem Seiteneffekt ab, der an anderer Stelle gesetzt wird.

  Empfehlung: Den Modus ausdrücklich übergeben, z. B. `ApplyResult(StationSearchResult result, MapOrigin? origin, MapFitMode fitMode)`. Neue Suchen übergeben `FitToResult`, `ReloadVisibleAreaAsync` übergibt `KeepViewport`. `IsAreaReloading` dient dann nur noch der Anzeige.

- **Fehlende Kapselung** — `UpdateAreaReloadState` baut mit `MapAreaQuery.From(viewport!.GetBounds(), [])` eine vollständige Abfrage, nur um zu prüfen, ob der Ausschnitt zu groß ist (Ergebnis `null`). Das nutzt den Rückgabewert einer Fabrikmethode als Größenprüfung. Außerdem steht dort `viewport!` mit Null-Forgiving-Operator, obwohl `show` die Prüfung schon enthält.

  Empfehlung: In `MapAreaQuery` eine eigene Methode `public static bool IsTooLarge(MapBounds bounds)` anbieten (sie nutzt `RadiusKmOf`) und in `UpdateAreaReloadState` verwenden. Das `!` entfällt, wenn `viewport` innerhalb eines `if (viewport is not null && …)`-Blocks verwendet wird.

### src/Tankradar.MAUI/ViewModels/MapViewModel.Route.cs (MapViewModel)

- **Kopplung / falsche Verantwortlichkeit** — `OnOwnPropertyChanged` ist laut Kommentar und Inhalt für die Sperre der Routen-Chips während einer Suche zuständig. Jetzt ruft er zusätzlich `_reloadVisibleAreaCommand.ChangeCanExecute()` auf (Z. 555). Ob der Nachlade-Befehl ausführbar ist, hängt damit unsichtbar an einem Handler im Routen-Partial.

  Empfehlung: Die Aktualisierung von `CanExecute` in den Nachlade-Bereich verlegen, z. B. in einen eigenen `PropertyChanged`-Zweig für `IsBusy` in `MapViewModel.AreaReload.cs` oder in eine Methode `OnBusyChanged()`, die beide Bereiche aufrufen. `OnOwnPropertyChanged` soll nur noch Routenzustand behandeln.

### src/Tankradar.MAUI/Views/Controls/StationBottomSheet.xaml.cs (StationBottomSheet)

- **Logik in der View statt im ViewModel** — `UpdateCount` und `OnViewModelPropertyChanged` bilden den Text der Kopfzeile (`SearchTexts.FormatStationCount` bzw. `SearchTexts.NoResults`) im Code-Behind. Dazu abonniert die View `PropertyChanged` des ViewModels und setzt `CountLabel.Text` direkt. Damit ist diese Logik nicht per Unit-Test prüfbar. Der E2E-Test `MapE2ETests_AreaReload.ReloadArea_UpdatesCountAndMarkers_KeepsViewport` (Z. 81) baut die Formatierung deshalb sogar nach.

  Empfehlung: Im `MapViewModel` eine Eigenschaft `StationCountText` anlegen (benachrichtigt bei Änderung von `TotalStationCount`), im XAML `Text="{Binding StationCountText}"` binden, das Abonnement samt `UpdateCount` aus dem Code-Behind entfernen und einen Unit-Test für die drei Fälle (0, 1, n) ergänzen.

### src/Tankradar.MAUI/Views/Controls/StationBottomSheet.xaml und MapSearchOverlay.xaml

- **Doppelter Code** — Der Stil `SearchChipButton` steht identisch in beiden neuen Steuerelementen. Dazu kommen sechs fast gleiche Chip-`DataTemplate`s (`FilterChipTemplate`, `SortChipTemplate`, `ModeChipTemplate`, `RadiusChipTemplate`, `RouteDistanceChipTemplate`, `RouteStartChipTemplate`), die sich nur im Präfix der `AutomationId` und teils in `IsEnabled` unterscheiden. Der gleiche `DataTrigger` für den ausgewählten Zustand steht sechsmal da.

  Empfehlung: `SearchChipButton` mit dem `DataTrigger` für `IsSelected` als impliziten bzw. benannten Stil in die gemeinsamen Ressourcen verschieben (`Resources/Styles`) und in beiden Steuerelementen verwenden. Die Templates sollen dann nur noch `Text`, `Command`, `Hint`, `IsEnabled` und `AutomationId` setzen.

### src/Tankradar.MAUI/Views/MapPage.xaml und Views/Controls/StationMapView.xaml

- **Hardcodierte Werte / versteckte Kopplung** — Die Lage der Overlay-Elemente hängt von Pixelwerten ab, die zum jeweils anderen Steuerelement passen müssen. Der Zähler in `StationMapView` hat `Margin="8,60,8,8"`, damit er unter der Suchschaltfläche von `MapPage` liegt (44 px + 8 + 8). Die Legende in `MapPage` hat `Margin="8,8,60,8"`, damit sie links der Zoom-Schaltflächen der Karte liegt. Die Nachlade-Schaltfläche hat `Margin="8,8,8,44"`, damit sie über der Quellenangabe liegt. Ändert sich eine Größe, verrutschen die anderen Elemente unbemerkt.

  Empfehlung: Die Abstände als benannte Ressourcen in der Ressourcen-Datei mit `MinimumTouchTarget` ablegen, z. B. `MapOverlayInset` = 8 und `MapOverlayStackOffset` = `MinimumTouchTarget` + 2 × Inset, und dort ansetzen. Oder die Kopplung umgehen: Den Zähler über eine Eigenschaft von `StationMapView` (z. B. `TopInset`) von der Seite aus verschieben lassen.

### src/Tankradar.MAUI/Views/Controls/StationMapView.xaml.cs (StationMapView)

- **Toter Code** — Die neue öffentliche Eigenschaft `Viewport` wird nirgends verwendet. Der Ausschnitt kommt ausschließlich über `ViewportChangedEventArgs.Viewport` an.

  Empfehlung: Die Eigenschaft `Viewport` entfernen (oder, falls sie gebraucht wird, einen Verwender und einen Test ergänzen).

### src/Tankradar.MAUI/Resources/Texts/MapTexts.cs (MapTexts)

- **Toter Code** — `MapTexts.ViewHeading` („Ansicht“) war die Überschrift der entfernten Chips „Liste/Karte“ in `MapPage.xaml` und wird jetzt nirgends mehr verwendet. `SettingsTexts.ViewHeading` ist davon nicht betroffen.

  Empfehlung: Die Konstante `MapTexts.ViewHeading` entfernen.

### src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Sort.cs (MapViewModelTests_Sort)

- **Testqualität (Implementierungsdetail / Tautologie)** — Die neue Zeile `Assert.Equal(ViewModel.HasMore && ViewModel.IsBottomSheetExpanded, ViewModel.ShowMoreVisible);` rechnet die Formel der Eigenschaft im Test nach. Sie ist immer wahr und prüft kein fachliches Verhalten.

  Empfehlung: Durch eine Prüfung mit festem Erwartungswert ersetzen, die zum Testfall passt (z. B. `Assert.True(ViewModel.ShowMoreVisible)`, wenn noch Tankstellen übrig sind), oder die Zeile streichen. `ShowMoreVisible` deckt `MapViewModelTests_Panel.ShowMore_OnlyVisible_WhenPanelExpanded` bereits ab.

### src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs, MapViewModelTests_SearchOverlay.cs, MapViewportTests_Bounds.cs, MapAreaQueryTests_Radius.cs

- **Testqualität (mehrere fachliche Fälle je Test)** — Mehrere neue Tests prüfen nacheinander verschiedene Fälle mit jeweils eigenem Act-Schritt, ohne klare Arrange-Act-Assert-Trennung:
  - `OnViewportChanged_ShowsReloadButton_WhenMovedFromLastSearch`: Erscheinen, Verschwinden nach dem Nachladen, nur Größenänderung, Verschieben, Rückkehr und Zoom (sechs Act-Schritte).
  - `ReloadVisibleArea_QueriesOnce_ReplacesResult_KeepsViewport`: prüft am Ende zusätzlich, dass eine neue Suche wieder einpasst.
  - `OnViewportChanged_HidesButton_InRouteMode`: prüft zusätzlich den Adressmodus.
  - `MapViewModelTests_SearchOverlay.Search_ClosesOverlay_OnStart`: ungültige Eingabe lässt den Dialog offen, gültige Eingabe schließt ihn.
  - `MapViewportTests_Bounds.GetBounds_ContainsCenterAndCorners`: prüft zusätzlich `MapBounds.ToString()`.
  - `MapAreaQueryTests_Radius.From_UsesCenterAndHalfDiagonal`: zwei unabhängige Ausschnitte.

  Empfehlung: Die Tests nach Fällen aufteilen, z. B. `OnViewportChanged_HidesButton_AfterReloadOfSameArea`, `OnViewportChanged_IgnoresResizeOnly`, `OnViewportChanged_ShowsButton_AfterZoom`, `Search_NewSearchAfterReload_FitsToResult`, `OnViewportChanged_ShowsButton_InAddressMode`, `Search_InvalidInput_KeepsOverlayOpen`, `MapBounds_ToString_HidesCoordinates`. Für gleichartige Datenfälle (`From_UsesCenterAndHalfDiagonal`) `[Theory]` mit `[InlineData]` verwenden.

## Geprüfte Dateien

- `src/Tankradar.MAUI/Models/Map/MapBounds.cs`
- `src/Tankradar.MAUI/Models/Map/MapFitMode.cs`
- `src/Tankradar.MAUI/Resources/Texts/MapTexts.cs`
- `src/Tankradar.MAUI/Resources/Texts/SearchTexts.cs`
- `src/Tankradar.MAUI/Services/Map/MapAreaQuery.cs`
- `src/Tankradar.MAUI/Services/Map/MapViewport.cs`
- `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`
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
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_MapView.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Panel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_SearchOverlay.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Settings.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Sort.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Bounds.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Math.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeFuelPriceService.cs`
