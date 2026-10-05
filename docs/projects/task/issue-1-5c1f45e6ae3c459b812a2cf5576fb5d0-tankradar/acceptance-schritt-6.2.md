# Abnahmeprüfung – Entwicklungsschritt 6

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Der Suchradius lässt sich nicht mehr im ganzen Bereich von 1 bis 25 km einstellen.** Die Vorgehensentscheidung „Suchradius“ und der Auftrag legen fest: „Der einstellbare Suchradius reicht von 1 bis 25 km (Standard 5 km).“ Seit der Nachbesserung bietet die Oberfläche nur noch die Chips `SearchRadius.Steps` = 2, 5, 10, 15 und 25 km an (`MapViewModel.RadiusOptions`, `MapPage.xaml`). Die Untergrenze 1 km ist damit nicht mehr wählbar, alle Zwischenwerte ebenfalls nicht. In Runde 1 war 1 km noch wählbar, und der E2E-Test `Radius1_ShowsOnlyNearestStation` wurde entfernt. Die Prüfung `SearchRadius.TryParse` (1–25) wirkt nur noch intern.
  - ADR 0002 nennt die Stufen zwar unter „Kontext“. Es begründet aber nicht, warum vom festgelegten Bereich abgewichen wird, und führt das nicht als Abweichung von der Vorgehensentscheidung.
  - Die Anwenderhilfe widerspricht sich: `docs/help/Suche/index.md` verspricht „1 bis 25 km“, `beschreibung.md` nennt die Stufen 2/5/10/15/25.
  - Abhilfe gibt es zwei: einen Chip „1 km“ ergänzen (eventuell weitere Stufen), oder die Einschränkung bewusst festhalten (ADR, Vorgehensentscheidung, `index.md`) und dafür eine Bestätigung einholen.

## Hinweise

**Abweichungen aus `acceptance-schritt-6.1.md`:**

- **Design der Suchseite: weitgehend behoben.**
  - Radius, Spritsorte und Sortierung sind jetzt Pillen-Chips. Ausgewählt sind sie Teal mit weißer Schrift, unausgewählt `#F1F5F9`, mindestens 44 px hoch.
  - Die Ergebniskarten haben eine Rundung von 16 px. Das entspricht `rounded-2xl` im Entwurf (Tailwind-Standard 1rem). Dazu kommen ein Schatten der Ebene 1 (`Border.Shadow`), `price-hero` in Teal (dunkel: Emerald) und eine Adresszeile (`SearchTexts.FormatAddress`).
  - ADR `docs/adr/0002-search-page-design-deviations.md` begründet die verbleibenden großen Abweichungen nachvollziehbar: Kartenansicht in einem späteren Schritt, Strom und Belegung laut ADR 0001, keine Logos oder Trends in der Quelle, Chips als `Button`, Schatten je Plattform, Listenlänge. Ausnahme ist der Radius (siehe Abweichung).
  - Nicht im ADR erwähnt sind kleinere Unterschiede zum Entwurf:
    - Die Preise stehen als Zeilen je Sorte unter Name und Adresse statt als einzelner Preis rechts. Das ist fachlich nötig, weil mehrere Sorten angezeigt werden.
    - Das Preisformat ist „1,859 €“ statt „1.63⁹ €/L“ (hochgestellte dritte Nachkommastelle, Einheit €/L).
    - Die Entfernung steht rechts neben dem Namen statt in der Adresszeile („Straße • 1,8 km“).
    - Radius, Spritsorte und Sortierung stehen in drei eigenen Karten mit Überschrift statt in einer kompakten Chip-Leiste unter der Suchleiste.

    Ich werte das nicht als Abweichung. Es sollte aber für die Vollständigkeit der Designdokumentation im ADR ergänzt werden.
- **Changelog des Zwischenstands: behoben.**
  - `review-versions/0.1.8_2026-10-05/CHANGELOG.md` enthält eine kurze, aussagekräftige Beschreibung.
  - `scripts/create-review-version.ps1` lehnt einen Aufruf ohne Changelog oder mit leerem Changelog vor dem Build ab. Geprüft: ohne `-ChangelogFile` und mit `-ChangelogFile '   '` jeweils die Meldung „Es wurde kein Changelog angegeben …“ und Exit-Code 1. Es wurde kein Build angestoßen und kein Verzeichnis angelegt.
