# ADR 0005: Kartenansicht der Suchergebnisse

## Status

Angenommen (2026-10-06); Zwischenspeicher, Speicherort, Kennung, Quellenlink und Mausbedienung präzisiert in
Schritt 10 (siehe Abschnitte „Zwischenspeicher nach HTTP-Caching-Angaben“, „Speicherort des Kachelspeichers“,
„Kennung und Quellenangabe“ und „Mausbedienung unter Windows“)

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
  Nutzungsrichtlinie wird eingehalten: identifizierende Kennung mit tatsächlicher App-Version und
  Kontaktangabe (`Tankatlas/<Version> (+https://github.com/martin-stromberg/Tankradar; de.martinstromberg.tankradar)`,
  siehe unten), Zwischenspeicherung im Speicher und auf dem Gerät nach den HTTP-Caching-Angaben des Servers (siehe unten;
  veraltete Kacheln werden bei Fehlern weiterverwendet; Obergrenze 100 MB), höchstens zwei parallele Abrufe, kurze Verzögerung vor dem Abruf
  (Kacheln, die sofort wieder verschwinden, werden nicht abgerufen), kein Vorabruf, ein Wartefenster von
  30 Sekunden nach Fehlern, keine Weiterleitungen, Antwortgröße und PNG-Format geprüft, Zoomstufen 4 bis 18.
  Die Adressvorlage ist eine Einstellung (`TileServerOptions`); ein Wechsel auf einen kommerziellen
  Kachelanbieter erfordert nur eine andere Vorlage (und ggf. einen Schlüssel, der dann wie der
  Tankerkönig-Schlüssel nie in Artefakte gelangen dürfte). Bei starker Verbreitung der App wäre ein
  eigener oder kommerzieller Kachelanbieter einzuplanen, da die OSM-Server nur leichte Nutzung vorsehen.
- **Zwischenspeicher nach HTTP-Caching-Angaben (präzisiert in Schritt 10).** Die Gültigkeit einer Kachel
  richtet sich nach den Angaben des Kachelservers, nicht nach einer festen Frist: `Cache-Control` hat Vorrang
  (`max-age`; `no-cache` verlangt vor jeder Wiederverwendung eine Rückfrage; `no-store` verbietet das Speichern
  auf dem Gerät, die Kachel bleibt nur im Arbeitsspeicher), danach `Expires` (bezogen auf die Serveruhr `Date`),
  und nur wenn der Server nichts angibt, gilt pauschal der **Rückfall von 7 Tagen** (`CacheLifetime`, entspricht der
  Mindestdauer der Nutzungsrichtlinie). Abgelaufene Kacheln werden **bedingt** neu angefragt
  (`If-None-Match` aus dem gespeicherten `ETag`, sonst `If-Modified-Since` aus `Last-Modified`); ein `304`
  verlängert die Gültigkeit ohne erneute Übertragung, ein `200` ersetzt die Kachel, bei einem Fehler wird die
  abgelaufene Kachel weiterverwendet. Je Kachel liegt neben der PNG-Datei eine kleine `.meta`-Datei (ETag,
  Last-Modified, Ablaufzeitpunkt); Kacheln aus früheren Versionen ohne `.meta` gelten ab ihrer Dateizeit für
  die Rückfallfrist. Abwegig lange Angaben werden auf ein Jahr begrenzt. Die Prüfung aller Fälle erfolgt gegen
  einen Test-HTTP-Handler und den Mock-Kachelserver (`ETag`, `Cache-Control`, `304`), nie gegen den echten Server.
- **Speicherort des Kachelspeichers (Schritt 10).** Die Kacheln liegen im Zwischenspeicher-Verzeichnis der
  Plattform (`FileSystem.CacheDirectory`, über `IAppDataPathProvider.GetCacheDirectory`) statt im Datenverzeichnis
  (`AppDataDirectory`). Unter iOS ist das Cache-Verzeichnis von der Datensicherung ausgenommen, das System darf es
  bei Platzmangel leeren; die Kacheln sind jederzeit neu abrufbar. Im Testmodus liegt der Zwischenspeicher im
  isolierten Testverzeichnis.
- **Kennung und Quellenangabe (Schritt 10).** Der `User-Agent` enthält die tatsächliche App-Version
  (`AppInfo.VersionString`, nur Buchstaben, Ziffern, Punkt, Minus und Plus; sonst `0.0.0`) und die Projekt-URL
  als Kontaktangabe; dieselbe Form verwenden Nominatim und der Preisdienst (`AppIdentity`). Die Quellenangabe
  „© OpenStreetMap-Mitwirkende“ ist ein Link (Schaltfläche im Linkstil, `Launcher`) auf
  https://www.openstreetmap.org/copyright und bleibt dauerhaft sichtbar.
- **Mausbedienung unter Windows (Schritt 10).** Die Gestenerkenner der Karte reagieren unter Windows nur auf
  Berührung und Stift. Für die Maus wertet die Karte die Zeigerereignisse des nativen Panels aus (nur
  `PointerDeviceType.Mouse`, linke Taste): Ziehen verschiebt die Karte, das Mausrad zoomt um ganze Stufen (eine
  Raste entspricht einer Stufe, Teilrasten von Touchpads summieren sich) und wird nicht an die umgebende Seite
  weitergegeben. Die Umrechnung liegt plattformunabhängig und unit-getestet in `MapPointerTracker`; die
  Zeigerereignisse selbst sind in FlaUI-Tests nicht prüfbar (der Mauszeiger des Anwenders darf im
  Off-Screen-Betrieb nicht bewegt werden) und werden bei der Abnahme von Hand geprüft.
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
  Favorit) entfällt. Route (Schritt 17) und Favoriten (Schritt 10, siehe ADR 0006) werden in der Detailansicht angeboten, die Ausstattung liefert die
  Quelle nicht (siehe ADR 0004).
- **Markierungen** zeigen den Preis (farbige Fläche mit weißem Text, 44 px hoch) ohne Marke und ohne
  Auszeichnungssymbol. „Auch Strom“, „Entlang Route“, Verkehrslage und die Kopfleiste mit Logo/Avatar
  entfallen (ADR 0001, ADR 0004; Routing ist Schritt 17).
- **Umschalter Liste/Karte** als Auswahl-Chips wie die übrigen Gruppen der Suchseite statt eines
  Segmentschalters; er steht in der ersten Karte der Suchseite.
- **Verschieben auch über Schaltflächen** (Pfeile) zusätzlich zur Wischgeste (unter Windows zusätzlich Ziehen
  mit der Maus und Zoom mit dem Mausrad), und Zoom über Schaltflächen und Kneifgeste mit ganzzahligen Stufen. Grund: Bedienung ohne Wischgeste (Tastatur, Sprachausgabe) und
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
