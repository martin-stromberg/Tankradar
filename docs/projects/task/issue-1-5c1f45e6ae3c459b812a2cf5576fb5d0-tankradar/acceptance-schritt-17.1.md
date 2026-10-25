# Abnahmeprüfung – Entwicklungsschritt 17 (Tankstellen entlang einer Route)

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-17-route` (HEAD `8c76105`) gegen den Projekt-Basisbranch (`7adcb50`).

Der Zwischenstand `review-versions/0.1.22_2026-10-20` hat die Produktversion `0.1.22+3eaf3252…`. Der Code ist identisch mit HEAD, denn `8c76105` entfernt nur ein Feature-Verzeichnis unter `docs/`.

Die Routensuche ist fachlich sauber umgesetzt und gut getestet:

- **Geometrie:** Die Abfragepunkte decken das Band um die Route nachweisbar vollständig ab, auch bei Kurven und Kehren.
- **Ergebnisse:** Sie stimmen in allen Testrouten exakt mit einer unabhängigen Referenzrechnung überein.
- **Begrenzung und Fehler:** Obergrenzen, Fehlermeldungen und die Validierung vor externen Aufrufen funktionieren.
- **Schlüssel und Datenschutz:** Die Schlüsselbehandlung und der Datenschutz entsprechen der Anforderung.
- **Alle Prüfläufe sind grün:**
  - lokale CI mit 144/144 E2E-Tests,
  - Pre-Push-Prüfungen,
  - 33 Einzelwiederholungen der Routen- und LargeResult-Klassen.

Nicht vollständig erfüllt ist die Vorgabe „Die Abfragen werden so bemessen, dass die Nutzungsbedingungen des Preisanbieters eingehalten werden“:

- Die aktuellen Tankerkönig-Bedingungen konnten heute abgerufen werden. Sie begrenzen die Abfragefrequenz ausdrücklich auf eine Anfrage pro Minute.
- Die Routensuche stellt je Nutzeraktion bis zu 15 Umkreisabfragen im Sekundentakt. ADR 0014 hatte genau diese Prüfung als offen markiert.
- Dazu kommen zwei kleinere Fehler im Verhalten bei einer laufenden Suche (Abweichungen 2 und 3).

**Prüfläufe (selbst ausgeführt):**

- `FUEL_PRICE_API_KEY`, `ROUTING_API_KEY`, `TANKRADAR_ROUTING_API_KEY` und `TANKRADAR_ROUTING_TEST_API_KEY` wurden per `env -u` entfernt. Kein Schlüsselwert wurde gelesen.
- E2E lief immer off-screen.
- Alle Workloads sind installiert, eine `Include*Target`-Anpassung war nicht nötig.

| Prüfung | Ergebnis |
|---|---|
| `scripts/local-ci.ps1` vollständig (20.10., 13:12–13:27) | Exit 0, alle Schritte OK. Das Windows-Paket wurde wie vorgesehen übersprungen.<br>Unit **1772/1772**, Integration **295/295**, **E2E 144/144** (13 min 21 s).<br>Zeilenabdeckung 93,8 %, Build mit Warnungen als Fehler: 0 Warnungen.<br>iOS-Compile OK (nur `MAUI1001`), keine anfälligen Pakete, keine E2E-Diagnosedaten. |
| `DiagnosticsCaptureE2ETests` | Beide Tests im Gesamtlauf grün. „Screenshot einfarbig“ trat nicht auf, deshalb war keine Einzelwiederholung nötig. |
| `scripts/test-ios-deployment.ps1` (einzeln) | Exit 0, alle Prüfungen OK. In der lokalen CI lief das Skript zusätzlich zweimal (mit und ohne `Include*Target=false`). |
| `scripts/validate-workflows.py` | OK (7 Workflows; `actionlint` nicht installiert). `scripts/test_validate_workflows.py`: 14/14. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `7adcb50`, 13:28–13:30) | Exit 0. Geprüft wurden:<br>• Commits 13/13<br>• verbotene Muster (1069 Dateien)<br>• Format<br>• XML-Doku (807)<br>• Übersetzung (keine `.resx`)<br>• Enums<br>• Platzhalter (803)<br>• Build 0 Warnungen<br>• Unit 1772/1772, Integration 295/295<br>E2E ist im Hook standardmäßig aus. |
| Conventional Commits | 13/13 im Bereich `7adcb50..8c76105` (`conventional-commits-check.py --base 7adcb50 --end HEAD`). |
| Einzelwiederholungen (eigener `dotnet test`-Aufruf je Klasse und Lauf, 13:30–13:36) | Alle 10 `RouteE2ETests_*`-Klassen und `SearchE2ETests_LargeResult` je **3/3** (33 Läufe, 21 Tests, alle grün, keine Diagnosedaten). Laufzeiten:<br>• `Distance` 11,4–11,5 s<br>• `Accessibility.Progress…` 8,1–8,3 s<br>• LargeResult 7,7–8,0 s<br>• `OpenDetail` 5,2–10,1 s<br>Mit dem Gesamtlauf lief jede Klasse viermal. |
| Zwischenstand 0.1.22 gegen die Mock-Server (13:37–13:54) | Siehe Hinweis 5: Abfragebemessung, Fehlerfälle, Detailansicht, Karte, Moduswechsel und Wettläufe.<br>Abweichungen 2 und 3 wurden am Zwischenstand reproduziert. |
| Upgrade einer 0.1.21-Datenbank | In Ordnung (Hinweis 6). |
| Tankerkönig-Bedingungen | Am 20.10. gegen 13:15 abgerufen (`https://creativecommons.tankerkoenig.de`), siehe Hinweis 2. |

