# ADR 0009: Tank- und Ladevorgänge im Tankbuch

## Status

Angenommen (2026-10-11)

## Kontext

Schritt 13 baut die in Schritt 12 minimal angelegte Tabelle `TankbookEntries` zum vollständigen Tank- bzw. Ladevorgang aus:
Datum und Uhrzeit, Tankstelle (bekannte Tankstelle oder freier Name), Art (Kraftstoff in Litern, Strom in kWh), Menge, Preis
pro Einheit, Gesamtbetrag, Kilometerstand, Teilbetankung, Notiz und Belegfoto. Je Vollvorgang wird der Verbrauch pro 100 km
angezeigt. Alles läuft lokal und offline. Dazu kommen drei Restpunkte aus der Abnahme von Schritt 12: Neuzählung beim
Bestätigen der Fahrzeug-Löschung, `Foreign Keys=True` und WCAG 2.5.3 „Label in Name“. Die Gesamtanforderung lässt mehrere
Auslegungsfragen offen; sie sind hier entschieden. Grundlage ist [ADR 0008](0008-vehicle-management.md).

## Entscheidungen

- **Eigene Seite je Fahrzeug.** Die Vorgänge stehen auf der Seite „Tank- und Ladevorgänge“ (Shell-Route `tankbookentries`,
  Parameter `vehicleId`). Geöffnet wird sie über die Schaltfläche „Tank- und Ladevorgänge“ auf der Fahrzeugkarte. Das
  Fahrzeug wird also per Namen gewählt, nie per Kennung. Formular und Löschrückfrage sind Bereiche in der Seite (wie ADR 0006
  und 0008).
- **„Chronologisch“ heißt neueste zuerst** (wie „Letzte Tankvorgänge“ im Designentwurf). Der Ordnungsschlüssel ist
  (Zeitpunkt, Kilometerstand, Kennung) und damit auch bei gleicher Minute eindeutig. Berechnet wird intern aufsteigend.
- **Zeitpunkt als lokale Wanduhrzeit** (`DateTime` ohne Zeitzone, minutengenau). Er wird mit „jetzt“ vorbelegt
  (`DatePicker`/`TimePicker`). Zeitpunkte später als jetzt plus eine Minute sind unzulässig. Eine Zeitzonenumrechnung würde
  die Uhrzeit vom Beleg verfälschen.
- **Zwei aus drei, blockierende Abweichung.** Aus zwei der Werte Menge, Preis pro Einheit und Gesamtbetrag ergibt sich der
  dritte, kaufmännisch gerundet: Gesamtbetrag auf 2, Preis auf 3, Menge auf 2 Nachkommastellen. Das Formular zeigt das
  Ergebnis live („Ergibt 74,36 €“) und übernimmt es beim Speichern ins leere Feld. Sind alle drei angegeben und weicht
  Menge × Preis um mehr als 0,02 € vom Gesamtbetrag ab, wird nicht gespeichert. Der Fehler steht am Gesamtbetrag und nennt den
  rechnerischen Betrag. Als stimmig gelten auch Werte, die genau der gerundete Quotient der beiden anderen sind. Sonst ließe
  sich ein berechneter Preis bei großen Mengen beim Korrigieren nicht mehr speichern. Mit weniger als zwei Werten erscheint
  „mindestens zwei der drei Werte“. Belege mit Zusatzgebühren erfasst man mit Menge und Gesamtbetrag.
- **Verbrauch beim Lesen berechnet** (`ConsumptionCalculator`, nicht gespeichert). Je Art gibt es eine eigene Kette, damit
  Hybridfahrzeuge keine Einheiten mischen: (Menge + Mengen der Teilbetankungen seit dem vorherigen Vollvorgang) / Strecke
  × 100, auf 2 Nachkommastellen. Der erste Vollvorgang, eine Strecke ≤ 0 und Teilbetankungen haben keinen Wert.
  Teilbetankungen vor dem ersten Vollvorgang fließen nicht ein. Korrekturen und Löschungen bleiben dadurch automatisch
  konsistent.
- **Art je Kraftstoffart:** Benzin und Diesel nur Kraftstoff, Elektro nur Strom, Hybrid beides (Chips, Kraftstoff
  vorgewählt). Ein Wechsel der Kraftstoffart eines Fahrzeugs mit Vorgängen bleibt erlaubt. Vorhandene Vorgänge behalten Art
  und Einheit, und beim Korrigieren bleibt die gespeicherte Art wählbar. Damit ist die offene Frage aus ADR 0008 „Folgen“
  beantwortet.
- **Kilometerstand:** nicht unter dem Start-Kilometerstand und dem des chronologischen Vorgängers (Gleichstand erlaubt) und
  nicht über dem des Nachfolgers. So macht ein nachgetragener oder korrigierter Vorgang den nächsten nicht ungültig. Die
  Prüfung liegt im Dienst und meldet den Grenzwert („nicht unter 10.500 km (vorheriger Vorgang)“). Umgekehrt lehnt
  `UpdateVehicleAsync` einen Start-Kilometerstand über dem niedrigsten Vorgang ab (`StartOdometerAboveEntries`).
- **Tankstelle ist Pflicht** (1–60 Zeichen): freier Name oder ein Chip aus „Bekannte Tankstellen“. Dazu zählen alle
  Favoriten-Tankstellen und danach bereits verwendete freie Namen, ohne Duplikate und höchstens 30. Bei gleichem Namen zeigt
  der Chip zusätzlich den Ort. Gespeichert werden der Name als Momentaufnahme und nur bei einer Favoriten-Tankstelle deren
  Kennung, **ohne Fremdschlüssel**. So bleibt der Vorgang unabhängig vom Preis-Cache und von der Sicherung. Ändert man den
  Namen von Hand, entfällt die Kennung.
