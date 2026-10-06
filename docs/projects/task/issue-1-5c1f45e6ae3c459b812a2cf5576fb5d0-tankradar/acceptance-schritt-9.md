# Abnahmeprüfung – Entwicklungsschritt 9

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Geprüft wurde der Diff `f852af9...HEAD` (Merge-Base mit dem Projekt-Basisbranch) auf
`task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-9-kartenansicht` (Stand `8b3df55`) gegen
`project-plan.md`, Abschnitt „### Schritt 9: Kartenansicht der Suchergebnisse“, die Tabelle „Grobe
Vorgehensentscheidungen“ (Karte, Preisniveau-Farben, Designentwurf, externe Dienste in Tests) und den
Designentwurf (Screen `suche_kartenansicht`, außerhalb des Repos entpackt).

Abgedeckte Punkte der Anforderung:

| Anforderung | Umsetzung | Nachweis |
| --- | --- | --- |
| Wechsel Liste/Karte, Vorbelegung aus Standardansicht | `MapViewModel.ViewOptions` (Chips `Search.View.*`), `SelectView` beim Laden/Ändern von `AppSettings.ResultView`; ein Wechsel startet keine Suche | Unit `MapViewModelTests_MapView`, E2E `MapE2ETests_View` (Wechsel, Standardansicht „Karte“) |
| Plattformübergreifende Karte auf OSM-Kacheln, FlaUI-prüfbar | eigene `StationMapView` (Kacheln als `Image`, Markierungen als `Button` mit AutomationIds), Mathematik in `MapViewport`; kein neues NuGet-Paket | Unit `MapViewportTests_Math`, E2E `MapE2ETests_*` |
| Quellenangabe „© OpenStreetMap-Mitwirkende“ sichtbar | dauerhaft unten rechts (`Map.Attribution`) | E2E `Map_ShowsAttributionLegendAndLoadsTilesFromMockServerOnly` |
| Zoomen und Verschieben | Schaltflächen +/−, Kneifgeste (ganzzahlige Stufen 4–18), Wischgeste, Pfeil-Schaltflächen, „Ausschnitt zurücksetzen“ | E2E `MapE2ETests_Navigation` (Zoomgrenzen, Zoom blendet Markierungen aus, Verschieben) |
| Eigener Standort bzw. gesuchte Position markiert; Standort nur gemäß GPS-Einstellung, nicht gespeichert | `MapOrigin` (Art `CurrentLocation`/`SearchedPlace`) nur im ViewModel-Speicher, `ToString()` ohne Koordinaten, verworfen bei neuer Suche/Fehler/Moduswechsel; Standortabfrage unverändert über bestehende Logik, Adresssuche fragt keinen Standort ab | Unit `LocationSearch_MarksOwnLocationAndAllStations`, `AddressSearch_MarksSearchedPlace_WithoutLocationQuery`, `Origin_IsDiscardedOnFailureAndModeChange_AndNeverLogged`; E2E `LocationSearch_MarksOwnLocation`, `AddressSearch_MarksSearchedPosition` |
| Markierung mit Preis, Farbe nach Preisniveau (Grün/Rot/Teal/Grau) | `PriceLevelClassifier` (exakte Dezimalarithmetik, Rot ab Minimum + 2/3 Spanne, nur bei Spanne > 0), `MapPalette`, Legende unter der Karte, Preisniveau zusätzlich als Bedienhilfe-Hinweis | Unit `PriceLevelClassifierTests_Levels` (Spanne, exakte Drittelgrenze, gleiche Preise, Gleichstand beim Minimum, einzelne Station, zwei Preise, geschlossene Stationen, nur geschlossene, ohne Preis, leer) |
| Maßgeblich: gefilterte Sorte, sonst erste Sorte der Einstellungen | `StationResultBuilder.ResolveFuelType` (dieselbe Regel wie die Preissortierung) | Unit `MapMarkerBuilderTests_Markers`; E2E `Markers_ShowPriceAndLevelOfFirstSelectedFuel`, `Filter_RebuildsMarkersForFilteredFuel` |
| Antippen einer Markierung öffnet die Detailansicht | `MarkerCommand` → `OpenStationCommand` | E2E `TapMarker_OpensDetailsAndBackReturnsToMap` |
| Kacheln in Tests nie von produktiven Servern | Endpunkt nur im Testmodus über `TANKRADAR_TILE_URL` (Loopback-HTTP, `MockTileServer` auf 127.0.0.1); im Testmodus ohne Adresse kein Abruf (kein Rückfall auf `tile.openstreetmap.org`); Unit-Tests nutzen `FakeHttpMessageHandler` (die produktive URL erscheint dort nur als erwarteter Wert, ohne Netzzugriff) | Unit `TileServerOptionsTests_Validation`, `HttpTileSourceTests_Fetch`; Integration `TileSourceMockServerTests_Flow`; E2E prüft Pfade und User-Agent der Mock-Abrufe |
| Windows-Zwischenstand | `review-versions/0.1.13_2026-10-06/` vorhanden, `CHANGELOG.md` beschreibt Schritt 9, Verzeichnis per `.gitignore` ausgeschlossen | siehe Verifikation |
| Zusatz: ADR 0004 um zwei Kleinabweichungen ergänzt | „Logo und Avatar in der Kopfleiste“ und „Geöffnet ohne ‚bis HH:MM‘“ (nachgetragen in Schritt 9), jeweils begründet | `docs/adr/0004-station-detail-design-deviations.md` |
| Zusatz: `validate-workflows.py` erkennt Verzeichnis-Uploads und `gh release upload` | `classify_upload_path` (Verzeichnisse, Muster, Variablen, unbekannte Endungen, Pfade außerhalb des Arbeitsbereichs), `gh_release_assets`/`check_release_commands` für `gh release create/upload` (auch mehrzeilig) | `scripts/test_validate_workflows.py`; aktuelle Workflows bestehen die Prüfung |

