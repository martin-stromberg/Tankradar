# Dokumentation — Tankatlas

Übersicht über alle dokumentierten Funktionsbereiche und Entwickler-Ressourcen von Tankatlas.

## Benutzung der App

- [Navigation und Design-System](Navigation/index.md) — Die vier Hauptbereiche von Tankatlas und wie Sie zwischen ihnen navigieren. Die App passt sich automatisch an Ihre bevorzugte Darstellung (Hell-/Dunkelmodus) an.
- [Einstellungen (Optionen)](Einstellungen/index.md) — Spritsorten auswählen und ordnen, Standortnutzung, Standardansicht und Standardsortierung festlegen. Änderungen werden sofort lokal gespeichert und überstehen Neustart und App-Updates.
- [Umkreissuche](Suche/index.md) — Tankstellen im Umkreis des aktuellen Standorts oder rund um eine eingegebene Adresse, einen Ort oder eine PLZ (Auflösung über OpenStreetMap-Nominatim, kein GPS nötig) im Bereich „Karte“ suchen (Radius 1 bis 25 km, Standard 5 km), Ergebnisliste mit Entfernung, Preisen und Preisalter oder alternativ Kartenansicht (OpenStreetMap-Kacheln, Preismarkierungen in Grün/Teal/Rot/Grau, Zoomen und Verschieben), Filter nach Spritsorte, Sortierung, Hinweise bei fehlendem Standort und Offline-Betrieb; Abnahme unter Windows ohne GPS über den Testmodus.
- [Tankstellen-Detailansicht](Tankstellendetails/index.md) — Detailansicht einer Tankstelle aus der Ergebnisliste: Adresse, Entfernung, Preise der aktivierten Sorten mit Alter und Amber-Markierung, Hinweise, Öffnungszeiten mit Stand, Offline-Betrieb und automatische Aktualisierung nach Wiederverbindung.
- [Favoritengruppen](Favoriten/index.md) — Tankstellen in der Detailansicht selbst benannten Gruppen zuordnen (Mehrfachzuordnung), Gruppen anlegen, umbenennen, beschreiben und nach Rückfrage löschen, Notiz und Priorität je Tankstelle; lokal gespeichert und offline verfügbar.
- [Kraftstoffpreise und Preis-Cache](Preisdaten/index.md) — Abruf der Preise über die Tankerkönig-API (Daten: Tankerkönig / MTS-K), lokaler Preis-Cache mit Zeitstempel, Preisalter „vor X Min.“ (ab 60 Minuten veraltet), Offline-Betrieb mit zuletzt bekannten Preisen, Verbindungserkennung und Hinterlegen des API-Schlüssels lokal bzw. als GitHub-Secret.

## Lokale Entwicklung

- [Git-Hooks zur Qualitätssicherung](git-hooks/index.md) — Automatisierte Qualitätsprüfungen beim Commit und Push. Prüft Übersetzungen, XML-Dokumentation, Platzhalter-Implementierungen, Enum-Testabdeckung, Code-Formatierung, Secrets und das Commit-Nachrichten-Format. Installationsanleitung und Troubleshooting enthalten.
- [CI/CD-Pipeline mit Pre-Releases und Releases](ci-cd/index.md) — Automatischer Build, Prüfungen und Veröffentlichung über GitHub Actions: Branch-Modell `main`/`staging`, Pre-Releases bei jedem Push auf `staging`, Releases bei jedem Push auf `main`, Versionierung ab 0.1.0, lokaler Prüflauf als Ersatz für Actions und Einrichtungs-Checkliste für GitHub.
