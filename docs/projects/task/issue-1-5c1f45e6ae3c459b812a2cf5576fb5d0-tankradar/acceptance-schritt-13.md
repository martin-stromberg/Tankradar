# Abnahmeprüfung – Entwicklungsschritt 13 (Tank- und Ladevorgänge erfassen)

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Das Tankbuch erfasst, zeigt, korrigiert und löscht Tank- und Ladevorgänge je Fahrzeug gemäß Schritt 13:

- Alle geforderten Angaben: Datum und Uhrzeit, Fahrzeug, Tankstelle (bekannte Tankstelle aus Favoriten und bereits
  verwendeten Namen oder freier Name), Art Kraftstoff/Strom mit Menge in L bzw. kWh, Preis pro Einheit, Gesamtbetrag,
  Kilometerstand, Kennzeichen „Teilbetankung“, optionale Notiz und optionales Belegfoto.
- Plausibilität: Aus zwei der drei Werte Menge, Preis und Gesamtbetrag ergibt sich der dritte (Live-Hinweis „Ergibt …“,
  Übernahme beim Speichern). Abweichungen werden blockierend mit dem rechnerischen Betrag gemeldet.
- Der Kilometerstand liegt nie unter dem Start-Kilometerstand oder dem Vorgänger und nie über dem Nachfolger. Die Prüfung
  sitzt im Dienst.
- Verbrauch pro 100 km je Vollvorgang nach der Volltank-zu-Volltank-Methode. Teilbetankungen werden dem nächsten
  Vollvorgang zugerechnet, Kraftstoff und Strom werden getrennt gerechnet. Die Berechnung erfolgt beim Lesen; Korrekturen
  und Löschungen wirken deshalb sofort auf die Folgewerte.
- Chronologische Liste (neueste zuerst, wie im Designentwurf), vollständig offline, nur lokal.
- Belegfotos liegen unverändert unter `<Datenverzeichnis>/receipts/` mit iOS-Dateischutz, mit Inhaltsprüfung. Sie werden
  mit dem Vorgang bzw. Fahrzeug gelöscht, nichts wird hochgeladen.
- Rein additive Migration `AddTankbookEntryDetails`, die Bestandsdaten erhält.
- Der Zusatz aus der Abnahme von Schritt 12 ist vollständig erledigt: Neuzählung beim Bestätigen, `Foreign Keys=True` und
  WCAG 2.5.3 (Name = sichtbarer Text, Kontext im Hint), auch für alle neuen Schaltflächen.

**Praktische Verifikation:**

- **`local-ci.ps1`:** vollständig grün mit E2E off-screen. Unit 1028/1028, Integration 120/120, FlaUI-E2E 90/90,
  Abdeckung 93,1 %, iOS-Compile-Prüfung OK. `DiagnosticsCaptureE2ETests` war ebenfalls grün, eine Wiederholung war nicht
  nötig.
- **Pre-Push-Prüfungen:** grün, Commit-Bereich `4351395..f7148b9` mit 7 Commits.
- **Neue und geänderte E2E-Klassen:** 3 × 15/15 ohne Ausfall.
- **Zwischenstand `0.1.17_2026-10-11`:** startet off-screen und hebt eine echte 0.1.16-Datenbank mit Fahrzeugen und
  Altvorgängen verlustfrei an. Das CHANGELOG ist korrekt UTF-8-kodiert.

Die selbst gemeldeten Zahlen des Umsetzungsagenten (Unit 1028, Integration 120, E2E 90) sind bestätigt. Die bewusst offen
gelassenen Punkte sind niedrige Qualitätsbefunde ohne Einfluss auf die Erfüllung (siehe „Hinweise“).

## Abweichungen