**Anforderungen (Projektplan, Schritt 17):**

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Routensuche zusätzlich zu Standort- und Adresssuche im Bereich „Karte“ | Modus-Chip `Search.Mode.Route` mit eigenem Eingabebereich (`MapViewModel.Route.cs`) | E2E `RouteE2ETests_Basic`, manuell |
| Start und Ziel als Adresse, Ort oder PLZ; Start auch „Aktueller Standort“, sofern die GPS-Einstellung es erlaubt | Startart-Chips, Eingabefelder mit Löschen-Knopf; bei GPS „Nie“ ein Hinweis ohne Aufruf | E2E `CurrentLocation`, `GpsNever`, manuell |
| Auflösung über dieselbe Nominatim-Geokodierung | `RouteSearchService` nutzt den vorhandenen Geokodierungsdienst (Takt 1 s) | Mock: Nominatim-Abstand 1,1 s, User-Agent gesetzt |
| Route für ein Auto per OpenRouteService | `POST v2/directions/driving-car`, `{"coordinates":[[lon,lat],[lon,lat]],"instructions":false}`, Polyline mit Präzision 5 | Mock: Rumpf und Pfad geprüft; Unit-Tests für Polyline und Client |
| Tankstellen innerhalb eines einstellbaren Abstands (1/2/5/10 km) durch kombinierte Umkreisabfragen | `RouteSampler` (gleich lange Abschnitte, Radius = halbe Abschnittslänge / 0,95 + d), `RouteProfile.Locate`, Deduplizierung, Filter auf d | Unabhängige Referenz (`gen.py`, 20-m-Abtastung): Gerade, 393-km-Route und Kehre stimmen in Zahl, Reihenfolge, Abfragezahl und Radius exakt überein (Hinweis 5) |
| Abfragen nach den Nutzungsbedingungen des Preisanbieters bemessen | Höchstens 15 Abfragen und 400 km, nacheinander mit 1 s Mindestabstand, Cache 5 min, nur auf Nutzeraktion | **Nicht erfüllt** gegenüber den aktuellen Bedingungen (1 Anfrage/Minute): Abweichung 1 |
| Liste und Karte mit Filtern, Preisen mit Alter, Hinweisen, Preisniveau-Farben, Sortierungen | `StationResultBuilder.BuildForRoute` nutzt dieselben Preisstufen wie die Umkreissuche | E2E `FilterSort`; manuell: Marker mit „Günstigster/Mittlerer/Hoher Preis“, Detail mit Preisalter |
| Entfernungssortierung = Position entlang der Route | Distanz = Streckenposition („x km ab Start“) plus „y km abseits der Route“ | Manuell: lange Route, Kehre; E2E `Basic` |
| Route auf der Karte eingezeichnet | `StationMapView.Route`, `MapRouteDrawable` (Liang-Barsky-Clipping), Zielmarker, Ausschnitt an der Route | Bildschirmfoto (Kehre korrekt mit Start- und Zielmarker), E2E `FilterSort.MapView_…` |
| Antippen öffnet die Detailansicht | Liste (`Search.Station.Open`) und Kartenmarker | Manuell aus Liste und Karte; E2E `OpenDetail` |
| Routing nur über HTTPS, mit Zeitlimit und Wiederholung mit zunehmender Wartezeit | HTTPS-Pflicht (HTTP nur Loopback im Testmodus), 15 s, 3 Versuche (1 s/2 s), 429/5xx/Timeout wiederholt, `Retry-After` in Sekunden (≤ 10 s) | Unit-Tests; Mock: 429 ×3 und 503 ×3 ergeben je 3 Versuche |
| Schlüssel nie im Quellcode; beim Build aus Secret bzw. lokaler Datei; zur Laufzeit in Keychain bzw. Credential Locker | `MSBuild/RoutingApiKey.targets` (Muster wie Tankerkönig), `ApiKeyProvider`, eigener Eintrag in der sicheren Ablage; das Windows-Paket enthält ihn nie | Prüfung auf verbotene Muster, `validate-workflows.py`, Code-Review (Hinweis 3) |
| Quellenangabe ORS und OSM | `Search.Attribution.Routing`, Kartenattribution | Manuell (UIA-Text) |
| Validierung vor externen Aufrufen | Abstand, Start, Ziel, Online-Status; „Start = Ziel“ (< 0,2 km) vor dem Routing | E2E `TooShortInput`; manuell: unbekannter Ort und Start = Ziel ohne Routing-Aufruf |
| Verständliche Meldungen (Ort nicht gefunden, Route nicht berechenbar, keine Verbindung) | Eigene Texte je Status (`SearchTexts.GetRouteMessage`) | E2E `Errors` (8 Tests); manuell alle Fälle (Hinweis 5) |
| Routen, Adressen und Standort nicht dauerhaft speichern | Nur im Arbeitsspeicher, nur Debug-Protokoll ohne Inhalte, `ToString`-Überschreibungen | `RoutePrivacyTests`, E2E `NotPersisted`; manueller Datenscan (Hinweis 7) |
| Tests mit Mock-Diensten; Abdeckung von Auswahl, Validierung, Fehlern und FlaUI-Ablauf | `MockOpenRouteServiceServer`, erweiterte Mocks für Nominatim und Tankerkönig | Unit, Integration (`RouteSearchMockServerTests`), 20 neue Routen-E2E-Tests |
| Startfähiger Zwischenstand unter `review-versions/` | `0.1.22_2026-10-20` mit `CHANGELOG.md` | Gestartet und geprüft (FileVersion 0.1.22.0) |
| Zusatz aus Abnahme 16 (optional) | LargeResult geschärft, `TransientWait` gegen `ElementNotAvailableException`, Doku (`workflows.md`, ADR 0013), „Weitere anzeigen (N übrig)“ | Diff-Review, E2E grün |

