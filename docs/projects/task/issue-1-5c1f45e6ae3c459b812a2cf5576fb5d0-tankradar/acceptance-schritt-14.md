# Abnahmeprüfung – Entwicklungsschritt 14 (Verbrauchs- und Kostenauswertung), erneute Prüfung nach Nachbesserungsrunde 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

Die drei Abweichungen aus der ersten Abnahme (`acceptance-schritt-14.1.md`) sind behoben:

- **Designentwurf:** Kachelraster „Ø Verbrauch“ / „Kosten / 100 km“ mit großer Zahl, Karte „Gesamtkosten <Monat>“ mit
  Betrag und Menge, Überschrift „Monatsanalyse <Monat Jahr>“ und Verbrauchsverlauf als Balkendiagramm der letzten 6
  Vorgänge sind umgesetzt (Variante a). ADR 0010 beschreibt den Stand zutreffend und begründet die übrigen Abweichungen
  tragfähig; Hilfe (`beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md`), README und `changes.log` sind
  nachgezogen.
- **Hinweistext:** Der Verweis auf den Startverbrauch erscheint nur noch für die Startart ohne hinterlegten
  Startverbrauch (`OffersStartConsumption`), nie im Strom-Block eines Hybrids.
- **Diagramme:** Rand-Beschriftungen werden in die Diagrammfläche geschoben und links- bzw. rechtsbündig gesetzt
  (`ChartTextArea`, unit-getestet); ohne Werte entfällt das Diagramm samt Sprachausgabe (`IsVisible` an `HasData`).

Fachlich ist alles unverändert korrekt; die Nachbesserung hat keine Regression erzeugt. `local-ci` ist bis auf den
bekannten Umgebungseffekt „Screenshot einfarbig“ grün (Unit 1126, Integration 122, E2E 97/98), die Pre-Push-Prüfungen
sind grün, und die neuen bzw. geänderten E2E-Klassen liefen 3 × fehlerfrei.

**Einschränkung der Prüfung:** Der Bildschirm des Rechners war während der gesamten Abnahme aus (letzte Eingabe gegen
10:27, Abschaltung nach 900 s). Dann rendert DWM keine WinUI-Inhalte, `PrintWindow` liefert einfarbige Flächen. Eine
Sichtprüfung per Bildschirmaufnahme war daher nicht möglich; der Bildschirm wurde bewusst nicht per Eingabe geweckt. Die
Darstellung ist stattdessen über die Layout-Rechtecke der UI-Automation (360/375/390/430 px), die Geometrie-Unit-Tests und
eine Messung der Diagrammtexte geprüft (siehe „Darstellung“). Empfehlung: Den Zwischenstand bei eingeschaltetem
Bildschirm einmal kurz ansehen (hell und dunkel).

## Abweichungen

