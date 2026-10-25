# Abnahmeprüfung – Entwicklungsschritt 17 (Tankstellen entlang einer Route), Runde 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Geprüft wurde der Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-17-route` (HEAD `e642fcd`) gegen die Merge-Basis mit dem Projekt-Basisbranch (`b26729e`). Der Schwerpunkt lag auf den Korrekturcommits `d85471c`, `c0d2486` und `e642fcd` seit der ersten Abnahme (`acceptance-schritt-17.1.md`). Schritt 17a (App-Icon) und die spätere Doku auf dem Basisbranch gehören nicht zu dieser Prüfung.

Der Zwischenstand `review-versions/0.1.24_2026-10-25` hat FileVersion `0.1.24.0` und ProductVersion `0.1.24+e642fcd9…`. Er ist also aus HEAD gebaut, und Ordnername und `CHANGELOG.md` passen dazu.

**Kurzfassung:**

- Alle drei Abweichungen aus 17.1 sind behoben.
- Die verpflichtende Änderung vom 2026-10-20 ist vollständig umgesetzt:
  - Schlüssel des Anwenders statt Build-Einbindung
  - Aktivierung nur mit Schlüssel
  - Modus „Route“ nur bei aktivierter Routensuche
  - neue Adresse `https://api.heigit.org/openrouteservice/`
- Alle Prüfläufe sind grün.
- Am Zwischenstand verhielt sich die Abfragegrenze in allen geprüften Lagen korrekt:
  - schnell aufeinanderfolgende Suchen
  - Abbruch in der Wartezeit
  - Abbruch, während eine Anfrage unterwegs ist
  - Verlassen der Seite
  - Moduswechsel
  - minimiertes Fenster
  - Doppeltipp
  - Cache-Treffer
- Ich habe keinen Weg gefunden, über den der Schlüssel austritt.
- Ein Produktiv-Build gerät nicht durch die neuen Variablen in den Testmodus.

Es bleiben nur wünschenswerte Härtungen (siehe Hinweise). Die offene Klärung mit Tankerkönig ist ein Hinweis, weil die vorläufige Grenze umgesetzt ist.

**Prüfläufe (selbst ausgeführt, 25.10.2026):**

- `FUEL_PRICE_API_KEY`, `ROUTING_API_KEY`, `TANKRADAR_ROUTING_API_KEY`, `TANKRADAR_ROUTING_TEST_API_KEY` und `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` wurden per `env -u` entfernt.
- Kein Schlüsselwert wurde gelesen.
- E2E lief immer off-screen.
- Alle Workloads sind installiert; `Include*Target` musste nicht angepasst werden.

