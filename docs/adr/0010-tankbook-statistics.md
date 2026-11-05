# ADR 0010: Verbrauchs- und Kostenauswertung im Tankbuch

## Status

Angenommen (2026-10-12)

## Kontext

Schritt 14 wertet die in Schritt 13 erfassten Tank- und Ladevorgänge aus: Durchschnittsverbrauch, Kosten pro 100 km,
Monatskosten sowie ein Verbrauchs- und ein Kostenverlaufsdiagramm je Fahrzeug. Die Darstellung folgt dem Designentwurf (Abschnitt „Abweichungen vom Designentwurf“). Alles läuft lokal und offline; es gibt keine
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
- **Aufwand.** Ein einfaches Balkendiagramm rechtfertigt keine Abhängigkeit.

Alternativen waren ein Chart-Paket (verworfen, siehe oben) und reine Textlisten (verworfen, weil die Anforderung Diagramme
verlangt). Die Textzusammenfassung und die Monatsliste bleiben als Alternative für die Sprachausgabe erhalten.

**Aufbau.** Skalierung und Layout (Wertebereich, Koordinaten, Beschriftungsstellen) liegen in der reinen, unit-getesteten Klasse
`ChartGeometry`; `BarChartDrawable` zeichnet nur anhand ihres Ergebnisses. Die Farben liefert `ChartPalette` für hell und
dunkel (abgeleitet aus dem Design-System; der neueste Balken in der Akzentfarbe, ältere Balken in einer helleren Abstufung mit
mindestens 3:1 Kontrast zur Fläche). Die Drawables sind reiner Zeichencode und von der Coverage ausgenommen
(`**/Views/Charts/**` in `coverlet.runsettings`); die Berechnung bleibt gemessen.

**Textflächen am Rand.** `ChartGeometry` liefert für jeden Wert und jede Achsenbeschriftung eine Fläche (`ChartTextArea`), die
immer innerhalb der Diagrammfläche liegt. Ragt die mittig gesetzte Fläche über den Rand, wird sie nach innen geschoben und der
Text am linken Rand linksbündig, am rechten rechtsbündig gezeichnet. Dadurch wird die erste und letzte Beschriftung auf jeder
Breite vollständig angezeigt; das ist unit-getestet. Ein einzelner Balken ist auf 44 pt Breite begrenzt.

**Ohne Daten kein Diagramm.** Die Seite blendet die `ChartGraphicsView` aus, solange `ChartData.HasData` falsch ist. Statt einer
leeren Fläche steht die Textzusammenfassung („Noch kein Verlauf …“); die Sprachausgabe kündigt kein leeres Diagramm an.

**Erscheinungsbild.** `ChartGraphicsView` zeichnet beim Wechsel zwischen hell und dunkel neu. Das Abonnement von
`Application.RequestedThemeChanged` hält die View nicht am Leben (schwache Referenz) und endet, sobald die View ihren Handler
verliert. So entsteht auch bei neu aufgebauten Listeneinträgen (`BindableLayout` nach jedem Speichern oder Löschen) keine
Speicherlücke; ein verwaistes Abonnement entfernt sich beim nächsten Wechsel selbst.

### Kennzahlen

- **Schwelle „zu wenige Vorgänge“.** Durchschnittsverbrauch und Kosten pro 100 km erscheinen, sobald je Art mindestens ein
  berechnetes Intervall vorliegt. Für die Startart des Fahrzeugs genügt seit Schritt 15a ein voller Vorgang, weil der
  Start-Kilometerstand die Basis liefert (ADR 0009); für die andere Art eines Hybrids braucht es weiterhin zwei volle Vorgänge.
  Darunter erscheint der Startverbrauch (falls vorhanden), sonst ein Hinweis. **Ergänzt in Schritt 15a – Intervall ab Startstand:**
  Es bildet Durchschnitt und Kosten pro 100 km nur, solange es kein reguläres Intervall (zwischen zwei vollen Vorgängen) gibt;
  die Kennzahl ist dann als „Ab Startstand – angenommen wird ein voller Tank beim Start“ gekennzeichnet (`TankbookAverageSource.FromStart`).
  Sobald ein reguläres Intervall vorliegt, geht das Startintervall nicht mehr in Durchschnitt und Kosten ein, weil die Annahme
  unsicher ist und den Wert sonst dauerhaft verzerren könnte; im Verbrauchsverlauf und in der Liste bleibt sein Wert sichtbar.
  Der gemessene Wert ab Startstand geht einem hinterlegten Startverbrauch vor; der Startverbrauch gilt nur noch, solange kein
  voller Vorgang mit Strecke vorliegt („Startverbrauch – noch keine Strecke aus vollen Vorgängen“; die Kennzeichnung gilt auch, wenn der erste volle Vorgang genau auf dem Start-Kilometerstand liegt und daher keine Strecke ergibt). Der Hinweis lautet für die Startart
  „Für den Verbrauch wird mindestens ein voller Vorgang benötigt.“ (`TankbookKindStatistics.IsStartKind`), für den Strom-Block
  eines Hybrids unverändert „… mindestens zwei volle Vorgänge …“.
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
  Vorgang sonst hunderte Zeilen erzeugt; Durchschnitt und Kosten pro 100 km enthalten weiterhin alle Vorgänge.
