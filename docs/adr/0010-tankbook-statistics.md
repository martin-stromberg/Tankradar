# ADR 0010: Verbrauchs- und Kostenauswertung im Tankbuch

## Status

Angenommen (2026-10-12)

## Kontext

Schritt 14 wertet die in Schritt 13 erfassten Tank- und Ladevorgänge aus: Durchschnittsverbrauch, Kosten pro 100 km,
Monatskosten sowie ein Verbrauchs- und ein Kostenverlaufsdiagramm je Fahrzeug. Alles läuft lokal und offline; es gibt keine
Schemaänderung. Die Gesamtanforderung lässt mehrere Auslegungsfragen offen; sie sind hier entschieden. Grundlage sind
[ADR 0008](0008-vehicle-management.md) (Fahrzeuge) und [ADR 0009](0009-tankbook-entries.md) (Vorgänge, Verbrauchskette).

## Entscheidungen

### Diagramme per MAUI Graphics, keine Diagrammbibliothek

Die Diagramme werden selbst gezeichnet (`GraphicsView` und `IDrawable` aus `Microsoft.Maui.Graphics`, Teil von
`Microsoft.Maui.Controls`). Es kommt keine neue NuGet-Abhängigkeit hinzu. Gründe:

- **iOS-Interpreter und Trimming.** Der iOS-Build läuft mit `MtouchInterpreter=all` und Trimming (`TrimmerRootAssembly` für
  die App, EF Core und Microsoft.Data.Sqlite), weil der Release-Build ohne JIT läuft und EF Core Laufzeitcode braucht.
  Diagrammbibliotheken (z. B. SkiaSharp-basierte Pakete, Pakete mit Reflection oder Source-Generatoren) sind dort ein
  Build- und Laufzeitrisiko, das sich erst auf dem Gerät zeigt. Der Zeichencode kommt ohne Reflection aus und ist trimming-
  und interpretersicher.
- **CI-Laufzeit.** Das 60-Minuten-Limit des iOS-Builds ist bereits knapp (deshalb `-all` durch `all` ersetzt). Zusätzliche
  native Bibliotheken würden Build und Paketgröße erhöhen.
- **Lizenz.** Diagrammpakete stehen teils unter kommerziellen oder Community-Lizenzen, die geprüft und gepflegt werden müssten.
- **Aufwand.** Zwei einfache Diagrammarten (Linie, Balken) rechtfertigen keine Abhängigkeit.

Alternativen waren ein Chart-Paket (verworfen, siehe oben) und reine Textlisten (verworfen, weil die Anforderung Diagramme
verlangt). Die Textzusammenfassung und die Monatsliste bleiben als Alternative für die Sprachausgabe erhalten.

**Aufbau.** Skalierung und Layout (Wertebereich, Koordinaten, Beschriftungsstellen) liegen in der reinen, unit-getesteten Klasse
`ChartGeometry`; `LineChartDrawable` und `BarChartDrawable` zeichnen nur anhand ihres Ergebnisses. Die Farben liefert
`ChartPalette` für hell und dunkel (abgeleitet aus dem Design-System). `ChartGraphicsView` wählt den Drawable nach
`ChartData.Kind`. Die Drawables sind reiner Zeichencode und von der Coverage ausgenommen (`**/Views/Charts/**` in
`coverlet.runsettings`); die Berechnung bleibt gemessen.

### Kennzahlen

- **Schwelle „zu wenige Vorgänge“.** Durchschnittsverbrauch und Kosten pro 100 km erscheinen, sobald je Art mindestens ein
  berechnetes Intervall vorliegt (zwei volle Vorgänge). Darunter erscheint der Startverbrauch (falls vorhanden), sonst ein Hinweis.
- **Startverbrauch.** Nur Ersatzwert für den Durchschnittsverbrauch, solange kein Intervall der Art existiert; gekennzeichnet
  als „Startverbrauch“ und nie mit berechneten Werten gemischt. Er gilt nur für die Art, deren Einheit er trägt
  (Elektro-Fahrzeug: Strom, sonst Kraftstoff; bei Hybrid also nur Kraftstoff). Kosten pro 100 km haben keinen Ersatzwert.
- **Durchschnittsverbrauch.** Mengengewichtet: Σ Intervallmengen / Σ Intervallstrecken × 100, auf 2 Nachkommastellen
  (`MidpointRounding.AwayFromZero`, wie `ConsumptionCalculator`). Das ungewichtete Mittel der Einzelwerte würde kurze Strecken
  überbewerten. Intervalle mit Strecke ≤ 0 und Teilbetankungen vor dem ersten Vollvorgang gehen nicht ein.
- **Kosten pro 100 km.** Σ Intervallkosten / Σ Intervallstrecken × 100. Die Intervallkosten sind der Gesamtbetrag des
  Vollvorgangs plus der dazwischenliegenden Teilbetankungen, damit Kosten und Strecke dieselben Intervalle betreffen.
  Teilbetankungen tragen Menge und Kosten zum nächsten Intervall bei, die Strecke zählt nur zwischen Vollvorgängen derselben Art.
- **Hybrid.** Je vorhandener Art ein eigener Block (Kraftstoff vor Strom); Liter und kWh werden nie gemischt, auch Kosten pro
  100 km, Monatskosten und Diagramme sind je Art getrennt (ADR 0009: je Art eine eigene Kette).
