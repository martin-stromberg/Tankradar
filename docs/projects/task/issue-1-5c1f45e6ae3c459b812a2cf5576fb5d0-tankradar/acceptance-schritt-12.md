# Abnahmeprüfung – Entwicklungsschritt 12 (Fahrzeugverwaltung im Tankbuch)

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Der Bereich „Tankbuch“ verwaltet Fahrzeuge vollständig gemäß Schritt 12:

- Anlegen, Bearbeiten und Löschen mit Name, Kraftstoffart, Start-Kilometerstand und optionalem Startverbrauch.
- Feldbezogene Validierung, z. B. kein negativer Kilometerstand.
- Eine Rückfrage vor jedem Löschen. Bei vorhandenen Vorgängen nennt sie deren Zahl und das endgültige Mitlöschen.
- Tatsächliches Mitlöschen per `ON DELETE CASCADE`.
- Eine rein additive Migration, die bestehende Daten erhält.
- Eingabefelder mit festen Einheiten.
- Ausschließlich lokale Speicherung ohne Netzwerkzugriffe.

Der Zusatz aus der Abnahme von Schritt 11 ist erledigt:

- Die Hilfe zum Suchcache ist korrigiert.
- Die Integrationstests sind ohne `ClearAllPools`.
- Der Drosselungstest misst clientseitig.

**Praktische Verifikation:**

- **`local-ci.ps1`:** vollständig grün, einschließlich aller 80 FlaUI-E2E-Tests off-screen. Diesmal war auch
  `DiagnosticsCaptureE2ETests` grün.
- **Pre-Push-Prüfungen:** grün, Commit-Bereich gegenüber `origin/…-tankradar`: 9 Commits.
- **Integrationssuite:** in 16 vollständigen Läufen ohne Fehlschlag, davon 6 unter dreifach paralleler Last.
- **Zwischenstand `0.1.16_2026-10-11`:** startet, wendet die Migration auf eine 0.1.15-Datenbank mit Bestandsdaten
  verlustfrei an und hat ein korrekt kodiertes CHANGELOG.

Die Anpassung der beiden E2E-Erwartungen an die Sprachausgabe-Beschreibung ist technisch korrekt und schwächt die
Prüfung der Anforderung nicht ab (Begründung unter „Hinweise“). Sie macht aber eine Unschärfe bei der Barrierefreiheit
sichtbar: Der vorgelesene Name enthält bei zwei Bestätigungsschaltflächen den sichtbaren Text nicht. Das ist eine
Empfehlung, keine Abweichung, weil Anforderung und Plan dazu nichts vorgeben.

## Abweichungen

Keine.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-12-fahrzeuge`, HEAD `d839d52`.
- **Basis:** `origin/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `6f858f3` (zugleich Merge-Base). Das lokale
  Label des Basisbranchs ist veraltet und wurde nicht verwendet.
- **Diff:** 76 Dateien, +4898/−68, in 9 Commits (`f1727c8` … `d839d52`).
  - Darin enthalten ist der Merge `6a5e8a6` des iOS-Hotfixes `6f858f3`.
  - Konflikt nur in `changes.log`; beide Einträge sind erhalten und konfliktmarkerfrei. Der Hotfix steht oben, darunter
    Schritt 12.
