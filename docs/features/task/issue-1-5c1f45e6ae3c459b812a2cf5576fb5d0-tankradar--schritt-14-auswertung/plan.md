# Umsetzungsplan: Tankradar Schritt 14 – Verbrauchs- und Kostenauswertung

## Übersicht

Je Fahrzeug erhält die Seite „Tank- und Ladevorgänge“ (`TankbookEntriesPage`) einen Bereich „Auswertung“: Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten sowie ein Verbrauchs- und ein Kostenverlaufsdiagramm, je Art (Kraftstoff/Strom) getrennt. Die Werte werden beim Lesen aus den vorhandenen Vorgängen berechnet (`ConsumptionCalculator`/`TankbookEntryOverview`/`TankbookEntryOrder` werden erweitert und wiederverwendet), ein neuer `ITankbookStatisticsService` liefert die Modelle, ein neues ViewModel stellt sie dar. Die Diagramme werden mit MAUI Graphics selbst gezeichnet (ADR 0010). Dazu kommen die Nacharbeiten aus Schritt 13 (Hilfetext Rundungsausnahme, präziserer Teilbetankungs-Hinweis, Tests für `DetectExtension` `mif1` und den Löschfehler-Zweig). Keine Schemaänderung, keine neue Abhängigkeit.

## Designentscheidungen

