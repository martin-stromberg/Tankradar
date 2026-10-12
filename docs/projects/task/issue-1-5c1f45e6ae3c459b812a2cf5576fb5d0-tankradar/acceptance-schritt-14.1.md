# Abnahmeprüfung – Entwicklungsschritt 14 (Verbrauchs- und Kostenauswertung)

## Ergebnis

**Status:** Anforderung nicht vollständig erfüllt

Fachlich ist die Auswertung korrekt und gut abgesichert:

- **Durchschnittsverbrauch:** mengengewichtet, Σ Intervallmengen / Σ Intervallstrecken × 100.
- **Kosten pro 100 km:** Σ Intervallkosten / Σ Intervallstrecken × 100, auf denselben Intervallen.
- **Monatskosten:** je Art getrennt, nach Ortszeit, lückenlos bis zum aktuellen Monat.
- **Startverbrauch:** Ersatzwert, solange keine zwei Vollvorgänge vorliegen.
- **Aktualisierung:** nach Erfassen, Korrigieren und Löschen, offline, ohne Migration.

Alle Prüfläufe sind grün und bestätigen die Selbstauskunft: Unit 1103, Integration 122, E2E 98. Die neuen E2E-Klassen
liefen 3 × fehlerfrei, und der Zwischenstand 0.1.18 startet.

Nicht erfüllt sind Teile der Vorgabe „gemäß Designentwurf, jede Abweichung wird festgehalten“ und des „verständlichen
Hinweises“. Außerdem hat die Diagrammdarstellung zwei sichtbare Mängel (siehe „Abweichungen“). Die Korrekturen aus
`7a71a6d` sind in der Sache richtig. Sie haben aber ADR 0010 und die Anwenderhilfe in einem veralteten bzw.
widersprüchlichen Stand hinterlassen.

## Abweichungen

- [ ] **1. Designabweichungen der Auswertung sind nicht oder falsch dokumentiert.**
  - **Vorgabe:** Laut Projektplan („historische Statistiken gemäß Designentwurf“) und Vorgehensentscheidung („Designentwurf
    … verbindlich. Jede Abweichung … wird in der Projektdokumentation festgehalten“) muss die Auswertung dem Entwurf folgen
    oder die Abweichung dokumentieren. Präzedenz ist die Abnahme 8.1.
  - **Falsche Aussage in ADR 0010:** Der Abschnitt „Abweichungen vom Designentwurf“ behauptet: „Übernommen sind die
    Kacheln Ø Verbrauch, Kosten pro 100 km und Monatskosten, das Verlaufsdiagramm mit Wert am Datenpunkt“. Tatsächlich
    zeigt die Seite alles untereinander in einer Karte (Beschriftung `FieldLabel` und Wert in `headline-md`), wie die
    Aufnahmen bei 390 px Breite belegen.
  - **Nicht übernommen und nirgends festgehalten:**
    - das zweispaltige Kachelraster „Ø Verbrauch“ / „Kosten / 100 km“ mit großer Zahl (`price-hero`, 28 px/800),
      abgesetzter Einheit und Symbol
    - die Karte „Gesamtkosten <Monat>“ mit Betrag des laufenden Monats und getankter Menge („98,50 L getankt“). An ihrer
      Stelle steht „Gesamtkosten“ über alle Vorgänge, eine im Entwurf nicht vorgesehene Kennzahl.
    - die Überschrift „Monatsanalyse <Monat>“
    - der Verbrauchsverlauf: Der Entwurf zeigt ein Balkendiagramm der letzten 6 Betankungen mit hervorgehobenem letztem
      Balken auf eigener Fläche. Umgesetzt ist ein Liniendiagramm der letzten 12 Werte.
  - **Veraltet nach `7a71a6d`:**
    - ADR 0010 „Barrierefreiheit“ sagt zuerst, die Zusammenfassung sei die `SemanticProperties.Description` des
      Diagramms, und zwei Sätze später das Gegenteil.
    - `docs/help/Tankbuch/beschreibung.md` („die Sprachausgabe liest sie als Beschreibung des Diagramms vor“) beschreibt
      noch den alten Stand.
    - In `ablauf-technisch.md` fehlt die neue AutomationId `TankbookEntries.Statistics.{Key}.MonthsNotice`.
  - **Umsetzbar, eine von zwei Varianten:**
    - **(a)** Kennzahlen wie im Entwurf darstellen: Kachelraster für Ø Verbrauch und Kosten pro 100 km sowie eine Karte
      „Monatskosten <laufender Monat>“ mit Betrag und Menge. Die Daten liegen mit `Months[0]` bzw. der Menge der
      Vorgänge des Monats bereits vor.
    - **(b)** Jede der genannten Abweichungen mit Begründung in ADR 0010 aufnehmen und die falsche Aussage streichen.
    - In beiden Fällen ADR 0010, `beschreibung.md` und `ablauf-technisch.md` an den Stand nach `7a71a6d` anpassen.
    - Empfehlung: (a) für die Kacheln und die Monatskarte. (b) genügt für die Diagrammart, denn eine Linie ist für einen
      Verlauf fachlich vertretbar.