- **Monatskarte.** „Gesamtkosten <Monat>“ zeigt die Kosten des laufenden Monats (der neueste Eintrag der Monatsreihe; das ist der Monat von „heute“, außer ein Vorgang innerhalb der Zukunftstoleranz liegt im Folgemonat) und die in
  diesem Monat getankte bzw. geladene Menge („98,50 L getankt“, bei Strom „kWh geladen“). Dazu führt `MonthlyCostSummary` die
  Summe der Mengen. Ohne Vorgang im laufenden Monat stehen dort 0,00 € und 0,00 L bzw. kWh. Die Gesamtsumme über alle Monate
  (`TankbookKindStatistics.TotalCost`) bleibt im Modell, wird aber nicht mehr angezeigt, weil der Entwurf sie nicht vorsieht.
- **Diagrammarten und Zeitraum.** Verbrauchsverlauf: Balkendiagramm der letzten 6 berechneten Vollvorgänge
  (`TankbookStatisticsLimits.ConsumptionChartMaxPoints`), wie im Entwurf. Kostenverlauf: Balkendiagramm der Monatskosten, die
  letzten 12 Monate (`ChartMaxPoints`). In beiden ist der neueste Balken hervorgehoben. Die Begrenzung betrifft nur die
  Zeichnung, Kennzahlen und Monatsliste enthalten alle Daten. Es gibt keinen Zeitraumfilter. Die Balken beginnen bei 0 (der
  Entwurf zeigt gekappte Balken, die Unterschiede übertreiben). Der Wert steht über dem Balken (bei mehr als sechs Balken nur am
  letzten), die Achse hat höchstens vier Beschriftungen.

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
  niedrigster 5,80, höchster 6,20“) als eigenen Text unter dem Diagramm. Das Diagramm selbst erhält nur eine kurze
  `SemanticProperties.Description` („Balkendiagramm des Verbrauchsverlaufs“ bzw. „Balkendiagramm der Monatskosten“), damit die
  Zusammenfassung nicht doppelt vorgelesen wird. Die Monatsliste ist die Tabellenalternative zum Kostendiagramm. Überschriften
  tragen `SemanticProperties.HeadingLevel`. Die Kacheln tragen eine Beschreibung für die Sprachausgabe („Durchschnittsverbrauch“
  statt „Ø Verbrauch“, „Kosten pro 100 km“ statt „Kosten / 100 km“). Überholte Ladevorgänge der Auswertung werden verworfen.
- **Hinweis bei zu wenigen Vorgängen.** „Für den Verbrauch werden mindestens zwei volle Vorgänge benötigt.“ Der zweite Satz
  („Alternativ kannst du am Fahrzeug einen Startverbrauch hinterlegen.“) steht nur, wenn die Art die Startart des Fahrzeugs ist
  und noch kein Startverbrauch hinterlegt ist (`TankbookKindStatistics.OffersStartConsumption`). Der Strom-Block eines Hybrids
  verweist nie darauf, weil der Startverbrauch dort nie gilt.

## Abweichungen vom Designentwurf

Der Entwurf `tankbuch_verbrauchsauswertung` (hell und dunkel) ist Grundlage und gilt als verbindlich; jede Abweichung steht hier.

**Übernommen:**

- Zweispaltiges Kachelraster mit den Kacheln „Ø Verbrauch“ und „Kosten / 100 km“: kleine Beschriftung, große Zahl
  (`price-hero`, 28 pt), die Einheit klein daneben. Auf schmalen Bildschirmen (360 bis 390 pt) bricht die Einheit unter die Zahl,
  statt abgeschnitten zu werden (`FlexLayout` mit Umbruch); auch lange Werte wie „20,00 kWh/100 km“ passen.
- Überschrift „Monatsanalyse <Monat Jahr>“ und die Karte „Gesamtkosten <Monat>“ mit dem Betrag des laufenden Monats in
  `price-hero` und der Menge als Plakette („98,50 L getankt“, bei Strom „… kWh geladen“).
- Verbrauchsverlauf als Balkendiagramm der letzten 6 Betankungen bzw. Ladevorgänge mit hervorgehobenem letztem Balken und dem
  Wert über jedem Balken; Farben des Design-Systems, Hell- und Dunkelmodus.

**Bewusste Abweichungen:**