Designabweichungen (Vorschaukarte entfällt, Markierungen ohne Marke, Chips statt Segmentschalter,
Pfeil-Schaltflächen, Zähler „X von Y Stationen sichtbar“, feste Kartenhöhe 440 px, Farbwerte mit
Kontrastbegründung) sind in ADR 0005 festgehalten; die Hilfe (`docs/help/Suche/*`,
`docs/help/Einstellungen/beschreibung.md`, `docs/help/index.md`, `docs/help/ci-cd/workflows.md`) und das
README sind aktualisiert.

### Praktische Verifikation

- `scripts/local-ci.ps1` einmal vollständig (inkl. Sicherheitsprüfung und E2E off-screen): **erfolgreich**.
  Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (beide Umgebungen), Restore, Formatprüfung,
  Sicherheitsprüfung („Keine anfaelligen Pakete gefunden“), Build mit Warnungen als Fehler (0 Fehler),
  Unit 591/591, Integration 91/91, Zeilenabdeckung 94,7 % (Schwelle 70 %), FlaUI-E2E 55/55,
  iOS-Compile-Prüfung OK. Windows-Paket wie vorgesehen übersprungen (nur mit `-Package`). Keine
  Wiederholung nötig.
- `scripts/test-ios-deployment.ps1`: „iOS-Deployment-Pruefung erfolgreich.“
- Zwischenstand `review-versions/0.1.13_2026-10-06/`: vorhanden, `CHANGELOG.md` nennt Schritt 9;
  Start off-screen mit temporärem `TANKATLAS_TEST_DATA_PATH` erfolgreich (Fenster „Tankatlas“, Prozess
  lief nach 10 s noch, legte nur `tankatlas.db*` im Testverzeichnis an), danach gezielt beendet und das
  Testverzeichnis entfernt; kein `Tankradar.MAUI`-Prozess verblieben.
- Es wurden keine echten Endpunkte (Tankerkönig, Nominatim, Kachelserver) aufgerufen; der
  `FUEL_PRICE_API_KEY` wurde nicht ausgelesen.

## Abweichungen

Keine.

## Hinweise

- **OSM-Tile-Usage-Policy – Caching:** Die Richtlinie verlangt vorrangig, die Caching-Header des Servers
  (`Cache-Control`, `Expires`, `ETag`) zu beachten, und nennt mindestens 7 Tage nur für den Fall, dass die
  Header nicht ausgewertet werden können. `HttpTileSource` speichert pauschal 7 Tage (Dateialter) und
  sendet keine bedingten Anfragen (`If-None-Match`/`If-Modified-Since`). Das ist die zulässige
  Rückfallregel und für den Server eher schonend; ADR 0005 sollte aber nicht pauschal „Richtlinie wird
  eingehalten“ behaupten, ohne diese Vereinfachung zu nennen. Empfehlung (optional): bedingte Anfragen mit
  `ETag` bei Ablauf, damit unveränderte Kacheln mit 304 beantwortet werden.
- **OSM-Tile-Usage-Policy – übrige Punkte erfüllt:** eigener User-Agent (`Tankatlas/0.1
  de.martinstromberg.tankradar`, kein Bibliotheks-Standard, keine Browser-Imitation), höchstens zwei
  parallele Abrufe (per Unit-Test belegt), kein Vorabruf/Bulk (nur sichtbare Kacheln, 150 ms Verzögerung,
  Abbruch nicht mehr sichtbarer Kacheln), keine `no-cache`-Header, Wartefenster nach Fehlern, sichtbare
  Attribution. Verbesserungsmöglichkeiten: Die Version im User-Agent ist fest „0.1“ statt der
  tatsächlichen App-Version, und eine Kontaktangabe (URL/E-Mail, laut Richtlinie optional, aber
  erwünscht) fehlt. Die Quellenangabe ist nicht mit https://www.openstreetmap.org/copyright verlinkt
  (die Richtlinie zeigt sie als Link; für eine App ist ein reiner Text verbreitet, ein antippbarer Link
  wäre die sauberere Lösung).
