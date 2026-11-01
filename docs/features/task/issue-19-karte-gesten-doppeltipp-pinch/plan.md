# Umsetzungsplan: Doppeltipp-Zentrierung und fließender Zwei-Finger-Zoom auf der Kartenansicht

## Übersicht

Die Kartenansicht (`StationMapView`) erhält zwei Gesten. Ein Doppeltipp (Doppelklick unter Windows) zentriert die Karte auf die angetippte Stelle und zoomt eine Stufe hinein, höchstens bis `MapViewport.MaxZoom`. Die Zwei-Finger-Geste zoomt fließend um den Gestenmittelpunkt: Während der Geste wird die Kachelebene skaliert angezeigt, bei Gestenende rastet der Zoom auf die nächste ganze Stufe ein, und der Punkt unter dem Gestenmittelpunkt bleibt stehen. Betroffen sind die Kartenmathematik (`MapViewport`), zwei neue Hilfsklassen für Gestenzustand und Doppeltipp-Erkennung, die Ansicht `StationMapView` (inkl. XAML und Windows-Zeigerteil) sowie die Hilfe und ADR 0005.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Zoom um einen Bildschirmpunkt (Fixpunkt) | Neue unveränderliche Methode `MapViewport.ScaleAtPoint(factor, x, y)` (Value Object, Domain Model) | Die Berechnung braucht die privaten Weltkoordinaten; der private Konstruktor bleibt die einzige Erzeugungsstelle. Die Methode ist plattformunabhängig und per Unit-Test prüfbar. `ZoomBy` bleibt unverändert, da Schaltflächen und Mausrad die Mitte behalten sollen. |
| Umrechnung Faktor zu Zoomstufen | Statische Methode `MapViewport.ZoomStepsFor(factor)` (round(log2(factor)), ungültige Faktoren ergeben 0) | Eine zustandslose Funktion; kein eigener Typ nötig. Die Rundung wird damit an genau einer Stelle getestet. |
| Doppeltipp-Zentrierung | Neue Methode `MapViewport.ZoomAndCenterAt(delta, x, y)`: Mitte auf den Tippunkt, Stufe um `delta` (begrenzt) | Die Mitte wird in Weltkoordinaten gesetzt, ohne Umweg über Breiten- und Längengrad. An der Obergrenze wird nur zentriert (ausdrücklich in der Anforderung vorgesehen). |
| Gestenzustand Zwei-Finger-Zoom | Neue Klasse `MapPinchTracker` (Zustandsobjekt, Muster wie `MapPointerTracker`) | Kumuliert den Gesamtfaktor und den Gestenmittelpunkt, ist ohne Oberfläche testbar und kapselt die Scale-Semantik an einer Stelle. |
| Doppeltipp-Erkennung, Touch und mobile Geräte | `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` am `Surface`-Grid in der XAML-Datei (deklarativ, neben `PinchGestureRecognizer`) | Plattformübergreifend, ohne eigene Zeitlogik. Entspricht dem in der Anforderung genannten Weg. |
| Doppelklick mit der Windows-Maus | Gate-Entscheidung in Schritt 1 (Prototyp). Ergebnis A: Der Recognizer feuert auch bei Maus, Pointer-Teil bleibt unverändert. Ergebnis B: Der Recognizer feuert nicht, `StationMapView.Pointer.cs` erkennt den Doppelklick über die neue Klasse `MapDoubleTapDetector` | Die Pointer-Handler setzen `e.Handled = true`, dadurch ist ungeklärt, ob der Recognizer Mausklicks erhält. Der Prototyp klärt das vor der Umsetzung. Bei B darf der Recognizer für Maus nicht zusätzlich feuern, weil sonst zweimal gezoomt wird. |
| Gate-Ergebnis (Schritt 1) | Ergebnis B. `Scale` inkrementell, `ScaleOrigin` relativ (0 bis 1). Doppelte Auslösung verhindert der Zeigerteil über die partielle Methode `SuppressDoubleTap`: War der letzte Druck ein Mausdruck, wertet `OnDoubleTapped` nicht aus | Ermittelt aus dem Quelltext von MAUI 10.0.120 (`GesturePlatformManager`, Windows): Pinch nutzt `ManipulationDelta.Delta.Scale` und teilt die Position durch die Größe der Ansicht; Doppeltipps laufen über WinUI `DoubleTapped`. Ob `DoubleTapped` bei `Handled=true` und Zeigerfang für die Maus auslöst, war ohne echten Doppelklick nicht prüfbar; Ergebnis B funktioniert in beiden Fällen ohne doppeltes Zoomen. Ungeprüft: Gerätetest Touch, iOS/Android-Semantik |
| Erkennungsschwellen bei Ergebnis B | Feste Werte in `MapDoubleTapDetector`: höchstens 500 ms zwischen den Tipps, höchstens 4 Pixel Abstand; Erkennung wird nach jedem Doppeltipp zurückgesetzt | Entspricht dem Windows-Standard, ist ohne Interop testbar. Eine Anbindung an die Systemeinstellung ist nicht Teil dieses Umfangs. |
| Anzeige der Geste (Vorschau) | Nur die Ebene `TileLayer` wird während der Geste skaliert (`Scale` mit Ankerpunkt `AnchorX`/`AnchorY` am Gestenmittelpunkt). Markierungen und Route bleiben unskaliert und werden erst bei Gestenende neu gezeichnet | Eine Skalierung der Markierungen würde Buttons verzerren. Der Sprung der Markierungen beim Einrasten ist akzeptiert. Die Vorschau wird auf die erlaubten Stufen begrenzt (Faktor zwischen Stufe `MinZoom` und `MaxZoom`). |
| Kacheln während der Geste | Keine Kachelabfrage während der Geste; Nachladen erst nach Gestenende in `Render()` | Vermeidet unnötige Anfragen an den Kachelserver. |
| Wischen während des Kneifens | `OnPanUpdated` ignoriert Verschiebungen, solange `MapPinchTracker` aktiv ist | Wischen und Kneifen würden sonst gleichzeitig die Mitte verändern und den Fixpunkt verfälschen. |
| Rendern und Zurücksetzen der Vorschau | `Render()` setzt die Skalierung der `TileLayer` immer auf 1 zurück | Schützt davor, dass ein nachträgliches Einpassen (`FitToContent`) oder eine Größenänderung während der Geste eine skalierte Ansicht stehen lässt. |
| Ereignisse | `ViewportChanged` nur bei Doppeltipp und bei Kneifgeste mit Stufenwechsel (über `RaiseViewportChanged()`) | Entspricht der Anforderung und der Regel „nur Nutzeraktionen“ (ADR 0005). |
| Dokumentation | Ergänzung von ADR 0005 (kein neuer ADR) | Die Gesten gehören zur selben Kartenentscheidung; der Abschnitt „Abweichungen vom Designentwurf“ muss angepasst werden. |

