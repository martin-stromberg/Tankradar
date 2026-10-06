# Anforderung: Kartenansicht der Suchergebnisse (Schritt 9)

Wörtliche Beschreibung aus project-plan.md, Schritt 9, plus Zusatz des Projektleiters (siehe Auftrag).

## Funktionale Anforderungen
- F1 Ergebnisse der Standort- und Adresssuche alternativ zur Liste auf einer Karte; Wechsel Liste/Karte; Vorgabe = Standardansicht aus den Einstellungen.
- F2 Plattformübergreifende Karte auf Basis von OpenStreetMap-Kacheln, gleich unter iOS und Windows, per FlaUI prüfbar.
- F3 Quellenangabe "© OpenStreetMap-Mitwirkende" sichtbar.
- F4 Zoomen und Verschieben.
- F5 Standortsuche: eigener Standort markiert; Adresssuche: gesuchte Position markiert; Standort nur gemäß GPS-Einstellung abgefragt, nicht gespeichert.
- F6 Je Tankstelle eine Markierung mit Preis; Farbe nach Preisniveau in der aktuellen Ergebnismenge: Grün günstigster, Rot oberes Drittel der Spanne, Teal übrige, Grau geschlossen. Maßgeblich: Preis der gefilterten Sorte, sonst zuerst gewählte Sorte aus den Einstellungen.
- F7 Antippen einer Markierung öffnet die Detailansicht.
- F8 Tests: Preisniveau-Einstufung (gleiche Preise, einzelne Station), FlaUI: Wechsel Liste/Karte, Zoomen, Verschieben, Details über Karte; Kacheln nie von produktiven Servern.
- F9 Windows-Zwischenstand 0.1.13 unter review-versions/ (nicht committen).
- F10 Zusatz: ADR 0004 um Logo/Avatar in der Kopfleiste und "Geöffnet" ohne "bis HH:MM" ergänzen (oder umsetzen).
- F11 Zusatz: scripts/validate-workflows.py erkennt Verzeichnis-Uploads und gh-release-upload-Aufrufe, die eine .ipa enthalten könnten.

## Nichtfunktionale Anforderungen
- OSM-Kachelnutzungsrichtlinie (User-Agent, Caching, keine Massenabrufe), Anbieterwahl per ADR.
- Keine neuen NuGet-Pakete ohne Lizenz-/Schwachstellenprüfung; iOS-Compile-Prüfung grün.
- Designentwurf (Karten-Screen) verbindlich; Abweichungen im ADR.
- Keine Schlüssel in Artefakten; keine echten Endpunkte in Tests.
