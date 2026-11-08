# Bestandsaufnahme — Aufruf der Tankstellendetails (Issue #44)

## Betroffene Ansicht

Die Startansicht „Favoriten“ (`FavoritesPage.xaml`) listet Tankstellen an zwei Stellen als Kacheln (`StationTile`):

- **Favoritengruppen** (`Groups` → `HomeGroupViewModel.Stations` → `HomeStationViewModel`, Präfix `Favorites.Station`)
- **Bereich „In der Nähe“** (`Nearby` → `NearbySectionViewModel.Entries` → `HomeStationViewModel`, Präfix `Nearby.Station`)

## Verdrahtung (bestehend, korrekt)

- `FavoritesViewModel.OpenStation` → `OpenStationAsync` → `IStationNavigator.OpenDetailAsync(station.Item)` mit Doppelaufruf-Sperre `_isOpeningStation` (`FavoritesViewModel.cs:515-542`).
- `NearbySectionViewModel.OpenStation` → `OpenStationAsync` → `_navigator.OpenDetailAsync(station.Item)` mit Doppelaufruf-Sperre (`NearbySectionViewModel.cs:266-293`).
- `ShellStationNavigator` registriert Route `stationdetail` (`AppShell.xaml.cs:34`); `StationDetailViewModel.ApplyQueryAttributes` übernimmt `StationListItem` (`StationDetailViewModel.cs:270-277`).
- `HomeStationViewModel.OpenCommand` ruft die übergebene `open`-Aktion (`HomeStationViewModel.cs:26`).
- Unit-Tests belegen den Pfad: `OpenStation_NavigatesWithStationAndGuardsDoubleOpen` (`FavoritesViewModelTests_Home.cs:197`), `Entries_OpenDetailAndOfferNoFavoriteActions` (`NearbySectionViewModelTests_Section.cs:153`).

## Der Defekt

`StationTile.xaml` (Commit `0506c99`, Issue #27): Der `TapGestureRecognizer` am Kachel-`Border` wurde entfernt; das Antippen hängt seitdem allein am transparenten Overlay-`Button` `OpenButton` (`Opacity="0.01"`, `Command="{Binding OpenCommand}"`).

Plattformursache: `Opacity="0.01"` wird als `float` `0.0099999998…` gespeichert und liegt damit **unter** der `hitTest`-Schwelle von iOS (`UIView.hitTest` ignoriert Views mit `alpha < 0.01`) — die Ebene ist dort für echte Berührungen unsichtbar und nicht erreichbar. Unter Windows/Android feuert die Schaltfläche echte Eingaben (empirisch unter Windows per `Mouse.Click` verifiziert); UI-Automation (Invoke) erreicht sie ebenfalls — deshalb blieben die E2E-Tests grün.

Vergleich: Die Tankstellenkarte der Kartenansicht (`StationBottomSheet.xaml:184-309`) trägt **beide** Mechanismen — `TapGestureRecognizer` auf dem `Border` (echte Zeigereingabe) **und** den Overlay-`Button` (Bedienhilfen/UIA). Dort funktioniert das Antippen.

Gleiches Defektmuster: `FavoriteGroupPage.xaml:134-149` — die Ebene über dem Stationsnamen ist ebenfalls nur ein Overlay-`Button` ohne Zeiger-Fallback (Gruppenansicht, gleiche Fehlerklasse).

## E2E-/Testinfrastruktur

- E2E bedient ausschließlich UI-Automation-Muster (kein Mausklick) wegen Off-Screen-Fensterbetrieb (`E2ETestBase`, `FavoritesE2ETestBase`); Invoke-Abdeckung existiert: `HomeE2ETests_OpenDetail` (Gruppe), `HomeE2ETests_Nearby.OpenNearbyStation_ShowsDetail` („In der Nähe“).
- Echte Zeigereingabe ist im Off-Screen-Betrieb nicht testbar; im Vordergrundbetrieb (`TANKRADAR_E2E_WINDOW=foreground`) mit `MainWindow.SetForeground()` prinzipiell möglich, aber nicht CI-tauglich.
- Präzedenz für strukturelle XAML-Tests: `PageTitleRedundancyTests` wertet Seiten-XAML per `XDocument` aus (`MauiSourceFiles.PathOf`).

## Betroffene Dateien

| Datei | Rolle |
|-------|-------|
| `src/Tankradar.MAUI/Views/Controls/StationTile.xaml` | Kachel der Favoriten- und Nähe-Liste — fehlender `TapGestureRecognizer` |
| `src/Tankradar.MAUI/Views/FavoriteGroupPage.xaml` | Gruppenansicht — gleiches Muster über dem Stationsnamen |
| `src/Tankradar.Tests.Unit/Unit/` | Ort des neuen strukturellen Regressionstests |
