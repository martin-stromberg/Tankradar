# Logik

## `MapViewport`
Datei: `src/Tankradar.MAUI/Services/Map/MapViewport.cs`

Unveränderlicher Kartenausschnitt (Web-Mercator, 256-px-Kacheln). Konstanten: `TileSize = 256`, `MinZoom = 4`, `MaxZoom = 18`, `MaxLatitude = 85.0511287798066`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Create(lat, lon, zoom, width, height)` | public static | Erstellt Ausschnitt um Position; Zoom wird auf 4..18 begrenzt |
| `Fit(points, width, height, padding, maxZoom)` | public static | Passt Ausschnitt auf Punkte mit Rand an; leere Liste: Mitteleuropa |
| `Resize(width, height)` | public | Gleiche Mitte und Stufe, andere Größe |
| `PanByPixels(deltaX, deltaY)` | public | Verschiebt die Mitte (Ausschnitt wandert entgegengesetzt); Längengrad umläuft |
| `ZoomBy(delta)` | public | Ändert Stufe um `delta`, **behält die geografische Mitte bei**; gibt `this` zurück, wenn Grenze erreicht |
| `ToScreen(lat, lon)` | public | Bildschirmposition einer Koordinate (kann außerhalb liegen) |
| `Contains(lat, lon, margin)` | public | Prüft Sichtbarkeit mit Rand |
| `VisibleTiles()` | public | Sichtbare Kacheln mit Position; Spalten werden umgebrochen, Zeilen außerhalb der Welt entfallen |
| `ToLatLon(x, y)` | public | Umkehrung von `ToScreen`; Breite wird an `MaxLatitude` begrenzt |
| `GetBounds()` | public | Begrenzungsbox aus vier Eckpunkten |
| `Fits(points, zoom, w, h)` | private static | Prüft, ob Punkte bei Stufe in den verfügbaren Bereich passen |
| `WorldSize(zoom)`, `XOf`, `YOf`, `LongitudeOf`, `LatitudeOf` | private static | Projektionshilfen |

Hinweise für die Anforderung:
- `ToLatLon` ist die Grundlage für eine neue Mitte am Tippunkt. Eine Methode für Zoom um einen Bildschirmpunkt (Fixpunkt) gibt es nicht; `ZoomBy` behält die Mitte, nicht den Punkt unter dem Zeiger.
- Der Konstruktor ist privat; neue Ausschnitte entstehen nur über `Create`, `Fit`, `Resize`, `PanByPixels`, `ZoomBy`.

Abonnierte Events: keine
Publizierte Events: keine

## `StationMapView`
Datei: `src/Tankradar.MAUI/Views/Controls/StationMapView.xaml.cs` (Markup: `StationMapView.xaml`)

Plattformübergreifende Karte. Hält `_viewport` (`MapViewport?`) und `_pinchScale` (`double`, Startwert 1).

Gesten und Eingaben in dieser Klasse:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnPanUpdated(sender, PanUpdatedEventArgs)` | private | Wischen: `PanByPixels` mit Differenz zu `TotalX/TotalY`, `Render()`, `RaiseViewportChanged()` |
| `OnPinchUpdated(sender, PinchGestureUpdatedEventArgs)` | private | `Started`: `_pinchScale = 1`; `Running`: `_pinchScale = e.Scale`; `Completed`: bei `> 1.3` `Zoom(1)`, bei `< 0.77` `Zoom(-1)`, danach `_pinchScale = 1`. `ScaleOrigin` wird nicht ausgewertet. |
| `Zoom(int delta)` | private | `ZoomBy`, `Render()`, `RaiseViewportChanged()` |
| `Pan(fractionX, fractionY)` | private | Verschiebung in Bruchteilen der Größe (Schaltflächen) |
| `PanByPixels(deltaX, deltaY)` | private | Verschiebung in Pixeln (auch von Maus-Ziehen genutzt) |
| `OnZoomInClicked` / `OnZoomOutClicked` | private | `Zoom(1)` / `Zoom(-1)` |
| `OnRecenterClicked` | private | `FitToContent()`, `RaiseViewportChanged()` |
| `OnPanNorthClicked` u. a. | private | `Pan` mit `PanFraction = 0.4` |
| `FitToContent()` | private | Einpassen auf Markierungen, Suchposition und Routen-Begrenzung, `Render()` |
| `FitToResult()` | private | `FitToContent()`, dann `ViewportFitted` |
| `RequestFit()` / `OnResultChanged()` | private | Einpassen verzögert über `Dispatcher` nach neuem Ergebnis (nur bei `MapFitMode.FitToResult`) |
| `OnSurfaceSizeChanged()` | private | Größe übernehmen (`Resize`) oder einpassen |
| `Render()`, `RenderTiles`, `RenderRoute`, `RenderMarkers`, `RenderOrigin`, `RenderDestination` | private | Zeichnen der Ebenen |
| `StartTileLoad` / `LoadTileAsync` | private | Kacheln über `ITileSource` laden (150 ms Verzögerung, Abbruch bei Entfernen) |
| `CreateMarkerButton(marker)` | private | Markierung als `Button`; `Clicked` führt `MarkerCommand` mit der Tankstelle aus |
| `RaiseViewportChanged()` | private | Löst `ViewportChanged` aus |
| `AttachPointerInput()` / `DetachPointerInput()` | partial, private | Verknüpfung der Zeigerereignisse (nur Windows, siehe unten) |

