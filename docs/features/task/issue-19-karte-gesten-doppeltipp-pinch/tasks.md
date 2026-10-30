# Tasks: Doppeltipp-Zentrierung und fließender Zwei-Finger-Zoom auf der Kartenansicht

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Prototyp (Gate) | Maus-Doppelklick mit `TapGestureRecognizer` (`NumberOfTapsRequired=2`) bei `Handled=true` prüfen, Ergebnis A oder B festhalten | Offen | — |
| 2 | Prototyp (Gate) | `PinchGestureUpdatedEventArgs.Scale` (inkrementell oder kumulativ) auf Windows bzw. Touch-Gerät protokollieren | Offen | — |
| 3 | Prototyp (Gate) | Einheit von `ScaleOrigin` (relativ oder absolut) protokollieren | Offen | — |
| 4 | Logik | `MapViewport.ZoomStepsFor(factor)` (statisch, round(log2), ungültige Faktoren 0) anlegen | Offen | — |
| 5 | Logik | `MapViewport.ScaleAtPoint(factor, x, y)` (Fixpunkt in Weltkoordinaten, Begrenzung, `this` bei keiner Änderung) anlegen | Offen | — |
| 6 | Logik | `MapViewport.ZoomAndCenterAt(delta, x, y)` (Mitte auf Tippunkt, Begrenzung, auch an Grenze neues Objekt) anlegen | Offen | — |
| 7 | Logik | Neue Klasse `MapPinchTracker` (`IsActive`, `Factor`, `OriginX`, `OriginY`, `Start`, `Update`, `End`) in `Services/Map` anlegen | Offen | — |
| 8 | Logik (bedingt B) | Neue Klasse `MapDoubleTapDetector` (`Register`, 500 ms, 4 Pixel, Zurücksetzen nach Doppeltipp) in `Services/Map` anlegen | Offen | — |
| 9 | Tests | Testklasse `MapViewportTests_Gestures`: `ZoomStepsFor` (Theory) | Offen | — |
| 10 | Tests | Testklasse `MapViewportTests_Gestures`: `ZoomStepsFor` ungültige Faktoren (Theory) | Offen | — |
| 11 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` Fixpunkt bei ganzzahligem Faktor | Offen | — |
| 12 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` Rastung und Fixpunkt bei nicht ganzzahligem Faktor | Offen | — |
| 13 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` Herauszoomen mit Fixpunkt | Offen | — |
| 14 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` Begrenzung an `MinZoom` und `MaxZoom` | Offen | — |
| 15 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` ohne Stufenänderung liefert dieselbe Instanz | Offen | — |
| 16 | Tests | Testklasse `MapViewportTests_Gestures`: `ScaleAtPoint` nahe Antimeridian (Längenumbruch) | Offen | — |
| 17 | Tests | Testklasse `MapViewportTests_Gestures`: `ZoomAndCenterAt` zentriert auf Tippunkt und zoomt eine Stufe | Offen | — |
| 18 | Tests | Testklasse `MapViewportTests_Gestures`: `ZoomAndCenterAt` an `MaxZoom` nur zentrieren | Offen | — |
| 19 | Tests | Testklasse `MapViewportTests_Gestures`: `ZoomAndCenterAt` an Ecken und Rändern innerhalb der Projektion | Offen | — |
| 20 | Tests | Testklasse `MapPinchTrackerTests_Factor`: Multiplikation der Einzelfaktoren | Offen | — |
| 21 | Tests | Testklasse `MapPinchTrackerTests_Factor`: `Update` vor `Start` wird ignoriert | Offen | — |
| 22 | Tests | Testklasse `MapPinchTrackerTests_Factor`: ungültige Einzelfaktoren (Theory) werden ignoriert | Offen | — |
| 23 | Tests | Testklasse `MapPinchTrackerTests_Factor`: Gestenmittelpunkt ist der letzte gemeldete | Offen | — |
| 24 | Tests | Testklasse `MapPinchTrackerTests_Factor`: `End` deaktiviert und setzt Faktor auf 1 zurück | Offen | — |
| 25 | Tests (bedingt B) | Testklasse `MapDoubleTapDetectorTests_Timing`: Doppeltipp innerhalb Zeit und Abstand | Offen | — |
| 26 | Tests (bedingt B) | Testklasse `MapDoubleTapDetectorTests_Timing`: Zeitfenster überschritten ergibt Einzeltipp | Offen | — |
| 27 | Tests (bedingt B) | Testklasse `MapDoubleTapDetectorTests_Timing`: Abstand über 4 Pixel ergibt Einzeltipp | Offen | — |
| 28 | Tests (bedingt B) | Testklasse `MapDoubleTapDetectorTests_Timing`: dritter Tipp nach Doppeltipp beginnt neue Folge | Offen | — |
| 29 | UI | `StationMapView`: Feld `_pinch` (`MapPinchTracker`) einführen, `_pinchScale` entfernen | Offen | — |
| 30 | UI | `StationMapView.OnPinchUpdated`: Started/Running/Completed gemäß Ablauf, Schwellen 1,3 / 0,77 entfernen, `ViewportChanged` nur bei Stufenwechsel | Offen | — |
| 31 | UI | `StationMapView.ShowPinchPreview`: `TileLayer.Scale` begrenzt, `AnchorX`/`AnchorY` am Gestenmittelpunkt, kein Neuzeichnen | Offen | — |
| 32 | UI | `StationMapView.Render`: `TileLayer`-Skalierung und Anker zurücksetzen | Offen | — |
| 33 | UI | `StationMapView.OnPanUpdated`: früher Ausstieg während aktiver Kneifgeste | Offen | — |
| 34 | UI | `StationMapView.ZoomAndCenterAt(x, y)` und `OnDoubleTapped` (Position über `GetPosition(Surface)`, `null` ignorieren) | Offen | — |
| 35 | UI | `StationMapView.xaml`: `TapGestureRecognizer` (`NumberOfTapsRequired="2"`, `Tapped="OnDoubleTapped"`) am `Surface`-Grid | Offen | — |
| 36 | UI (bedingt B) | `StationMapView.Pointer.cs`: `MapDoubleTapDetector` in `OnPointerPressed` einbinden, bei Doppelklick `ZoomAndCenterAt`; Doppelauslösung mit Recognizer ausschließen | Offen | — |
| 37 | Dokumentation | `docs/help/Suche/beschreibung.md`, Zeile „Zoomen“: Doppeltipp und Kneifgeste ergänzen | Offen | — |
| 38 | Dokumentation | `docs/help/Suche/ablauf-anwender.md`, Schritt 3: Doppeltipp und Kneifgeste ergänzen | Offen | — |
| 39 | Dokumentation | `docs/adr/0005-map-view-decisions.md`: Abschnitt „Abweichungen vom Designentwurf“ anpassen, Doppeltipp ergänzen | Offen | — |
| 40 | Nachweis | Manueller Test unter Windows (run-Skill): Doppelklick zentriert, Stufe +1, bei Stufe 18 nur Zentrieren, Nachladen-Schaltfläche erscheint | Offen | — |
| 41 | Nachweis | Manueller Test Kneifgeste auf Touch-Gerät: fließende Vorschau, Einrasten, Fixpunkt; Ergebnis mit Screenshot-Verweis | Offen | — |
| 42 | Regression | Unit-Suite vollständig ausführen (inkl. `E2EInputPolicyTests_NoFocus`) | Offen | — |
| 43 | Regression | Integrationssuite vollständig ausführen | Offen | — |
| 44 | Regression | E2E-Teilmenge `MapE2ETests_*` ausführen (Navigation, AreaReload, Markers) | Offen | — |
| 45 | Regression | Build Release ohne Warnungen | Offen | — |
