# Tasks: Navigation zur Tankstelle aus der Detailansicht

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `StationInfo.Position` (`GeoPosition?`) ergänzen | Erledigt | `StationInfoTests_Position` |
| 2 | Datenmodell | `StationListItem.Position` (`GeoPosition?`) ergänzen | Erledigt | `StationDetailViewModelTests_Navigation.Navigate_OriginWithPosition_OpensCoordinates` |
| 3 | Datenmodell | `StationDetailItem` um `GeoPosition? Position` (optionaler Parameter) und `HasPosition` erweitern | Erledigt | `StationDetailItemTests_Navigation.HasPosition_ReflectsPosition` |
| 4 | Logik | `StationResultBuilder` auf `station.Position` umstellen, `HasValidPosition` entfernen | Erledigt | `StationResultBuilderTests_*` (bestehende Tests bleiben grün) |
| 5 | Logik | `RouteStationSearchService` Positionsprüfung auf `station.Position` umstellen | Erledigt | `RouteStationSearchServiceTests_*` (bestehende Tests bleiben grün) |
| 6 | Logik | `StationDetailBuilder.Build` reicht `current.Position` an `StationDetailItem` durch | Erledigt | `StationDetailItemTests_Navigation.Build_ValidCoordinates_SetsPosition` |
| 7 | Logik | `IMapOpener`/`EssentialsMapOpener` in `Services/Navigation/IMapOpener.cs` anlegen | Erledigt | `EssentialsMapOpenerTests` |
| 8 | UI-Texte | `DetailTexts.Navigate` und `DetailTexts.NavigationFailed` ergänzen | Erledigt | `StationDetailViewModelTests_Navigation.Navigate_OpenerFails_ShowsMessage` |
| 9 | ViewModel | `StationDetailViewModel`: `IMapOpener`-Parameter, `NavigateCommand`, `CanNavigate`, `NavigateAsync`, `LastNavigationTask`, `Show` übergibt `origin.Position`, `Detail`-Setter meldet `CanNavigate` | Erledigt | `StationDetailViewModelTests_Navigation` |
| 10 | UI | `StationDetailPage.xaml`: Schaltfläche `Detail.Navigate` unter der Station-Karte einfügen | Erledigt | `DetailE2ETests_Navigate.NavigateButton_VisibleAndTappable_StaysOnDetailPage` |
| 11 | DI | `MauiProgram`: `IMapOpener` als Singleton mit `IsTestMode()` registrieren | Erledigt | Kein direkter Test (DI-Auflösung über Build; Verhalten via `EssentialsMapOpenerTests`) |
| 12 | Tests | `FakeMapOpener` unter `Unit/Support/` anlegen und `StationDetailViewModelTestBase` anpassen | Erledigt | `StationDetailViewModelTests_Navigation` |
| 13 | Tests | `EssentialsMapOpenerTests` anlegen | Erledigt | `EssentialsMapOpenerTests` |
| 14 | Tests | `StationDetailItemTests_Navigation` anlegen (Builder setzt `Position`) | Erledigt | `StationDetailItemTests_Navigation` |
| 15 | Tests | `StationDetailViewModelTests_Navigation` anlegen | Erledigt | `StationDetailViewModelTests_Navigation` |
| 16 | Tests | `StationInfoTests_Position` anlegen | Erledigt | `StationInfoTests_Position` |
| 17 | E2E-Tests | `DetailE2ETests_Navigate.cs` anlegen (Schaltfläche sichtbar/bedienbar; Ausblenden ohne Ziel per Unit-Test) | Erledigt | `DetailE2ETests_Navigate` (Ausführung in CI) |
