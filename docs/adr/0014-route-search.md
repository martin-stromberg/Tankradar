# ADR 0014: Tankstellen entlang einer Route

## Status

Angenommen (2026-10-20); Änderungen nach Korrekturrunde 1 (2026-10-20/25): eigener Schlüssel je Anwender, neue Adresse, vorläufige Abfragegrenze

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
- Rechenbeispiele (ohne Abfragegrenze): d = 2 km, Schritt 43,7 km; 100 km ergeben 3 Abfragen (s = 33,3 km, R = 19 km). Mit der vorläufigen Grenze von 3 Abfragen sind Routen
  bis 3 × Schritt durchsuchbar: 136,8 km (d = 1), 131,1 km (d = 2), 114 km (d = 5), 85,5 km (d = 10).
- Die Auswahl filtert anschließend exakt: Eine Tankstelle gehört zur Route, wenn ihr Abstand zur Streckenlinie ≤ d ist (lokale Projektion
  in Kilometer, Fußpunkt auf der Teilstrecke, Haversine zum Fußpunkt). Dubletten (Station-ID) entfallen; sortiert wird nach der Position
  entlang der Route, bei Gleichstand nach Kennung. Bei Schleifen gewinnt das nächstgelegene Teilstück; die kleinere Streckenposition zählt nur bei Gleichstand (1e-9).

### Obergrenze, Takt und Nutzungsbedingungen von Tankerkönig

- **Wortlaut der Nutzungsbedingungen** (abgerufen 2026-10-20 von `https://creativecommons.tankerkoenig.de`, Schreibfehler im Original): Die Umkreissuche ist „auf einen radius von 25km und die
  Abfragefrequenz auf einen requet/Minute beschränkt“. Unter „API-Requests“: „Apps, Websites und andere Systeme: Requests on Demand – auf Useraktion – durchführen“; „Regelmäßige, nicht explizit
  vom User initiierte Requests sind zu vermeiden“; zu Massendaten: „Die API ist dazu allerdings nicht geeignet und Versuche, das durchzuführen werden geblockt (und API-Keys deaktiviert).“
- **Abweichung 1 (vorläufig):** Die Routensuche läuft ausschließlich auf Nutzeraktion, braucht aber mehrere Umkreisabfragen. Bis zur Klärung mit Tankerkönig (Entscheidung des Anwenders vom
  2026-10-20) gilt: **höchstens 3 Umkreisabfragen je Routensuche** mit **mindestens 10 s Abstand** (`RouteSearchOptions.DefaultMaxQueries`, `DefaultMinQueryInterval`; nur dort anzupassen). Routen, die mehr
  Abfragen bräuchten, werden **vor der ersten Preisabfrage** mit einem Hinweis abgelehnt, der die bei gewähltem Abstand durchsuchbare Länge nennt (`RouteSearchResult.Limit`); es wird nicht still gekürzt.
  Die frühere Grenze (15 Abfragen, 400 km, 1 s) entfällt. Nach der Antwort von Tankerkönig wird die Grenze angepasst und hier mit Quelle festgehalten; bis dahin trägt der Auftraggeber das Restrisiko,
  dass die Minutengrenze strenger auszulegen ist.
- **Takt:** Die Abfragen laufen **strikt nacheinander**, ohne Parallelität, ohne Hintergrundabfragen und ohne automatische Wiederholung der Gesamtsuche. `RouteQueryPacer` erzwingt den Mindestabstand
  zwischen zwei Abfragen, die den Preisdienst tatsächlich erreichen: Jede Abfrage reserviert einen Absendezeitpunkt (monotone Uhr), frühestens ein Intervall nach dem zuvor reservierten, auch **über mehrere
  Routensuchen und gleichzeitige Aufrufer hinweg**; die Reservierung gilt auch bei Abbruch während der laufenden Anfrage, nur ein Abbruch in der Wartezeit gibt sie frei. Abfragen, die der Such-Cache (5 Minuten)
  bedient (`IFuelPriceService.IsSearchCached`), warten nicht. Die Wartezeit ist abbrechbar; die Fortschrittsanzeige nennt schon währenddessen die anstehende Abfrage („Tankstellen werden gesucht (2 von 3)“), und **Suche abbrechen**
  beendet die Suche. Alle Abfragen laufen über denselben Preisdienst und dessen allgemeine Drosselung (`RequestThrottle`, 1 s, nicht verkürzbar) wie die normale Suche; die Routensuche umgeht sie
  nicht, sie wartet nur länger. Eine Suche dauert damit höchstens rund 20 s zuzüglich Routing; folgt sie direkt auf eine frühere Suche, kommt für die erste Abfrage bis zu 10 s Wartezeit hinzu.