Die in `requirement.md` offenen Fragen sind hier begründet entschieden und gelten als geklärt.

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Schwelle „zu wenige Vorgänge“ (Frage 1) | Durchschnittsverbrauch und Kosten/100 km erscheinen, sobald je Art mindestens ein berechnetes Intervall vorliegt (= zwei Vollvorgänge, 1 `Calculated`-Wert). Darunter: Startverbrauch (falls vorhanden), sonst Hinweis. | Ein Intervall ist die kleinste Einheit, für die `ConsumptionCalculator` einen Wert liefert; eine höhere Schwelle würde vorhandene Werte verbergen. |
| Startverbrauch (Frage 2) | Nur Ersatzwert für den Durchschnittsverbrauch, solange kein berechnetes Intervall der Art existiert; gekennzeichnet als „Startverbrauch“ (Quelle `TankbookAverageSource.StartConsumption`); danach nie mehr, kein Mischen mit Berechnetem. Er gilt nur für die Art, deren Einheit er trägt: Elektro-Fahrzeug -> `Electricity`, sonst -> `Fuel` (bei Hybrid also nur Kraftstoff). Kosten/100 km bekommen keinen Ersatzwert. | Der Startverbrauch ist ein Schätzwert des Anwenders; die Kennzeichnung verhindert Verwechslung, die Einheit (L bzw. kWh) bleibt konsistent (`TankbookTexts.GetConsumptionUnit`). |
| Durchschnittsverbrauch | Mengengewichtet: Σ Intervallmengen / Σ Intervallstrecken × 100, 2 Nachkommastellen (`MidpointRounding.AwayFromZero`, wie `ConsumptionCalculator`). Intervallmenge = Menge des Vollvorgangs + dazwischenliegende Teilbetankungen. | Das ungewichtete Mittel der Einzelwerte überbewertet kurze Strecken. Intervalle mit Strecke <= 0 (`NoDistance`) und Teilbetankungen vor dem ersten Vollvorgang gehen nicht ein. |
| Kosten pro 100 km (Frage 3) | Σ Intervallkosten / Σ Intervallstrecken × 100, 2 Nachkommastellen; Intervallkosten = `TotalAmount` des Vollvorgangs + `TotalAmount` der dazwischenliegenden Teilbetankungen. Berechnungsbereich identisch mit dem des Durchschnittsverbrauchs („Summe Kosten / Summe Strecke im Berechnungsbereich“). | Kosten und Strecke beziehen sich auf dieselben Intervalle; Teilbetankungen tragen Kosten wie Menge bei (Vorgabe Schritt 14). Die Strecke zwischen erstem und letztem Vollvorgang würde bei `NoDistance`-Intervallen abweichen. |
| Hybrid (Frage 4) | `TankbookStatistics` enthält je vorhandener Art einen eigenen Block `TankbookKindStatistics` (Reihenfolge `Fuel`, `Electricity`); Liter und kWh werden nie gemischt, auch Kosten/100 km, Monatskosten und Diagramme sind je Art getrennt. | ADR 0009 führt je Art eine eigene Kette; eine Summe über Arten wäre fachlich sinnlos. Nicht-Hybrid-Fahrzeuge haben nur einen Block. |
| Monatskosten (Frage 5) | Σ `TotalAmount` aller Vorgänge der Art (inkl. Teilbetankungen, auch vor dem ersten Vollvorgang) je Kalendermonat nach `OccurredAt` (lokale Wanduhrzeit, `DateTime` ohne Zeitzone, keine Umrechnung). Die Monatsreihe läuft lückenlos vom Monat des ersten Vorgangs der Art bis zum späteren aus letztem Vorgangsmonat und aktuellem Monat (Referenzdatum aus `TimeProvider`); Monate ohne Vorgang erscheinen mit 0,00 € (`EntryCount` 0). Anzeige neuester Monat zuerst. | Kosten sind Geldflüsse, unabhängig von der Verbrauchskette. Eine lückenlose Reihe macht Balkendiagramm und Liste ehrlich; ausgeblendete Monate würden den Verlauf verfälschen. |
| Strecke bei Teilbetankungen (Frage 11) | Bestätigt: Strecke nur zwischen Vollvorgängen derselben Art; Teilbetankungen tragen nur Menge und Kosten bei. | Entspricht der bestehenden Kette in `ConsumptionCalculator`. |
| Diagrammart und Zeitraum (Frage 6) | Verbrauchsverlauf: Liniendiagramm mit einem Punkt je berechnetem Vollvorgang. Kostenverlauf: Balkendiagramm der Monatskosten. Beide zeigen die letzten 12 Datenpunkte (`TankbookStatisticsLimits.ChartMaxPoints`); Kennzahlen und Monatsliste enthalten alle Daten, kein Zeitraumfilter. | Verbrauch ist je Vorgang sinnvoll, Kosten je Monat (nutzen die Monatsaggregation). 12 Punkte bleiben auf Telefonbreite lesbar; die Begrenzung betrifft nur die Zeichnung. |
| Diagramm-Technik, ADR 0010 | Eigene Zeichnung mit `GraphicsView`/`IDrawable` (`Microsoft.Maui.Graphics`, Teil von `Microsoft.Maui.Controls`), keine neue NuGet-Abhängigkeit. Begründung im neuen `docs/adr/0010-tankbook-statistics.md`: iOS läuft mit `MtouchInterpreter=all` und Trimming (`TrimmerRootAssembly`); Drittbibliotheken für Diagramme (z. B. SkiaSharp-basierte Pakete, Reflection/Source-Generatoren) sind dort ein Build- und Laufzeitrisiko, und das 60-Minuten-Limit des CI-Builds ist bereits knapp; Lizenzbedingungen von Diagrammpaketen (teils kommerziell/Community-Lizenz) müssten geprüft und gepflegt werden; zwei einfache Diagrammarten rechtfertigen keine Abhängigkeit. | Vorgabe des Auftraggebers; reduziert Risiko und Wartung. |
| Zeichenlogik testbar halten | Skalierung/Layout (Wertebereich, Koordinaten, Beschriftungsstellen) in der reinen Klasse `ChartGeometry`; `LineChartDrawable`/`BarChartDrawable` zeichnen nur anhand ihres Ergebnisses. Farben über `ChartPalette` (hell/dunkel, abgeleitet aus `DesignSystem.xaml`-Werten, Muster `MapPalette`). Drawables und `ChartGraphicsView` liegen in `Views/Charts/` und werden wie `TankradarContentPage` aus der Coverage ausgenommen. | Zeichnen ist nicht unit-testbar, die Geometrie schon; die Coverage-Schwelle von 70 % bleibt haltbar. |
| Barrierefreiheit der Diagramme | Jedes Diagramm hat eine sichtbare Textzusammenfassung (`…ChartSummary`, z. B. „Verlauf, 3 Werte, zuletzt 6,00 L/100 km, niedrigster 5,80, höchster 6,20“); das `GraphicsView` erhält sie als `SemanticProperties.Description`. Die Monatsliste ist die Tabellenalternative zum Kostendiagramm. | Grafik allein ist für Screenreader nicht erfassbar; der Entwurf enthielt dazu nichts. |
| Erweiterung statt Duplikat | `TankbookEntryOverview` erhält die optionalen Werte `IntervalQuantity` und `IntervalCost` (nur bei `Calculated` belegt); `ConsumptionCalculator` füllt sie aus derselben Kette (zusätzlich zur Teilbetankungsmenge wird die Teilbetankungssumme der Kosten geführt). Das neue statische `TankbookStatisticsCalculator` (Transaction Script) aggregiert diese Werte; `TankbookEntryOrder.Ascending` ordnet. Es gibt keine zweite Verbrauchsrechnung. | Eine einzige Wahrheit für Strecke/Verbrauch; additive Änderung (Konstruktorparameter mit Standardwert `null`). |
| Dienst | `ITankbookStatisticsService`/`TankbookStatisticsService` (Service Layer, Singleton) lädt das Fahrzeug über `IVehicleService.GetVehicleAsync` und Vorgänge über `ITankbookEntryService.GetEntriesAsync`, ruft den Calculator mit dem Referenzdatum aus `TimeProvider.GetLocalNow()`. Ergebnis-Typ `TankbookStatisticsLoad` mit `TankbookStatisticsStatus` (`Ok`, `VehicleNotFound`) statt Ausnahme; Speicherfehler werden wie bei `GetEntriesAsync` als Ausnahme durchgereicht und im ViewModel abgefangen. | Muster wie `ITankbookEntryService`; Wiederverwendung statt eigener Datenbankabfrage. Doppeltes lokales Lesen (Liste und Auswertung) ist bei SQLite-Mengen dieser Art unkritisch. |
| UI-Platzierung | Bereich innerhalb der bestehenden `TankbookEntriesPage` (keine neue Route): zwischen Formular-`Border` und Überschrift „Vorgänge, neueste zuerst“. Wiederverwendet werden `TankbookCard`, das Überschriftenmuster mit `SemanticProperties.HeadingLevel` und das Hinweis-Labelmuster der Seite. Neues Kind-ViewModel `TankbookStatisticsViewModel` als Eigenschaft `Statistics` von `TankbookEntriesViewModel` (wie `Form`). Keine neue Auswahlinteraktion (das Fahrzeug bestimmt die Seite). | Fahrzeugbezug und Neuladen (`LoadAsync` nach Öffnen, `Form.Saved`, Löschen, Korrigieren) bestehen bereits; Aktualisierung ohne neues Ereignis. |
| Designentwurf (Frage 7) | Der Entwurf `tankbuch_verbrauchsauswertung` ist bekannt (`inventory/design.md`). Übernommen: Kacheln Ø Verbrauch, Kosten/100 km, Monatskosten, Verlaufsdiagramm mit Wert am Datenpunkt, Design-System-Farben, Dark Mode. Nicht umgesetzt (nicht gefordert bzw. keine Datengrundlage): Vormonats-Trendzeilen, Plakette „Effizient“, Zielmarke „Ziel: 5,50 L“, Fahrzeug-Dropdown. Abweichungen stehen in ADR 0010. | Im Datenmodell gibt es keinen Zielwert; Zusätze würden spekulative Regeln erzeugen. |
| Texte | Neue Klasse `TankbookStatisticsTexts` in `Resources/Texts/` (deutsch, `German`-Kultur, Muster `TankbookEntryTexts`). | Konvention des Projekts. |
| Wortlaut Anwenderhilfe (Frage 8) | Rundungsausnahme wörtlich mit den Werten aus ADR 0009: Gesamtbetrag 2, Preis 3, Menge 2 Nachkommastellen, Toleranz 0,02 €; als stimmig gilt auch ein Wert, der genau dem gerundeten Quotienten der beiden anderen entspricht. | Anwender sollen die Prüfregel nachvollziehen können. |
| Wortlaut Teilbetankungs-Hinweis (Frage 9) | Neuer Text `TankbookEntryTexts.PartialHint` im Formular, sichtbar solange „Teilbetankung“ aktiv ist: „Der Verbrauch wird erst ab dem ersten vollen Vorgang berechnet. Die Menge einer Teilbetankung fließt in den nächsten vollen Vorgang ein; Teilbetankungen vor dem ersten vollen Vorgang zählen nicht für den Verbrauch, ihre Kosten erscheinen aber in den Monatskosten.“ | Präzisiert die bisherige Aussage „zählen nicht“; dauerhaft sichtbar statt erst nach einem Fehler. |
| Tests aus Schritt 13 (Frage 10) | Verbindlicher Teil dieses Schritts. | Kleine Testlücken, geringer Aufwand, Auslieferungs-Gate „grüne Tests“. |