## Programmabläufe

### Zwei-Finger-Zoom (Kneifen)

1. Bei `GestureStatus.Started` setzt `OnPinchUpdated` den `MapPinchTracker` zurück und startet ihn mit dem Gestenmittelpunkt (`ScaleOrigin`). Ist `_viewport` nicht gesetzt, passiert nichts.
2. Bei `GestureStatus.Running` ruft `OnPinchUpdated` `MapPinchTracker.Update(e.Scale, origin)` auf. Der Tracker multipliziert den Einzelfaktor (Annahme: inkrementell, siehe Offene Punkte 3) in den Gesamtfaktor und merkt sich den letzten Gestenmittelpunkt.
3. Bei `Running` setzt die Vorschau: `TileLayer.Scale` ist der Gesamtfaktor, begrenzt auf den Bereich zwischen `MinZoom` und `MaxZoom` relativ zur aktuellen Stufe. `AnchorX`/`AnchorY` ist der Gestenmittelpunkt. Es wird kein `Render()` und kein Kachelladen ausgelöst.
4. Bei `GestureStatus.Completed` ermittelt `OnPinchUpdated` die Schrittzahl über `MapViewport.ZoomStepsFor(Gesamtfaktor)`.
5. Ist die Schrittzahl ungleich 0, berechnet `MapViewport.ScaleAtPoint(Gesamtfaktor, x, y)` den neuen Ausschnitt mit dem Gestenmittelpunkt als Fixpunkt. Ist sie 0, bleibt der Ausschnitt unverändert.
6. Der Tracker wird beendet. `Render()` setzt die Skalierung der `TileLayer` auf 1 und zeichnet neu.
7. `RaiseViewportChanged()` wird nur ausgelöst, wenn sich die Zoomstufe geändert hat.

Beteiligte Klassen/Komponenten: `StationMapView` (`OnPinchUpdated`, `Render`), `MapPinchTracker`, `MapViewport` (`ZoomStepsFor`, `ScaleAtPoint`), `TileLayer` (AbsoluteLayout)

### Doppeltipp (Touch und Windows-Maus, Ergebnis A)

1. Die XAML-Datei enthält am `Surface`-Grid einen `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` und dem Handler `OnDoubleTapped`.
2. `OnDoubleTapped` liest die Position über `e.GetPosition(Surface)`. Ist sie `null`, endet der Ablauf.
3. `ZoomAndCenterAt(x, y)` ruft `MapViewport.ZoomAndCenterAt(1, x, y)` auf, setzt `_viewport`, ruft `Render()` und `RaiseViewportChanged()` auf.
4. Liegt ein Doppeltipp auf einer Markierung, bedient zuerst der Button die Navigation (Anforderung, Offene Punkte 1). Der Recognizer erhält dann keinen Tipp.
5. Der einfache Tipp hat keinen Handler und bleibt unverändert.

