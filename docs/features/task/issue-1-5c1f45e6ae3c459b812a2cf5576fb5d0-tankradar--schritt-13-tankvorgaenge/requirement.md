# Anforderung – Schritt 13: Tank- und Ladevorgänge erfassen

Quelle: `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Abschnitt
„Schritt 13: Tank- und Ladevorgänge erfassen“ einschließlich „Zusatz aus der Abnahme von Schritt 12“. Weitere Grundlagen:
Tabelle „Grobe Vorgehensentscheidungen“ des Projektplans, `requirement.md` des Projekts (Modell „Fuel Entry / Charge
Entry“), `acceptance-schritt-12.md`, ADR 0008, Designentwurf (`design-draft/…zip`, Screen `tankbuch_verbrauchsauswertung`,
`DESIGN.md` „Tankbuch-Formulare“).

## Fachliche Zusammenfassung

Die in Schritt 12 minimal angelegte Tabelle `TankbookEntries` (nur `Id`, `VehicleId`, `CreatedUtc`) wird per additiver
Migration zum vollständigen Tank- bzw. Ladevorgang ausgebaut. Für jedes Fahrzeug können Nutzer Vorgänge mit Datum/Uhrzeit,
Tankstelle (Auswahl aus bekannten Tankstellen oder freier Name), Art (Kraftstoff in Litern oder Strom in kWh), Menge,
Preis pro Einheit, Gesamtbetrag, Kilometerstand, Kennzeichen „Teilbetankung“, optionaler Notiz und optionalem Belegfoto
erfassen, chronologisch einsehen, korrigieren und löschen. Ein reiner Rechenbaustein ermittelt zu jedem Vollvorgang den
Verbrauch pro 100 km aus der seit dem vorherigen Vollvorgang gefahrenen Strecke; Teilbetankungen fließen mit ihrer Menge in
den nächsten Vollvorgang ein. Alles läuft lokal und offline; Belegfotos liegen im App-Datenverzeichnis unter dem
iOS-Dateischutz. Zusätzlich werden drei Restpunkte aus der Abnahme von Schritt 12 erledigt (Neuzählung beim Bestätigen der
Fahrzeug-Löschung, `Foreign Keys=True`, WCAG 2.5.3 „Label in Name“).

## Fachpunkte

1. **Vorgang erfassen, einsehen, korrigieren, löschen** – je Fahrzeug; Löschen nur nach Rückfrage (Muster aus Schritt 12).
2. **Angaben eines Vorgangs** – Datum und Uhrzeit; Fahrzeug; Tankstelle; Art (Kraftstoff/Strom) mit Menge in L bzw. kWh;
   Preis pro Einheit; Gesamtbetrag; Kilometerstand; Kennzeichen „Teilbetankung“ (Standard: aus = Vollvorgang); optional
   Notiz und Belegfoto. Alle Werte gibt der Nutzer selbst ein; es gibt keine Strompreisquelle.
3. **Auswahl der Tankstelle (Auswahl-/Identifikationsanforderung)** – Der Nutzer wählt eine Tankstelle **per Namen** aus
   einer Liste bekannter Tankstellen (mindestens die Tankstellen seiner Favoriten) **oder** gibt einen Namen frei ein
   (z. B. Ladeort „Zuhause“). Interne Kennungen (Tankerkönig-ID) werden nie angezeigt oder eingegeben.
4. **Auswahl des Fahrzeugs (Auswahl-/Identifikationsanforderung)** – Vorgänge werden einem Fahrzeug über dessen Namen bzw.
   Listeneintrag zugeordnet, nie über eine Kennung.
5. **Plausibilität Menge/Preis/Gesamtbetrag** – Aus zwei Werten ergibt sich der dritte; sind alle drei angegeben und weicht
   Menge × Preis pro Einheit vom Gesamtbetrag ab, wird die Abweichung gemeldet.
6. **Kilometerstand** – darf nicht unter dem des vorherigen Eintrags (chronologisch) bzw. dem Start-Kilometerstand des
   Fahrzeugs liegen.
7. **Verbrauch** – zu jedem Vollvorgang: (Menge / seit dem vorherigen Vollvorgang gefahrene Strecke) × 100, in L/100 km bzw.
   kWh/100 km. Teilbetankungen erhalten keinen eigenen Wert; ihre Menge wird dem nächsten Vollvorgang zugerechnet.
8. **Darstellung** – Einträge eines Fahrzeugs chronologisch.
9. **Offline, Datenschutz** – vollständig offline nutzbar; Belegfotos nur lokal und per systemseitigem Dateischutz
   geschützt; keine Weitergabe an Dritte.
10. **Tests** – Unit-Tests für Validierung und Verbrauchsberechnung (erster Eintrag, Teilbetankungen, kWh), FlaUI-Test
    „Tankvorgang erfassen → Verbrauch wird angezeigt“, FlaUI-Offline-Szenario (Mock-Preisdienst nicht erreichbar):
    Startseite und Detailansicht zeigen zwischengespeicherte Preise mit Altersangabe und Offline-Hinweis; Tankbuch voll
    nutzbar (Fahrzeug anlegen, Vorgang erfassen, Verbrauch ansehen, Vorgang korrigieren und löschen).
11. **Zwischenstand** – startfähiger Windows-Stand unter `review-versions/<Version>_<JJJJ-MM-TT>/` (nicht committen).
12. **Zusatz aus Abnahme 12:**
    - a) Löschrückfrage Fahrzeug: Zahl der mitzulöschenden Vorgänge beim Bestätigen erneut ermitteln; ist ein Vorgang
      hinzugekommen, muss die Rückfrage ihn nennen (nicht stillschweigend mitlöschen).
    - b) `Foreign Keys=True` ausdrücklich in der Verbindungszeichenfolge.
    - c) WCAG 2.5.3: Der vorgelesene Name von „Ja, löschen“, „Fahrzeug und Vorgänge löschen“ und „Abbrechen“
      (Fahrzeugformular) beginnt mit dem sichtbaren Text, Kontext steht im Hint; dasselbe Muster für alle neuen
      Schaltflächen dieses Schritts.

## Betroffene Klassen und Komponenten

### Datenmodell (EF Core, SQLite)
- `Data/TankbookEntryEntity` – neue Eigenschaften (Zeitpunkt, Art, Menge, Preis pro Einheit, Gesamtbetrag, Kilometerstand,
  Teilbetankung, Tankstellenname, optionale Tankstellen-Kennung, Notiz, Belegfoto-Dateiname); `TankradarDbContext` –
  Konfiguration; neue Migration (per `dotnet ef migrations add`), rein additiv mit Datenübernahme für Bestandszeilen.
- Verbindungszeichenfolge mit `Foreign Keys=True` (`MauiProgram`, zentral im `TankradarDbContext`).

### Modelle (`Models/Tankbook`)
- Neu: Enum für die Art des Vorgangs (Kraftstoff/Strom), Record für einen Vorgang, Eingabe-Record, Ergebnis-Enum,
  Grenzwerte, Feldfehler, Verbrauchsergebnis je Vorgang, bekannte Tankstelle.

### Logik / Services (`Services/Tankbook`)
- Neu: Dienst für Vorgänge (`ITankbookEntryService`), Validator/Parser für die Formulareingaben, reiner Rechenbaustein für
  Verbrauch und Plausibilität, Ablage der Belegfotos (`IReceiptPhotoStore`) und Fotoauswahl (`IReceiptPhotoPicker`, mit
  Testimplementierung für FlaUI).
- Geändert: `IVehicleService`/`VehicleService` (Löschen mit erwarteter Vorgangszahl; Start-Kilometerstand nicht über dem
  ersten Vorgang laut ADR 0008 „Folgen“; Belegfotos beim Mitlöschen entfernen), `IDatabaseFileProtector` (Schutz einzelner
  Dateien/Verzeichnisse für Belegfotos).

### UI
- Neu: Seite für die Vorgänge eines Fahrzeugs (Liste, Formular, Rückfrage) mit ViewModel und Listeneintrags-ViewModel;
  Navigation von der Fahrzeugkarte; Texte zentral in `Resources/Texts` (`TankbookTexts` bzw. neue Textklasse).
- Geändert: `TankbookPage.xaml`, `VehicleItemViewModel`, `TankbookViewModel` (Neuzählung, WCAG-Muster, Einstieg in die
  Vorgänge).

### Tests
- Unit: Parser/Validator, Verbrauchsberechnung, Plausibilität, Dienst, ViewModels, Texte, Fotoablage.
- Integration: Migration mit Bestandsdaten, Dienst auf echter Datei, `PRAGMA foreign_keys`.
- FlaUI-E2E: Erfassen + Verbrauch, Validierung, Korrigieren/Löschen, Belegfoto (Testauswahl), Offline-Szenario,
  angepasste Tests aus Schritt 12 (WCAG-Namen, Neuzählung).

### Dokumentation
- `docs/help/Tankbuch/`, neuer ADR, `changes.log`, Zwischenstand 0.1.17.

## Implementierungsansatz

- Vertikaler Schnitt nach dem Muster von Schritt 12: EF-Entität + Migration → Dienst über `IDbContextFactory` mit
  Ergebnis-Enums statt Ausnahmen → ViewModel mit feldbezogenen Fehlern und Ansage über `IScreenReaderAnnouncer` →
  XAML-Bereiche in der Seite (keine Systemdialoge).
- Verbrauch und Plausibilität als reine statische Funktionen (gut unit-testbar), vom Dienst beim Lesen der Liste
  angewendet; nichts davon wird gespeichert.
- Bekannte Tankstellen aus `FavoriteEntries` ⨝ `Stations` (lokal, ohne Netz) und aus bereits verwendeten Tankstellennamen.
- Belegfoto: Auswahl über MAUI `MediaPicker`, Kopie ins App-Datenverzeichnis, Dateischutz über das bestehende
  Protector-Muster; im Testmodus (`TANKATLAS_TEST_DATA_PATH`) liefert eine Testimplementierung eine vorgegebene Datei.
- iOS-Release: Interpreter (`MtouchInterpreter=all`) und `TrimmerRootAssembly` bleiben unverändert; alle neuen Typen,
  die EF per Reflection nutzt, liegen in der App-Assembly.

## Konfiguration

Keine neue Benutzereinstellung. Die Fotoauswahl für E2E-Tests wird über eine Test-Umgebungsvariable gesteuert (nur im
Testmodus wirksam), analog zu `TANKATLAS_TEST_LOCATION`.

## Offene Fragen

Keine, die die Umsetzung blockieren. Folgende Auslegungen werden im Plan begründet festgelegt (Annahmen):

- Toleranz für die Plausibilitätsprüfung und ob eine Abweichung das Speichern verhindert.
- Reihenfolge „chronologisch“ (Designentwurf: neueste zuerst).
- Ob die Tankstelle Pflicht ist.
- Verhalten bei Hybridfahrzeugen (Kraftstoff und Strom) und beim Wechsel der Kraftstoffart eines Fahrzeugs mit
  vorhandenen Vorgängen.
- Prüfung des Kilometerstands gegenüber dem nachfolgenden Eintrag beim Nachtragen/Korrigieren.
- Speicherung von Datum/Uhrzeit (lokale Wanduhrzeit).