Abonnierte Events (intern, Konstruktor und XAML):
- `Surface.SizeChanged` → `OnSurfaceSizeChanged`
- `Surface.HandlerChanged` → `AttachPointerInput`
- `Surface.Unloaded` → `DetachPointerInput`
- XAML: `PanGestureRecognizer.PanUpdated` → `OnPanUpdated`; `PinchGestureRecognizer.PinchUpdated` → `OnPinchUpdated`
- Schaltflächen-Ereignisse `Clicked` (Zoom, Zentrieren, Verschieben) und `Button.Clicked` je Markierung

Publizierte Events:
- `ViewportChanged` (`EventHandler<ViewportChangedEventArgs>`): nach Benutzerbewegung (Wischen, Kneifen, Mausrad, Schaltflächen, Zentrieren)
- `ViewportFitted`: nach Einpassen auf ein neues Ergebnis

Verwendung: `src/Tankradar.MAUI/Views/MapPage.xaml.cs` abonniert beide Ereignisse (`OnMapViewportChanged` → `MapViewModel.OnViewportChanged`, `OnMapViewportFitted` → `MapViewModel.OnViewportFitted`).

Der Zoom-Pfad der Kneifgeste ist schon mit dem `Zoom(int)`-Pfad der Schaltflächen verbunden. Ein Fixpunkt-Zoom ist nicht vorhanden.

### Streitpunkt: Scale-Semantik

Die MAUI-XML-Dokumentation (`Microsoft.Maui.Controls.xml` im Paket `microsoft.maui.controls.core` 10.0.120) nennt:
- `PinchGestureUpdatedEventArgs.Scale`: „Gets the relative scale of the pinch gesture **since the last update**. Default is 1.“
- `ScaleOrigin`: „the origin point of the pinch gesture, which is the center between the two touch points.“ Eine Einheit steht nicht dabei.

Aus dem Code folgt: `OnPinchUpdated` setzt `_pinchScale = e.Scale` bei jedem `Running`-Ereignis. Bei inkrementellen Werten bleibt davon nur der letzte Schritt übrig, und die Schwellen 1,3 / 0,77 beim `Completed`-Ereignis prüfen dann nicht die Gesamtgeste. Das ist nicht durch einen Lauf belegt. Die Semantik unter Windows ist ungetestet, und der Code enthält dazu keinen Kommentar.

### Streitpunkt: Zeigerereignisse und Gesten unter Windows

`StationMapView.Pointer.cs` (nur `#if WINDOWS`), Klasse `StationMapView` (partial):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AttachPointerInput()` | partial, private | Hängt `PointerPressed`, `PointerMoved`, `PointerReleased`, `PointerCanceled`, `PointerCaptureLost`, `PointerWheelChanged` an `Surface.Handler.PlatformView` |
| `DetachPointerInput()` | partial, private | Löst alle Ereignisse, gibt `_pointer` frei |
| `IsMouse(PointerRoutedEventArgs)` | private static | Prüft `PointerDeviceType == Mouse` |
| `OnPointerPressed` | private | Nur Maus und linke Taste: `_pointer.Press`, `CapturePointer`, **`e.Handled = true`** |
| `OnPointerMoved` | private | Maus gedrückt: `_pointer.Move` → `PanByPixels`, `e.Handled = true`; sonst `Release` |
| `OnPointerReleased` | private | `_pointer.Release`, `ReleasePointerCapture` |
| `OnPointerCaptureLost` | private | `_pointer.Release` |
| `OnPointerWheelChanged` | private | `_pointer.Wheel(...)` → `Zoom(steps)`, `e.Handled = true` |

Befunde:
- Es gibt keine Doppelklick-Erkennung (weder Zeit noch Position). `OnPointerPressed` setzt `e.Handled = true`, sodass der zweite Klick den Handler auslöst, ohne Doppeltipp-Logik.
- Der Kommentar in `StationMapView.xaml.cs` (Summary zu `AttachPointerInput`) behauptet, die Gestenerkenner reagierten unter Windows nur auf Berührung und Stift. Das ist eine Aussage im Code, nicht durch eine Quelle belegt.
- Ob ein `TapGestureRecognizer` Mausklicks erhält, wenn `e.Handled = true` gesetzt ist, ist aus dem Code und der Dokumentation nicht entscheidbar. Die MAUI-Windows-Implementierung liegt nur als kompilierte DLL im NuGet-Cache vor; die Frage bleibt offen und braucht einen Prototyp.

## `MapPointerTracker`
Datei: `src/Tankradar.MAUI/Services/Map/MapPointerTracker.cs`

Plattformunabhängige Übersetzung von Mauseingaben. `WheelNotch = 120`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Press(x, y)` | public | Beginnt Ziehen, merkt Startposition |
| `Move(x, y)` | public | Liefert `ScreenPoint?`-Verschiebung seit letzter Meldung; `null` ohne Ziehen |
| `Release()` | public | Beendet Ziehen |
| `Wheel(delta)` | public | Liefert ganze Zoomstufen; Teilrasten werden summiert |
| `IsDragging` | public | Eigenschaft, ob gezogen wird |

Abonnierte Events: keine. Publizierte Events: keine.
Aufgerufen von: `StationMapView.Pointer.cs` (Maus-Handler).