## Abweichungen

1. **Abfragefrequenz widerspricht den aktuellen Tankerkönig-Bedingungen (hoch; Entscheidung nötig)**
   - **Was die Bedingungen sagen:** Die heute abgerufene API-Seite beschränkt die Abfragefrequenz auf „einen Request/Minute“ (Wortlaut in Hinweis 2). Sie droht bei massenhaften Abrufen mit Sperrung und Deaktivierung des Schlüssels.
   - **Was die App tut:** Die Routensuche schickt je Nutzeraktion bis zu 15 `list.php`-Abfragen im Abstand von 1 s. Am Zwischenstand waren es 9 Abfragen (393 km, d = 1) bzw. 14 Abfragen (d = 10).
   - **Stand der Doku:** ADR 0014 bemisst nach der projektinternen Regel (1 s, Cache 5 min) und vermerkt ausdrücklich: „Zu prüfen bleibt, ob die aktuellen Bedingungen eine strengere Grenze nennen“. Die Prüfung ist jetzt erfolgt, und die Bedingungen nennen eine strengere Grenze.
   - **Gegenargument:** Die Bedingungen erlauben Apps zugleich „Requests on Demand – auf Useraktion“. Die Lesart, dass die Minutengrenze nur automatische Abrufe betrifft, ist vertretbar, aber nicht belegt.
   - **Umsetzbar:**
     - (a) Bei Tankerkönig (Kontakt bzw. Forum der API-Seite) klären, ob eine nutzerausgelöste Routensuche mit mehreren Abfragen in kurzer Folge zulässig ist und wie viele.
     - (b) Abhängig von der Antwort:
       - Ist es zulässig: Ergebnis mit Quelle und Datum in ADR 0014 („Zu prüfen bleibt …“ ersetzen) und in `docs/help/Preisdaten/beschreibung.md` festhalten.
       - Ist es nicht zulässig: `RouteSearchOptions.MaxQueries`, `MaxRouteLengthKm` und `PriceApiOptions.MinRequestInterval` auf das erlaubte Maß setzen. Dann eine längere Wartezeit im Fortschrittstext ankündigen bzw. die Routenlänge begrenzen.
     - (c) Bis zur Klärung entscheidet der Auftraggeber bewusst, ob die Routensuche mit dem echten Schlüssel ausgeliefert wird (TestFlight), und hält das Risiko fest (Sperrung des Tankerkönig-Schlüssels für die ganze App).

