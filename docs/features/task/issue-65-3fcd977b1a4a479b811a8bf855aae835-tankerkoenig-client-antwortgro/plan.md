# Umsetzungsplan: Tankerkönig-Client — Antwortgröße begrenzen und JSON streaming verarbeiten (Issue #65)

## Übersicht

Der `TankerkoenigClient` verarbeitet die Antworten aller drei Tankerkönig-Endpunkte (`list.php`, `detail.php`, `prices.php`) künftig größenbegrenzt: Der Abruf erfolgt mit `HttpCompletionOption.ResponseHeadersRead`, das Limit wird zweistufig (`Content-Length`-Vorabprüfung plus begrenztes Lesen des Antwortstreams) durchgesetzt und der JSON-Körper per `JsonDocument.ParseAsync` ausgewertet. Übergroße Antworten werden als `PriceApiException(PriceFailure.InvalidResponse)` ohne Wiederholung abgelehnt. Betroffen sind `TankerkoenigClient`, `PriceApiOptions` (neue Option `MaxResponseBytes`), die Test-Infrastruktur (`FakeHttpMessageHandler`, `MockTankerkoenigServer`) sowie neue Unit-, Integrations- und E2E-Tests. `MauiProgram`, `ITankerkoenigClient`, `FuelPriceService` und die UI bleiben unverändert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine HTTP-Antwort des Tankerkönig-Preisdienstes je Einzelabruf; alle drei Endpunkte (`list.php`/`SearchAsync`, `detail.php`/`GetDetailAsync`, `prices.php`/`GetPricesAsync` mit 1 bis `MaxPriceIds` = 10 Kennungen) |
| Auslösende Ereignisse | Jeder Aufruf der `ITankerkoenigClient`-Methoden — indirekt über `FuelPriceService` aus Umkreissuche, Stationsdetail-Anzeige, Preisaktualisierung und Verfügbarkeitsprobe (`CheckAvailabilityAsync`); kein neuer Nutzer-Trigger |
| Erwartete Wirkung | Antworten bis zum Limit (1 MiB) werden wie bisher geparst und fachlich ausgewertet. Antworten über dem Limit werden kontrolliert abgelehnt — ohne unbegrenzten Speicherverbrauch und ohne Wiederholung — als `PriceApiException(PriceFailure.InvalidResponse)`, sodass `FuelPriceService` und die Oberfläche sie wie andere Preisdienst-Fehler behandeln (Cache-/Offline-Rückfall, Anzeigetext „unerwartete Antwort") |
| Umgang mit fehlenden Objekten | **Fehlende `Content-Length`-Kopfzeile (Chunked):** Das Limit wird beim Lesen des Antwortstreams durchgesetzt (begrenzter Puffer), nicht nur per Vorabprüfung. **`PriceApiOptions.MaxResponseBytes`:** kann nicht fehlen — `init`-Eigenschaft mit Standardwert, in `Validate()` geprüft (wird im `TankerkoenigClient`-Konstruktor ausgeführt). **`EndpointNotConfigured`/`ApiKeyMissing`:** bestehende Vorabprüfungen bleiben unverändert und greifen vor jeder Netzoperation |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Übergroße Antworten werden mit dem generischen Anzeigetext „unerwartete Antwort" (`PriceFailure.InvalidResponse`) gemeldet — es gibt keinen eigenen Fehlergrund und keinen eigenen Hinweistext, der die Ursache benennt | Konsistenz mit dem vorhandenen `PriceFailure`-Modell; `InvalidResponse` ist fachlich zutreffend („nicht auswertbar"), bereits mit Texten hinterlegt und fließt ohne weiteren Code in den Rückfall; die Ursache „Antwort zu groß" ist für den Anwender nicht unterscheidbar handhabbar | bestätigt (2026-11-23, durch Issue-Text: Akzeptanzkriterium „Verhalten und Fehlerbehandlung konsistent mit den anderen externen Diensten") |
| 2 | `SocketsHttpHandler.PooledConnectionLifetime`/`IHttpClientFactory` wird in diesem Change nicht umgesetzt; das Prüfergebnis wird dokumentiert und die Umstellung als Follow-up empfohlen | Laut Issue nur Prüfaufgabe; eine sinnvolle Umstellung betrifft alle vier externen Dienste gleichermaßen und liegt damit außerhalb der Akzeptanzkriterien dieses Changes; `IHttpClientFactory` erfordert zudem das Paket `Microsoft.Extensions.Http` (derzeit nicht referenziert) und bringt für reine Singleton-Clients ohne Delegating-Handler-Pipeline keinen Mehrwert | bestätigt (2026-11-23, durch Issue-Text: „Ergänzend **prüfen**, ob … eingeführt wird" — Prüfung dokumentiert, Umsetzung als Follow-up) |
| 3 | Das Antwortgrößenlimit ist nicht nutzer- oder umgebungskonfigurierbar; es gilt ein fester Standardwert in `PriceApiOptions` | Expliziter Scope-Ausschluss der Anforderung; kein Bedarf an Konfigurierbarkeit erkennbar | bestätigt (2026-11-23, Issue verlangt keine Konfigurierbarkeit) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `TankerkoenigClient.GetAsync` (Variantenwahl) | **Variante B — Streaming-Verarbeitung:** `GetAsync` mit `HttpCompletionOption.ResponseHeadersRead`, `Content-Length`-Vorabprüfung, begrenzendes Lesen des Antwortstreams in `MemoryStream` (Muster `HttpTileSource.DownloadAsync`, `HttpTileSource.cs:181-222`) und Auswertung per `JsonDocument.ParseAsync` (Muster `AppConfiguration.cs:51`) | `MaxResponseContentBufferSize` (Variante A) greift bei `ResponseHeadersRead` nicht, läge nur in der DI-Registrierung und wäre mit dem vorhandenen Testaufbau (`new HttpClient(Handler)` in `TankerkoenigClientTestBase.CreateClient`) nicht prüfbar; eine Pufferlimit-`HttpRequestException` würde obendrein in den Wiederholungszweig laufen und nach `MaxAttempts` fälschlich als `Unreachable` enden. Das clientseitige zweistufige Limit ist ohne Netzwerk testbar, deckt fehlendes `Content-Length` ab und erlaubt die gezielte Einstufung als nicht wiederholbarer `InvalidResponse` |
| Verankerung des Limits | Neue `init`-Eigenschaft `MaxResponseBytes` (`int`, Standard 1 MiB) in `PriceApiOptions`, geprüft in `Validate()` — Muster `TileServerOptions.MaxTileBytes` (`TileServerOptions.cs:67,120`) | Macht das Limit in Tests auf kleine Werte setzbar (vgl. `HttpTileSourceTests` mit `MaxTileBytes = 1024`) statt an >1-MiB-Antwortvolumen gebunden zu sein; folgt der bestehenden Options-/Validierungskonvention; wird im Konstruktor mitgeprüft |
| Konkreter Grenzwert | **1 MiB** (`1024 * 1024` Bytes) | `list.php` bei `MaxRadiusKm` = 25 km liefert die größten Antworten; in Ballungsräumen (mehrere hundert bis über tausend Stationen à ca. 300 Bytes) liegen reale Antworten im drei- bis mittleren vierstelligen KB-Bereich. Der Nominatim-Wert 256 KB würde legitime Antworten riskieren abzulehnen; 1 MiB liegt deutlich unter dem Routing-Wert (4 MiB) und begrenzt den Speicher ausreichend — konservative Wahl zugunsten funktionierender Abrufe |
| Fehlereinstufung | `PriceFailure.InvalidResponse` — kein neuer Enum-Wert | Anzeigetexte vorhanden (`PriceTexts.GetCheckMessage`, `PriceTexts.cs:70`; `SearchTexts.cs:319`); `FuelPriceService` überführt jeden `PriceFailure` automatisch in den Rückfall; keine UI-/Enum-/Textänderung nötig. Die Überschreitung ist kein wiederholbarer Fehler: `PriceApiException` wird direkt geworfen und von keinem `catch` in `GetAsync` erfasst (gleiches Muster wie `Rejected`). Davon zu unterscheiden: `IOException`/`HttpIOException` beim begrenzten Körperlesen ist ein Übertragungsfehler und wird — wie `HttpRequestException` — als wiederholbarer Verbindungsfehler eingeordnet (`lastError` → Wiederholung → `Unreachable`), gefilterter `catch` nach Muster `HttpTileSource.cs:238` |
| `MauiProgram.AddPriceServices` | Keine Änderung an der HttpClient-Registrierung | Das Limit liegt testbar in `PriceApiOptions`/Client; `MaxResponseContentBufferSize` am DI-Client wäre bei `ResponseHeadersRead` wirkungslos und würde das Akzeptanzkriterium „Limit durch Tests abgedeckt" nicht erfüllen |
| Handler-Lifetime (Prüfaufgabe) | **Ergebnis:** `SocketsHttpHandler.PooledConnectionLifetime` ist fachlich sinnvoll (Singleton-`HttpClient`s übernehmen DNS-Änderungen sonst nie), wird aber als separates Follow-up empfohlen, da es alle vier Dienste einheitlich betreffen sollte; `IHttpClientFactory` wird nicht empfohlen (neues Paket `Microsoft.Extensions.Http`, kein Mehrwert für Singletons ohne Handler-Pipeline) | Entscheidung gemäß Issue dokumentiert; die Akzeptanzkriterien dieses Changes verlangen keinen Handler-Umbau |

## Programmabläufe

### Größenbegrenzter Abruf (`GetAsync`)

Schritt-für-Schritt des geänderten Kernablaufs (einzige Stelle, die HTTP ausführt und Antwortkörper liest):

1. Vorabprüfungen unverändert: `EndpointNotConfigured` → `PriceApiException(EndpointNotConfigured)`; fehlender Schlüssel → `PriceApiException(ApiKeyMissing)` — vor jeder Netzoperation.
2. Wiederholungsschleife unverändert: `MaxAttempts` Versuche, `RequestThrottle.WaitAsync`, verknüpftes Abbruchsignal `timeout.CancelAfter(_options.RequestTimeout)`.
3. Anfrage per `_httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)` — es werden nur die Kopfzeilen gepuffert.
4. Erfolgsstatus: `response.Content.Headers.ContentLength` > `_options.MaxResponseBytes` → `PriceApiException(PriceFailure.InvalidResponse)` direkt werfen — kein `lastError`, keine Wiederholung, kein Einlesen des Körpers (gleiches Muster wie die `Rejected`-Ablehnung).
5. Sonst: `ReadAsStreamAsync(timeout.Token)` und begrenzendes Lesen in `MemoryStream` mit festem Chunk-Puffer (Muster `HttpTileSource.DownloadAsync`): nach jedem gelesenen Chunk `buffer.Length > _options.MaxResponseBytes` prüfen; Überschreitung → `PriceApiException(PriceFailure.InvalidResponse)` direkt werfen (deckt fehlendes `Content-Length`/Chunked ab).
6. Der Puffer wird an den Anfang zurückgesetzt und per `ParseDocumentAsync` mit `JsonDocument.ParseAsync` ausgewertet: `JsonException` bzw. Nicht-Objekt-Wurzel → `PriceApiException(InvalidResponse)` (bisherige Einstufung, unverändert).
7. Nicht-Erfolgsstatus unverändert: <500 (außer 429) → `PriceApiException(Rejected)`; 429/5xx und Zeitüberschreitung → `lastError` + Wiederholung mit verdoppelter Wartezeit → nach allen Versuchen `PriceApiException(Unreachable)`. Netzwerk- **und Übertragungsfehler** werden ebenfalls als wiederholbare Verbindungsfehler eingeordnet: Der bestehende `catch (HttpRequestException)` wird zu einem gefilterten `catch (Exception ex) when (ex is HttpRequestException or IOException)` erweitert (Muster `HttpTileSource.cs:238`), damit auch `IOException`/`HttpIOException` beim begrenzten Körperlesen (abgeschnittene Antwort, „The response ended prematurely", HTTP/2-/HTTP/3-Stream-Fehler unter .NET ≥ 8 — Körperabbruch ist ein Übertragungsfehler) in `lastError` mündet statt als unklassifizierte Ausnahme aus `GetAsync` zu entkommen. Wie bisher wird nur eine bereinigte `HttpRequestException` übergeben, die den Ausnahmetyp (bei `HttpRequestException` samt `HttpRequestError`) nennt — keine Originalmeldung, keine Anfrageadresse, kein Schlüssel. Die neuen `PriceApiException`s der Größenprüfung sind weder `HttpRequestException` noch `IOException` und werden vom Filter nicht erfasst — sie laufen nie in den Wiederholungszweig; ein nutzerseitiger Abbruch (`OperationCanceledException` bei gesetztem `cancellationToken`) wird ebenfalls nicht erfasst und durchgereicht.
8. Protokollierung unverändert: nie Schlüssel oder vollständige Anfrageadresse; die Fehlermeldung der Größenablehnung enthält ebenfalls weder Adresse noch Schlüssel.

Beteiligte Klassen/Komponenten: `TankerkoenigClient`, `PriceApiOptions`, `PriceApiException`, `PriceFailure`, `HttpClient`, `JsonDocument`.

### Aufruf der drei Endpunkte

`SearchAsync`, `GetDetailAsync`, `GetPricesAsync` rufen `GetAsync` auf und arbeiten mit dem zurückgegebenen `JsonDocument` weiter (`using var document = await GetAsync(...)` statt `body` + `ParseDocument`); `EnsureOk` und das fachliche Parsen (`TryParseStation`, `ParseOpeningTimes`, `AddPrice`) bleiben unverändert.

Beteiligte Klassen/Komponenten: `TankerkoenigClient`, `JsonDocument`.

### Fehlerweitergabe an den Anwender (unverändert)

`FuelPriceService` fängt `PriceApiException` an allen drei Abrufstellen und in `CheckAvailabilityAsync` und bildet `ex.Failure` auf die Ergebnisrecords (`StationSearchResult`/`StationDetailResult`/`StationRefreshResult` mit `PriceDataSource.Cache`/`OfflineFallback`) bzw. den Prüfstatus ab; Anzeige über `PriceTexts`/`SearchTexts`. Durch die Wiederverwendung von `InvalidResponse` ist hier kein Code geändert — der Ablauf ist nur zum Nachverfolgen dokumentiert.

Beteiligte Klassen/Komponenten: `FuelPriceService`, `DataSourceViewModel`, `PriceTexts`, `SearchTexts`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| — | — | Keine neue Produktivklasse. |
| `UnknownLengthContent` (in `FakeHttpMessageHandler`, geschachtelt/privat, Testprojekt) | `HttpContent`-Ableitung | Liefert Test-Antworten ohne `Content-Length`-Kopfzeile (`TryComputeLength` → `false`), um Chunked-Übertragung bzw. fehlende Längenangabe zu simulieren |
| `FailingStream` (in `TankerkoenigClientTests_ResponseSize`, geschachtelt/privat, Testprojekt) | `MemoryStream`-Ableitung | Wirft in `ReadAsync`/`CopyToAsync` eine `IOException`, um den Abbruch des Antwortstroms während des begrenzten Körperlesens zu simulieren (Muster `HttpTileSourceTests_Fetch.cs:220-231`) |
| `TankerkoenigClientTests_ResponseSize` (Testprojekt) | Testklasse auf `TankerkoenigClientTestBase` | Deckt die Größenbegrenzung aller drei Endpunkte sowie die Einstufung von Körperabbrüchen (`IOException` beim Lesen) als wiederholbaren Verbindungsfehler ab (Konvention `TankerkoenigClientTests_<Aspekt>`) |
| `PriceServiceE2ETests_Oversized` (E2E-Projekt) | E2E-Testklasse auf `PriceServiceE2ETestBase` | Anwendersichtbarer Nachweis der kontrollierten Ablehnung über die Verbindungsprüfung |

## Änderungen an bestehenden Klassen

### `PriceApiOptions` (`src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs`)

- **Neue Eigenschaften:** `MaxResponseBytes` (`int`, `init`, Standard `1024 * 1024`) — größtes zulässiges Antwortvolumen des Preisdienstes in Bytes.
- **Geänderte Methoden:** `Validate()` — `MaxResponseBytes < 1` in die Ungültigkeitsbedingung für Zeit-/Wiederholungs-/Grenzwerte aufnehmen (`InvalidOperationException`).
- `FromEnvironment` bleibt unverändert (kein Umgebungsschalter; der Standard gilt auch im Testmodus).

### `TankerkoenigClient` (`src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`)

- **Geänderte Methoden:**
  - `GetAsync` — Rückgabetyp `Task<JsonDocument>` statt `Task<string>`; Anfrage mit `HttpCompletionOption.ResponseHeadersRead`; `Content-Length`-Vorabprüfung und begrenzendes Lesen des Streams gegen `_options.MaxResponseBytes`; bei Überschreitung `PriceApiException(PriceFailure.InvalidResponse)` ohne Wiederholung; Auswertung über `ParseDocumentAsync`. Zusätzlich wird der Fehler-`catch` von `catch (HttpRequestException)` auf `catch (Exception ex) when (ex is HttpRequestException or IOException)` erweitert (Muster `HttpTileSource.cs:238`), damit `IOException`/`HttpIOException` beim Körperlesen als wiederholbarer Verbindungsfehler (`lastError` → Wiederholung → `Unreachable`) eingeordnet wird; die Bereinigung auf Typname/`HttpRequestError` ohne Adresse/Schlüssel bleibt. Signatur des internen Aufrufs bleibt `(relativeQuery, endpointName, cancellationToken)`.
  - `ParseDocument` → `ParseDocumentAsync` (`Stream`/`MemoryStream` + `CancellationToken`, Rückgabe `Task<JsonDocument>`) — `JsonDocument.ParseAsync` statt `JsonDocument.Parse(string)`; Fehlereinstufung (`JsonException`/Nicht-Objekt → `InvalidResponse`) unverändert.
  - `SearchAsync`, `GetDetailAsync`, `GetPricesAsync` — `using var document = await GetAsync(...)` anstelle von `body` + `ParseDocument(body)`; Fachparsing und `EnsureOk` unverändert.
- **Keine** neuen Eigenschaften, Events oder Interface-Änderungen; `ITankerkoenigClient` bleibt unverändert.

### `FakeHttpMessageHandler` (`src/Tankradar.Tests.Unit/Unit/Support/FakeHttpMessageHandler.cs`)

- **Neue Methoden:** `RespondWithoutContentLength(HttpStatusCode status, byte[] body)` — Antwortschritt mit `HttpContent`, dessen `Content-Length` unbekannt bleibt (über `UnknownLengthContent`), zum Prüfen des Limits ohne Längenangabe.
- **Neue interne Klasse:** `UnknownLengthContent` (privat geschachtelt) — schreibt `body` in `SerializeToStreamAsync`, `TryComputeLength` liefert `false`.

### `MockTankerkoenigServer` (`src/TestSupport/MockTankerkoenigServer.cs`)

- **Neue Eigenschaften:** `ResponsePaddingBytes` (`int`, Standard `0`) — hängt an jeden JSON-Antwortkörper die angegebene Zahl insignifikanter Leerzeichen an; der Körper bleibt gültiges JSON, `ContentLength64` wächst entsprechend. Zweck: übergroße Antworten für Integrations- und E2E-Tests erzeugen.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PriceApiOptions.MaxResponseBytes` | `>= 1` | `InvalidOperationException` aus `Validate()` — der `TankerkoenigClient`-Konstruktor lehnt die Einstellungen ab |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `PriceApiOptions.MaxResponseBytes` | `int` (Bytes) | `1024 * 1024` (1 MiB) | Größtes zulässiges Antwortvolumen des Tankerkönig-Dienstes; nicht nutzer- oder umgebungskonfigurierbar |

## Seiteneffekte und Risiken

- **`FuelPriceService`:** Übergroße Antworten erzeugen künftig `PriceFailure.InvalidResponse` und fließen ohne Codeänderung in die bestehenden Cache-/Offline-Rückfälle und Anzeigetexte — beabsichtigte neue Verhaltensklasse.
- **Zeitlimit:** Mit `ResponseHeadersRead` deckt dasselbe `timeout.Token` Kopfzeilenempfang und Stream-Lesen ab; das bisherige Zeitverhalten je Versuch bleibt gewahrt (bisher galt das Token auch für `ReadAsStringAsync`).
- **`MemoryStream`-Position:** Der begrenzte Puffer muss vor `JsonDocument.ParseAsync` an den Anfang zurückgesetzt werden — Implementierungshinweis, kein Code hier.
- **Wiederholungssemantik:** Die Größenablehnung muss per direktem `throw` (nicht über `lastError`) erfolgen, damit sie nicht in den Wiederholungszweig läuft — entspricht dem `Rejected`-Muster.
- **Stream-Lesefehler (`IOException`/`HttpIOException`):** Mit dem neuen Körperlesen über `ReadAsStreamAsync`/Chunk-Puffer können Fehler auftreten, die nicht als `HttpRequestException` auftreten (abgeschnittene Antwort, „The response ended prematurely", HTTP/2-/HTTP/3-Stream-Fehler als `HttpIOException : IOException` unter .NET ≥ 8). Ohne Behandlung entkämen sie als unklassifizierte Ausnahme aus `GetAsync` — `FuelPriceService` fängt nur `PriceApiException`, der `PriceFailure`-Vertrag wäre verletzt. Daher: gefilterter `catch` auf `HttpRequestException or IOException` (Muster `HttpTileSource.cs:238`); ein Körperabbruch ist ein Übertragungsfehler und wird als wiederholbarer Verbindungsfehler (`lastError` → Wiederholung → `Unreachable`) eingeordnet. Die gleiche Exposition bestand bereits beim bisherigen `ReadAsStringAsync` — die Erweiterung schließt also zugleich einen Bestandsfehlerweg. Grenzfälle des Filters: `PriceApiException` (Größe/`Rejected`) und nutzerseitiger Abbruch bleiben unberührt, da sie weder `HttpRequestException` noch `IOException` sind.
- **Bestehende Tests:** Interne Signaturänderung von `GetAsync`/`ParseDocument` betrifft keine öffentliche Schnittstelle; alle vorhandenen Client-Tests rufen nur `SearchAsync`/`GetDetailAsync`/`GetPricesAsync` — keine Anpassung erwartet. `MockTankerkoenigServer`-Antworten bleiben weit unter dem Limit (`SearchE2ETests_LargeResult` mit 300 Stationen ≈ <100 KB).
- **Integrations-/E2E-Clients** erzeugen `new HttpClient` ohne `MaxResponseContentBufferSize` — unverändert wirksam, da das Limit im Client liegt.

## Umsetzungsreihenfolge

1. **`PriceApiOptions.MaxResponseBytes` einführen**
   - Voraussetzungen: Keine.
   - Beschreibung: `init`-Eigenschaft mit Standard 1 MiB; `Validate()` um `MaxResponseBytes < 1` erweitern; XML-Dokumentationskommentar.

2. **`TankerkoenigClient` auf größenbegrenzte Stream-Verarbeitung umbauen**
   - Voraussetzungen: Schritt 1 (`MaxResponseBytes` vorhanden und validiert).
   - Beschreibung: `GetAsync` auf `ResponseHeadersRead`, `Content-Length`-Vorabprüfung und begrenzendes Lesen (Muster `HttpTileSource.DownloadAsync`) umstellen; `ParseDocument` → `ParseDocumentAsync` mit `JsonDocument.ParseAsync`; die drei Aufrufer auf `using var document = await GetAsync(...)` umstellen; Größenüberschreitung als `PriceApiException(InvalidResponse)` ohne Wiederholung; Fehler-`catch` auf `ex is HttpRequestException or IOException` erweitern (Muster `HttpTileSource.cs:238`), damit `IOException`/`HttpIOException` beim Körperlesen als wiederholbarer Verbindungsfehler (`lastError` → `Unreachable`) eingeordnet wird.

3. **`FakeHttpMessageHandler` um Antwort ohne `Content-Length` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `RespondWithoutContentLength` + geschachtelte `UnknownLengthContent`-Klasse.

4. **`MockTankerkoenigServer` um Antwort-Padding erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `ResponsePaddingBytes`-Eigenschaft; Padding beim Schreiben der Antwort anhängen (gültiges JSON bleibt erhalten).

5. **Unit-Tests für die Größenbegrenzung und Optionsvalidierung**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `TankerkoenigClientTests_ResponseSize` anlegen; `PriceApiOptionsTests_Validation` um `MaxResponseBytes`-Fall ergänzen.

6. **Integrationstest: Übergroße Antwort über `FuelPriceService`**
   - Voraussetzungen: Schritte 2 und 4.
   - Beschreibung: Test mit `MockTankerkoenigServer` + `ResponsePaddingBytes` > 1 MiB: `SearchNearbyAsync` liefert `Failure == InvalidResponse` mit Rückfall-`Source`; genau eine Serveranfrage (keine Wiederholung).

7. **E2E-Test: Verbindungsprüfung mit übergroßer Antwort**
   - Voraussetzungen: Schritte 2 und 4.
   - Beschreibung: `PriceServiceE2ETests_Oversized` nach Muster `PriceServiceE2ETests_Unreachable`/`_Reachable`: App gegen Mock mit Padding > 1 MiB, `RunServiceCheck`, Statustext „Der Preisdienst hat eine unerwartete Antwort geliefert." prüfen.

8. **Prüfergebnis Handler-Lifetime dokumentieren**
   - Voraussetzungen: Keine.
   - Beschreibung: Ergebnis der Prüfung zu `SocketsHttpHandler.PooledConnectionLifetime`/`IHttpClientFactory` (Empfehlung: einheitliches Follow-up für alle vier Dienste; `IHttpClientFactory` nicht einführen) in der PR-Beschreibung bzw. Umsetzungsdokumentation festhalten; ggf. Follow-up-Issue anlegen/verlinken.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SearchAsync_OversizedResponse_ThrowsInvalidResponseWithoutRetry` | `TankerkoenigClientTests_ResponseSize` (neu) | `RespondWithBytes` mit Körper > `MaxResponseBytes` (Optionen mit kleinem Limit, z. B. 1 KB) → `PriceApiException` mit `Failure == InvalidResponse`; `Handler.Requests` enthält genau 1 Anfrage; `Delay.Delays` leer (keine Wiederholung) |
| `SearchAsync_OversizedWithoutContentLength_ThrowsInvalidResponse` | `TankerkoenigClientTests_ResponseSize` | `RespondWithoutContentLength` mit übergroßem Körper → gleiche Einstufung und keine Wiederholung (Nachweis, dass das Limit ohne Längenangabe greift) |
| `SearchAsync_ResponseWithinLimit_IsParsed` | `TankerkoenigClientTests_ResponseSize` | Normale Antwort unter dem Limit wird wie bisher geparst (Stationen/Preise) |
| `SearchAsync_ResponseAtExactLimit_IsParsed` | `TankerkoenigClientTests_ResponseSize` | Grenzfall: Antwortkörper exakt am Limit (mit insignifikantem Whitespace aufgefüllt) wird akzeptiert |
| `GetDetailAsync_OversizedResponse_ThrowsInvalidResponse` | `TankerkoenigClientTests_ResponseSize` | `detail.php`: übergroße Antwort → `InvalidResponse`, keine Wiederholung |
| `GetPricesAsync_OversizedResponse_ThrowsInvalidResponse` | `TankerkoenigClientTests_ResponseSize` | `prices.php`: übergroße Antwort → `InvalidResponse`, keine Wiederholung |
| `OversizedError_NeverLeaksUrlOrKey` | `TankerkoenigClientTests_ResponseSize` | Fehlermeldung der Größenablehnung enthält weder Anfrageadresse noch Schlüssel (Konvention der `_Retry`-Tests) |
| `SearchAsync_BodyReadFails_RetriesThenUnreachable` | `TankerkoenigClientTests_ResponseSize` | `Respond` mit `StreamContent(new FailingStream())` — `IOException` beim Körperlesen auf allen Versuchen → `PriceApiException` mit `Failure == Unreachable`; `Handler.Requests` enthält `MaxAttempts` Anfragen; `Delay.Delays` enthält `MaxAttempts - 1` Einträge. Nachweis: Körperabbruch läuft über `lastError` in den Wiederholungszweig und endet klassifiziert statt als unbehandelte `IOException` |
| `SearchAsync_BodyReadFailsOnce_SucceedsOnRetry` | `TankerkoenigClientTests_ResponseSize` | Erster Schritt `FailingStream` (`IOException` beim Lesen), zweiter Schritt normale gültige Antwort → Abruf gelingt und liefert Stationen; `Handler.Requests` == 2 (Nachweis der Wiederholbarkeit des Übertragungsfehlers) |
| `FailingStream` | `TankerkoenigClientTests_ResponseSize` (privat geschachtelt) | `MemoryStream`-Ableitung, die in `ReadAsync`/`CopyToAsync` `IOException` wirft — simuliert den Körperabbruch beim Stream-Lesen (Muster `HttpTileSourceTests_Fetch.cs:220-231`); kein neuer `FakeHttpMessageHandler`-Schritt nötig, `Respond(Func<...>)` reicht |
| `RespondWithoutContentLength` + `UnknownLengthContent` | `FakeHttpMessageHandler` (Hilfsmethode) | Antwortkörper ohne `Content-Length` für Chunked-Simulation |
| `Validate_InvalidMaxResponseBytes_IsRejected` (oder Erweiterung von `Validate_InvalidLimits_AreRejected`) | `PriceApiOptionsTests_Validation` | `MaxResponseBytes < 1` → `InvalidOperationException` |
| `Search_OversizedResponse_FallsBackWithInvalidResponse` | `PriceServiceMockServerTests_Flow` (Integration, `Category=OsInterface`) | `MockTankerkoenigServer.ResponsePaddingBytes` > 1 MiB → `SearchNearbyAsync` liefert `Failure == InvalidResponse` mit Rückfall-`Source`; `_server.ListRequests == 1` (kein erneuter Versuch) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PriceApiOptionsTests_Validation.Validate_InvalidLimits_AreRejected` | Erweiterung um den Fall `MaxResponseBytes < 1`, sofern der neue Fall dort eingegliedert wird (alternativ eigener Test — dann keine Anpassung) |
| Alle übrigen bestehenden Tests | Keine Anpassung erwartet: öffentliche Signaturen, `PriceFailure`-Werte, Anzeigetexte und Testinfrastruktur-Verhalten bleiben unverändert; `FakeHttpMessageHandler`/`MockTankerkoenigServer` werden nur ergänzt |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Anwender löst die Verbindungsprüfung aus (`Settings.PriceService.Check`); der Dienst liefert eine übergroße Antwort; die App meldet „Der Preisdienst hat eine unerwartete Antwort geliefert." | `PriceServiceE2ETests_Oversized` (neu, auf `PriceServiceE2ETestBase`, Muster `PriceServiceE2ETests_Unreachable`) | Übergroße Antworten kontrolliert ablehnen; konsistente Fehlerbehandlung mit anderen externen Diensten | Einziger anwendersichtbarer Weg der neuen Fehlerklasse: prüft den vollständigen Pfad UI-Aktion → `FuelPriceService` → `TankerkoenigClient` → Anzeigetext über echten HTTP-Verkehr (`MockTankerkoenigServer` mit `ResponsePaddingBytes` > 1 MiB) |
| Bestand (Happy Path) | Umkreissuche/Verbindungsprüfung mit normaler Antwort funktioniert unverändert | `PriceServiceE2ETests_Reachable.ServiceCheck_AgainstMock_ReportsReachable`, `SearchE2ETests_*`, `SearchE2ETests_LargeResult` (300 Stationen) | Normale Antworten funktionsfähig | Der Benutzerfluss ändert sich nicht; die Bestandstests prüfen exakt diesen Weg und bleiben weit unter dem Limit — kein neuer Test erforderlich |

Bestehende E2E-Tests: Keine Anpassung erforderlich — die Mock-Antworten aller Suiten bleiben weit unter 1 MiB; `PriceServiceE2ETests_Unreachable` und `SearchE2ETests_LargeResult` laufen unverändert.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Umkreissuche mit normaler Antwort | Nutzer öffnet die Karte bzw. löst eine Suche aus (systemgetrieben über `MapViewModel` → `FuelPriceService.SearchNearbyAsync`) | `PriceApiOptions` aus `FromEnvironment` mit Standard-`MaxResponseBytes` (1 MiB); API-Schlüssel hinterlegt; keine gesonderte Sitzung nötig — fehlende interne Objekte entstehen nicht (Optionen werden stets erzeugt und validiert) | `SearchAsync` → `GetAsync` mit `ResponseHeadersRead` → `Content-Length` ≤ Limit → begrenztes Lesen → `JsonDocument.ParseAsync` → `EnsureOk` → Stationsparsing | Stationen/Preise erscheinen wie bisher (`PriceDataSource.Live`); keine Fehlermeldung | Bestehende Tests bleiben grün: `Search_AgainstMock_ReturnsStationsAndPrices` (Integration), `PriceServiceE2ETests_Reachable`, `SearchE2ETests_*`; neu: `SearchAsync_ResponseWithinLimit_IsParsed`, `SearchAsync_ResponseAtExactLimit_IsParsed` |
| 2 | Übergroße Antwort mit `Content-Length` | Nutzer tippt „Preisdienst prüfen" (oder Suche); der Server sendet > 1 MiB mit Längenangabe | Wie Szenario 1 | `GetAsync` empfängt Kopfzeilen → `Content-Length` > `MaxResponseBytes` → `PriceApiException(InvalidResponse)` ohne Einlesen und ohne erneuten Versuch → `FuelPriceService`/`DataSourceViewModel` bilden den Grund ab | Statustext „Der Preisdienst hat eine unerwartete Antwort geliefert." bzw. Rückfall auf zuletzt bekannte Preise; genau eine Serveranfrage; kein unbegrenzter Speicherverbrauch | `SearchAsync_OversizedResponse_ThrowsInvalidResponseWithoutRetry` (Unit), `Search_OversizedResponse_FallsBackWithInvalidResponse` (Integration), `PriceServiceE2ETests_Oversized` (E2E) |
| 3 | Übergroße Antwort ohne `Content-Length` (Chunked) | Wie Szenario 2, Server sendet ohne Längenangabe | Wie Szenario 1 | Vorabprüfung entfällt → begrenzter Stream-Lesevorgang → Puffer überschreitet `MaxResponseBytes` → `PriceApiException(InvalidResponse)` ohne Wiederholung | Wie Szenario 2 | `SearchAsync_OversizedWithoutContentLength_ThrowsInvalidResponse` (Unit, via `RespondWithoutContentLength`) |
| 4 | Verweigerte Zustände bleiben unverändert (Blockade ohne Netzoperation) | Aufruf im Testmodus ohne Endpunkt oder ohne hinterlegten Schlüssel | `EndpointNotConfigured` bzw. `ApiKeyMissing` — bewusst ungültige Ausgangszustände | Vorabprüfung in `GetAsync` greift weiterhin vor jeder Netzoperation; Größenprüfung kommt nicht zum Zug | Bestehende Fehlertexte; keine Anfrage verlässt die App | Bestehende Tests bleiben grün: `*_EndpointNotConfigured_RefusesWithoutRequest`, `SearchAsync_WithoutKey_FailsWithoutRequest` |

## Offene Punkte

Keine. Die vier in `requirement.md` dokumentierten offenen Fragen wurden fachlich begründet entschieden: Grenzwert 1 MiB (siehe Designentscheidungen), Fehlereinstufung `PriceFailure.InvalidResponse`, Variante B (Streaming-Verarbeitung mit clientseitigem zweistufigem Limit) und Zurückstellung der Handler-Lifetime-Umstellung als dokumentiertes Prüfergebnis mit Follow-up-Empfehlung.