| Prüfung | Ergebnis |
|---|---|
| `scripts/local-ci.ps1` vollständig (03:00–03:16) | Exit 0, alle Schritte OK; das Windows-Paket wurde wie vorgesehen übersprungen.<br>Unit **1889/1889**, Integration **291/291**, **E2E 147/147** (13 min 52 s).<br>Zeilenabdeckung 94 %; Build mit Warnungen als Fehler: 0 Warnungen.<br>Keine anfälligen Pakete; iOS-Compile OK (nur `MAUI1001`).<br>Keine E2E-Diagnosedaten. |
| Testzahlen gegenüber 17.1 nachvollzogen | Integration: −9 (`ApiKeyBuildTests_Routing` entfernt), +2 (`BackupServiceTests_RouteKey`), +3 (`RouteSearchMockServerTests_Limit`).<br>E2E: −1 (`RouteE2ETests_KeyMissing`), +4 (`RouteE2ETests_Settings`). |
| `DiagnosticsCaptureE2ETests` | Beide Tests im Gesamtlauf grün. „Screenshot einfarbig“ trat nicht auf, deshalb war keine Einzelwiederholung nötig. |
| `scripts/test-ios-deployment.ps1` (einzeln) | Exit 0, 45 Prüfungen OK. Zusätzlich lief es zweimal in der lokalen CI. |
| `scripts/validate-workflows.py` | OK (7 Workflows; `actionlint` nicht installiert). `test_validate_workflows.py`: 14/14. |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push (Refs auf stdin, Basis `b26729e`, 03:17–03:19) | Exit 0. Geprüft wurden:<br>• Commits 17/17<br>• verbotene Muster (1091 Dateien)<br>• Format<br>• XML-Doku (829)<br>• Übersetzung<br>• Enums<br>• Platzhalter (825)<br>• Build 0 Warnungen<br>• Unit 1889/1889, Integration 291/291<br>E2E ist im Hook standardmäßig aus. |
| Conventional Commits | 17/17 im Bereich `b26729e..e642fcd` (`conventional-commits-check.py --base b26729e --end HEAD`). |
| Einzelwiederholungen (eigener `dotnet test`-Aufruf je Klasse und Lauf, 03:19–03:23) | Je **3/3** grün, ohne Diagnosedaten:<br>• `RouteE2ETests_Settings` (4 Tests, 17 s)<br>• `RouteE2ETests_Errors` (8 Tests, 36–37 s)<br>• `RouteE2ETests_Basic` (2 Tests, 10 s) |
| Zwischenstand 0.1.24 gegen die Mock-Server (03:18–03:31) | Alles in Ordnung (Hinweis 3). |
| Upgrade einer 0.1.22-Datenbank | In Ordnung (Hinweis 4). |
| Quellen | Am 25.10. erneut abgerufen:<br>• Tankerkönig-Bedingungen (unverändert, Wortlaut wie in ADR 0014)<br>• ORS-Ankündigung: neue Basis `api.heigit.org/openrouteservice/v2/`, Abschaltung der alten Adresse 02.–06.11.2026 |

**Abweichungen aus 17.1 und verpflichtende Änderung:**

