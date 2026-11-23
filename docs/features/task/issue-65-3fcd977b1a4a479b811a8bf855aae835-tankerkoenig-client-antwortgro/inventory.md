# Bestandsaufnahme: Tankerkönig-Client — Antwortgröße begrenzen und JSON streaming verarbeiten (Issue #65)

Bestandsaufnahme der Preisdienst-Anbindung (`TankerkoenigClient`, `FuelPriceService`, DI-Registrierung) bezogen auf die Anforderung, API-Antworten größenbegrenzt zu verarbeiten statt unbegrenzt per `ReadAsStringAsync` in den Speicher zu lesen.

## Zusammenfassung

- `TankerkoenigClient.GetAsync` (`src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs:193-252`) liest den Erfolgskörper aller drei Endpunkte (`list.php`, `detail.php`, `prices.php`) unbegrenzt per `ReadAsStringAsync` (Zeile 222) in einen `string`; `ParseDocument` (Zeile 254-271) parst daraus synchron per `JsonDocument.Parse`. Kein `Content-Length`-Check, kein `HttpCompletionOption.ResponseHeadersRead`, kein `MaxResponseContentBufferSize`.
- Die HttpClient-Registrierung in `MauiProgram.AddPriceServices` (`src/Tankradar.MAUI/MauiProgram.cs:312-318`) setzt nur `AllowAutoRedirect = false`, `Timeout = InfiniteTimeSpan` und einen `User-Agent` — im Gegensatz zu `AddGeocodingServices` (Zeile 208-212: `MaxResponseContentBufferSize = 256 KB`) und `AddRoutingServices` (Zeile 233-237: `4 MB`) kein Größenlimit. `HttpTileSource` (Kartenkacheln) ist ausgenommen: eigenes Limit `TileServerOptions.MaxTileBytes` (512 KB), streambasiert durchgesetzt (`HttpTileSource.cs:206-218`).
- Fehlerbild bestehender Mechanismen: `HttpRequestException` (z. B. aus einer Pufferlimit-Verletzung) läuft aktuell in den Wiederholungszweig und endet nach `MaxAttempts` als `PriceApiException(PriceFailure.Unreachable)` — eine „übergroße Antwort“ existiert als Fehlerbild noch nicht; `PriceFailure.InvalidResponse` ist als passender Grund bereits vorhanden und in `PriceTexts`/`SearchTexts` mit Anzeigetexten hinterlegt.
- `PriceApiOptions` (`src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs`) enthält noch keine Eigenschaft für ein Antwortgrößenlimit; `Validate()` prüft Zeit-/Wiederholungswerte und HTTPS.
- Alle vier externen Dienste nutzen manuell erzeugte Singleton-`HttpClient`s mit `HttpClientHandler`; weder `SocketsHttpHandler`/`PooledConnectionLifetime` noch `IHttpClientFactory`/`AddHttpClient` kommen irgendwo im `src/`-Baum vor (Prüfaufgabe des Issues).
- `ITankerkoenigClient` umfasst `SearchAsync`, `GetDetailAsync`, `GetPricesAsync`; der Fehlervertrag ist `PriceApiException` mit `PriceFailure`. `FuelPriceService` fängt `PriceApiException` an allen drei Aufrufstellen und bildet `ex.Failure` auf Cache-/Offline-Rückfall ab.
- Test-Infrastruktur vorhanden: `TankerkoenigClientTestBase` (Fake-`HttpMessageHandler`, `RecordingDelay`, `ManualTimeProvider`, `FakeApiKeyProvider`, `new HttpClient(Handler)` ohne Limits), `FakeHttpMessageHandler.RespondWithBytes` für beliebige Antwortkörper (ein Schritt ohne `Content-Length` existiert nicht), `StubTankerkoenigClient` für Service-Tests, `MockTankerkoenigServer` (Loopback-`HttpListener`, sendet immer `Content-Length64`) für Integrations-/E2E-Tests.
- Keine bestehenden Tests für Antwortgrößen beim `TankerkoenigClient`; Konvention `TankerkoenigClientTests_<Aspekt>` mit drei vorhandenen Aspekt-Dateien (`_Parsing`, `_Prices`, `_Retry`, `_EndpointNotConfigured`). `MauiProgramTests_RoutingServices` existiert; ein Pendant für `AddPriceServices` fehlt.

Test-Ausgangszustand: Alle Testsuiten vollständig ausgeführt und bestanden — blockierende Lane: Unit 2146/2146, Integration 199/199, E2E (gefiltert) 2/2; best-effort-Lane (`OsInterface`): Unit 34/34, Integration 113/113, FlaUI-E2E 183/183; Skript-Tests (`npm test`) 32/32. Insgesamt 2709 Tests, 0 Fehlschläge, 0 übersprungen. Nachweis: [inventory/tests.md](inventory/tests.md) mit TRX-Reports und Konsolenlogs unter `inventory/test-results/`.

## Details

- [Datenmodell](inventory/models.md) — `PriceApiOptions`, `StationInfo`, `FuelPrice`, `OpeningTimeEntry`, Ergebnisrecords
- [Logik](inventory/logic.md) — `TankerkoenigClient`, `FuelPriceService`, `MauiProgram`-Registrierungen, `RequestThrottle`/`TaskDelay`, `ApiKeyProvider`
- [Enums](inventory/enums.md) — `PriceFailure`, `FuelType`, `PriceDataSource`
- [Interfaces](inventory/interfaces.md) — `ITankerkoenigClient`, `IFuelPriceService`, `IApiKeyProvider`/`IApiKeyStore`, `IDelay`, `IPriceCacheInvalidator`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und Hilfsmethoden