Beteiligte Klassen/Komponenten: `StationMapView` (`OnDoubleTapped`, `ZoomAndCenterAt`), `TapGestureRecognizer` (XAML), `MapViewport` (`ZoomAndCenterAt`)

### Doppelklick mit der Windows-Maus (nur bei Ergebnis B)

1. `OnPointerPressed` (nur Maus, linke Taste) übergibt die Position und den Zeitstempel an `MapDoubleTapDetector`.
2. Liefert der Detektor einen Doppeltipp, ruft `StationMapView.Pointer.cs` `ZoomAndCenterAt(x, y)` auf, dieselbe Methode wie beim Recognizer.
3. Der Detektor setzt sich nach einem erkannten Doppeltipp zurück; ein dritter schneller Klick beginnt eine neue Folge.
4. Der Ziehen-Ablauf (`MapPointerTracker`) und das Mausrad bleiben unverändert.

Beteiligte Klassen/Komponenten: `StationMapView.Pointer.cs`, `MapDoubleTapDetector`, `StationMapView` (`ZoomAndCenterAt`)

### Vorschau und Kachelebene beim Wischen während des Kneifens

1. `OnPanUpdated` prüft zuerst `MapPinchTracker.IsActive`. Ist die Kneifgeste aktiv, endet der Handler ohne Verschiebung.
2. Nach Gestenende ist die Sperre aufgehoben; der nächste Wisch arbeitet wie bisher.

Beteiligte Klassen/Komponenten: `StationMapView` (`OnPanUpdated`), `MapPinchTracker`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `MapPinchTracker` | Klasse (Zustandsobjekt, `src/Tankradar.MAUI/Services/Map/MapPinchTracker.cs`) | Kumuliert den Gesamtfaktor einer Kneifgeste (Multiplikation der Einzelfaktoren), merkt den letzten Gestenmittelpunkt und zeigt an, ob eine Geste aktiv ist. Plattformunabhängig. |
| `MapDoubleTapDetector` | Klasse (Zustandsobjekt, `src/Tankradar.MAUI/Services/Map/MapDoubleTapDetector.cs`) | Erkennt einen Doppeltipp aus Position und Zeitstempel (Zeitfenster 500 ms, Abstand 4 Pixel). Nur angelegt, wenn der Prototyp (Schritt 1) Ergebnis B liefert. Die Zeit wird als Parameter übergeben, damit der Test ohne Uhr auskommt. |

## Änderungen an bestehenden Klassen

### `MapViewport` (Klasse, Domain Model / Value Object)

- **Neue Methoden:**
  - `ZoomStepsFor(factor)` (öffentlich, statisch): Liefert die ganzzahlige Schrittzahl round(log2(factor)). Ungültige Faktoren (0, negativ, NaN, unendlich) liefern 0.
  - `ScaleAtPoint(factor, x, y)`: Liefert einen neuen Ausschnitt mit Zoom + `ZoomStepsFor(factor)`, begrenzt auf `MinZoom`..`MaxZoom`. Der geografische Punkt unter (x, y) liegt danach an derselben Bildschirmposition. Ergibt die Berechnung keine Änderung, wird `this` zurückgegeben. Berechnung: Weltkoordinate des Punkts vor dem Zoom ist `_centerX + (x - Width/2)` bzw. `_centerY + (y - Height/2)`; die neue Mitte ist die Weltkoordinate mal 2^Schritte, abzüglich des Abstands zur Mitte. X wird modulo Weltbreite umgebrochen, Y begrenzt der Konstruktor.
  - `ZoomAndCenterAt(delta, x, y)`: Liefert einen neuen Ausschnitt, dessen Mitte der Punkt (x, y) ist, mit Zoom + delta, begrenzt auf `MinZoom`..`MaxZoom`. Gibt auch an der Grenze einen neuen Ausschnitt zurück (nur Zentrieren).
- **Geänderte Methoden:** keine. `ZoomBy`, `ToLatLon`, `ToScreen`, `Create`, `PanByPixels` bleiben unverändert.
- **Neue Events:** keine.

### `MapPinchTracker` (neue Klasse)

- **Neue Eigenschaften:** `IsActive` (bool), `Factor` (double, Start 1), `OriginX`, `OriginY` (double) — Gesamtfaktor und letzter Gestenmittelpunkt.
- **Neue Methoden:**
  - `Start(originX, originY)`: Setzt Faktor auf 1, aktiviert den Tracker und merkt den Mittelpunkt.
  - `Update(scale, originX, originY)`: Multipliziert den Einzelfaktor in `Factor`, aktualisiert den Mittelpunkt. Wirkungslos ohne `Start`. Ungültige Einzelfaktoren (nicht endlich oder kleiner oder gleich 0) werden ignoriert.
  - `End()`: Deaktiviert den Tracker und setzt den Faktor auf 1.