- **Grundlagen:**
  - `project-plan.md`: Schritt 12 einschließlich „Zusatz aus der Abnahme von Schritt 11“ und die Tabelle „Grobe
    Vorgehensentscheidungen“
  - `requirement.md`: Vehicle-Modell, offene Frage 6.1.4
  - `acceptance-schritt-11.md`: Format und Restpunkte
  - Designentwurf (`DESIGN.md` „Tankbuch-Formulare“, Screen `tankbuch_verbrauchsauswertung`), nur gelesen, entpackt im
    Scratchpad

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Mehrere Fahrzeuge im Bereich „Tankbuch“ | `TankbookPage` mit Fahrzeugkarten (Name, darunter „Kraftstoffart • Start … km • Startverbrauch …“), nach Namen geordnet (de-DE, ohne Groß-/Kleinschreibung); Hinweis bei leerer Liste | Unit `TankbookViewModelTests_Load` (4), `VehicleServiceTests_Manage.GetVehicles_AreOrderedByNameGerman`; E2E `TankbookE2ETests_Persistence` (zwei Fahrzeuge in Namensordnung) |
| Name | Getrimmt, 1–40 Zeichen, eindeutig ohne Beachtung der Groß- und Kleinschreibung (Prüfung im Dienst und eindeutiger Index mit `NOCASE`; gleichzeitiges Speichern ergibt `DuplicateName`) | Unit `VehicleValidatorTests_Form`, `VehicleServiceTests_Manage` (Duplikat, eigener Name in anderer Schreibweise, gleichzeitiges Speichern); Integration `Database_RejectsDuplicateVehicleNamesCaseInsensitive`; E2E `DuplicateName_ShowsNameError` |
| Kraftstoffart (Benzin, Diesel, Elektro, Hybrid) | Enum `VehicleFuelKind`, als Name gespeichert; Chips mit fester Auswahl, „Benzin“ vorausgewählt; unbekannter Speicherwert führt zu Benzin und einer Warnung ohne Namen | Unit `TankbookTextsTests_Labels.GetFuelKindLabel_AllValues`, `Add_OpensFormWithPetrolPreselected`, `GetVehicles_UnknownStoredFuelKind_…`; E2E `Flow` (Wechsel Diesel → Elektro) |
| Start-Kilometerstand | Pflicht, ganze Zahl 0–9.999.999, Tausenderpunkte erlaubt; Einheit „km“ | Unit `VehicleInputParserTests_Odometer` (11 Fälle), `VehicleValidatorTests_Input` |
| Optionaler Startverbrauch | Leer erlaubt; > 0 und < 100, höchstens 2 Nachkommastellen, Komma oder einzelner Punkt; als `decimal` in `TEXT` exakt gespeichert | Unit `VehicleInputParserTests_Consumption` (11 Fälle); Integration `Vehicles_SurviveRestartOnRealDatabaseFile` (16,55 exakt) |
| Anlegen, Bearbeiten, Löschen | Formular und Rückfrage als Bereiche in der Seite; Bearbeiten ist mit den gespeicherten Werten vorbelegt | Unit `TankbookViewModelTests_Form` (10), `_Delete` (9), `VehicleServiceTests_Manage`/`_Delete`; **E2E `TankbookE2ETests_Flow.CreateEditDeleteVehicle_Flow`** (Anlegen, Bearbeiten mit Einheitenwechsel, Löschen mit Abbrechen und Bestätigen) |
| Validierung (u. a. kein negativer Kilometerstand) | Alle Feldfehler gleichzeitig unter dem Feld, roter Rahmen, nichts gespeichert, Ansage über die Sprachausgabe; zusätzlich Schutzprüfung `VehicleValidator.Validate` im Dienst | Unit `Save_InvalidFields_ShowsFieldErrorsAndSavesNothing`, `ParseOdometer_InvalidInput_ReturnsError("-5" → Negative)`, `Validate_Input_ReturnsResult(-1 → InvalidOdometer)`, `CreateVehicle_InvalidInput_IsRejectedWithoutSaving`; **E2E `InvalidInput_ShowsFieldErrorsAndSavesNothing`** (leerer Name, „-5“ mit „Der Kilometerstand darf nicht negativ sein.“, „abc“) |
| Ausdrückliche Rückfrage bei vorhandenen Vorgängen, die das Mitlöschen nennt | Immer eine Rückfrage, die Zahl der Vorgänge wird frisch gelesen. Text bei Vorgängen: „Fahrzeug „X“ löschen? Dabei werden auch die N zugehörigen Tank- und Ladevorgänge endgültig gelöscht. Das lässt sich nicht rückgängig machen.“ Die Bestätigung heißt dann „Fahrzeug und Vorgänge löschen“. | Unit `FormatDeleteQuestion_ByEntryCount` (0/1/3), `Delete_WithEntries_AsksExplicitlyAndDeletesEntries`, `Delete_WithoutEntries_AsksSimpleQuestion`; **E2E `DeleteVehicleWithEntries_AsksExplicitlyAndDeletesEntries`** |
| Vorgänge werden tatsächlich mitgelöscht | `TankbookEntries.VehicleId` mit `ON DELETE CASCADE`, Vorgänge anderer Fahrzeuge bleiben erhalten | Unit `DeleteVehicle_WithEntries_DeletesItsEntriesOnly`; Integration `Database_CascadesVehicleDeleteToEntries` (SQL und `PRAGMA foreign_key_list` = `CASCADE`), `DeleteVehicle_WithEntries_OnRealFile_RemovesEntries`; E2E: echte App, danach 0 Vorgänge in der Datenbank |
| Migration erhält bestehende Daten | `20261011105508_AddVehicles` legt nur zwei neue Tabellen und Indizes an; `MigrationsTests_ModelSync` ohne offene Modelländerungen | Integration `Initialize_FromSchemaBeforeVehicles_KeepsExistingDataAndAddsTables`, `DatabaseInitializerTests_FirstStart`/`_Upgrade` (6 Migrationen); praktisch: Upgrade einer echten 0.1.15-Datenbank mit Bestandsdaten (siehe unten) |
| Nur lokal, offline, keine Weitergabe | `VehicleService` arbeitet nur über `IDbContextFactory`, ohne HTTP und ohne Verbindungsabhängigkeit. Protokolle nennen nur Ausnahmetypen. | Code-Durchsicht (kein `HttpClient`, keine URL im Diff); Unit `Load_StorageFailure_ShowsMessageWithoutLoggingNames`; E2E-Tankbuch läuft ohne jeden Mock-Server |
| Eingabefelder mit Einheiten gemäß Design | Fester Suffix `km`, `L/100 km` bzw. `kWh/100 km` (wechselt mit der Kraftstoffart), Rahmen `#E2E8F0`, Radius 8, Fokus 2 px `#0F766E`, Fehlerrahmen rot, Touch-Ziele 44. Abweichungen (Karte „Aktives Fahrzeug“, Kennzeichen, Statistik usw.) sind in ADR 0008 begründet. | E2E `Flow` (Einheiten `km`, `L/100 km`, Wechsel auf `kWh/100 km`); `DESIGN.md` §5 mit XAML abgeglichen |
| FlaUI: Anlegen, Bearbeiten, Löschen | 5 neue E2E-Tests: `Flow`, `Validation` (2), `DeleteWithEntries`, `Persistence` (echter Neustart) | alle grün in `local-ci` |
| Zusatz 1: Hilfe zum Suchcache | `docs/help/Startseite/beschreibung.md`: „… danach ist der Merker wirkungslos und wird bei der nächsten Preisabfrage, spätestens beim Beenden der App, verworfen“; deckt sich mit dem Verhalten aus 11 | Diff |
| Zusatz 2: sporadische Integrationstests | `TestDatabase` mit `Pooling=False`, alle `ClearAllPools()` entfernt (im ganzen Repository keine Fundstelle mehr); Drosselungstest misst mit `RequestTimingHandler` clientseitig (≥ 990 ms) | `TestDatabaseTests_Pooling` (2); 16 Läufe grün (siehe unten) |
| Windows-Zwischenstand | `review-versions/0.1.16_2026-10-11/` | siehe „Zwischenstand“ |
| Dokumentation | `docs/help/Tankbuch/` (4 Dateien), ADR 0008 (beantwortet die offene Frage 6.1.4: Mitlöschen nach Bestätigung), Navigationshilfe, `changes.log` | Durchsicht |