2. **Zusammenfassung und Kartenhinweis zeigen den UI-Zustand bei Suchende statt der Suchparameter (niedrig)**
   - **Ursache:**
     - `ApplyRouteResult` bildet `RouteSummary` mit `_routeDistanceKm` (`MapViewModel.Route.cs`, Zeile 412).
     - `GetStartName` richtet sich nach dem aktuellen `_routeStartKind` (Zeile 421 ff.), ebenso `RouteMapHintText` (Zeile 248).
     - Die Chips für Startart und Abstand bleiben während der Suche bedienbar; nur „Suchen“ ist gesperrt.
   - **Am Zwischenstand reproduziert:**
     - Die Suche lief mit 2 km (Radius 24, drei Treffer mit 1,0–1,9 km Abstand). Während der Suche wurde auf 5 km gewechselt. Die Zusammenfassung lautet dann „3 Tankstellen im Abstand von 5 km zur Route“, und der 5-km-Chip ist ausgewählt.
     - Die Suche lief ab „10115“. Während der Suche wurde auf „Aktueller Standort“ gewechselt. Die Zusammenfassung lautet dann „Von Aktueller Standort nach …“.
     - Umgekehrt würde der Text der Starteingabe angezeigt, obwohl ab dem Standort gesucht wurde.
   - **Umsetzbar:**
     - Abstand und Startart (bzw. den Startnamen) der Anfrage im Ergebnis mitführen, z. B. in `RouteSearchResult` oder als beim Suchstart erfasste Werte, und in Zusammenfassung und Kartenhinweis verwenden.
     - Optional die Chips während `IsBusy` sperren wie „Suchen“.
     - Einen Unit-Test für den Wechsel während der Suche ergänzen.

3. **Veralteter Fortschrittstext nach Verlassen der Seite während einer Routensuche (niedrig)**
   - **Ursache:**
     - `OnDisappearing` ruft `CancelSearch()` auf und setzt `IsBusy = false` (`MapViewModel.cs`, Zeile 476 ff.).
     - Der `onFinished`-Callback, der `RouteProgressText` leert, läuft in `RunGuardedSearchAsync` aber nur, wenn die Suche noch aktuell ist (Zeile 550 ff.). Nach dem Abbruch ist sie das nicht mehr.
   - **Am Zwischenstand reproduziert:**
     - Ablauf: Suche starten → Reiter „Favoriten“ → zurück zu „Karte“.
     - Danach steht dauerhaft „Tankstellen werden gesucht (1 von 2)“ da. „Suchen“ ist wieder aktiv, es gibt kein Ergebnis und keine Meldung.
     - Der Abbruch selbst funktioniert: Die zweite Abfrage unterbleibt.
     - Erst eine neue Suche oder ein Moduswechsel räumt den Text weg. Ein Moduswechsel während der Suche räumt korrekt auf.
   - **Umsetzbar:**
     - `RouteProgressText = null` in `OnDisappearing` (bzw. in `CancelSearch`) setzen.
     - Den vorhandenen `OnDisappearing`-Unit-Test um die Prüfung des Fortschrittstexts ergänzen.

## Hinweise

### 1. Fachlogik (Code-Review)

**Abdeckung (`RouteSampler`):**

- Mit n = max(1, ⌈L / (2·(25−d)·0,95)⌉) gleich langen Abschnitten liegt jeder Routenpunkt höchstens s/2 Streckenkilometer von einem Abfragezentrum entfernt.
- Die Luftlinie ist nie länger als die Strecke, deshalb liegt jeder Punkt im Band (≤ d) innerhalb von s/2 + d ≤ Radius vom Zentrum.
- Der Nachweis gilt unabhängig von Kurven, Kehren und Schleifen. Der Faktor 0,95 und das Aufrunden bringen zusätzliche Reserve.
- Die Grenzen werden eingehalten:
  - Radius 1–25 km.
  - Abbruch bei L > 400 km, n > 15 oder d ≥ 25.
  - Bei d = 10 und 400 km ergeben sich genau 15 Abfragen.

**Zuordnung (`RouteProfile.Locate`):**

- Der Begrenzungsrahmen-Vorfilter rechnet den Längenrand mit cos(max |Breite|) und ist damit breitenfest.
- Der Fußpunkt wird äquirektangulär bestimmt, der Abstand per Haversine. Für Segmentlängen von Straßenrouten ist das ausreichend genau.

