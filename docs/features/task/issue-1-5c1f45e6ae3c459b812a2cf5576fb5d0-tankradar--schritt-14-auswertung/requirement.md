# Anforderungsübersetzung: Tankradar – Schritt 14 Verbrauchs- und Kostenauswertung

**Quelle:** `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Abschnitt „Schritt 14“ inkl. „Zusatz aus der Abnahme von Schritt 13“
**Kontext:** `docs/adr/0009-tankbook-entries.md`, `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/requirement.md`
**Abhängigkeit:** Schritt 13

---

## Fachliche Zusammenfassung

Je Fahrzeug wird im Tankbuch eine Auswertung der erfassten Tank- und Ladevorgänge bereitgestellt. Sie zeigt den Durchschnittsverbrauch (l/100 km bzw. kWh/100 km), die Kosten pro 100 km, die Monatskosten und Verlaufsdiagramme von Verbrauch und Kosten über die Zeit. Die Berechnung erfolgt beim Lesen auf Basis des `ConsumptionCalculator` und der `TankbookEntryOrder` (ADR 0009) und ist vollständig offline verfügbar. Die Auswertung aktualisiert sich nach jedem Erfassen, Korrigieren oder Löschen eines Vorgangs. Liegen zu wenige Vollvorgänge vor, wird ein optional hinterlegter Startverbrauch (`StartConsumption`) herangezogen oder ein verständlicher Hinweis angezeigt. Zusätzlich werden drei Nacharbeiten aus der Abnahme von Schritt 13 umgesetzt: eine Anwenderhilfe zur Rundungsausnahme, ein präziserer Hinweis bei Teilbetankungen vor dem ersten Vollvorgang und optionale Tests für `DetectExtension` (Bildsignatur `mif1`) und den Löschfehler-Zweig in `TankbookEntriesViewModel`.

---

## Betroffene Klassen und Komponenten

**Datenmodell / Eigenschaften**
- `Vehicle`: Nutzung der bestehenden Eigenschaft `StartConsumption` (optional) – keine neue Eigenschaft angenommen (Annahme, siehe unten).
- `TankbookEntry` (bzw. `TankbookEntryEntity`): nur lesend, keine Schemaänderung vorgesehen.
- Neue Auswertungsmodelle (Wertobjekte/DTOs) für Kennzahlen, Monatswerte und Verlaufspunkte, z. B. `TankbookStatistics`, `MonthlyCostSummary`, `TrendPoint` (Namen Vorschlag, nicht vorgegeben).

**Logik / Services**
- `ConsumptionCalculator`: Erweiterung um Kennzahlen je Art (Kraftstoff bzw. Strom) und um Kosten pro 100 km; Wiederverwendung der bestehenden Kette aus ADR 0009.
- `TankbookEntryOrder`: Wiederverwendung für die zeitliche Sortierung.
- Neuer Auswertungsdienst (z. B. `ITankbookStatisticsService` / `TankbookStatisticsService`), der Vorgänge je Fahrzeug lädt und die Kennzahlen berechnet. Muster analog zu `ITankbookEntryService` (Ergebnis-Typen statt Ausnahmen).

**Interfaces**
- `ITankbookStatisticsService` (neu).

**Enums**
- Keine neuen Enums angenommen. Art (Kraftstoff/Strom) wird aus ADR 0009 übernommen.

**UI-Komponenten / ViewModels**
- Neue Auswertungsansicht im Tankbuch, z. B. als Bereich auf der Seite „Tank- und Ladevorgänge“ (Shell-Route `tankbookentries`, Parameter `vehicleId`) oder als eigene Seite – Entscheidung offen (siehe unten).
- Neues ViewModel für die Auswertung oder Erweiterung von `TankbookEntriesViewModel`.
- Diagramm-Komponente für Verbrauchs- und Kostenverlauf (Bibliothek noch nicht festgelegt).
- Erweiterung der Hilfeseite „Tankbuch“ (`docs/help/`) um die Rundungsausnahme (Zusatz aus Schritt 13).
- Anpassung des Hinweistextes bei Teilbetankungen im Formular von Schritt 13.
- Barrierefreiheit: Für neue Schaltflächen gilt WCAG 2.5.3 (sichtbarer Text als Name, Kontext in `SemanticProperties.Hint`).

**Tests**
- Unit-Tests `ConsumptionCalculator`/Auswertungsdienst mit Beispieldaten: Monatsgrenzen, Teilbetankungen, Fahrzeug ohne Einträge, nur ein Vollvorgang, Startverbrauch-Fallback, Strecke ≤ 0, Elektro (kWh), Hybrid (getrennte Ketten).
- Unit-Tests für die Monatsaggregation (Zeitzone/Ortszeit wie in ADR 0009).
- Tests für `DetectExtension` (`mif1`) und den Löschfehler-Zweig von `TankbookEntriesViewModel` (Zusatz aus Schritt 13, optional).
- FlaUI-Szenario: Anzeige der Kennzahlen nach Erfassen von Vorgängen.
- Test der Aktualisierung nach Korrektur und Löschung eines Vorgangs.

---

## Implementierungsansatz

- Die Auswertung wird beim Lesen berechnet, nicht gespeichert. Damit bleiben Korrekturen und Löschungen automatisch konsistent (wie in ADR 0009 für den Verbrauch festgelegt).
- Verbrauch: Je Art eine eigene Kette, keine Vermischung von Litern und kWh bei Hybridfahrzeugen. Teilbetankungen gehen mit ihrer Menge in den nächsten Vollvorgang ein. Teilbetankungen vor dem ersten Vollvorgang fließen nicht ein (ADR 0009).
- Kosten: Teilbetankungen fließen mit ihren Kosten in die Kostenwerte ein (Vorgabe Schritt 14).
- Monatskosten: Zuordnung nach Zeitpunkt des Vorgangs (`OccurredAt`, lokale Wanduhrzeit, ADR 0009).
- Verlauf: Datenpunkte je Vollvorgang (Verbrauch) bzw. je Monat oder je Vorgang (Kosten) – Granularität offen.
- Startverbrauch-Fallback: Bei zu wenigen Vollvorgängen wird `StartConsumption` herangezogen, sonst Hinweistext (Schwelle offen).
- Ereignisse: Keine neuen Events angenommen. Die Auswertung wird beim Öffnen der Seite berechnet bzw. nach Speichern, Korrigieren oder Löschen über die bestehende Ladeaktion neu geladen (Annahme).
- Abhängigkeiten: `ITankbookEntryService`, `ConsumptionCalculator`, `TankbookEntryOrder`, Shell-Navigation des Tankbuchs.
- Offline: keine Netzwerkabhängigkeit.
- Keine Schemamigration erwartet (nur Lesen). Falls ein Startverbrauch-Feld fehlt, wäre eine additive Migration nötig; Annahme: vorhanden (laut Requirement-Dekomposition, Schritt 12/13).

---

## Konfiguration

- Der Startverbrauch ist bereits pro Fahrzeug konfigurierbar (optionale Eigenschaft `StartConsumption`). Eine zusätzliche Anwendungseinstellung ist nicht vorgesehen.
- Auswahl des Fahrzeugs: Die Auswertung ist fahrzeugbezogen und wird über die Fahrzeugkarte im Tankbuch erreicht (wie in ADR 0009 für die Vorgangsseite festgelegt). Es entsteht keine neue Auswahlinteraktion.
- Zeitraumfilter (z. B. letzte 12 Monate) ist nicht gefordert; nicht umgesetzt, solange nicht vom Kunden verlangt.

---

## Offene Fragen

1. **Schwelle „zu wenige Vorgänge“:** Ab wie vielen Vollvorgängen wird der Durchschnittsverbrauch angezeigt? Genügt ein Intervall (zwei Vollvorgänge), oder wird der Startverbrauch bereits bei einem Vollvorgang genutzt?
2. **Verwendung des Startverbrauchs:** Wird der Startverbrauch als Ersatzwert in der Durchschnittsanzeige dargestellt oder nur als Anfangswert gekennzeichnet? Wie wird die Quelle (Startwert vs. Berechnung) angezeigt?
3. **Kosten pro 100 km:** Bezugsgröße ist angenommen die Summe der Gesamtbeträge im betrachteten Zeitraum geteilt durch die Strecke zwischen erstem und letztem Vollvorgang. Ist das gewünscht, oder soll die Kostenbasis anders gewählt werden (z. B. Kosten je Intervall zwischen zwei Vollvorgängen)?
4. **Hybridfahrzeuge:** Wie werden Verbrauch und Kosten dargestellt, wenn Kraftstoff und Strom gemischt vorliegen – getrennte Werte, oder nur Kraftstoff?
5. **Monatskosten:** Sollen Monatskosten Kraftstoff und Strom getrennt ausweisen? Soll ein Monat ohne Vorgänge angezeigt oder ausgeblendet werden?
6. **Verlaufsgrafiken:** Welche Diagrammart und welcher Zeitraum (alle Vorgänge oder begrenzt)? Welche Bibliothek ist für .NET MAUI vorgesehen? Der Designentwurf ist im Repository nicht auffindbar und muss als Referenz bereitgestellt werden.
7. **Designentwurf:** Liegt der Entwurf für die Statistik-Ansicht vor (Layout, Pflichtbestandteile)?
8. **Wortlaut der Anwenderhilfe:** Soll die Rundungsausnahme mit den Werten aus ADR 0009 („Gesamtbetrag auf 2, Preis auf 3, Menge auf 2 Nachkommastellen“, Toleranz 0,02 €) wörtlich in der Hilfe stehen?
9. **Wortlaut des Teilbetankungs-Hinweises:** Genaue Formulierung für den präzisierten Hinweis vor dem ersten Vollvorgang (Vorschlag: der Verbrauch wird erst ab dem ersten Vollvorgang berechnet; die Menge der Teilbetankung fließt in den nächsten Vollvorgang ein).
10. **Optionale Tests aus Schritt 13:** Sind die Tests für `DetectExtension` und den Löschfehler-Zweig verpflichtend für diesen Schritt oder optional?
11. **Strecke bei Teilbetankungen:** Bestätigung, dass die Strecke ausschließlich zwischen Vollvorgängen gemessen wird und Teilbetankungen nur Menge und Kosten beitragen.