### Bewertung: E2E-Anpassung an die Sprachausgabe-Beschreibung (`d839d52`)

**Mechanik.** In .NET MAUI legt `SemanticProperties.Description` den zugänglichen Namen fest:

- unter Windows als `AutomationProperties.Name` (UIA-Name),
- unter iOS als `AccessibilityLabel`.

`SemanticProperties.Hint` landet in `HelpText` bzw. `AccessibilityHint`. UIA liefert daher für die Bestätigung als
`Name` die Beschreibung, also „Ja, Fahrzeug „Golf Variant“ löschen“ bzw. „Fahrzeug „Golf“ und Vorgänge löschen“, und
nicht den Buttontext. Die Beschreibung kam mit dem Review-Befund in `c8a4dda` hinzu. Die beiden E2E-Erwartungen danach
anzupassen, ist folgerichtig. Die neuen Werte sind genau das, was ein Screenreader vorliest.

**Prüft der Test die Anforderung weiter?** Ja.

- Die Kernaussage der Rückfrage wird unverändert über `Tankbook.Vehicle.DeleteQuestion` geprüft: „2 zugehörigen Tank-
  und Ladevorgänge endgültig gelöscht“. Ein Label ohne Description liefert als Name seinen sichtbaren Text.
- Das tatsächliche Mitlöschen wird über die Datenbank geprüft: `CountEntries == 0`.
- Der erwartete Name ist kein beliebiger Ist-Wert. Er ist in `TankbookTextsTests_Labels.ButtonDescriptions_…` und
  `Delete_AnnouncesQuestionAndDescribesButtonsWithName` festgeschrieben und nennt weiterhin „und Vorgänge löschen“.