Keine.

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-14-auswertung`, HEAD `3e29a5e`.
- **Basis:** lokaler Projekt-Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `799efb3`
  (zugleich Merge-Base).
- **Diff gesamt:** 78 Dateien, +4252/−16, 10 Commits (`1949be4` … `3e29a5e`).
- **Nachbesserung:** `ee0ece5..3e29a5e`, 41 Dateien, +908/−311, 3 Commits:
  - `2e8deb5` fix: Auswertung nach Designentwurf, Hinweistext je Art, Diagramme ohne Abschneiden
  - `8407e4f` docs: ADR 0010, Hilfe, README, `changes.log`
  - `3e29a5e` fix: Kommentare und Randfall der Textfläche nach Code-Review
- **Grundlagen:** `project-plan.md` (Schritt 14 mit Zusatz aus Schritt 13, Vorgehensentscheidungen „Designentwurf“ und
  „Oberflächentests als Auslieferungs-Gate“), `requirement.md` („Statistik-Auswertung“), `acceptance-schritt-14.1.md`,
  ADR 0010, Designentwurf `tankbuch_verbrauchsauswertung` hell/dunkel (`code.html`, nur lesend aus der bereits im
  Scratchpad entpackten Kopie).

### Abweichungen aus 14.1

| Nr. | Abweichung aus 14.1 | Behoben | Nachweis |
|---|---|---|---|
| 1 | Falsche Aussage in ADR 0010 („Übernommen sind die Kacheln …“) | ja | Abschnitt „Übernommen“ entspricht jetzt der Umsetzung. Per UIA: zwei Kacheln nebeneinander (360 px: Inhalt 8–127 bzw. 151–270 pt; 390 px: 8–142 bzw. 166–300 pt). |
| 1 | Zweispaltiges Kachelraster mit großer Zahl, abgesetzter Einheit (und Symbol) | ja, Variante a | `Grid ColumnDefinitions="*,*"`, Stil `StatisticsTile`, Zahl in `price-hero` (28 pt), Einheit klein in eigenem Label (`…Average`/`…AverageUnit`, `…CostPer100Km`/`…CostPer100KmUnit`). Symbole bewusst weggelassen und in ADR 0010 festgehalten. |
| 1 | Karte „Gesamtkosten <Monat>“ mit Betrag des laufenden Monats und Menge statt „Gesamtkosten“ über alles | ja, Variante a | `MonthlyCostSummary.TotalQuantity`, Karte aus `Months[0]`. UIA: „Gesamtkosten Oktober“, „79,86 €“, „43,40 L getankt“ bzw. „78,40 kWh geladen“. Unit `TankbookKindStatisticsViewModelTests_MonthCard` (4), E2E `_Flow` („70,00 L getankt“), `_Months` (leerer laufender Monat 0,00 €). Die Gesamtsumme bleibt als nicht angezeigter Modellwert (in ADR und Hilfe so beschrieben). |
| 1 | Überschrift „Monatsanalyse <Monat>“ | ja | `MonthAnalysisHeading` („Monatsanalyse Oktober 2026“, Ebene 4). Steht über der Monatskarte statt über den Kacheln; in ADR 0010 begründet (siehe unten). |
| 1 | Verbrauchsverlauf: Balkendiagramm der letzten 6 mit hervorgehobenem letztem Balken | ja (Detail offen, siehe „Kleinigkeiten“) | `ConsumptionChartMaxPoints = 6`, `LineChartDrawable` entfernt, `BarChartDrawable` mit `IsLast` (Akzentfarbe, fette Wertschrift). UIA: „Verlauf, 6 Werte, zuletzt 6,24 L/100 km, niedrigster 5,24, höchster 6,24“ bei 9 berechneten Werten (nachgerechnet). Unit `ConsumptionChart_ShowsLastSixValues_…`, `Layout_OnlyLastItemIsHighlighted`. |
| 1 | ADR 0010 „Barrierefreiheit“ widersprüchlich | ja | Nur noch eine Aussage: kurze `SemanticProperties.Description` am Diagramm, Zusammenfassung als eigener Text. Per UIA bestätigt (Gruppe „Balkendiagramm des Verbrauchsverlaufs“, danach Text „Verlauf, …“). |
| 1 | `beschreibung.md` beschreibt alten Stand der Sprachausgabe | ja | Absatz „Barrierefreiheit“ neu; Kacheln, Monatsanalyse, Balkendiagramme und Hinweistext je Art beschrieben. |
| 1 | `ablauf-technisch.md`: AutomationId `…MonthsNotice` fehlt | ja | Liste ergänzt um `MonthsNotice`, `AverageUnit`, `CostPer100KmUnit`, `MonthAnalysisHeading`, `MonthCostLabel`, `MonthCost`, `MonthQuantity`; `TotalCost` entfernt; „fehlt ohne Werte“ bei beiden Diagrammen. |
| 2 | Irreführender Startverbrauch-Verweis (Hybrid-Strom bzw. bereits hinterlegter Startverbrauch) | ja | `TankbookKindStatistics.OffersStartConsumption = isStartKind && StartConsumption is null`; `GetTooFewEntriesNotice(bool)`. Unit `TankbookKindStatisticsViewModelTests_Notice` (8, u. a. `Hybrid_WithStartConsumption_ElectricityNoticeHasNoStartHint`, `Hybrid_WithoutStartConsumption_…`, `Fuel_WithStartConsumption_NoNotice`). E2E `_Fallback` mit Hybrid „Plugin“ (Startverbrauch 5,4, ein Ladevorgang): Hinweis genau der erste Satz. Praktisch am Zwischenstand: „Hybrid Start“ Strom ohne Verweis, „Hybrid ohne“ Kraftstoff mit, Strom ohne Verweis, „Nur Teil“ (Diesel ohne Startverbrauch) mit Verweis. |
| 3 | Abgeschnittene erste/letzte Achsen- und Wertbeschriftung | ja (im Code und per Test; Sichtprüfung nicht möglich) | `ChartGeometry.PlaceText` legt die 60-pt-Fläche mittig und schiebt sie an den Rand der Diagrammfläche, Ausrichtung dann links bzw. rechts; `BarChartDrawable` zeichnet in diese Fläche. Unit `Layout_TextAreas_StayInsideBounds` (1/2/6/12 Werte, 200–308 pt), `_AlignAtEdges`, `_StayInsideVertically`. Die Diagrammfläche ist laut UIA 308 pt (390 px) bzw. 278 pt (360 px) breit. |
| 3 | Leere Diagrammfläche ohne Datenpunkte, Ansage „…diagramm“ | ja | `IsVisible="{Binding ConsumptionChart.HasData}"` bzw. `CostChart.HasData`. Per UIA fehlt `…ConsumptionChart` bei „Elektro Start“, „Hybrid Start“, „Hybrid ohne“ (beide Arten) und „Nur Teil“; dort steht nur „Noch kein Verlauf: Es liegen keine berechneten Werte vor.“. E2E `_Fallback` prüft das Fehlen (Diesel, Startverbrauch, Hybrid-Strom). |

**Hinweise aus 14.1:**

| Hinweis | Stand |
|---|---|
| Sprachausgabe „Ø Verbrauch“ | behoben: Beschriftungen der Kacheln tragen „Durchschnittsverbrauch“ bzw. „Kosten pro 100 km“ (per UIA bestätigt) |
| Sprachausgabe des Leerwerts „–“ | unverändert (Kleinigkeit) |
| Leck über `RequestedThemeChanged` | behoben: `ThemeSubscription` mit `WeakReference`, Abmeldung bei Handler-Verlust und Selbstabmeldung |
| README nennt Schritt 11 | behoben: Stand Schritt 14 (0.1.18) mit Schritten 12 und 13 |
| CHANGELOG des Zwischenstands knapp, mit Umschrift | behoben: vollständiger `changes.log`-Eintrag mit Umlauten |
| `MonthsNotice` nennt „24“ fest; Ladefehler lässt alte Auswertung stehen; Balkenwerte ganze Euro | unverändert, ohne Einfluss |
| Jahreszahl auf der Achse bei mehr als einem Jahr Spanne | Kostenverlauf beschriftet „MM/yy“; Verbrauchsverlauf weiter „dd.MM.“ (bei 6 Werten selten mehrdeutig) |
| Unversioniertes `docs/features/` | weiterhin vorhanden, nicht committet; Löschen ist Sache des Anwenders |

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Je Fahrzeug Statistiken gemäß Designentwurf | Bereich „Auswertung“ der Vorgangsseite: je Art Kachelraster, Monatsanalyse-Karte, Verbrauchsverlauf (Balken, 6), Kostenverlauf (Balken, 12 Monate), Monatsliste. Abweichungen in ADR 0010. | UIA-Layout an 9 Fahrzeugen; E2E `TankbookStatisticsE2ETests_*` |
| Durchschnittsverbrauch L/100 km bzw. kWh/100 km | unverändert: Σ Intervallmengen / Σ Strecken × 100, Rundung am Ende | Unit `TankbookStatisticsCalculatorTests_Average`; Zwischenstand „Benzin viele“ 5,58 L/100 km, „Elektro wenig“ 19,60 kWh/100 km (beide nachgerechnet) |
| Kosten pro 100 km | unverändert: Σ Intervallkosten / Σ Strecken × 100 | Unit `_Cost`; Zwischenstand 10,22 € bzw. 6,86 € (nachgerechnet); E2E `_Flow` 11,15 € |
| Monatskosten | lückenlose Reihe je Art nach Ortszeit, Liste bis 24 Monate, Karte für den laufenden Monat mit Menge | Unit `_Months` (+ Menge), `_MonthCard`; E2E `_Months`; Zwischenstand „April 2026: 88,51 €“ = 66,43 € + Teilbetankung 22,08 € |
| Grafische Verläufe von Verbrauch und Kosten | MAUI Graphics, nur noch `BarChartDrawable`, Geometrie in `ChartGeometry` | Unit `ChartGeometryTests` (17 Fälle inkl. Textflächen); UIA: Diagramm-Gruppen mit 140 pt Höhe |
| Teilbetankungen: Menge in den nächsten Vollvorgang, Kosten in die Kostenwerte | unverändert | Unit `ConsumptionCalculatorTests_Interval`; E2E `_Partial`; Zwischenstand „Benzin viele“ mit zwei Teilbetankungen |
| Zu wenige Vorgänge: Startverbrauch oder verständlicher Hinweis | Startverbrauch der Startart gekennzeichnet; Hinweis mit Verweis nur, wenn ein Startverbrauch für die Art gelten würde und fehlt | siehe Abweichung 2 |
| Fahrzeug ohne Einträge | Hinweis „Noch keine Auswertung: …“ | E2E `_Fallback`; Zwischenstand „Leer“ |
| Offline, Aktualisierung nach Erfassen/Korrigieren/Löschen | unverändert | E2E `_Update` (Monatskarte 130,13 € → 74,36 €), `_Persistence`, `OfflineE2ETests_Tankbook` |
| Tests (Beispieldaten, Monatsgrenzen, Teilbetankungen, ohne Einträge; FlaUI Kennzahlen nach Erfassen) | +23 Unit-Tests gegenüber 14.1 (1103 → 1126), E2E-Prüfungen auf Kachel- und Monatskarten-IDs umgestellt | siehe „Praktische Verifikation“ |
| Keine Schemaänderung | unverändert, keine Migration | Zwischenstand legt 7 Migrationen bis `AddTankbookEntryDetails` an, `integrity_check` = ok |
| Zusatz 13a–c | unverändert erfüllt (siehe 14.1) | Unit/E2E grün |
| Windows-Zwischenstand | `review-versions/0.1.18_2026-10-12/` neu gebaut auf `3e29a5e` | siehe unten |
| iOS: keine neue Abhängigkeit, csproj unverändert | siehe „Datenschutz und iOS“ | |

### Bewertung der Designabweichungen in ADR 0010

| Abweichung | Begründung trägt? |
|---|---|
| Getrennte Blöcke je Art (Hybrid) | ja; Liter und kWh dürfen nicht gemischt werden (ADR 0009). |
| „Monatsanalyse“ über der Monatskarte statt über den Kacheln | ja; die Kacheln zeigen Werte über alle Vorgänge, eine Monatsüberschrift darüber wäre fachlich falsch. |
| „–“ plus „noch nicht berechenbar“ bzw. Startverbrauch in der Kachel | ja; notwendiger Zustand, den der Entwurf nicht zeigt. |
| Balken beginnen bei 0 (Entwurf kappt die Achse) | ja; gekappte Balken übertreiben Unterschiede, der genaue Wert steht über dem Balken. |
| Keine Benchmarklinie/„Ziel“ | ja; kein Zielwert im Datenmodell. |
| Keine Trendzeilen ggü. Vormonat, keine Plakette „Effizient“ | ja; die Anforderung nennt keine Regel dafür. |
| Keine Symbole in Kacheln und Karte | trägt, aber schwächer formuliert als nötig: ADR 0004 bettet Material-Symbole bereits als Vektorpfade ein, „keine Symbolschrift“ ist also kein Hindernis. Der eigentliche Grund (rein dekorativ, Beschriftungen tragen die Information) reicht aus. |
| Kein Fahrzeug-Dropdown, keine „Aktives Fahrzeug“-Karte (inkl. „Gesamt-Ø“) | ja; das Fahrzeug bestimmt die Seite (ADR 0009). |
| Achsenbeschriftung als Datum/Monat, Kostenverlauf und Monatsliste zusätzlich | ja; die Anforderung verlangt Kostenverlauf und Monatskosten. |
| Ältere Balken in heller Abstufung mit ≥ 3:1 (Abschnitt „Aufbau“) | ja; der Entwurf nutzt im Dunkelmodus `#21262D` auf dunkler Fläche (kaum sichtbar). Gemessen: hell `#4F9E96`/`#FFFFFF` 3,15:1, dunkel `#1F8F83`/`#283044` 3,33:1; hervorgehoben 5,47:1 bzw. 7,07:1; Text 17,85:1 bzw. 11,62:1; Achse 4,76:1 bzw. 5,13:1. |

