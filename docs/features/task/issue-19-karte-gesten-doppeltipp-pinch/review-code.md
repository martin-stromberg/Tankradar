# Code-Review

## Ergebnis

**Status:** Keine Befunde

Basis: `origin/staging`, Merge-Base `4fee2a3`. Geprüft wurden die uncommitteten Änderungen im Arbeitsverzeichnis. Der Branch enthält außer dem Plan-Commit `f62b7d7` keine eigenen Code-Commits.

### Nachprüfung der Befunde aus `review-code.1.md`

1. **Doppeltipp auf Bedienschaltflächen und Markierungen (StationMapView): behoben.**
   - `ControlsPanel` hat jetzt einen Namen. `OnDoubleTapped` verwirft Positionen, für die `IsOnControl` zutrifft. Geprüft werden `ControlsPanel.Frame` und die `Frame`s aller `_markerButtons`.
   - Beide Bezugsrahmen stimmen mit `e.GetPosition(Surface)` überein. `ControlsPanel` ist ein direktes Kind von `Surface`, und `MarkerLayer` füllt `Surface` ab (0, 0) aus.
   - Die Suchpositionsmarkierung `_originMarker` ist ein nicht bedienbarer `Border`; dass sie fehlt, ist richtig.
   - Unter Windows setzt `OnAnyPointerPressed` jetzt `_lastPressWasMouse`. Der Handler ist über `AddHandler(..., handledEventsToo: true)` angehängt und greift deshalb auch bei Drücken auf Schaltflächen.
   - `DetachPointerInput` entfernt den Handler wieder mit `RemoveHandler`. Durch `??=` bleibt die Delegatinstanz gleich, das An- und Abhängen ist also symmetrisch.
   - Der Maus-Doppelklick in `OnPointerPressed` braucht keine `IsOnControl`-Prüfung. Schaltflächen markieren `PointerPressed` als behandelt, und `OnPointerPressed` ist ohne `handledEventsToo` angehängt.
2. **Laufende Kneifgeste beim Einpassen bzw. bei Größenänderung: behoben.** `FitToContent` und `OnSurfaceSizeChanged` (vor `Resize`) rufen `_pinch.End()` auf. Ein folgendes `Completed` endet dann in `CompletePinch` über `if (!_pinch.IsActive) return;` ohne Wirkung und ohne `ViewportChanged`. Der Pfad über `FitToResult` bei `_fitPending` mündet ebenfalls in `FitToContent`.
3. **Überflüssiger Code im Pinch-Ablauf: behoben.** Im Zweig `Started` steht kein `_pinch.End()` mehr. `CompletePinch` weist `viewport.ScaleAtPoint(...)` direkt zu.
4. **Doppelter Code in `MapViewport`: behoben.** `WrapX(double, int)` und `ToWorld(double, double)` sind eingeführt. `PanByPixels`, `ScaleAtPoint` und `ZoomAndCenterAt` nutzen `WrapX`. `ToLatLon`, `ScaleAtPoint` und `ZoomAndCenterAt` nutzen `ToWorld`. Das Verhalten bleibt gleich:
   - `ToLatLon` begrenzt die Breite weiterhin vor dem Umrechnen.
   - `ZoomAndCenterAt` begrenzt `worldY` auf die alte Weltgröße, bevor skaliert wird.
5. **Testmethoden mit mehreren Fällen: behoben.**
   - Getrennt sind jetzt `ScaleAtPoint_AtMaxZoom_StaysAtMax` und `ScaleAtPoint_AtMinZoom_StaysAtMin`.
   - Ebenso getrennt sind `ScaleAtPoint_FactorBelowRoundingThreshold_ReturnsSameInstance` (Theory) und `ScaleAtPoint_AtLimit_ReturnsSameInstance`.
   - Die Eckentests sind als zwei Theories über die Eckposition aufgeteilt, eine je Aspekt (Breite, Länge).
6. **Fehlende Testabdeckung: behoben.** Neu sind:
   - `ScaleAtPoint_FactorBeyondMaxZoom_ClampsAndKeepsOrigin` und `ScaleAtPoint_FactorBeyondMinZoom_ClampsAndKeepsOrigin`, beide mit Fixpunktprüfung bei begrenzter Zielstufe
   - `Register_SecondTapWithEarlierTimestamp_IsSingle`
   - `Start_WhileActive_ResetsFactorAndOrigin`

### Geprüfte neue Änderungen ohne Befund

- **`Render`:** Die Vorschau (`TileLayer.Scale`, Anker) wird jetzt vor dem frühen Rücksprung zurückgesetzt. Damit bleibt auch bei unsichtbarer Karte keine skalierte Kachelebene stehen.
- **`OnPinchUpdated`:** Der Mittelpunkt wird aus dem relativen `ScaleOrigin` in Pixel umgerechnet. Der Ablauf ist: Started → Start, Running → Update und Vorschau, Completed → `CompletePinch`, Canceled → End und Neuzeichnen. `ShowPinchPreview` begrenzt den Vorschaufaktor auf die erreichbaren Zoomstufen.
- **`OnPanUpdated`:** Während der Kneifgeste wird nur die Lage mitgeführt. Nach dem Loslassen eines Fingers springt die Karte deshalb nicht.
- **Zeitstempel:** `PointerPoint.Timestamp` zählt in µs und wird per `/ 1000` in ms umgerechnet. Das passt zu `MapDoubleTapDetector.MaxIntervalMs`.
- **`TankbookStatisticsE2ETests_Hybrid`:** Alle vier Vorgänge liegen zwischen Monatsbeginn und jetzt; das letzte bei `monthStart + 4 * step` und damit vor `now`. Ein Sonderfall bleibt unkritisch: Läuft der Test in den ersten Minuten eines Monats, kann `AddEntry` (minutengenau) mehrere Vorgänge auf dieselbe Minute legen. `TankbookEntryOrder` sortiert bei gleichem Zeitpunkt nach dem Kilometerstand, und der steigt in den Testdaten. Reihenfolge und Verbrauchsberechnung bleiben daher richtig.
- **`SearchE2ETests_FuelOrder`:** Nach jedem `MoveFuelType` wartet `WaitUntil` auf die erwartete Zwischenreihenfolge. Die Fehlermeldungen sind aussagekräftig, und die abschließende Prüfung bleibt erhalten.

## Befunde

Keine.

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
- `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookStatisticsE2ETests_Hybrid.cs`
- `src/Tankradar.Tests.E2E/E2E/FlaUI/SearchE2ETests_FuelOrder.cs`
