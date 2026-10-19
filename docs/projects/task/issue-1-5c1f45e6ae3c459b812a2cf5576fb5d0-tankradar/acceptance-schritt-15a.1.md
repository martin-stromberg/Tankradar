# Abnahmeprüfung – Entwicklungsschritt 15a (Anzeigename von Tankstellen und erster Verbrauch ab Startstand)

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Beide Rückmeldungen aus TestFlight sind im Kern gelöst und am Zwischenstand praktisch bestätigt.

- **Inhabername:** „Klaus Heinrich“ steht jetzt klein unter der Marke „Westfalen“. Das gilt auf der Startseite, in „In der Nähe“, in der Gruppenansicht, in den Suchergebnissen und in der Detailansicht. Der eigene Name hat überall Vorrang und übersteht Preisabruf, Neustart sowie Sicherung und Wiederherstellung.
- **Erster Verbrauch:** Die erste Volltankung liefert einen Verbrauch ab Startstand. Teilbetankungen davor zählen mit. Bei Hybridfahrzeugen gilt das nur für Kraftstoff, bei Elektrofahrzeugen für Strom. Liste, Auswertung und Verlauf bleiben konsistent, auch nach geändertem Start-km, Kraftstoffartwechsel, Korrektur und Löschen.

Nicht vollständig erfüllt sind zwei Punkte:

1. **Tankbuch-Zuordnung:** Der Plan verlangt dort ausdrücklich „Marke prominent, Stammdatenname klein darunter“. Umgesetzt ist nur der Anzeigename. Zwei Favoriten gleicher Marke im selben Ort sind nicht mehr zu unterscheiden. In 0.1.19 waren sie es noch.
2. **Präparierte Sicherung:** Eine Sicherung kann beliebig lange eigene Namen einschleusen. Ab einer extremen Länge stürzt die App bei „Vorgang erfassen“ reproduzierbar ab.

Außerdem ist die als erledigt dokumentierte Versionsangleichung des Zwischenstands nicht wirksam (niedrig).

**Prüfläufe (selbst ausgeführt, HEAD `24eca26`, `FUEL_PRICE_API_KEY` per `env -u` entfernt, nie gelesen):**

| Prüfung | Ergebnis |
|---|---|
| `local-ci.ps1` vollständig, E2E off-screen, 2 Läufe (12:44–12:56, 12:58–13:10) | jeweils Exit 1, nur wegen E2E 111/113. Die Fehlschläge stammen nachweislich von fremden Tastatureingaben (siehe Hinweise) und waren bei Wiederholung grün. |
| Alle übrigen Schritte von `local-ci` (Node, Workflows, iOS-Skript, Restore, Format, Sicherheitsprüfung, Build mit Warnungen als Fehler, Abdeckung, iOS-Compile) | OK in beiden Läufen. 0 Warnungen im Windows-Build, iOS nur `MAUI1001`, keine anfälligen Pakete. |
| Unit / Integration | 1336/1336 und 221/221, je 3× (beide `local-ci`-Läufe und Pre-Push) |
| Zeilenabdeckung | 93,4 % (Schwelle 70 %) |
| Pre-Push-Prüfungen wie `.githooks/pre-push`, ohne Push | Exit 0. Conventional Commits 6/6 (`194d49e..HEAD`) und 166 (`main..HEAD`). Muster, Format, XML-Doku, Übersetzung, Enums und Platzhalter OK. |
| Neue bzw. geänderte E2E-Klassen 3× (`FavoritesE2ETests_DisplayName`, `TankbookEntriesE2ETests_Flow`, `TankbookStatisticsE2ETests_Fallback`, `_Update`) | 5/5, 5/5, 3/5. Die zwei Fehlschläge enthalten fremde Eingaben (`Detail.Name` = „ntenZuhause tanken“). Die Wiederholung war 3/3. |
| `BackupServiceTests_Resilience.Restore_WithPooledConnectionHoldingWal_Succeeds` 5× einzeln | 5/5, dazu grün in allen 3 Integrationsläufen. Keine Flakiness beobachtet. |
| `DiagnosticsCaptureE2ETests` | in beiden Gesamtläufen grün (Bildschirm an), „Screenshot einfarbig“ trat nicht auf |

