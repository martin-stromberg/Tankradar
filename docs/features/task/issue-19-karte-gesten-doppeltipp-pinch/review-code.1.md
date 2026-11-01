# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basis: `origin/staging`, Merge-Base `4fee2a3`. Geprüft wurden die uncommitteten Änderungen im Arbeitsverzeichnis (der Branch hat außer dem Plan-Commit keine eigenen Code-Commits).

Geprüfte Schwerpunkte ohne Befund:

- **Fixpunkt-Mathematik `ScaleAtPoint`:** Der Weltpunkt unter `(x, y)` ist `_center + offset`. Er wird mit `2^(target - Zoom)` skaliert, und davon wird `offset` wieder abgezogen. Damit bleibt er an derselben Bildschirmstelle stehen. Die Skalierung nutzt die *begrenzte* Zielstufe, also bleibt der Fixpunkt auch an `MinZoom`/`MaxZoom` erhalten. Die Länge wird umgebrochen. Die Breite begrenzt der Konstruktor auf `[0, WorldSize]`. Liegt der Fixpunkt außerhalb der Weltkarte, geht er dadurch verloren; das ist akzeptabel.
- **Rundung `ZoomStepsFor`:** Gerundeter Zweierlogarithmus, `AwayFromZero`. Die Schwellen liegen bei √2 ≈ 1,414 und 1/√2 ≈ 0,707. Ungültige Faktoren ergeben 0.
- **`ZoomAndCenterAt`:** Das Begrenzen der Breite vor dem Skalieren ist gleichwertig zum Begrenzen danach. An `MaxZoom` wird nur zentriert, die Methode liefert eine neue Instanz, und `ViewportChanged` wird ausgelöst.
- **Zustandsautomat der Kneifgeste:** Started → Start, Running → Update + Vorschau, Completed → Einrasten + Neuzeichnen (+ Ereignis nur bei Stufenwechsel), Canceled → End + Neuzeichnen. `Render` setzt die Vorschau immer zurück. `_viewport` wird nach der ersten Zuweisung nie wieder `null`, daher bleibt der Tracker durch den frühen Rücksprung nicht hängen.
- **Pan während der Kneifgeste:** Die Pan-Updates werden verschluckt, `_lastPanX/Y` aber mitgeführt. Deshalb springt die Karte nach dem Loslassen eines Fingers nicht. Ein Pan vor der Kneifgeste hat `ViewportChanged` bereits bei jedem Running ausgelöst.
- **Doppeltes Zoomen Maus/Recognizer (Windows):** Für Mausdrücke auf `Surface` greift `SuppressDoubleTap`. Berührung und Stift laufen nur über den Recognizer. Der Zeitstempel (`PointerPoint.Timestamp`, µs) wird korrekt in ms umgerechnet.

## Befunde

### StationMapView.xaml / StationMapView.xaml.cs (StationMapView)

