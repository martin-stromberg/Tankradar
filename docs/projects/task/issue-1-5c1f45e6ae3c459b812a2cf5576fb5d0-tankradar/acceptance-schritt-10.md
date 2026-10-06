# Abnahmeprüfung – Entwicklungsschritt 10

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

### Geprüfter Stand

- Branch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-10-favoriten`, HEAD `769d0b9`.
- Geprüft wurde der Diff gegen `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` (90 Dateien) und der Designentwurf
  (`tankstellen_details_favoriten`, `startseite_favoriten_gruppen`; außerhalb des Repos entpackt).

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Selbst benannte Gruppen, lokal gespeichert, offline | Tabellen `FavoriteGroups`/`FavoriteEntries` in der einzigen SQLite-Datenbank, `FavoritesService` (EF Core) ohne Netzzugriff | Unit `FavoritesServiceTests_*` (`*_SurviveRestart`), Integration `Favorites_SurviveRestartOnRealDatabaseFile`, E2E `Favorites_SurviveApplicationRestart` |
| Mehrfachzuordnung | eindeutiger Index `(GroupId, StationId)`: mehrere Gruppen je Tankstelle erlaubt, je Gruppe nur einmal | Unit `AddStation_MultipleGroups_AllowedDuplicateInGroupRejected` |
| „Zu Favoriten hinzufügen“ in der Detailansicht mit Auswahl einer bestehenden oder neuen Gruppe | Karte „Favoritengruppen“ (`StationFavoritesViewModel`): Auswahl nur der noch nicht zugeordneten Gruppen; „Anlegen und hinzufügen“ in einem Schritt (`AddStationToNewGroupAsync`, alles oder nichts) | Unit `StationFavoritesViewModelTests_Flow`, `AddStationToNewGroup_Failure_StoresNothing`; E2E Flow |
| „Aus Favoriten entfernen“ nur bei Zuordnung, bei mehreren Gruppen mit Auswahl | bei einer Gruppe sofort, bei mehreren Mehrfachauswahl mit „Entfernen“ | Unit `RemoveStation_FromOneGroup_KeepsOthers`, `RemoveStation_FromSeveralGroups_RemovesAllChosen`; E2E `MultipleGroups_RemoveAsksWhichGroup` |
| Zuordnung ausschließlich über die Detailansicht | `AddStation*`/`RemoveStationAsync` werden nur aus `StationFavoritesViewModel` aufgerufen; Gruppenübersicht und Gruppenansicht rufen nur `CreateGroup`, `UpdateGroup`, `DeleteGroup`, `UpdateEntry` auf (per Suche über alle Aufrufer bestätigt) | Code, ADR 0006, Hilfe |
| Gruppen erstellen, umbenennen, nach Rückfrage löschen, Beschreibung | Übersicht „Neue Gruppe“ mit Beschreibung; Gruppenansicht „Gruppe bearbeiten“; Löschen nur über die Rückfrage in der Seite (`DeleteCommand` öffnet nur die Rückfrage, `ConfirmDeleteCommand` löscht) | Unit `FavoritesServiceTests_Groups`, `FavoriteGroupViewModelTests_Group`; E2E `CreateGroupWithDescription_DeleteAsksForConfirmation` |
| Gruppenansicht mit Notiz und Priorität, nach Priorität geordnet | vier Stufen (Keine/Niedrig/Mittel/Hoch), Reihenfolge hoch zuerst, dann Name | Unit `GetEntries_AreOrderedByPriorityThenName`, `UpdateEntry_StoresNoteAndPriority_SurvivesRestart`; E2E `EntryNoteAndPriority_AreSavedAndShown` |
| FlaUI-Ablauf „suchen – Details – zu neuer Gruppe – umbenennen – entfernen“ | `FavoritesE2ETests_Flow.SearchOpenDetailAddToNewGroupRenameAndRemove` (inkl. Prüfung, dass die Detailansicht den neuen Namen zeigt) | grün im Lauf |
| Windows-Zwischenstand | `review-versions/0.1.14_2026-10-06/` | siehe unten |

### Datenintegrität und Migration

- **Gruppe löschen:** Fremdschlüssel `FavoriteEntries → FavoriteGroups` mit `ON DELETE CASCADE`. Die Zuordnungen der
  Gruppe entfallen, andere Gruppen und die Tankstelle bleiben erhalten (Unit `DeleteGroup_RemovesOnlyItsEntries`,
  Integration `Database_CascadesGroupDeleteAndProtectsStations` auf einer echten Datei mit aktiven Fremdschlüsseln).
- **Tankstellen** sind über `Restrict` geschützt; die App löscht keine `Stations` (per Suche geprüft: es gibt keine
  `Remove`/`ExecuteDelete` auf Stationen). Favoriten behalten so Name und Adresse offline.
- **Umbenennen:** Namen werden getrimmt (1–40 Zeichen) und ohne Beachtung der Groß- und Kleinschreibung auf Eindeutigkeit
  geprüft, auch bei Umlauten (Dienst mit `OrdinalIgnoreCase`). Ein Index mit `NOCASE` sichert zusätzlich ab. Der eigene
  Name ist beim Umbenennen erlaubt (`UpdateGroup_NameOfOtherGroup_IsRejectedOwnNameIsAllowed`).
- **Notiz/Priorität:** Die Notiz hat höchstens 500 Zeichen, leer wird zu `null`. Die Priorität wird als Enum-Name
  gespeichert, unbekannte Werte werden beim Lesen als „Keine“ behandelt.
- **Migration `AddFavorites`** legt nur zwei neue Tabellen und Indizes an. Bestehende Tabellen werden nicht verändert.
  Der Upgrade-Test `Initialize_FromSchemaBeforeFavorites_KeepsExistingDataAndAddsTables` migriert eine echte Datei auf
  den Stand vor dem Schritt, befüllt sie und bestätigt nach dem Upgrade, dass Tankstelle, Preis, Spritsorten und
  Einstellungen erhalten sind.

### Designentwurf

Die Abweichungen sind in ADR 0006 begründet dokumentiert. ADR 0004 und ADR 0005 verweisen darauf:

- kein Stern in der Kopfkarte;
- „Zu Favoriten hinzufügen“/„Aus Favoriten entfernen“ statt „Zu weiterer Gruppe hinzufügen / verwalten“ und eines
  Kreuzes am Chip;
- „Aktiv synchronisiert“ entfällt;
- Gruppenübersicht und Gruppenansicht als Zwischenlösung bis zur Startseite in Schritt 11;
- Priorität in benannten Stufen.

Die Karte „Favoritengruppen“ mit Chips der zugeordneten Gruppen entspricht dem Entwurf.

### Zusätze (Kachel-Restpunkte)

1. **HTTP-Caching** (`TileCachePolicy`, `HttpTileSource`):
   - `Cache-Control` hat Vorrang:
     - `no-store`: nur im Arbeitsspeicher, eine vorhandene Datei wird entfernt;
     - `no-cache`: speichern, aber vor jeder Verwendung rückfragen;
     - `max-age` wird ausgewertet.
   - Danach gilt `Expires` relativ zu `Date`. Ein ungültiges `Expires` gilt als abgelaufen (RFC 9111).
   - Nur ohne jede Angabe gilt der Rückfall von 7 Tagen.
   - Bedingte Anfragen nutzen `If-None-Match` bzw. `If-Modified-Since`. Ein `304` verlängert die Gültigkeit, und bei
     einem Fehler wird die veraltete Kachel weiterverwendet.
   - Abgedeckt durch `HttpTileSourceTests_HttpCaching` (8 Tests) und `HttpTileSourceTests_NoStoreAndExpires`, nur gegen
     einen Test-Handler bzw. den Mock-Server.
   - ADR 0005 ist präzisiert.
2. **Speicherort:** `IAppDataPathProvider.GetCacheDirectory()` liefert `FileSystem.CacheDirectory`, im Testmodus das
   isolierte Testverzeichnis. Das alte Verzeichnis `<AppData>/tiles` wird bestmöglich gelöscht (`LegacyTileCache`), aber
   nicht, wenn es dem aktuellen Verzeichnis entspricht.
3. **User-Agent und Quellenangabe:** Der User-Agent lautet
   `Tankatlas/<AppInfo.VersionString> (+https://github.com/martin-stromberg/Tankradar; de.martinstromberg.tankradar)`.
   Die URL entspricht dem tatsächlichen Remote. Er gilt einheitlich für Kacheln, Nominatim und den Preisdienst. Die
   Quellenangabe ist eine Schaltfläche im Linkstil und öffnet `https://www.openstreetmap.org/copyright` über `Launcher`.
4. **Mausbedienung unter Windows:** `StationMapView.Pointer.cs` nimmt nur Maus und linke Taste an. Ziehen verschiebt,
   das Mausrad zoomt um ganze Stufen und summiert Teilrasten. Die Ereignisse werden beim Entfernen abgemeldet. Die
   Umrechnung prüft `MapPointerTrackerTests_Mouse`. Die Markierungen sind `Button`s und behandeln Mausklicks selbst, das
   Antippen öffnet also weiterhin die Details. **Ziehen und Mausrad im echten Fenster sind nur von Hand prüfbar** und
   wurden nicht geprüft, weil der Mauszeiger des Anwenders nicht bewegt werden durfte.

Kleinere Beobachtungen ohne Einfluss auf die Erfüllung:

- Der Header `Age` wird bei der Frischeberechnung nicht abgezogen.
- Bei einem `304` ohne eigene Caching-Angaben gilt der Rückfall von 7 Tagen, nicht das ursprüngliche `max-age`. Nach
  RFC 9111 würden die gespeicherten Kopfzeilen fortgelten. Für die OSM-Server, die bei `304` eigene Angaben senden, ist
  das praktisch folgenlos.
- Beim Aufräumen nach Größe zählt die Dateizeit der PNG. Diese bleibt bei einem `304` unverändert, sodass häufig
  bestätigte Kacheln früher verdrängt werden können.
- Eine gleichzeitige Doppelzuordnung derselben Tankstelle löst den eindeutigen Index aus. Der Dienst meldet das als
  `DuplicateName`, also mit der Meldung zu doppelten Namen statt „bereits zugeordnet“. Das ist nur bei einem Wettlauf
  zwischen zwei Ansichten möglich.

### Praktische Verifikation

- **`scripts/local-ci.ps1`**, einmal vollständig mit E2E off-screen und Sicherheitsprüfung:

  | Prüfung | Ergebnis |
  |---|---|
  | Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
  | Restore, Format, Sicherheitsprüfung („Keine anfälligen Pakete“) | OK |
  | Build mit Warnungen als Fehler | OK |
  | Unit | 693/693 |
  | Integration | 95/95 |
  | Zeilenabdeckung | 93 % (Schwelle 70 %) |
  | iOS-Compile-Prüfung | OK |
  | **FlaUI-E2E** | **60/61**, gescheitert: `DiagnosticsCaptureE2ETests.FailingTestBodyProducesScreenshotUiTreeAndErrorFile` („Screenshot einfarbig“) |

  Alle neuen Favoriten-E2E-Tests (Flow, Groups) waren grün.
- **`scripts/test-ios-deployment.ps1`:** erfolgreich (Exit-Code 0).
- **Zwischenstand `review-versions/0.1.14_2026-10-06/`:**
  - Er ist vorhanden und per `.gitignore` ausgeschlossen. `CHANGELOG.md` beschreibt Schritt 10 mit den Zusätzen.
  - Die Binärdateien stammen von 13:47. `FileVersion` ist `0.1.14.0`, `ProductVersion` ist `0.1.0+801064b`: Stand
    `801064b`, der sich vom HEAD nur in der Dokumentation unterscheidet.
  - Startfähigkeit: Ein kurzer Start off-screen mit temporärem `TANKATLAS_TEST_DATA_PATH` lief. Nach 12 s reagierte der
    Prozess, das Fenster hieß „Tankatlas“ und die Datenbank wurde im temporären Verzeichnis angelegt. Danach wurde der
    Prozess gezielt per PID beendet und das Verzeichnis entfernt. Kein `Tankradar.MAUI`-Prozess ist übrig geblieben.

### Untersuchung des E2E-Fehlschlags „Screenshot einfarbig“

Bewertung: **Umgebungseffekt, kein Produkt- oder Testfehler dieses Schritts.** Begründung:

- **Isoliert ebenfalls rot:** Nur `DiagnosticsCaptureE2ETests` off-screen ergab 1 Fehler und 1 Erfolg.
- **Auch im Vordergrund rot:** Genau ein Lauf mit `TANKRADAR_E2E_WINDOW=foreground` scheiterte mit derselben Meldung. Der
  Off-Screen-Modus ist damit nicht die Ursache.
- **Sitzung:** Sie ist nicht gesperrt. `query user` zeigt „Aktiv“, `LogonUI.exe` läuft nicht, und der Eingabedesktop ist
  erreichbar. Das letzte Entsperren war um 10:11.
- **Bildschirm:** Er ist sehr wahrscheinlich aus. Die letzte Benutzereingabe lag rund 19 600 s (≈ 5,4 h) zurück, und das
  aktive Energieschema schaltet den Bildschirm am Netzteil nach 900 s ab (`VIDEOIDLE` = `0x384`). Bei ausgeschaltetem
  Bildschirm rendert und komponiert DWM für ein neu gestartetes WinUI-Fenster keine neuen Bilder. `PrintWindow` mit
  `PW_RENDERFULLCONTENT` liefert dann eine einfarbige Fläche. Die übrigen 60 Tests laufen nur über UI Automation und
  brauchen kein gerendertes Bild.
- **Code unverändert:** Schritt 10 ändert weder den Test noch `E2EDiagnostics.cs` noch `OffscreenWindow.cs`. Diese
  Dateien wurden zuletzt in `c72adbe` (Schritt 6a) geändert. Im Lauf der Abnahme von Schritt 9 (aktive Sitzung) war
  derselbe Test off-screen grün (55/55), ebenso zuletzt in der CI (`v0.1.0-rc.5`).
- **Nicht direkt nachgewiesen:** Dass der Test bei eingeschaltetem Bildschirm grün ist, lässt sich ohne Eingriff am
  Rechner nicht zeigen. Der Bildschirm lässt sich nicht ohne Benutzereingabe einschalten, und die Maus durfte nicht
  bewegt werden.

Empfehlung: Den Test bzw. `local-ci.ps1` einmal bei eingeschaltetem Bildschirm wiederholen, z. B. vor dem Push. Ist er
dann grün, ist der Befund erledigt. Optional sollte der Test bei ausgeschaltetem Bildschirm übersprungen werden oder
eine verständlichere Meldung liefern, damit das Auslieferungs-Gate lokal nicht scheinbar ohne Grund rot ist.

### Sonstiges

- Im Arbeitsverzeichnis liegt das unversionierte Verzeichnis `docs/features/` mit Altlasten aus Schritt 2
  (`…--schritt-2-git-hooks`). Es gehört nicht zu diesem Schritt und ist nicht committet. Es sollte bei Gelegenheit
  entfernt werden.
- Echte Endpunkte wurden nicht aufgerufen. `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben. Es gab keine
  Commits, keine Pushes und keine Änderungen an der Git-Konfiguration.