### Regressionsprüfung der Nachbesserung

- **Kacheln und Monatskarte:** Werte und Einheiten in getrennten Labels; die E2E-Tests prüfen beide
  (`TankbookE2EDriver.WaitForStatistics` teilt „6,00 L/100 km“ in Wert und Einheit). Keine verwaisten Verweise auf
  `TotalCost` in Oberfläche, Tests oder Hilfe.
- **Verbrauchsverlauf (6 Balken):** Begrenzung im ViewModel, Zusammenfassung bezieht sich auf dieselben 6 Werte; die
  Kennzahlen enthalten weiterhin alle Intervalle.
- **`LineChartDrawable` entfernt:** keine Verweise mehr in Code und Doku; `ChartKind` entfernt, `ChartData` ohne Art.
- **`OffersStartConsumption`:** nur für die Startart ohne Startverbrauch; bei Kraftstoffwechsel des Fahrzeugs
  (alte Vorgänge anderer Art) erscheint folgerichtig kein Verweis.
- **`ChartTextArea`:** Fläche nie außerhalb der Diagrammfläche, auch bei Breiten unter 60 pt (`Math.Max(bounds.Left, …)`).
- **`HasData`-Bindung:** kompilierte Bindung über `x:DataType`; per UIA wirksam.
- **Schwache Referenz für `RequestedThemeChanged`:** Ablauf korrekt (Abmeldung in `OnHandlerChanging`, Neuanmeldung in
  `OnHandlerChanged`, Selbstabmeldung bei eingesammelter View). Praktisch geprüft: Wechsel des App-Erscheinungsbilds
  hell → dunkel bei geöffneter Seite mit 4 Diagrammen und nach Neuaufbau der Liste (anderes Fahrzeug); die App blieb
  reaktionsfähig, keine Ausnahme. Das Neuzeichnen selbst war wegen des ausgeschalteten Bildschirms nicht sichtbar.