Keine.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-13-tankvorgaenge`, HEAD `f7148b9`.
- **Basis:** lokaler Projekt-Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `4351395` (zugleich
  Merge-Base; lokal aktueller als `origin`).
- **Diff:** 147 Dateien, +11524/−285, in 7 Commits (`32b2259` … `f7148b9`), darunter der Merge `70983f1` des Basisbranchs.
- **Grundlagen:**
  - `project-plan.md`: Schritt 13 einschließlich „Zusatz aus der Abnahme von Schritt 12“ und die Tabelle „Grobe
    Vorgehensentscheidungen“, besonders „Schutz lokaler Daten“ und „Keine Strompreise in 1.0“
  - `requirement.md`: Fuel/Charge Entry, Logbook Management, Offline Mode
  - `acceptance-schritt-12.md`
  - Umsetzungsplan und ADR 0009
  - Designentwurf (`DESIGN.md` „Tankbuch-Formulare“, Screen `tankbuch_verbrauchsauswertung`), nur gelesen, entpackt im
    Scratchpad

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Vorgänge erfassen, einsehen, korrigieren, löschen | Eigene Seite `TankbookEntriesPage` je Fahrzeug (Schaltfläche „Tank- und Ladevorgänge“ auf der Fahrzeugkarte). Formular und Rückfrage sind Bereiche in der Seite. `ITankbookEntryService` arbeitet mit Ergebnis-Enums und Load-and-Patch. | Unit `TankbookEntryServiceTests_Create`/`_Change`/`_Read`, `TankbookEntriesViewModelTests_*`, `TankbookEntryFormViewModelTests_*`; E2E `TankbookEntriesE2ETests_Flow`, `_EditDelete` |
| Datum und Uhrzeit | `DatePicker`/`TimePicker`, mit „jetzt“ vorbelegt, minutengenau, lokale Wanduhrzeit. Nicht später als jetzt + 1 Min. (Formular und Dienst). | Unit `TankbookEntryValidatorTests_Form` (`FutureDate`), `_Input` |
| Fahrzeug | Die Seite gehört zu genau einem Fahrzeug, gewählt per Namen auf der Karte (nie per Kennung). | Unit `TankbookViewModelTests_Navigation`; E2E `Tankbook.Vehicle.Entries` mit Hint „Fahrzeug „Golf““ |
| Tankstelle: Liste bekannter Tankstellen oder manueller Name | Pflichtfeld (1–60 Zeichen) mit Chips „Bekannte Tankstellen“: Favoriten (Ort bei gleichem Namen), dann verwendete freie Namen, ohne Duplikate, höchstens 30. `StationId` ohne Fremdschlüssel, entfällt bei Handänderung. | Unit `TankbookEntryServiceTests_KnownStations` (3), `TankbookEntryFormViewModelTests_Station`; E2E `OfflineE2ETests_Tankbook` (Chip „Alpha Tankstelle“ gewählt, `StationId` gespeichert, freier Name ohne `StationId`) |
| Art Kraftstoff oder Strom, Menge in L bzw. kWh | `TankbookEntryKind`. Benzin/Diesel → Kraftstoff, Elektro → Strom, Hybrid → wählbar. Einheiten L/kWh, €/L bzw. €/kWh. Ladeorte als freier Name, keine Strompreisquelle. | Unit `IsKindAllowed_ByVehicleFuelKind`, `Hybrid_OffersBothKinds`; E2E `TankbookEntriesE2ETests_Electric` (Ladeort „Zuhause“, `kWh`, `€/kWh`, „16,50 kWh/100 km“) |
| Preis pro Einheit und Gesamtbetrag; Plausibilität „zwei ergeben den dritten, Abweichungen werden gemeldet“ | `AmountCalculator`, kaufmännisch gerundet (Gesamt 2, Preis 3, Menge 2 Nachkommastellen). Bei drei Werten blockierender Fehler am Gesamtbetrag ab \|Menge × Preis − Gesamt\| > 0,02 €, außer ein Wert ist genau der gerundete Quotient (siehe unten). Weniger als zwei Werte → Fehler. Ein berechneter Wert außerhalb seines Bereichs → Fehler am berechneten Feld. | Unit `AmountCalculatorTests_Completion` (6 Fälle), `_Plausibility` (4); E2E `TankbookEntriesE2ETests_Flow` („Ergibt 74,36 €“, „Ergibt 1,859 €/L“), `_Validation` („Menge × Preis ergibt 74,36 €, nicht 75,00 €.“, „mindestens zwei der drei Werte“) |
| Kilometerstand nicht unter Vorgänger bzw. Start | `CheckOdometer` im Dienst: ≥ Start, ≥ chronologischer Vorgänger über alle Arten (Gleichstand erlaubt), ≤ Nachfolger; der Grenzwert steht in der Meldung. Zusätzlich lehnt `UpdateVehicleAsync` einen Start-Kilometerstand über dem niedrigsten Vorgang ab. | Unit `TankbookEntryServiceTests_Odometer` (5), `UpdateVehicle_StartOdometerAboveFirstEntry_IsRejected`; E2E `_Validation` („nicht unter 10.500 km (vorheriger Vorgang)“), `TankbookE2ETests_Validation.StartOdometerAboveFirstEntry_ShowsOdometerError` |
| Verbrauch pro 100 km je Vollvorgang, Standard voll, Teilbetankung ohne eigenen Wert und dem nächsten Vollvorgang zugerechnet | `ConsumptionCalculator` (siehe fachliche Bewertung unten) | Unit `ConsumptionCalculatorTests_FullEntries` (4), `_Partial` (4), `_Electric` (2); E2E `_Flow` („Verbrauch 6,00 L/100 km“, „Strecke 500 km“, erster Vorgang ohne Verbrauch), `_Partial` ((20 + 10) L / 500 km = 6,00) |
| Chronologische Liste | Neueste zuerst wie „Letzte Tankvorgänge“ im Entwurf. Ordnungsschlüssel (Zeitpunkt, km, Id), damit eindeutig. | Unit `GetEntries_NewestFirstWithConsumption`, `Calculate_EntriesOutOfOrder_UsesChronologicalOrder`; E2E (Reihenfolge Beta vor Alpha) |
| Korrigieren und Löschen wirken auf Folgeberechnungen | Verbrauch wird nicht gespeichert, sondern beim Lesen neu berechnet. Löschen erst nach Rückfrage im Eintrag, die Fotodatei wird nach dem Commit entfernt. | E2E `_EditDelete` (km 11.000 → 11.500: 6,00 → 3,00 L/100 km, „Strecke 1.000 km“; Abbrechen behält; Löschen), `OfflineE2ETests_Tankbook` (dasselbe offline) |
| Notiz und Belegfoto (optional) | Notiz bis 500 Zeichen. Foto über `MediaPicker` (`PickPhotosAsync`, Limit 1; Kamera nur bei `IsCaptureSupported`), im Testmodus `TestReceiptPhotoPicker`. | Unit `ReceiptPhotoStoreTests_Files` (8), `TankbookEntryFormViewModelTests_Photo` (7); E2E `_Receipt`, `_Persistence` |
| Belegfotos lokal und unter Dateischutz | Kopie nach `<Datenverzeichnis>/receipts/<Guid>.<Endung>`, gespeichert wird nur der Dateiname (Pfadschutz über `IsValidFileName`). `IDatabaseFileProtector.ProtectFile` setzt unter iOS `CompleteUntilFirstUserAuthentication` auf Datei und Verzeichnis, wie bei der Datenbank. Signaturprüfung JPEG/PNG/HEIF, höchstens 20 MB. | Unit `Import_SupportedPhoto_CopiesAndProtects`, `Import_ContentWithoutPhotoSignature_IsRejected`, `Import_TooLarge_…`; E2E `_Receipt` (Datei bytegleich unter `receipts/`, Name `^[0-9a-f]{32}\.png$`) |
| Foto wird mit dem Vorgang bzw. Fahrzeug gelöscht | Vorgang löschen, Foto ersetzen und Fahrzeug löschen (Kaskade) entfernen die Dateien nach dem Commit. Ausstehende Fotos werden bei Abbrechen, Ersetzen oder einer verspäteten Auswahl gelöscht. Verwaiste Dateien werden beim ersten Laden je Prozess bereinigt. | Unit `DeleteEntry_RemovesRowAndPhoto`, `UpdateEntry_ReplacedPhoto_DeletesOldFile`, `DeleteVehicle_WithEntries_DeletesReceiptPhotos`, `GetEntries_FirstCall_DeletesUnreferencedPhotos`, `Cancel_DeletesPendingPhoto`; Integration `TankbookEntryServiceTests_RealDatabase`; E2E `_Receipt` (danach kein Foto mehr) |
| Kein Upload, keine Weitergabe an Dritte | Tankbuch und Fotoablage ohne `HttpClient` und ohne URL. Protokolle nennen nur Ausnahmetypen. | Code-Durchsicht des Diffs (keine URL außer XAML-Namensräumen) |
| Vollständig offline nutzbar | Das Tankbuch hängt nicht von der Verbindung ab. | E2E `OfflineE2ETests_Tankbook`; alle `TankbookEntriesE2ETests` laufen ohne Mock-Server |
| Migration erhält Bestandsdaten | `20261011170920_AddTankbookEntryDetails`: nur `ADD COLUMN` mit Standardwerten plus Indexwechsel. Datenübernahme: Zeitpunkt = `CreatedUtc` als Ortszeit auf die Minute, km = Start-km, Elektro → Strom. | Integration `TankbookEntriesMigrationTests_Upgrade`, `DatabaseInitializerTests_*` (7 Migrationen), `MigrationsTests_ModelSync`; praktischer Upgrade 0.1.16 → 0.1.17 (siehe unten) |
| Tests: Validierung und Verbrauch mit Randfällen; FlaUI Erfassen + Verbrauch | Abgedeckt: erster Eintrag, Teilbetankung (auch vor dem ersten und nach dem letzten Vollvorgang), mehrere Teilbetankungen, kWh, Hybrid-Ketten, Strecke 0, ungeordnete Eingabe | siehe oben; E2E `_Flow` |
| FlaUI-Offline-Szenario | Favorit anlegen, Neustart mit geschlossenem Port. Geprüft werden Startseiten-Banner, Preis „1,859 €/L“ mit Alter, Detail-Banner mit Preis und Alter, dann im Tankbuch: Fahrzeug anlegen, Vorgang mit bekannter Tankstelle, zweiten Vorgang, Verbrauch, Korrektur und Löschen. | E2E `OfflineE2ETests_Tankbook` (Bewertung siehe unten) |
| Zusatz 12a: Neuzählung beim Löschen | `DeleteVehicleAsync(id, expectedEntryCount)` zählt in derselben Transaktion. Bei Abweichung wird nicht gelöscht, die Rückfrage nennt die neue Zahl, wird angesagt und braucht eine erneute Bestätigung. | Unit `DeleteVehicle_EntryCountChanged_KeepsVehicleAndReportsCount`, `Delete_EntryAddedMeanwhile_ShowsUpdatedQuestionAndKeepsVehicle`; E2E `TankbookE2ETests_DeleteWithEntries.EntryAddedBeforeConfirming_IsNamedBeforeDeleting` (2 → 3, Fahrzeug bleibt, dann gelöscht) |
| Zusatz 12b: `Foreign Keys=True` | `TankradarDbContext.BuildConnectionString` (`SqliteConnectionStringBuilder.ForeignKeys = true`), genutzt von App und Integrations-`TestDatabase` | Unit `TankradarDbContextTests_ConnectionString`; Integration `ForeignKeysTests_Connection` |
| Zusatz 12c: WCAG 2.5.3 | An Schaltflächen mit Text keine `SemanticProperties.Description` mehr; der Kontext steht im Hint. Gilt für „Ja, löschen“/„Fahrzeug und Vorgänge löschen“, „Abbrechen“, „Bearbeiten“/„Löschen“ der Fahrzeugkarte, „Tank- und Ladevorgänge“ und alle neuen Schaltflächen (Vorgang erfassen, Foto auswählen/aufnehmen/entfernen, Beleg anzeigen, Bearbeiten, Löschen, Ja, löschen, Abbrechen). | Unit `Delete_ButtonsNameIsVisibleTextAndHintNamesVehicle`, `Buttons_NameIsVisibleTextAndHintGivesContext`; E2E prüfen Name = sichtbarer Text und Hint (`ReadHint`) |
| Windows-Zwischenstand | `review-versions/0.1.17_2026-10-11/` | siehe „Zwischenstand und Upgrade“ |
| iOS | `MtouchInterpreter`/`TrimmerRootAssembly` in `Tankradar.MAUI.csproj` unverändert (die Datei ist nicht im Diff). Neue Entitätseigenschaften und Typen liegen in der App-Assembly (`TrimmerRootAssembly Include="Tankradar.MAUI"`). `MediaPicker` wird statisch aufgerufen (MAUI Essentials). `Info.plist` (iOS und MacCatalyst) enthält `NSCameraUsageDescription`/`NSPhotoLibraryUsageDescription`. | Unit `PlatformManifestTests_Photos`; iOS-Compile-Prüfung in `local-ci` |
| `PrivacyInfo.xcprivacy` | Keine Änderung nötig: Fotos verlassen das Gerät nicht (keine „Collected Data Types“), und Datei-Zeitstempel (`C617.1`) sind bereits deklariert. | Durchsicht |
| Dokumentation | ADR 0009, ADR 0008 „Folgen“, `docs/help/Tankbuch/` (4 Dateien), `docs/help/index.md`, `changes.log` | Durchsicht |

### Fachliche Bewertung der Verbrauchsberechnung

- **Methode:** Volltank-zu-Volltank. Für einen Vollvorgang V mit dem vorherigen Vollvorgang P derselben Art gilt:
  (Menge V + Mengen der Teilbetankungen derselben Art zwischen P und V) / (km V − km P) × 100, auf zwei Nachkommastellen
  gerundet. Das entspricht der Formel der Anforderung und ergänzt sie korrekt um die Teilbetankungen. Nach der Methode
  wird die zwischen zwei Vollzuständen getankte Gesamtmenge auf die Strecke umgelegt.
- **Erste Betankung:** ohne Wert („Noch kein Verbrauch: erster voller Vorgang“). Das ist korrekt, weil der Füllstand vor
  dem ersten Vollvorgang unbekannt ist. Teilbetankungen davor fließen folgerichtig nicht ein. Der Start-Verbrauch des
  Fahrzeugs ist für Schritt 14 vorgesehen.
- **Teilbetankungen:** Sie haben keinen eigenen Wert, ihre Menge zählt beim nächsten Vollvorgang derselben Art. Eine
  Teilbetankung nach dem letzten Vollvorgang bleibt offen, bis der nächste Vollvorgang kommt.
- **Strecke ≤ 0:** kein Wert („keine Strecke seit dem letzten vollen Vorgang“). Dieser Vorgang wird neuer Bezugspunkt.
  Wegen der Kilometerregel kann die Strecke nicht negativ werden.
- **Strom und Kraftstoff:** getrennte Ketten, Einheiten werden nie gemischt (L/100 km bzw. kWh/100 km). Bei Plug-in-Hybriden
  ist die Strecke einer Kette die Gesamtstrecke zwischen zwei Vollvorgängen derselben Art. Das ist eine vertretbare,
  dokumentierte Vereinfachung.
- **Wechsel der Kraftstoffart:**
  - Vorhandene Vorgänge behalten ihre Art und Einheit. Beim Korrigieren bleibt die gespeicherte Art wählbar, bei
    abweichender Art zeigt die Liste sie an.
  - Neue Vorgänge folgen der neuen Kraftstoffart. Beim Wechsel z. B. von Benzin zu Elektro beginnt die Stromkette mit
    einem ersten Vollvorgang ohne Wert.
  - Benzin ↔ Diesel bleibt in derselben Kraftstoffkette.
  - Nachweis: Unit `UpdateEntry_StoredKindStaysAllowedAfterVehicleKindChange`, `UpdateVehicle_FuelKindChangeWithEntries_IsAllowed`.
- **Korrekturen und Löschungen:** Weil nichts gespeichert wird, sind alle Folgewerte immer konsistent. Ein Vorgang, der
  durch Korrektur von Datum oder Kilometern umsortiert wird, wird gegen seine neuen Nachbarn geprüft (`CheckOdometer`
  schließt ihn selbst aus).

### Bewertung: Rundungsregel abweichend vom Plan

- **Plan:** Fehler, wenn bei drei Werten \|Menge × Preis − Gesamt\| > 0,02 €.
- **Umsetzung (ADR 0009):** Zusätzlich gelten drei Werte als stimmig, wenn einer davon genau der gerundete Quotient der
  beiden anderen ist.
- **Bewertung:** Die Abweichung ist sachlich notwendig und korrekt dokumentiert.
  - Ein aus Menge und Gesamtbetrag berechneter Preis (3 Nachkommastellen) erzeugt bei großen Mengen einen Rundungsfehler
    bis 0,0005 € × Menge. Bei 80 L sind das bis zu 0,04 €.
  - Nach der reinen Planregel ließe sich ein so gespeicherter Vorgang beim Korrigieren nicht mehr speichern.
  - Die Ausnahme lässt nur Abweichungen innerhalb der Rundungsgenauigkeit zu. Tippfehler werden weiterhin gemeldet (E2E:
    75,00 statt 74,36 €).
- **Kleinigkeit:** `docs/help/Tankbuch/beschreibung.md` nennt nur die 0,02-€-Regel, nicht die Ausnahme. Für Anwender ist
  das in der Praxis bedeutungslos. ADR 0009 beschreibt die Ausnahme vollständig.

### Bewertung: Offline-E2E setzt `DetailsUpdatedUtc` per SQL zurück

**Mechanik:**

- `FuelPriceService.GetStationDetailAsync` liefert Details, die jünger als `CacheLifetime` (5 Min.) sind, direkt aus dem
  Cache, ohne Abruf und damit ohne Offline-Hinweis. Das ist bewusstes Verhalten aus früheren Schritten (Cache zuerst,
  Abrufe drosseln).
- Im Test sind die Details erst Sekunden alt.
- `AgeStationDetails()` setzt `Stations.DetailsUpdatedUtc` auf „vor einer Stunde“. Erst dadurch versucht die App beim
  Öffnen einen Abruf. Er scheitert am geschlossenen Port, und die Detailansicht zeigt `Detail.OfflineBanner`.

**Legitim?** Ja.

- Der Eingriff ersetzt nur das Verstreichen von Zeit in den Testdaten. Er entspricht dem realen Fall, dass ein Nutzer
  offline Details öffnet, die älter als 5 Minuten sind.
- Er umgeht keine App-Logik: Abruf, Fehlerpfad, `OfflineFallback` und Anzeige laufen echt.
- Preise und Preisalter (`PriceEntries.RetrievedUtc`) bleiben unberührt; die Altersangabe „vor X Min.“ wird weiterhin
  echt geprüft.
- Die Details bleiben innerhalb von `DetailFreshness.MaxAge` anzeigbar.
- Der Eingriff ist im Test kommentiert und auf die isolierte Testdatenbank beschränkt.

**Restpunkt (optional):**

- Die Änderung erfolgt, während die App die Datenbank offen hat. Das ist unkritisch, weil offline niemand
  `DetailsUpdatedUtc` schreibt; in 4 Läufen gab es keinen Ausfall.
- Robuster wäre die Änderung vor dem Neustart bei beendeter App oder eine im Testmodus injizierbare Zeitquelle.

### Praktische Verifikation

**`scripts/local-ci.ps1`**

- Vollständig von 22:38 bis 22:46 (2026-10-11), E2E off-screen.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` aus der
  Umgebung entfernt, ohne sie zu lesen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK (0 Warnungen; `Tankradar.MAUI.dll` in diesem Lauf neu geschrieben) |
