# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Designabweichungen der Suchseite sind nicht dokumentiert.** Laut Anforderung folgt die Gestaltung dem Designentwurf. Die Vorgehensentscheidung „Designentwurf“ verlangt außerdem: „Jede Abweichung … wird in der Projektdokumentation festgehalten.“ Der Entwurf (`suche_kartenansicht/screen.png`) zeigt Radius und Spritsorte als Chips (Pillenform, ausgewählt `#0F766E` mit weißer Schrift, unausgewählt `#F1F5F9`). Die Stationskarte hat `rounded-lg` (16 px), Schatten der Ebene 1 und einen hervorgehobenen Preis rechts neben Name, Adresse und Entfernung. `MapPage.xaml` setzt das anders um:
  - Der Radius ist ein freies Zahlenfeld (`Entry` `Search.Radius` mit „km“-Suffix) statt Chips.
  - Spritsortenfilter und Sortierung sind `RadioButton`-Gruppen in eigenen Karten statt Chips.
  - Die Karten haben `RoundRectangle 12` und keinen Schatten.
  - Die Adresszeile fehlt, und die Preise stehen als Zeilen unter dem Namen.

  Die Abweichungen sind fachlich zum Teil begründbar, etwa das freie Feld wegen des Bereichs 1–25 km. Sie stehen aber weder in `README.md` („Bewusste Designabweichung“) noch in `docs/adr/` oder `docs/help/Suche/`. Im Plan (`docs/features/…/plan.md`) stehen die RadioButtons nur als Wiederverwendung eines vorhandenen UI-Musters, nicht als Abweichung vom Entwurf. Die fehlende Kartenansicht samt Umschalter „Liste/Karte“ ist durch Schritt 9 gedeckt und keine Abweichung.
- [ ] **Der Windows-Zwischenstand hat keinen Changelog-Inhalt.** Die Vorgehensentscheidung „Windows-Zwischenstände“ verlangt einen startfähigen Stand „mit kurzem Changelog“. `review-versions/0.1.7_2026-10-05/CHANGELOG.md` enthält nur „Keine Changelog-Beschreibung angegeben.“ Frühere Stände wie 0.1.6 haben eine Beschreibung. Der Stand selbst ist vorhanden und startfähig (siehe Hinweise).

## Hinweise

**Fachlich vollständig im Code wiedergefunden:**

- **Radius:** Gültig sind 1–25 km, Standard 5 (`SearchRadius`). Die Eingabe muss aus reinen Ziffern bestehen. Sie wird vor Standort- und API-Aufruf geprüft (`MapViewModel.SearchAsync`) und zusätzlich in `FuelPriceService.Validate`.
- **Filter:** „Alle“ oder eine der in den Einstellungen gewählten Sorten. Ein ungültiger Filter gilt als „Alle“.
- **Sortierung:** nach Preis, Entfernung oder Name, mit stabilen Nachrangkriterien. Voreingestellt ist `AppSettings.ResultSortOrder`.
- **Preiszeilen:** in der Reihenfolge der Einstellungen. Fehlende Preise, Entfernungen und Öffnungsstatus werden ausgeblendet, nicht durch Platzhalter ersetzt.
- **Preisalter:** Anzeige „vor X Min.“. Ab 60 Minuten wird der Alterstext per `DataTrigger` in `ColorTertiaryAmber` gefärbt, dazu kommt der Chip „Preis unbestätigt“.
- **Offline:** Banner „Offline: …“, zuletzt bekannte Preise mit Herkunftshinweis und passende Fehlermeldungen.
- **Standort:** Er wird nur beim Auslösen der Suche abgefragt. Bei „Nie“ gibt es keine Berechtigungs- oder Positionsabfrage (Fail Secure über `AllowsLocation`), sondern einen Hinweis.
- **Datenschutz:** `GeoPosition.ToString()` gibt keine Koordinaten aus. Geloggt werden nur Ausnahmetypen, gespeichert werden keine Nutzerpositionen. Der Suchcache hält gerundete Koordinaten nur im Arbeitsspeicher.
- **Plattformen:** iOS und MacCatalyst haben `NSLocationWhenInUseUsageDescription` = „Ermittlung von Tankstellen in der Nähe“. Das iOS-Privacy-Manifest enthält `PreciseLocation` (nicht verknüpft, kein Tracking). Windows hat `DeviceCapability location`.
- **Hilfe:** Die Seiten unter `docs/help/Suche/` sind vollständig und verlinkt.

**Umbenennung der Testmodus-Variable:** Alle vier Stellen nutzen die Konstante `TestDataPaths.TestDataPathEnvironmentVariable` = `TANKATLAS_TEST_DATA_PATH`: `AppDataPathProvider`, `PriceApiOptions`, `ApiKeyProvider`/`ApiKeyStoreSelector` und `LocationServiceSelector`. Der alte Name `TEST_DATA_PATH` wirkt nirgends mehr als Aktivierung. Ich habe `.github/`, `.githooks/` (`core.hooksPath`), `.git/hooks`, `scripts/` und die Testbasen durchsucht. Er kommt nur noch in historischen Einträgen (`changes.log`, frühere `acceptance-schritt-*.md`) und in Negativtests vor (`TestModeVariableTests_Rename`, `LocationServiceSelectorTests_TestMode`). Die Preisdienst-Variablen heißen weiterhin `TANKRADAR_PRICE_API_URL`/`_KEY`. Das ist nicht beauftragt, aber uneinheitlich.

**Bewertung der gemeldeten Abweichungen:**