- **Monatskosten.** Σ Gesamtbetrag aller Vorgänge der Art, auch Teilbetankungen und solche vor dem ersten Vollvorgang, je
  Kalendermonat des Zeitpunkts (lokale Wanduhrzeit ohne Zeitzone, keine Umrechnung; 31.12. 23:59 gehört zum Dezember). Die Reihe
  läuft lückenlos vom Monat des ersten Vorgangs bis zum späteren aus letztem Vorgangsmonat und aktuellem Monat (`TimeProvider`);
  Monate ohne Vorgang zeigen 0,00 €. Eine lückenlose Reihe hält Balkendiagramm und Liste ehrlich. Die angezeigte Monatsliste ist auf
  die neuesten 24 Monate begrenzt (`TankbookStatisticsLimits.MonthListMaxRows`, mit Hinweis), weil ein sehr weit zurückliegender
  Vorgang sonst hunderte Zeilen erzeugt; Durchschnitt, Kosten pro 100 km und Gesamtkosten enthalten weiterhin alle Vorgänge.
- **Diagrammarten und Zeitraum.** Verbrauchsverlauf: Liniendiagramm mit einem Punkt je berechnetem Vollvorgang. Kostenverlauf:
  Balkendiagramm der Monatskosten. Beide zeigen die letzten 12 Datenpunkte (`TankbookStatisticsLimits.ChartMaxPoints`); die
  Begrenzung betrifft nur die Zeichnung, Kennzahlen und Monatsliste enthalten alle Daten. Es gibt keinen Zeitraumfilter.
  Der Wert steht am Datenpunkt (bei mehr als sechs Punkten nur am letzten), die Achse hat höchstens vier Beschriftungen.

### Berechnung und Aufbau

- **Berechnung beim Lesen, nichts gespeichert.** `TankbookEntryOverview` erhält die optionalen Werte `IntervalQuantity` und
  `IntervalCost` (nur bei berechnetem Verbrauch belegt), die `ConsumptionCalculator` aus derselben Kette füllt. Das statische
  `TankbookStatisticsCalculator` aggregiert sie. Es gibt keine zweite Verbrauchsrechnung.
- **Dienst.** `ITankbookStatisticsService` liefert `TankbookStatisticsLoad` (`Ok`, `VehicleNotFound`) statt einer Ausnahme;
  Speicherfehler werden durchgereicht und im `TankbookStatisticsViewModel` abgefangen, sodass die Liste der Vorgänge nutzbar
  bleibt. Protokolliert wird nur der Ausnahmetyp.
- **Platzierung.** Der Bereich „Auswertung“ steht auf der Seite „Tank- und Ladevorgänge“ zwischen Formular und Liste; das
  Fahrzeug bestimmt die Seite, es gibt keine zweite Auswahl. Die Auswertung wird mit der Liste geladen und dadurch nach
  Erfassen, Korrigieren und Löschen aktualisiert.
- **Barrierefreiheit.** Jedes Diagramm hat eine sichtbare Textzusammenfassung (z. B. „Verlauf, 3 Werte, zuletzt 6,00 L/100 km,
  niedrigster 5,80, höchster 6,20“), die das `GraphicsView` als `SemanticProperties.Description` erhält. Die Monatsliste ist
  die Tabellenalternative zum Kostendiagramm. Überschriften tragen `SemanticProperties.HeadingLevel`. Das Diagramm selbst erhält
  nur eine kurze Beschreibung („Liniendiagramm des Verbrauchsverlaufs“), damit die Zusammenfassung nicht doppelt vorgelesen wird.
  Beim Wechsel zwischen hellem und dunklem Erscheinungsbild zeichnen die Diagramme neu; überholte Ladevorgänge der Auswertung
  werden verworfen.

## Abweichungen vom Designentwurf

Der Entwurf `tankbuch_verbrauchsauswertung` ist Grundlage. Übernommen sind die Kacheln Ø Verbrauch, Kosten pro 100 km und
Monatskosten, das Verlaufsdiagramm mit Wert am Datenpunkt, die Farben des Design-Systems und der Dunkelmodus. Nicht umgesetzt:

- **Trendzeilen gegenüber dem Vormonat** und die Plakette „Effizient“: Dafür gibt es keine fachliche Regel in der Anforderung.
- **Zielmarke („Ziel: 5,50 L“)**: Im Datenmodell gibt es keinen Zielwert.
- **Fahrzeug-Dropdown**: Das Fahrzeug bestimmt die Seite (siehe ADR 0009).
- Zusätzlich zum Entwurf: Kostenverlauf als Balkendiagramm, getrennte Blöcke je Art und eine Textzusammenfassung je Diagramm.

## Folgen

- Keine Migration; bestehende Daten werden unverändert ausgewertet (`TankbookStatisticsServiceTests_RealDatabase`).
- Der Export (Schritt 16) kann die Kennzahlen aus `TankbookStatisticsCalculator` wiederverwenden.
- Der Inhalt eines `GraphicsView` ist per UI-Automation nicht prüfbar; die E2E-Tests prüfen die Zusammenfassungstexte und die
  Existenz der Diagramm-AutomationIds, die Geometrie ist unit-getestet.
- Eine Zeitzonen- oder Sommerzeitumstellung verschiebt keine Monatsgrenzen, weil Zeitpunkte als Wanduhrzeit gespeichert werden.