- **Leistung der Ergebnisliste: entschärft.**
  - Die Liste bleibt ein nicht virtualisiertes `BindableLayout`, zeigt aber höchstens 25 Karten an. „Weitere anzeigen (N weitere)“ ergänzt jeweils 25.
  - Ein Filter- oder Sortierwechsel und eine neue Suche setzen die Liste auf die erste Seite zurück und rechnen das vollständige Ergebnis neu. Die Seitenbildung erfolgt also nach Filter und Sortierung, nicht vorher. Das ist fachlich vertretbar und korrekt.
  - Ein Neuladen der Einstellungen behält die angezeigte Menge bei (`resetPaging: false`).
  - Abgedeckt durch Unit-Tests (`MapViewModelTests_Chips`: Seiten, Sortierwechsel, kurzes Ergebnis), einen Integrationstest (300 Stationen) und den E2E-Test `SearchE2ETests_LargeResult` (300 Stationen, zweite Seite, Sortierwechsel zurück auf Seite 1).
  - Restpunkt: Jedes „Weitere anzeigen“ ersetzt die gesamte Liste, sodass alle bereits sichtbaren Karten neu aufgebaut werden. Nach mehrfachem Nachladen kehrt der Aufwand also zurück. Einen Test für „Filterwechsel setzt Seiten zurück“ gibt es nicht, nur einen für den Sortierwechsel.
- **Mock-Format der Umkreissuche: behoben.** `MockTankerkoenigServer` liefert in `list.php` weder `wholeDay` noch `openingTimes`, nur noch `detail.php`. Der E2E-Test erwartet jetzt ausdrücklich keinen Hinweis „Automatentankstelle“ ohne bekannte Details.

**Übernahme der Öffnungszeiten aus dem Detail-Cache:**

- Kein Datenleck zwischen Stationen. `PriceRepository.GetKnownDetailsAsync` lädt nur Stationen mit `DetailsUpdatedUtc != null` und gibt sie als Wörterbuch nach Primärschlüssel zurück. `FuelPriceService.WithKnownDetailsAsync` ordnet ausschließlich über die Station-ID zu, `StationInfo.WithDetails` übernimmt nur `WholeDay`, `OpeningTimes` und `DetailsUpdatedUtc`. Live-Preise, `IsOpen`, Adresse und Entfernung bleiben erhalten (Test `WithDetails_TakesDetailsAndKeepsLivePrices`).
- Ein Lesefehler im Cache lässt die Suche unberührt; protokolliert wird nur der Ausnahmetyp.
- Die Detailangaben haben keine Altersgrenze und keinen Hinweis auf ihr Alter. Ein „Automatentankstelle“ aus einer monatealten Detailabfrage erscheint ohne Kennzeichnung. Öffnungszeiten ändern sich selten, und die Anforderung („soweit vorhanden“) verlangt dafür keinen Hinweis. Ich werte das daher nicht als Abweichung. Empfehlung: spätestens mit der Detailansicht (Schritt 8) eine Altersgrenze festlegen.
- Bis Schritt 8 löst die Oberfläche keine Detailabfrage aus. Der Hinweis „Automatentankstelle“ erscheint in der Live-Suche deshalb praktisch nie. Das ist nach der Entscheidung „Fehlende Quelldaten“ zulässig und in `docs/help/Suche/beschreibung.md` erklärt.

**Barrierefreiheit der Chips:**

- Der Auswahlzustand wird nur über `SemanticProperties.Hint` = „Ausgewählt“ angeboten, bei nicht ausgewählten Chips ist der Hinweis leer. Das wirkt sich je Plattform so aus:
  - Windows: Narrator liest den Hinweis als `HelpText` mit.
  - iOS: Unter VoiceOver erscheint er als `AccessibilityHint`. Er wird verzögert vorgelesen und entfällt ganz, wenn der Nutzer „Hinweise“ abgeschaltet hat.
- Ein echter Zustand fehlt: kein `Selected`-Trait unter iOS, kein `SelectionItem`- oder `Toggle`-Pattern unter UIA. Für Screenreader gilt der Chip als normale Schaltfläche, und die Gruppenzugehörigkeit (Einzelauswahl) wird nicht vermittelt.
- Das genügt als Minimallösung. Robust im Sinne von WCAG 4.1.2 (Name, Rolle, Wert) ist es nicht. Empfehlung: den Zustand zusätzlich in die `SemanticProperties.Description` aufnehmen, etwa „5 km, ausgewählt“, oder plattformspezifisch das `Selected`-Trait setzen. Die Anforderung nennt keine Vorgaben zur Barrierefreiheit, daher keine Abweichung.