- **README und `changes.log`:** zutreffend; der `changes.log`-Eintrag ist identisch mit dem CHANGELOG des Zwischenstands.

### Darstellung (Zwischenstand, ohne Bildschirmaufnahme)

**Vorgehen:**

- Zwischenstand im Testmodus gestartet (`TANKATLAS_TEST_DATA_PATH` im Scratchpad, `TANKATLAS_TEST_WINDOW=offscreen`,
  ohne `TANKRADAR_PRICE_API_URL`, also ohne jeden Preisabruf; Schlüsselvariablen per `env -u` entfernt).
- Synthetische Daten per SQL (`seed2.py` im Scratchpad), 9 Fahrzeuge:
  - **Benzin viele:** 10 volle Vorgänge und 2 Teilbetankungen über 10 Monate.
  - **Extrem:** Diesel mit Höchstwerten, 8 × 999 L zu 9.989,00 € auf je 100 km, 3 davon im laufenden Monat.
  - **Elektro wenig:** 2 Ladevorgänge.
  - **Elektro Start:** Startverbrauch 18,5, 1 Ladevorgang.
  - **Hybrid Start:** Startverbrauch 5,4, je 1 Vorgang Kraftstoff/Strom.
  - **Hybrid ohne:** ohne Startverbrauch, je 1 Vorgang Kraftstoff/Strom.
  - **Hybrid viel:** 14 Monate, je Monat Kraftstoff und Strom.
  - **Leer:** ohne Vorgang.
  - **Nur Teil:** 2 Teilbetankungen.