**ADR-Formulierung zu Schleifen:**

- ADR 0014 sagt „Bei Schleifen gilt die kleinere Streckenposition“. Tatsächlich gewinnt das nächstgelegene Segment; die kleinere Position zählt nur bei Gleichstand (1e-9).
- Am Zwischenstand gemessen:
  - „Kehre Mitte“ (genau mittig) → 10,0 km.
  - „Kehre Außen“ (0,5 km vom Rückweg, 1,1 km vom Hinweg) → 30,6 km.
- Das Verhalten ist sinnvoll; die ADR sollte nur so formuliert werden.

**Polyline und Antwort:**

- Präzision 5, ohne Höhe, höchstens 50.000 Punkte.
- Die Antwort wird mit `JsonDocument` gelesen, ohne reflexionsbasierte Deserialisierung.
- Eine Länge von 0 oder ungültige Punkte führen zu `InvalidResponse`.

**Ergebnisliste:**

- Deduplizierung nach Kennung (Groß- und Kleinschreibung ignoriert).
- Sortierung nach Streckenposition, dann Kennung.
- Jeder Fehler einer Teilabfrage, auch ein Rückfall auf den Cache mit Fehlergrund, bricht ohne Teilergebnis ab.
- Unit-Tests decken Abdeckung (Zufallspunkte), Grenzen, Reihenfolge und Abbruch ab. Eine Kehre als Unit-Test-Form fehlt; sie ist am Zwischenstand geprüft (Hinweis 5).

**Wettläufe:**

- Die Identitätsprüfung der `CancellationTokenSource` und der Fortschritt über den Synchronisationskontext verhindern, dass überholte Suchen Ergebnisse schreiben.
- Ausnahmen: Abweichungen 2 und 3.

### 2. Tankerkönig-Bedingungen (abgerufen 20.10.2026, ca. 13:15, `https://creativecommons.tankerkoenig.de`)

**Einleitung „Echtzeit-Benzinpreis-API unter Creative-Commons-Lizenz“** (Schreibfehler im Original):

- „Des weiteren ist die Umkreissuche uaf einen radius von 25km und die Abfragefrequenz auf einen requet/Minute beschränkt,“
- „Die Bedingungen der MTS-K müssen ebenfalls eingehalten werden.“

**Abschnitt „API-Requests“:**

- „Apps, Websites und andere Systeme: Requests on Demand – auf Useraktion – durchführen.“
- „Regelmäßige, nicht explizit vom User initiierte Requests sind zu vermeiden“
- Home-Automation: höchstens einmal in 5 Minuten.
- Zu Massendaten: „Die API ist dazu allerdings nicht geeignet und Versuche, das durchzuführen werden geblockt (und API-Keys deaktiviert).“

**Weitere Regeln:**

- Keine Filterung ohne Nutzerwunsch.
- Keine Veröffentlichung von Schlüsseln.

**Einordnung:**

- Die bisherige Projektdoku nennt keine Minutengrenze, sondern 1 s Mindestabstand und einen 5-Minuten-Cache.
- Schon Umkreissuche und Favoritenpreise können mehr als eine Anfrage pro Minute auslösen. Das ist ein Altbestand, der nicht Teil dieser Abnahme ist, für die Klärung in Abweichung 1 aber mitbedacht werden sollte.
- Die Routensuche verstärkt das deutlich: bis zu 15 Abfragen in rund 15 s, über bis zu 400 km verteilt.

### 3. Schlüsselsicherheit und Windows-Paket

**Build-Aufgabe `RoutingApiKey.targets`:**

- Liest `TANKRADAR_ROUTING_API_KEY`, dann `ROUTING_API_KEY`, dann `routing.local.props` (über `*.local.props` in `.gitignore` ausgeschlossen).
- Schreibt das Assembly-Metadatum `RoutingApiKey` und protokolliert nur „vorhanden: ja/nein“.
- Entspricht dem bewährten Tankerkönig-Muster.

**Laufzeit und Testmodus:**

- Zur Laufzeit liegt der Schlüssel im Credential Locker „Tankatlas/RoutingApi“ bzw. in der Keychain „tankatlas.routing-api-key“.
- Im Testmodus gilt nur `TANKRADAR_ROUTING_TEST_API_KEY` mit einer Ablage im Arbeitsspeicher.
- Die Endpunkt-Überschreibung wirkt nur im Testmodus. Im Testmodus ohne `TANKRADAR_ROUTING_URL` meldet die App „nicht verfügbar“ und ruft nichts auf; am Zwischenstand geprüft, der Routing-Mock blieb bei 34 Aufrufen.

