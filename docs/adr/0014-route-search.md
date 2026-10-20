# ADR 0014: Tankstellen entlang einer Route

## Status

Angenommen (2026-10-20)

## Kontext

Schritt 17 ergänzt im Bereich „Karte“ neben Standort- ([ADR 0002](0002-search-page-design-deviations.md)) und Adresssuche
([ADR 0003](0003-address-search-decisions.md)) die Suche **entlang einer Route**: Start (aktueller Standort oder Adresse) und Ziel werden
über Nominatim aufgelöst, die Route für ein Auto liefert OpenRouteService (Directions, Profil `driving-car`), die Tankstellen kommen aus
mehreren Tankerkönig-Umkreisabfragen. Die Darstellung nutzt die Kartenansicht ([ADR 0005](0005-map-view-decisions.md)). Der Projektplan
schloss bisher „mehrere zusammengeführte Abfragen“ aus (Zeile „Suchradius“); dieser ADR begründet, warum das für die Routensuche nicht gilt.
Vorgaben: nur HTTPS, Zeitlimit, Wiederholung mit zunehmender Wartezeit, kein Schlüssel im Quelltext, Routen/Adressen/Standort nicht
dauerhaft speichern, keine neue Abhängigkeit (iOS: `MtouchInterpreter` und `TrimmerRootAssembly` bleiben unverändert).

## Entscheidungen

### Abfragegeometrie (lückenlose Überdeckung des Abstandsbandes)

- Abstandsstufen zur Route: **1, 2, 5 und 10 km**, Standard 2 km (`RouteDistance`). Der Abstand ist nicht persistent (wie der Suchradius),
  es gibt keine Migration und keine Auswirkung auf Sicherung oder Export.
- Jede Umkreisabfrage hat einen Radius R ≤ 25 km (Grenze der Preisquelle). Die Route der Länge L wird in n = ⌈L / Schritt⌉ gleich lange
  Abschnitte der Länge s = L / n geteilt, Schritt = 2 · (25 − d) · 0,95 (d = Abstand). Der Mittelpunkt einer Abfrage liegt auf der
  Streckenmitte des Abschnitts, der Radius ist ⌈s / 2 / 0,95 + d⌉ km (ganze Kilometer, weil die Abfrage ganzzahlige Radien kennt),
  mindestens 1 und höchstens 25.
- **Beweis der Überdeckung:** Eine Tankstelle im Abstand ≤ d von einem Routenpunkt P des Abschnitts liegt höchstens s/2 + d vom Mittelpunkt
  entfernt (Dreiecksungleichung: P ist entlang der Route höchstens s/2 vom Mittelpunkt entfernt, die Luftlinie ist nie länger als der Weg).
  Wegen s ≤ Schritt ist s/2/0,95 + d ≤ 25; der Radius ist also zulässig und überdeckt die Tankstelle. Der Faktor 0,95 fängt Rundungs- und
  Projektionsfehler ab. Ein Test (`RouteSamplerTests_Coverage`) belegt das mit Zufallspunkten für alle Stufen, gerade und gekrümmte Routen
  und die Enden der Route.
- Rechenbeispiele: d = 2 km, Schritt 43,7 km; 100 km ergeben 3 Abfragen (s = 33,3 km, R = 19 km), 400 km ergeben 10 Abfragen.
  d = 10 km, Schritt 28,5 km; 400 km ergeben 15 Abfragen (s = 26,7 km, R = 24 km).
- Die Auswahl filtert anschließend exakt: Eine Tankstelle gehört zur Route, wenn ihr Abstand zur Streckenlinie ≤ d ist (lokale Projektion
  in Kilometer, Fußpunkt auf der Teilstrecke, Haversine zum Fußpunkt). Dubletten (Station-ID) entfallen; sortiert wird nach der Position
  entlang der Route, bei Gleichstand nach Kennung. Bei Schleifen gilt die kleinere Streckenposition.

### Obergrenze, Takt und Nutzungsbedingungen von Tankerkönig

- Höchstens **15 Abfragen** und **400 km** Routenlänge je Suche (`RouteSearchOptions`); bei Überschreitung bricht die Suche mit einer
  Meldung ab, es wird nicht still gekürzt. Bei d = 10 km ergeben 400 km genau 15 Abfragen.
- Die Abfragen laufen **strikt nacheinander** mit dem vorhandenen Mindestabstand von 1 s (`RequestThrottle`, nicht verkürzbar), ohne
  Parallelität, ohne Hintergrundabfragen und ohne automatische Wiederholung der Gesamtsuche. Identische Abfragepunkte sind deterministisch
  und werden vom Such-Cache (5 Minuten) bedient. Eine Suche dauert damit höchstens rund 15 s zuzüglich Routing.
- Meldet eine Abfrage einen Fehler (auch Rückfall auf den Cache mit Fehlergrund), bricht die Suche ab; Teilergebnisse würden Lücken
  entlang der Route verschweigen.
- Nutzungsbedingungen: Beim Schreiben dieses ADR (2026-10-20) war die Seite der API (`https://creativecommons.tankerkoenig.de`) nicht
  abrufbar (HTTP 502), `https://www.tankerkoenig.de` nennt nur die Lizenz CC BY 4.0 und verweist auf die API-Seite. Die Bemessung folgt
  deshalb den bereits dokumentierten Regeln des Projekts (Mindestabstand 1 s, Radius ≤ 25 km, kein Hintergrundabruf, Cache 5 Minuten;
  siehe `docs/help/Preisdaten/beschreibung.md`) und ist bewusst konservativ. **Zu prüfen bleibt**, ob die aktuellen Bedingungen eine
  strengere Grenze nennen; die Konstanten stehen zentral in `RouteSearchOptions` und `PriceApiOptions` und werden nur dort angepasst.