## Abweichungen

### 1. Tankbuch-Auswahl ohne Stammdatenname, gleichnamige Stationen nicht unterscheidbar (mittel)

- **Vorgabe:** Projektplan 15a nennt die Tankbuch-Zuordnung ausdrücklich: „Marke prominent … Stammdatenname klein darunter“.
- **Umsetzung:** `TankbookEntryService.GetKnownStationsAsync` liefert nur `StationNaming.GetPrimary(...)`. `TankbookEntryTexts.FormatKnownStationLabel` ergänzt bei Namensgleichheit nur den Ort. ADR 0012 nennt das „wie bei den Chips üblich“; die Abweichung vom Plan ist dort nicht begründet.
- **Praktisch belegt:** Zwei JET-Favoriten in Berlin („JET BERLIN HAUPTSTR. 1“, „JET BERLIN NEBENWEG 22“):
  - In 0.1.19 hießen die Chips genau so und waren unterscheidbar.
  - In 0.1.20 heißen beide Chips „JET (Berlin)“, auch für die Sprachausgabe.
  - Beide übernehmen nur „JET“ als Namen des Vorgangs. Welche Station gemeint ist, lässt sich nur raten.
  - Bei Marken, deren Quellname die Straße enthält (JET, Shell), ist das ein Rückschritt gegenüber 0.1.19.
- **Umsetzbar:**
  - Im Chip den Stammdatennamen als zweite Zeile zeigen, oder bei gleicher Beschriftung den Stammdatennamen bzw. Straße und Hausnummer ergänzen (z. B. „JET · Hauptstrasse 1“). Den Ort dafür nicht als einziges Unterscheidungsmerkmal verwenden.
  - Unit-Test und E2E-Test mit zwei Stationen gleicher Marke im selben Ort ergänzen.
  - ADR 0012 und die Hilfe anpassen.

### 2. Überlange eigene Namen aus einer präparierten Sicherung, Absturz bei „Vorgang erfassen“ (mittel)

- **Vorgabe:**
  - ADR 0012: `CustomName` höchstens 60 Zeichen.
  - Grundsatz aus Schritt 15: Eine ungültige Sicherung wird mit Fehlermeldung abgelehnt oder sauber verworfen; die App bleibt voll funktionsfähig.
- **Befund:** Der Neuaufbau (`BackupDatabasePreparer`) kopiert `Stations.CustomName` ungeprüft. Praktisch eingespielt wurden Ableitungen einer echten 0.1.20-Sicherung mit gültigen Prüfsummen:
  - **5.000 Zeichen:** angenommen. Jede Favoritenkachel wird bildschirmhoch (rund 25 Zeilen), die App bleibt bedienbar.
  - **200.000 Zeichen ohne Leerzeichen:** angenommen. Startseite, Gruppen- und Detailansicht funktionieren. „Vorgang erfassen“ beendet die App jedoch reproduzierbar (2 von 2 Versuchen; Ereignisprotokoll: `Microsoft.UI.Xaml.dll`, Ausnahme `0xc000027b`). Der Absturz bleibt bei jedem Neustart, solange ein solcher Name an einem Favoriten hängt.
  - **Behebbar** nur je Station über die Detailansicht („Eigenen Namen entfernen“; das Eingabefeld kürzt auf 60 Zeichen) oder durch Einspielen einer sauberen Sicherung. Die Ursache ist für Anwender nicht erkennbar.
- **Umsetzbar:**
  - Beim Neuaufbau `CustomName` mit mehr als `StationNaming.MaxCustomNameLength` Zeichen als `InvalidFile` ablehnen, alternativ verwerfen oder kürzen.
  - Integrationstest ergänzen (Sicherung mit 61 bzw. 200.000 Zeichen).
  - Als Härtung empfehlenswert, nicht 15a-spezifisch: Dieselbe Ursache besteht vermutlich für `Name` und `Brand` (seit Schritt 15 ungeprüft; nicht praktisch geprüft). Für diese Felder eine plausible Obergrenze vorsehen und lange Texte in Kacheln und Chips auf wenige Zeilen begrenzen (`MaxLines`/`LineBreakMode`).