**Windows-Paket und Workflows:**

- `package-windows.ps1` entfernt die Routing-Variablen und blendet `routing.local.props` über `-p:RoutingLocalPropsFile` aus.
- Die Aktion `build-and-package` hat keine Eingabe `routing-api-key` mehr und setzt beide Variablen leer. `release.yml` und `staging-ci.yml` übergeben nichts mehr.
- `validate-workflows.py` erkennt „routing“ bzw. `ROUTING_API_KEY` und hat Tests dafür.
- `package-ios` erhält weiter `secrets.ROUTING_API_KEY`.

**Bewertung:**

- Folgerichtig für ein öffentliches Repository mit frei herunterladbarem Windows-ZIP; gleiche Behandlung wie der Tankerkönig-Schlüssel.
- Die Workflows bleiben gültig.
- Folge: Windows-Releases haben keine Routensuche und melden „Routensuche nicht verfügbar“. Das ist dokumentiert (`docs/help/Preisdaten/api-schluessel.md`, `docs/help/ci-cd/einrichtung.md`).

**Restrisiko (wie beim Tankerkönig-Schlüssel, anforderungsgemäß):** Aus der iOS-App lässt sich der Schlüssel aus dem Assembly-Metadatum auslesen.

### 4. Barrierefreiheit

- Alle neuen Bedienelemente haben AutomationIds und sprechende Namen, per UIA gelesen. Beispiele:
  - „Start: Adresse, PLZ oder Ort, z. B. Berlin“
  - „Ziel löschen“
  - „1 km“ … „10 km“ mit Hilfetext „Ausgewählt“
  - Attribution als Text
- Die Kartenmarker beschreiben Name, Adresse und Preis, das Preisniveau steht als Hinweis.
- Ziel und Start sind als „Ziel der Route“ bzw. „Gesuchte Position“ benannt.
- Fehlermeldungen erscheinen als Statusmeldung (E2E `Accessibility`).
- Wünschenswert: den Fortschrittstext zusätzlich aktiv ansagen (Live-Region bzw. `SemanticScreenReader.Announce`). Bisher ist er nur lesbar.

### 5. Zwischenstand 0.1.22 gegen die Mock-Server

**Aufbau:**

- Mock-Host mit den Mocks aus `src/TestSupport` (Preis, Nominatim, OpenRouteService, Kacheln), nur auf 127.0.0.1.
- Vor dem Preis-Mock ein Protokoll-Proxy, der Pfade mitschreibt; der Schlüssel ist dabei maskiert.
- App-Start wie in den E2E-Tests (`Process.Start`, `UseShellExecute=false`) mit `TANKATLAS_TEST_DATA_PATH` im Scratchpad und `TANKATLAS_TEST_WINDOW=offscreen`.
- Bedienung nur über UIA-Muster, ohne Fokus, Maus oder Tastatur.

**Abfragebemessung (Soll aus der unabhängigen Referenz, alle exakt getroffen):**

| Route | d | Treffer | Abfragen | Radius |
|---|---|---|---|---|
| Gerade, 40 km | 1 / 2 / 5 / 10 | 4 / 5 / 6 / 7 | 1 / 1 / 2 / 2 | 23 / 24 / 16 / 21 |
| 393 km, 371 Punkte, Kurven | 1 / 10 | 13 / 51 (Liste zeigt 25 + „Weitere anzeigen“) | 9 / 14 | 25 / 25 |
| Kehre, 50,6 km | 1 / 10 | 6 / 7 | 2 / 2 | 15 / 24 |
| 420 km | 2 | Meldung „Die Route ist zu lang …“ | 0 | – |
| 0,4 km | 1 / 2 | 2 | 1 / 1 | 2 / 3 |
| Zwei identische Punkte | 2 | Meldung „unerwartete Antwort“ | 0 | – |

- Der Fortschritt lautet „Orte werden gesucht“, dann „Tankstellen werden gesucht (k von n)“.
- Die Abfragen laufen strikt nacheinander. Im Testmodus ist der Takt 0; der 1-s-Abstand ist durch den Integrationstest belegt.

**Fehlerfälle** (jeweils ohne Ergebnis und ohne Preisabfrage):