### `MapDoubleTapDetector` (neue Klasse, nur bei Ergebnis B)

- **Neue Methoden:** `Register(x, y, timestampMs)`: liefert `true`, wenn dieser Tipp den zweiten Tipp eines Doppeltipps bildet (höchstens 500 ms nach dem vorherigen Tipp und höchstens 4 Pixel entfernt). Danach wird der Zustand zurückgesetzt.

### `StationMapView` (Klasse, View / partial)

- **Neue Eigenschaften:** `_pinch` (`MapPinchTracker`, privat).
- **Entfernte Eigenschaften:** `_pinchScale` (ersetzt durch `_pinch`).
- **Neue Methoden:**
  - `OnDoubleTapped(sender, TappedEventArgs)`: liest die Position über `GetPosition(Surface)` und ruft `ZoomAndCenterAt` auf.
  - `ZoomAndCenterAt(x, y)`: `MapViewport.ZoomAndCenterAt(1, x, y)`, `Render()`, `RaiseViewportChanged()`. Gemeinsame Methode für Recognizer und Windows-Maus.
  - `ShowPinchPreview(factor, x, y)`: setzt `TileLayer.Scale` (begrenzt) und `AnchorX`/`AnchorY` ohne Neuzeichnen.
- **Geänderte Methoden:**
  - `OnPinchUpdated`: Ersetzt die Auswertung bei `Completed` mit den Schwellen 1,3 / 0,77. Neu: `Started` startet den Tracker, `Running` aktualisiert den Tracker und die Vorschau, `Completed` berechnet Schrittzahl, setzt `ScaleAtPoint`, beendet den Tracker, zeichnet neu und löst `ViewportChanged` nur bei geänderter Stufe aus. Schwellen entfallen.
  - `OnPanUpdated`: Früher Ausstieg, solange `_pinch.IsActive`.
  - `Render`: setzt `TileLayer.Scale` auf 1 und den Anker auf die Mitte zurück, bevor Kacheln, Route und Markierungen gezeichnet werden.
- **Neue Events:** keine.
- **Neue Event-Handler:** `OnDoubleTapped` auf `TapGestureRecognizer.Tapped` des `Surface`-Grids (XAML).

### `StationMapView.xaml` (Markup)

- **Neu:** `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` und `Tapped="OnDoubleTapped"` in den `GestureRecognizers` des `Surface`-Grids, neben `PanGestureRecognizer` und `PinchGestureRecognizer`.

### `StationMapView.Pointer.cs` (nur `#if WINDOWS`, nur bei Ergebnis B)

- **Geänderte Methoden:**
  - `OnPointerPressed`: Übergibt bei linker Maustaste Position und Zeitstempel an `MapDoubleTapDetector`; bei Doppeltipp `ZoomAndCenterAt(x, y)`. Das Ziehen und die Zustandsänderung von `_pointer` bleiben unverändert.
  - `OnPointerReleased`: unverändert.
- **Neue Eigenschaften:** `_doubleTap` (`MapDoubleTapDetector`).
- **Neue Event-Handler:** keine.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Pinch-Einzelfaktor (`e.Scale`) | Muss endlich und größer als 0 sein | Wert wird ignoriert (Tracker); keine Stufenänderung |
| Gesamtfaktor (`MapPinchTracker.Factor`) | Umrechnung in Schritte nur bei endlichem Wert größer 0 | `ZoomStepsFor` liefert 0; Ansicht bleibt unverändert, keine `ViewportChanged`-Meldung |
| Zielstufe | Immer auf `MinZoom`..`MaxZoom` begrenzt | Grenze wird gehalten; an der Obergrenze nur Zentrieren bei Doppeltipp |
| Tippposition (`GetPosition(Surface)`) | Muss vorhanden sein | Ohne Position keine Aktion |

## Konfigurationsänderungen

Keine. Die Gesten sind feste Bedienkonventionen (ADR 0005). Die Erkennungsschwellen für Ergebnis B (500 ms, 4 Pixel) sind Konstanten in `MapDoubleTapDetector`, keine Konfiguration.

## Seiteneffekte und Risiken