### Aufhebung des Ausschlusses kombinierter Abfragen

Der Ausschluss im Projektplan galt größeren Radien um einen Punkt. Die Radiussuche bleibt **eine** Abfrage (Radius ≤ 25 km). Eine Strecke
lässt sich dagegen fachlich nicht mit einer Abfrage abdecken; die Routensuche ist durch Obergrenze (15), Drosselung (1 s), Cache und die
Beschränkung auf ausdrücklich ausgelöste Suchen begrenzt.

### Routing-Dienst

- `POST {BaseUrl}v2/directions/driving-car` mit `coordinates` (Länge, Breite) und `instructions=false`; der Schlüssel steht im Kopf
  `Authorization`, nie in der Adresse. Ausgewertet werden `routes[0].geometry` (kodierte Polylinie, Genauigkeit 5) und nur diese; die Länge
  folgt aus den Punkten. Dekodierung und Geometrie sind selbst implementiert (kein Paket, keine Reflexion).
- Zeitlimit 15 s je Versuch, bis zu 3 Versuche mit Wartezeit 1 s, 2 s; Mindestabstand 1,5 s (Kontingent). 429 und 5xx sowie Zeitlimit
  werden wiederholt (Ergebnis `QuotaExceeded` bzw. `Unreachable`); 401/403 `KeyRejected`; 404 oder Code 2009/2010 `RouteNotFound`;
  Code 2004 `RouteTooLong`; sonstige 4xx `Rejected`; unlesbar `InvalidResponse`. Weiterleitungen werden nicht verfolgt.
- Orte: Wie die Adresssuche der erste Treffer (`limit=1`, ADR 0003); der aufgelöste Name steht in der Zusammenfassung, damit Fehltreffer
  erkennbar sind. Start und Ziel werden nacheinander aufgelöst (Mindestabstand 1,1 s). Liegen beide weniger als 200 m auseinander, meldet
  die Suche „selber Ort“ ohne Routing-Aufruf.
- Quellenangabe in der Oberfläche: „Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende“.

### Schlüssel `ROUTING_API_KEY`

- Wie der Tankerkönig-Schlüssel: Build-Einbindung über `MSBuild/RoutingApiKey.targets` (Quellen nacheinander `TANKRADAR_ROUTING_API_KEY`,
  `ROUTING_API_KEY`, nicht versionierte `routing.local.props` mit Element `RoutingApiKey`; Assembly-Metadatum `RoutingApiKey`; Log nur
  „vorhanden: ja/nein“), zur Laufzeit Credential Locker (`Tankatlas/RoutingApi`) bzw. Keychain (`tankatlas.routing-api-key`). Im Testmodus
  gilt nur `TANKRADAR_ROUTE_API_KEY` (getrennt von der Build-Variablen), Ablage im Speicher.
- Das **Windows-Paket enthält den Schlüssel nie** (öffentlich herunterladbar): `package-windows.ps1` und die Aktion `build-and-package`
  leeren die Routing-Variablen und blenden die lokale Datei aus; die Workflow-Prüfung erzwingt das. iOS-Builds betten ihn aus dem Secret ein.
- Fehlt der Schlüssel, meldet die Routensuche „nicht verfügbar“ (`RouteSearchStatus.RoutingUnavailable`) ohne jeden externen Aufruf;
  die übrige App bleibt nutzbar.

### Datenschutz

Start-/Zieltexte, Positionen und Route leben nur im Arbeitsspeicher des `MapViewModel`; es gibt keine Tabelle, Einstellung, Datei, Sicherung
oder Protokollzeile dazu (Statuswerte und Zählungen ausgenommen). Die Tankerkönig-Abfragen speichern wie bisher Stationen und Preise
(öffentliche Daten); durch bis zu 15 Abfragen kann der Bestand bekannter Stationen auch Stationen außerhalb des Bandes enthalten. Das ist
akzeptiert (wie bei Radius- und Adresssuche). Tests belegen, dass keine Routen- oder Adressdaten gespeichert oder protokolliert werden.

### Darstellung

Das Ergebnis nutzt Liste, Karte, Filter, Preise mit Alter, Hinweise, Preisniveau-Farben und Sortierungen. „Entfernung“ ist die Position
entlang der Route („12,3 km ab Start“), zusätzlich „0,8 km abseits der Route“. Die Karte zeichnet die vereinfachte Linie (Douglas-Peucker,
höchstens 1500 Punkte, beim Zeichnen zusätzlich nach Zoomstufe zusammengefasst und auf den Ausschnitt zugeschnitten) unter den Markern,
markiert Start (Ursprungsmarkierung) und Ziel und passt den Ausschnitt an die Begrenzungsbox an. Die Schaltfläche „Weitere anzeigen“ heißt
jetzt „Weitere anzeigen (N übrig)“, damit die Sprachausgabe „weitere“ nicht doppelt liest.

## Folgen

- Neue Dienste unter `Services/Routing/`, Presenter-Logik in `MapViewModel.Route.cs`.
- Eine Routensuche belastet Tankerkönig mit bis zu 15 Abfragen; die Konstanten sind zentral und prüfbar.
- Die Aufhebung des Ausschlusses betrifft nur die Routensuche.