| Punkt | Umsetzung | Nachweis |
|---|---|---|
| 17.1 Abw. 1: Abfragefrequenz | Umgesetzt laut Entscheidung „Abfragegrenze Routensuche“:<br>• `RouteSearchOptions.DefaultMaxQueries = 3`, `DefaultMinQueryInterval = 10 s`, nur dort einstellbar<br>• `RouteQueryPacer` (Singleton) reserviert Absendezeitpunkte auf einer monotonen Uhr, auch über Suchen und gleichzeitige Aufrufer hinweg<br>• Abbruch in der Wartezeit gibt die Reservierung frei; ein Abbruch während der Anfrage behält sie<br>• Cache-Treffer warten nicht (`IsSearchCached`)<br>• Alle Abfragen laufen weiter über `SearchNearbyAsync` und damit über `RequestThrottle`<br>• Zu lange Routen werden vor der ersten Preisabfrage abgelehnt, der Hinweis nennt die durchsuchbare Länge<br>• ADR 0014 und die Hilfe zitieren die Bedingungen | Unit `RouteQueryPacerTests` (11), Integration `RouteSearchMockServerTests_Limit` (3), Zwischenstand (Hinweis 3) |
| 17.1 Abw. 2: Zusammenfassung zeigt UI-Zustand | `RouteSearchSnapshot` (Abstand, Startart, Start- und Zieltext) bei Suchstart; Zusammenfassung, Kartenhinweis und Request nutzen ihn.<br>Chips, Eingabefelder und Löschknöpfe sind während der Suche gesperrt (`RouteInputsEnabled`). | Unit `MapViewModelTests_RouteRequest`; Zwischenstand: Chip „5 km“, Startart und Ziel während der Suche `IsEnabled=False`, danach `True` |
| 17.1 Abw. 3: Fortschrittstext nach Verlassen | `OnDisappearing` und `CancelSearch` leeren `RouteProgressText` und geben „Suchen“ frei. | Unit; Zwischenstand Szenario H |
| Verpflichtend: keine Build-Einbindung | Entfernt wurden:<br>• `RoutingApiKey.targets` und der `Import` im csproj<br>• `routing.local.props`, `ReadBuildTimeKey(metadataName)`<br>• die Secret-Übergabe in `release.yml` und `staging-ci.yml`<br>• die Eingabe `routing-api-key` und die Variable in `package-ios`<br>• die leeren Variablen in `build-and-package`<br>• die Sonderbehandlung in `package-windows.ps1`<br>• die Routing-Regeln in `validate-workflows.py`<br>Der Tankerkönig-Pfad ist unverändert. | `git grep` findet außerhalb der Doku keine Routing-Schlüssel-Reste. Die `Tankradar.MAUI.dll` des Zwischenstands enthält kein Metadatum `RoutingApiKey`. Alle drei Aufrufer von `package-ios` (`pr-staging-ci`, `staging-ci`, `release`) übergeben keine entfernte Eingabe. |
| Verpflichtend: Eintrag in den Optionen | Karte „Routensuche“ mit:<br>• verdecktem Feld (`IsPassword`, im Testmodus unverdeckt)<br>• Hinweis und HTTPS-Link zur Beantragung<br>• Speichern, Prüfen (eine Testanfrage mit festen Koordinaten, ohne Wiederholung, abbrechbar), Entfernen<br>• Schalter, der nur mit Schlüssel bedienbar ist<br>Ablage im Credential Locker `Tankatlas/RoutingApi` und `…/RoutingEnabled` bzw. in der Keychain; `RemoveAsync` ist neu. | E2E `RouteE2ETests_Settings`; Unit `RouteSearchSettingsViewModelTests`, `RouteSearchAccessTests`; Zwischenstand Szenario 1 |
| Verpflichtend: „Route“ nur bei Aktivierung, Aufrufe nur dann | `IRouteSearchAccess` liefert den Schlüssel nur bei eingeschaltetem Schalter. `ModeOptions` blendet „Route“ aus. Der Wechsel wirkt ohne Neustart (Ereignis `Changed` und Neulesen in `OnAppearing`). Aus dem Modus „Route“ geht es zurück auf „Aktueller Standort“. | Zwischenstand: Szenarien 1, L und M; der Mock zählte ohne Aktivierung keine Routenaufrufe |
| Verpflichtend: Entfernen schaltet aus | `RemoveKeyAsync` entfernt zuerst den Schalter, dann den Schlüssel. | E2E, Unit, Zwischenstand Szenario M |
| Verpflichtend: neue Basisadresse | `RoutingOptions.DefaultBaseUrl = https://api.heigit.org/openrouteservice/`, Directions unter `v2/directions/driving-car`; der Mock bildet `/openrouteservice/` nach. | Mock: Pfad `/openrouteservice/v2/directions/driving-car` |
| Kleinere Hinweise aus 17.1 | Umgesetzt:<br>• „1 Tankstelle“<br>• eine Nachkommastelle unter 10 km<br>• Detailansicht mit „ab Start, … abseits der Route“<br>• Hilfetext zur Sortierung | Unit; Zwischenstand Szenario K („1 Tankstelle“) |

## Abweichungen

Keine.

## Hinweise

### 1. Sicherheit des Schlüssels

Ich habe keinen Austrittsweg gefunden.

**Anfrage:**

- Der Schlüssel steht nur im Kopf `Authorization`.
- Am Mock geprüft: Pfad ohne Schlüssel, Rumpf nur mit Koordinaten, `LastAuthorization` gleich dem Testschlüssel.
- Weiterleitungen sind abgeschaltet (`AllowAutoRedirect=false`).
- Erlaubt sind nur Schlüssel aus ASCII-Zeichen `!`–`~` mit höchstens 512 Zeichen; damit ist keine Header-Injektion möglich.

**Protokoll und Meldungen:**

- Protokolliert werden nur Ausnahmetypen und Statuscodes, nie Meldungen des Netzwerkstacks und nie der Schlüssel.
- Alle Rückmeldungen sind feste Texte.

**Oberfläche:**

- Das Feld zeigt nie den gespeicherten Schlüssel. Es enthält nur die eigene Eingabe und wird nach dem Speichern und beim Verlassen der Seite geleert (am Zwischenstand: Wert `""`).
- Der UIA-Name ist der Platzhalter „OpenRouteService-Schlüssel“. Produktiv ist das Feld eine `PasswordBox` ohne lesbaren Wert.
- E2E-Diagnosedateien enthalten aus dem UIA-Baum nur `Name`, nicht `Value`.

