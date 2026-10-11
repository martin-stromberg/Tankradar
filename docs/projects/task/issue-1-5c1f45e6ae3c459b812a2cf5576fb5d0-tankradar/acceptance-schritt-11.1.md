# Abnahmeprüfung – Entwicklungsschritt 11

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Fachlich ist die Startseite samt Bereich „In der Nähe“ umgesetzt. Alle Tests sind grün: `local-ci.ps1` vollständig, darunter
FlaUI-E2E 75/75. Zwei Punkte stehen der Abnahme entgegen:

- Ab der 101. Favoriten-Tankstelle zeigt die Startseite fälschlich „Keine Preise bekannt“, ohne Hinweis.
- Der Pre-Push-Hook würde den Push des Stands abbrechen.

## Abweichungen

1. **Ab der 101. Favoriten-Tankstelle fehlen die Preise, ohne Hinweis.**
   - **Anforderung:** Die Startseite zeigt „alle Favoritengruppen mit ihren Tankstellen“, und zu jeder Tankstelle erscheinen
     die Preise der gewählten Sorten.
   - **Ursache:** `FuelPriceService.NormalizeIds` kürzt mit `.Take(MaxRefreshIds)` auf 100 verschiedene Kennungen. Das gilt
     für `RefreshStationsAsync` und auch für den rein lokalen `GetKnownStationsAsync`. Die Zahl der Favoriten ist nicht
     begrenzt (Schritt 10).
   - **Folge:** Betroffen ist jede Tankstelle jenseits der ersten 100 Kennungen (Reihenfolge: Gruppen nach Namen, darin
     Priorität und Name). Sie fehlt in `FavoritesViewModel._stations`, und `HomeOverviewBuilder` baut sie als unbekannte
     Tankstelle ohne Preise auf. Die Kachel zeigt „Keine Preise bekannt“, auch wenn lokal Preise vorliegen, und die
     Tankstelle wird nie aktualisiert.
   - **Dokumentation:** Weder die Startseite noch die Hilfe (`docs/help/Startseite/`) weisen darauf hin. Nur ADR 0007 nennt
     „höchstens 100 Kennungen je Aufruf“. Kein Test deckt die Grenze ab.
   - **Umsetzung:**
     - `GetKnownStationsAsync` liest nur lokal und braucht daher keine Obergrenze.
     - Bei `RefreshStationsAsync` die Grenze entfernen. Die Last begrenzen bereits die Aufteilung in Anfragen zu je
       10 Kennungen, die Drosselung und die 5-Minuten-Frist.
     - Soll die Grenze doch bleiben, die übrigen Tankstellen mit ihren lokalen Preisen anzeigen und einen sichtbaren
       Hinweis geben.
     - Unit-Test mit mehr als 100 Kennungen ergänzen sowie Hilfe und ADR 0007 anpassen.

2. **Der Pre-Push-Hook würde den Push dieses Stands abbrechen.**
   - **Vorgabe:** Laut Schritt 2 müssen die Hooks auf dem aktuellen Stand ohne Beanstandung durchlaufen. Ausgeliefert wird
     per Push und PR nach `staging`.
   - **a) Platzhalterprüfung:** `no-notimplemented-check.py --all --strict` endet mit Exit-Code 1.
     - Es gibt 8 Funde in `src/Tankradar.Tests.Unit/Unit/HttpTileSourceTests_Fetch.cs`, Testhilfe `FailingStream`
       (Zeilen 228–260). Dort bestehen Member nur aus `throw`, etwa `Length`, `Position`, `Read`, `ReadAsync`, `Seek`,
       `SetLength` und `Write`.
     - Die Klasse ist mit den Review-Korrekturen in `396a0ff` hinzugekommen. Beim Commit meldet der Hook nur eine Warnung,
       der Fund blieb deshalb unbemerkt. Im Basisbranch gibt es ihn nicht.
     - Umsetzung: `FailingStream` ohne reine `throw`-Member formulieren, z. B. von `MemoryStream` ableiten und nur
       `ReadAsync` überschreiben, das `ValueTask.FromException<int>(new IOException(…))` bzw. `Task.FromException<int>(…)`
       liefert. Danach muss `python .githooks/no-notimplemented-check.py --all --strict` grün sein.
   - **b) Commit-Format:** `conventional-commits-check.py` scheitert am Merge-Commit `0803808`.
     - Seine Nachricht lautet „Merge branch 'task/…-tankradar' into task/…--schritt-11-startseite“.
     - Der Commit liegt im Push-Bereich `origin/task/issue-1-…-tankradar..HEAD`. Merge-Base und Remote-Stand sind beide
       `46364b2`.
     - Alle Merges im Basisbranch haben das Format `merge: …`.
     - Umsetzung: Die Nachricht vor dem Push ins Format `merge: …` bringen. Dafür den Merge neu erzeugen oder den
       Schrittbranch ohne interaktiven Rebase umschreiben. Alternativ den Schritt so in den Basisbranch übernehmen, dass
       `0803808` nicht im gepushten Bereich liegt.

