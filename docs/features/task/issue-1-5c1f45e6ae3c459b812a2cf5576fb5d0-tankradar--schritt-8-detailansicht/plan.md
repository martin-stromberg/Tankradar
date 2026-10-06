# Plan

1. `DetailFreshness` (24 h) und `StationInfo.WithoutStaleDetails`; Anwendung in `FuelPriceService.WithKnownDetailsAsync`, `StationResultBuilder`, `StationDetailBuilder`.
2. `StationDetailItem`/`OpeningHoursLine`, `StationDetailBuilder`, `DetailTexts`.
3. `IStationNavigator`/`ShellStationNavigator` (Route `stationdetail`), `StationDetailViewModel` (IQueryAttributable, Offline, Wiederverbindung), `StationDetailPage`, DI und Route.
4. `MapViewModel.OpenStationCommand`, Schaltfläche „Details“ und Tippen auf Karte in `MapPage`.
5. `AddressInput`: typografische Apostrophe und Striche normalisieren.
6. csproj: Schlüssel-Reihenfolge; Doku `api-schluessel.md`, `workflows.md`.
7. Drosselung: `GeocodingOptions.DefaultMinRequestInterval` 1100 ms, `RequestThrottle` mit monotoner Uhr; E2E-Grenze 1000 ms.
8. Tests: Unit (Freshness, Builder, ViewModel, Map, Throttle, Adresse), Integration (Anpassung), E2E (Detail öffnen).
9. Doku `docs/help/Tankstellendetails/`, README.

## Offene Punkte

Keine.