- Die sichtbaren Beschriftungen („Ja, löschen“ / „Fahrzeug und Vorgänge löschen“) prüfen die Unit-Tests über
  `ConfirmDeleteLabel`. Die XAML-Bindung ist kompiliert (`x:DataType`). Per UIA ist der sichtbare Text nicht mehr direkt
  lesbar; das ist der einzige Verlust.

**Barrierefreiheit: Name und sichtbarer Text (Empfehlung, nicht blockierend).**

Nach WCAG 2.5.3 „Label in Name“ soll der zugängliche Name den sichtbaren Text enthalten, am besten am Anfang. Sonst
finden Sprachsteuerungen (Windows Voice Access, iOS Voice Control) die Schaltfläche über den gesehenen Text nicht.

| Schaltfläche | Sichtbar | Vorgelesener Name | Enthält den sichtbaren Text? |
|---|---|---|---|
| `Tankbook.Vehicle.Edit` | Bearbeiten | Golf bearbeiten | ja |
| `Tankbook.Vehicle.Delete` | Löschen | Golf löschen | ja |
| `Tankbook.Vehicle.DeleteCancel` | Abbrechen | Löschen von „Golf“ abbrechen | ja |
| `Tankbook.Vehicle.DeleteConfirm` (ohne Vorgänge) | Ja, löschen | Ja, Fahrzeug „Golf“ löschen | **nein** |
| `Tankbook.Vehicle.DeleteConfirm` (mit Vorgängen) | Fahrzeug und Vorgänge löschen | Fahrzeug „Golf“ und Vorgänge löschen | **nein** |
| `Tankbook.Form.Cancel` | Abbrechen | Formular schließen, ohne zu speichern | **nein** |

**Vorschlag:**

- Die Beschreibung mit dem sichtbaren Text beginnen, z. B. „Ja, löschen: Fahrzeug „Golf““, „Fahrzeug und Vorgänge
  löschen: „Golf““ und „Abbrechen, Formular schließen ohne zu speichern“.
- Alternativ den Kontext in `SemanticProperties.Hint` legen und den Namen gleich dem sichtbaren Text lassen.
- Die E2E-Tests könnten dann `Name.StartsWith(<sichtbarer Text>)` und den Fahrzeugnamen prüfen und so beides absichern.

