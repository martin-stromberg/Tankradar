# Abnahmeprüfung – Entwicklungsschritt 11 (nach Nachbesserungsrunde 1)

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Beide Abweichungen aus `acceptance-schritt-11.1.md` sind behoben:

- Die Startseite zeigt die Preise aller Favoriten-Tankstellen, auch über 100 hinaus.
- Der Pre-Push-Hook läuft auf dem Stand durch.

Die Nachbesserung hat keine Regression erzeugt.

- **`local-ci.ps1`:** vollständig grün bis auf den bekannten Umgebungseffekt von `DiagnosticsCaptureE2ETests` („Screenshot
  einfarbig“ bei ausgeschaltetem Bildschirm). Alle 74 übrigen E2E-Tests sind grün, darunter alle 16 Tests zu Startseite,
  „In der Nähe“ und Einstellungen.
- **Offene Hinweise, nicht blockierend:**
  - Eine Formulierung zum Datenschutz in der Anwenderhilfe ist ungenau.
  - Zwei sporadisch scheiternde Integrationstests stammen aus früheren Schritten.

## Abweichungen

Keine.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-11-startseite`, HEAD `bd758ee`.
- **Diff:** gegen `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` (Merge-Base und `origin` beide `46364b2`):
  116 Dateien, +6440/−227.
  - Davon Nachbesserung `a56a24e..bd758ee`: 14 Dateien, +185/−49, in drei Commits (`ca32262`, `1ef9c24`, `bd758ee`).
- **Grundlagen:**
  - `project-plan.md`, Schritt 11 und Schritt 2 (Hooks)
  - `requirement.md` („Standort-Datenschutz“)
  - `acceptance-schritt-10.md` (Format, Restpunkte)
  - `acceptance-schritt-11.1.md`

### Abweichungen aus der ersten Abnahme (11.1)

| Nr. | Abweichung 11.1 | Behoben | Nachweis |
|---|---|---|---|
| 1 | Ab der 101. Favoriten-Tankstelle fehlen die Preise ohne Hinweis (`NormalizeIds` kürzte mit `.Take(100)`) | **ja** | Siehe unten. |
| 2a | `no-notimplemented-check.py --all --strict` scheitert an `FailingStream` | **ja** | Siehe unten. |
| 2b | `conventional-commits-check.py` scheitert am Merge-Commit `0803808` („Merge branch …“) | **ja** | Siehe unten. |

**Zu Abweichung 1 (100 Kennungen):**

- **Code:** `MaxRefreshIds` und `.Take(MaxRefreshIds)` sind entfernt. Das betrifft `RefreshStationsAsync` und
  `GetKnownStationsAsync`.
- **Tests:** Neue Unit-Tests `FuelPriceServiceTests_ManyStations` mit 135 Kennungen gegen echtes SQLite (In-Memory):
  - `GetKnownStationsAsync_MoreThanHundredIds_ReturnsAll` liefert alle 135, ohne Abruf.
  - `RefreshStationsAsync_MoreThanHundredIds_FetchesAllInChunksOfTen` sendet 14 Anfragen (13 × 10 + 5) und liefert alle
    135. Die 135. Tankstelle trägt den frisch abgerufenen Preis.
- **Dokumentation:** ADR 0007, `docs/help/Startseite/ablauf-technisch.md` und `docs/help/Preisdaten/ablauf-technisch.md`
  nennen keine Obergrenze mehr. Eine Suche nach „höchstens 100“ bzw. `MaxRefreshIds` findet keinen Rest.
- **Last:** Die Abfrage `ids.Contains(...)` läuft unter EF Core 10 mit SQLite. 135 Kennungen sind im Test nachgewiesen.
  Die Last begrenzen weiterhin die Pakete zu 10 Kennungen, die Drosselung und die Frist von fünf Minuten.

**Zu Abweichung 2a (`FailingStream`):**

- `FailingStream` erbt jetzt von `MemoryStream`. Er überschreibt nur noch `ReadAsync(Memory<byte>)` mit
  `ValueTask.FromException` und `CopyToAsync` mit `Task.FromException`.
- `no-notimplemented-check.py --all --strict` meldet OK (370 Dateien). `GetTile_StreamReadFails_ReturnsNull` ist grün.

**Zu Abweichung 2b (Merge-Commit):**

- Der Merge heißt jetzt `0209f09 merge: Basisbranch in Schritt 11 uebernommen` mit den Eltern `fb1161a` und `46364b2`.
- Die Bäume aller neu aufgebauten Commits stimmen mit der alten Historie überein (verglichen per Tree-Hash):
  - `0209f09` = `0803808`
  - `5651ea5` = `64b8f60`
  - `d68a7f8` = `bed4e78`
  - `a56a24e` = `396a0ff`
- `conventional-commits-check.py` meldet OK für den Push-Bereich `46364b2..bd758ee` (9 Commits) und für `main..HEAD`
  (103 Commits).

**Übrige Punkte aus den Hinweisen von 11.1:**

| Punkt | Stand |
|---|---|
| Suchcache behält Ortsschlüssel bis zum Ende der App | `PruneSearchCache` entfernt abgelaufene Einträge bei jedem Aufruf von `SearchNearbyAsync`, `RefreshStationsAsync` und `GetKnownStationsAsync`. `TryRemove(KeyValuePair)` entfernt nur unveränderte Einträge und ist damit threadsicher. Test: `SearchCache_ExpiredEntries_AreRemoved`. Zur Aussage in der Hilfe siehe unten. |
| Umschalter „Gruppe ein-/ausklappen“ ohne Gruppenname und Zustand | `HomeGroupViewModel.ToggleDescription` liefert z. B. „Gruppe ein-/ausklappen: Arbeitsweg, aufgeklappt“, gebunden an `SemanticProperties.Description` und aktualisiert bei `IsExpanded`. Test: `Load_ToggleDescription_NamesGroupAndState`. Die E2E-Tests finden den Umschalter weiterhin über die AutomationId. |
| Hilfe zur Tabulator-Erreichbarkeit | `ablauf-anwender.md` korrigiert: Erreichbar sind die Schaltflächen und Chips, nicht die Kachel. Die Kachelbeschreibung nennt die Hinweise nicht. |
| Designabweichungen fehlten in ADR 0007 | Nachgetragen: Kopfzeile „Vor X Min. aktualisiert (Quelle)“, Gruppensymbole und Zwischenüberschrift, Öffnungsstatus und Spar-Zeile, Schalter in „Optionen“ statt im Bereich, Lesezeichen-Knopf, Lage des Bereichs. Die Lage „unter den Gruppen, vor ‚Neue Gruppe‘“ entspricht `FavoritesPage.xaml`. |
| `CHANGELOG.md` des Zwischenstands doppelt kodiert | `create-review-version.ps1` liest mit `-Encoding UTF8`. Der neue `review-versions/0.1.15_2026-10-11/CHANGELOG.md` enthält keine Zeichenfolgen „Ã“ bzw. „â€““ mehr, die Umlaute sind korrekt. |
| `FavoritesViewModel` groß, Fehlalarm in `csproj-xmldoc-check.py`, doppelte AutomationIds | Unverändert. Betrifft nur die Wartbarkeit bzw. den Hook, siehe 11.1. |

### Anforderung gegen Umsetzung

Die Nachbesserung ändert an der fachlichen Umsetzung nur den Preisabruf für mehr als 100 Kennungen und die Beschreibung
des Umschalters. Die Zuordnung aus 11.1 gilt weiter. Alle genannten Tests waren in diesem Lauf grün.

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Startseite zeigt alle Gruppen mit ihren Tankstellen | `GetOverviewAsync`, `HomeOverviewBuilder`, einklappbare Gruppenkarten mit `StationTile`. Jetzt ohne Obergrenze der Tankstellenzahl. | Unit `FavoritesServiceTests_Overview`, `HomeOverviewBuilderTests_Content`, `FuelPriceServiceTests_ManyStations`; E2E `HomeE2ETests_Overview.TwoGroups_…`, `CollapseGroup_…` |
| Name | Kachel `Favorites.Station.Name` | E2E `TwoGroups_…` |
| Entfernung nur bei erlaubter Standortermittlung | Standort nur bei `GpsUsage.AllowsLocation()` und Bedarf, Entfernung lokal berechnet | Unit `Load_GpsNever_NeverCallsLocationService`, `Load_GpsAllowed_ShowsDistances`, `Load_LocationUnavailable_ShowsNoDistances`; E2E `GpsNever_HidesNearbyAndSendsNoRadiusRequest` |
| Preise der gewählten Sorten in Einstellungsreihenfolge, „vor X Min.“, Amber ab 60 Minuten | `StationResultBuilder.BuildItem` | Unit `Build_ShowsPricesInSettingsOrderWithAge`, `Build_MarksPricesFromSixtyMinutesAsStale`; E2E `HomeE2ETests_FuelOrder` |
| Hinweise „Preis unbestätigt“, „Automatentankstelle“ | `StationHints` | Unit `Build_HintsUnconfirmedAndAutomated`; E2E `TwoGroups_…` |
| Aktualisierung beim Öffnen und auf Wunsch | `RefreshStationsAsync` über `prices.php` in Paketen zu 10, Frist von 5 Minuten, „Preise aktualisieren“ erzwingt den Abruf | Unit `FuelPriceServiceTests_RefreshStations`, `Refresh_Command_ForcesRequest`; Integration `PriceRefreshMockServerTests_Flow`; E2E `HomeE2ETests_Refresh` |
| Offline: letzte Preise mit Alter und Hinweis in der Kopfzeile | `OfflineFallback` blendet `Favorites.OfflineBanner` ein | Unit `Offline_ShowsLastKnownPricesAndBanner`; E2E `HomeE2ETests_Offline` |
| Automatische Aktualisierung nach Wiederverbindung, Hinweis entfällt | `ConnectionRestored` nur bei angezeigter Seite, auf dem UI-Kontext | Unit `FavoritesViewModelTests_Reconnect`, `_Threading`; Integration `Refresh_ConnectionRestored_FetchesAgain` |
| Antippen öffnet die Detailansicht | Kachel bzw. „Details öffnen“, mit Schutz gegen doppeltes Öffnen | Unit `OpenStation_…`; E2E `HomeE2ETests_OpenDetail` |
| „In der Nähe“ nur bei aktivem Schalter und erlaubter GPS-Nutzung, nach Sorte filterbar, rein informativ | `NearbySectionViewModel`: 5 km, höchstens 10 Einträge, Chips „Alle“ und je Sorte, nur „Details öffnen“ | Unit `Hidden_When…`, `Filter_ByFuelType_…`, `Entries_OpenDetailAndOfferNoFavoriteActions`; E2E `HomeE2ETests_Nearby` (3), `HomeE2ETests_NearbyGpsNever` |
| Schalter in den Einstellungen | `AppSettings.ShowNearby`, Migration `AddNearbySetting` | Unit `SettingsViewModelTests_Nearby`; Integration `NearbySettingMigrationTests_Upgrade`; E2E `SettingsE2ETests_NearbyPersistence` (2), `SettingsE2ETests_Defaults` |
| Standortdaten werden nicht gespeichert | Position nur im Arbeitsspeicher. Protokolle nennen nur Ausnahmetypen. Der Suchcache verliert abgelaufene Ortsschlüssel. | Integration `HomePrivacyTests_Persistence`; Unit `Position_IsNeverLogged`, `SearchCache_ExpiredEntries_AreRemoved` |
| Hinweis ohne Gruppen | `FavoritesTexts.NoGroups` | E2E `WithoutGroups_ShowsHintOnly` |
| Zusatz 1: Gruppenansicht öffnet die Detailansicht | `FavoriteEntryItemViewModel.OpenCommand` | E2E `FavoritesE2ETests_Flow.GroupView_OpenEntry_ShowsStationDetail` |
| Zusatz 2: Restpunkte aus Schritt 10 | `Age`, gespeicherte Lebensdauer bei `304`, `AlreadyMember` | Unit `TileCachePolicyTests_Age`, `HttpTileSourceTests_AgeAndRevalidation`, `FavoritesServiceTests_Concurrency` |
| Tests: Aufbereitung der Gruppenübersicht, Wiederverbindung, FlaUI für Gruppen, Preise und „In der Nähe“ | siehe oben | grün |
| Windows-Zwischenstand | `review-versions/0.1.15_2026-10-11/` | siehe Praktische Verifikation |

### Bewertung: Suchcache und Datenschutzaussage

Die Einträge werden nicht beim Verlassen der Seite entfernt. Abgelaufene Einträge (`CacheLifetime`, 5 Minuten) entfernt
erst der nächste Aufruf von `SearchNearbyAsync`, `RefreshStationsAsync` oder `GetKnownStationsAsync`.

- **ADR 0007 und `docs/help/Startseite/ablauf-technisch.md`:** konsistent. Sie beschreiben genau dieses Verhalten („bei
  jeder weiteren Anfrage des Preisdienstes“ bzw. „bei jedem Aufruf von …“).
- **Anwenderhilfe `docs/help/Startseite/beschreibung.md`:** nicht ganz konsistent. Dort steht, Tankatlas merke sich den
  Ort „höchstens fünf Minuten lang …; danach wird dieser Merker entfernt“.
  - Nach fünf Minuten ist der Eintrag zwar wirkungslos. Er bleibt aber bis zum nächsten Aufruf des Preisdienstes im
    Arbeitsspeicher, notfalls bis zum Ende der App.
  - Beispiel: Ohne Favoriten und mit aktivem „In der Nähe“ öffnet der Anwender die Startseite und arbeitet dann eine Stunde
    im Tankbuch.
  - `FavoritesViewModel` ruft `GetKnownStationsAsync` bei leerer Favoritenliste gar nicht auf, und `RefreshStationsAsync`
    kehrt bei leerer Liste vor dem Aufräumen zurück. Der Eintrag bleibt deshalb diese Stunde lang bestehen.
- **Bewertung:** Keine Abweichung.
  - Die Anforderung („Standortdaten werden nicht gespeichert“, „Keine persistenten GPS-Rohdaten“) ist erfüllt. Der
    Schlüssel ist auf etwa 110 m gerundet, liegt nur im Arbeitsspeicher und wird nie gespeichert, protokolliert oder
    übertragen.
  - Gegenüber 11.1 ist der Stand deutlich besser.
- **Empfehlung:** den Satz in `beschreibung.md` angleichen, etwa: „… gilt höchstens fünf Minuten und wird danach bei der
  nächsten Preisabfrage, spätestens beim Beenden der App, verworfen“. Alternativ den Cache in `OnDisappearing` bzw. per
  Zeitgeber aufräumen.

### Praktische Verifikation

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei `git push origin HEAD:task/…-tankradar`: lokal `bd758ee`, Remote-Stand `46364b2`.
  - `FUEL_PRICE_API_KEY` und `TANKRADAR_FUEL_PRICE_API_KEY` wurden für den Kindprozess per `env -u` entfernt, ohne sie zu
    lesen.

| Prüfung | 1. Lauf | 2. Lauf |
|---|---|---|
| Branch-Schutz | OK | OK |
| `conventional-commits-check.py --base 46364b2 --end bd758ee` | OK (9 Commits) | OK |
| `forbidden-patterns-check.py --all --strict` | OK (568 Dateien) | OK |
| `format-code-style-check.py --strict` | OK | OK |
| `csproj-xmldoc-check.py --all` | OK (374 Dateien) | OK |
| `translation-check.py --all` | OK (keine `.resx`) | OK |
| `enum-coverage-check.py --all --strict` | OK | OK |
| `no-notimplemented-check.py --all --strict` | OK (370 Dateien) | OK |
| `test-execution-check.py` (Build, Unit, Integration, ohne E2E) | **Fehler**: Integration 108/109, `GeocodingMockServerTests_Flow.Resolve_TwoQuickRequests_AreAtLeastOneSecondApart` | OK: Unit 807/807, Integration 109/109 |
| Gesamt | Exit-Code 1 | **Exit-Code 0**, „Pre-Push-Prüfungen erfolgreich abgeschlossen“ |

Zum Fehler im ersten Lauf siehe „Sporadische Integrationstests“.

**`scripts/local-ci.ps1`**

- Einmal vollständig von 11:48 bis 11:58, E2E off-screen, mit Sicherheitsprüfung, ohne die beiden Schlüsselvariablen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK |
| Unit | 807/807 (vorher 803, neu: 3 × `ManyStations`, 1 × `ToggleDescription`) |
| Integration | 109/109 |
| Zeilenabdeckung | 92,8 % (Schwelle 70 %) |
| FlaUI-E2E | 74/75 in 8 min 1 s. Gescheitert ist nur `DiagnosticsCaptureE2ETests.FailingTestBodyProducesScreenshotUiTreeAndErrorFile` („Der Screenshot zeigt keinen Fensterinhalt (einfarbig)“). |
| iOS-Compile-Prüfung | OK |

- **Schrittrelevante E2E-Tests:** Alle 16 waren grün:
  - `HomeE2ETests_*` (11)
  - `SettingsE2ETests_NearbyPersistence` (2)
  - `SettingsE2ETests_Defaults` (2)
  - `FavoritesE2ETests_Flow.GroupView_OpenEntry_ShowsStationDetail`

**E2E-Fehlschlag „Screenshot einfarbig“**

Bewertung: bekannter Umgebungseffekt wie in Schritt 10.

- **Einzelwiederholung:** `dotnet test src/Tankradar.Tests.E2E -c Release --no-build --filter DiagnosticsCaptureE2ETests`
  ergab 1/2. Gescheitert ist derselbe Test mit derselben Meldung.
- **Bildschirm:** sehr wahrscheinlich aus.
  - Die letzte Benutzereingabe lag rund 3 960 s zurück, der Bildschirm schaltet am Netzteil nach 900 s ab
    (`VIDEOIDLE` = `0x384`).
  - `LogonUI.exe` lief nicht, die Sitzung war also nicht gesperrt.
- **Code unverändert:** Der Schritt ändert weder den Test noch `E2EDiagnostics` noch `OffscreenWindow`. Sie wurden zuletzt
  in `c72adbe` (Schritt 6a) geändert.
- **Laufzeit:** Die übrigen 74 Tests brauchen kein gerendertes Bild und sind grün. Die längere Laufzeit (8 statt 4 Minuten
  in 11.1) verteilt sich gleichmäßig auf alle Tests (Summe 481 s, längster Test 20 s), ohne Ausreißer.
- **Empfehlung wie in Schritt 10:** vor dem Push bzw. PR den Test einmal bei eingeschaltetem Bildschirm wiederholen. Die
  PR-CI nach `staging` führt E2E ohnehin blockierend aus.

**Sporadische Integrationstests (nicht schrittbedingt)**

- **Häufigkeit:** In 20 vollständigen Läufen der Integrationssuite auf HEAD scheiterten 2, jeweils mit einem anderen Test.
  - Gezählt sind 2 Hook-Läufe, `local-ci` sowie 5 + 12 Einzelläufe (Debug).
  - Dazu kommen 3 isolierte Läufe der Geokodierungstests, alle grün.
- **`GeocodingMockServerTests_Flow.Resolve_TwoQuickRequests_AreAtLeastOneSecondApart`** (Schritt 7, unverändert):
  - Der Test misst den Abstand der Anfragen in Echtzeit auf der Serverseite des Mocks und verlangt mindestens 950 ms.
  - Unter der Last parallel laufender Testklassen kann der gemessene Abstand kürzer ausfallen.
  - Er scheiterte im ersten Hook-Lauf, isoliert war er 3 von 3 Mal grün.
- **`FavoritesMigrationTests_Upgrade.Database_CascadesGroupDeleteAndProtectsStations`** (Schritt 10, unverändert):
  - Der Test scheiterte mit `ObjectDisposedException: SQLitePCL.sqlite3` während einer aktiven Abfrage.
  - Wahrscheinliche Ursache: Das globale `SqliteConnection.ClearAllPools()` läuft parallel in anderen Testklassen. Es steht
    in `TestDatabase.Dispose` und in einzelnen Tests.
  - Der Schritt fügt drei weitere Aufrufe nach diesem Muster hinzu (`HomePrivacyTests_Persistence` 2×,
    `NearbySettingMigrationTests_Upgrade` 1×). Das kann die Häufigkeit leicht erhöhen.
- **Vergleich mit dem Basisstand:** nicht möglich. Eine Kopie von `46364b2` im Scratchpad ließ sich nicht bauen
  (`XamlCompiler.exe`, vermutlich wegen der Pfadlänge).
- **Bewertung:** Kein Fehler der Produktlogik und keine Abweichung dieses Schritts. Das Auslieferungs-Gate kann dadurch
  aber gelegentlich ohne Produktfehler rot werden.
- **Empfehlung (eigener kleiner Schritt):**
  - Integrationstests mit Datenbank in eine nicht parallele Collection legen oder `Pooling=False` statt `ClearAllPools`
    verwenden.
  - Den Abstand im Drosselungstest auf Clientseite messen bzw. über einen `TimeProvider` prüfen.

**Zwischenstand `review-versions/0.1.15_2026-10-11/`**

- Er ist neu erzeugt (08:46) und per `.gitignore` ausgeschlossen. `FileVersion` ist `0.1.15.0`, `ProductVersion` ist
  `0.1.0+bd758eef…` und entspricht damit HEAD.
- `CHANGELOG.md` ist korrekt UTF-8 und beschreibt den Schritt und die Nachbesserung.
- **Startfähigkeit:** Der Zwischenstand lief off-screen mit temporärem `TANKATLAS_TEST_DATA_PATH` im Scratchpad an:
  - Nach 15 s reagierte der Prozess, das Fenster hieß „Tankatlas“.
  - `tankatlas.db` wurde angelegt, einschließlich Migration `20261006124841_AddNearbySetting` und Spalte `ShowNearby`.
  - Es gab 0 Tankstellen, also keinen Abruf ohne Testadressen.
  - Der Prozess (PID 8784) wurde gezielt beendet. Danach lief kein `Tankradar.MAUI`-Prozess mehr.

### Datenschutz

- **Standort:**
  - Er wird nur bei erlaubter GPS-Nutzung und bei Bedarf ermittelt und liegt nur in `FavoritesViewModel._position`. In
    `OnDisappearing` wird er auf `null` gesetzt.
  - Gespeichert werden weder Position noch Entfernung (Integration `HomePrivacyTests_Persistence`: DB-Zeilen und Rohdatei).
  - Alle neuen Protokollaufrufe im Diff nennen nur `{ExceptionType}` bzw. `{Failure}`, keine Koordinaten und keine URL.
  - Der Suchcache hält nur gerundete Schlüssel im Arbeitsspeicher und verliert sie jetzt nach Ablauf (siehe oben).
- **Keine Produktivendpunkte in Tests:**
  - Die neuen Unit-Tests nutzen `StubTankerkoenigClient` und SQLite im Arbeitsspeicher.
  - Integration und E2E laufen gegen `MockTankerkoenigServer`, den Nominatim- und den Kachel-Mock auf Loopback bzw. gegen
    einen geschlossenen Port.
  - `creativecommons.tankerkoenig.de` kommt nur als Lizenztext in der Mock-Antwort vor.
  - Tankerkönig, Nominatim und `tile.openstreetmap.org` wurden nicht aufgerufen.
- **API-Schlüssel:**
  - Der Diff enthält nur synthetische UUIDs (`11111111-…`, `22222222-…`, `99999999-…`, `0000000x-…`).
  - `forbidden-patterns-check.py --all --strict` ist grün.
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben.

### Kleinigkeiten ohne Einfluss auf die Erfüllung

- **`SearchCache_ExpiredEntries_AreRemoved`:** liest das private Feld `_searchCache` per Reflection. Das ist bei
  Umbenennungen fehleranfällig. Prüfbar wäre es auch über das Verhalten, z. B. Anfrage nach Ablauf ⇒ neuer Abruf.
- **`GetTile_StreamReadFails_ReturnsNull`:** prüft nur das Ergebnis `null`. Das liefert auch ein leerer Strom über „keine
  PNG“. Der Test kann den Lesefehler daher nicht vom Formatfehler unterscheiden. So war es schon vor der Nachbesserung.
- **`ablauf-technisch.md`:** beschreibt das Aufräumen des Suchcaches im Abschnitt „5. Verlassen der Seite“. Der Inhalt
  stimmt, er gehört aber eher zum Preisabruf.

### Sonstiges

- Im Arbeitsverzeichnis liegt weiterhin das unversionierte Verzeichnis `docs/features/` mit Altlasten aus Schritt 2. Es
  gehört nicht zu diesem Schritt.
- Es gab keine Commits, keine Pushes und keine Codeänderungen.
  - Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert.
  - Testergebnisse liegen in den per `.gitignore` ausgeschlossenen Ordnern `TestResults/` und `coverage-report/`.