**Sicherung und Export:**

- Die Sicherung am Zwischenstand bestand aus `database/tankatlas.db` und `manifest.json`. Darin gab es weder den Testschlüssel noch einen Routing-Begriff.
- Auch Datenbank, WAL und SHM des Testverzeichnisses waren frei von Schlüssel und Fehleingabe.
- Der Export (Tankbuch) ist nicht betroffen.

**Testmodus-Weiche (Feld unverdeckt):**

- Sie hängt an derselben Bedingung wie alle übrigen Testschalter (`TANKATLAS_TEST_DATA_PATH` gesetzt).
- Im Testmodus nutzt `RouteSearchAccess.Create` ausschließlich In-Memory-Ablagen, Credential Locker und Keychain werden nie geöffnet. Ein echter, gespeicherter Schlüssel kann dort also nie unverdeckt erscheinen. Sichtbar ist höchstens, was im Testmodus selbst eingetippt wird.
- Die Weiche ist damit sauber begrenzt.

**Produktiv-Build und Testvariablen:**

- `TANKRADAR_ROUTING_TEST_API_KEY` und `TANKRADAR_ROUTE_QUERY_INTERVAL_MS` wirken nur im Testmodus (Unit-Tests `Create_OutsideTestMode_UsesPlatformStores` und `FromEnvironment_OutsideTestMode_UsesDefaultsAndIgnoresTestVariables`).
- Wer `TANKATLAS_TEST_DATA_PATH` bewusst setzt, ist im Testmodus (Altbestand, auf iOS nicht möglich). Dann gilt:
  - Ohne `TANKRADAR_ROUTING_URL` erfolgt kein Routing-Aufruf (Szenario 6).
  - Der eingebaute Tankerkönig-Schlüssel wird nie verwendet. Ein Takt-Override kann also nicht den gemeinsamen Schlüssel belasten.

**Restpunkt (wünschenswert):**

- Die Hilfe nennt die Ablage „gerätelokal“. MAUI-`SecureStorage` legt Keychain-Einträge auf iOS mit „AfterFirstUnlock“ an, nicht mit „ThisDeviceOnly“.
- Sie wandern daher mit verschlüsselten Gerätesicherungen bzw. beim Gerätewechsel mit (keine iCloud-Synchronisation).
- Entweder die Formulierung präzisieren oder die Zugriffsklasse bewusst so lassen. Für den Tankerkönig-Schlüssel gilt dasselbe.

### 2. Abfragegrenze und Workflows

**Robustheit (Code-Review und Zwischenstand):**

- Die Grenze von 3 Abfragen erzwingt `RouteSampler` vor der ersten Preisabfrage.
- Der Takt ist global (Singleton, monotone Uhr), auch bei minimiertem Fenster. Auf iOS ist eine im Ruhezustand angehaltene Uhr nur konservativer.
- Ein Doppeltipp startet keine zweite Suche, weil „Suchen“ gesperrt ist.

**Härtung 1 – Takt-Jitter:**

- Die Reservierungen liegen exakt 10 s auseinander. Die tatsächlichen Absendezeiten schwanken aber um die Timer-Auflösung.
- Gemessen am Protokoll-Proxy: 9,987–10,013 s; am Mock 9,981–10,012 s.
- Theoretisch kann außerdem `RequestThrottle` die erste Abfrage nach ihrem Zeitpunkt verzögern, wenn kurz zuvor eine andere Preisanfrage lief (z. B. Favoriten). Dann schrumpft der folgende Abstand um bis zu 1 s.
- Für eine strikte Einhaltung von „mindestens 10 s“ wäre eine kleine Reserve sinnvoll (z. B. +100 ms), oder die Reservierung ab dem tatsächlichen Absenden.
- Fachlich ist das unerheblich (≤ 6 Abfragen je Minute). Es ist daher keine Abweichung.

**Härtung 2 – Rundung in der Grenzmeldung:**