## Programmabläufe

### Auswertung berechnen (Lesen)

1. `TankbookStatisticsService.GetStatisticsAsync(vehicleId, ct)` lädt `IVehicleService.GetVehicleAsync`; `null` -> Ergebnis `VehicleNotFound`.
2. Lädt `ITankbookEntryService.GetEntriesAsync(vehicleId, ct)` (`TankbookEntryOverview`, neueste zuerst, mit `IntervalQuantity`/`IntervalCost`).
3. Ruft `TankbookStatisticsCalculator.Calculate(vehicle, overviews, today)` mit `today = TimeProvider.GetLocalNow().DateTime`.
4. Der Calculator sortiert mit `TankbookEntryOrder.Ascending` (auf `Entry`), gruppiert nach `TankbookEntryKind` und bildet je vorhandener Art (Reihenfolge `Fuel`, `Electricity`) einen `TankbookKindStatistics`:
   - Berechnete Intervalle = Überblicke mit `ConsumptionState == Calculated`; Summen Σ Strecke, Σ Menge, Σ Kosten.
   - Mit mindestens einem Intervall: `AverageConsumption` = Σ Menge / Σ Strecke × 100, `CostPer100Km` = Σ Kosten / Σ Strecke × 100, Quelle `Calculated`.
   - Ohne Intervall: ist die Art die Startverbrauchs-Art und `StartConsumption` gesetzt -> `AverageConsumption` = Startverbrauch, Quelle `StartConsumption`; sonst Quelle `None`. `CostPer100Km` bleibt `null`.
   - `TotalCost` = Σ `TotalAmount` aller Vorgänge der Art; `TotalDistanceKm` = Σ Strecke.
   - `Months`: lückenlose Monatsreihe (`OccurredAt.Year/Month`) vom ersten Vorgangsmonat bis max(letzter Vorgangsmonat, Monat von `today`) mit Summe `TotalAmount` und Zahl der Vorgänge, neuester Monat zuerst.
   - `ConsumptionTrend`: je `Calculated`-Vorgang ein `TrendPoint` (`OccurredAt`, Verbrauch), ältester zuerst.
5. Rückgabe `TankbookStatisticsLoad(Ok, TankbookStatistics)`.

### Auswertung anzeigen und aktualisieren

1. `TankbookEntriesViewModel.LoadAsync` (Öffnen, `Form.Saved`, Löschen, Korrigieren) lädt wie bisher Fahrzeug und Vorgänge und ruft danach `Statistics.LoadAsync(vehicleId)`.
2. `TankbookStatisticsViewModel.LoadAsync` ruft `ITankbookStatisticsService.GetStatisticsAsync` und baut je Art ein `TankbookKindStatisticsViewModel` (Texte, Hinweise, `ChartData`, Monatszeilen). Ausnahme -> `Notice = TankbookStatisticsTexts.LoadFailed` (Protokoll nur mit Ausnahmetyp, ohne Werte); die Vorgangsliste bleibt unberührt.
3. Fahrzeug ohne Vorgänge -> keine Art-Blöcke, `Notice = NoEntriesNotice`. Art mit Quelle `None` -> Hinweis „Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt“ (mit Verweis auf den optionalen Startverbrauch am Fahrzeug, sofern nicht gesetzt). Quelle `StartConsumption` -> Wert mit Kennzeichnung „Startverbrauch – noch keine zwei vollen Vorgänge“. Kosten/100 km ohne Wert -> „noch nicht berechenbar“.
4. `Show(vehicleId)` setzt auch `Statistics` zurück (kein Altzustand).
5. Bei Ladefehler Ansage über `IScreenReaderAnnouncer` wie bei `LoadFailed`; bei normaler Aktualisierung keine zusätzliche Ansage.

### Diagramm zeichnen

1. `ChartGraphicsView` (abgeleitet von `GraphicsView`, BindableProperty `Data` vom Typ `ChartData`) erzeugt bei Datenänderung den passenden `IDrawable` (`LineChartDrawable` oder `BarChartDrawable` nach `ChartData.Kind`) und ruft `Invalidate`.
2. Der Drawable lässt `ChartGeometry.Layout(ChartData, RectF)` Wertebereich (Min/Max mit Rand, Basis 0 bei Balken), Punkt-/Balkenrechtecke und Beschriftungsstellen berechnen und zeichnet Achsenlinie, Linie+Punkte bzw. Balken, Wert am Datenpunkt und Datums-/Monatsmarken an höchstens 4 Stellen; Farben aus `ChartPalette` nach `Application.Current.RequestedTheme`.
3. Ohne Daten blendet die Seite das Diagramm aus (`HasData`). Ein einzelner Punkt wird als Punkt gezeichnet.

### Teilbetankungs-Hinweis

