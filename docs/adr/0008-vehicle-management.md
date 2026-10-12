# ADR 0008: Fahrzeugverwaltung im Tankbuch

## Status

Angenommen (2026-10-11)

## Kontext

Schritt 12 baut den bisher leeren Bereich „Tankbuch“ zur Fahrzeugverwaltung aus: mehrere Fahrzeuge mit Name,
Kraftstoffart (Benzin, Diesel, Elektro, Hybrid), Start-Kilometerstand und optionalem Startverbrauch anlegen, bearbeiten
und löschen; ausdrückliche Rückfrage, wenn Tank- oder Ladevorgänge mitgelöscht würden; feldbezogene Validierung;
ausschließlich lokale Speicherung. Verbindlich ist der Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`,
Screen `tankbuch_verbrauchsauswertung`, `DESIGN.md` Abschnitt „Tankbuch-Formulare“). Die Gesamtanforderung lässt offen, was
mit den Einträgen eines gelöschten Fahrzeugs geschieht (offene Frage 6.1.4).

## Entscheidungen

- **Eigenes Enum `VehicleFuelKind`** (`Petrol`, `Diesel`, `Electric`, `Hybrid`), gespeichert als Enum-Name. `FuelType`
  (Spritsorten der Preisquelle E5/E10/Diesel) wird nicht wiederverwendet, weil es ein anderer Persistenzvertrag ist.
- **Verbrauchseinheit aus der Kraftstoffart:** Elektro → kWh/100 km, sonst (auch Hybrid) L/100 km. Die Einheit wird nicht
  gespeichert. Ladevorgänge von Plug-in-Hybriden gehören zu den Schritten 13 und 14.
- **Minimale Vorgangstabelle schon jetzt.** `TankbookEntries` (`Id`, `VehicleId` mit `ON DELETE CASCADE`, `CreatedUtc`)
  entsteht mit der Migration `AddVehicles`, ohne Oberfläche. Nur so sind Rückfrage und Mitlöschen bereits prüfbar. Schritt 13
  erweitert die Tabelle per eigener Migration, ohne `VehicleId` und Kaskade zu ändern. Damit ist die offene Frage 6.1.4
  beantwortet: Die Vorgänge werden nach ausdrücklicher Bestätigung mitgelöscht.
- **Rückfrage immer, im Listeneintrag.** Ohne Vorgänge lautet sie schlicht „Fahrzeug „X“ löschen?“; mit Vorgängen nennt sie
  deren Zahl und das endgültige Mitlöschen, die Bestätigung heißt dann „Fahrzeug und Vorgänge löschen“. Die Zahl wird beim
  Antippen von „Löschen“ frisch gelesen. Der Hinweis auf eine vorherige Sicherung folgt in Schritt 15.
- **Formular und Rückfrage als Bereiche in der Seite**, keine Dialoge des Betriebssystems und keine eigene Route (wie
  [ADR 0006](0006-favorite-groups.md)); die Kraftstoffart wird über Chips gewählt (feste Auswahl, keine Freitexteingabe).
- **Auswahl per Name:** Bearbeiten und Löschen sind Schaltflächen je Listeneintrag; die Schaltflächen tragen den
  Fahrzeugnamen als Beschreibung für die Sprachausgabe. Interne Kennungen werden nie angezeigt oder eingegeben.
- **Namen** getrimmt, 1 bis 40 Zeichen, eindeutig ohne Beachtung der Groß- und Kleinschreibung (Prüfung im Dienst, eindeutiger
  Index mit `NOCASE` als Sicherung). Spätere Schritte wählen Fahrzeuge per Namen aus.
- **Wertebereiche:** Start-Kilometerstand 0 bis 9.999.999 km (ganze Zahl, Tausenderpunkte erlaubt); Startverbrauch optional,
  größer als 0 und kleiner als 100 mit höchstens zwei Nachkommastellen (Komma oder einzelner Punkt), gespeichert als `decimal`
  in einer `TEXT`-Spalte.
- **Vorbelegung:** Ein neues Fahrzeug hat „Benzin“ vorausgewählt; die Auswahl ist sichtbar und änderbar.
- **Kein Änderungsereignis** am Dienst: Nur die Tankbuchseite zeigt Fahrzeuge und lädt nach jeder Änderung selbst neu.
- **Datenschutz:** keine Netzwerkzugriffe; Protokolle nennen nur Ausnahmetypen, nie Fahrzeugnamen oder Werte.

## Abweichungen vom Designentwurf

Nicht umgesetzt, weil sie zu späteren Schritten gehören oder nicht gefordert sind:

- **Karte „Aktives Fahrzeug“** mit Auswahlliste, Gesamt-Kilometern und Gesamt-Durchschnitt (Schritte 13 und 14).
- **Kennzeichen** („B-MW 2024“): nicht gefordert.
- **Monatsanalyse und Statistik** (Schritt 14).
- **„Neuen Tankvorgang erfassen“ und Historie** (Schritt 13).
- **Export** (Schritt 16).
- **Sync-Banner:** Cloud-Synchronisation ist nicht Teil von Version 1.0.

Übernommen bzw. angepasst:

- Die Fahrzeugeinträge übernehmen die Gestaltung der Einträge der Auswahlliste des Entwurfs: Name, darunter
  „Kraftstoffart • Start … km • Startverbrauch …“. Für den Namen wird `headline-md` verwendet, weil das Design-System keinen
  Stil `title-sm` enthält.
- Eingabefelder nach „Tankbuch-Formulare“: fester Einheiten-Suffix (`km`, `L/100 km`, `kWh/100 km`), neutraler Rahmen
  `#E2E8F0`, Fokusrahmen 2 px Teal `#0F766E`; bei einem Feldfehler ist der Rahmen rot. Das Namensfeld erhält denselben
  Rahmen ohne Suffix. Umgesetzt als reines XAML-Muster (`Border` um `Entry` und Einheiten-`Label`) ohne eigenes
  Steuerelement. Der Fokusrahmen bindet über `x:Reference` an `Entry.IsFocused`; damit diese Bindungen kompiliert werden
  (sonst Warnung XC0025), setzt das Projekt `MauiEnableXamlCBindingWithSourceCompilation`.

