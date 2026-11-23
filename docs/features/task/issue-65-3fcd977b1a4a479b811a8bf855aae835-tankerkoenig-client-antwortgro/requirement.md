# Übersetzte Anforderung – Issue #65: Tankerkönig-Client: Antwortgröße begrenzen und JSON streaming verarbeiten

## Fachliche Zusammenfassung

Der `TankerkoenigClient` verarbeitet Antworten des Preisdienstes künftig größenbegrenzt: Übergroße Antworten werden kontrolliert abgelehnt, statt wie bisher vollständig und unbegrenzt über `ReadAsStringAsync` in den Speicher gelesen zu werden. Damit wird das Schutzniveau an die übrigen externen Dienste angeglichen (Nominatim: `MaxResponseContentBufferSize = 256 KB`; OpenRouteService: `4 MB`) und die Fehlereinstufung bleibt über `PriceApiException`/`PriceFailure` konsistent. Ergänzend ist zu prüfen, ob `SocketsHttpHandler.PooledConnectionLifetime` oder `IHttpClientFactory` für sauberes Handler- und DNS-Verhalten eingeführt wird.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine HTTP-Antwort des Tankerkönig-Preisdienstes je Einzelabruf. Davon betroffen sind alle drei Endpunkte: `list.php` (Umkreissuche, `SearchAsync`), `detail.php` (Tankstellendetails, `GetDetailAsync`) und `prices.php` (Preisabfrage für 1 bis `TankerkoenigClient.MaxPriceIds` = 10 Kennungen, `GetPricesAsync`).
- **Auslösende Ereignisse:** Jeder Aufruf der `ITankerkoenigClient`-Methoden — indirekt ausgelöst durch Umkreissuche, Stationsdetail-Anzeige und Preisaktualisierung über `FuelPriceService` (nutzer- und systemgetrieben). Kein neuer Nutzer-Trigger.
- **Erwartete Wirkung:**
  - Antworten bis zum festgelegten Limit werden wie bisher geparst und fachlich ausgewertet; die bestehende Funktionalität (Umkreissuche, Details, Preise) bleibt unverändert.
  - Antworten über dem Limit werden kontrolliert abgelehnt — ohne unbegrenzten Speicherverbrauch — und als `PriceApiException` mit einem passenden `PriceFailure`-Grund nach oben gereicht, sodass `FuelPriceService` und die Oberfläche sie wie andere Preisdienst-Fehler behandeln (z. B. Anzeigetext „unerwartete Antwort" bzw. Fallback auf Cache/letzte bekannte Preise).
- **Umgang mit fehlenden Objekten:**
  - **Fehlende `Content-Length`-Kopfzeile:** Das Größenlimit muss auch greifen, wenn der Server die Länge nicht vorab sendet (z. B. Chunked-Übertragung) — entweder über `MaxResponseContentBufferSize` oder über begrenztes Lesen des Antwortstreams. Eine reine `Content-Length`-Prüfung allein ist nicht ausreichend.
  - **API-Schlüssel/Endpunkt:** Die bestehenden Vorabprüfungen (`EndpointNotConfigured`, `ApiKeyMissing`) bleiben unverändert und greifen vor jeder Netzoperation.
- **Scope-Ausschlüsse:**
  - Keine Änderung am Anfrageverhalten: Zeitlimit je Anfrage, Wiederholung mit verdoppelter Wartezeit (`MaxAttempts`, `RetryBaseDelay`), Drosselung (`RequestThrottle`), HTTPS-Pflicht und das Verbot automatischer Weiterleitungen bleiben bestehen.
  - Keine UI-Änderung; die Fehleranzeige erfolgt über die vorhandenen `PriceFailure`-Texte (`PriceTexts`, `SearchTexts`).
  - Der Karten-Kachelclient (`HttpTileSource`) und die übrigen Dienste (Nominatim, OpenRouteService) sind nicht Teil des Scopes — ihre Limits sind bereits gesetzt.
  - `SocketsHttpHandler.PooledConnectionLifetime`/`IHttpClientFactory` ist laut Issue eine Prüfaufgabe („ergänzend prüfen"), keine Pflichtmaßnahme; die Entscheidung ist im Ergebnis zu dokumentieren.
  - Keine konfigurierbare Nutzereinstellung für das Größenlimit.

## Betroffene Klassen und Komponenten

- **Logikklassen / Services (zu ändern):**
  - `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs` — `GetAsync` (Zeile 193–252) liest den Erfolgskörper per `ReadAsStringAsync` (Zeile 222); `ParseDocument` (Zeile 254–271) parst aus dem String. Hier entsteht die Begrenzung bzw. die Stream-Verarbeitung.
  - `src/Tankradar.MAUI/MauiProgram.cs` — `AddPriceServices` (Zeile 302–339): Die HttpClient-Registrierung (Zeile 312–318) setzt bisher weder `MaxResponseContentBufferSize` noch `PooledConnectionLifetime`; hier ist der Client analog zu `AddGeocodingServices` (Zeile 208–212) und `AddRoutingServices` (Zeile 233–237) zu ergänzen.
- **Datenmodellklassen (ggf. zu erweitern):**
  - `PriceApiOptions` (`src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs`) — optional um eine Eigenschaft für das Antwortgrößenlimit mit fachlich begründetem Standardwert; dann in `Validate()` mitprüfen.
  - `PriceFailure` (`src/Tankradar.MAUI/Models/Pricing/PriceFailure.cs`) — nur falls ein eigener Fehlergrund für übergroße Antworten gewählt wird; `InvalidResponse` („nicht auswertbar") wäre bereits vorhanden und mit Anzeigetexten hinterlegt (`PriceTexts`, `SearchTexts`).
- **Interfaces:** `ITankerkoenigClient` bleibt unverändert (Signaturen und `PriceApiException`-Vertrag unverändert).
- **Enums:** siehe `PriceFailure` oben.
- **UI-Komponenten / Controller:** keine.
- **Tests:**
  - Neue Testklasse nach bestehender Konvention `TankerkoenigClientTests_<Aspekt>` in `src/Tankradar.Tests.Unit/Unit/` (Vorschlag: `TankerkoenigClientTests_ResponseSize.cs`), abgeleitet von `TankerkoenigClientTestBase`.
  - `FakeHttpMessageHandler` (`src/Tankradar.Tests.Unit/Unit/Support/FakeHttpMessageHandler.cs`) kann über `RespondWithBytes` bereits beliebig große Antwortkörper liefern; ggf. ist ein Antwortschritt ohne `Content-Length` zu ergänzen, wenn das Limit ohne Längenangabe geprüft werden soll.
  - Wichtig: Liegt das Limit nur in der DI-Registrierung (`MaxResponseContentBufferSize` am erzeugten `HttpClient`), deckt der bisherige Testaufbau (`new HttpClient(Handler)` in `TankerkoenigClientTestBase.CreateClient`) das Akzeptanzkriterium „Größenbegrenzung durch Tests" nicht ab — das Limit muss dann testbar im Client oder über `PriceApiOptions` verankert werden bzw. der Test den konfigurierten Client nachbauen.

## Implementierungsansatz

Das Issue nennt zwei gleichwertige Varianten; die Entscheidung ist im Planungsschritt zu treffen:

- **Variante A – Pufferlimit am `HttpClient`:** `MaxResponseContentBufferSize` mit fachlich begründetem Wert auf den Preis-`HttpClient` in `AddPriceServices` setzen (konsistent zu Nominatim/Routing). Zu beachten: Bei Überschreitung wirft `GetAsync`/`ReadAsStringAsync` eine `HttpRequestException`, die im bisherigen Wiederholungszweig landet und nach `MaxAttempts` Versuchen als `PriceFailure.Unreachable` endet — unnötige Wiederholungen und eine irreführende Einstufung. Für „kontrolliert abgelehnt" ist die Überschreitung daher entweder vor dem Lesen anhand `Content-Length` zu prüfen oder die Ausnahme gezielt als nicht wiederholbaren Fehler (z. B. `PriceFailure.InvalidResponse`) zu behandeln.
- **Variante B – Streaming-Verarbeitung:** Abruf mit `HttpCompletionOption.ResponseHeadersRead` und Auswertung per `JsonDocument.ParseAsync` über `ReadAsStreamAsync` (Muster siehe `OpenRouteServiceKeyChecker`/`HttpTileSource` für `ResponseHeadersRead` bzw. `AppConfiguration` für `JsonDocument.ParseAsync`). Achtung: `MaxResponseContentBufferSize` greift bei `ResponseHeadersRead` **nicht**; ein Größenlimit muss dann eigenständig durchgesetzt werden (`Content-Length`-Prüfung plus begrenzendes Lesen des Streams).
- Empfehlung (Annahme, im Plan zu bestätigen): Limit nahe am Client durchsetzen — als Konstante in `TankerkoenigClient` oder als Option in `PriceApiOptions` — statt ausschließlich in der DI-Registrierung, damit die Begrenzung ohne Netzwerk über die vorhandene Test-Infrastruktur abgedeckt werden kann. Übergroße Antworten sollten nicht wiederholt werden: Einstufung als nicht wiederholbarer `PriceFailure` (naheliegend `InvalidResponse`).
- Erweiterungspunkte: `TankerkoenigClient.GetAsync` (einzige Stelle, die Erfolgskörper liest) und `ParseDocument` (String → ggf. Stream/`JsonDocument.ParseAsync`).
- Ergänzende Prüfung: `SocketsHttpHandler.PooledConnectionLifetime` bzw. `IHttpClientFactory`. Alle vier Dienste nutzen aktuell manuell erzeugte Singleton-`HttpClient`s mit `HttpClientHandler` (kein `SocketsHttpHandler`); die Entscheidung (mitnehmen oder als Follow-up begründet zurückstellen) ist zu dokumentieren.
- Konsistenz: Fehlerbehandlung und Protokollierung folgen dem bestehenden Muster — nie den API-Schlüssel oder die vollständige Anfrageadresse protokollieren (vgl. Kommentar Zeile 239), Anzeigetexte über `PriceTexts`/`SearchTexts`.

## Konfiguration

- Das Antwortgrößenlimit ist nicht nutzerkonfigurierbar; Vorschlag: feste Konstante im `TankerkoenigClient` oder `init`-Eigenschaft in `PriceApiOptions` mit fachlich begründetem Standardwert. Begründungshilfe für den Wert: `detail.php` und `prices.php` (≤ 10 Kennungen) liefern kleine Dokumente; `list.php` bei `MaxRadiusKm` = 25 km liefert die größten Antworten (drei- bis niedriger vierstelliger KB-Bereich je nach Stationsdichte). Ein Limit im Bereich 256 KB (Nominatim-Vergleichswert) bis 1 MB erscheint angemessen; der genaue Wert ist festzulegen.
- Keine neue Umgebungsvariable nötig; `PriceApiOptions.FromEnvironment` und der Testmodus bleiben unverändert.

## Offene Fragen

1. **Konkreter Grenzwert:** Welcher Wert gilt als fachlich begründet (z. B. 256 KB analog Nominatim oder 1 MB als Reserve für dichte Ballungsräume)? Ggf. mit realen Antwortgrößen von `list.php` bei 25 km Radius plausibilisieren.
2. **Fehlereinstufung übergroßer Antworten:** Reicht `PriceFailure.InvalidResponse` (bestehender Anzeigetext „unerwartete Antwort") oder ist ein eigener `PriceFailure`-Wert mit eigenem Text gewünscht?
3. **Variantenwahl:** Nur `MaxResponseContentBufferSize` (minimalinvasiv) oder Stream-Verarbeitung per `JsonDocument.ParseAsync` mit eigenem Limit (stärkerer Umbau von `GetAsync`/`ParseDocument`)? Beide Varianten erfüllen das Issue; die Wahl beeinflusst die Testbarkeit und die Fehlereinstufung.
4. **Handler-Lifetime:** `SocketsHttpHandler.PooledConnectionLifetime`/`IHttpClientFactory` im selben Change mitziehen (betrifft dann idealerweise alle vier Dienste gleichermaßen) oder als separates Follow-up dokumentieren?
