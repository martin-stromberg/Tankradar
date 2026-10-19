# Anforderung: Schritt 16 – Export des Tankbuchs als CSV oder PDF

Quelle: `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Abschnitt „Schritt 16“ samt Block „Zusatz aus der Abnahme von Schritt 15a“, ergänzt um Vorgaben des Auftraggebers (siehe „Vorgaben“). Abhängigkeit: Schritt 14 (Auswertung). Zielversion: 0.1.21.

## Fachliche Zusammenfassung

Zu einem Fahrzeug werden die Tank- und Ladevorgänge des Tankbuchs auf ausdrückliche Veranlassung des Anwenders exportiert, wahlweise auf einen Zeitraum eingeschränkt: als CSV-Datei zur Weiterverarbeitung in (deutschsprachigen) Tabellenkalkulationen oder als PDF-Bericht mit den Einträgen und den Kennzahlen (Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten). Die erzeugte Datei wird ausschließlich über die Teilen-/Speichern-Funktion des Betriebssystems bereitgestellt; es gibt keine automatische Weitergabe. Zusätzlich werden die Härtungen aus der Abnahme von Schritt 15a umgesetzt.

## Fachliche Punkte

1. **Auslöser und Weitergabe.** Ein Export entsteht nur, wenn der Anwender ihn auslöst (Schaltfläche). Die Datei wird über die Systemfunktion Teilen/Speichern (`Share` bzw. Windows-Speichern) übergeben. Keine Netzwerkübertragung, keine automatische Weitergabe, keine Hintergrundexporte.
2. **Auswahl des Fahrzeugs** (Auswahl-/Identifikationsanforderung). Der Export bezieht sich auf das Fahrzeug der geöffneten Seite „Tank- und Ladevorgänge“; das Fahrzeug ist dort per Namen gewählt (ADR 0009), nie per Kennung. Eine zweite Fahrzeugauswahl ist nicht nötig.
3. **Zeitraum.** Wahlweise „alle Vorgänge“ oder ein Zeitraum (von/bis, einschließlich beider Tage; lokale Wanduhrzeit wie ADR 0009). Anwenderauswahl über Auswahlchips (Voreinstellungen) und gegebenenfalls Datumsfelder; keine Eingabe interner Kennungen.
4. **Format.** CSV oder PDF, vom Anwender gewählt.
5. **CSV für deutschsprachige Tabellenkalkulationen.** Umlaute korrekt (UTF-8 mit BOM), Semikolon als Trennzeichen, Dezimalkomma, Einheiten Liter bzw. kWh. Die Einzelheiten (Spalten, Datumsformat, Zahlenformat, Schutz vor Formelinjektion) werden in einem neuen ADR 0013 entschieden.
6. **PDF-Bericht.** Enthält die Einträge des Zeitraums und die Kennzahlen (Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten). Die Kennzahlen stammen aus denselben Berechnungen wie die Auswertung (ADR 0010: `ConsumptionCalculator`, `TankbookStatisticsCalculator`), keine zweite Rechenlogik. Technik: bevorzugt ohne neue Abhängigkeit; sonst nur frei lizenziert (MIT/Apache/BSD) und lauffähig unter iOS mit Interpreter (`MtouchInterpreter=all`) und Trimming. Entscheidung samt Alternativen steht im ADR 0013.
7. **Anzeigenamen der Tankstellen** wie ADR 0012 (Anzeigename: eigener Name, sonst Marke, sonst Quellname; Vorgänge tragen den Namen als Momentaufnahme nach ADR 0009).
8. **Fahrzeug ohne Einträge / Zeitraum ohne Einträge.** Verständliche Rückmeldung bzw. ein gültiges leeres Ergebnis (Entscheidung im Plan); kein Absturz, keine leere oder beschädigte Datei.
9. **Testmodus.** Ist `TANKATLAS_TEST_DATA_PATH` gesetzt, erscheint kein echter Teilen-Dialog, sondern die Datei wird in das Testdatenverzeichnis geschrieben (Muster `TestBackupSharer`/`BackupSharerSelector`).
10. **Zwischenstand.** Nach Abschluss ein startfähiger Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root (Version 0.1.21; nicht committen).

## Härtungen aus der Abnahme von Schritt 15a (optional, hier eingeplant)

- H1 Kachelzahlen-E2E-Test schärfen: volle Elementhöhe sicherstellen (ein teilweise sichtbares Element meldet nur die sichtbare Höhe).
- H2 Regressionstest für Sicherungen mit eingebettetem NUL-Zeichen in Textfeldern; die zusätzliche Byteprüfung wird in ADR 0011 ergänzt.
- H3 Doppelten Satz in der Hilfe zu den Tankstellendetails (Absatz „Eigener Name“) entfernen.
- H4 Hinweis zur Teilladung in der Tankbuch-Hilfe besser platzieren.
- H5 Geöffnete Vorgangsseite nach einer Wiederherstellung neu laden (besteht seit Schritt 15).
- H6 Abnahmen starten Zwischenstände nicht per `Start-Process` (kein Fenster im Vordergrund); betrifft Abnahmevorgehen, keinen Produktcode.

## Betroffene Klassen und Komponenten (voraussichtlich)

- Neu (Logik): CSV-Erzeugung, PDF-Erzeugung, Export-Dienst (`ITankbookExportService`), Zeitraumfilter, Ablage/Übergabe (`IExportSharer` mit `MauiExportSharer`/`TestExportSharer` und Selektor), Modelle/Enums für Format und Zeitraum.
- Neu (UI): Bereich „Export“ auf `TankbookEntriesPage` mit `TankbookExportViewModel`; Texte in `Resources/Texts`.
- Bestehend: `TankbookEntryService`, `ConsumptionCalculator`, `TankbookStatisticsCalculator`, `TankbookEntriesViewModel`, `StationNaming`, `MauiProgram`, `BackupViewModel`/Wiederherstellungsablauf (H5), `BackupDatabasePreparer` (H2, nur Test/ADR).
- Doku: `docs/adr/0013-…`, `docs/adr/0011`, `docs/help/Tankbuch/*`, `docs/help/Tankstellendetails/beschreibung.md`, `docs/help/index.md`, `changes.log`, README bei Bedarf.
- Tests: Unit, Integration, FlaUI-E2E.

## Vorgaben und Randbedingungen

- Keine Änderung von `MtouchInterpreter` und `TrimmerRootAssembly` in `Tankradar.MAUI.csproj`; ein Bedarf wird nur gemeldet.
- Migrationen erhalten Daten (falls überhaupt nötig; erwartet wird keine Schemaänderung).
- AutomationIds nach Schema `TankbookEntries.<Bereich>.<Element>`; FlaUI-E2E nutzen `TankbookE2ETestBase`/`TankbookE2EDriver`/`SetFieldValue`.
- Tests rufen keine Produktivendpunkte auf (Mock-Server, `TANKRADAR_*_URL`).
- E2E ohne Fokusdiebstahl: nur UIA-Muster, kein `Focus()`/`SetForeground`/Tastatur/Maus (abgesichert durch `E2EInputPolicyTests_NoFocus`).
- Hilfe unter `docs/help`, Eintrag in `changes.log`, Version 0.1.21; Texte deutsch; XML-Dokumentation für alle Member (Pre-Commit-Hook `csproj-xmldoc-check.py`; Ausweichform `get { return …; }` bei Eigenschaften).

## Offene Fragen (zur Klärung im Plan)

- Aufbau der Datumsauswahl für den Zeitraum und ihre Testbarkeit per UIA.
- Verhalten bei leerem Ergebnis (Hinweis ohne Datei oder Datei mit Kopf).
- PDF-Technik (Annahme: eigener, schlanker PDF-Schreiber ohne Abhängigkeit).
- Wie Belegfotos behandelt werden (Annahme: nicht Teil des Exports).