### 3. Versionsangleichung des Zwischenstands wirkt nicht, obwohl als erledigt dokumentiert (niedrig)

- **Vorgabe:** Optionale Härtung aus Abnahme 15.
- **Dokumentiert als erledigt:** `changes.log` und die `CHANGELOG.md` von 0.1.20 schreiben „Der Windows-Zwischenstand trägt die Zwischenstandsversion (0.1.20) als App-Version“.
- **Befund:**
  - Richtig sind nur Dateiversion und Produktversion der EXE (0.1.20.0 bzw. `0.1.20+17bc48d…`).
  - Die App selbst (`AppInfo.VersionString`) meldet weiter „0.1.0.1“. Sichtbar ist das in der Rückfrage der Wiederherstellung („Sicherung vom 18.10.2026 16:34, Version 0.1.0.1 …“) und im Manifest einer 0.1.20-Sicherung (`appVersion` „0.1.0.1“).
  - **Ursache:** Die Assembly-Metadaten `Microsoft.Maui.ApplicationModel.AppInfo.Version` erzeugt MAUI (`Microsoft.Maui.Resizetizer.After.targets`) aus dem zwischengespeicherten `obj/…/Package.appxmanifest`. Das zusätzliche `-p:ApplicationDisplayVersion` in `scripts/create-review-version.ps1` greift beim inkrementellen Publish nicht.
- **Umsetzbar:**
  - Vor dem Publish das Zwischenmanifest bzw. `obj` der App entfernen oder neu erzeugen lassen.
  - Danach die Metadaten im Skript prüfen (z. B. Warnung, wenn `AppInfo.Version` nicht mit `-Version` beginnt).
  - `changes.log` gegebenenfalls korrigieren.

## Hinweise

### Anforderungstabelle

