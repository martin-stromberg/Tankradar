# Abnahmeprüfung – Entwicklungsschritt 7

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

### Geprüfte Anforderungspunkte (Code, Tests, Dokumentation)

- **Adresssuche neben Standortsuche:** Der Bereich „Karte“ hat die Suchart-Chips „Aktueller Standort“ und
  „Adresse, Ort oder PLZ“ (`SearchMode`, `MapViewModel.ModeOptions`). Das Eingabefeld mit „Eingabe löschen“
  erscheint nur im Adressmodus (`MapPage.xaml`). Die aufgelöste Position läuft durch denselben Pfad
  (`StationSearchQuery` → `SearchNearbyAsync` → `ApplyResult`) wie die Standortsuche. Damit gelten dieselbe Liste,
  Preise mit Alter, Hinweise, Sortierung und Spritsortenfilter. Die Entfernung bezieht sich auf die gesuchte Position.
  Das zeigt der E2E-Test `AddressSearch_ShowsStationsAroundAddress` mit „0,0 km“ und den Koordinaten aus dem Mock.
- **Radius:** Die Chips 1–25 km gelten wie bei der Standortsuche. Das ist laut der Entscheidung „Suchradius“
  verbindlich und hat Vorrang vor „5–50 km“. Die Begründung steht in ADR 0003.
- **Nominatim auf beiden Plattformen:** `NominatimGeocodingService` ist plattformneutral über `HttpClient` in
  `MauiProgram` registriert. Es gibt keinen plattformspezifischen Geocoder.
- **Nutzungsrichtlinie:**
  - Anfragen gehen nur über `SearchCommand` bzw. `ReturnCommand` hinaus. Es gibt keinen Handler für Textänderungen
    und keine Autovervollständigung. `IsTextPredictionEnabled=False` ist gesetzt.
  - Es gibt eine eigene `RequestThrottle` mit `MinRequestInterval` ≥ 1 s. `Validate()` erzwingt das auch im
    Testmodus. Der Zeitpunkt wird unter einer Semaphore erfasst. Wird mehrmals schnell abgesendet, bricht
    `CancelSearch()` die alte Suche zwar ab, die neue Anfrage wartet aber trotzdem auf den Ablauf des Intervalls.
    Das belegen Unit-Tests (750-ms-Wartezeit, serialisierte parallele Aufrufe) und der E2E-Test
    `TwoQuickSearches_RespectOneRequestPerSecond` (Abstand ≥ 950 ms am Mock-Server).
  - Der `User-Agent` lautet „Tankatlas/0.1 de.martinstromberg.tankradar“.
  - Es gilt: nur HTTPS, keine Weiterleitungen, kein automatisches Wiederholen, `limit=1`.
  - Die Quellenangabe „Geodaten © OpenStreetMap-Mitwirkende“ ist im Adressmodus sichtbar. Ein E2E-Test prüft das.
- **Eingabevalidierung vor dem API-Aufruf:** `AddressInput.Validate` läuft im ViewModel vor jedem Dienstaufruf und
  noch einmal im Dienst selbst. Die Whitelist lässt alle Unicode-Buchstaben und -Ziffern zu, also auch Umlaute und
  ß, dazu `. , - ' / ( ) & + # :`. Gültig sind 3–120 Zeichen, Leerraum wird normalisiert. Damit werden „Hauptstraße 1,
  10115 Berlin“, „Köln-Ehrenfeld“, „St. Ingbert (Saar)“, „Straße des 17. Juni 1“ und Hausnummern wie „12a“
  angenommen. Steuerzeichen, `< > " \ | % ;` und Bidi-Zeichen werden abgewiesen. Der Suchbegriff wird zusätzlich
  per `Uri.EscapeDataString` maskiert.
- **Meldungen:** Für „kein Treffer“ („…in Deutschland kein Ort gefunden…“), offline (vor der Anfrage, ohne
  Netzaufruf), nicht erreichbar/Timeout/5xx/429, Ablehnung (4xx) und unerwartete Antwort gibt es jeweils einen
  eigenen, verständlichen Text. Nach einem Fehler wird die Ergebnisliste geleert.
- **Keine Speicherung/Protokollierung:** Die Eingabe liegt nur im Arbeitsspeicher. Protokolliert werden nur Status und
  Ausnahmetyp. `GeocodingResult.ToString()` gibt nur den Status aus. Integrationstests durchsuchen die Datenbankzeilen,
  die SQLite-Datei (auch binär nach den Double-Werten) und das Protokoll auf Adresse, Ortsname und Koordinaten.
- **Ohne GPS / „GPS-Nutzung: Nie“:** Der Adresspfad ruft `ILocationService` nicht auf. Unit- und E2E-Tests
  (`AddressSearch_WorksWithGpsNever`) prüfen das.
- **`countrycodes=de`:** Die Beschränkung ist fachlich vertretbar, weil Tankerkönig nur deutsche Tankstellen liefert.
  Grenznahe Suchen per deutscher Adresse funktionieren weiter. Die Beschränkung ist in ADR 0003 und in der Hilfe
  („Einschränkungen“) dokumentiert, und die Meldung bei fehlendem Treffer nennt „in Deutschland“.
- **Testmodus:** Ist `TANKATLAS_TEST_DATA_PATH` gesetzt, aber `TANKRADAR_GEOCODING_URL` fehlt oder ist ungültig, gilt
  `EndpointNotConfigured`. Dann wird die Auflösung ohne Netzaufruf verweigert, es gibt keinen Rückfall auf den
  produktiven Dienst. Eine Override-URL wirkt nur im Testmodus. HTTP ist nur zu Loopback-Adressen erlaubt (Unit-Tests
  in `GeocodingOptionsTests_Validation`).
