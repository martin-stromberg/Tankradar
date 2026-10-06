# ADR 0005: Kartenansicht der Suchergebnisse

## Status

Angenommen (2026-10-06)

## Kontext

Schritt 9 stellt die Suchergebnisse alternativ zur Liste auf einer Karte dar. Die Karte muss unter iOS und
Windows gleich aussehen, in FlaUI-Tests über UI Automation prüfbar sein, die Quellenangabe
„© OpenStreetMap-Mitwirkende“ zeigen, zoom- und verschiebbar sein und je Tankstelle eine Markierung mit Preis
und Preisniveau-Farbe tragen. Kacheln dürfen in Tests nie von produktiven Servern kommen; das Repository
ist öffentlich (keine Schlüssel in Artefakten). Designentwurf: Screen `suche_kartenansicht`.

## Entscheidung

- **Eigene Kartenkomponente ohne neues NuGet-Paket.** Native Karten-Steuerelemente (Maps-Bibliotheken,
  WebView-Karten) sind unter iOS und Windows verschieden oder brauchen Zusatzpakete mit eigenen Lizenz-,
  Schlüssel- und Prüfpflichten, und ihre Inhalte sind für UI Automation nicht zugänglich. Die Komponente
  `StationMapView` setzt Kacheln als Bilder und Markierungen als Schaltflächen auf einem
  `AbsoluteLayout`; die gesamte Kartenmathematik (Web-Mercator, Einpassen, sichtbare Kacheln) liegt
  UI-unabhängig in `MapViewport` und ist unit-getestet. Es entstehen keine neuen Abhängigkeiten, die
  Sicherheitsprüfung bleibt unverändert.
- **Kachelanbieter: die Standard-Kachelserver von OpenStreetMap** (`tile.openstreetmap.org`), konsistent
  mit der bereits genutzten Quelle der Adresssuche (Nominatim) und der geforderten Quellenangabe. Die
  Nutzungsrichtlinie wird eingehalten: identifizierende Kennung (`Tankatlas/0.1 de.martinstromberg.tankradar`),
  Zwischenspeicherung im Speicher und auf dem Gerät (sieben Tage; veraltete Kacheln werden bei Fehlern
  weiterverwendet; Obergrenze 100 MB), höchstens zwei parallele Abrufe, kurze Verzögerung vor dem Abruf
  (Kacheln, die sofort wieder verschwinden, werden nicht abgerufen), kein Vorabruf, ein Wartefenster von
  30 Sekunden nach Fehlern, keine Weiterleitungen, Antwortgröße und PNG-Format geprüft, Zoomstufen 4 bis 18.
  Die Adressvorlage ist eine Einstellung (`TileServerOptions`); ein Wechsel auf einen kommerziellen
  Kachelanbieter erfordert nur eine andere Vorlage (und ggf. einen Schlüssel, der dann wie der
  Tankerkönig-Schlüssel nie in Artefakte gelangen dürfte). Bei starker Verbreitung der App wäre ein
  eigener oder kommerzieller Kachelanbieter einzuplanen, da die OSM-Server nur leichte Nutzung vorsehen.
- **Datenschutz:** Der Kachelabruf verrät dem Kachelserver die IP-Adresse und den betrachteten
  Kartenausschnitt (Kachelnummern), nicht aber die genaue Suchposition. Kachelnummern werden nicht
  protokolliert. Die Suchposition (eigener Standort bzw. gesuchte Adresse) wird nur im Arbeitsspeicher
  des ViewModels als `MapOrigin` gehalten, bei jeder neuen Suche, bei Fehlern und beim Wechsel der
  Suchart verworfen und nie gespeichert oder protokolliert. Der Standort wird weiterhin nur gemäß der
  Einstellung zur Standortnutzung abgefragt; die Adresssuche fragt ihn nicht ab.
- **Tests ohne produktive Server:** Im Testmodus ist ein Kachelserver nur über `TANKRADAR_TILE_URL`
  (Loopback-HTTP, Mock-Server `MockTileServer`) einstellbar; ohne Adresse werden im Testmodus keine
  Kacheln abgerufen (kein Rückfall auf den produktiven Server).