| Anforderung (Plan 15a / Entscheidungstabelle) | Umsetzung | Nachweis | Bewertung |
|---|---|---|---|
| Marke prominent, Stammdatenname klein darunter, ohne Marke bleibt der Name | `StationNaming` (eigener Name > Marke > Name; zweite Zeile nur bei Abweichung, Groß-/Kleinschreibung egal), `StationInfo.DisplayName/SubName` | `StationNamingTests_Display`, E2E `FavoritesE2ETests_DisplayName`, Zwischenstand | erfüllt |
| Startseite/Favoriten und „In der Nähe“ | `StationTile` mit `SubName`; Sprachausgabe der Kachel nennt beide Namen | E2E; Zwischenstand („JET, JET BERLIN HAUPTSTR. 1, 0,0 km …“) | erfüllt |
| Gruppenansicht | `FavoritesService.GetEntriesAsync` (Ordnung nach Anzeigename), `FavoriteGroup.Entry.SubName` | E2E, Unit, Zwischenstand | erfüllt |
| Suchergebnisse | `StationResultBuilder` (Name = Anzeigename, `SubName`, Sortierung) | E2E, `SearchMockServerTests_OwnerName`, Zwischenstand | erfüllt |
| Karte | Beschreibung der Markierung (Sprachausgabe) mit Anzeigename | Code-Prüfung (`MapMarkerBuilder`); kein Test mit Marke | erfüllt, Härtung siehe unten |
| Detailansicht | Überschrift = Anzeigename, `Detail.SubName` klein darunter, Marken-Chip entfällt (ADR 0004/0012) | E2E, Zwischenstand (Bild) | erfüllt |
| Tankbuch-Zuordnung | nur Anzeigename, Ort bei Gleichheit | `TankbookEntryServiceTests_KnownStationNames`; E2E nur mit eigenem Namen | **teilweise (Abweichung 1)** |
| Eigenen Namen vergeben, ändern, entfernen (Detailansicht) | Karte „Eigener Name“, `SetCustomNameAsync` (Ok/TooLong/NotFound), Eingabe max. 60 | E2E, `StationDetailViewModelTests_CustomName`; Zwischenstand (mehr als 60 Zeichen lehnt das Feld ab) | erfüllt |
| Lokal gespeichert, Migration erhält Daten | `Stations.CustomName`, Migration `AddStationCustomName` (additiv) | `StationCustomNameMigrationTests_Upgrade`; Upgrade 0.1.19 → 0.1.20 praktisch (siehe unten) | erfüllt |
| Teil der Sicherung, ältere Sicherungen einspielbar | Neuaufbau kopiert die Spalten der Referenz | `BackupServiceTests_CustomName`; praktisch: Sicherung mit Name wiederhergestellt, 0.1.19-Sicherung eingespielt | erfüllt; Längenprüfung fehlt (Abweichung 2) |
| Vorrang überall, nie vom Abruf überschrieben | `SaveAsync` setzt `CustomName` nie; `WithCustomNamesAsync` für Live-Daten | Unit/Integration; praktisch: „Preise aktualisieren“ (84 → 96 Preisstände), Name bleibt | erfüllt |
| Erste Volltankung liefert Verbrauch ab Start-km (Tank voll angenommen) | `ConsumptionCalculator.Calculate(entries, startKm, startKind)` | Unit, E2E, Zwischenstand | erfüllt |
| Teilbetankungen davor zählen mit | Teilsummen ab Startbasis | Unit; praktisch (20 L Teil + 30 L voll auf 500 km = 10,00) | erfüllt |
| Kennzeichnung „ab Startstand“ und Hinweis | „(ab Startstand)“ in der Verbrauchszeile, `ConsumptionNote`, Quelle in der Auswertung, Zusatz in der Verlaufszusammenfassung | E2E, Unit, Zwischenstand | erfüllt |
| Auswertung angepasst | `FromStart` nur ohne reguläres Intervall; Startverbrauch nur ohne vollen Vorgang | E2E `_Fallback`/`_Update`, Unit, Zwischenstand | erfüllt |
| Hilfe, ADR 0009 ergänzt | ADR 0009/0010/0011/0012, Hilfe (Details, Favoriten, Startseite, Suche, Tankbuch, Sicherung), README, `changes.log` | Sichtung | erfüllt (Abweichungen 1 und 3 betreffen auch die Doku) |
| Tests wie im Plan gefordert | Marke mit/ohne Marke, eigener Name inkl. Sicherung und Migration, Verbrauch ab Startstand; FlaUI: Name vergeben, Favoriten, erster Verbrauch | siehe Prüfläufe | erfüllt |
| Windows-Zwischenstand | `review-versions/0.1.20_2026-10-18` (Build aus `17bc48d`; bis HEAD nur Dokulöschung) | gestartet, geprüft | erfüllt; App-Version siehe Abweichung 3 |
| Härtungen aus 15 (optional) | `InvalidFile` bei Quellfehlern; Datenschutzhinweis in Hilfe und ADR 0011; Versionsangabe | praktisch: abgeschnittene Sicherungsdatenbank → „Die Datei ist keine gültige Tankatlas-Sicherung oder beschädigt. Deine Daten wurden nicht verändert.“ | 2 von 3 wirksam (Abweichung 3) |

### Fachliche Prüfung „Verbrauch ab Startstand“ (Zwischenstand 0.1.20, Werte nachgerechnet)

Fahrzeug Diesel1: Start 10.000 km, Startverbrauch 5,4. Vorgänge: Teilbetankung 20 L bei 10.200 km, voll 30 L bei 10.500 km, voll 30 L bei 11.000 km (je 1,70 €/L).