- Meldet eine Abfrage einen Fehler (auch Rückfall auf den Cache mit Fehlergrund), bricht die Suche ab; Teilergebnisse würden Lücken entlang der Route verschweigen.
- Altbestand (nicht Teil dieser Entscheidung): Auch Umkreissuche und Favoritenpreise können mehr als eine Anfrage pro Minute auslösen; das ist bei der Klärung mit Tankerkönig mitzubedenken.

### Aufhebung des Ausschlusses kombinierter Abfragen

Der Ausschluss im Projektplan galt größeren Radien um einen Punkt. Die Radiussuche bleibt **eine** Abfrage (Radius ≤ 25 km). Eine Strecke
lässt sich dagegen fachlich nicht mit einer Abfrage abdecken; die Routensuche ist durch Obergrenze (3), Mindestabstand (10 s), Cache und die
Beschränkung auf ausdrücklich ausgelöste Suchen begrenzt.

### Routing-Dienst

- Basisadresse: `https://api.heigit.org/openrouteservice/` (Quelle: Ankündigung des Anbieters, https://ask.openrouteservice.org/t/deprecating-api-openrouteservice-org-in-favour-of-api-heigit-org/7912;
  die frühere Adresse `api.openrouteservice.org` wird Anfang November 2026 abgeschaltet). Der Mock-Server bildet den Pfadanteil `/openrouteservice/` nach.
- `POST {BaseUrl}v2/directions/driving-car` mit `coordinates` (Länge, Breite) und `instructions=false`; der Schlüssel steht im Kopf
  `Authorization`, nie in der Adresse. Ausgewertet werden `routes[0].geometry` (kodierte Polylinie, Genauigkeit 5) und nur diese; die Länge
  folgt aus den Punkten. Dekodierung und Geometrie sind selbst implementiert (kein Paket, keine Reflexion).
- Zeitlimit 15 s je Versuch, bis zu 3 Versuche mit Wartezeit 1 s, 2 s; Mindestabstand 1,5 s (Kontingent). 429 (ein Retry-After in Sekunden verlängert die Wartezeit, gedeckelt auf 10 s; die HTTP-Datumsform wird ignoriert, es gilt die Standardwartezeit) und 5xx sowie Zeitlimit
  werden wiederholt (Ergebnis `QuotaExceeded` bzw. `Unreachable`); 401/403 `KeyRejected`; 404 oder Code 2009/2010 `RouteNotFound`;
  Code 2004 `RouteTooLong`; sonstige 4xx `Rejected`; unlesbar `InvalidResponse`. Weiterleitungen werden nicht verfolgt.
- Orte: Wie die Adresssuche der erste Treffer (`limit=1`, ADR 0003); der aufgelöste Name steht in der Zusammenfassung, damit Fehltreffer
  erkennbar sind. Start und Ziel werden nacheinander aufgelöst (Mindestabstand 1,1 s). Liegen beide weniger als 200 m auseinander, meldet
  die Suche „selber Ort“ ohne Routing-Aufruf.
- Quellenangabe in der Oberfläche: „Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende“.

### Schlüssel des Anwenders (statt Build-Einbindung)

- Die App ist öffentlich verfügbar: Ein eingebauter Schlüssel wäre auslesbar, und alle Nutzer teilten sich Kontingent und Missbrauchsrisiko des Projektkontos (Entscheidung des Anwenders vom 2026-10-20).
  Daher gibt es **keine Build-Einbindung** mehr: `RoutingApiKey.targets`, `routing.local.props`, die Variablen `TANKRADAR_ROUTING_API_KEY`/`ROUTING_API_KEY`, das Secret und die Sonderbehandlung in
  `package-windows.ps1`, `build-and-package` und `validate-workflows.py` sind entfernt. Der Tankerkönig-Schlüssel ist unverändert.