## Restpunkte aus Schritt 11

- **Anwenderhilfe Suchcache** (`docs/help/Startseite/beschreibung.md`): Der Merker ist höchstens fünf Minuten wirksam und
  wird danach bei der nächsten Preisabfrage, spätestens beim Beenden der App, verworfen. Nur die Dokumentation wurde
  korrigiert; das Verhalten ist datenschutzkonform.
- **Sporadische Integrationstests:** `TestDatabase` nutzt Verbindungen ohne Pool (`Pooling=False`), alle Aufrufe von
  `SqliteConnection.ClearAllPools()` entfallen. Es gibt damit keinen globalen Eingriff in Verbindungen paralleler
  Testklassen mehr (Ursache der `ObjectDisposedException`). Der Drosselungstest der Geokodierung misst den Abstand
  clientseitig beim Absenden (`RequestTimingHandler`, mindestens 990 ms) statt der lastabhängigen Empfangszeit am Mock.

## Folgen

- Schritt 13 baut auf `TankbookEntries` und `IVehicleService` auf. Er prüft zusätzlich, dass der Start-Kilometerstand nicht
  über dem ersten Vorgang liegt, und wie mit einem Wechsel der Kraftstoffart bei vorhandenen Vorgängen umzugehen ist.
  Beides ist in [ADR 0009](0009-tankbook-entries.md) entschieden; dort sind auch die Neuzählung beim Löschen und das
  WCAG-Muster (Kontext im Hinweis statt in der Beschreibung) festgehalten, die die Beschreibungen der Schaltflächen ersetzen.
- Schritt 15 ergänzt die Löschrückfrage um den Sicherungshinweis (`TankbookTexts.FormatDeleteQuestion`).
