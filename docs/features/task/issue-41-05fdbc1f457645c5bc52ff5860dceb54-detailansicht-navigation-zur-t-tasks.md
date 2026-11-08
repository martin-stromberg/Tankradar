# Tasks: Navigation zur Tankstelle aus der Detailansicht

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `StationInfo.Position` (`GeoPosition?`) ergänzen | Offen | — |
| 2 | Datenmodell | `StationListItem.Position` (`GeoPosition?`) ergänzen | Offen | — |
| 3 | Datenmodell | `StationDetailItem` um `GeoPosition? Position` (optionaler Parameter) und `HasPosition` erweitern | Offen | — |
| 4 | Logik | `StationResultBuilder` auf `station.Position` umstellen, `HasValidPosition` entfernen | Offen | — |
| 5 | Logik | `RouteStationSearchService` Positionsprüfung auf `station.Position` umstellen | Offen | — |
| 6 | Logik | `StationDetailBuilder.Build` reicht `current.Position` an `StationDetailItem` durch | Offen | — |
| 7 | Logik | `IMapOpener`/`EssentialsMapOpener` in `Services/Navigation/IMapOpener.cs` anlegen | Offen | — |
| 8 | UI-Texte | `DetailTexts.Navigate` und `DetailTexts.NavigationFailed` ergänzen | Offen | — |
| 9 | ViewModel | `StationDetailViewModel`: `IMapOpener`-Parameter, `NavigateCommand`, `CanNavigate`, `NavigateAsync`, `LastNavigationTask`, `Show` übergibt `origin.Position`, `Detail`-Setter meldet `CanNavigate` | Offen | — |
| 10 | UI | `StationDetailPage.xaml`: Schaltfläche `Detail.Navigate` unter der Station-Karte einfügen | Offen | — |
| 11 | DI | `MauiProgram`: `IMapOpener` als Singleton mit `IsTestMode()` registrieren | Offen | — |
| 12 | Tests | `FakeMapOpener` unter `Unit/Support/` anlegen und `StationDetailViewModelTestBase` anpassen | Offen | — |
| 13 | Tests | `EssentialsMapOpenerTests` anlegen | Offen | — |
| 14 | Tests | `StationDetailItemTests_Navigation` anlegen (Builder setzt `Position`) | Offen | — |
| 15 | Tests | `StationDetailViewModelTests_Navigation` anlegen | Offen | — |
| 16 | Tests | `StationInfoTests_Position` anlegen | Offen | — |
| 17 | E2E-Tests | `DetailE2ETests_Navigate.cs` anlegen (Schaltfläche sichtbar/bedienbar; Ausblenden ohne Ziel) | Offen | — |