- **`BindableLayout` in `ScrollView` statt `CollectionView`:** Funktional in Ordnung, aber ein echtes Leistungsrisiko. `BindableLayout` virtualisiert nicht. Bei 25 km in Großstädten liefert Tankerkönig leicht mehrere hundert Stationen. Jede Karte besteht aus etwa 15–25 Elementen, dazu kommen verschachtelte `BindableLayout`s für Preiszeilen. Das ergibt mehrere tausend native Controls. Weil die Liste bei jedem Filter- oder Sortierwechsel komplett ersetzt wird, werden sie jedes Mal neu aufgebaut. Spürbare Hänger unter WinUI und auf älteren iPhones sind wahrscheinlich. Kein Test prüft eine große Ergebnismenge, die E2E-Tests arbeiten mit wenigen Mock-Stationen. Empfehlung: Rückkehr zu `CollectionView` oder eine Begrenzung bzw. Seitenweise-Anzeige, plus ein Test mit etwa 300 Stationen. Die Anforderung nennt keine Leistungsvorgabe, deshalb ist das hier nicht als Abweichung gewertet.
- **Änderung am `TankerkoenigClient` (`wholeDay`/`openingTimes` auch aus `list.php`):** Die Änderung selbst schadet nicht. Nach der öffentlichen API-Dokumentation liefert die echte Umkreissuche `list.php` diese Felder aber nicht, sondern nur `detail.php`. Geprüft habe ich das nach Kenntnisstand, ohne Aufruf des echten Endpunkts. `MockTankerkoenigServer` wurde so erweitert, dass die Listenantwort die Felder enthält (`AlphaJson`, `GenericJson`). Der E2E-Test `SearchE2ETests_Basic` (`Search.Hint.Automated`) bestätigt damit ein Verhalten, das gegen den echten Dienst nicht eintritt: In der Live-Suche erscheint „Automatentankstelle“ praktisch nie. Bei einem Cache-Treffer nutzt `FindNearbyAsync` nur Öffnungszeiten aus einem früheren Detailabruf; Listenwerte werden ohnehin nicht gespeichert (`PriceRepository` speichert `WholeDay` nur bei `DetailsUpdatedUtc`). Nach Anforderung („soweit vorhanden“) und Entscheidung „Fehlende Quelldaten“ ist das Ausblenden zulässig. Empfehlung: Den Mock wieder an das reale Antwortformat angleichen und spätestens mit der Detailansicht (Schritt 8) gecachte Öffnungszeiten in die Listenaufbereitung einbeziehen.
- **Entfernter E2E-Test zur Umbenennung:** Das ist vertretbar. Alle E2E-Tests starten die App über `E2ETestBase` mit der neuen Konstante und belegen damit, dass der neue Name wirkt. Dass der alte Name nicht wirkt, prüfen Unit-Tests an allen vier Verbraucherstellen.
- **Verweigerte Standortberechtigung und Amber-Markierung nur auf Unit-Ebene:** Das ist vertretbar. Im Testmodus wird die echte Plattformberechtigung bewusst nie berührt. Farben lassen sich per UI Automation nicht verlässlich auslesen. Der `DataTrigger` ist im XAML vorhanden, `IsStale` und die Meldungstexte sind per Unit-Test abgedeckt. Die E2E-Tests decken Suchablauf, Filter, Sortierung, Spritsortenreihenfolge, „Nie“, Radiusvalidierung, Fehler- und Offline-Fall mit Altersanzeige ab.

**Weitere Beobachtungen:**

- Altersangaben werden nur bei Suche, Filter- oder Sortierwechsel oder beim erneuten Öffnen der Seite neu berechnet. Bleibt die Liste lange offen, veraltet „vor X Min.“, und die Amber-Markierung kommt zu spät.
- Der Offline-Hinweis steht am Anfang des scrollbaren Inhalts und scrollt mit, ist also keine feste Kopfzeile.

**Praktische Verifikation:**

- **`scripts/local-ci.ps1` (einmal, inkl. E2E):** Exit-Code 0. Pipeline-Skripte, Workflow-Validierung, iOS-Deployment-Skript (beide Umgebungen), Restore, Format, Sicherheitsprüfung (keine anfälligen Pakete) und Build mit Warnungen als Fehler sind OK. 343 Unit-Tests und 52 Integrationstests bestanden, Zeilenabdeckung 94,3 % (Schwelle 70 %). 32 FlaUI-E2E-Tests bestanden. Die iOS-Compile-Prüfung (`net10.0-ios`) ist OK. Die Paketierung war nicht angefordert und wurde übersprungen.
- **`scripts/test-ios-deployment.ps1`:** „iOS-Deployment-Pruefung erfolgreich.“
- **Windows-Zwischenstand `review-versions/0.1.7_2026-10-05/`:**
  - Der Stand ist vorhanden und per `.gitignore` ausgeschlossen. `bin/Tankradar.MAUI.dll` (19:22) enthält bereits `TANKATLAS_TEST_DATA_PATH`.
  - Ich habe ihn mit `TANKATLAS_TEST_DATA_PATH` auf ein temporäres Verzeichnis unter `%TEMP%` gestartet, ohne Preisdienst-Variablen und ohne Teststandort. Das Fenster „Tankatlas“ erschien, und die Datenbank lag ausschließlich im Testverzeichnis. Der Prozess hatte keine Verbindungen außer Loopback.
  - Danach habe ich ihn sofort per Name `Tankradar.MAUI` beendet und das Testverzeichnis gelöscht. Es läuft kein `Tankradar.MAUI` mehr.
  - Eine UI-Bedienung fand wie gewünscht nicht statt.
- Es wurde kein echter Tankerkönig-Endpunkt aufgerufen. Ich habe nichts committet, gepusht oder an Konfiguration geändert. Den Designentwurf habe ich nur im Scratchpad außerhalb des Repos entpackt.