- **Keine echten Endpunkte in Tests:** Unit-Tests nutzen `FakeHttpMessageHandler` mit `geo.example.test`.
  Integrations- und E2E-Tests nutzen `MockNominatimServer` auf `127.0.0.1` mit freiem Port. `nominatim.openstreetmap.org`
  kommt in Tests nur als Zeichenkette in Options-Prüfungen vor, ohne Netzaufruf.
- **Testqualität (Stichprobe):** Die Tests sind aussagekräftig und prüfen konkrete Werte und nicht nur, dass kein
  Fehler auftritt:
  - `AddressInputTests_Validation` deckt gültige deutsche Adressen, Normalisierung, alle Fehlerarten und
    Injektionsversuche ab.
  - `NominatimGeocodingServiceTests_Resolve` prüft Abfrageparameter (`q`, `limit`, `countrycodes`, `format`,
    `accept-language`), `User-Agent`, das Kürzen des Anzeigenamens und das Entfernen von Steuerzeichen.
  - `NominatimGeocodingServiceTests_Throttle` prüft die Wartezeit exakt über eine Fake-Uhr.
  - `MapViewModelTests_Address` (17 Tests) deckt Moduswechsel, Offline, Fehlerstatus, Filter/Sortierung ohne neuen
    Aufruf und „nichts gespeichert“ ab.
  - Die FlaUI-Tests decken den Ablauf ab: Treffer, Sortierung/Filter, GPS „Nie“, Sichtbarkeit von Feld, Quellenangabe
    und Löschen-Schaltfläche, kein Treffer, ungültige Eingabe ohne Anfrage, nicht erreichbarer Dienst, Drosselung.
- **Dokumentation:** `docs/help/Suche/` (Beschreibung, Ablauf), ADR 0003 und README sind ergänzt.

### Praktische Verifikation

- `scripts/local-ci.ps1` lief einmal vollständig (inkl. E2E off-screen und Sicherheitsprüfung) und endete mit
  „Lokaler Prüflauf erfolgreich.“, Exit-Code 0:
  - Unit 463/463, Integration 82/82, FlaUI-E2E 41/41
  - Zeilenabdeckung 94,8 % (Schwelle 70 %)
  - Statische Analyse mit Warnungen als Fehler: 0 Fehler
  - Sicherheitsprüfung: „Keine anfaelligen Pakete gefunden.“
  - iOS-Compile-Prüfung erfolgreich
  - Eine Wiederholung war nicht nötig.
- `scripts/test-ios-deployment.ps1` meldet „iOS-Deployment-Prüfung erfolgreich.“
- Zwischenstand `review-versions/0.1.10_2026-10-06/`:
  - Er ist vorhanden und per `.gitignore` ausgeschlossen.
  - `CHANGELOG.md` beschreibt Schritt 7.
  - `bin/Tankradar.MAUI.exe` startete mit temporärem `TANKATLAS_TEST_DATA_PATH` und
    `TANKATLAS_TEST_WINDOW=offscreen`. Das Fenster „Tankatlas“ war vorhanden und der Prozess lief nach 8 s noch.
    Danach wurde gezielt nur dieser Prozess beendet und das temporäre Verzeichnis entfernt.
- Es wurden keine echten Endpunkte aufgerufen. Das Arbeitsverzeichnis ist nach dem Lauf unverändert, bis auf
  das bereits vorher vorhandene, nicht versionierte `docs/features/`.

### Beobachtungen (nicht abnahmerelevant)

- **Typografische Zeichen:** Die Whitelist lässt das typografische Apostroph `’` (U+2019) und Gedankenstriche
  `–`/`—` nicht zu. Die iOS-Tastatur ersetzt `'` mit „Intelligente Interpunktion“ standardmäßig durch `’`. Eingaben
  wie „Up’n Kamp“ würden dann mit „ungültige Zeichen“ abgelehnt. Bei deutschen Adressen ist das selten. Empfehlung
  für später: `’` und `–` vor der Prüfung auf `'` bzw. `-` normalisieren oder zulassen.
- **Kennung uneinheitlich dokumentiert:** Die Hilfe (`docs/help/Suche/beschreibung.md`) nennt die Kennung
  „Tankatlas/0.1 (de.martinstromberg.tankradar)“ mit Klammern. Code und ADR verwenden sie ohne Klammern.
  Die Nominatim-Richtlinie empfiehlt außerdem eine Kontaktmöglichkeit (URL/E-Mail) in der Kennung. Die App-Kennung
  reicht zur Identifikation, könnte aber ergänzt werden. Die Versionsangabe `0.1` ist fest codiert und folgt nicht
  der App-Version.
- **Missverständliche Voraussetzung in der Hilfe:** In `docs/help/Suche/ablauf-anwender.md` nennt der Abschnitt
  „Voraussetzungen“ weiterhin „Standort und GPS steht auf Immer oder Nur bei Nutzung“ als allgemeine Voraussetzung.
  Für die Adresssuche gilt das nicht, was weiter unten im Dokument auch richtig erklärt wird.
- **Offline im Adressmodus:** Ohne Verbindung zeigt die Adresssuche nur den Hinweis „Keine Netzverbindung…“ und
  keine zwischengespeicherten Preise, weil keine Position ermittelt werden kann. Das ist folgerichtig und so
  dokumentiert.
- **Nicht versioniertes Verzeichnis:** Das Verzeichnis `docs/features/` (Testprotokolle aus Schritt 2) liegt
  nicht versioniert im Arbeitsverzeichnis. Laut Commit-Nachricht wurde es aus dem Repository entfernt.
  Es sollte vor einem späteren `git add -A` aufgeräumt oder ignoriert werden.