- `FormatRouteLimit` rundet die durchsuchbare Länge kaufmännisch. Bei 10 km meldet die App „bis 86 km“ (85,5 km), die Hilfe nennt „85 km“.
- Eine Route mit 136,9 km wird bei 1 km abgelehnt mit „Die Route ist 137 km lang … nur bis 137 km“.
- Besser ist es, die Grenze abzurunden (`Math.Floor`).
- Bei 1 km ist „oder einen kleineren Abstand“ nicht möglich; den Satzteil dann weglassen.

**Wiederholungen des Preisdienstes:**

- Scheitert eine Abfrage mit 5xx bzw. 429, wiederholt der Preisdienst wie bisher bis zu dreimal innerhalb von etwa 3 s, danach bricht die Routensuche ab.
- Das ist die bestehende Wiederholungsstrategie aller Suchen und keine zusätzliche Abfrage im Sinne der Grenze. Es wird nur der Vollständigkeit halber erwähnt.

**Schlüsselprüfung vor der Aktivierung:**

- „Schlüssel prüfen“ ruft OpenRouteService auch bei ausgeschalteter Routensuche auf. Das verlangt der Plan („Prüfung per kleiner Testanfrage“).
- Der Aufruf geschieht nur auf Tipp, mit festen, nicht personenbezogenen Koordinaten, und ist in `api-schluessel.md` dokumentiert.
- Kein Handlungsbedarf.

**Workflows:**

- `validate-workflows.py`, die zugehörigen Tests, `test-ios-deployment.ps1` und der iOS-Compile sind grün.
- `package-ios` hat keine verwaiste Eingabe mehr.
- Ein vorhandenes Secret `ROUTING_API_KEY` kann im Repository gelöscht werden; die Doku sagt das.
- Optionale Härtung: `validate-workflows.py` könnte eine Wiedereinführung von `ROUTING_API_KEY` ausdrücklich ablehnen.

**Entwicklerrechner:**

- Wer 0.1.22 lokal ohne Testmodus mit `routing.local.props` gebaut und gestartet hat, hat das Projekt-Konto im Credential Locker `Tankatlas/RoutingApi`. Die neue Version zeigt dann „Schlüssel: gespeichert“; der Schalter bleibt aus.
- Ausgeliefert wurde der Build-Schlüssel nie (`origin/staging` enthält keine `RoutingApiKey.targets`).
- Gegebenenfalls einmal „Schlüssel entfernen“.

**Bewusst offen (bestätigt, nicht weiter verfolgt):**

- parallele Schalteraktionen bzw. nicht atomares Lesen von Schlüssel und Schalter (endet „aus“)
- konservative Wartezeit offline

### 3. Zwischenstand 0.1.24 gegen die Mock-Server

**Aufbau:**

- Mock-Host mit den Mocks aus `src/TestSupport` (neu gebaut, ORS-Pfad `/openrouteservice/`), nur auf 127.0.0.1.
- Vor dem Preis-Mock ein Protokoll-Proxy, der Pfade mitschreibt; der Schlüssel ist dabei maskiert.
- App-Start wie in den E2E-Tests: `Process.Start` mit `UseShellExecute=false`, `TANKATLAS_TEST_DATA_PATH` im Scratchpad, `TANKATLAS_TEST_WINDOW=offscreen`.
- Bedienung nur über UIA-Muster.
- Für die Taktprüfung lief `TANKRADAR_ROUTE_QUERY_INTERVAL_MS=10000`, also der Produktivwert.