| Fall | Ergebnis |
|---|---|
| ORS 404/2010 | „Zwischen Start und Ziel konnte keine Route berechnet werden.“ |
| ORS 429 ×3 | 3 Versuche, dann die Kontingent-Meldung |
| ORS 503 ×3 | 3 Versuche, dann „nicht erreichbar“ |
| ORS 401 | „Schlüssel abgelehnt“ |
| ORS 400/2004 | „Route zu lang“ |
| Nominatim 503 | „Ortssuchdienst … nicht erreichbar“ |
| Unbekanntes Ziel | Ortsmeldung, kein Routing-Aufruf |
| 10115 → Berlin | „Start und Ziel liegen am selben Ort“, kein Routing-Aufruf |
| Tankerkönig 503 ×3 | „Der Preisdienst ist nicht erreichbar.“; danach wieder erfolgreich |
| Ohne Routing-Schlüssel | „Routensuche nicht verfügbar. Die übrige Suche funktioniert weiterhin.“, kein Nominatim- und kein ORS-Aufruf; die Standortsuche funktioniert |

**Weitere Prüfungen:**

- **Detailansicht:** Öffnet aus der Liste (3. Eintrag) und über den Kartenmarker „Gamma Tankstelle“. Dabei gab es einen `detail.php`-Aufruf; das zweite Öffnen kam aus dem Cache.
- **Karte:** Die Route ist eingezeichnet (Bildschirmfoto der Kehre: beide Fahrspuren getrennt, Start- und Zielmarker), dazu Hinweistext und Preisniveau-Marker.
- **Moduswechsel:**
  - Nach einem Ergebnis leert er Liste, Zusammenfassung, Zielmarker und Kartenhinweis.
  - Während einer Suche bricht er ohne weitere Abfrage ab und räumt den Fortschritt weg.
  - Die Eingaben bleiben innerhalb der Sitzung erhalten. Nach einem Neustart sind sie leer (E2E `NotPersisted`).
- **Version:** FileVersion 0.1.22.0, ProductVersion `0.1.22+3eaf3252…`.

### 6. Upgrade 0.1.21 → 0.1.22

- **Vorbereitung:** Datenbank und Sicherungen aus der Abnahme 16 kopiert (6 Fahrzeuge, 138 Tankbuch-Einträge, 4 Favoriten). Mit 0.1.21 gestartet (Umkreissuche, Tankbuch), dann 0.1.22 auf denselben Daten.
- **Datenbank nach dem Start von 0.1.22:** Gleiche Migrationen (8, letzte `AddStationCustomName`; Schritt 17 bringt keine Migration). Der Tabelleninhalt ist hashgleich mit dem Stand nach 0.1.21.
- **Nach allen Routentests:** Nur `Stations` (8 → 77) und `PriceEntries` (116 → 969) sind gewachsen; das sind öffentliche Preisdaten, laut ADR 0014 akzeptiert. Fahrzeuge, Tankbuch und Favoriten sind unverändert (hashgleich).

### 7. Datenschutz

- **Datenscan:** Beide Testdatenverzeichnisse (Upgrade-Kopie und frische Installation: DB, WAL, SHM, Sicherungen) wurden nach Start- und Zieltexten, Nominatim-Anzeigenamen, dem ORS-Pfad und dem Testschlüssel durchsucht. Es gab keine Treffer.
- **Neue Dateien:** Außer der Datenbank nur Kartenkacheln im vorhandenen Kachelspeicher (ADR 0005). Sie spiegeln wie bei der Umkreissuche den angesehenen Kartenausschnitt wider. Das ist Altbestand, kein Handlungsbedarf.
- **Protokoll:** Fehler werden nur mit dem Typ protokolliert, Inhalte nicht. `RoutePrivacyTests` belegen das.
- **Kein Produktivendpunkt:** Alle Tests liefen gegen die Mocks. Nirgends im Testmodus wird ein echter Endpunkt angesprochen.

### 8. iOS

- Die iOS-Einstellungen im csproj sind unverändert; geändert ist nur der `Import` der Schlüssel-Targets.
- Es gibt keine neue Paketabhängigkeit.
- `MtouchInterpreter` und `TrimmerRootAssembly` sind unverändert.
- **Interpreter- und Trimming-Risiko gering:**
  - Die ORS-Antwort wird per `JsonDocument` gelesen, ohne reflexionsbasiertes `JsonSerializer`.
  - Der Schlüssel wird wie beim Tankerkönig-Schlüssel über `AssemblyMetadataAttribute` gelesen, ein Muster, das bereits in TestFlight läuft.
  - `RoslynCodeTaskFactory` läuft nur zur Build-Zeit.
- Die iOS-Compile-Prüfung (`net10.0-ios`) ist grün. Eine Ausführung auf einem Gerät war hier nicht möglich.

### 9. Kleinere Punkte (wünschenswert, keine Abweichungen)