- **Markierungen (`Button`):** Ein Doppeltipp auf eine Tankstellenmarkierung öffnet zuerst die Detailansicht (wie der einfache Tipp). Der Doppeltipp hat dort keinen eigenen Effekt (Empfehlung aus Offene Punkte 1). Der Test `MapE2ETests_Markers.TapMarker_OpensDetailsAndBackReturnsToMap` bleibt gültig.
- **Windows-Zeigerbehandlung:** `e.Handled = true` in `OnPointerPressed` kann den `TapGestureRecognizer` für Maus unterdrücken (Ergebnis B) oder nicht (Ergebnis A). Der Prototyp entscheidet; eine doppelte Auslösung (Recognizer und Detektor) muss in jedem Fall verhindert werden.
- **Scale-Semantik:** Die MAUI-Dokumentation beschreibt `Scale` als Wert seit dem letzten Update. Wenn sich auf dem Gerät ein kumulativer Wert zeigt, würde die Multiplikation den Zoom überhöhen. Mitigation: Die Auswertung sitzt nur in `MapPinchTracker`; eine Umstellung ist dort lokal möglich und durch die Unit-Tests abgedeckt.
- **Einheit von `ScaleOrigin`:** Ist der Wert absolut statt relativ, müssen `ShowPinchPreview` und `ScaleAtPoint`-Aufruf mit den Pixelwerten der Oberfläche arbeiten (Division durch `Surface.Width`/`Height` nur, wenn relativ). Die Umrechnung steht in einer Stelle (`OnPinchUpdated`).
- **Einrasten:** Der Faktor der Vorschau weicht vom gerasteten Faktor 2^Schritte ab; beim Einrasten springen Kacheln um höchstens den Faktor 2^0,5. Der Fixpunkt bleibt dabei erhalten. Das ist beabsichtigt, sollte aber auf dem Gerät gesichtet werden.
- **Wischen und Kneifen auf Touch-Geräten:** Die Sperre in `OnPanUpdated` verhindert Verschiebungen während der Kneifgeste; ein Wisch, der vor der Kneifgeste beginnt, wird normal beendet.
- **Nachladen-Schaltfläche (`OnViewportChanged`, ADR 0005 / ADR 0014):** Wird über `ViewportChanged` gespeist; bleibt unverändert, erscheint jetzt auch nach Doppeltipp und Kneifen mit Stufenwechsel.
- **Einpassen durch Suchergebnisse:** `FitToContent` und `Render` können während einer Geste aufgerufen werden; die Vorschau wird durch das Zurücksetzen in `Render` beendet (Schutz, siehe Designentscheidungen).
- **Mausrad, Zoom-Schaltflächen, Zentrieren, Verschieben:** Unverändert; ihr Verhalten wird durch bestehende Tests (`ZoomBy_KeepsCenterAndStopsAtLimits`, `MapE2ETests_Navigation`) abgedeckt.
- **Barrierefreiheit:** Die Gesten sind Zusätze; Zoom und Verschieben über Schaltflächen, Tastatur und Sprachausgabe bleiben erhalten.

## Umsetzungsreihenfolge

1. **Windows-Prototyp (Gate): Doppelklick, Scale-Semantik und `ScaleOrigin` prüfen**
   - Voraussetzungen: Build der Lösung grün; lauffähige Windows-App (`run`-Skill); für die Scale-Prüfung ein Touch-Gerät oder Touch-Simulation. Ohne Touch-Gerät: Prüfung über MAUI-Dokumentation und Quelltext des Pakets, Annahme wird als Risiko im Testnachweis vermerkt.
   - Beschreibung: Temporären Prototyp (wird verworfen) bauen, der `TapGestureRecognizer` mit `NumberOfTapsRequired=2` am `Surface` setzt und mit Maus-Doppelklick testet, während `OnPointerPressed` `Handled=true` setzt. Zusätzlich `e.Scale` und `e.ScaleOrigin` bei Kneifgesten protokollieren. Ergebnis (A oder B, Scale-Semantik, Einheit von `ScaleOrigin`) in den Designentscheidungen und in `tasks.md` festhalten. Ergebnis bestimmt, ob Schritt 5 und 8 umgesetzt werden.

2. **`MapViewport` erweitern: `ZoomStepsFor`, `ScaleAtPoint`, `ZoomAndCenterAt`**
   - Voraussetzungen: Keine (bestehende Klasse `MapViewport` mit privaten Weltkoordinaten).
   - Beschreibung: Drei neue öffentliche Methoden gemäß Abschnitt „Änderungen an bestehenden Klassen“. Keine Änderung an `ZoomBy`, `ToLatLon`, `Create`, `PanByPixels`.

3. **Unit-Tests für die `MapViewport`-Erweiterungen**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Neue Testklasse `MapViewportTests_Gestures` (Datei `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Gestures.cs`), siehe Abschnitt Tests.

4. **`MapPinchTracker` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue Klasse in `src/Tankradar.MAUI/Services/Map/MapPinchTracker.cs` mit `IsActive`, `Factor`, `OriginX`, `OriginY`, `Start`, `Update`, `End`. Muster wie `MapPointerTracker` (Klasse ohne Abhängigkeit zur Oberfläche).

5. **Bedingt (nur bei Gate-Ergebnis B): `MapDoubleTapDetector` anlegen**
   - Voraussetzungen: Schritt 1 (Ergebnis B).
   - Beschreibung: Neue Klasse in `src/Tankradar.MAUI/Services/Map/MapDoubleTapDetector.cs` mit `Register(x, y, timestampMs)`, Schwellen 500 ms und 4 Pixel, Zurücksetzen nach Doppeltipp.