| Szenario | Ergebnis |
|---|---|
| 1. Frische Installation ohne Schlüssel | Modi nur „Aktueller Standort“ und „Adresse“. Status „Schlüssel: nicht gespeichert“, Schalter gesperrt und aus, Hinweis sichtbar, „Entfernen“ unsichtbar. 0 Aufrufe. |
| 1. Prüfen falsch, Prüfen richtig, Speichern | Falsch: „Der Schlüssel wurde abgelehnt …“, Schalter bleibt gesperrt.<br>Richtig: „Der Schlüssel ist gültig. Er ist noch nicht gespeichert.“<br>Speichern: „gespeichert“, Feld leer, Schalter bedienbar und aus, Modus „Route“ weiter unsichtbar.<br>Schalter an: „Route“ erscheint ohne Neustart.<br>Prüfen ohne Eingabe prüft den gespeicherten Schlüssel.<br>Je Prüfung genau 1 Aufruf; Abstand der ORS-Aufrufe ≈ 1,5 s (Drosselung des Routingdienstes). |
| A. 120 km, 2 km | 3 Abfragen mit 10,0 s Abstand, 13 Treffer, Dauer 22,7 s.<br>Der Fortschritt zeigt während der Wartezeit „(2 von 3)“ bzw. „(3 von 3)“.<br>„Suche abbrechen“ ist sichtbar; Chips, Startart und Ziel sind gesperrt.<br>Zusammenfassung: „…, 120 km, 13 Tankstellen im Abstand von 2 km zur Route“. |
| B. Direkt danach 1 km (kein Cache) | Die erste Abfrage wartet („(1 von 3)“ ab 1,7 s) bis 10,0 s nach der letzten Abfrage von A; danach 10,0 s und 10,0 s. |
| C. Wieder 2 km (Cache) | 0 Abfragen, fertig nach 2,0 s, gleiches Ergebnis. |
| D. Zu lang | Jeweils 0 Preisabfragen:<br>• 5 km: „Die Route ist 120 km lang. Bei 5 km Abstand zur Route kann nur bis 114 km Länge gesucht werden, weil die Preisabfragen begrenzt sind. …“<br>• 10 km: „… bis 86 km …“ (Hinweis 2)<br>• 600 km bei 2 km: „… bis 131 km …“ |
| F. „Suche abbrechen“ in der Wartezeit | Meldung „Die Routensuche wurde abgebrochen.“, Fortschritt leer, „Suchen“ frei, keine weitere Abfrage. |
| G. Danach sofort eine neue Suche | Die erste Abfrage kam 17 s nach der zuletzt gesendeten (≥ 10 s); danach 10,0 s und 10,0 s. |
| H. Seite verlassen in der Wartezeit | Zurück auf „Karte“: Fortschritt leer, „Suchen“ frei, keine weitere Abfrage.<br>Abweichung 3 aus 17.1 ist behoben. |
| I. Moduswechsel in der Wartezeit | Abbruch, keine weitere Abfrage. |
| J. Fenster 0,6 s nach „Suchen“ für 25 s minimiert | Die Suche lief im Hintergrund weiter: 3 Abfragen mit 10,0 s Abstand, Ergebnis vollständig. |
| N. Abbruch, während Abfrage 1 unterwegs war (Antwort 4 s verzögert), dann sofort neue Suche | Die erste Abfrage der neuen Suche kam genau 10,0 s nach der abgebrochenen; die Reservierung bleibt also bestehen. |
| K. Doppeltipp auf „Suchen“ | Der zweite Tipp trifft eine gesperrte Schaltfläche; nur eine Suche. Zusammenfassung „… 1 Tankstelle …“. |
| L. Schalter aus, während „Route“ gewählt ist | Karte zurück auf „Aktueller Standort“ (ausgewählt), Radius-Chips sichtbar, Routeneingaben und Chip „Route“ weg. |
| M. Schlüssel entfernen | „Der Schlüssel wurde entfernt. Die Routensuche ist ausgeschaltet.“ Schalter gesperrt und aus, „Route“ verschwindet. Kein ORS-Aufruf nach L und M. |
| 4. Sicherung und Wiederherstellung | Schlüssel und Schalter an → Sicherung → Schlüssel entfernen → Wiederherstellung: bleibt „nicht gespeichert“ und aus, „Route“ unsichtbar.<br>Schlüssel und Schalter an → erneute Wiederherstellung: bleibt „gespeichert“ und an.<br>Die Wiederherstellung berührt den Zustand also in keiner Richtung. |
| 6. Testmodus mit Testschlüssel, ohne `TANKRADAR_ROUTING_URL` | Suche: „Routensuche nicht verfügbar. Die übrige Suche funktioniert weiterhin.“<br>Prüfung: „Die Prüfung ist gerade nicht möglich …“<br>Kein Routing- und kein Nominatim-Aufruf. |
| Zählung insgesamt | 18 ORS-Aufrufe = 3 Prüfungen + 15 Routensuchen; keiner ohne Aktivierung.<br>25 `list.php`-Aufrufe der Routensuche; der kleinste Abstand lag bei 9,987 s (Jitter, Hinweis 2). |