- [ ] **2. Irreführender Hinweis bei zu wenigen Vorgängen (Hybrid bzw. Art ohne gültigen Startverbrauch).**
  - **Problem:** `TankbookKindStatisticsViewModel` zeigt bei Quelle `None` immer `TooFewEntriesNotice`: „Für den Verbrauch
    werden mindestens zwei volle Vorgänge benötigt. Alternativ kannst du am Fahrzeug einen Startverbrauch hinterlegen.“
    Der Startverbrauch gilt aber nur für die Startart (Elektro → Strom, sonst Kraftstoff).
  - **Nachweis:** Praktisch am Zwischenstand geprüft. Hybrid „Plugin“ mit hinterlegtem Startverbrauch 5,4 L/100 km und
    einem Ladevorgang: Der Block „Strom“ rät, einen Startverbrauch zu hinterlegen. Der Startverbrauch ist aber schon
    hinterlegt und würde für Strom nie greifen.
  - **Vorgaben:** Der Projektplan verlangt einen „verständlichen Hinweis“. Der Umsetzungsplan sah den Verweis nur vor,
    „sofern nicht gesetzt“.
  - **Umsetzbar:** Den zweiten Satz nur zeigen, wenn die Art die Startart ist und kein Startverbrauch hinterlegt ist.
    Dazu z. B. ein Kennzeichen in `TankbookKindStatistics` ergänzen. Mit Unit-Test für den Strom-Block eines Hybrids und
    für ein Fahrzeug mit gesetztem Startverbrauch.

- [ ] **3. Diagrammdarstellung: abgeschnittene Randbeschriftungen und leere Diagrammflächen.**
  - **Abgeschnittene Achsenbeschriftung:** Im Liniendiagramm liegen erster und letzter Punkt 12 pt vom Rand
    (`ChartGeometry.PaddingLeft`/`PaddingRight`). Die Achsenbeschriftung wird mittig um den Punkt gezeichnet und dabei
    abgeschnitten.
    - In allen Aufnahmen bei 390 und 360 px fehlt der erste Datumsteil: „04.06.“ erscheint links angeschnitten, „12.10.“
      ohne Schlusspunkt.
    - Unter iOS wird die Zeichnung ebenfalls auf die View-Grenzen beschnitten.
    - **Umsetzbar:** erste Beschriftung linksbündig und letzte rechtsbündig ausrichten, oder den Zeichenbereich um die
      halbe Beschriftungsbreite einrücken. Gleiches gilt für den Wert am ersten bzw. letzten Punkt.
  - **Leere Diagrammflächen:** Ohne Datenpunkte bleibt eine leere 160-pt-Fläche stehen.
    - Für die Sprachausgabe heißt sie trotzdem „Liniendiagramm des Verbrauchsverlaufs“. Aufnahme: Block „Strom“ von
      „Plugin“, Text darunter „Noch kein Verlauf …“.
    - Der Umsetzungsplan legt fest: „Ohne Daten blendet die Seite das Diagramm aus (`HasData`)“. `ChartData.HasData`
      existiert, wird in der XAML aber nicht verwendet. Das Plan-Review hat das als umgesetzt abgehakt.
    - **Umsetzbar:** `IsVisible="{Binding ConsumptionChart.HasData}"` bzw. `CostChart.HasData` an den beiden
      `ChartGraphicsView`, mit E2E-Prüfung (Diagramm-AutomationId fehlt bei einem Vollvorgang).

## Hinweise

### Geprüfter Stand

- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-14-auswertung`, HEAD `ee0ece5`.
- **Basis:** lokaler Projekt-Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` = `799efb3`. Er ist
  zugleich Merge-Base; `origin` steht auf `f2067cc`.
- **Diff:** 75 Dateien, +3653/−14, in 7 Commits (`1949be4` … `ee0ece5`).
  - `ee0ece5` entfernt nur Plan- und Inventurdateien.
  - Der Code-Stand entspricht `7a71a6d`.
