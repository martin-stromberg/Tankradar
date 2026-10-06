# Bestandsaufnahme

- Vorhanden: `IFuelPriceService.GetStationDetailAsync` (Cache 5 min, Offline-Fallback), `PriceRepository` (Details mit `DetailsUpdatedUtc`), `IConnectionMonitor.ConnectionRestored`, `StationResultBuilder`, `StationHints`, `PriceFreshness`, Mock-Server mit `detail.php`, E2E-Basis `SearchE2ETestBase`.
- Fehlend: Navigation Liste -> Detail, Detail-ViewModel/Seite/Aufbereitung, Altersgrenze der Detailangaben, Normalisierung typografischer Zeichen, Schlüssel aus `FUEL_PRICE_API_KEY`.
- Drosselung: `RequestThrottle` nutzt `GetUtcNow` und Standardabstand exakt 1 s (`GeocodingOptions`).