Anforderung, Plan und Designentwurf geben für die Barrierefreiheit nichts vor. Deshalb ist das keine Abweichung.

### Bewertung: Stabilisierung der sporadischen Tests

**`ClearAllPools` / `ObjectDisposedException`:** Die echte Ursache ist behoben, nicht verdeckt.

- Der globale Eingriff `SqliteConnection.ClearAllPools()` in Verbindungen paralleler Testklassen ist vollständig entfallen.
- `TestDatabase` öffnet Verbindungen ohne Pool. Dadurch lässt sich das Testverzeichnis ohne Pool-Leerung löschen
  (`Dispose_AfterServiceUse_RemovesDirectoryWithoutClearingPools`).
- Es gibt keinen Retry und keine Ausnahme-Unterdrückung.

**Drosselungstest der Geokodierung:** Die eigentliche Ursache ist behoben, eine kleine Restunschärfe bleibt.

- Gemessen wird jetzt beim Eintritt in den `HttpClient`-Handler, also nach `RequestThrottle.WaitAsync`. Vorher war es die
  lastabhängige Empfangszeit am Mock-Server.
- `Assert.Equal(2, Nominatim.Requests)` stellt sicher, dass beide Anfragen den Server erreicht haben.
- Kein Retry.
- **Restunschärfe:** Der gemessene Abstand enthält die Laufzeit zwischen dem Zeitstempel der Drosselung und dem
  Handler-Eintritt. Beim ersten Aufruf kann sie etwas länger sein, etwa durch JIT. Dafür bleiben nur 10 ms Toleranz.
- In allen Läufen dieser Abnahme gab es keinen Fehlschlag.
- **Optional:** den Abstand an der Drosselung selbst messen, z. B. mit einem aufzeichnenden `TimeProvider` bzw. `IDelay`
  wie in `RequestThrottleTests_Interval`, oder die Toleranz begründet etwas größer wählen.

**`TankbookE2ETestBase.PressForVehicle`:** Das ist eine legitime Synchronisation und kein Verdecken.

- Indexermittlung, Suche und `Invoke` laufen gemeinsam in `WaitUntil` mit fester Frist (`DefaultTimeout`).
- Abgefangen werden nur die Ausnahmen veralteter UIA-Elemente, wenn `BindableLayout` die Liste neu aufbaut:
  `ElementNotAvailableException`, `PropertyNotSupportedException`, `InvalidOperationException`.
- Assertions und andere Fehler bleiben sichtbar. Nach Ablauf der Frist scheitert der Test mit einer klaren Meldung.
- Die Wirkung prüft jeder Aufrufer anschließend: Formulartitel und Vorbelegung bzw. die Rückfrage mit Fahrzeugnamen.
- **Restrisiken:**
  - Index und Schaltflächen stammen aus zwei UIA-Abfragen. Baut sich die Liste genau dazwischen mit anderer Reihenfolge
    um, träfe der Klick ein anderes Fahrzeug. Die folgenden Assertions würden das aufdecken.
  - Ein `Invoke`, das ausgelöst hat und erst danach wirft, würde wiederholt. Für „Bearbeiten“ und „Löschen“ ist das
    idempotent.
  - Für die Bestätigung des Löschens wird `PressForVehicle` nicht verwendet, sondern `Press`.

### Praktische Verifikation

**`scripts/local-ci.ps1`**