| Fall | Liste | Auswertung | Erwartet |
|---|---|---|---|
| 0.1.19 (Ausgangslage) | „Noch kein Verbrauch: erster voller Vorgang“, 6,00 | 6,00 | gemeldetes Problem reproduziert |
| 0.1.20 nach Upgrade | 10,00 (ab Startstand) + Hinweis, 6,00 | 6,00 „Berechnet aus vollen Vorgängen“, 10,20 €; Verlauf 2 Werte, „ältester Wert ab Startstand berechnet“ | ✓ |
| Start-km 10.000 → 10.100 | 12,50 (ab Startstand), 6,00 | 6,00; Verlauf höchster Wert 12,50 | ✓ (50 L / 400 km) |
| Kraftstoffart Diesel → Elektro | erster Vollvorgang „Noch kein Verbrauch“, 6,00 | 6,00 berechnet, kein Startwert | ✓ konsistent; Rückwechsel stellt 10,00 wieder her |
| Erste Volltankung zu Teilbetankung korrigiert | 8,00 (ab Startstand) | 8,00 „Ab Startstand …“, 13,60 € | ✓ (80 L / 1.000 km, 136 €) |
| Erste Volltankung gelöscht | 5,00 (ab Startstand) | 5,00 „Ab Startstand …“, 8,50 € | ✓ (50 L / 1.000 km) |
| Hybrid1 (Start 20.000): Strom voll 10 kWh bei 20.300, Kraftstoff voll 40 L bei 20.500 | Kraftstoff 8,00 (ab Startstand); Strom „Noch kein Verbrauch“ | Kraftstoff 8,00 „Ab Startstand“, 14,40 €; Strom „mindestens zwei volle Vorgänge“ | ✓ nur Kraftstoff |
| Elektro1 (Start 5.000): voll 15 kWh bei 5.100 | 15,00 kWh (ab Startstand) | 15,00 „Ab Startstand“, 6,00 € | ✓ Strom als Startart |
| Null1: erste Volltankung genau auf Start-km 30.000, Startverbrauch 5,4 | „Kein Verbrauch: keine Strecke seit dem Startstand“ | 5,40 „Startverbrauch – noch kein voller Vorgang“ | bekannter offener Punkt: Kennzeichnung sachlich ungenau (niedrig) |

Liste, Kennzahlen und Verlauf stammen aus derselben Berechnung (`TankbookEntryService.GetEntriesAsync`) und waren in allen Fällen konsistent. Der Startwert bleibt im Verlauf sichtbar. Gekennzeichnet ist er nur in der sichtbaren Zusammenfassung unter dem Diagramm, nicht am Balken selbst; das ist vertretbar.

### Anzeige und Barrierefreiheit

- **Anwenderproblem gelöst:** Die Kachel zeigt „Westfalen“ fett und „Klaus Heinrich“ klein in Sekundärfarbe, darunter die Adresse (Bildschirmaufnahme der Startseite und der Detailansicht). Ein eigener Name ersetzt die Marke; darunter steht dann der Stammdatenname, die Marke ist nicht mehr zu sehen (so in ADR 0012 beschrieben).
- **Sprachausgabe:**
  - Kacheln nennen Anzeigename und Stammdatenname.
  - Die Detailansicht liest Name und Zweitname als getrennte Texte.
  - Die Schaltflächen der Gruppenansicht und die Kartenmarkierungen nennen nur den Anzeigenamen. Bei mehreren Stationen gleicher Marke klingt das gleich („JET, Super E5 …“).
  - Härtung: Zweitname oder Straße in die Beschreibung der Markierung aufnehmen.
- **Kleinigkeiten:**
  - Der Hinweistext „Der eigene Name gilt nur auf diesem Gerät“ ist wegen der Sicherung missverständlich.
  - Beim Elektrofahrzeug spricht der Hinweis vom „Tank“ statt vom Akku.
  - Liefert die Quelle die Marke nur in anderer Schreibweise als den Namen (z. B. „shell“/„Shell“), gewinnt die Schreibweise der Marke.

### Testabdeckung der Markenanzeige (Mock ohne Marke)

