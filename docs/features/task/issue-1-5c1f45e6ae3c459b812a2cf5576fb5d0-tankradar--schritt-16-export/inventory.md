# Bestandsaufnahme: Schritt 16 – Export des Tankbuchs als CSV oder PDF

Analysiert wurden Tankbuch, Auswertung, Sicherung/Teilen, Projektkonfiguration (PDF-Optionen) und Teststruktur, bezogen auf `requirement.md` (Export CSV/PDF samt Härtungen aus Schritt 15a).

## Zusammenfassung

- Ein Export existiert nicht: kein Dienst, kein Modell, keine UI, keine Tests. Die Daten liegen vor: `ITankbookEntryService.GetEntriesAsync` liefert Vorgänge samt Verbrauch; `TankbookStatisticsCalculator.Calculate` akzeptiert eine beliebige Teilmenge von `TankbookEntryOverview`, ein Zeitraumfilter nach der Verbrauchsberechnung ist also möglich.
- Es gibt keinen Zeitraumfilter in Dienst und Auswertung. Anzeigegrenzen (24 Monate, 6/12 Diagrammpunkte) liegen im ViewModel/Diagramm und wirken nicht auf die Rohdaten.
- Vorgänge tragen den Tankstellennamen als Momentaufnahme (`TankbookEntry.StationName`, ADR 0009/0012); `StationNaming` liefert den Anzeigenamen aktueller Stationen.
- PDF: **keine** vorhandene Lösung. `Tankradar.MAUI.csproj` und `project.assets.json` enthalten weder SkiaSharp noch ein PDF-Paket. `Microsoft.Maui.Graphics 10.0.20` enthält nur Typen zum Lesen/Rendern (`IPdfPage`, `IPdfRenderService`), keinen PDF-Export. `SKDocument.CreatePdf` ist daher nicht verfügbar. SkiaSharp (MIT) wäre eine neue native Abhängigkeit unter iOS-Interpreter/Trimming ohne Beleg der Lauffähigkeit und mit möglichem Bedarf an Änderungen an `MtouchInterpreter`/`TrimmerRootAssembly` (untersagt). Details: [Logik](inventory/logic.md), Abschnitt „PDF-Möglichkeiten im Bestand“.
- Teilen: `IBackupSharer`/`MauiBackupSharer` (`Share.Default.RequestAsync(ShareFileRequest)`) sowie `TestBackupSharer`/`BackupSharerSelector` (Testmodus per `TANKATLAS_TEST_DATA_PATH`, Ziel `test-shared`) sind das Vorbild; Namen und Ablage sind sicherungsspezifisch.
- Härtungen: Der Kachelzahlen-Test prüft nur `Höhe <= 2 * Schriftgröße`, nicht die volle Sichtbarkeit; `BackupDatabasePreparer` prüft Textgrenzen auch in Bytes, ADR 0011 erwähnt das nicht, ein NUL-Regressionstest fehlt; `BackupViewModel.Restored` wird nirgends abonniert und `TankbookEntriesViewModel` lädt nur bei `OnAppearing()`; die Hilfetexte zu H3 (`docs/help/Tankstellendetails/beschreibung.md`, Zeile 15 sagt zweimal, dass der Name Teil der Sicherung ist und ein Preisabruf ihn nicht ändert) und H4 (`docs/help/Tankbuch/beschreibung.md`, Zeilen 29 und 52) sind bearbeitbar.
- Dokumentation: ADR 0001 bis 0012 vorhanden (neu: 0013); Hilfe unter `docs/help/<Bereich>/{index,beschreibung,ablauf-anwender,ablauf-technisch}.md`, Übersicht `docs/help/index.md`; `changes.log` (neuester Eintrag Version 0.1.20).

Test-Ausgangszustand: Unit 1345/1345 und Integration 241/241 grün (Release, Windows-Zielframework, 0 Fehlschläge). E2E nicht ausgeführt (Testlücke). Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
