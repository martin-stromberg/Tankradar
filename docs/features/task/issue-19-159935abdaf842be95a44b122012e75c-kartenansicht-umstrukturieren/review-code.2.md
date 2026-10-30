# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basisbranch: `origin/staging`. Laut Reflog wurde der Branch mit „Created from origin/staging“ angelegt, die Merge-Base ist `bfca445`. Geprüft wurden die uncommitteten Änderungen und die neuen, noch nicht versionierten Dateien. Der einzige Commit auf dem Branch (`221e0d4`) enthält nur den Plan.

### Stand der Befunde aus `review-code.1.md`

| Befund aus Durchlauf 1 | Stand |
|---|---|
| God-Klasse `MapViewModel` | Behoben. Das Nachladen liegt jetzt in `MapViewModel.AreaReload.cs`, Panel und Suchdialog in `MapViewModel.Panels.cs`. |
| Doppelter Code beim Laden der Einstellungen und Auswählen der Spritsorten | Behoben. Alle Stellen nutzen jetzt `GetSettingsOrReportAsync(clearResult, token)` und `SelectedFuelTypes(settings)`. |
| `ApplyResult` hängt vom Seiteneffekt `IsAreaReloading` ab | Behoben. Der Modus wird jetzt ausdrücklich übergeben: `ApplyResult(result, origin, MapFitMode)`. |
| Größenprüfung über die Fabrikmethode `MapAreaQuery.From`, dazu `viewport!` | Behoben. Es gibt jetzt `MapAreaQuery.IsTooLarge(bounds)`, das Pattern `_currentViewport is { } viewport` ersetzt das `!`. |
| `OnOwnPropertyChanged` (Routen-Partial) steuert den Nachlade-Befehl | Behoben. Das übernimmt jetzt `OnAreaReloadPropertyChanged` in `MapViewModel.AreaReload.cs`. |
| Text der Trefferanzahl wird im Code-Behind von `StationBottomSheet` gebildet | Behoben. `MapViewModel.StationCountText` ist gebunden und hat Unit-Tests für 0, 1 und n Tankstellen. |
| Chip-Stil und `DataTrigger` doppelt in beiden Steuerelementen | Behoben. `SearchChipButton` und `SearchChoiceChip` liegen jetzt in `Resources/MapStyles.xaml`. |
| Pixelabstände zwischen den Overlay-Elementen hart codiert | Behoben. Die Abstände sind jetzt benannte `Thickness`-Ressourcen in `MapStyles.xaml`, jeweils mit Herleitung. |
| Ungenutzte Eigenschaft `StationMapView.Viewport` | Behoben (entfernt). |
| Ungenutzte Konstante `MapTexts.ViewHeading` | Behoben (entfernt). |
| Tautologische Prüfung von `ShowMoreVisible` in `MapViewModelTests_Sort` | Behoben. Dort steht jetzt `Assert.True(ViewModel.ShowMoreVisible)`. |
| Tests mit mehreren fachlichen Fällen | Weitgehend behoben, Tests wurden aufgeteilt und Theories eingeführt. Ein Rest ist unten unter Testqualität aufgeführt. |

## Befunde

### src/Tankradar.MAUI/Views/MapPage.xaml (MapPage)

- **Doppelter Code** — Der neue Stil `MapOverlayButton` (Z. 21–31) stimmt Setter für Setter mit dem Stil `MapControlButton` in `Views/Controls/StationMapView.xaml` (Z. 14–25) überein. Der einzige Unterschied ist das fehlende `FontFamily="InterSemibold"`. Der neue Stil `MapOverlayBadge` (Z. 32–37) ist identisch mit `MapBadge` in `StationMapView.xaml` (Z. 26–31). Beide Dateien binden bereits die gemeinsame `MapStyles`-Ressource ein. Sie wurde genau dafür angelegt, aber die zwei Stilpaare liegen nicht darin.

  Empfehlung: Je einen Stil `MapControlButton` und `MapBadge` nach `Resources/MapStyles.xaml` verschieben. Den Unterschied bei `FontFamily` entweder angleichen oder in `MapPage` lokal per Setter überschreiben. Danach `MapOverlayButton` und `MapOverlayBadge` in `MapPage.xaml` sowie die lokalen Kopien in `StationMapView.xaml` entfernen und alle Verwendungen auf die gemeinsamen Schlüssel umstellen.

- **Namenskonventionen / Einheitlichkeit** — Für dieselbe CLR-Namespace `Tankradar.MAUI.Resources` gibt es jetzt zwei XML-Präfixe: das neue `xmlns:resources` (Z. 4, für `<resources:MapStyles />`) und das bestehende `xmlns:palette` (Z. 9, für `palette:MapPalette.*`).

  Empfehlung: Ein Präfix verwenden. In den anderen neuen Steuerelementen heißt es `resources`, also `palette` entfernen und `palette:MapPalette.*` auf `resources:MapPalette.*` umstellen.

### src/Tankradar.Tests.Unit/Unit/MapViewModelTests_AreaReload.cs (MapViewModelTests_AreaReload)

- **Testqualität (mehrere fachliche Fälle je Test)** — `OnViewportChanged_DisablesButtonWithHint_WhenTooLarge` prüft drei Dinge nacheinander mit jeweils eigenem Act-Schritt (Z. 197–213): den Hinweis und den gesperrten Befehl bei zu großem Ausschnitt, dass `Reload()` dann nichts abfragt, und dass ein anschließendes Heranzoomen (`ZoomBy(5)`) Hinweis und Sperre wieder aufhebt. Der letzte Teil ist ein eigener Fall.

  Empfehlung: Den letzten Teil (Z. 210–212) in einen eigenen Test auslagern, z. B. `OnViewportChanged_ClearsTooLargeHint_AfterZoomIn`.

### src/Tankradar.Tests.Unit/Unit/MapAreaQueryTests_Radius.cs (MapAreaQueryTests_Radius)

- **Testqualität (mehrere Fälle je Test / Überschneidung)** — `From_ReturnsNull_WhenRadiusExceeds25Km` (Z. 55–61) prüft drei Datenfälle in einem `[Fact]`: 25 km sind erlaubt, knapp darüber gibt `null`, und ein sehr großer Ausschnitt gibt `null`. Der erste Fall passt nicht zum Testnamen. Die Grenzwerte 52.44 und 52.46 sind außerdem schon in `IsTooLarge_MatchesMaximumRadius` abgedeckt. `From_UsesMinimumOneKilometer` (Z. 66–71) prüft ebenfalls zwei Datenfälle in einem `[Fact]`.

  Empfehlung: `From_ReturnsNull_WhenRadiusExceeds25Km` als `[Theory]` nur mit den `null`-Fällen (52.46 und den großen Ausschnitt) schreiben und den 25-km-Fall in einen eigenen Test `From_AllowsMaximumRadius` verschieben. `From_UsesMinimumOneKilometer` ebenfalls auf `[Theory]` mit `[InlineData]` umstellen.

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
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_MapView.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Panel.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_SearchOverlay.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Settings.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewModelTests_Sort.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Bounds.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Math.cs`
- `src/Tankradar.Tests.Unit/Unit/Support/FakeFuelPriceService.cs`