- Jeder Anwender beantragt einen eigenen Schlüssel (https://openrouteservice.org/dev/#/signup) und trägt ihn in den **Optionen** → **Routensuche** ein: verdecktes Feld, **Schlüssel speichern**,
  **Schlüssel prüfen** (eine einzelne Testanfrage `POST v2/directions/driving-car` mit festen Koordinaten, ohne Wiederholung; Rückmeldung gültig / abgelehnt / nicht erreichbar / Kontingent), **Schlüssel entfernen**
  und der Schalter **Routensuche aktivieren**, der erst mit gespeichertem Schlüssel bedienbar ist.
- Ablage: Credential Locker (`Tankatlas/RoutingApi`, Schalter `Tankatlas/RoutingEnabled`) bzw. Keychain (`tankatlas.routing-api-key`, `tankatlas.routing-enabled`) über die vorhandene Abstraktion
  `IApiKeyStore` (neu: `RemoveAsync`). „Gerätelokal“ ist ungenau: `SecureStorage` legt Keychain-Einträge mit „AfterFirstUnlock“ an (nicht „ThisDeviceOnly“); sie sind nicht mit iCloud synchronisiert, können aber in verschlüsselten Gerätesicherungen enthalten sein (siehe ADR 0015). Der Schlüssel steht nur im Kopf `Authorization`, wird nie angezeigt, protokolliert oder in Meldungen/Adressen übernommen; das Eingabefeld wird nach dem Speichern und
  beim Verlassen der Seite geleert.
- **Aktivierung:** Die Routensuche ist aktiv, wenn der Schalter an **und** ein Schlüssel gespeichert ist (`IRouteSearchAccess.IsEnabledAsync`). Nur dann ist `SearchMode.Route` in der Kartensuche sichtbar,
  und nur dann liefert der Zugang dem Routingdienst einen Schlüssel – ohne Aktivierung erfolgt kein Aufruf an OpenRouteService. Das Entfernen des Schlüssels schaltet die Routensuche aus. Ein Wechsel
  wirkt ohne Neustart (Ereignis `Changed` und Neulesen beim Erscheinen der Seite); war „Route“ gewählt, geht die Karte zurück auf den Standardmodus, eine laufende Suche wird abgebrochen.
- **Sicherung (Schritt 15):** Schlüssel und Schalter liegen nicht in der Datenbank und sind kein Teil der Sicherung (Integrationstest `BackupServiceTests_RouteKey`). Eine Wiederherstellung berührt sie nicht; auf einem
  Gerät ohne Schlüssel ist die Routensuche danach aus. Es gibt keine Migration.
- Testmodus: `TANKRADAR_ROUTING_TEST_API_KEY` belegt die In-Memory-Ablage mit Schlüssel und eingeschaltetem Schalter vor (ohne die Variable startet die App ohne Schlüssel); das Schlüsselfeld ist im Testmodus
  unverdeckt, weil ein Passwortfeld per UI-Automation nicht beschreibbar ist. Im Testmodus ohne `TANKRADAR_ROUTING_URL` meldet die App „nicht verfügbar“ und ruft nichts auf.
- Kein Aufruf ohne Schlüssel: Fehlt der Schlüssel, meldet die Routensuche „nicht verfügbar“ (`RouteSearchStatus.RoutingUnavailable`); die übrige App bleibt nutzbar.

### Datenschutz

Start-/Zieltexte, Positionen und Route leben nur im Arbeitsspeicher des `MapViewModel`; es gibt keine Tabelle, Einstellung, Datei, Sicherung
oder Protokollzeile dazu (Statuswerte und Zählungen ausgenommen). Die Tankerkönig-Abfragen speichern wie bisher Stationen und Preise
(öffentliche Daten); durch bis zu 3 Abfragen kann der Bestand bekannter Stationen auch Stationen außerhalb des Bandes enthalten. Das ist
akzeptiert (wie bei Radius- und Adresssuche). Tests belegen, dass keine Routen- oder Adressdaten gespeichert oder protokolliert werden.

### Darstellung

Das Ergebnis nutzt Liste, Karte, Filter, Preise mit Alter, Hinweise, Preisniveau-Farben und Sortierungen. „Entfernung“ ist die Position
entlang der Route („12,3 km ab Start“), zusätzlich „0,8 km abseits der Route“ (auch in der Detailansicht). Zusammenfassung und Kartenhinweis beschreiben die Anfrage (Abstand, Startart und Orte zum Zeitpunkt des Suchstarts); Chips und Eingaben sind während der Suche gesperrt. Routenlängen unter 10 km werden mit einer Nachkommastelle angegeben. Die Karte zeichnet die vereinfachte Linie (Douglas-Peucker,
höchstens 1500 Punkte, beim Zeichnen zusätzlich nach Zoomstufe zusammengefasst und auf den Ausschnitt zugeschnitten) unter den Markern,
markiert Start (Ursprungsmarkierung) und Ziel und passt den Ausschnitt an die Begrenzungsbox an. Die Schaltfläche „Weitere anzeigen“ heißt
jetzt „Weitere anzeigen (N übrig)“, damit die Sprachausgabe „weitere“ nicht doppelt liest.

## Folgen

- Neue Dienste unter `Services/Routing/`, Presenter-Logik in `MapViewModel.Route.cs`.
- Eine Routensuche belastet Tankerkönig mit höchstens 3 Abfragen im Abstand von mindestens 10 s; die Konstanten sind zentral und prüfbar. Die Grenze ist vorläufig bis zur Klärung mit Tankerkönig.
- Der Anwender braucht einen eigenen OpenRouteService-Schlüssel; ohne ihn bleibt die Suchart „Route“ unsichtbar.
- Die Aufhebung des Ausschlusses betrifft nur die Routensuche.