- **Fehlerbehandlung / Vorbedingung** — Der `TapGestureRecognizer NumberOfTapsRequired="2"` hängt am `Surface`-Grid. Dieses Grid enthält auch die Bedienschaltflächen oben rechts: Zoom `+`/`−`, `◎` und die vier Verschiebe-Schaltflächen (`StationMapView.xaml`, Zeilen 51–95). `OnDoubleTapped` prüft nicht, wo der Doppeltipp liegt. Tippt jemand zweimal schnell auf `+` (eine naheliegende Bedienung), kann zusätzlich zu den zwei `Zoom(1)` ein `ZoomAndCenterAt` auf die Lage der Schaltfläche ausgelöst werden. Unter iOS blockiert `UIControl` nur Einfach-Tipp-Recognizer der Elternansicht. Unter Windows bubbelt `DoubleTapped` zum `Surface`. Hinzu kommt unter Windows: Buttons markieren `PointerPressed` als behandelt, also läuft `OnPointerPressed` für Drücke auf Schaltflächen nicht. `_lastPressWasMouse` bleibt dann auf dem alten Wert (anfangs `false`), und ein Maus-Doppelklick auf eine Schaltfläche wird nicht unterdrückt. Auf dem Gerät ist zu prüfen, ob WinUI-`ButtonBase` `DoubleTapped` behandelt. Der Code verlässt sich darauf bisher nicht.

  Empfehlung: In `OnDoubleTapped` Doppeltipps ignorieren, deren Position in den Grenzen des Bedienbereichs liegt. Dafür dem äußeren `VerticalStackLayout` (Zeile 51) einen Namen geben, z. B. `x:Name="ControlsPanel"`, und `ControlsPanel.Bounds.Contains(position)` prüfen; analog ggf. für `MarkerLayer`-Buttons. Alternativ den Recognizer auf eine eigene, vollflächige Ebene unterhalb von `MarkerLayer` und Bedienelementen legen. Zusätzlich in `StationMapView.Pointer.cs` `_lastPressWasMouse` über `target.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(...), handledEventsToo: true)` setzen, damit der Wert auch bei Drücken auf Kindelemente stimmt. Das Abhängen in `DetachPointerInput` mit `RemoveHandler` ergänzen.

- **Zustandsautomat / Interaktion mit `ViewportChanged`** — `FitToContent` (neues Suchergebnis über `RequestFit`, Zentrieren-Schaltfläche) und `OnSizeChanged`/`Resize` können während einer laufenden Kneifgeste den Ausschnitt ersetzen. `Render` setzt dabei zwar die Vorschau zurück, `_pinch` bleibt aber aktiv. Beim folgenden `Completed` wendet `CompletePinch` den Gesamtfaktor auf den *neu eingepassten* Ausschnitt an, den der Anwender so nie in der Vorschau gesehen hat. Außerdem löst die Methode `ViewportChanged` aus. Direkt nach einer neuen Suche erscheint dann die Nachlade-Schaltfläche, obwohl das Einpassen sie bewusst nicht auslösen soll (Dokumentation von `ViewportChanged`).

  Empfehlung: In `FitToContent` vor dem Neuberechnen `_pinch.End()` aufrufen. Ein anschließendes `Completed` endet dann über `if (!_pinch.IsActive) return;` ohne Wirkung. Optional ebenso bei einer Größenänderung (`OnSizeChanged`), weil sich dort `Width`/`Height` und damit der Bezug des Gestenmittelpunkts ändern.

- **Überflüssiger Code** — `OnPinchUpdated`, Fall `Started`: `_pinch.End()` direkt vor `_pinch.Start(...)` ist wirkungslos, weil `Start` `IsActive`, `Factor` und Mittelpunkt vollständig neu setzt. `CompletePinch`: Die Bedingung `MapViewport.ZoomStepsFor(factor) != 0 ? viewport.ScaleAtPoint(...) : viewport` doppelt die Prüfung, die `ScaleAtPoint` selbst schon macht (gibt `this` zurück, wenn sich die Stufe nicht ändert).

  Empfehlung: `_pinch.End()` im `Started`-Zweig entfernen. In `CompletePinch` direkt `_viewport = viewport.ScaleAtPoint(factor, originX, originY);` zuweisen.

### MapViewport.cs (MapViewport)

- **Doppelter Code** — Der Umbruch der Länge `((x % world) + world) % world` steht jetzt dreimal: `PanByPixels`, `ScaleAtPoint`, `ZoomAndCenterAt`. Die Umrechnung Bildschirm → Welt (`_centerX + (x - (Width / 2))` bzw. für Y) steht in `ToLatLon`, `ScaleAtPoint` und `ZoomAndCenterAt`.

  Empfehlung: Eine private statische Hilfsmethode `WrapX(double x, int zoom)` einführen und in den drei Methoden verwenden. Optional eine private Methode `ToWorld(double x, double y)` für den Weltpunkt unter einer Bildschirmlage, die `ToLatLon`, `ScaleAtPoint` und `ZoomAndCenterAt` gemeinsam nutzen.