- **Grundlagen:**
  - `project-plan.md`: Schritt 14 mit „Zusatz aus der Abnahme von Schritt 13“ und die Vorgehensentscheidungen
    „Designentwurf“ und „Oberflächentests als Auslieferungs-Gate“
  - `requirement.md` (Logbook Management „Statistik-Auswertung“)
  - die Entscheidungstabelle des Umsetzungsplans (`git show 1949be4:…/plan.md`)
  - ADR 0009 und 0010
  - `acceptance-schritt-13.md`
  - Code-Review `docs/features/…/review-code.md` (unversioniert)
  - Designentwurf `tankbuch_verbrauchsauswertung` (hell/dunkel) als `code.html`, nur im Scratchpad entpackt. Die
    `screen.png` des Entwurfs sind keine Bilder (Inhalt „FIFE Image failed to fetch“).

### Anforderung gegen Umsetzung

| Anforderung | Umsetzung | Nachweis |
|---|---|---|
| Je Fahrzeug Statistiken auf der Tankbuch-Seite | Bereich „Auswertung“ in `TankbookEntriesPage` zwischen „Vorgang erfassen“ und Liste. `TankbookStatisticsViewModel` als `Statistics` von `TankbookEntriesViewModel`. | E2E `TankbookStatisticsE2ETests_Flow`; Aufnahmen 390/360 px |
| Durchschnittsverbrauch L/100 km bzw. kWh/100 km | `TankbookStatisticsCalculator`: Σ `IntervalQuantity` / Σ `DistanceKm` × 100, nur Intervalle `Calculated` mit Strecke > 0, Rundung am Ende (2 Stellen, `AwayFromZero`). Einheit je Art. | Unit `TankbookStatisticsCalculatorTests_Average` (u. a. 10 L/100 km + 30 L/500 km = 6,67, nicht Mittel 8,00); E2E `_Flow` 6,00 L/100 km, `_Hybrid` 20,00 kWh/100 km |
| Kosten pro 100 km | Σ `IntervalCost` (Gesamtbetrag des Vollvorgangs + Teilbetankungen dazwischen) / Σ Strecke × 100 | Unit `TankbookStatisticsCalculatorTests_Cost` (5); E2E `_Flow` 55,77 € / 500 km = 11,15 €, `_Update` 5,58 € nach Korrektur |
| Monatskosten | Σ `TotalAmount` aller Vorgänge der Art je Kalendermonat von `OccurredAt` (Wanduhrzeit, keine Umrechnung), lückenlos bis `max(letzter Vorgang, heute)`, neuester zuerst. Liste höchstens 24 Monate mit Hinweis, Balkendiagramm der letzten 12. | Unit `TankbookStatisticsCalculatorTests_Months` (9, u. a. 31.12. 23:59/01.01. 00:00, 29.02. Schaltjahr, leere Monate, `Unspecified`), `TankbookStatisticsViewModelTests_Limits`; E2E `_Months` |
| Grafische Verläufe von Verbrauch und Kosten | MAUI Graphics: Linie (Verbrauch je berechnetem Vollvorgang), Balken (Monatskosten), je höchstens 12 Punkte, Geometrie in `ChartGeometry` | Unit `ChartGeometryTests` (7); Aufnahmen. Darstellung siehe Abweichungen 1 und 3. |
| Teilbetankungen: Menge in den nächsten Vollvorgang, Kosten in die Kostenwerte | `ConsumptionCalculator` führt neben der Menge die Kostensumme je Art (Reset auch bei `NoDistance`). Teilbetankungen vor dem ersten Vollvorgang zählen nur in Monats- und Gesamtkosten. | Unit `ConsumptionCalculatorTests_Interval` (5), `Cost_PartialsIncludeCost`, `Cost_PartialBeforeFirstFull_NotInCostPer100Km`; E2E `_Partial` ((10 + 20) L / 500 km, 55,77 €) |
| Zu wenige Vorgänge: Startverbrauch oder verständlicher Hinweis | Schwelle: ein berechnetes Intervall (zwei Vollvorgänge) je Art. Darunter Startverbrauch der Startart, gekennzeichnet „Startverbrauch – noch keine zwei vollen Vorgänge“, sonst Hinweis. Kosten pro 100 km „noch nicht berechenbar“. | Unit `TankbookStatisticsCalculatorTests_Threshold` (7); E2E `_Fallback`. Hinweistext siehe Abweichung 2. |
| Fahrzeug ohne Einträge | Keine Blöcke, Hinweis „Noch keine Auswertung: …“ | Unit `Threshold_NoEntries_HasNoKinds`, `Load_NoEntries_ShowsNoEntriesNotice`; E2E `_Flow`, `_Fallback` |
| Offline, Aktualisierung nach Erfassen, Korrigieren, Löschen | Berechnung beim Lesen, nichts gespeichert. `TankbookEntriesViewModel.LoadAsync` lädt die Auswertung nach jedem Laden; überholte Ladevorgänge werden verworfen (`_loadVersion`). | Unit `TankbookEntriesViewModelTests_Statistics` (4), `Load_SupersededResult_IsDiscarded`, `Load_ResetWhileLoading_DiscardsResult`; E2E `_Update`, `_Persistence` (Neustart), `OfflineE2ETests_Tankbook` (6,00 → 3,00 → „–“ bei geschlossenem Preisdienst-Port) |
| Tests: Beispieldaten, Monatsgrenzen, Teilbetankungen, Fahrzeuge ohne Einträge; FlaUI Kennzahlen nach Erfassen | 16 neue bzw. erweiterte Unit-Klassen, 1 Integrationsklasse (2 Tests), 8 neue E2E-Klassen plus Ergänzung des Offline-Szenarios | siehe „Praktische Verifikation“ |
| Keine Schemaänderung | Keine Migration, Modell-Snapshot unverändert | Integration `TankbookStatisticsServiceTests_RealDatabase.Statistics_ExistingData_NoMigrationNeeded_Unchanged`; der Zwischenstand legt 7 Migrationen bis `AddTankbookEntryDetails` an, also keine neue |
| Zusatz 13a: Rundungsausnahme in der Hilfe | `beschreibung.md` nennt die Toleranz von 0,02 €, die Ausnahme „gerundeter Quotient“ und die Stellen 2/3/2 | Durchsicht |
| Zusatz 13b: Hinweis Teilbetankung präzisiert | `TankbookEntryTexts.PartialHint` unter dem Schalter, solange „Teilbetankung“ an ist (nennt den ersten vollen Vorgang und die Monatskosten) | Unit `TankbookEntryFormViewModelTests_PartialHint`, `PartialHint_MentionsFirstFullEntryAndCosts`; E2E `TankbookEntriesE2ETests_PartialHint` |
| Zusatz 13c (optional): Tests `mif1` und Löschfehler-Zweig | `ReceiptPhotoStoreTests_Detection` (5: `mif1`, `msf1`, unbekannte Marke, zu kurz, `heic`), `TankbookEntriesViewModelTests_DeleteResult` mit `StubTankbookEntryService` | Unit grün |
| Windows-Zwischenstand | `review-versions/0.1.18_2026-10-12/` | siehe unten |
| iOS: keine neue Abhängigkeit, `MtouchInterpreter`/`TrimmerRootAssembly` unverändert | Kein `.csproj`, `.props` oder `.json` im Diff. `MtouchInterpreter=all` und 5 × `TrimmerRootAssembly` bestehen unverändert. `GraphicsView`/`Microsoft.Maui.Graphics` gehören zu MAUI; Zeichencode ohne Reflection, Bindings mit `x:DataType` kompiliert. | `git diff --name-only`; iOS-Compile-Prüfung in `local-ci` OK |
| Dokumentation | ADR 0010, ADR 0009 „Folgen“, `docs/help/Tankbuch/*` (4 Dateien), `docs/help/index.md`, `changes.log` 0.1.18 | Durchsicht; Mängel siehe Abweichung 1 |