- Navigation nur über UI-Automation-Muster, keine Mausklicks.
- Ausgewertet: Layout-Rechtecke und Namen aller `TankbookEntries.Statistics.*`-Elemente bei 390 und 360 px, Stichproben
  bei 375 und 430 px. Die Aufnahmen per `PrintWindow` waren einfarbig (dunkel bzw. nach dem Wechsel weiß), also nicht
  auswertbar.

**Ergebnis:**

- **Kein Element ragt aus der Karte:** Inhaltsbreite 308 pt (390 px), 293 pt (375 px), 278 pt (360 px). Alle Texte
  umbrechen statt abzuschneiden; Startverbrauch-Kennzeichnung bis 3 Zeilen, „noch nicht berechenbar“ 2 Zeilen.
- **Einheit der Kachel:** steht je nach Breite neben der Zahl (375/430 px) oder darunter (in den Messungen bei
  360/390 px). „€“ steht neben der Zahl, „kWh/100 km“ unter „19,60“ bei 360 px. Nie abgeschnitten, wie in ADR 0010
  beschrieben. Bei 390 px zeigte sich die Lage in zwei Messungen unterschiedlich, d. h. `FlexLayout` reagiert auf die
  Vorgeschichte der Größenänderungen; unkritisch.
- **Hinweistexte, leere Diagramme, Kennzahlen:** wie in der Tabelle „Abweichungen aus 14.1“.
- **Extreme Beträge:** Monatskarte „29.967,00 €“ und „2.997,00 L getankt“ passen (Menge bricht unter den Betrag); siehe
  „Kleinigkeiten“ zu „9.989,00“.