| Unit | 1028/1028 (Schritt 12: 889) |
| Integration | 120/120 (Schritt 12: 116; neu: `TankbookEntriesMigrationTests_Upgrade`, `TankbookEntryServiceTests_RealDatabase` 2, `ForeignKeysTests_Connection`) |
| Zeilenabdeckung | 93,1 % (Schwelle 70 %) |
| FlaUI-E2E | **90/90** in 6 min 1 s, darunter alle 7 `TankbookEntriesE2ETests_*`, `OfflineE2ETests_Tankbook`, alle `TankbookE2ETests_*` und beide `DiagnosticsCaptureE2ETests`. Kein Umgebungseffekt „Screenshot einfarbig“. |
| iOS-Compile-Prüfung (`net10.0-ios`) | OK |
| Gesamt | Exit-Code 0, „Lokaler Prüflauf erfolgreich.“ |

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei einem Push nach `refs/heads/task/…-tankradar`: lokal `f7148b9`, Remote-Stand `4351395`.
  - Schlüsselvariablen wie oben entfernt.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py --base 4351395 --end f7148b9` | OK (7 Commits, einschließlich Merge `70983f1 merge: …`) |
| `forbidden-patterns-check.py --all --strict` | OK (721 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (503 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (499 Dateien) |
| `test-execution-check.py` (Build Debug, Unit, Integration; E2E im Hook standardmäßig aus) | OK: Unit 1028/1028, Integration 120/120 |
| Gesamt | Exit-Code 0, „Pre-Push-Prüfungen erfolgreich abgeschlossen.“ |

Zusätzlich meldet `conventional-commits-check.py --base main --end HEAD` OK (126 Commits).

**Wiederholte E2E-Läufe (Flakiness)**

Filter: `TankbookEntriesE2ETests|OfflineE2ETests_Tankbook|TankbookE2ETests`, also alle neuen und geänderten E2E-Klassen
mit 15 Tests. Release, `--no-build`, off-screen, nacheinander.

| Lauf | Ergebnis | Dauer |
|---|---|---|
| 1 | 15/15 | 1 min 59 s |
| 2 | 15/15 | 2 min 0 s |
| 3 | 15/15 | 1 min 57 s |

Zusammen mit `local-ci` lief jede neue E2E-Klasse viermal ohne Ausfall.

### Zwischenstand und Upgrade

**Zwischenstand `review-versions/0.1.17_2026-10-11/`**

- **Herkunft:** erzeugt um 22:36, nach HEAD (22:21), und per `.gitignore` ausgeschlossen. `FileVersion` ist `0.1.17.0`,
  `ProductVersion` ist `0.1.0+f7148b92…` und entspricht damit HEAD.
- **`CHANGELOG.md`:** UTF-8 mit BOM, ohne „Ã“/„â€““-Folgen. Umlaute und typografische Anführungszeichen sind korrekt, der
  Inhalt entspricht dem `changes.log`-Eintrag von Schritt 13.
- **Frischer Start** (Testmodus, `TANKATLAS_TEST_WINDOW=offscreen`, eigenes Datenverzeichnis im Scratchpad, ohne
  Schlüssel- und Dienstadressen-Variablen):
  - PID 32740 reagierte, Fenstertitel „Tankatlas“.
  - 7 Migrationen bis `AddTankbookEntryDetails`, Index `IX_TankbookEntries_VehicleId_OccurredAt`, Fremdschlüssel
    `CASCADE`, `integrity_check` = ok.
  - Danach per PID beendet.

**Upgrade 0.1.16 → 0.1.17 mit Bestandsdaten**

1. Zwischenstand 0.1.16 gestartet (PID 35740, reagierte, Titel „Tankatlas“), Datenbank bis `AddVehicles` angelegt, per
   PID beendet.
2. Synthetische Bestandsdaten eingefügt:
   - Einstellungen (`ShowNearby = 1`, Karte, Sortierung nach Name), Kraftstoffsorte, Tankstelle, Preis, Favoritengruppe
     und -eintrag
   - drei Fahrzeuge: Golf/Benzin mit Startverbrauch 5,4; Zoe/Elektro; Passat/Diesel mit 6,25
   - drei Altvorgänge aus dem Schema von Schritt 12, einer davon mit `CreatedUtc` samt Sekundenbruchteilen
3. Zwischenstand 0.1.17 auf derselben Datei gestartet (PID 28128, reagierte, Titel „Tankatlas“), per PID beendet.

**Ergebnis:**

- `AddTankbookEntryDetails` ist angewendet. Alle Bestandsdaten (Einstellungen, Preise, Favoriten, Fahrzeuge einschließlich
  der Startverbräuche als exakter Text) sind unverändert.
- Die Altvorgänge haben alle neuen Spalten mit Standardwerten:
  - `OccurredAt` = Ortszeit auf die Minute, z. B. `10:00:42.1234567` UTC → `12:00:00`
  - `OdometerKm` = Start-km des Fahrzeugs
  - `Kind` = `Electricity` beim Elektrofahrzeug, sonst `Fuel`
- Der alte Index ist ersetzt, der Fremdschlüssel steht weiter auf `CASCADE`. `integrity_check` = ok, `foreign_key_check`
  ist leer.

Danach lief kein `Tankradar.MAUI`-Prozess mehr. Beendet wurden nur die eigenen drei Prozesse. Ein noch laufender
`testhost.exe` gehört zu einem anderen Repository und wurde nicht angefasst.

### Datenschutz

- **Nur lokal:**
  - Vorgänge liegen in der lokalen SQLite-Datenbank, Belegfotos unter `receipts/` im Datenverzeichnis.
  - Der Diff enthält keinen `HttpClient` und keine URL außer XAML-Namensräumen.
  - Alle neuen Protokollaufrufe nennen nur `{ExceptionType}` oder gar keine Werte.
  - Einzige Ausnahme ist `DatabaseFileProtector` mit `{Path}` auf Debug-Ebene. Der Debug-Logger ist nur in
    `DEBUG`-Builds registriert, der Dateiname ist eine GUID.
- **Keine Produktivendpunkte in Tests:**
  - Die Unit-Tests nutzen SQLite im Arbeitsspeicher bzw. Fakes.
  - Die Integrationstests nutzen Dateien im isolierten Testverzeichnis.
  - Die Tankbuch-E2E-Tests laufen ohne Mock-Server; das Offline-Szenario nutzt den lokalen Mock bzw. einen geschlossenen
    Port.
  - Die Testfotos werden zur Laufzeit als PNG im Temp-Verzeichnis erzeugt.
  - Die praktischen Starts liefen im Testmodus ohne Dienstadressen.
- **API-Schlüssel:**
  - Im Diff stehen nur die synthetischen Kennungen `00000001-…`/`00000002-0000-4000-8000-000000000000`.
  - `forbidden-patterns-check.py --all --strict` ist grün.
  - Die Protokolle dieser Abnahme enthalten keine `apikey=`-Fundstelle.
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben, sondern nur per `env -u` bzw. `ProcessStartInfo.Environment.Remove` entfernt.
- **Testdaten:** Die Testdaten des Upgrade-Tests sind synthetisch und enthalten keine personenbezogenen Daten.
- **iOS-Sicherung:** Wie die Datenbank liegen die Belegfotos unter `FileSystem.AppDataDirectory` und damit in der vom
  Nutzer gesteuerten Gerätesicherung. Das ist keine Weitergabe an Dritte. Die Sicherung der Fotos in der App folgt in
  Schritt 15 (ADR 0009 „Folgen“).

### Kleinigkeiten ohne Einfluss auf die Erfüllung

- **Bewusst offen gelassen (Code-Review `review-code.md`, niedrig):**
  - `TankbookEntryFormViewModel` hat 1114 Zeilen. Empfehlung: den Foto-Lebenszyklus in ein eigenes ViewModel auslagern.
  - `TankbookEntryValidator.ValidateForm` hat 12 Parameter. Empfehlung: ein Parameterobjekt einführen.
  - `ReceiptPhotoStore.DetectExtension` ist `public` und nicht direkt getestet; die Marken `mif1`/`msf1` sind ungetestet.
  - Der Zweig „fachlicher Löschfehler“ in `TankbookEntriesViewModel.DeleteAsync` ist ohne Test. Er ist derzeit praktisch
    unerreichbar, weil `DeleteEntryAsync` nur `Ok` oder `EntryNotFound` liefert, und `EntryNotFound` gilt als erledigt.
  - Alles das sind sinnvolle Aufräumpunkte für einen späteren Schritt.
- **Hinweistext Teilbetankung:** „Teilbetankung: zählt beim nächsten vollen Vorgang mit“ erscheint auch bei einer
  Teilbetankung vor dem ersten Vollvorgang. Deren Menge zählt nicht, weil der nächste Vollvorgang als erster ohnehin
  keinen Wert hat. Fachlich ohne Folgen.
- **Hilfetext zur Plausibilität:** Die Ausnahme „gerundeter Quotient“ fehlt (siehe oben).

### Sonstiges

- **Dokumentation des Feature-Verzeichnisses:** `docs/features/task/…--schritt-13-tankvorgaenge/` und die getrackte
  `…-schritt-13-tankvorgaenge-tasks.md` (lokal geändert) sind noch vorhanden. Das Löschen wurde vom Berechtigungssystem
  abgelehnt und wird separat mit dem Anwender geklärt. Das ist keine Abweichung. Das unversionierte Verzeichnis aus
  Schritt 2 liegt weiterhin dort.
- **Abgelehnte Aktion in dieser Abnahme:**
  - Ein kombinierter Befehl (Löschen eines Testdaten-Ordners im Scratchpad per `rm -rf` plus Start von 0.1.16) wurde vom
    Berechtigungssystem abgelehnt.
  - Das Löschen wurde nicht wiederholt; der Ordner existierte ohnehin noch nicht.
  - Der Start lief danach als eigener Befehl.
  - Die Testdaten der Starts liegen weiter im Scratchpad (`abn13/`).
- **Keine Änderungen am Repository:**
  - Es gab keine Commits, keine Pushes und keine Codeänderungen. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert (keine Datei jünger als der Beginn der
    Abnahme). Der Designentwurf ist nur im Scratchpad entpackt.
  - Testergebnisse liegen in den per `.gitignore` ausgeschlossenen Ordnern `TestResults/` und `coverage-report/` sowie im
    Scratchpad.