### Fachliche Bewertung

- **Mengengewichtung korrekt:**
  - Die Summe der Intervallmengen geteilt durch die Summe der Intervallstrecken entspricht der Volltank-Methode über den
    ganzen Zeitraum.
  - Das ungewichtete Mittel würde kurze Strecken überbewerten; ein Test sichert das ab.
  - Intervalle mit Strecke ≤ 0 und Teilbetankungen vor dem ersten Vollvorgang fallen heraus. Das ist folgerichtig, weil
    der Füllstand davor unbekannt ist.
  - Es gibt keine zweite Verbrauchsrechnung: Die Intervallwerte kommen aus derselben Kette wie die Einzelwerte aus
    Schritt 13.
- **Kosten pro 100 km korrekt:**
  - Die Kosten des schließenden Vollvorgangs plus die der Teilbetankungen im Intervall entsprechen genau dem Kraftstoff,
    der die Intervallstrecke ermöglicht hat.
  - Die Kosten des ersten Vollvorgangs und offene Teilbetankungen nach dem letzten Vollvorgang zählen richtigerweise
    nicht.
  - Die Rundung erfolgt nur am Ende.
- **Monatszuordnung und Zeitzonen:**
  - `OccurredAt` ist lokale Wanduhrzeit (`Kind` `Unspecified`, minutengenau aus Schritt 13).
  - „Heute“ ist `TimeProvider.GetLocalNow().DateTime`; beide werden ohne Umrechnung verglichen.
  - Eine Umstellung der Sommerzeit oder eine Reise in eine andere Zeitzone verschiebt also keine Monatsgrenze. Ein
    Vorgang zählt im Monat des auf dem Beleg stehenden Datums. Das ist die fachlich erwartete Zuordnung.
  - Die Monatsgrenzen sind getestet, einschließlich Jahreswechsel und Schaltjahr.