- **Diagrammtexte**, gemessen mit Segoe UI 11 px; „€“ einschließlich Leerzeichen ≈ 9 px:

  | Text | normal | fett |
  |---|---|---|
  | „6,24“ | 20 | 22 |
  | „999,00“ | 32 | 35 |
  | „1.234 €“ / „9.989 €“ | ≈ 35 | ≈ 38 |
  | „29.967 €“ | ≈ 41 | ≈ 44 |
  | „05.10.“ (Achse) | 29 | 31 |

  - Abstand der Balkenmitten bei 6 Balken: 47,3 pt (390 px), 44,8 pt (375 px), 42,3 pt (360 px).
  - Bis zu vierstellige Monatsbeträge haben auf jeder Breite mindestens 5 pt Luft. Bei fünfstelligen Monatsbeträgen
    (≥ 10.000 € je Art und Monat) berühren sich die Werte erst unter 375 pt. Das bestätigt den bewusst offenen Punkt
    der Selbstauskunft als niedrig.
  - Die Achsenbeschriftungen der Stellen 2 und 3 bei 6 Balken liegen benachbart, haben aber rund 14 pt Luft.
  - Die Textflächen liegen nach den Unit-Tests stets innerhalb der Diagrammfläche; erste und letzte Beschriftung werden
    damit nicht mehr abgeschnitten. Optisch nicht bestätigt (Bildschirm aus).

### Bewusst offene Punkte der Selbstauskunft

| Punkt | Bewertung |
|---|---|
| Zusammengesetzte Sprachausgabe je Kachel | Niedrig. Per UIA liest die Sprachausgabe „Durchschnittsverbrauch“, „19,60“, „kWh/100 km“, „Berechnet aus vollen Vorgängen“ als einzelne Texte in sinnvoller Reihenfolge. |
| Berührung der Wertbeschriftungen bei sehr langen Beträgen und 6 Balken | Niedrig, siehe Messung oben (erst bei fünfstelligen Monatsbeträgen unter 375 pt). |
| Dispatcher für `Invalidate` im Theme-Ereignis | Niedrig. Zwei Wechsel des Erscheinungsbilds liefen unter Windows ohne Fehler; unter iOS nicht prüfbar. |
| Keine Unit-Tests für `ChartGraphicsView` | Vertretbar: reine Anbindung, Logik in `ChartGeometry` getestet, `Views/Charts` aus der Abdeckung ausgenommen. |

Die Selbstauskunft „Code-Review durch Unteragent ohne hohe/mittlere Befunde“ ließ sich nicht anhand eines Dokuments
prüfen: Im Feature-Verzeichnis liegt nur das Review der ersten Runde (09:39). Eine eigene Durchsicht der Nachbesserung
ergab keinen hohen oder mittleren Befund.

### Praktische Verifikation

**`scripts/local-ci.ps1`**