- Vollständig von 16:41 bis 16:47, E2E off-screen, mit Sicherheitsprüfung.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` aus der
  Umgebung entfernt, ohne sie zu lesen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK |
| Unit | 889/889 (Schritt 11: 807) |
| Integration | 116/116 (Schritt 11: 109; neu: `VehiclesMigrationTests_Upgrade` 3, `VehicleServiceTests_RealDatabase` 2, `TestDatabaseTests_Pooling` 2) |
| Zeilenabdeckung | 93,4 % (Schwelle 70 %) |
| FlaUI-E2E | **80/80** in 4 min 37 s, darunter alle 5 `TankbookE2ETests_*` und beide `DiagnosticsCaptureE2ETests`. Kein Umgebungseffekt „Screenshot einfarbig“, eine Wiederholung war nicht nötig. |
| iOS-Compile-Prüfung (`net10.0-ios`) | OK |
| Gesamt | Exit-Code 0, „Lokaler Prüflauf erfolgreich.“ |

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei einem Push nach `refs/heads/task/…-tankradar`: lokal `d839d52`, Remote-Stand `6f858f3`.
  - Schlüsselvariablen wie oben entfernt.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py --base 6f858f3 --end d839d52` | OK (9 Commits, einschließlich Merge `6a5e8a6 merge: …`) |