- **Randfälle:**
  - **Keine Vorgänge:** Hinweis.
  - **Ein Vorgang:** „–“ und „noch nicht berechenbar“ bzw. Startverbrauch.
  - **Nur Teilbetankungen:** Quelle `None`, Kosten erscheinen in den Monatskosten.
  - **Hybrid:** zwei getrennte Blöcke, nie gemischt; der Startverbrauch gilt nur für Kraftstoff (Hinweistext siehe
    Abweichung 2).
  - **Wechsel der Kraftstoffart:** Alte Vorgänge behalten ihre Art und bilden einen eigenen Block. Der Startverbrauch
    folgt der aktuellen Kraftstoffart, konsistent mit der Einheitenanzeige im Fahrzeugformular (Schritt 12).
  - **Gelöschte oder korrigierte Vorgänge:** Weil die Werte beim Lesen berechnet werden, sind sie immer konsistent
    (E2E `_Update`, Offline-Szenario).
- **Sehr alte Vorgänge:** Die Liste ist auf 24 Monate begrenzt (Korrektur aus dem Code-Review). Kennzahlen und
  Gesamtkosten enthalten weiterhin alles; getestet mit einem Vorgang aus 1995.

### Prüfung der Korrekturen aus `7a71a6d`

| Befund aus dem Code-Review | Korrektur | Bewertung |
|---|---|---|
| Dunkelmodus: kein Neuzeichnen beim Wechsel Hell/Dunkel | `ChartGraphicsView` abonniert `Application.RequestedThemeChanged` in `OnHandlerChanged` und meldet sich in `OnHandlerChanging` (neuer Handler `null`) ab, dann `Invalidate()`. Beim Wechsel A → B wird nicht doppelt abonniert. | In der Sache richtig, nicht getestet (`Views/Charts` aus der Abdeckung ausgenommen; ein Systemwechsel des Erscheinungsbilds wurde in der Abnahme bewusst nicht ausgelöst). Restrisiko siehe unten „Kleinigkeiten“. |
| Race beim Fahrzeugwechsel | `_loadVersion`, erhöht in `LoadAsync` und `Reset`; spätes Ergebnis und späte Ausnahme werden verworfen | Korrekt und mit 2 Unit-Tests über `TaskCompletionSource` belegt |
| Doppelte Ansage der Zusammenfassung | `SemanticProperties.Description` des Diagramms ist jetzt die kurze Konstante „Liniendiagramm des Verbrauchsverlaufs“ bzw. „Balkendiagramm der Monatskosten“; die Zusammenfassung steht nur im Label darunter | Per UIA bestätigt: Diagramm `Group` mit Namen = kurze Beschreibung, Zusammenfassung als eigener `Text`, keine Doppelung. ADR und Hilfe beschreiben noch den alten Stand (Abweichung 1). |
| Monatsliste unbegrenzt | `MonthListMaxRows = 24` plus Hinweis `MonthsNotice` | Korrekt, getestet; der Hinweistext nennt „24“ fest statt der Konstante |
| E2E `_Months` | Auf relative Monate umgestellt (läuft auch ab 2027, wenn die Liste sonst über 24 Monate hinausginge) | Sinnvoll; der Jahreswechsel bleibt durch Unit-Tests abgedeckt |

Die bewusst offen gelassenen Punkte (doppelter Code der Drawables, doppeltes Laden der Vorgänge, langer Konstruktor,
`BindableProperty` im statischen Konstruktor) sind Qualitätsbefunde ohne Einfluss auf die Erfüllung.

### Bewertung: überlappende Beschriftungen auf iPhone-Breite

**Praktisch geprüft** (Aufnahmen per `PrintWindow`, Fenster off-screen, 96 dpi, also 1 px = 1 pt):