- Vollständig von 11:51 bis 12:04 (2026-10-12), E2E off-screen.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` entfernt, ohne
  sie zu lesen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests (32), Workflow-Validierung (11), iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK, 0 Warnungen (`Tankradar.MAUI.dll` Release um 11:52:39 neu geschrieben) |
| Unit | 1126/1126 |
| Integration | 122/122 |
| Zeilenabdeckung | 93,3 % (Schwelle 70 %) |
| FlaUI-E2E | **97/98** in 11 min 26 s. Alle 7 `TankbookStatisticsE2ETests_*`, `TankbookEntriesE2ETests_PartialHint`, `OfflineE2ETests_Tankbook` und `DiagnosticsCaptureE2ETests.PassingTestBodyProducesNoDiagnostics` grün. Gescheitert nur `DiagnosticsCaptureE2ETests.FailingTestBodyProducesScreenshotUiTreeAndErrorFile` („Der Screenshot zeigt keinen Fensterinhalt (einfarbig).“). |
| iOS-Compile-Prüfung (`net10.0-ios`) | OK (bekannte Warnung `MAUI1001` aus dem unveränderten csproj) |
| Gesamt | Exit-Code 1 allein wegen des Umgebungseffekts |

**Umgebungseffekt „Screenshot einfarbig“**

- **Einzelwiederholung** um 12:05: `dotnet test … --filter DiagnosticsCaptureE2ETests` ergab 1/2, derselbe Test mit
  derselben Meldung.
- **Ursache:** Bildschirm aus. Um 12:06 lag die letzte Eingabe 5 947 s zurück, der Bildschirm schaltet am Netzteil nach
  900 s ab (`VIDEOIDLE` = `0x384`). Die Sitzung war aktiv (`query user`), `LogonUI.exe` lief nicht.
- **Bestätigung:** Eigene `PrintWindow`-Aufnahmen des Zwischenstands waren ebenfalls einfarbig.
- **Code:** Der Schritt ändert weder den Test noch `E2EDiagnostics` noch `OffscreenWindow`.
- **Bewertung:** bekannter Umgebungseffekt wie in den Abnahmen 10 und 11. Die Selbstauskunft „E2E ohne Fehler“ ist
  damit plausibel (bei eingeschaltetem Bildschirm).

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei einem Push nach `refs/heads/task/…-tankradar`: lokal `3e29a5e`, Remote-Stand `799efb3`.
  - Schlüsselvariablen wie oben entfernt.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py` (`799efb3..3e29a5e`) | OK, 10 Commits |