- Vor 15a prüfte kein E2E-Test die Marke (der Chip `Detail.Brand` wurde nie abgefragt). Dass der Mock jetzt keine Marke liefert, entfernt also keine bestehende Prüfung.
- Folge der Umstellung: Fast alle E2E-Tests laufen über den Pfad „ohne Marke“. Der in der Praxis häufigste Fall „mit Marke“ wird per FlaUI nur über `StationZeta` in `FavoritesE2ETests_DisplayName` abgedeckt (Suche, Detail, Favoriten, Gruppe).
- Nur durch Unit- und Integrationstests abgedeckt sind: „In der Nähe“, Beschreibung der Kartenmarkierung, Tankbuch-Auswahl mit Marke (E2E nur mit eigenem Namen), Sortierung nach Name mit Marke, zwei Stationen gleicher Marke.
- Keine Abweichung, denn die Testvorgaben des Plans sind erfüllt. Empfohlen: ein E2E-Test mit zwei Mock-Stationen gleicher Marke im selben Ort (deckt Abweichung 1 ab) und „In der Nähe“ mit Marke.

### Bewertung der „bewusst offenen“ Punkte

| Punkt | Bewertung |
|---|---|
| Länge von `CustomName` beim Wiederherstellen ungeprüft | Nicht nur Härtung: Absturz belegt (Abweichung 2) |
| Identische Beschriftung in der Tankbuch-Auswahl | Nicht unterscheidbar, Rückschritt gegenüber 0.1.19 (Abweichung 1) |
| Erster voller Vorgang genau auf dem Startstand | bestätigt; Auswertung zeigt „Startverbrauch – noch kein voller Vorgang“, obwohl ein voller Vorgang existiert; niedrig |
| Doppelte Startart-Regel, Kopiermethode `WithCustomName`, keine UUID-Prüfung in `SetCustomNameAsync`, `_savedCustomName` bis zum nächsten `Show` | Wartungs- bzw. Randrisiken ohne beobachtete Wirkung; niedrig, Einschätzung des Code-Reviews geteilt |

### Zwischenstand und Upgrade (praktisch, off-screen, nur UI-Automation, Testpfad im Scratchpad)

- **Testaufbau:**
  - Lokaler Preisdienst-Mock auf `127.0.0.1:18765` (Python, nur Loopback) mit sechs Stationen, darunter Inhabername/Westfalen und zwei JET im selben Ort; Testschlüssel `mock-key-abn15a`.
  - Gestartet nur mit gesetztem `TANKATLAS_TEST_DATA_PATH` (`…\scratchpad\abn15a\d19` bzw. `d20`) und `TANKATLAS_TEST_WINDOW=offscreen`.
- **0.1.19 (`review-versions/0.1.19_2026-10-18`):**
  - Über die Oberfläche angelegt: Gruppe „Heimat“ mit 4 Favoriten, Fahrzeuge Diesel1/Hybrid1/Elektro1, 3 Vorgänge.
  - 3 weitere Vorgänge per SQL bei geschlossener App.
  - Sicherung erstellt.
- **Upgrade auf 0.1.20 (Kopie der Daten):**
  - Migration `20261018094636_AddStationCustomName` angewendet, Spalte `CustomName` vorhanden.
  - Stationen 6, Favoriten 4, Vorgänge 6, Fahrzeuge 3, `CustomName` überall NULL.
  - Startseite sofort mit Marke und Zweitname.
- **Eigener Name:**
  - „Zuhause“ vergeben → Detail, Favoriten, Gruppe und Tankbuch-Auswahl zeigen „Zuhause“ / „Klaus Heinrich“.
  - Preisabruf überschreibt ihn nicht.
  - Entfernen → „Westfalen“, Meldung „Der eigene Name wurde entfernt.“
- **Sicherung und Wiederherstellung:**
  - Sicherung aus 0.1.20 enthält `CustomName` „Zuhause“. Nach Entfernen und Wiederherstellen ist der Name zurück.
  - Die 0.1.19-Sicherung in 0.1.20 wird angenommen, migriert, `integrity_check` ok, ohne eigenen Namen. Der Verbrauch ab Startstand erscheint sofort.
- **Version:**
  - EXE: Dateiversion 0.1.20.0, Produktversion `0.1.20+17bc48d…`; HEAD `24eca26` unterscheidet sich davon nur durch gelöschte Doku-Dateien.
  - In der App: „0.1.0.1“ (Abweichung 3).