6. **Unit-Tests für `MapPinchTracker` und ggf. `MapDoubleTapDetector`**
   - Voraussetzungen: Schritt 4; bei Ergebnis B zusätzlich Schritt 5.
   - Beschreibung: Testklassen `MapPinchTrackerTests_Factor` (und bei B `MapDoubleTapDetectorTests_Timing`) in `src/Tankradar.Tests.Unit/Unit/`.

7. **`StationMapView`: Pinch-Auswertung umbauen**
   - Voraussetzungen: Schritte 2 und 4; Gate-Ergebnis zur Scale-Semantik und zur Einheit von `ScaleOrigin` (Schritt 1).
   - Beschreibung: Feld `_pinchScale` durch `_pinch` ersetzen; `OnPinchUpdated` gemäß Programmablauf „Zwei-Finger-Zoom“; `ShowPinchPreview`; `OnPanUpdated` mit Sperre; `Render` setzt `TileLayer`-Skalierung zurück.

8. **`StationMapView`: Doppeltipp-Handler und XAML**
   - Voraussetzungen: Schritt 2; bei Ergebnis A genügt das (Gate). Bei Ergebnis B wird der Recognizer nicht für Maus wirksam, sondern nur für Touch; die Zeile bleibt trotzdem.
   - Beschreibung: `TapGestureRecognizer` in `StationMapView.xaml`; `OnDoubleTapped` und `ZoomAndCenterAt` in `StationMapView.xaml.cs`.

9. **Bedingt (nur bei Gate-Ergebnis B): Windows-Doppelklick im Zeigerteil**
   - Voraussetzungen: Schritte 1 (Ergebnis B), 5 und 8.
   - Beschreibung: In `StationMapView.Pointer.cs` `MapDoubleTapDetector` einbinden; bei Doppelklick `ZoomAndCenterAt`. Prüfen, dass der Recognizer für Maus nicht zusätzlich auslöst (manuell im Schritt 11).

10. **Dokumentation: Hilfe und ADR 0005**
    - Voraussetzungen: Schritte 7 und 8 (Verhalten ist festgelegt).
    - Beschreibung: `docs/help/Suche/beschreibung.md` Zeile „Zoomen“ (Abschnitt Kartenansicht) um Doppeltipp/Doppelklick und Kneifgeste ergänzen; `docs/help/Suche/ablauf-anwender.md` Zeile „Mit +/− zoomen“ entsprechend; `docs/adr/0005-map-view-decisions.md`, Abschnitt „Abweichungen vom Designentwurf“, anpassen (Kneifgeste verläuft fließend, rastet ein; Schaltflächen und Mausrad unverändert) und Ergänzung um Doppeltipp.

11. **Manueller Nachweis unter Windows (Gesten)**
    - Voraussetzungen: Schritte 7, 8, ggf. 9; lauffähige App (`run`-Skill).
    - Beschreibung: Doppelklick auf die Karte: Mitte am Klickpunkt, Stufe +1, bei Stufe 18 nur Zentrieren; Nachladen-Schaltfläche erscheint. Kneifgeste (Touch-Gerät): fließende Vorschau, Einrasten, Fixpunkt. Ergebnis als Screenshot-Nachweis im Testnachweis festhalten (keine Datei im Repo).

