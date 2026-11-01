# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Alle Planelemente aus `plan.md` (Änderungen an Klassen, Methoden, XAML, Windows-Zeigerteil, Tests und Dokumentation) sind im Code bzw. in den Dokumenten vorhanden. Der manuelle Windows- und Touch-Nachweis (Plan-Schritt 11) ist bewusst nicht Teil der Implementierungsprüfung und steht unter „Hinweise“.

## Umgesetzte Planelemente

### Neue Klassen

- [x] `MapPinchTracker` (Klasse) — angelegt in `src/Tankradar.MAUI/Services/Map/MapPinchTracker.cs`
- [x] Feld `IsActive`, `Factor`, `OriginX`, `OriginY` in `MapPinchTracker` — vorhanden
- [x] Methode `Start`, `Update`, `End` in `MapPinchTracker` — vorhanden (Verhalten gemäß Plan: `Update` ignoriert ohne Start und ungültige Einzelfaktoren)
- [x] `MapDoubleTapDetector` (Klasse) — angelegt in `src/Tankradar.MAUI/Services/Map/MapDoubleTapDetector.cs` (bedingt, Gate-Ergebnis B)
- [x] Methode `Register(x, y, timestampMs)` in `MapDoubleTapDetector` — vorhanden, Zeitfenster 500 ms und Abstand 4 Pixel, Zurücksetzen nach Doppeltipp

### Erweiterungen `MapViewport`

- [x] Methode `ZoomStepsFor(factor)` (öffentlich, statisch) — vorhanden, ungültige Faktoren ergeben 0
- [x] Methode `ScaleAtPoint(factor, x, y)` — vorhanden, Fixpunkt in Weltkoordinaten, Begrenzung auf MinZoom..MaxZoom, `this` bei keiner Änderung
- [x] Methode `ZoomAndCenterAt(delta, x, y)` — vorhanden, liefert auch an der Grenze ein neues Objekt
- [x] Bestehende Methoden `ZoomBy`, `ToLatLon`, `Create`, `PanByPixels` — unverändert (per `git diff` nur Zeilen hinzugefügt)

### `StationMapView` (`StationMapView.xaml.cs`)

- [x] Feld `_pinch` (`MapPinchTracker`) — eingeführt, `_pinchScale` entfernt
- [x] `OnPinchUpdated` — Started/Running/Completed gemäß Programmablauf, Schwellen 1,3 / 0,77 entfernt, `ViewportChanged` nur bei Stufenwechsel (`CompletePinch`)
- [x] `ShowPinchPreview` — begrenzt `TileLayer.Scale`, setzt `AnchorX`/`AnchorY` am Gestenmittelpunkt, ohne Neuzeichnen
- [x] `OnPanUpdated` — früher Ausstieg, solange `_pinch.IsActive`
- [x] `Render` — setzt `TileLayer.Scale` auf 1 und den Anker auf die Mitte zurück
- [x] `OnDoubleTapped` — Position über `GetPosition(Surface)`, `null` wird ignoriert
- [x] `ZoomAndCenterAt(x, y)` — gemeinsame Methode für Recognizer und Windows-Maus, ruft `Render()` und `RaiseViewportChanged()` auf
- [x] Partielle Methode `SuppressDoubleTap` (Doppelauslösung-Schutz, Ergebnis B) — vorhanden

### `StationMapView.xaml`

- [x] `TapGestureRecognizer` mit `NumberOfTapsRequired="2"` und `Tapped="OnDoubleTapped"` am `Surface`-Grid, neben Pan- und Pinch-Recognizer — vorhanden

### `StationMapView.Pointer.cs` (Windows, Ergebnis B)

- [x] `OnPointerPressed` — übergibt bei linker Maustaste Position und Zeitstempel an `MapDoubleTapDetector`, bei Doppeltipp `ZoomAndCenterAt(x, y)`
- [x] Feld `_doubleTap` (`MapDoubleTapDetector`) — vorhanden
- [x] Ziehen (`MapPointerTracker`) und `OnPointerReleased` — unverändert

### Tests

- [x] `MapViewportTests_Gestures` (`src/Tankradar.Tests.Unit/Unit/MapViewportTests_Gestures.cs`) — alle 11 Testmethoden aus dem Plan vorhanden (davon zwei als Theory)
- [x] `MapPinchTrackerTests_Factor` (`src/Tankradar.Tests.Unit/Unit/MapPinchTrackerTests_Factor.cs`) — alle 5 Testmethoden vorhanden
- [x] `MapDoubleTapDetectorTests_Timing` (`src/Tankradar.Tests.Unit/Unit/MapDoubleTapDetectorTests_Timing.cs`) — alle 4 Testmethoden vorhanden
- [x] Keine Änderung an bestehenden Tests und keine neue E2E-Datei (wie im Plan vorgesehen)