### E2E-Fehlschläge: Ursache fremde Tastatureingaben (Umgebung, nicht 15a)

- **Betroffene Tests:** In den beiden Gesamtläufen und einem Wiederholungslauf schlugen sechs verschiedene E2E-Tests fehl: `BackupE2ETests_Import`, `TankbookStatisticsE2ETests_Flow`, `BackupE2ETests_Persistence`, `BackupE2ETests_TamperedSchema`, `FavoritesE2ETests_DisplayName`, `TankbookEntriesE2ETests_Flow`. Alle waren bei Wiederholung grün.
- **Diagnosedaten** belegen fremde Eingaben in das unsichtbare Testfenster:
  - Fahrzeugname „git -C ../ta-push push origin task/iGolf“.
  - Eigener Name „ntenZuhause tanken“.
  - Mehrfach ein unerwarteter Wechsel auf den Reiter „Favoriten“.
  - Während der Abnahme wurde in derselben Desktop-Sitzung getippt; die Tastatureingaben landeten im Testfenster.
- **Hinweis zu Schritt 6a:** Die E2E-Hilfen setzen den Fokus auf Eingabefelder (`SettingsE2ETestBase.SetFieldValue` → `Focus()`). Damit fängt das unsichtbare Fenster Tastatureingaben des Anwenders ab. Das stört den Anwender (seine Eingaben gehen verloren) und macht Läufe unzuverlässig.
- **Empfehlung:** Fokuswechsel entfernen bzw. durch reine `ValuePattern`-Zustellung ersetzen. Meine UIA-Hilfen für den Zwischenstand arbeiteten ohne `SetFocus` zuverlässig. Alternativ E2E nur ohne parallele Eingaben laufen lassen.
- Kein Bezug zu 15a.

### Datenschutz und iOS

- **Daten:** Eigener Name, Stationen und Preisstände bleiben lokal. Der Datenschutzhinweis zur Sicherung steht in Hilfe und ADR 0011.
- **Tests:** keine Produktivendpunkte im Diff, nur Mock-Server; kein Schlüssel. `forbidden-patterns-check --all --strict` grün (896 Dateien).
- **iOS:** `Tankradar.MAUI.csproj` und `Info.plist` unverändert, keine neue `PackageReference`, keine geänderte Projektdatei. iOS-Compile-Prüfung OK.

### Sonstiges

- **Repository:**
  - Keine Commits, keine Pushes, kein Code geändert. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` unverändert (keine Datei jünger als der Beginn der Abnahme).
  - `../ta-push` nicht angefasst.
  - `TestResults/`, `coverage-report/` und `e2e-diagnostics/` erzeugt `local-ci` selbst (gitignoriert).
- **Testdaten, Mock und Diagnosekopien** liegen im Scratchpad (`abn15a/`).
- **Eigene Prozesse** per PID beendet:
  - App 0.1.19: 44284, 13700.
  - App 0.1.20: 36940 (abgestürzt, Abweichung 2), 36112 (abgestürzt), 23988.
  - Mock: 12072, 15748.
  - Am Ende lief kein `Tankradar.MAUI`-, `testhost`- oder Mock-Prozess.
- **Keine Systemeinstellungen geändert;** keine Maus- oder Tastatureingaben, nur UI-Automation und `PrintWindow`-Aufnahmen des eigenen Fensters.
- **Vom Berechtigungssystem abgelehnt:**
  - `curl` gegen den lokalen Mock (Funktionsprobe): nicht ersetzt; der Mock wurde nur indirekt über die App genutzt.
  - Ein zusammengesetzter Befehl mit `rm -rf`: ohne das Löschen erneut ausgeführt; das Verzeichnis existierte nicht.
  - Keine Ablehnung wurde umgangen.
- **Zeitlicher Ablauf:** Die Abnahme war zwischen 13:10 und 16:20 wegen eines Nutzungslimits unterbrochen. Danach wurden die Läufe ausgewertet und die fehlenden Prüfungen nachgeholt.