- **Testdaten:**
  - Elektrofahrzeug mit 6 Verbrauchswerten aus 5 Zeichen („18,50“ … „19,67“) und 6 Monatsbalken („312 €“ … „635 €“).
    Das ist der ungünstigste Fall, denn bis 6 Punkte wird jeder Wert beschriftet.
  - Hybrid mit 14 Monaten (12 Punkte, nur der letzte Wert beschriftet).
- **Fensterbreiten:**
  - 390 px: Diagrammbreite 308 pt, entspricht iPhone 12–16.
  - 360 px: schmaler als das iPhone SE mit 375 pt Bildschirmbreite und rund 309 pt Diagrammbreite.

**Ergebnis:**

- **Keine gegenseitige Überlappung der Wertbeschriftungen:**
  - Benachbarte Punkte liegen bei 6 Werten auf iPhone-Breite (ab 375 pt) rund 57 pt auseinander, im 360-px-Fenster rund
    51 pt. Die Texte sind bei 11 pt höchstens etwa 31 pt breit („16,52“) bzw. etwa 30 pt („312 €“).
  - Die 60-pt-Rechtecke aus dem Code-Review überlappen, die Schrift selbst nicht.
  - Auch vierstellige Monatsbeträge („1.234 €“, rund 40 pt) bleiben unter der Balkenbreite von rund 47 pt.
- **Die Linie kann durch eine Wertbeschriftung laufen** (keine Hinterlegung, sichtbar bei „15,27“). Der Wert bleibt
  lesbar.
- **Die Schrift ist fest 11 pt** und wächst nicht mit der iOS-Textgröße. Das ist vertretbar, weil Zusammenfassung,
  Kennzahlen und Monatsliste als skalierender Text vorliegen.
- **Bewertung:**
  - Für ein iPhone-Ziel ist das akzeptabel und keine Abweichung.
  - Ein echter, auf jeder Breite sichtbarer Mangel ist dagegen das Abschneiden der ersten und letzten Achsenbeschriftung
    (Abweichung 3).
  - Empfehlung für später: bei mehr als einem Jahr Spanne die Jahreszahl zeigen, denn „17.10.“ ist dann mehrdeutig.

### Barrierefreiheit und Dunkelmodus

- **Überschriften:** „Auswertung“ (H2), Art (H3), „Verbrauchsverlauf“/„Kostenverlauf“/„Monatskosten“ (H4).
- **Textalternativen:**
  - Jedes Diagramm hat eine sichtbare Zusammenfassung, z. B. „Verlauf, 6 Werte, zuletzt 16,37 kWh/100 km, niedrigster
    15,27, höchster 19,67“.
  - Die Monatsliste ist die Tabellenalternative zum Kostendiagramm.
- **Keine Doppelansage:** per UIA geprüft (siehe oben). Ausnahme: das leere Diagramm (Abweichung 3).
- **Neue Schaltflächen:** keine (WCAG 2.5.3 nicht berührt).
- **Dunkelmodus:**
  - Die Aufnahmen entstanden im dunklen Erscheinungsbild des Systems. Linie und Balken (`#2DD4BF` auf `#283044`, rund
    7:1) und Beschriftungen (`#EEF0FF`) sind gut lesbar.
  - Hell laut Code: `#0F766E` auf `#FFFFFF` (rund 5,5:1), Text `#0F172A`, Achse `#64748B` (rund 4,8:1).
  - Alle übrigen Farben laufen über `AppThemeBinding`.
- **Kleinigkeiten:** Sprachausgaben lesen „Ø Verbrauch“ und den Leerwert „–“ ungünstig vor, etwa „durchgestrichenes O“
  bzw. „Gedankenstrich“. Empfehlung: `SemanticProperties.Description` „Durchschnittsverbrauch“ bzw. „kein Wert“.

### Praktische Verifikation

**`scripts/local-ci.ps1`**

- Vollständig von 10:28 bis 10:39 (2026-10-12), E2E off-screen.
- `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY` und `TANKRADAR_PRICE_API_KEY` wurden per `env -u` entfernt,
  ohne sie zu lesen.