- **Preisniveau-Farben** (`PriceLevelClassifier`, exakte Dezimalarithmetik): Grün = günstigster Preis
  (bei Gleichstand alle Tankstellen mit diesem Preis), Rot = Preis ≥ Minimum + 2/3 der Spanne (nur bei
  Spanne > 0, also nie bei einer einzelnen Tankstelle oder nur gleichen Preisen), Teal = übrige, Grau =
  geschlossen. Geschlossene Tankstellen gehen nicht in die Spanne ein, damit „günstigster Preis“ ein
  tatsächlich nutzbares Angebot bezeichnet. Unbekannter Öffnungszustand gilt als geöffnet. Eine
  Tankstelle ohne Preis der maßgeblichen Sorte erhält eine Markierung ohne Preis (Teal, nicht in der Spanne).
  Maßgeblich ist die gefilterte Sorte, ohne Filter die zuerst gewählte Sorte der Einstellungen
  (`StationResultBuilder.ResolveFuelType`, dieselbe Regel wie die Preissortierung). Die Karte zeigt die
  gesamte gefilterte Ergebnismenge, nicht nur die erste Listenseite.
- **Farbwerte:** Grün `#006C49`, Rot `#BA1A1A`, Teal `#0F766E`, Grau `#64748B` (Entwurfs-Tokens
  `secondary`, `error`, `primary-container`, Slate 500). Das hellere Emerald-Grün des Designsystems
  erreicht mit weißem Text keinen ausreichenden Kontrast (unter 4,5:1). Farbe ist nicht das einzige
  Merkmal: Beschreibung und Hinweis der Markierung nennen das Preisniveau, eine Legende erklärt die Farben.

## Abweichungen vom Designentwurf

- **Tippen öffnet direkt die Detailansicht.** Die Anforderung verlangt das Öffnen der Detailansicht beim
  Antippen einer Markierung; die Vorschaukarte des Entwurfs (Auswahl, Ausstattung, „Route starten“,
  Favorit) entfällt. Route und Favoriten gehören zu den Schritten 17 und 10, die Ausstattung liefert die
  Quelle nicht (siehe ADR 0004).
- **Markierungen** zeigen den Preis (farbige Fläche mit weißem Text, 44 px hoch) ohne Marke und ohne
  Auszeichnungssymbol. „Auch Strom“, „Entlang Route“, Verkehrslage und die Kopfleiste mit Logo/Avatar
  entfallen (ADR 0001, ADR 0004; Routing ist Schritt 17).
- **Umschalter Liste/Karte** als Auswahl-Chips wie die übrigen Gruppen der Suchseite statt eines
  Segmentschalters; er steht in der ersten Karte der Suchseite.
- **Verschieben auch über Schaltflächen** (Pfeile) zusätzlich zur Wischgeste, und Zoom über Schaltflächen
  und Kneifgeste mit ganzzahligen Stufen. Grund: Bedienung ohne Wischgeste (Tastatur, Sprachausgabe) und
  Prüfbarkeit per UI Automation, die keine Mausgesten braucht. „Ausschnitt zurücksetzen“ passt den
  Ausschnitt wieder auf alle Ergebnisse und die Suchposition ein (statt „Auf mich zentrieren“).
- **Zähler** „X von Y Stationen sichtbar“ (Entwurf: „18 Stationen (10 km)“), da er sich mit dem
  Ausschnitt ändert und so Zoomen und Verschieben prüfbar macht; zusätzlich die Zoomstufe.
- **Kartenhöhe fest (440 px) innerhalb der scrollenden Suchseite.** Auf Touchgeräten kann die
  Wischgeste der Karte mit dem Scrollen der Seite konkurrieren; die Bedienung über die Schaltflächen
  bleibt immer möglich.

## Folgen

- Die Kartenkomponente ist die Grundlage für spätere Schritte (z. B. Routenansicht, Schritt 17).
- Das Verhalten der Gesten unter iOS ist nur per Compile-Prüfung (`local-ci.ps1`) abgesichert und erst auf
  einem Gerät bzw. in TestFlight prüfbar.