- **Getrennte Blöcke je Art.** Der Entwurf zeigt ein Fahrzeug mit einer Art. Bei Hybridfahrzeugen gibt es je Art einen eigenen
  Block (Überschrift „Kraftstoff“ bzw. „Strom“), damit Liter und kWh nie vermischt werden (ADR 0009).
- **Überschrift „Monatsanalyse“ über der Monatskarte statt über den Kacheln.** Die Kacheln zeigen Werte über alle Vorgänge, nur
  die Karte gehört zum laufenden Monat; die Überschrift steht dort, wo sie stimmt. Darüber steht die Überschrift „Auswertung“
  (Teil der Vorgangsseite statt einer eigenen Seite).
- **Kachelwert „noch nicht berechenbar“.** Ohne Intervall zeigt die Kachel „–“ und statt der Einheit „noch nicht berechenbar“
  bzw. bei „Ø Verbrauch“ den gekennzeichneten Startverbrauch.
- **Balken beginnen bei 0.** Der Entwurf kappt die Achse (Balken 34 bis 50 von 70 für Werte um 5,4); das übertreibt Unterschiede.
  Dafür steht der genaue Wert über dem Balken.
- **Keine Benchmarklinie und kein Ziel („Ziel: 5,50 L“).** Im Datenmodell gibt es keinen Zielwert.
- **Keine Trendzeilen gegenüber dem Vormonat** („-0,3 L ggü. Vormonat“, „Stabil“) und keine Plakette „Effizient“: Dafür gibt es
  keine fachliche Regel in der Anforderung.
- **Keine Symbole** in Kacheln und Karte (Tank-, Münz- und Kalendersymbol des Entwurfs). Symbole wären laut
  [ADR 0004](0004-station-detail-design-deviations.md) als Vektorpfade ohne neue Ressource möglich; verzichtet wird, weil die
  Information vollständig in den Beschriftungen steckt, die Symbole für die Sprachausgabe ohne Wert wären und die Kacheln
  auf 360 pt Breite dichter würden.
- **Verbrauchsverlauf nicht auf der Fläche der Monatskarte und ohne Abstufung.** Im Entwurf steht das Balkendiagramm innerhalb
  der Monatskarte, und ältere Balken sind in mehreren Stufen abgeschwächt. Hier steht der Verlauf als eigener Abschnitt unter den
  Kacheln (er zeigt Vorgänge, nicht den Monat), und es gibt nur zwei Zustände: letzter Balken in der Akzentfarbe, ältere Balken in
  einer einheitlich helleren Farbe mit mindestens 3:1 Kontrast zur Fläche.
- **Fahrzeug-Dropdown und „Aktives Fahrzeug“-Karte:** Das Fahrzeug bestimmt die Seite (ADR 0009).
- **Achsenbeschriftung** steht mit Datum bzw. Monat unter höchstens vier Balken (der Entwurf nennt „Mär“, „Apr“ …); der
  Kostenverlauf (Monatskosten als Balken) und die Monatsliste kommen zusätzlich zum Entwurf, weil die Anforderung einen
  Kostenverlauf und Monatskosten verlangt.

## Folgen

- Keine Migration; bestehende Daten werden unverändert ausgewertet (`TankbookStatisticsServiceTests_RealDatabase`).
- Der Export (Schritt 16) kann die Kennzahlen aus `TankbookStatisticsCalculator` wiederverwenden.
- Der Inhalt eines `GraphicsView` ist per UI-Automation nicht prüfbar; die E2E-Tests prüfen die Zusammenfassungstexte, die
  Kennzahlen und die Existenz der Diagramm-AutomationIds (ohne Werte deren Fehlen), die Geometrie samt Randausrichtung ist
  unit-getestet.
- Eine Zeitzonen- oder Sommerzeitumstellung verschiebt keine Monatsgrenzen, weil Zeitpunkte als Wanduhrzeit gespeichert werden.
- **Kachelzahlen ohne Umbruch (Schritt 15).** Die großen Zahlen der Kacheln stehen mit `LineBreakMode="NoWrap"` und verkleinern sich
  nach Länge (`StatisticsTileFont`: bis 6 Zeichen 28 pt, 7 Zeichen 24 pt, 8 Zeichen 21 pt, darüber 18 pt), damit auch Werte
  wie „9.989,00“ oder „14.984,00“ auf 360 bis 390 pt Breite einzeilig bleiben; die Einheit bricht weiter unter die Zahl. Der
  Betrag der Monatskarte erhält `NoWrap`. Die Schriftgröße ist unit-getestet, die Einzeiligkeit per FlaUI
  (`TankbookStatisticsE2ETests_TileNumbers`).
- **Offen: Sichtprüfung.** Das Aussehen der Auswertung (Kacheln, Diagramme in Hell- und Dunkelmodus, Schriftanpassung) ist bisher
  nur über Geometrie, Texte und AutomationIds geprüft, nicht visuell am Gerät. Sie bleibt Teil der Abnahme auf dem iPhone.