| Prüfung | Ergebnis |
|---|---|
| Node-Tests, Workflow-Validierung, iOS-Deployment-Skript (2×) | OK |
| Restore, Format, Sicherheitsprüfung | OK |
| Build mit Warnungen als Fehler | OK, 0 Warnungen (`Tankradar.MAUI.dll` Release in diesem Lauf um 10:29 neu geschrieben) |
| Unit | 1103/1103 (Schritt 13: 1028) |
| Integration | 122/122 (Schritt 13: 120; neu `TankbookStatisticsServiceTests_RealDatabase` 2) |
| Zeilenabdeckung | 93,3 % (Schwelle 70 %) |
| FlaUI-E2E | **98/98** in 9 min 16 s, darunter alle 7 `TankbookStatisticsE2ETests_*`, `TankbookEntriesE2ETests_PartialHint`, `OfflineE2ETests_Tankbook` und beide `DiagnosticsCaptureE2ETests`. Kein Umgebungseffekt „Screenshot einfarbig“, eine Wiederholung war nicht nötig. |
| iOS-Compile-Prüfung (`net10.0-ios`) | OK (bekannte Warnung `MAUI1001` aus dem unveränderten csproj) |
| Gesamt | Exit-Code 0, „Lokaler Prüflauf erfolgreich.“ |

**Pre-Push-Prüfungen**

- **Vorgehen:** `.githooks/pre-push` direkt aufgerufen, ohne Push.
  - Eingabe wie bei einem Push nach `refs/heads/task/…-tankradar`: lokal `ee0ece5`, Remote-Stand `799efb3`.
  - Schlüsselvariablen wie oben entfernt.

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz | OK |
| `conventional-commits-check.py` (`799efb3..ee0ece5`) | OK, 7 Commits |
| `forbidden-patterns-check.py --all --strict` | OK (764 Dateien) |
| `format-code-style-check.py --strict` | OK |
| `csproj-xmldoc-check.py --all` | OK (554 Dateien) |
| `translation-check.py --all` | OK (keine `.resx`) |
| `enum-coverage-check.py --all --strict` | OK |
| `no-notimplemented-check.py --all --strict` | OK (550 Dateien) |
| `test-execution-check.py` (Build Debug, Unit, Integration; E2E im Hook standardmäßig aus) | OK: Unit 1103/1103, Integration 122/122 |
| Gesamt | Exit-Code 0, „Pre-Push-Prüfungen erfolgreich abgeschlossen.“ |

Zusätzlich meldet `conventional-commits-check.py --base main --end HEAD` OK (136 Commits).

**Wiederholte E2E-Läufe (Flakiness)**

- **Filter:** `TankbookStatisticsE2ETests|TankbookEntriesE2ETests_PartialHint|OfflineE2ETests_Tankbook`, also alle neuen
  und geänderten E2E-Klassen mit 9 Tests.
- **Ausführung:** Release, `--no-build`, off-screen, nacheinander.

| Lauf | Ergebnis | Dauer |
|---|---|---|
| 1 | 9/9 | 1 min 23 s |
| 2 | 9/9 | 1 min 18 s |
| 3 | 9/9 | 1 min 19 s |

Zusammen mit `local-ci` lief jede neue E2E-Klasse viermal ohne Ausfall.

### Zwischenstand `review-versions/0.1.18_2026-10-12/`

- **Herkunft:**
  - erzeugt um 10:27, per `.gitignore` ausgeschlossen (`.gitignore:435`)
  - `FileVersion` `0.1.18.0`, `ProductVersion` `0.1.0+7a71a6dd…`
  - Gebaut wurde also auf `7a71a6d`, 5 s vor dem Commit `ee0ece5`. Der Code ist identisch, denn
    `git diff 7a71a6d ee0ece5 -- src` ist leer.
- **`CHANGELOG.md`:**
  - UTF-8 mit BOM, ohne Kodierungsfehler.
  - Inhaltlich zutreffend, aber sehr knapp (ein Satz) und mit Umschrift („praezisierter“).
  - Die früheren Stände übernahmen den `changes.log`-Eintrag mit Umlauten. Empfehlung: künftig wieder den
    `changes.log`-Eintrag übergeben. Das ist keine Abweichung, weil „kurzer Changelog“ gefordert ist.
- **Start:**
  - Testmodus mit `TANKATLAS_TEST_WINDOW=offscreen`, eigenem Datenverzeichnis im Scratchpad und
    `TANKRADAR_PRICE_API_URL` auf einem geschlossenen Loopback-Port, ohne Schlüsselvariablen.
  - PID 32348 lief, Fenstertitel „Tankatlas“.
  - 7 Migrationen bis `AddTankbookEntryDetails`, `integrity_check` = ok.