### MapViewportTests_Gestures.cs (MapViewportTests_Gestures)

- **Testqualität (mehrere Fälle je Test)** — `ScaleAtPoint_ClampsAtMinAndMaxZoom` prüft Ober- und Untergrenze in einer Methode. `ScaleAtPoint_NoStepChange_ReturnsSameInstance` prüft drei Fälle: Faktor 1, Faktor 1,3 unter der Rundungsschwelle und Grenze erreicht. `ZoomAndCenterAt_AtCornersAndEdges_StaysInProjection` prüft zwei Ecken mit je zwei Aspekten.

  Empfehlung: Aufteilen in `ScaleAtPoint_AtMaxZoom_StaysAtMax` / `ScaleAtPoint_AtMinZoom_StaysAtMin` sowie `ScaleAtPoint_FactorBelowRoundingThreshold_ReturnsSameInstance` (als `[Theory]` mit 1,0 und 1,3) und `ScaleAtPoint_AtLimit_ReturnsSameInstance`. Den Eckentest als `[Theory]` über die Eckposition schreiben.

- **Testabdeckung** — Es fehlt ein Test, dass der Fixpunkt erhalten bleibt, wenn die Zielstufe begrenzt wird. Beispiel: Stufe 17, Faktor 8 → Stufe 18, effektiver Faktor 2. Genau dieser Pfad (`scale` aus der begrenzten Zielstufe) ist die heikelste Stelle der Fixpunkt-Mathematik.

  Empfehlung: Test `ScaleAtPoint_FactorBeyondMaxZoom_ClampsAndKeepsOrigin` ergänzen (`Create(..., 17, ...)`, `ScaleAtPoint(8, 200, 450)`, Zoom 18 und `AssertFixedPoint`), analog für `MinZoom`.

### MapDoubleTapDetectorTests_Timing.cs / MapPinchTrackerTests_Factor.cs

- **Testabdeckung** — Ungetestet sind zwei Zweige. In `MapDoubleTapDetector.Register` fehlt der Zweig für einen rückläufigen Zeitstempel (`timestampMs - _previousTimestampMs >= 0`). In `MapPinchTracker.Start` fehlt das Zurücksetzen von Faktor und Mittelpunkt bei erneutem Start während einer laufenden Geste.

  Empfehlung: `Register_SecondTapWithEarlierTimestamp_IsSingle` sowie `Start_WhileActive_ResetsFactorAndOrigin` ergänzen.

## Hinweis außerhalb des Umfangs

Im Arbeitsverzeichnis ist zusätzlich `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookStatisticsE2ETests_Hybrid.cs` geändert: Die Testdaten werden in den laufenden Monat gelegt. Die Änderung gehört nicht zu Issue #19 und war im Git-Status zu Beginn des Auftrags nicht enthalten. Ein technischer Fehler ist darin nicht erkennbar. Vor dem Commit klären, ob sie in diesen Branch gehört.

## Geprüfte Dateien

- `src/Tankradar.MAUI/Services/Map/MapViewport.cs`
- `src/Tankradar.MAUI/Services/Map/MapPinchTracker.cs`
- `src/Tankradar.MAUI/Services/Map/MapDoubleTapDetector.cs`
- `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml`
- `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml.cs`
- `src/Tankradar.MAUI/Views/Controls/StationMapView.Pointer.cs`
- `src/Tankradar.Tests.Unit/Unit/MapViewportTests_Gestures.cs`
- `src/Tankradar.Tests.Unit/Unit/MapPinchTrackerTests_Factor.cs`
- `src/Tankradar.Tests.Unit/Unit/MapDoubleTapDetectorTests_Timing.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookStatisticsE2ETests_Hybrid.cs` (siehe Hinweis)