## Hinweise

### Geprüfter Stand

- **Branch und Diff:**
  - Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-11-startseite`, HEAD `396a0ff`.
  - Diff gegen `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` mit Merge-Base `46364b2`: 114 Dateien,
    +6303/−226.
- **Grundlagen:**
  - `project-plan.md`, Schritt 11, und die Tabelle der Vorgehensentscheidungen.
  - Die Schrittunterlagen aus dem Verlauf (`2dadd5f`, in `396a0ff` gelöscht): `requirement.md` mit dem Zusatz des
    Projektleiters und `plan.md`.
  - `acceptance-schritt-10.md`. Die dort genannten Restpunkte stehen als Zusatz 2 im Schritt.
  - Designentwurf `startseite_favoriten_gruppen`, außerhalb des Repos entpackt.

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Startseite zeigt alle Gruppen mit ihren Tankstellen | `IFavoritesService.GetOverviewAsync` (eine Abfrage), `HomeOverviewBuilder`, Gruppenkarten (einklappbar) mit `StationTile`. Gruppen nach Namen, Tankstellen nach Priorität und Name. Eine Tankstelle in mehreren Gruppen erscheint in jeder. | Unit `FavoritesServiceTests_Overview`, `HomeOverviewBuilderTests_Content`; E2E `HomeE2ETests_Overview.TwoGroups_ShowStationsPricesDistanceAndHints`, `CollapseGroup_…`. **Ab 100 Tankstellen: Abweichung 1** |
| Name | Kachel `Favorites.Station.Name` | E2E `TwoGroups_…` |
| Entfernung nur, wenn ein Standort ermittelt werden darf | Standort nur bei `GpsUsage.AllowsLocation()` und Bedarf. Die Entfernung wird lokal berechnet (`PriceRepository.DistanceKm`). Die Position liegt nur im ViewModel und wird in `OnDisappearing` verworfen. | Unit `Load_GpsNever_NeverCallsLocationService`, `Load_GpsAllowed_ShowsDistances`, `Load_LocationUnavailable_ShowsNoDistances`; E2E `TwoGroups_…`, `GpsNever_HidesNearbyAndSendsNoRadiusRequest` (keine Entfernung) |
| Preise der gewählten Sorten in Einstellungsreihenfolge, „vor X Min.“, Amber ab 60 Minuten | `StationResultBuilder.BuildItem` (jetzt öffentlich), dieselbe Aufbereitung wie Liste und Detail. Amber am Alterstext wie in Suche und Detail. | Unit `Build_ShowsPricesInSettingsOrderWithAge`, `Build_MarksPricesFromSixtyMinutesAsStale`, `Build_UnselectedFuelTypeNotShown`; E2E `HomeE2ETests_FuelOrder`, `TwoGroups_…` (Altersformat) |
| Hinweise „Preis unbestätigt“, „Automatentankstelle“ | `StationHints` wie in der Suche | Unit `Build_HintsUnconfirmedAndAutomated`; E2E `TwoGroups_…` (Automatentankstelle) |
| Aktualisierung beim Öffnen und auf Wunsch | `RefreshStationsAsync` über `prices.php` (höchstens 10 Kennungen je Anfrage). Ohne „erzwingen“ gilt eine Frist von 5 Minuten. „Preise aktualisieren“ erzwingt den Abruf. | Unit `FuelPriceServiceTests_RefreshStations` (10), `Refresh_Command_ForcesRequest`; Integration `PriceRefreshMockServerTests_Flow` (5), `PriceRepositoryTests_Stations` (3); E2E `HomeE2ETests_Refresh` |
| Offline: letzte Preise mit Alter, Hinweis in der Kopfzeile | Ein `OfflineFallback` blendet `Favorites.OfflineBanner` ein. „Preise aktualisieren“ ist dann gesperrt. | Unit `Offline_ShowsLastKnownPricesAndBanner`; E2E `HomeE2ETests_Offline` (Neustart mit geschlossenem Port) |
| Automatische Aktualisierung nach Wiederverbindung, Hinweis entfällt | `ConnectionRestored` wirkt nur bei angezeigter Seite. Abgerufen wird nach einem Offline-Stand, bei veralteten Preisen oder bei einem Offline-Ergebnis von „In der Nähe“. Ereignisse laufen auf dem UI-Kontext. | Unit `FavoritesViewModelTests_Reconnect` (7), `FavoritesViewModelTests_Threading` (4); Integration `Refresh_ConnectionRestored_FetchesAgain`. Per FlaUI nicht prüfbar, im Plan begründet. |
| Antippen öffnet die Detailansicht | Antippen der Kachel oder „Details öffnen“, mit Schutz gegen doppeltes Öffnen | Unit `OpenStation_NavigatesWithStationAndGuardsDoubleOpen`; E2E `HomeE2ETests_OpenDetail` |
| „In der Nähe“ nur bei Schalter und erlaubter GPS-Nutzung | `NearbySectionViewModel.IsVisible`. Ohne Bedarf keine Standortabfrage. | Unit `Hidden_WhenSettingOff`, `Hidden_WhenGpsNever`, `Disabled_DoesNotRequestLocation`; E2E `NearbySwitch_ShowsSectionWithStationsByDistance`, `GpsNever_…` |
| Tankstellen um den Standort, nach Spritsorte filterbar | Umkreissuche mit 5 km, höchstens 10 Einträge nach Entfernung. Die Chips „Alle“ und je Sorte filtern ohne neuen Abruf. | Unit `Load_BuildsEntriesSortedByDistanceMaxTen`, `Filter_ByFuelType_ChangesListWithoutNewRequest`; E2E `FuelFilter_NarrowsAndRestoresEntries` |
| Nur Information, keine Favoritenaktionen | Die Kachel bietet nur „Details öffnen“. Zuordnen ist nur in der Detailansicht möglich (ADR 0006/0007). | Unit `Entries_OpenDetailAndOfferNoFavoriteActions`; E2E `OpenNearbyStation_ShowsDetail` |
| Schalter in den Einstellungen | `AppSettings.ShowNearby`, Spalte `UserSettings.ShowNearby` (Migration `AddNearbySetting`, Standard aus). Hinweis bei GPS-Nutzung „Nie“. | Unit `SettingsViewModelTests_Nearby` (6), `SettingsServiceTests_LoadSave`; Integration `NearbySettingMigrationTests_Upgrade` (Upgrade einer echten Datei vom Stand `AddFavorites`, alle Daten bleiben), `SettingsServiceTests_RestartPersistence`; E2E `SettingsE2ETests_NearbyPersistence`, `SettingsE2ETests_Defaults` |
| Standortdaten werden nicht gespeichert | Position nur im Arbeitsspeicher. Protokolle nennen nur Ausnahmetypen. | Integration `HomePrivacyTests_Persistence`: DB-Zeilen und Rohdatei ohne Position, Protokoll ohne Koordinaten, `lat=` und `prices.php`; Unit `Position_IsNeverLogged` |
| Hinweis ohne Gruppen | `FavoritesTexts.NoGroups` erklärt das Anlegen | E2E `WithoutGroups_ShowsHintOnly`, dabei keine Anfrage an den Mock |
| Zusatz 1: Gruppenansicht öffnet die Detailansicht, ADR 0006 | `FavoriteEntryItemViewModel.OpenCommand`, „Details öffnen“ und Antippen des Namens | Unit `OpenEntry_*` (3); E2E `FavoritesE2ETests_Flow.GroupView_OpenEntry_ShowsStationDetail`; ADR 0006 aktualisiert |
| Zusatz 2: Restpunkte aus Schritt 10 | `Age` kürzt die Frische. Eine `304` ohne eigene Angaben übernimmt die gespeicherte Lebensdauer (`TileMetadata.Lifetime`), Altbestand erhält den Rückfall. `SaveAsync(…, duplicateResult)` meldet `AlreadyMember` statt `DuplicateName`. | Unit `TileCachePolicyTests_Age` (6), `HttpTileSourceTests_AgeAndRevalidation` (7), `FavoritesServiceTests_Concurrency` (4); ADR 0005/0006 präzisiert |
| Windows-Zwischenstand | `review-versions/0.1.15_2026-10-11/` | siehe Praktische Verifikation |

### Bewertung der bewusst offen gelassenen Punkte

- **Stilles Kürzen auf 100 Kennungen:** relevant, siehe Abweichung 1. Die Anzeige „Keine Preise bekannt“ ist in diesem Fall
  sachlich falsch.
- **Doppelte AutomationIds bei mehreren Gruppen:** für die Anforderung nicht relevant.
  - Es ist dasselbe Muster wie bisher, etwa `Search.Station.*` und `Favorites.Group.*` aus Schritt 10.
  - Die gleichen IDs liegen in verschiedenen Elternelementen (je Gruppenkarte bzw. Kachel). UI Automation verlangt
    Eindeutigkeit nur unter Geschwistern.
  - Bildschirmleser lesen Name und Beschreibung, nicht die AutomationId.
  - Die E2E-Tests unterscheiden über die Reihenfolge (`FindAllOrdered`) und waren in zwei Läufen grün.
  - Optional: eine Container-ID je Gruppe (z. B. `Favorites.Group.<Id>`), um Prüfungen auf eine Gruppe einzugrenzen.
- **Unbegrenzte Caches in `FuelPriceService`:** für die Anforderung nicht relevant.
  - `_refreshed` wächst nur mit der Zahl der Favoriten.
  - `_searchCache` gibt es seit Schritt 5/6. Er behält je Umkreissuche einen Schlüssel aus Koordinaten, gerundet auf drei
    Nachkommastellen (≈ 110 m), bis die App endet. Durch „In der Nähe“ kommt bei jedem Erscheinen der Startseite an einem
    neuen Ort ein Eintrag hinzu.
  - Das ist kein Speichern im Sinne der Anforderung, denn nichts wird dauerhaft abgelegt. Es passt aber nicht ganz zur
    Aussage in Hilfe und ADR 0007, der Standort werde beim Verlassen der Seite verworfen.
  - Empfehlung: Einträge entfernen, die älter als `CacheLifetime` sind. Das begrenzt den Speicher und entfernt auch die
    Ortsspuren.
- **Größe von `FavoritesViewModel`:** Mit rund 580 Zeilen vereint es Startseite, Gruppenanlage und Wiederverbindung. Das
  betrifft nur die Wartbarkeit. Bei Gelegenheit die Gruppenanlage in ein eigenes ViewModel auslagern.
- **Kein erneutes Code-Review nach den Korrekturen:** Diese Prüfung hat die geänderten Teile gezielt durchgesehen:
  - Startseite, „In der Nähe“, Preisabruf nach Kennungen
  - Repository, Einstellungen, Migration
  - Kachel-Cache, Favoritendienst
  - Tests

  Dabei fiel Abweichung 2a auf, die mit den Review-Korrekturen entstanden ist. Sonst ergaben sich keine Fehler in Logik
  oder Threading. Die Ereignisse von Verbindung und Favoritendienst werden auf den UI-Kontext umgeleitet.

### Umgehung des Hooks `csproj-xmldoc-check.py`

Akzeptabel.

- **Fehlalarm des Hooks:** Die Vollständigkeitsprüfung arbeitet mit regulären Ausdrücken. Sie hält Eigenschaften mit
  Ausdruckskörper und Lambda für Methoden. Eine Gegenprobe mit der Hook-Funktion bestätigt das:
  `public bool HasStalePrice => PriceLines.Any(line => line.IsStale);` meldet fehlende `<param>`- und `<returns>`-Tags.
- **Ausweichform:** Die Schreibweise `get { return …; }` gilt als korrekt und ist im Code schon seit Schritt 10 üblich
  (`FavoriteEntryItemViewModel`).
- **Geprüfter Endstand:**
  - `csproj-xmldoc-check.py --all` ist grün (373 Dateien).
  - Der Build mit `TreatWarningsAsErrors` erzwingt CS1591.
  - In den geänderten Dateien wurde kein Dokumentationskommentar entfernt oder durch Kommentarzeilen von seiner
    Deklaration getrennt.
  - `--no-verify` wurde nicht verwendet.
- **Einschränkung:** Welche zwei Testdateien umgeschrieben wurden, lässt sich im Verlauf nicht mehr erkennen, weil das vor
  dem Commit geschah. Geprüft wurde deshalb der Endstand.
- **Empfehlung:** den Fehlalarm im Hook beheben.

Andere Hook-Prüfungen schlagen dagegen fehl, siehe Abweichung 2.

### Designentwurf

- **In ADR 0007 begründet:**
  - Tages-Empfehlung, Routenknöpfe, Strom/Laden (ADR 0001)
  - Marken-Logo, Badges „Tiefstpreis“ und „Nah“, Mindestpreis je Gruppe
  - Sortieren sowie Spritsorten-Filter nur in „In der Nähe“
- **Nicht in ADR 0007 erwähnt** (Kleinigkeiten, bitte nachtragen):
  - Kopfzeile „Vor X Min. aktualisiert (Quelle)“. Der Plan sah ausdrücklich vor, sie zu dokumentieren.
  - Gruppensymbole und die Zwischenüberschrift „Meine Favoriten · N Gruppen“.
  - Im Bereich „In der Nähe“: Öffnungsstatus („Geöffnet bis …“), Spar-Zeile („Günstigste in 3 km …“, „Bester Preis“) und
    der Schalter direkt im Bereich. Die Anforderung verlangt den Schalter in „Optionen“.
  - Lesezeichen-Knopf je Eintrag. Er entfällt laut Anforderung.
  - Lage des Bereichs vor „Neue Gruppe“.

### Barrierefreiheit (Kleinigkeiten)

- Der Umschalter „Gruppe ein-/ausklappen“ nennt in seiner Beschreibung weder den Gruppennamen noch den Zustand. Bei
  mehreren Gruppen hört man daher mehrmals dasselbe.
- Die Hilfe (`docs/help/Startseite/ablauf-anwender.md`) sagt, Kacheln seien per Tabulator erreichbar. Erreichbar sind nur
  die Schaltflächen, nicht die Kachel selbst. Die Kachelbeschreibung nennt die Hinweise nicht.

### Praktische Verifikation

- **`scripts/local-ci.ps1`:** einmal vollständig, 08:04–08:09.
  - Lauf mit E2E off-screen und Sicherheitsprüfung.
  - `FUEL_PRICE_API_KEY` und `TANKRADAR_FUEL_PRICE_API_KEY` wurden für den Kindprozess per `env -u` entfernt, ohne sie zu
    lesen.

  | Prüfung | Ergebnis |
  |---|---|
  | Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
  | Restore, Format, Sicherheitsprüfung („Keine anfaelligen Pakete“) | OK |
  | Build mit Warnungen als Fehler | OK |
  | Unit | 803/803 |
  | Integration | 109/109 |
  | Zeilenabdeckung | 92,5 % (Schwelle 70 %) |
  | **FlaUI-E2E** | **75/75** in 4 min 3 s, auch `DiagnosticsCaptureE2ETests` (Bildschirm an) |
  | iOS-Compile-Prüfung | OK |

- **Zweiter E2E-Lauf** (Startseite, Favoriten, Einstellungen „In der Nähe“/Standardwerte, off-screen): 22/22 in 1 min 12 s.
  Alle 14 neuen E2E-Tests waren in beiden Läufen grün.
- **Vollständiger Neubau:** `dotnet build Tankradar.sln -c Release --no-incremental -p:TreatWarningsAsErrors=true` mit
  Windows-Ziel ergab 0 Warnungen und 0 Fehler.
- **Hook-Prüfungen**, wie sie der Pre-Push ausführen würde:

  | Prüfung | Ergebnis |
  |---|---|
  | `forbidden-patterns-check.py --all --strict` | OK |
  | `format-code-style-check.py --strict` | OK |
  | `csproj-xmldoc-check.py --all` | OK (373 Dateien) |
  | `translation-check.py --all` | OK |
  | `enum-coverage-check.py --all --strict` | OK |
  | **`no-notimplemented-check.py --all --strict`** | **Fehler** (`FailingStream`, Abweichung 2a) |
  | **`conventional-commits-check.py --base 46364b2 --end HEAD`** | **Fehler** (`0803808`, Abweichung 2b) |

- **Zwischenstand `review-versions/0.1.15_2026-10-11/`:**
  - Er ist vorhanden und per `.gitignore` ausgeschlossen.
  - `FileVersion` ist `0.1.15.0`, `ProductVersion` ist `0.1.0+396a0ff…`. Die Binärdateien sind von 08:01–08:02 und
    entsprechen damit HEAD.
  - Startfähigkeit: Die App lief off-screen mit temporärem `TANKATLAS_TEST_DATA_PATH` an:
    - Nach 15 s reagierte der Prozess, das Fenster hieß „Tankatlas“.
    - Die Datenbank wurde angelegt, einschließlich Migration `20261006124841_AddNearbySetting` und Spalte `ShowNearby`.
    - Ohne Testadressen fand kein Abruf statt.
    - Danach wurde der Prozess gezielt per PID beendet und das Verzeichnis entfernt.
  - **`CHANGELOG.md` ist doppelt kodiert.** Statt Umlauten und Gedankenstrichen steht dort z. B. „NÃ¤he“ bzw. „â€”“.
    - Ursache: `scripts/create-review-version.ps1` liest `-ChangelogFile` mit `Get-Content -Raw` ohne `-Encoding`. Unter
      Windows PowerShell 5.1 wird UTF-8 ohne BOM dabei als ANSI gelesen.
    - Frühere Stände umgingen das durch Umschreibungen wie „ae“ und „ue“.
    - Empfehlung: den Changelog bei der Nachbesserung neu erzeugen und das Skript mit `-Encoding UTF8` lesen lassen.

### Datenschutz

- **Standort:**
  - Die Position wird nur bei erlaubter GPS-Nutzung und bei Bedarf ermittelt, also wenn Favoriten vorhanden sind oder
    „In der Nähe“ aktiv ist.
  - Sie liegt nur im `FavoritesViewModel` und wird beim Verlassen der Seite verworfen. An „In der Nähe“ wird nur das
    Ergebnis übergeben.
  - Gespeichert werden weder Position noch Entfernung. `StationEntity` hat keine Entfernungsspalte.
  - Protokolle nennen nur Ausnahmetypen und `PriceFailure`.
  - Zum Suchcache siehe oben.
- **Keine Produktivendpunkte in Tests:**
  - Die neuen Unit- und Integrationstests nutzen `StubTankerkoenigClient`, `FakeHttpMessageHandler` und den
    `MockTankerkoenigServer` auf Loopback, der jetzt auch `prices.php` bedient.
  - Die URL des echten Kachelservers kommt nur als erwarteter Wert in `HttpTileSourceTests_Fetch` vor, mit Fake-Handler und
    ohne Netzzugriff.
  - Die E2E-Tests laufen über die Testumgebung gegen die Mock-Server bzw. einen geschlossenen Port.
  - Im Testmodus ohne Adresse findet kein Abruf statt; das hat der Start des Zwischenstands bestätigt.
  - Tankerkönig, Nominatim und `tile.openstreetmap.org` wurden nicht aufgerufen.
- **API-Schlüssel:**
  - Der Diff enthält nur synthetische UUIDs und den Mock-Schlüssel.
  - Die neuen Protokollaufrufe geben keine URL aus (`prices.php` fehlt laut Datenschutztest im Protokoll).
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben.

### Sonstiges

- Im Arbeitsverzeichnis liegt weiterhin das unversionierte Verzeichnis `docs/features/` mit Altlasten aus Schritt 2. Es
  gehört nicht zu diesem Schritt.
- Es gab keine Commits, keine Pushes und keine Codeänderungen. Nur dieser Bericht wurde angelegt.
- Der einzige selbst gestartete `Tankradar.MAUI`-Prozess (Start des Zwischenstands) wurde per PID beendet. Danach lief
  kein `Tankradar.MAUI`-Prozess mehr.
