# Umsetzungsplan

## Architektur

1. `TestDataPaths.GeocodingUrlEnvironmentVariable` = `TANKRADAR_GEOCODING_URL`.
2. `Services/Geocoding/`: `AddressQuery` (Normalisierung + Validierung, Enum `AddressInputError`), `GeocodingStatus` (Found, NotFound, Offline, Unavailable, Rejected, EndpointNotConfigured), `GeocodingResult`, `GeocodingOptions` (Basisadresse, Zeitlimit, Mindestabstand 1 s, User-Agent, Testmodus-Override wie `PriceApiOptions`), `IGeocodingService`, `NominatimGeocodingService` (eine Anfrage, Drosselung über eigene `RequestThrottle`, `countrycodes=de`, `limit=1`, `format=jsonv2`, `accept-language=de`, kein Redirect, Position validiert über `GeoPosition`, nur Typ-Logging).
3. `MapViewModel`: Suchmodus (`SearchMode` Enum + Chips `Search.Mode.*`), `AddressText`, `ClearAddressCommand`, `ResolvedPlace`, Attributionstext; im Adressmodus Validierung -> Offline-Prüfung -> Geokodierung -> `SearchNearbyAsync`; kein Standortaufruf. Keine Position als Feld.
4. `MapPage.xaml`: Modus-Chips, Entry (ReturnCommand = SearchCommand, ClearButton), Attribution, aufgelöster Ort. `SearchTexts` ergänzen.
5. DI in `MauiProgram`.
6. `TestSupport/MockNominatimServer` (verlangt User-Agent, protokolliert Anfragen/Zeitpunkte).
7. Tests: Unit (AddressQuery, Options, Service parsing/Drosselung/Fehler, ViewModel Adressmodus, Privacy), Integration (Mock-Server-Flow, Persistenzprüfung), E2E (FlaUI Adressablauf, nicht gefunden, GPS „Nie“, Validierung).
8. Doku: `docs/help/Suche`, Index, README, ADR 0003 (Radius 1–25 statt 5–50, Land DE, Designabweichung).
9. Zwischenstand 0.1.10.

## Offene Punkte

(keine)