1. `TankbookEntryFormViewModel.IsPartial` an -> `ShowPartialHint` true, Label mit `TankbookEntryTexts.PartialHint` unter dem Schalter sichtbar; aus -> verborgen.

### Löschen mit Fehlerergebnis (nur Test, kein neuer Ablauf)

1. `DeleteEntryAsync` liefert ein Ergebnis ungleich `Ok`/`EntryNotFound` -> `StatusMessage = TankbookEntryTexts.GetResultMessage(result, null)`, kein Neuladen. Wird durch einen neuen Test abgedeckt.

Beteiligte Klassen/Komponenten: `ConsumptionCalculator`, `TankbookEntryOverview`, `TankbookEntryOrder`, `TankbookStatisticsCalculator`, `TankbookStatisticsService`, `TankbookStatisticsViewModel`, `TankbookEntriesViewModel`, `TankbookEntriesPage`, `ChartGeometry`, `LineChartDrawable`, `BarChartDrawable`, `ChartGraphicsView`.

## Neue Klassen

Alle neuen Eigenschaften werden im Stil `get { return …; }` geschrieben (der csproj-xmldoc-Check lehnt Ausdruckskörper-Eigenschaften ab); alle öffentlichen Member erhalten XML-Dokumentation.

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TankbookStatistics` | Datenmodellklasse (`sealed record`, `Models/Tankbook/`) | Auswertung eines Fahrzeugs: `VehicleId`, `Kinds` (Liste `TankbookKindStatistics`), `HasEntries` |
| `TankbookKindStatistics` | Datenmodellklasse | Kennzahlen einer Art: `Kind`, `AverageConsumption` (`decimal?`), `AverageSource`, `CostPer100Km` (`decimal?`), `CalculatedIntervals`, `TotalDistanceKm`, `TotalCost`, `Months`, `ConsumptionTrend` |
| `MonthlyCostSummary` | Datenmodellklasse | `Year`, `Month`, `TotalCost`, `EntryCount` |
| `TrendPoint` | Datenmodellklasse | `OccurredAt`, `Value` |
| `TankbookAverageSource` | Enum | `None`, `Calculated`, `StartConsumption` |
| `TankbookStatisticsStatus` | Enum | `Ok`, `VehicleNotFound` |
| `TankbookStatisticsLoad` | Datenmodellklasse | Ergebnis-Typ: `Status`, `Statistics` (`TankbookStatistics?`) |
| `TankbookStatisticsLimits` | Konstantenklasse | `ChartMaxPoints` = 12 |
| `TankbookStatisticsCalculator` | statische Klasse (`Services/Tankbook/`) | Aggregation (siehe Programmablauf), nutzt `TankbookEntryOrder` |
| `ITankbookStatisticsService` | Interface | `GetStatisticsAsync(long vehicleId, CancellationToken)` -> `Task<TankbookStatisticsLoad>` |
| `TankbookStatisticsService` | Klasse (sealed, Singleton) | Umsetzung; Abhängigkeiten `IVehicleService`, `ITankbookEntryService`, `TimeProvider` |
| `TankbookStatisticsViewModel` | ViewModel (sealed, `BaseViewModel`) | Zustand des Bereichs: `Kinds`, `Notice`, `HasNotice`, `HasKinds`, `LoadAsync`, `Reset` |
| `TankbookKindStatisticsViewModel` | ViewModel | Texte und Diagrammdaten einer Art (`AutomationKey`, Überschrift, `AverageText`, `AverageSourceText`, `CostPer100KmText`, `TotalCostText`, `Notice`, `ConsumptionChart`, `ConsumptionChartSummary`, `CostChart`, `CostChartSummary`, `Months`) |
| `MonthlyCostRowViewModel` | Wertobjekt / ViewModel | Zeilentext „Oktober 2026: 74,36 €“ (leerer Monat „0,00 €“) |
| `TankbookStatisticsTexts` | Textklasse (`Resources/Texts/`) | Auswertungstexte, Formatierer, Barrierefreiheits-Texte |
| `ChartKind`, `ChartPoint`, `ChartData` | Enum / Datenmodellklassen (`Models/Tankbook/`) | Diagrammdaten: Art (Line/Bar), Punkte (Label, Wert), Einheit, `HasData` |
| `ChartGeometry` | statische Klasse (`Services/Charts/`) | reine Layoutberechnung für beide Diagrammarten |
| `ChartPalette` | statische Klasse (`Resources/`) | Diagrammfarben hell/dunkel |
| `LineChartDrawable`, `BarChartDrawable` | `IDrawable` (`Views/Charts/`) | Zeichnung mit `ICanvas` |
| `ChartGraphicsView` | Steuerelement (`GraphicsView`, `Views/Charts/`) | bindbare Eigenschaft `Data`, Drawable-Auswahl |
| `docs/adr/0010-tankbook-statistics.md` | ADR | Diagramme per MAUI Graphics, Kennzahl-Definitionen, Abweichungen vom Entwurf |

Reihenfolge-Abhängigkeiten: Enums und Modelle vor `TankbookStatisticsCalculator`; `ConsumptionCalculator`-Erweiterung vor dem Calculator; Calculator vor Dienst; Dienst vor ViewModel; `ChartData`/`ChartGeometry` vor Drawables und Seite.

## Änderungen an bestehenden Klassen

### `TankbookEntryOverview` (Datenmodellklasse)

- **Neue Eigenschaften:** `IntervalQuantity` (`decimal?`) — Menge im Intervall (Vollvorgang + Teilbetankungen); `IntervalCost` (`decimal?`) — Kosten im Intervall. Nur bei `Calculated` belegt. Als optionale Konstruktorparameter am Ende (Standard `null`), damit bestehende Aufrufe gültig bleiben.

### `ConsumptionCalculator` (statische Klasse)

- **Geänderte Methoden:** `Calculate` — führt neben `partialSums` eine Kostensumme je Art, befüllt `IntervalQuantity`/`IntervalCost` bei `Calculated`; Reset der Kostensumme wie bei der Mengensumme (auch bei `NoDistance`). Verbrauchswerte und Zustände bleiben unverändert; Klassenkommentar ergänzen.

### `TankbookEntryTexts` (Textklasse)

- **Neue Konstanten:** `PartialHint` (Wortlaut siehe Designentscheidungen).

### `TankbookEntryFormViewModel` (ViewModel)

- **Neue Eigenschaften:** `ShowPartialHint` (`bool`, abgeleitet von `IsPartial`, `PropertyChanged` beim Wechsel von `IsPartial`), `PartialHintText`.

### `TankbookEntriesViewModel` (ViewModel)

- **Neue Eigenschaften:** `Statistics` (`TankbookStatisticsViewModel`, per Konstruktor injiziert).
- **Geänderte Methoden:** `LoadAsync` — ruft nach erfolgreichem Laden von Fahrzeug und Vorgängen `Statistics.LoadAsync(vehicleId)`; `Show` — ruft `Statistics.Reset()`. Der Löschpfad bleibt unverändert (lädt über `LoadAsync` neu).

### `TankbookEntriesPage` (XAML-Seite)

- Neuer Bereich „Auswertung“ (`Border` mit Stil `TankbookCard`, `BindingContext="{Binding Statistics}"`) mit Überschrift (`HeadingLevel`), Hinweis, `BindableLayout` über `Kinds` mit Kacheln (Ø Verbrauch + Quelle, Kosten/100 km, Gesamtkosten), je zwei `ChartGraphicsView` mit Zusammenfassungs-Label und Monatsliste. `PartialHint` unter dem Teilbetankungs-Schalter im Formular. Die Seite bleibt `TankradarContentPage`; Code-Behind unverändert.
- **AutomationIds** (Schema `Bereich.Element[.Unterelement]`; `{Key}` = `Fuel`/`Electricity` über `AutomationKey`-Binding wie bei den Auswahl-Chips): `TankbookEntries.Statistics.Heading`, `TankbookEntries.Statistics.Notice`, `TankbookEntries.Statistics.{Key}.Heading`, `….{Key}.Average`, `….{Key}.AverageSource`, `….{Key}.CostPer100Km`, `….{Key}.TotalCost`, `….{Key}.Notice`, `….{Key}.ConsumptionChart`, `….{Key}.ConsumptionChartSummary`, `….{Key}.CostChart`, `….{Key}.CostChartSummary`, `….{Key}.MonthsHeading`, `….{Key}.Month` (je Zeile identisch, von oben nach unten per `TankbookE2EDriver.FindAllOrdered`), sowie `TankbookEntries.Form.PartialHint`.
- Barrierefreiheit: Überschriften mit `SemanticProperties.HeadingLevel`; Diagramme mit `SemanticProperties.Description`; keine neue Schaltfläche (andernfalls: sichtbarer Text als Name, Kontext in `SemanticProperties.Hint`, WCAG 2.5.3). Farben aus dem Design-System mit ausreichendem Kontrast in hell und dunkel.

### `MauiProgram` (Startklasse)

- Registrierung `ITankbookStatisticsService`/`TankbookStatisticsService` als Singleton; `TankbookStatisticsViewModel` transient.

### `coverlet.runsettings` (Konfiguration)

- `**/Views/Charts/**` in die Coverage-Ausschlüsse (reine Zeichnung); die Berechnung steht in `ChartGeometry` und bleibt gemessen.

### Dokumentation und Protokoll

- `docs/help/Tankbuch/beschreibung.md`: neuer Abschnitt „Auswertung“ (Kennzahlen, Schwelle, Startverbrauch, Hybrid, Monatskosten, Diagramme, Barrierefreiheit); Rundungsausnahme in „Menge, Preis und Gesamtbetrag“; präziser Teilbetankungs-Hinweis in „Verbrauchsberechnung“. `ablauf-anwender.md` (Auswertung ansehen), `ablauf-technisch.md` (Dienst, Calculator, Diagramm, Tests), `index.md` bei Bedarf; `docs/help/index.md` Beschreibungszeile um Auswertung ergänzen.
- `docs/adr/0010-tankbook-statistics.md` (Status „Angenommen“): Kontext, Entscheidung (eigene Zeichnung per MAUI Graphics), iOS-Interpreter/Trimming (`MtouchInterpreter=all`, `TrimmerRootAssembly`, 60-Minuten-Limit), Lizenz, kurze Alternativen, Kennzahl-Definitionen (Schwelle, Gewichtung, Hybrid, Monatsreihe), Abweichungen vom Designentwurf, Folgen.
- `changes.log`: neuer oberster Eintrag `## <Datum> — Verbrauchs- und Kostenauswertung (Tankradar Schritt 14, Version 0.1.18)` mit Stichpunkten (Auswertung, Diagramme, Rundungsausnahme in der Hilfe, Teilbetankungs-Hinweis, nachgeholte Tests) sowie „Tests:“- und „Dokumentation:“-Zeilen.

## Datenbankmigrationen

Keine. Die Auswertung liest `TankbookEntries` und `Vehicles.StartConsumption` (seit Migration `AddVehicles` vorhanden); das Modell-Snapshot bleibt unverändert. Ein Integrationstest sichert zu, dass bestehende Daten unverändert ausgewertet werden. Sollte dennoch eine Schemaänderung nötig werden, wäre sie additiv mit Erhalt der Daten (Skill `entityframework-database`).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Eingaben | Keine neuen Eingaben; bestehende Validierung unverändert. | – |
| Berechnungsdaten | Intervalle mit Strecke <= 0 und Teilbetankungen vor dem ersten Vollvorgang gehen nicht in Durchschnitt/Kosten pro 100 km ein; Division nur bei Σ Strecke > 0. | Kein Wert, Hinweistext statt Zahl |
| `StartConsumption` | Nutzung nur wenn gesetzt (> 0, < 100 laut `VehicleValidator`). | Ohne Wert: Hinweis |

## Konfigurationsänderungen

Keine Anwendungseinstellungen (kein `appsettings`-Eintrag; der Startverbrauch bleibt Fahrzeugeigenschaft; die Anzeigeobergrenze ist die Konstante `TankbookStatisticsLimits.ChartMaxPoints`). Einzig der Coverage-Ausschluss in `coverlet.runsettings` (siehe oben).

## Seiteneffekte und Risiken

- **`TankbookEntryOverview`/`ConsumptionCalculator`:** Zwei zusätzliche optionale Werte; Records vergleichen strukturell, bestehende Tests mit `Assert.Equal` auf ganze Überblicke sind zu prüfen. Verbrauchswerte bleiben gleich.
- **`TankbookEntriesViewModel`-Konstruktor:** neuer Parameter; `TankbookEntriesViewModelTestBase` und alle DI-Aufrufer anpassen.
- **Seitenlänge/UIA:** Der neue Bereich verlängert die Seite; E2E-Tests der Liste (`FindAllOrdered`) müssen Elemente weiter per AutomationId finden. Der Bereich darf den Leerhinweis `TankbookEntries.Empty` nicht verdecken.
- **Doppeltes Lesen:** Liste und Auswertung lesen je einmal; bei Fehler der Auswertung bleibt die Liste nutzbar.
- **GraphicsView unter FlaUI/UIA:** Der Inhalt eines `GraphicsView` ist nicht prüfbar; E2E prüft die Zusammenfassungs-Labels und die Existenz der Diagramm-AutomationIds.
- **iOS:** keine neue Abhängigkeit; `GraphicsView` gehört zu MAUI. Zeichencode ohne Reflection, daher trimming- und interpretersicher.
- **Zeit:** Die Monatsreihe bis zum aktuellen Monat hängt von `TimeProvider`; Tests nutzen `ManualTimeProvider`. Monatsgrenzen gelten für lokale Wanduhrzeit ohne Umrechnung (`Kind` Unspecified); kein `ToLocalTime`/`ToUniversalTime`.
- **Dezimalrundung:** Summen exakt, Rundung nur am Ende (2 Stellen, `AwayFromZero`); Kosten/100 km sind keine Summe gerundeter Einzelwerte.
- **Coverage 70 %:** durch Ausschluss der reinen Zeichenklassen und Tests der Berechnung gesichert.

## Umsetzungsreihenfolge

1. **Enums und Modelle anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: `TankbookAverageSource`, `TankbookStatisticsStatus`, `MonthlyCostSummary`, `TrendPoint`, `TankbookKindStatistics`, `TankbookStatistics`, `TankbookStatisticsLoad`, `TankbookStatisticsLimits`, `ChartKind`, `ChartPoint`, `ChartData` mit XML-Doku und `get { return …; }`-Eigenschaften.
2. **`TankbookEntryOverview` und `ConsumptionCalculator` erweitern**
   - Voraussetzungen: vorhandene Klassen
   - Beschreibung: `IntervalQuantity`/`IntervalCost` ergänzen und befüllen; bestehende `ConsumptionCalculator`-Tests laufen lassen.
3. **`TankbookStatisticsCalculator` anlegen**
   - Voraussetzungen: Schritte 1, 2
   - Beschreibung: Aggregation je Art (Intervalle, Durchschnitt, Kosten pro 100 km, Startverbrauch, Monatsreihe mit Referenzdatum, Verlaufspunkte).
4. **Dienst und DI**
   - Voraussetzungen: Schritt 3; `IVehicleService`, `ITankbookEntryService`, `TimeProvider` (vorhanden)
   - Beschreibung: `ITankbookStatisticsService`, `TankbookStatisticsService`, Registrierung in `MauiProgram`.
5. **Texte**
   - Voraussetzungen: Schritt 1
   - Beschreibung: `TankbookStatisticsTexts` (Kennzahlen, Quelle, Hinweise, Monatszeile, Diagrammzusammenfassung, `LoadFailed`, `NoEntriesNotice`); `TankbookEntryTexts.PartialHint`.
6. **Diagrammgeometrie und Palette**
   - Voraussetzungen: `ChartData` (Schritt 1)
   - Beschreibung: `ChartGeometry`, `ChartPalette`.
7. **Drawables und `ChartGraphicsView`**
   - Voraussetzungen: Schritt 6; `Microsoft.Maui.Graphics` ist über `Microsoft.Maui.Controls` vorhanden (keine neue Abhängigkeit)
   - Beschreibung: `LineChartDrawable`, `BarChartDrawable`, `ChartGraphicsView`; Coverage-Ausschluss `**/Views/Charts/**` in `coverlet.runsettings`.
8. **ViewModels**
   - Voraussetzungen: Schritte 4, 5, 6
   - Beschreibung: `MonthlyCostRowViewModel`, `TankbookKindStatisticsViewModel`, `TankbookStatisticsViewModel`; Einbindung in `TankbookEntriesViewModel` (`Statistics`, `LoadAsync`, `Show`); `TankbookEntryFormViewModel.ShowPartialHint`; DI-Registrierung.
9. **Seite**
   - Voraussetzungen: Schritte 7, 8
   - Beschreibung: Bereich „Auswertung“ in `TankbookEntriesPage.xaml` mit AutomationIds und Semantik; `PartialHint` im Formular.
10. **Unit- und Integrationstests (Berechnung, Dienst, ViewModels, Texte, Geometrie)**
    - Voraussetzungen: Schritte 1–8
    - Beschreibung: siehe Abschnitt Tests; Support-Anpassungen (`TankbookEntriesViewModelTestBase`, `TankbookEntryFactory.CreateAt`, Stub-Service für Löschergebnis).
11. **Nachgeholte Tests aus Schritt 13**
    - Voraussetzungen: `ReceiptPhotoStore.DetectExtension` (vorhanden), `TankbookEntriesViewModelTestBase`
    - Beschreibung: `DetectExtension` mit `mif1`/`msf1`/unbekannter Marke/zu kurzer Datei; Löschfehler-Zweig.
12. **E2E-Tests**
    - Voraussetzungen: Schritt 9; `TankbookE2EDriver`, `SetFieldValue`, `TANKATLAS_TEST_DATA_PATH`-Isolierung (vorhanden)
    - Beschreibung: Hilfsmethoden in `TankbookTestDatabase` (Art, Betrag, Zeitpunkte wählbar) und im Treiber (Auswertung lesen); neue Szenarien.
13. **Dokumentation, ADR 0010, `changes.log`**
    - Voraussetzungen: Schritte 1–12 (Inhalte stehen fest)
    - Beschreibung: Hilfe `docs/help/Tankbuch/*`, `docs/help/index.md`, ADR 0010, `changes.log` Version 0.1.18.
14. **Gesamtlauf**
    - Voraussetzungen: alle vorigen
    - Beschreibung: Build (0 Warnungen), `dotnet format --verify-no-changes`, Unit, Integration, E2E komplett grün (Auslieferungs-Gate); csproj-xmldoc-Check (Eigenschaftsform `get { return …; }`).

## Tests

### Neue Tests

Testklassen bleiben klein und je Thema getrennt (Skill `test-class-structure`).

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Calculate_TwoFull_SetsIntervalQuantityAndCost`, `Calculate_PartialBetween_AddsQuantityAndCostToInterval`, `Calculate_PartialBeforeFirstFull_NoIntervalValues`, `Calculate_NoDistance_HasNoIntervalValues`, `Calculate_Hybrid_IntervalsPerKind` | `ConsumptionCalculatorTests_Interval` | neue Intervallwerte; Verbrauchswerte unverändert |
| `Average_TwoFullEntries_IsWeightedConsumption`, `Average_UnequalDistances_IsWeightedNotMeanOfValues`, `Average_NoDistanceInterval_IsExcluded`, `Average_RoundsAwayFromZeroToTwoDecimals` | `TankbookStatisticsCalculatorTests_Average` | Gewichtung, Ausschluss, Rundung |
| `Threshold_NoEntries_HasNoKinds`, `Threshold_OneFullEntry_SourceNone`, `Threshold_OneFullEntryWithStart_UsesStartConsumption`, `Threshold_TwoFullEntries_UsesCalculatedEvenWithStart`, `Threshold_ElectricVehicle_StartConsumptionForElectricity`, `Threshold_Hybrid_StartConsumptionOnlyForFuel`, `Threshold_OnlyPartialEntries_SourceNone` | `TankbookStatisticsCalculatorTests_Threshold` | Schwelle, Startverbrauch-Fallback, Art-Zuordnung |
| `Cost_FullEntries_IsSumCostOverSumDistance`, `Cost_PartialsIncludeCost`, `Cost_PartialBeforeFirstFull_NotInCostPer100Km`, `Cost_DistanceZeroOrNegative_NoValue`, `Cost_StartConsumption_HasNoCostPer100Km` | `TankbookStatisticsCalculatorTests_Cost` | Kosten pro 100 km, Teilbetankungskosten, Strecke <= 0 |
| `Months_GroupsByCalendarMonthOfOccurredAt`, `Months_Boundary_Dec31_2359_And_Jan01_0000`, `Months_Boundary_EndOfFebruaryLeapYear`, `Months_EmptyMonthsBetween_AreZero`, `Months_RunsUntilCurrentMonth`, `Months_IncludePartialAndPartialBeforeFirstFull`, `Months_UnspecifiedKind_NoTimeZoneShift`, `Months_NewestFirst`, `Months_Hybrid_SeparatePerKind` | `TankbookStatisticsCalculatorTests_Months` | Monatsaggregation, Monatsgrenzen, leere Monate, Wanduhrzeit |
| `Trend_OnePointPerCalculatedEntry_OldestFirst`, `Trend_NoCalculated_IsEmpty` | `TankbookStatisticsCalculatorTests_Trend` | Verlaufspunkte |
| `Statistics_VehicleWithoutEntries`, `Statistics_UnknownVehicle_ReturnsVehicleNotFound`, `Statistics_UsesEntryServiceOrderAndTimeProvider`, `Statistics_AfterEditAndDelete_ReflectsChange` | `TankbookStatisticsServiceTests_Read` | Dienst gegen Test-Datenbank (`TankbookEntryFixture`, `ManualTimeProvider`) |
| `Load_ShowsAverageCostAndSource`, `Load_Hybrid_ShowsTwoBlocks`, `Load_NoEntries_ShowsNoEntriesNotice`, `Load_TooFewEntries_ShowsNotice`, `Load_StartConsumption_ShowsSourceLabel`, `Load_Failure_ShowsMessageWithoutValues`, `Reset_ClearsState`, `ChartSummary_DescribesValues` | `TankbookStatisticsViewModelTests_Load` | Darstellung, Hinweise, Barrierefreiheits-Text |
| `Entries_Save_RefreshesStatistics`, `Entries_Edit_RefreshesStatistics`, `Entries_Delete_RefreshesStatistics`, `Show_ResetsStatistics` | `TankbookEntriesViewModelTests_Statistics` | Aktualisierung nach Erfassen/Korrigieren/Löschen |
| `Partial_On_ShowsHint`, `Partial_Off_HidesHint` | `TankbookEntryFormViewModelTests_PartialHint` | Hinweis im Formular |
| Textformate (Kennzahlen, Monat „Oktober 2026“, leerer Monat, Einheiten L/kWh, Zusammenfassung) | `TankbookStatisticsTextsTests_Format`, `TankbookStatisticsTextsTests_Messages` | Wortlaut/Formatierung in deutscher Kultur |
| `PartialHint_MentionsFirstFullEntryAndCosts` | `TankbookEntryTextsTests_Messages` (ergänzen) | Wortlaut |
| `Layout_ScalesValuesIntoBounds`, `Layout_SinglePoint`, `Layout_AllEqualValues_NoDivisionByZero`, `Layout_Bars_StartAtZero`, `Layout_LimitsToMaxPoints`, `Layout_LabelPositions_AtMostFour` | `ChartGeometryTests` | Skalierung und Randfälle |
| `DetectExtension_Mif1_ReturnsHeif`, `DetectExtension_Msf1_ReturnsHeif`, `DetectExtension_UnknownFtypBrand_ReturnsNull`, `DetectExtension_TooShortFile_ReturnsNull`, `DetectExtension_Heic_ReturnsHeic` | `ReceiptPhotoStoreTests_Detection` (neu) | Bildsignatur `mif1` u. a. |
| `Delete_ResultOtherThanOkOrNotFound_ShowsResultMessageAndKeepsEntries` (z. B. `InvalidQuantity` -> `SaveFailed`-Text, kein Neuladen) | `TankbookEntriesViewModelTests_DeleteResult` (neu) | Löschfehler-Zweig; Hilfsklasse `StubTankbookEntryService` (Dekorator mit einstellbarem Löschergebnis) in `Unit/Support/` |
| `Statistics_OnRealDatabaseFile_SurvivesRestartAndMatchesEntries`, `Statistics_ExistingData_NoMigrationNeeded_Unchanged` | `TankbookStatisticsServiceTests_RealDatabase` (Integration) | echte SQLite-Datei, kein Schemawechsel, Daten erhalten |
| `TankbookEntryFactory.CreateAt(id, occurredAt, odometerKm, quantity, totalAmount, isPartial, kind)` | `Unit/Support/TankbookEntryFactory` (ergänzen) | Datensätze mit frei wählbarem Zeitpunkt/Betrag |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TankbookEntriesViewModelTestBase` und alle davon abgeleiteten Klassen | neuer Konstruktorparameter `TankbookStatisticsViewModel` (Fixture liefert Dienst/ViewModel) |
| `ConsumptionCalculatorTests_*`, `TankbookEntryServiceTests_Read` | Prüfung, dass Vergleiche ganzer `TankbookEntryOverview`-Objekte durch die neuen Eigenschaften nicht brechen; Anpassung falls nötig |
| `ReceiptPhotoStoreTests_Files` | ggf. Aufteilung wegen Klassengröße |
| `TankbookEntryTextsTests_Messages` | ergänzte Konstante |

### E2E-Tests (primärer Funktionsnachweis)

Neue Klassen auf Basis `TankbookE2ETestBase`; Eingaben mit `SetFieldValue`, Datenverzeichnis isoliert über `TANKATLAS_TEST_DATA_PATH`; `TankbookE2EDriver` erhält Lesehilfen für die Auswertung. Massendaten über `TankbookTestDatabase` (Erweiterung um Art, Gesamtbetrag und explizite Zeitpunkte); der Happy Path wird zusätzlich über das Formular erzeugt.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Zwei Vollvorgänge per Formular erfassen -> Ø Verbrauch, Kosten/100 km, Monatskosten, Diagramm-Zusammenfassungen sichtbar mit erwarteten Werten | `TankbookStatisticsE2ETests_Flow` (`TwoFullEntries_ShowAverageCostAndMonth`) | Anzeige der Kennzahlen nach Erfassen | Zeigt, dass Dienst, ViewModel, Seite und Diagramm-Steuerelement zusammenarbeiten |
| Pflicht | Vorgang korrigieren und löschen -> Werte ändern sich, nach dem Löschen Rückfall auf Hinweis | `TankbookStatisticsE2ETests_Update` (`EditAndDeleteEntry_UpdatesStatistics`) | Aktualisierung nach Korrektur/Löschung | Aktualisierung hängt am UI-Neuladen |
| Pflicht | Fahrzeug ohne Vorgänge: Hinweis ohne Werte; ein Vollvorgang ohne Startverbrauch: Hinweis „mindestens zwei volle Vorgänge“; mit Startverbrauch: Wert mit Kennzeichnung „Startverbrauch“ | `TankbookStatisticsE2ETests_Fallback` | zu wenige Vollvorgänge / Startverbrauch | Anwendersichtbare Hinweistexte |
| Pflicht | Vorgänge über eine Monatsgrenze (31.12. 23:59 / 01.01. 00:00, ein leerer Monat dazwischen) -> Monatszeilen mit richtigen Summen, leerer Monat 0,00 € | `TankbookStatisticsE2ETests_Months` | Monatskosten je Kalendermonat | Zusammenspiel Persistenz, lokale Zeit und Anzeige |
| Pflicht | Teilbetankung zwischen Vollvorgängen: Kosten und Menge im Durchschnitt/Kosten pro 100 km berücksichtigt | `TankbookStatisticsE2ETests_Partial` | Teilbetankungen in Kosten | Fachlich zentral für die Kostenbasis |
| Hoch | Hybridfahrzeug mit Kraftstoff- und Stromvorgängen -> zwei getrennte Blöcke mit L/100 km bzw. kWh/100 km | `TankbookStatisticsE2ETests_Hybrid` | getrennte Ketten | Sichtbare Trennung |
| Hoch | Teilbetankungs-Schalter im Formular zeigt/verbirgt den präzisierten Hinweis | `TankbookEntriesE2ETests_PartialHint` | Hinweistext Teilbetankung | UI-Zustand des Formulars |
| Hoch | Auswertung nach App-Neustart unverändert; offline vorhanden | `TankbookStatisticsE2ETests_Persistence`; Ergänzung in `OfflineE2ETests_Tankbook` | Berechnung beim Lesen, offline | Persistenz und Offline-Fähigkeit |

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `OfflineE2ETests_Tankbook` | ergänzende Prüfung, dass die Auswertung ohne Netz erscheint |
| `TankbookE2EDriver`, `TankbookTestDatabase` | neue Hilfsmethoden (Auswertung lesen, Seed mit Art/Betrag/Zeitpunkt) |
| übrige `TankbookEntriesE2ETests_*` | nur Regression (Seitenlänge, Sichtbarkeit), keine inhaltliche Änderung erwartet |

## Offene Punkte

Keine.