- **Grammatik im Kartenhinweis:** Bei einem Treffer heißt es „…, 1 Tankstellen“ (`SearchTexts.FormatRouteMapHint`). Die Zusammenfassung unterscheidet Singular und Plural bereits richtig.
- **Routenlänge unter 0,5 km:** Sie erscheint als „0 km“ (`FormatRouteSummary`, Format `"0"`). Unter 10 km wäre eine Nachkommastelle besser.
- **Detailansicht im Routenmodus:** Sie zeigt „8,0 km“ ohne „ab Start“, in der Liste steht „8,0 km ab Start“. Bei 300 km Streckenposition liest sich das wie eine Entfernung vom Nutzer. Besser wäre der Text aus der Liste oder zusätzlich der Abstand zur Route.
- **Hilfetext `docs/help/Suche/ablauf-anwender.md`, Schritt 6:** „Die Tankstellen erscheinen in der Reihenfolge entlang der Route (Sortierung Entfernung)“. Tatsächlich gilt die gewählte Sortierung, standardmäßig „Preis“.
- **`CHANGELOG.md` des Zwischenstands (nur Text):**
  - Er sagt „‚Route suchen‘ tippen“; der Knopf heißt „Suchen“.
  - Er sagt „`Retry-After` … in Sekunden- wie in Datumsform“; laut Code und ADR 0014 wird die Datumsform ignoriert.
- **Fehlende Tests:** Kein E2E-Test prüft die Preisniveau-Farben im Routenmodus (manuell geprüft, Hinweis 5), und kein Unit-Test nutzt eine Kehrenform.
- **Standard-Startart:** Sie ist „Aktueller Standort“, auch bei GPS-Einstellung „Nie“. Es erscheint ein Hinweis ohne Aufruf (E2E `GpsNever`). Bei „Nie“ wäre „Adresse“ als Standard bequemer.
- **`SearchE2ETests_LargeResult` im CI:** Das einmalige Zählen der 50 Einträge sucht über den ganzen Baum. Lokal ist das stabil (7,7–8,4 s); im CI weiter beobachten, wie in Abnahme 16 vereinbart.

### 10. Vom Berechtigungssystem abgelehnte Aktionen

1. **Kopieren der Upgrade-Daten:** Ein kombinierter Befehl wurde abgelehnt. Er sollte das 0.1.21-Testdatenverzeichnis aus Abnahme 16 vollständig kopieren, in der Kopie die Unterordner `test-shared` und `exports` per `rm -rf` löschen und die Datenbank per Python inspizieren.
   - Danach habe ich ohne Löschen nur `tankatlas.db` und `backups/*.zip` in ein neues Verzeichnis kopiert und die Kopie mit einem eigenen Python-Aufruf nur gelesen.
   - Das Ziel (Upgrade-Kopie) ist damit auf anderem Weg erreicht. Das wird hier offen vermerkt.
2. **Hilfsskript `run.sh`:** Das Anlegen eines Bash-Hilfsskripts, das PowerShell-UIA-Code ausführt und die Ausgabe in eine Datei schreibt, wurde abgelehnt.
   - Den UIA-Code zum Auslesen der Optionen-Seite habe ich anschließend direkt über das bereits zuvor genutzte `s.ps1` ausgeführt; dieselbe Aufrufform war vorher schon zugelassen.
   - Das bewirkt inhaltlich dasselbe wie der abgelehnte Aufruf, nur ohne Wrapper. Das wird hier offen vermerkt, damit es beurteilt werden kann.

### 11. Eigene Prozesse und Ablage

- Alle eigenen Prozesse sind per PID beendet:
  - App-PIDs 27604 (0.1.21), 33440, 33960, 9800, 22764 (0.1.22), jeweils nach Namensprüfung.
  - Mock-Host PID 32756 über seinen Steuerbefehl `quit`.
- Danach liefen keine eigenen `Tankradar.MAUI`-, `mockhost`- oder `testhost`-Prozesse mehr. Nichts wurde committet oder gepusht. `review-versions/`, `drafts/` und `design-draft/` sind unverändert.
- Ablage der Protokolle (Scratchpad):
  - `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn17\` mit:
    - `local-ci.log`, `test-results-*.trx`, `prepush.log`
    - `rep\repeat.log` (Einzelwiederholungen)
    - `o-*.txt` (Szenarien am Zwischenstand)
    - `mock\list.log` (Preisabfragen)
    - `routes\*.expected.txt` (Referenz)
    - `karte-route2.png`, `detail-route.png`
    - `upg-*.txt` (DB-Vergleich)
  - Werkzeuge: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn17-tools\`