| `forbidden-patterns-check.py --all --strict` | OK (766 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (555 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (551 Dateien) |
| `test-execution-check.py` (Build Debug, Unit, Integration; E2E im Hook standardmäßig aus) | OK: Unit 1126/1126, Integration 122/122 |
| Gesamt | Exit-Code 0, „Pre-Push-Prüfungen erfolgreich abgeschlossen.“ (12:06–12:07) |

Zusätzlich meldet `conventional-commits-check.py --base main --end HEAD` OK (139 Commits).

**Wiederholte E2E-Läufe (Flakiness)**

- **Filter:** `TankbookStatisticsE2ETests|TankbookEntriesE2ETests_PartialHint|OfflineE2ETests_Tankbook`, also alle neuen
  und geänderten E2E-Klassen des Schritts mit 9 Tests.
- **Ausführung:** Release, `--no-build`, off-screen, nacheinander.

| Lauf | Ergebnis | Dauer |
|---|---|---|
| 1 | 9/9 | 1 min 26 s |
| 2 | 9/9 | 1 min 27 s |
| 3 | 9/9 | 1 min 26 s |

Zusammen mit `local-ci` lief jede neue bzw. geänderte E2E-Klasse viermal ohne Ausfall.

### Zwischenstand `review-versions/0.1.18_2026-10-12/`

- **Herkunft:**
  - erzeugt um 11:48, nach dem Commit `3e29a5e` (11:23), per `.gitignore` ausgeschlossen
  - `FileVersion` `0.1.18.0`, `ProductVersion` `0.1.0+3e29a5ed…`
- **`CHANGELOG.md`:**
  - UTF-8 mit BOM, ohne Kodierungsfehler.
  - Wortgleich mit dem `changes.log`-Eintrag 0.1.18 (mit Umlauten), inhaltlich zutreffend.
- **Start:**
  - Testmodus wie oben, PID 17104 und für eine Nachmessung PID 2524, Fenstertitel „Tankatlas“.
  - 7 Migrationen bis `AddTankbookEntryDetails`, `integrity_check` = ok.
  - Beide eigenen Prozesse wurden per PID beendet. Danach lief kein `Tankradar.MAUI`- und kein `testhost`-Prozess mehr.

### Datenschutz und iOS

- **Nur lokal:**
  - Die Auswertung liest nur die lokale SQLite-Datenbank. Der Diff enthält keine URL, keinen `HttpClient` und keine
    neue Protokollzeile außer dem Ausnahmetyp in `TankbookStatisticsViewModel`.
  - Es wird nichts gespeichert und nichts weitergegeben.
- **Keine Produktivendpunkte:**
  - Die Tests nutzen Fakes, Testdatenbanken und im Offline-Szenario einen geschlossenen Port.
  - Der Zwischenstand lief im Testmodus ohne Preis-Endpunkt; Abrufe werden dort verweigert.
- **API-Schlüssel:**
  - Kein Schlüssel im Diff; `forbidden-patterns-check.py --all --strict` ist grün.
  - Die Protokolle dieser Abnahme enthalten keine `apikey=`-Fundstelle.
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben, nur per `env -u` entfernt.
- **iOS:**
  - Kein `.csproj`, `.props` oder `.json` im Diff, nur `coverlet.runsettings` (Ausschluss `**/Views/Charts/**`).
  - `MtouchInterpreter=all` und 5 × `TrimmerRootAssembly` bestehen unverändert.
  - Keine neue Abhängigkeit (`GraphicsView`/`Microsoft.Maui.Graphics` gehören zu MAUI).
  - Zeichencode ohne Reflection, Bindings kompiliert (`x:DataType`); iOS-Compile-Prüfung OK.

### Kleinigkeiten und Empfehlungen (ohne Einfluss auf die Erfüllung)

- **Verbrauchsverlauf, Detail nicht in ADR 0010 festgehalten:**
  - Im Entwurf steht das Diagramm innerhalb der Karte „Gesamtkosten <Monat>“ auf einer eigenen, leicht abgesetzten
    Fläche (`surface-container-low`) mit der Überschrift „Verlauf (L/100km letzte 6 Betankungen)“; der vorletzte Balken
    ist zusätzlich in Grün abgestuft.
  - Umgesetzt ist das Diagramm unter der Karte direkt auf der Kartenfläche, mit der Überschrift „Verbrauchsverlauf der
    letzten Vorgänge“ und nur einem hervorgehobenen Balken.
  - Das ist fachlich gleichwertig und optisch eine Kleinigkeit. Der Vollständigkeit halber sollte ADR 0010 es mit
    einem Satz nennen.
- **Lange Kachelwerte auf schmalen Breiten:**
  - Zahlen mit vier Stellen vor dem Komma (z. B. „9.989,00“ für Kosten / 100 km) werden bei 360 und 375 px innerhalb
    der Zahl auf zwei Zeilen umbrochen (UIA-Höhe 65 statt 33 pt); bei 390 px passen sie.
  - Solche Werte entstehen nur aus unplausiblen Daten, etwa einem vertippten Kilometerstand mit 1 km Strecke.
  - ADR 0010 („auch lange Werte … passen“) gilt für realistische Werte.
  - Empfehlung für später: Schriftgröße der Kachelzahl bei langen Werten verkleinern oder `LineBreakMode="NoWrap"` mit
    Verkleinerung.
- **Aufbau der Kachel unter iOS:** Wert und Einheit sind getrennte Elemente; VoiceOver liest sie nacheinander. Optional
  lässt sich die Kachel zu einem Element mit zusammengesetzter Beschreibung zusammenfassen.

### Sonstiges

- **Keine Änderungen am Repository:**
  - Keine Commits, keine Pushes und keine Codeänderungen. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert: keine Datei jünger als der Beginn der
    Abnahme. Der Zwischenstand wurde nur ausgeführt; seine Daten lagen im Scratchpad.
  - Testergebnisse liegen in den ausgeschlossenen Ordnern `TestResults/` und `coverage-report/` sowie im Scratchpad
    (`abn14b/`, dort auch Seed-Skript, UIA-Messungen und die einfarbigen Aufnahmen).
- **Vorübergehende Systemeinstellung:**
  - Für die Prüfung hell/dunkel wurde die Benutzereinstellung `AppsUseLightTheme` (HKCU) von 0 auf 1 und nach rund
    20 s wieder auf 0 gesetzt (12:12), jeweils mit der üblichen Benachrichtigung `WM_SETTINGCHANGE` „ImmersiveColorSet“.
  - Der Ausgangszustand (`AppsUseLightTheme` = 0, `SystemUsesLightTheme` = 0) ist wiederhergestellt.
- **Keine Energieeinstellungen** wurden geändert. Der Bildschirm wurde nicht geweckt; es gab keine Maus- oder
  Tastatureingaben.
- **Berechtigungen:** Keine Aktion wurde vom Berechtigungssystem abgelehnt.