**User-Agent:** `Tankatlas/0.1.24.1 (+…)`. Das ist dasselbe Schema wie bei 0.1.22, dort `0.1.22.1`.

### 4. Upgrade 0.1.22 → 0.1.24

- **Vorbereitung:** Datenbank (mit WAL und SHM) und Sicherungen aus der ersten Abnahme kopiert: 6 Fahrzeuge, 138 Tankbuch-Einträge, 4 Favoriten.
- **Mit 0.1.22 (Zwischenstand):** Umkreissuche und Favoriten funktionieren.
- **Danach mit 0.1.24 auf denselben Daten:**
  - Modi ohne „Route“; Routensuche „nicht gespeichert“ und aus.
  - Umkreis- und Adresssuche liefern Treffer.
  - Favoritengruppe „Heimat“, Fahrzeuge und die 5 alten Sicherungen werden angezeigt.
- **Datenbank:**
  - Weiter 8 Migrationen (letzte `AddStationCustomName`); Schritt 17 bringt keine Migration.
  - Fahrzeuge, Tankbuch, Favoriten, Einstellungen und Stationen sind hashgleich.
  - Nur `PriceEntries` ist durch die Suchen gewachsen (969 → 974 → 982).

### 5. Tests und Dokumentation

- Neue E2E-Tests decken die Optionen ab (`RouteE2ETests_Settings`). Wartezeit, „Suche abbrechen“ und gesperrte Eingaben sind über Unit- und Integrationstests abgedeckt und hier manuell bestätigt. Ein E2E-Test für „Suche abbrechen“ wäre wünschenswert.
- ADR 0014, Hilfe (`Suche/beschreibung.md`, `ablauf-anwender.md`, `Preisdaten/beschreibung.md`, `api-schluessel.md`), README und `ci-cd/einrichtung.md` sind konsistent nachgezogen. Ausnahme ist die Rundung „85“ bzw. „86 km“ (Hinweis 2).
- Uneinheitlich, aber vertretbar: „Suche abbrechen“ verwirft auch das vorher angezeigte Ergebnis. Beim Verlassen der Seite bleibt dagegen das letzte vollständige Ergebnis stehen; es beschreibt dank Snapshot weiterhin korrekt seine eigene Anfrage.

### 6. Abgelehnte Aktionen, Prozesse und Ablage

**Abgelehnte Aktionen:** Keine.

**Prozesse:**

- Alle eigenen Prozesse sind beendet:
  - App-PIDs 5464, 37200, 24528 (0.1.22), 10916 und 44412, jeweils per PID nach Namensprüfung
  - Mock-Host PID 29752 über den Steuerbefehl `quit`
- Danach liefen keine eigenen `Tankradar.MAUI`-, `mockhost`- oder `testhost`-Prozesse mehr.

**Arbeitsbaum:**

- Nichts committet oder gepusht, kein Code geändert.
- `review-versions/`, `drafts/` und `design-draft/` sind unverändert.
- Die untracked Verzeichnisse `docs/features/` (Altbestand aus Schritt 14) und `acceptance-schritt-17.1.md` lagen schon vorher vor.

**Ablage (Scratchpad):**

- `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn17r2\` mit:
  - `local-ci.log`, `test-results-*.trx`, `prepush.log`, `test-ios-deployment.log`
  - `rep\repeat.log`
  - `o-sc*.txt` (Szenarien)
  - `mock\list.log` (Preisabfragen mit Zeitstempeln)
  - `upg-*.txt` (DB-Vergleich)
  - `zu-lang.png`, `optionen-routensuche.png`
- Werkzeuge: `C:\Users\Martin\AppData\Local\Temp\claude\D--Repositories-softwareschmiede-5c1f45e6-ae3c-459b-812a-2cf5576fb5d0\c397edbf-de25-4dac-ad83-3a5ac00606d0\scratchpad\abn17r2-tools\`