- **Wahl des Kachelanbieters für die öffentlich verteilte App:** ADR 0005 begründet `tile.openstreetmap.org`
  nachvollziehbar (Konsistenz mit Nominatim, keine Schlüssel, Richtlinienmaßnahmen) und benennt, dass bei
  starker Verbreitung ein eigener oder kommerzieller Anbieter einzuplanen ist. Der Austausch ist mit
  geringem Aufwand möglich (`TileServerOptions.DefaultUrlTemplate`, HTTPS-Validierung, `ITileSource`), ist
  produktiv aber nur per Codeänderung und neuem Build möglich (keine Laufzeitkonfiguration) – bei einer
  Sperre durch die OSMF (Richtlinie: Sperre ohne Vorwarnung möglich) bliebe die Karte bis zu einem
  App-Update ohne Hintergrund (Markierungen bleiben bedienbar). Vor einer breiteren App-Store-Verteilung
  sollte die Entscheidung erneut geprüft werden (z. B. Messung des Kachelaufkommens, ggf. Wechsel des
  Anbieters mit Schlüsselbehandlung wie beim Routing-Dienst).
- **Preisniveau-Auslegung:** Geschlossene Stationen gehen nicht in die Spanne ein und sind grau; das ist in
  ADR 0005 und Hilfe begründet und mit der Vorgehensentscheidung vereinbar. Unbekannter Öffnungszustand
  gilt als geöffnet; Stationen ohne Preis der maßgeblichen Sorte erscheinen bei „Alle“ als Teal-Markierung
  mit Anfangsbuchstaben (dokumentiert). Randnotiz: Die Spanne wird nur über Stationen mit gültiger
  Position gebildet (`MapMarkerBuilder` filtert vorher); eine Station ohne Koordinaten beeinflusst die
  Einstufung daher nicht. Praktisch ohne Bedeutung, da sie ohnehin nicht auf der Karte erscheint.
- **Konsistenz Liste/Karte:** Liste und Karte nutzen dieselbe gefilterte Gesamtmenge (`_allStations`,
  die Karte nicht nur die erste Listenseite) und dieselbe Sortenregel wie die Preissortierung; der
  E2E-Test `SwitchView_BetweenListAndMap_ShowsTheSameResults` belegt das. Die Liste selbst zeigt keine
  Preisniveau-Farben – das fordert die Anforderung auch nicht.
- **Standortschutz / Kachel-Cache:** Die Suchposition liegt nur im Arbeitsspeicher und wird weder
  persistiert noch protokolliert (Logausgaben nennen nur Ausnahmetypen bzw. HTTP-Status, nie
  Kachelnummern). Der Kachel-Cache (`<AppDataDirectory>/tiles/z/x/y.png`, bis 100 MB, 7 Tage) lässt – wie
  bei jeder Karten-App üblich – Rückschlüsse auf betrachtete Gebiete zu (hohe Zoomstufen um die
  Suchposition). Er liegt im App-Datenverzeichnis (unter iOS `Library`, Teil von Geräte-/iCloud-Backups)
  statt im Cache-Verzeichnis (`FileSystem.CacheDirectory`, vom Backup ausgenommen und vom System
  bereinigbar). Empfehlung: Cache-Verzeichnis verwenden und den Hinweis auf den lokalen Kachelspeicher in
  ADR 0005 ergänzen (die Hilfe erwähnt ihn bereits).
- **Bedienbarkeit (nur auf Gerät prüfbar):** Die Karte (440 px) liegt in der scrollenden Suchseite; unter
  iOS kann die Wischgeste der Karte mit dem Seiten-Scroll konkurrieren (in ADR 0005 benannt, Schaltflächen
  als Ausweichweg vorhanden). Unter Windows reagiert der MAUI-`PanGestureRecognizer` erfahrungsgemäß nur
  auf Touch/Stift, nicht auf Ziehen mit der Maus; ein Mausrad-Zoom fehlt. Mit Maus wird die Karte daher
  über die Schaltflächen bedient – für die Entwicklungs-/Testplattform ausreichend, aber nicht mit dem
  Gerät gleichwertig. Die Kneifgeste ändert die Zoomstufe erst am Ende der Geste um genau eine Stufe
  (kein stufenloses Mitzoomen); das sollte in TestFlight auf Akzeptanz geprüft werden.
- **Zusatz `validate-workflows.py`:** Die neue Prüfung deckt `actions/upload-artifact` und
  `gh release create/upload` ab. Andere Veröffentlichungswege (z. B. `softprops/action-gh-release` mit
  `files:`) werden nicht erkannt; sie werden derzeit nicht verwendet.
- **Arbeitskopie:** Das unversionierte Verzeichnis `docs/features/` enthält nur Testprotokolle aus
  Schritt 2 und gehört nicht zu diesem Schritt; es sollte weder committet noch übersehen werden.
- Der Projekt-Basisbranch enthält eine neuere Korrektur der E2E-Testbasis (`E2EStartupPolicy`), die in
  diesem Branch fehlt; das ist laut Auftrag kein Mangel dieses Schritts. Der E2E-Lauf war hier auch ohne
  diese Korrektur vollständig grün.