- **Belegfoto** über `IReceiptPhotoPicker`: `MediaPicker` (Fotomediathek; „Foto aufnehmen“ nur mit Kamera). `PickPhotoAsync`
  ist in .NET MAUI 10 veraltet, deshalb `PickPhotosAsync` mit `SelectionLimit = 1`. Im Testmodus liefert
  `TestReceiptPhotoPicker` die Datei aus `TANKATLAS_TEST_RECEIPT_PHOTO`. Das Foto wird unverändert nach
  `<Datenverzeichnis>/receipts/<Guid>.<Endung>` kopiert und erhält den Dateischutz der Datenbank
  (`IDatabaseFileProtector.ProtectFile`, iOS `CompleteUntilFirstUserAuthentication`). Gespeichert wird nur der Dateiname.
  Erlaubt sind JPG, PNG und HEIC/HEIF bis 20 MB. Der Typ wird zusätzlich an der Dateisignatur geprüft (JPEG `FF D8 FF`,
  PNG `89 50 4E 47 …`, HEIF `ftyp` mit HEIF-Marke); Dateien ohne passende Signatur werden abgelehnt, ein falsch benanntes Foto
  erhält die Endung seines tatsächlichen Formats. Während einer laufenden Auswahl ist „Foto auswählen“ gesperrt; wird das
  Formular inzwischen geschlossen oder neu geöffnet, wird das nachträglich übernommene Foto sofort gelöscht. Ein ausgewähltes,
  noch nicht gespeichertes Foto wird beim Abbrechen oder Ersetzen gelöscht. Ersetzte und mitgelöschte Fotos werden nach dem Speichern entfernt. Beim ersten Laden je Prozess werden
  nicht referenzierte Dateien bereinigt.
- **Dienstschicht** `ITankbookEntryService` (Load-and-Patch über `TankbookEntryEntity.Apply`, Ergebnis-Enums statt
  Ausnahmen, Protokolle nur mit Ausnahmetyp).
- **Migration `AddTankbookEntryDetails`** ist rein additiv (`ALTER TABLE … ADD COLUMN` mit Standardwerten, Index
  (`VehicleId`, `OccurredAt`)). Bestandszeilen erhalten als Zeitpunkt `CreatedUtc` in
  Ortszeit auf die Minute gekürzt (`strftime('%Y-%m-%d %H:%M:00', CreatedUtc, 'localtime')`, wie neu erfasste Vorgänge), den
  Start-Kilometerstand des Fahrzeugs und bei Elektrofahrzeugen die Art „Strom“.
- **Zusatz 12a – Neuzählung:** `DeleteVehicleAsync(vehicleId, expectedEntryCount)` zählt in derselben Transaktion. Weicht
  die Zahl von der in der Rückfrage genannten ab, wird nicht gelöscht. Die Rückfrage nennt dann die neue Zahl, wird erneut
  angesagt und braucht eine weitere Bestätigung.
- **Zusatz 12b:** `TankradarDbContext.BuildConnectionString` setzt `Foreign Keys=True`. Die App und die Integrationstests
  nutzen dieselbe Verbindungszeichenfolge.
- **Zusatz 12c – WCAG 2.5.3:** Der vorgelesene Name einer Schaltfläche ist ihr sichtbarer Text. Der Kontext steht
  ausschließlich in `SemanticProperties.Hint`, z. B. „Ja, löschen“ mit dem Hinweis
  „Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen“. Das gilt für die Rückfrage, „Abbrechen“ des
  Fahrzeugformulars sowie „Bearbeiten“ und „Löschen“ der Fahrzeugkarte (Hinweis „Fahrzeug „Golf““; die bisherigen
  Beschreibungen „Golf bearbeiten“/„Golf löschen“ entfallen) und für alle neuen Schaltflächen dieses Schritts.
  Statusmeldungen der Vorgangsseite (Lade- und Löschfehler) werden zusätzlich angesagt. Chips behalten „Ausgewählt“ im Hinweis.

## Abweichungen vom Designentwurf

- **Keine Karte „Aktives Fahrzeug“ mit Auswahlliste:** Das Fahrzeug wird über seine Karte im Tankbuch gewählt, die Vorgänge
  stehen auf einer eigenen Seite.
- **Monatsanalyse, Verlauf und Statistik** folgen in Schritt 14, der **Export** in Schritt 16.
- **Sync-Banner** entfällt; Cloud-Synchronisation ist nicht Teil von Version 1.0.
- **„Alle 42 anzeigen“** entfällt, weil die Seite alle Vorgänge zeigt.
- Die Eintragskarte übernimmt den Entwurf: Tankstelle und Gesamtbetrag, darunter Datum und Preis pro Einheit, dann Menge,
  Strecke, Verbrauch bzw. Grund ohne Verbrauch, Notiz in Anführungszeichen und „Beleg anzeigen“. Hinzu kommen die
  Kennzeichnung „Teilbetankung“ und bei Hybridfahrzeugen die Art.

## Folgen

- Schritt 14 wertet die Vorgänge aus (Monatsanalyse, Verlauf) und nutzt dafür `ConsumptionCalculator` und `TankbookEntryOrder` (siehe [ADR 0010](0010-tankbook-statistics.md)).
- Schritt 15 (Sicherung) muss die Belegfotos aus `receipts/` mitsichern; die Vorgänge referenzieren sie nur per Dateinamen.
- Unter Windows kann die Vorschau eines HEIC-Fotos ohne HEIF-Erweiterung leer bleiben; das Foto bleibt gespeichert.