- **Sichtprüfung:**
  - Synthetische Fahrzeuge und Vorgänge per SQL eingefügt, Navigation nur über UI-Automation-Muster (keine Mausklicks).
  - Die Auswertung zeigte die erwarteten Werte, z. B. Hybrid 6,14 L/100 km und 12,19 € / 100 km bei 14 Monaten mit
    12 Verlaufspunkten.
  - Grundlage der Abweichungen 2 und 3 und der Bewertung oben.
- **Aufräumen:** Ein zweiter Start (PID 41148) diente der Prüfung von Hybrid und leerem Diagramm. Beide eigenen
  Prozesse wurden per PID beendet. Danach lief kein `Tankradar.MAUI`- und kein `testhost`-Prozess mehr.

### Datenschutz

- **Nur lokal:**
  - Die Auswertung liest nur die lokale SQLite-Datenbank. Im Diff gibt es weder `HttpClient` noch eine URL.
  - Das einzige neue Protokoll (`TankbookStatisticsViewModel`) nennt nur `{ExceptionType}`.
  - Es wird nichts gespeichert und nichts weitergegeben.
- **Keine Produktivendpunkte in Tests:**
  - Unit-Tests nutzen Fakes bzw. SQLite im Arbeitsspeicher, Integrationstests Dateien im Testverzeichnis.
  - Die Auswertungs-E2E laufen ohne Mock-Server mit isolierter Testdatenbank; das Offline-Szenario nutzt einen
    geschlossenen Port.
  - Die praktischen Starts liefen im Testmodus mit Loopback-Adresse.
- **API-Schlüssel:**
  - Kein Schlüssel und keine schlüsselartige Kennung im Diff; `forbidden-patterns-check.py --all --strict` ist grün.
  - Die Protokolle dieser Abnahme (`local-ci`, Pre-Push) enthalten keine `apikey=`-Fundstelle.
  - `FUEL_PRICE_API_KEY` wurde weder gelesen noch ausgegeben, nur per `env -u` entfernt.
- **Testdaten:** Die Daten der Sichtprüfung sind synthetisch und liegen nur im Scratchpad.

### Kleinigkeiten ohne Einfluss auf die Erfüllung

- **Abonnement für den Wechsel des Erscheinungsbilds:**
  - Nach jedem Speichern oder Löschen erzeugt das `BindableLayout` neue Diagramm-Views.
  - Werden die Handler entfernter Kinder nicht getrennt (MAUI trennt automatisch vor allem beim Verlassen einer Seite),
    bleiben die alten Views über `Application.RequestedThemeChanged` bis zum Schließen der Seite bzw. der App referenziert.
  - Das ist ein kleines, begrenztes Leck; nur aus dem Code abgeleitet, nicht gemessen.
  - Robuster wäre ein schwaches Abonnement oder die Farbwahl über bindbare `AppThemeBinding`-Farben.
- **Bei einem Ladefehler der Vorgangsliste** bleibt die vorherige Auswertung stehen, weil `Statistics.LoadAsync` nur im
  Erfolgsfall läuft. Das ist praktisch bedeutungslos.
- **Balkenwerte sind auf ganze Euro gerundet** („5 €“ für 5,25 €). Die Zusammenfassung und die Liste nennen den genauen
  Betrag.
- **README:** Der Statusabsatz nennt noch „Entwicklungsschritt 11 umgesetzt“. Die Schritte 12–14 fehlen; das ist seit
  Schritt 12 offen. Empfehlung: vor dem nächsten Pre-Release nachziehen. Für Schritt 14 nicht gefordert.
- **Feature-Verzeichnis:**
  - Unter `docs/features/task/…--schritt-14-auswertung/` liegen unversioniert `review-code.md`, `review.md` und
    `inventory/test-results/`, dazu weiterhin das Verzeichnis aus Schritt 2.
  - Sie sind nicht committet; das Löschen ist Sache des Anwenders.

### Sonstiges

- **Keine Änderungen am Repository:**
  - Es gab keine Commits, keine Pushes und keine Codeänderungen. Angelegt wurde nur dieser Bericht.
  - `review-versions/`, `drafts/` und `design-draft/` blieben unverändert: keine Datei jünger als der Beginn der Abnahme.
    Der Zwischenstand wurde nur ausgeführt; seine Daten lagen im Scratchpad.
  - Testergebnisse liegen in den per `.gitignore` ausgeschlossenen Ordnern `TestResults/` und `coverage-report/` sowie
    im Scratchpad (`abn14/`, dort auch die Aufnahmen).
- **Keine Energieeinstellungen** wurden geändert, und keine Aktion wurde vom Berechtigungssystem abgelehnt.
