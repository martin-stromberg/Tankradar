# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** Läufe vom 2026-11-23 ab 23:05 Uhr MEZ (UTC+1); Build/Restore-Protokoll in `test-results/build.log`.
- **Branch und Commit-ID:** `task/issue-65-3fcd977b1a4a479b811a8bf855aae835-tankerkoenig-client-antwortgro`, Commit `0fae8df550c29026226ad6316c92585609b366a5` („Backmerge from main to staging (#99)", 2026-11-23 21:30:34 +0100).
- **Uncommittete Änderungen im getesteten Stand:** ausschließlich untracked `docs/features/task/issue-65-3fcd977b1a4a479b811a8bf855aae835-tankerkoenig-client-antwortgro/` (Requirement und diese Bestandsaufnahme). Kein Produktiv- oder Testcode verändert.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (lokale Entwicklermaschine), .NET SDK `11.0.100` (`global.json`: `11.0.100`, rollForward `latestPatch`; zusätzlich SDK 10.0.404 installiert), Runtime `Microsoft.NETCore.App 11.0.0`. MAUI-Workloads installiert (`maui-windows 11.0.0/11.0.100`, android, ios, maccatalyst). Node `v24.15.0` für die Skript-Tests. Build/Test der Windows-Job-Simulation wie `scripts/local-ci.ps1` und die CI-Workflows: `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true` (nur `net11.0-windows10.0.19041.0`).
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Befehle aus `.github/workflows/staging-ci.yml`/`pr-staging-ci.yml` (Jobs `build-and-test` = blockierende Lane mit `--filter "Category!=OsInterface"`, Job `os-interface-tests` = best-effort-Lane mit `--filter "Category=OsInterface"`, Issue #93) und `scripts/local-ci.ps1`. Suiten: `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (alle xunit, `net11.0-windows10.0.19041.0`) sowie `npm test` (`node --test "scripts/*.test.mjs"`, Skript-Tests). Coverage-Läufe der blockierenden Lane mit `--settings coverlet.runsettings --collect:"XPlat Code Coverage"` wie in der CI.
- **Vorlauf:** `dotnet restore Tankradar.sln -p:Configuration=Release` (Exit 0) und `dotnet build Tankradar.sln --configuration Release --no-restore` (Exit 0, 0 Warnungen, 0 Fehler) — Nachweis [build.log](test-results/build.log). Alle `dotnet test`-Aufrufe mit `--no-build` gegen diesen Build.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit (blockierend) | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --filter "Category!=OsInterface" --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <test-results> --logger "trx;LogFileName=test-results-unit.trx"` | Repo-Root | 0 | 2146 | 0 | 0 | [TRX](test-results/test-results-unit.trx), [Log](test-results/console-unit.log), [Coverage](test-results/9f95982a-fa95-401f-9f29-54f2ab4b80da/coverage.cobertura.xml) |
| Integration (blockierend) | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --filter "Category!=OsInterface" --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <test-results> --logger "trx;LogFileName=test-results-integration.trx"` | Repo-Root | 0 | 199 | 0 | 0 | [TRX](test-results/test-results-integration.trx), [Log](test-results/console-integration.log), [Coverage](test-results/70043160-3e16-4891-b834-99bf023d93a4/coverage.cobertura.xml) |
| E2E (blockierend, gefiltert) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --filter "Category!=OsInterface" --results-directory <test-results> --logger "trx;LogFileName=test-results-e2e.trx"` | Repo-Root | 0 | 2 | 0 | 0 | [TRX](test-results/test-results-e2e.trx), [Log](test-results/console-e2e.log) |
| Unit (OsInterface, best-effort) | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --filter "Category=OsInterface" --results-directory <test-results> --logger "trx;LogFileName=test-results-os-interface-unit.trx"` | Repo-Root | 0 | 34 | 0 | 0 | [TRX](test-results/test-results-os-interface-unit.trx), [Log](test-results/console-os-interface-unit.log) |
| Integration (OsInterface, best-effort) | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --filter "Category=OsInterface" --results-directory <test-results> --logger "trx;LogFileName=test-results-os-interface-integration.trx"` | Repo-Root | 0 | 113 | 0 | 0 | [TRX](test-results/test-results-os-interface-integration.trx), [Log](test-results/console-os-interface-integration.log) |
| Skript-Tests | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 32 | 0 | 0 | [Log](test-results/console-npm-scripts.log) |
| E2E (OsInterface/FlaUI, best-effort) | `TANKRADAR_E2E_WINDOW=offscreen dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --filter "Category=OsInterface" --results-directory <test-results> --logger "trx;LogFileName=test-results-os-interface-e2e.trx"` | Repo-Root | 0 | 183 | 0 | 0 | [TRX](test-results/test-results-os-interface-e2e.trx), [Log](test-results/console-os-interface-e2e.log) |

`<test-results>` = `docs/features/task/issue-65-3fcd977b1a4a479b811a8bf855aae835-tankerkoenig-client-antwortgro/inventory/test-results`. Geheimnisse: Die Konsolenlogs enthalten weder API-Schlüssel noch Zugangsdaten (der Client protokolliert per Design nie den Schlüssel; die Tests nutzen Testwerte wie `test-credential`/`mock-key-for-tests`).

### Nachgewiesene bestehende Testfehler

Keine. Alle Läufe waren vollständig erfolgreich (Exit-Code 0, 0 Fehlschläge, 0 Übersprungen): 2146 + 199 + 2 Tests der blockierenden Lane, 34 + 113 + 183 der best-effort-Lane, 32 Skript-Tests — insgesamt 2709 ausgeführte Tests. Hinweis: Die E2E-Kategorie `Category!=OsInterface` enthält projektweit nur 2 Tests (`StartFailureCleanupE2ETests.Dispose_AfterFailedStart_DoesNotThrow`, `FlakyConventionTests.FlakyMarkedTests_AlwaysCarryOsInterface`); alle übrigen E2E-Tests sind als `OsInterface` markiert und liefen in der best-effort-Lane. Die wiederkehrenden `Application failed to exit`-Zeilen im FlaUI-Log sind Laufzeit-Hinweise von FlaUI beim Prozessende, keine Testfehlschläge (alle betroffenen Tests bestanden).

### Testlücken und Ausführungsprobleme

- **FlaUI-E2E-Lane (`Category=OsInterface`, 183 von 185 entdeckten Tests):** vollständig ausgeführt und bestanden (25,8 min). In der CI läuft diese Lane als best-effort-Job `os-interface-tests` (Issue #93).
- **Keine Coverage-Auswertung der Schwellenprüfung:** `reportgenerator`/`check-coverage.mjs` (70 %-Schwelle der CI) wurde nicht ausgeführt; die Cobertura-Dateien der beiden Coverage-Läufe liegen als Nachweis vor.
- **Format-/Sicherheits-/iOS-Prüfung** (`dotnet format --verify`, `check-vulnerabilities.mjs`, iOS-Compile-Check) sind Prüfschritte, keine Testsuiten, und wurden nicht ausgeführt.
- **Keine Tests für das Antwortgrößenlimit beim `TankerkoenigClient`** (Gegenstand der Anforderung): Es existiert weder ein Test, der eine übergroße Antwort ablehnt, noch eine Testmöglichkeit, ein Limit an der DI-Registrierung zu prüfen — `TankerkoenigClientTestBase.CreateClient` erzeugt `new HttpClient(Handler)` ohne `MaxResponseContentBufferSize` (siehe Hilfsmethoden unten). `FakeHttpMessageHandler` kann über `RespondWithBytes` beliebig große Körper liefern, bietet aber keinen Antwortschritt ohne `Content-Length`-Kopfzeile (`StringContent`/`ByteArrayContent` setzen die Länge implizit).
- **Kein DI-Test für `AddPriceServices`:** `MauiProgramTests_RoutingServices` prüft nur `AddRoutingServices`; eine Registrierungsprüfung der Preisdienste (inkl. HttpClient-Eigenschaften) existiert nicht.

## Testklassen

### `TankerkoenigClientTestBase`
Datei: `src/Tankradar.Tests.Unit/Unit/TankerkoenigClientTestBase.cs` — abstrakte Basis (keine Testfälle). Stellt `ListBody` (JSON-Antwort mit zwei Stationen), `Handler` (`FakeHttpMessageHandler`), `Delay` (`RecordingDelay`), `Clock` (`ManualTimeProvider`), `Keys` (`FakeApiKeyProvider`) und `CreateClient(PriceApiOptions?)` bereit: `new TankerkoenigClient(new HttpClient(Handler), Keys, options, Delay, new RequestThrottle(...), Clock, NullLogger)` — kein Netzwerk, kein `MaxResponseContentBufferSize`.

### `TankerkoenigClientTests_Parsing`
Datei: `src/Tankradar.Tests.Unit/Unit/TankerkoenigClientTests_Parsing.cs`
- `SearchAsync_ParsesStationsAndPrices` — Stationen/Preise mit Abrufzeitstempel; fehlende Angaben bleiben leer.
- `SearchAsync_BuildsHttpsRequestWithInvariantNumbers` — Anfrageaufbau (HTTPS, invariante Zahlen, Schlüssel als Parameter).
- `GetDetailAsync_ParsesOpeningTimesAndWholeDay` — Öffnungszeiten der `detail.php`-Antwort.
- `GetDetailAsync_InvalidId_ThrowsBeforeRequest` — UUID-Prüfung vor dem Abruf.
- `SearchAsync_BadBodies_AreClassified` (Theory, 4 Fälle) — `InvalidResponse` bei kaputtem JSON/fehlender Liste, `Rejected` bei `ok:false`.

### `TankerkoenigClientTests_Prices`
Datei: `src/Tankradar.Tests.Unit/Unit/TankerkoenigClientTests_Prices.cs`
- `GetPricesAsync_ParsesPricesAndFalseValues` — Preiszeilen; `false`/`0` werden ausgelassen.
- `GetPricesAsync_BuildsRequestWithIdsAndInvariantFormat` — kommagetrennte Kennungen, Schlüssel als Parameter.
- `GetPricesAsync_NoPricesStatus_YieldsNoLines` — `status:"no prices"` → leere Liste.
- `GetPricesAsync_InvalidIdOrMoreThanTen_ThrowsBeforeRequest` (Theory, 3 Fälle) — Mengen-/UUID-Prüfung vor dem Abruf.
- `GetPricesAsync_BadBodies_AreClassified` (Theory, 4 Fälle) — Fehlereinstufung wie bei der Suche.

### `TankerkoenigClientTests_Retry`
Datei: `src/Tankradar.Tests.Unit/Unit/TankerkoenigClientTests_Retry.cs`
- `SearchAsync_ServerErrors_AreRetriedWithIncreasingDelay` — 5xx mit verdoppelter Wartezeit.
- `SearchAsync_AlwaysFailing_ThrowsUnreachableAfterMaxAttempts` — `Unreachable` nach 3 Versuchen.
- `SearchAsync_ClientErrors_AreNotRetried` (Theory, 3 Fälle: 400/401/404) — sofort `Rejected`.
- `SearchAsync_TooManyRequests_IsRetried` — 429 wird wiederholt.
- `SearchAsync_NoResponse_TimesOutAndReportsUnreachable` — `RequestTimeout` → `TimeoutException` → `Unreachable`.
- `SearchAsync_CallerCancellation_Propagates` — Caller-Abbruch ohne Wiederholung.
- `SearchAsync_WithoutKey_FailsWithoutRequest` — `ApiKeyMissing` ohne Anfrage.
- `Failures_NeverContainTheKey` / `NetworkErrors_NeverLeakUrlOrKey` — kein Schlüssel-/URL-Leak in Ausnahmen.
- `Constructor_HttpOptions_Throws` — unsichere Optionen werden abgelehnt.

### `TankerkoenigClientTests_EndpointNotConfigured`
Datei: `src/Tankradar.Tests.Unit/Unit/TankerkoenigClientTests_EndpointNotConfigured.cs`
- `SearchAsync_EndpointNotConfigured_RefusesWithoutRequest`
- `GetDetailAsync_EndpointNotConfigured_RefusesWithoutRequest`

### `PriceApiOptionsTests_Validation`
Datei: `src/Tankradar.Tests.Unit/Unit/PriceApiOptionsTests_Validation.cs` (9 Tests)
- `Default_IsHttpsAndValid`, `Validate_Http_IsRejected`, `Validate_LoopbackHttp_OnlyWithTestFlag`, `Validate_InvalidLimits_AreRejected`, `FromEnvironment_*` (5 Tests: Override ohne/mit Testmodus, Remote-HTTP, Testmodus ohne URL → `EndpointNotConfigured`, Normalmodus).

### `FuelPriceServiceTests_*`
Dateien: `src/Tankradar.Tests.Unit/Unit/FuelPriceServiceTests_{Search,Detail,DetailAge,Offline,RefreshStations,ManyStations,CustomName,InvalidateCaches,StorageFailure}.cs` auf `FuelPriceServiceTestBase` (`StubTankerkoenigClient` als Client-Double). Relevant für die Anforderung: die `*_ApiFailure*`/`_FallsBack*`-Tests (`SearchNearbyAsync_AnyApiFailure_FallsBack`, `GetStationDetailAsync_ApiFailure_FallsBackToKnownData`, `Refresh_ApiFailure_FallsBackWithFailure`, `CheckAvailabilityAsync_MapsOutcomes`) prüfen, dass jeder `PriceFailure` des Clients in den Rückfall überführt wird.

### `HttpTileSourceTests_Fetch` (Referenzmuster für Größenlimit)
Datei: `src/Tankradar.Tests.Unit/Unit/HttpTileSourceTests_Fetch.cs` — Test mit `TileServerOptions.MaxTileBytes` gegen einen Handler mit großem Körper (Zeile ~168-170): das begrenzende Stream-Lesen des `HttpTileSource` ist durch Unit-Tests abgedeckt; für den Preis-Client existiert kein Pendant.

### `MauiProgramTests_RoutingServices`
Datei: `src/Tankradar.Tests.Unit/Unit/MauiProgramTests_RoutingServices.cs` — `AddRoutingServices_ResolvesAllRoutingServices`, `AddRoutingServices_SharesTransportAndAccess`, `AddRoutingServices_RegistersSingletons`. Ein analoger Test für `AddPriceServices` existiert nicht (`AddPriceServices` ist `private`; `AddRoutingServices` ist `public`).

### Integrationstests mit echtem `TankerkoenigClient`
Dateien: `src/Tankradar.Tests.Integration/Integration/PriceServiceMockServerTests_Flow.cs` (7 Tests, eigener Aufbau) und `PriceRefreshMockServerTests_Flow.cs` plus `SearchMockServerTests_*` auf `SearchMockServerTestBase` — erzeugen den Client mit `new HttpClient { Timeout = InfiniteTimeSpan }` gegen `MockTankerkoenigServer` (Loopback-`HttpListener`, antwortet mit `Content-Length64`). Kein Test mit übergroßer Antwort oder fehlendem `Content-Length`.

## Hilfsmethoden

### `FakeHttpMessageHandler`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/FakeHttpMessageHandler.cs`
- `Respond(Func<HttpResponseMessage>)` — frei erzeugte Antwort (ermöglicht theoretisch Körper ohne `Content-Length` über eigenen `HttpContent`).
- `RespondWith(HttpStatusCode, string)` — `StringContent` (UTF-8, `application/json`).
- `RespondWithBytes(HttpStatusCode, byte[])` — `ByteArrayContent`, beliebig große Körper möglich.
- `Throw(Exception)` — wirft beim Versand.
- `Hang()` — Antwortet nie (bis Abbruch).
- Aufzeichnung: `Requests`, `UserAgents`, `IfNoneMatch`, `IfModifiedSince`.

### `FakeApiKeyProvider` / `FakeApiKeyStore`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/FakeApiKeyDoubles.cs`
- `FakeApiKeyProvider.Value` — fester Schlüssel (`test-credential` Standard); `null` simuliert fehlenden Schlüssel.
- `FakeApiKeyStore` — `Stored`, `ThrowOnGet/Set/Remove`, `SetCount`, `RemoveCount` (für `ApiKeyProvider`-Tests).

### `RecordingDelay`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/RecordingDelay.cs`
- `Delays` — aufgezeichnete Wartezeiten; kehrt sofort zurück (`IDelay`).

### `ManualTimeProvider`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/ManualTimeProvider.cs`
- `Now`/`UtcNow`, `Advance(TimeSpan)`, `CreateTimer` mit manuellen `ManualTimer`s — steuerbare `TimeProvider`-Uhr.

### `StubTankerkoenigClient`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/StubTankerkoenigClient.cs`
- `SearchResult`, `DetailResult`, `PricesResult`, `OmitUnknownPriceIds`, `Failure` (`PriceApiException` für alle Abrufe), `PricesFailure`/`PricesFailureFromCall`, Zähler `SearchCalls`/`DetailCalls`/`PricesRequests` — `ITankerkoenigClient`-Double für `FuelPriceService`-Tests.

### `MockTankerkoenigServer` (TestSupport, Integration/E2E)
Datei: `src/TestSupport/MockTankerkoenigServer.cs`
- Loopback-`HttpListener` für `list.php`/`detail.php`/`prices.php` mit festen Teststationen (`StationAlpha`…`StationZeta`, `StationJetMain`/`StationJetSide`, `StationRoute*`), `AddGeneratedStations(count)` für große Ergebnismengen (erzeugt größere Antwortkörper — potenziell nutzbar für Limit-Tests auf Integrationsebene), `SetPrices`, `EnqueueStatuses`, `ResponseDelay`, Anfrageaufzeichnung. Antwortet stets mit `Content-Length64` (kein Chunked-Modus).

### `SearchMockServerTestBase` (Integration)
Datei: `src/Tankradar.Tests.Integration/Integration/Support/SearchMockServerTestBase.cs`
- `CreateService(...)` — `FuelPriceService` mit echtem `TankerkoenigClient` (`new HttpClient { Timeout = InfiniteTimeSpan }`) gegen den Mock; `CreateGeocoding`, `CreateRouteSearch` analog.
