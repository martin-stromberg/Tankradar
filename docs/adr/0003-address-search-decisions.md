# ADR 0003: Entscheidungen zur Suche nach Adresse, Ort oder PLZ

## Status

Angenommen (2026-10-05)

## Kontext

Die Beschreibung des Entwicklungsschritts „Suche nach Adresse, Ort oder PLZ“ nennt einen Radius von
5 bis 50 km. Der Projektplan legt dagegen fest: Suchradius 1 bis 25 km über Chips mit den Stufen 1,
2, 5, 10, 15 und 25 km, weil die Preisquelle (Tankerkönig) höchstens 25 km zulässt und der
Designentwurf (Screen `suche_kartenansicht`) Chips statt eines freien Zahlenfelds zeigt. Der Entwurf
enthält außerdem ein Suchfeld „Adresse, PLZ oder Ort (z.B. Frankfurt, 60311...)“ mit Löschen-Schaltfläche.
Die Adressauflösung erfolgt über OpenStreetMap-Nominatim, dessen Nutzungsrichtlinie einzuhalten ist.

## Entscheidung

- **Radius:** Die Adresssuche verwendet dieselben Radiuschips (1 bis 25 km) wie die Standortsuche.
  Ein Radius bis 50 km wäre von der Preisquelle nicht bedienbar (Antwort „rad ungültig oder größer als 25“);
  eine künstliche Mehrfachabfrage wäre eine nicht angeforderte Erweiterung. Der Projektplan hat Vorrang.
- **Suchart statt eigener Seite:** Der Bereich „Karte“ erhält die Chips „Aktueller Standort“ und
  „Adresse, Ort oder PLZ“. Das Eingabefeld (mit Löschen-Schaltfläche, Platzhalter nach Entwurf) erscheint nur im
  Adressmodus. Das Glas-/Suchleisten-Layout des Entwurfs gehört weiterhin zur späteren Kartenansicht
  (siehe ADR 0002); die Auswahlchips und Karten folgen dem bestehenden Designsystem.
- **Nutzungsrichtlinie Nominatim:** Anfrage nur beim ausdrücklichen Absenden (Schaltfläche oder
  Such-/Eingabetaste, keine Autovervollständigung), höchstens eine Anfrage je Sekunde über eine eigene
  Drosselung (`GeocodingOptions.MinRequestInterval` darf 1 s nicht unterschreiten, auch im Testmodus nicht),
  identifizierender `User-Agent` („Tankatlas/0.1 de.martinstromberg.tankradar“), nur HTTPS, kein
  automatisches Wiederholen, keine Weiterleitungen, ein Treffer je Anfrage. Die Quellenangabe
  „Geodaten © OpenStreetMap-Mitwirkende“ steht im Suchbereich.
- **Beschränkung auf Deutschland:** Die Anfrage setzt `countrycodes=de`, weil die Preisquelle nur
  deutsche Tankstellen kennt; Treffer im Ausland würden nur leere Listen ergeben.
- **Eingabevalidierung:** 3 bis 120 Zeichen aus Buchstaben, Ziffern, Leerzeichen und `. , - ' / ( ) & + # :`;
  Steuer- und Markup-Zeichen werden vor dem externen Aufruf abgewiesen. Der Suchbegriff wird in der
  Anfrageadresse maskiert.
- **Datenschutz:** Adresse, Ortsname und Position werden nicht gespeichert und nicht protokolliert; die
  Eingabe lebt nur im Arbeitsspeicher des ViewModels. Das Ergebnis der Auflösung (`GeocodingResult`) gibt
  in `ToString()` nur den Status aus.
- **Tests:** Nominatim wird in Tests nie produktiv angesprochen. Im Testmodus (`TANKATLAS_TEST_DATA_PATH`)
  kann die Adresse über `TANKRADAR_GEOCODING_URL` auf den lokalen `MockNominatimServer` zeigen; ohne Angabe
  wird die Auflösung verweigert (kein Rückfall auf den produktiven Dienst).

## Folgen

Wird die Preisquelle später gewechselt oder erweitert, können Radiusgrenze und Länderbeschränkung ohne
Änderung des Ablaufs angepasst werden (`SearchRadius`, Parameter in `NominatimGeocodingService`).