### Dokumentation

- [x] `docs/help/Suche/beschreibung.md`, Zeile „Zoomen“ — Doppeltipp und Kneifgeste ergänzt
- [x] `docs/help/Suche/ablauf-anwender.md`, Schritt 3 — Doppeltipp und Kneifgeste ergänzt
- [x] `docs/adr/0005-map-view-decisions.md` — Ergänzung im Abschnitt „Abweichungen vom Designentwurf“ (Zeilen 111 ff.) um Kneifgeste und Doppeltipp

### Tasks-Datei

- [x] `docs/features/task/issue-19-karte-gesten-doppeltipp-pinch/tasks.md` — vollständig neu geschrieben. Alle 45 Aufgaben stehen darin; die Testnachweise nennen jetzt konkrete Testmethoden (bzw. „Kein direkter Test“ mit Begründung).

## Offene Aufgaben

Nicht ausgefüllt, da der Status „Vollständig umgesetzt“ lautet.

## Hinweise

- **Manuelle Abnahme (Plan-Schritt 11, bewusst nicht als Implementierungsaufgabe gewertet):** Der Doppelklick mit echter Windows-Maus (Mitte am Klickpunkt, Stufe +1, bei Stufe 18 nur Zentrieren, Erscheinen der Nachladen-Schaltfläche) und die Kneifgeste auf einem Touch-Gerät (fließende Vorschau, Einrasten, Fixpunkt) sind vom Anwender bei der Abnahme zu prüfen. In `tasks.md` stehen dafür die Aufgaben 40 und 41 auf `Offen`, mit dem Vermerk „Abnahme durch den Anwender“.
- **Ungeprüfte Annahmen aus dem Gate (Aufgaben 1–3):** Die Entscheidung „Ergebnis B“ (Maus-Doppelklick wird vom Recognizer nicht zuverlässig geliefert, daher eigener Detektor) sowie die Annahmen „Scale inkrementell“ und „ScaleOrigin relativ“ stützen sich auf den Quelltext und die XML-Dokumentation von MAUI 10.0.120. Auf einem Touch-Gerät sowie auf iOS/Android sind sie nicht geprüft. Der Plan sieht vor, dass sich die Auswertung bei Gegenbefund lokal in `MapPinchTracker.Update` und `OnPinchUpdated` korrigieren lässt.
- **Gegenüber dem Plan leicht erweitert:** `OnPinchUpdated` behandelt zusätzlich `GestureStatus.Canceled` (beendet die Geste und zeichnet neu). Der Plan nennt diesen Fall nicht; das Verhalten ist sinnvoll und beeinträchtigt die Planerfüllung nicht.
- **Testnachweise nicht erneut ausgeführt:** Dieses Review hat keine Tests gestartet. Die gespeicherten Logs im Ordner `inventory/test-results/` stammen vom Ausgangsstand (Unit 2016, Integration 297 Tests). Die im Tasks-Stand genannten Umsetzungsläufe (Unit 2049/2049, Integration 297/297, E2E-Teilmenge `MapE2ETests_*` grün) sind dort nicht als Datei belegt.
- **E2E-Abdeckung der Gesten:** Wie im Plan (Offene Punkte 9) beschrieben, gibt es keinen E2E-Test für Doppelklick und Kneifgeste, weil `E2EInputPolicyTests_NoFocus` Maus- und Klickaufrufe in E2E-Quellen verbietet. Die Abdeckung liegt bei den Unit-Tests der Rechenlogik und Erkennung sowie beim manuellen Nachweis.
- **Drei fehlgeschlagene E2E-Tests ohne Kartenbezug (laut Tasks-Stand):** `TankbookEntriesE2ETests_Persistence.Entries_SurviveApplicationRestart`, `TankbookStatisticsE2ETests_Hybrid.HybridVehicle_ShowsSeparateBlocksPerKind` und `SearchE2ETests_FuelOrder.ChangedOrderAndDeselectedFuel_AreReflectedAfterNewSearch`. Sie werden im Plan-Review nicht bewertet, sind aber vor einer Abnahme zu klären.