12. **Regression: Unit, Integration, E2E**
    - Voraussetzungen: Alle vorherigen Schritte.
    - Beschreibung: Unit- und Integrationsläufe vollständig; E2E-Teilmenge `MapE2ETests_*` (inkl. `E2EInputPolicyTests_NoFocus` in Unit) ausführen. Ergebnis in `tasks.md` eintragen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ZoomStepsFor_RoundsFactorToWholeLevels` (Theory) | `MapViewportTests_Gestures` | 1 und 1,4 liefern 0; 1,5 und 2 liefern 1; 4 liefert 2; 0,5 und 0,7 liefern -1 |
| `ZoomStepsFor_InvalidFactor_IsNoChange` (Theory) | `MapViewportTests_Gestures` | 0, negative Werte, NaN und unendlich liefern 0 |
| `ScaleAtPoint_IntegerFactor_KeepsGeoPointUnderOriginFixed` | `MapViewportTests_Gestures` | Geografischer Punkt unter (x, y) liegt nach Zoom auf derselben Pixelposition (Toleranz 0,5 Pixel) |
| `ScaleAtPoint_NonIntegerFactor_SnapsAndKeepsOrigin` | `MapViewportTests_Gestures` | Faktor 1,6 rastet auf Stufe +1, Fixpunkt bleibt |
| `ScaleAtPoint_ZoomOut_KeepsOrigin` | `MapViewportTests_Gestures` | Faktor 0,5 verkleinert um eine Stufe, Fixpunkt bleibt |
| `ScaleAtPoint_ClampsAtMinAndMaxZoom` | `MapViewportTests_Gestures` | Stufe 18 mit Faktor 4 bleibt 18; Stufe 4 mit Faktor 0,25 bleibt 4 |
| `ScaleAtPoint_NoStepChange_ReturnsSameInstance` | `MapViewportTests_Gestures` | Faktor 1 und Stufe-Grenze liefern `Assert.Same` (keine Änderung) |
| `ScaleAtPoint_NearAntimeridian_WrapsCenterLongitude` | `MapViewportTests_Gestures` | Zoom nahe ±180 Grad: Fixpunkt bleibt, Mittellänge liegt in -180..180 |
| `ZoomAndCenterAt_CentersOnTapAndZoomsOneLevel` | `MapViewportTests_Gestures` | Neue Mitte entspricht `ToLatLon(x, y)` des alten Ausschnitts; Stufe +1 |
| `ZoomAndCenterAt_AtMaxZoom_OnlyCenters` | `MapViewportTests_Gestures` | Stufe bleibt 18, Mitte liegt am Tippunkt (neues Objekt, `Assert.NotSame` zur Ausgangslage zulässig) |
| `ZoomAndCenterAt_AtCornersAndEdges_StaysInProjection` | `MapViewportTests_Gestures` | Tipp an (0,0) und (Breite, Höhe): Breitengrad innerhalb `MaxLatitude` |
| `Update_MultipliesIncrementalFactors` | `MapPinchTrackerTests_Factor` | Einzelfaktoren 0,9, 1,1, 1,2 ergeben Gesamtfaktor 1,188 |
| `Update_BeforeStart_IsIgnored` | `MapPinchTrackerTests_Factor` | Ohne `Start` bleibt der Faktor 1 |
| `Update_InvalidScale_IsIgnored` (Theory) | `MapPinchTrackerTests_Factor` | 0, negativ, NaN werden nicht multipliziert |
| `Origin_IsLastReportedOrigin` | `MapPinchTrackerTests_Factor` | Gestenmittelpunkt entspricht dem letzten `Update` |
| `End_ResetsAndDeactivates` | `MapPinchTrackerTests_Factor` | Nach `End` ist `IsActive` false, Faktor 1 |
| `Register_SecondTapWithinWindowAndDistance_IsDoubleTap` (nur Ergebnis B) | `MapDoubleTapDetectorTests_Timing` | Zwei Tipps innerhalb 500 ms und 4 Pixel ergeben `true` |
| `Register_SecondTapAfterWindow_IsSingle` (nur Ergebnis B) | `MapDoubleTapDetectorTests_Timing` | Zeitfenster überschritten: `false` |
| `Register_SecondTapFarAway_IsSingle` (nur Ergebnis B) | `MapDoubleTapDetectorTests_Timing` | Abstand über 4 Pixel: `false` |
| `Register_ThirdTapAfterDouble_StartsNewSequence` (nur Ergebnis B) | `MapDoubleTapDetectorTests_Timing` | Nach Doppeltipp ist der dritte Tipp ein Einzeltipp |

Hinweis zu Dateien: `MapViewportTests_Gestures` und `MapPinchTrackerTests_Factor` sind jeweils eigene Dateien nach Thema (Konvention aus `test-class-structure`). Der bestehende Test `MapViewportTests_Math` bleibt unberührt.

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine Anpassung nötig | `ZoomBy`, `ToLatLon`, `PanByPixels`, `Create` und `Fit` bleiben unverändert; `MapViewportTests_Math`, `MapViewportTests_Bounds`, `MapViewportTests_RouteBounds`, `MapPointerTrackerTests_Mouse` und `MapViewModelTests_AreaReload` bleiben gültig |

Die Unit-Tests für `E2EInputPolicyTests_NoFocus` müssen grün bleiben; im E2E-Projekt wird kein neuer Code mit verbotenen Aufrufen hinzugefügt.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung verlangt einen E2E-Test für den Doppelklick. Die Regel `E2EInputPolicyTests_NoFocus` verbietet in E2E-Quellen `.Click(`, `.DoubleClick(`, `Mouse.` und `.Drag*(`. Ein Doppelklick oder eine Kneifgeste über UI Automation ist in diesem Rahmen nicht zulässig, und die UI-Automation-Muster bieten für Doppelklick oder Kneifen keinen Aufruf. Der Gestenpfad ist deshalb nicht über den E2E-Weg prüfbar. Die Gestenprüfung erfolgt über Unit-Tests der Rechenlogik und Erkennung (Schritte 3, 6), den manuellen Nachweis unter Windows (Schritt 11) und die Regression bestehender E2E-Tests (Schritt 12). Eine Ausnahme von der Policy ist eine Entscheidung des Projekts (Offene Punkte 9).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht (Regression) | Zoom über Schaltfläche bleibt in den Grenzen 4 bis 18 | `MapE2ETests_Navigation.Zoom_ChangesLevelWithinLimits` | Bestehende Bedienung über Schaltflächen bleibt erhalten | Schützt den gemeinsamen Pfad `Zoom`, `Render`, `RaiseViewportChanged` vor der Änderung |
| Pflicht (Regression) | Nachladen-Schaltfläche erscheint nach Verschieben oder Zoomen | `MapE2ETests_AreaReload.PanOrZoom_ShowsReloadButtonWithoutQuery` | `ViewportChanged`-Verhalten bleibt unverändert | Schützt die Ereigniskette bis zur Oberfläche |
| Pflicht (Regression) | Markierung antippen öffnet Details und kehrt zur Karte zurück | `MapE2ETests_Markers.TapMarker_OpensDetailsAndBackReturnsToMap` | Einfacher Tipp auf Markierung bleibt unverändert | Relevant für Offene Punkte 1 (Doppeltipp auf Markierung) |
| Nicht planbar (Policy) | Doppelklick zentriert und zoomt eine Stufe | Keine neue E2E-Datei | Akzeptanzkriterium Doppeltipp | Die E2E-Policy verbietet die dafür nötigen Mausaufrufe; Abdeckung über Unit-Tests und den manuellen Nachweis (Schritt 11) |
| Nicht planbar (Policy) | Kneifgeste steuert den Zoom fließend | Keine neue E2E-Datei | Akzeptanzkriterium Zwei-Finger-Zoom | Kneifen ist über UI Automation nicht auslösbar; Abdeckung über Unit-Tests und den manuellen Nachweis auf einem Touch-Gerät |

Bestehende E2E-Tests, die angepasst werden müssen: Keine. Die bestehenden Tests bedienen die Karte über Schaltflächen und bleiben gültig.

## Offene Punkte

Die folgenden Punkte stammen aus der Anforderung. Es lagen in diesem Durchlauf keine Antworten vor. Die Empfehlungen sind in die Planung eingeflossen, soweit angegeben.

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Doppeltipp auf eine Tankstellenmarkierung: sofort Details öffnen oder Einzeltipp verzögern? | a): Der erste Tipp öffnet weiterhin sofort die Detailansicht; der Doppeltipp hat dort keinen eigenen Effekt. Der Plan übernimmt das. |
| 2 | Doppeltipp-Zeit und iOS | Windows-Maus: feste Werte 500 ms und 4 Pixel (nur bei Gate-Ergebnis B). Touch und iOS/Android: `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` aus Schritt 8 erfasst alle Plattformen, daher ja. |
| 3 | Semantik von `PinchGestureUpdatedEventArgs.Scale` (inkrementell oder kumulativ, je Plattform) | Planannahme: inkrementell laut MAUI-Dokumentation. Im Gate (Schritt 1) auf Windows und, falls verfügbar, auf einem Touch-Gerät prüfen. Bei Gegenbefund nur `MapPinchTracker.Update` umstellen. |
| 4 | Einheit von `ScaleOrigin` (relativ 0 bis 1 oder absolut) | Im Gate (Schritt 1) protokollieren. Planannahme: relativ, Umrechnung bei Abweichung zentral in `OnPinchUpdated`. Kein Vorschlag ohne Prüfung möglich. |
| 5 | Animation des Doppeltipp-Zooms | Sofort springen (Annahme der Anforderung beibehalten). |
| 6 | Kleine Geste, die auf dieselbe Stufe rundet | Ansicht bleibt unverändert, Vorschau wird zurückgesetzt, keine `ViewportChanged`-Meldung. |
| 7 | Doppeltipp bei Stufe 18 | Zentrieren ohne Zoom; `ViewportChanged` wird ausgelöst, weil es eine Nutzeraktion ist. |
| 8 | Neuer ADR oder Ergänzung von ADR 0005 | Ergänzung von ADR 0005 im Abschnitt „Abweichungen vom Designentwurf“ und bei der Bedienung, kein neuer ADR. |
| 9 | Keine E2E-Abdeckung der Gesten wegen `E2EInputPolicyTests_NoFocus` (Anforderung verlangt einen E2E-Test für den Doppelklick) | Keine Ausnahme von der Policy. Abdeckung über Unit-Tests der Logik und Erkennung plus manueller Windows-Nachweis (Schritt 11), dokumentiert im Testnachweis. Die Entscheidung über eine Ausnahme liegt beim Projekt; ohne Entscheidung gilt die Empfehlung. |
| 10 | Wischen während der Kneifgeste | Vorschlag wurde als Designentscheidung übernommen: Wischen wird während aktiver Kneifgeste ignoriert. Ein Gegenvorschlag des Projekts ist möglich, aber nicht erforderlich. |
