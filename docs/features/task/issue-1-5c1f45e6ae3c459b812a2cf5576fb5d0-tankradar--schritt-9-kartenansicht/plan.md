# Umsetzungsplan Schritt 9

## Architektur
1. Reine Logik (`Models/Map`, `Services/Map`): `PriceLevel`, `PriceLevelClassifier` (exakte Dezimalarithmetik: grün = Preis == Minimum der offenen Stationen; rot = 3*(p-min) >= 2*(max-min) bei Spanne > 0; geschlossene grau und nicht in der Spanne; Station ohne Preis der Sorte = Teal), `MapMarkerBuilder`, `MapOrigin`, `WebMercator`, `MapViewport` (Mitte, Zoom, Größe; Projektion, Verschieben, Zoomen, Einpassen, sichtbare Kacheln).
2. Kacheln: `TileServerOptions` (HTTPS-Pflicht, Testmodus-Override `TANKRADAR_TILE_URL`, ohne URL im Testmodus verweigert), `ITileSource`/`HttpTileSource` (identifizierender User-Agent, höchstens 2 parallele Abrufe, Datei-Cache 7 Tage, veralteter Cache bei Fehler, Fehlermerker 30 s, keine Weiterleitungen, Größenlimit).
3. ViewModel: Ansichtsumschalter (`ViewOptions`, Standard aus Einstellungen, nur bei Änderung der Einstellung übernommen), `MapMarkers`, `Origin` (nur im Arbeitsspeicher, bei neuer Suche/Moduswechsel/Fehler gelöscht), `StationListItem` um Koordinaten + `IsOpen` erweitert.
4. UI: Steuerelement `StationMapView` (AbsoluteLayout aus Kachelbildern, Markierungs-Buttons mit AutomationId/Beschreibung/Hinweis, Ursprungsmarke, Zoom-/Zentrieren-/Verschiebe-Schaltflächen, Zähler, Zoomstufe, Quellenangabe, Pan-/Pinch-Gesten), Einbindung in `MapPage` anstelle der Liste, Legende.
5. Testunterstützung: `MockTileServer` (Loopback, 1x1-PNG, zählt Anfragen, User-Agent); `SearchE2ETestBase` startet ihn und setzt `TANKRADAR_TILE_URL`.
6. Tests: Unit (Klassifikation inkl. Grenzfälle, Mercator/Viewport, Marker, Kachelquelle, Optionen, ViewModel, Texte), Integration (Kachelquelle gegen Mock-Server), E2E (Wechsel, Standard aus Einstellungen, Zoom, Verschieben, Details über Marker, Quellenangabe, nur Mock-Kacheln).
7. ADR 0005 (Kartenkomponente, Kachelanbieter, Abweichungen vom Entwurf, Preisniveau-Regeln); ADR 0004 ergänzt; Hilfe `docs/help/Suche/Kartenansicht.md`; README.
8. `validate-workflows.py`: Verzeichnis-/Glob-Uploads (upload-artifact) nur für erlaubte Pfade; `gh release create/upload` mit .ipa, Verzeichnis, Glob oder Variablen als Asset verboten; Python-Tests.

## Offene Punkte
(keine)