**Technische Beobachtungen (Code-Review-Nachlese):**

- `MapViewModel.RadiusText` ist weiter eine öffentliche, frei setzbare Zeichenkette. Die Oberfläche bindet sie nicht mehr, sie dient nur noch als interner Zwischenspeicher der Chip-Auswahl. Das ist toter API-Rest. Ein direkter Radius-Wert (`int`) wäre klarer.
- Die drei Chip-`DataTemplate`s in `MapPage.xaml` sind bis auf das AutomationId-Präfix identisch (Duplikat). Der gemeinsame `DataTrigger` ließe sich in den Style verlagern.
- `HasMore` und `ShowMoreText` werden nur bei einer Änderung von `Stations` gemeldet. Das funktioniert, weil `StationResultBuilder.Build` immer eine neue Liste liefert. Die Kopplung ist aber fragil.
- Die E2E-Tests zur ungültigen Radiuseingabe sind entfallen, weil es kein freies Feld mehr gibt. Die Validierung bleibt durch Unit-Tests abgedeckt (`SearchRadiusTests_Validation`, `MapViewModelTests_Chips.RadiusTextOutsideSteps_DeselectsChipsAndInvalidIsRejected`) und wird im Preisdienst zusätzlich geprüft.
- Die übrigen Punkte aus `acceptance-schritt-6.1.md` gelten unverändert:
  - Altersangaben werden nicht laufend aktualisiert.
  - Der Offline-Hinweis scrollt mit dem Inhalt mit.
  - Die Preisdienst-Variablen heißen weiterhin `TANKRADAR_*`.

**Fachlich ohne Regression (erneut geprüft):**

- Filter: „Alle“ und die gewählten Sorten.
- Sortierung: nach Preis, Entfernung oder Name, Voreinstellung aus den Optionen.
- Preiszeilen in der Reihenfolge der Einstellungen.
- „vor X Min.“, ab 60 Minuten in Amber.
- „Preis unbestätigt“.
- Offline-Hinweis mit den zuletzt bekannten Preisen.
- Standort nur beim Suchen und gemäß GPS-Einstellung; bei „Nie“ keine Abfrage, sondern ein Hinweis.
- Keine Speicherung von Rohkoordinaten.
- iOS-Zwecktext „Ermittlung von Tankstellen in der Nähe“.
- Testmodus-Variable `TANKATLAS_TEST_DATA_PATH`.

**Praktische Verifikation:**

- **`scripts/local-ci.ps1` (einmal, inkl. E2E):** Exit-Code 0, „Lokaler Prüflauf erfolgreich“. Im Einzelnen:
  - Pipeline-Skripte, Workflow-Validierung, iOS-Deployment-Skript (beide Umgebungen), Restore, Format und Sicherheitsprüfung: OK.
  - Build mit Warnungen als Fehler: 0 Warnungen.
  - Tests: 354 Unit-Tests und 58 Integrationstests bestanden, Zeilenabdeckung 94,4 % (Schwelle 70 %). 32 FlaUI-E2E-Tests bestanden, ohne Wiederholung.
  - iOS-Compile-Prüfung: OK, mit 1 Warnung `MAUI1001` (XamlC-Inflator), die schon bekannt ist.
- **`scripts/test-ios-deployment.ps1`:** „iOS-Deployment-Pruefung erfolgreich.“ (Exit-Code 0).
- **Zwischenstand `review-versions/0.1.8_2026-10-05/`:**
  - Vorhanden und per `.gitignore` ausgeschlossen. Der Changelog ist gefüllt, die DLL enthält die neuen Bestandteile (`ShowMoreCommand`, `RadiusChipTemplate`, `GetKnownDetailsAsync`, `TANKATLAS_TEST_DATA_PATH`).
  - Ich habe ihn mit `TANKATLAS_TEST_DATA_PATH` auf ein temporäres Verzeichnis unter `%TEMP%` gestartet. Das Fenster „Tankatlas“ erschien, und die Datenbank `tankatlas.db` lag ausschließlich im Testverzeichnis.
  - Danach habe ich ihn sofort per Name `Tankradar.MAUI` beendet und das Testverzeichnis gelöscht. Es läuft kein `Tankradar.MAUI` mehr. Die UI habe ich nicht bedient.
- Ich habe keinen echten Tankerkönig-Endpunkt aufgerufen, nichts committet oder gepusht und keine Konfiguration geändert. Den Designentwurf habe ich nur im Scratchpad außerhalb des Repos entpackt.