| `forbidden-patterns-check.py --all --strict` | OK (617 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (417 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (413 Dateien) |
| `test-execution-check.py` (Build, Unit, Integration; E2E im Hook standardmäßig aus) | OK: Unit 889/889, Integration 116/116 |
| Gesamt | Exit-Code 0, „Pre-Push-Prüfungen erfolgreich abgeschlossen.“ |

Zusätzlich meldet `conventional-commits-check.py --base main --end d839d52` OK (115 Commits).

**Wiederholte Integrationsläufe (Stabilisierung)**

| Läufe | Konfiguration | Ergebnis |
|---|---|---|
| 1 | `local-ci` (Release, Coverage) | 116/116 |
| 1 | Pre-Push (Debug) | 116/116 |
| 8 | nacheinander, Debug, `--no-build` | 8 × 116/116, je etwa 19 s |
| 6 | 2 Runden mit je 3 gleichzeitigen Prozessen, Release (erhöhte CPU-Last) | 6 × 116/116 |

16 von 16 vollständigen Läufen waren grün, auch `Resolve_TwoQuickRequests_AreAtLeastOneSecondApart` und die
Migrationstests. Zum Vergleich Schritt 11: 2 Fehlschläge in 20 Läufen. Die Stichprobe ist kein Beweis, passt aber zur
behobenen Ursache.

**Zwischenstand `review-versions/0.1.16_2026-10-11/`**

- **Herkunft:** erzeugt um 14:07, nach HEAD (13:59), und per `.gitignore` ausgeschlossen. `FileVersion` ist `0.1.16.0`,
  `ProductVersion` ist `0.1.0+d839d528…` und entspricht damit HEAD.
- **`CHANGELOG.md`:** UTF-8 mit BOM, ohne „Ã“/„â€““-Folgen. Umlaute und typografische Anführungszeichen sind korrekt, der
  Inhalt entspricht dem `changes.log`-Eintrag von Schritt 12. Zeilenenden sind gemischt CRLF/LF; das ist harmlos.
- **Frischer Start** (Testmodus, `TANKATLAS_TEST_WINDOW=offscreen`, eigenes Datenverzeichnis im Scratchpad, ohne
  Schlüssel- und Dienstadressen-Variablen):
  - PID 21956 reagierte, Fenstertitel „Tankatlas“.
  - `tankatlas.db` mit 6 Migrationen bis `20261011105508_AddVehicles`.
  - Die Tabellen `Vehicles` und `TankbookEntries` sind vorhanden. Der Fremdschlüssel steht auf `CASCADE`, der Index
    `IX_Vehicles_Name` ist eindeutig.
  - Danach per PID beendet.
- **Upgrade mit Bestandsdaten:**
  1. Zwischenstand 0.1.15 gestartet (PID 27528), Datenbank bis `AddNearbySetting` angelegt, per PID beendet.
  2. Bestandsdaten eingefügt: Einstellungen mit `ShowNearby = 1`, Tankstelle, Preis, Favoritengruppe und Eintrag.
  3. Zwischenstand 0.1.16 auf derselben Datei gestartet (PID 38200, reagierte, Titel „Tankatlas“), per PID beendet.
  - **Ergebnis:** `AddVehicles` ist angewendet, alle Bestandsdaten sind unverändert, die neuen Tabellen sind leer.
- Danach lief kein `Tankradar.MAUI`-Prozess mehr. Es wurden nur die eigenen drei Prozesse beendet.

### Datenschutz

- **Nur lokal:**
  - Fahrzeuge und Vorgänge liegen nur in der lokalen SQLite-Datenbank.
  - Der Diff enthält weder `HttpClient` noch URLs noch Verbindungslogik im Tankbuch.
  - Die sechs neuen Protokollaufrufe nennen nur `{ExceptionType}` oder gar keine Werte, nie Namen oder Kilometerstände.
  - `SemanticScreenReaderAnnouncer` protokolliert den angesagten Text nicht.
- **Keine Produktivendpunkte in Tests:**
  - Die Unit-Tests nutzen SQLite im Arbeitsspeicher.
  - Die Integrationstests nutzen Dateien im isolierten Testverzeichnis bzw. den Nominatim-Mock auf `127.0.0.1`.
  - Die Tankbuch-E2E-Tests laufen ganz ohne Mock-Server. Abrufe sind im Testmodus ohne Adresse gesperrt.
  - Auch die praktischen Starts liefen im Testmodus ohne Dienstadressen.
- **API-Schlüssel:**
  - Im Diff steht nur die synthetische Kennung `00000001-0000-4000-8000-000000000000`.
  - `forbidden-patterns-check.py --all --strict` ist grün.
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben, sondern für alle Läufe nur per `env -u` bzw.
    `Remove-Item Env:` entfernt.

### Kleinigkeiten ohne Einfluss auf die Erfüllung

- **Kaskade hängt am Standard der SQLite-Bibliothek:**
  - Das Mitlöschen setzt eingeschaltete Fremdschlüssel voraus. Die App setzt sie nicht ausdrücklich, weder per
    `Foreign Keys=True` in der Verbindungszeichenfolge noch per `PRAGMA`. Sie verlässt sich auf den Standard von
    `e_sqlite3` (`SQLITE_DEFAULT_FOREIGN_KEYS=1`), wie schon die Favoritengruppen aus Schritt 10.
  - Unter Windows ist das durch Integration und E2E belegt. iOS nutzt ebenfalls `SQLitePCLRaw.bundle_e_sqlite3`.
  - Robuster wäre `Foreign Keys=True` in `MauiProgram`, ersatzweise ein kurzer Blick in TestFlight (Schritt 18).
- **Restfall für Schritt 13:** Die Zahl der Vorgänge wird beim Antippen von „Löschen“ gelesen. Kommt zwischen Rückfrage
  und Bestätigung ein Vorgang hinzu, nennt die Rückfrage ihn nicht. In Schritt 12 gibt es keine Erfassung. Schritt 13
  sollte die Zahl beim Bestätigen erneut prüfen oder die Rückfrage schließen.
- **`docs/help/Tankbuch/ablauf-technisch.md`:** erwähnt die Sprachausgabe-Beschreibungen und `IScreenReaderAnnouncer`
  nicht. Die Hilfe spricht den Leser mit „Sie“ an, die Oberflächentexte mit „du“; das ist im Projekt durchgängig so.

### Sonstiges

- Im Arbeitsverzeichnis liegt weiterhin das unversionierte Verzeichnis `docs/features/` aus Schritt 2. Es gehört nicht zu
  diesem Schritt.
- Es gab keine Commits, keine Pushes und keine Codeänderungen.
  - Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert (keine Datei jünger als 14:07). Der
    Designentwurf und die Testdatenbanken der Starts liegen im Scratchpad.
  - Testergebnisse liegen in den per `.gitignore` ausgeschlossenen Ordnern `TestResults/` und `coverage-report/`.
