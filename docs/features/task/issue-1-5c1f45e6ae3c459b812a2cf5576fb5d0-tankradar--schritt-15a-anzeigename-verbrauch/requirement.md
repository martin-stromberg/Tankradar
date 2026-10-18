# Anforderung: Schritt 15a – Anzeigename von Tankstellen und erster Verbrauch ab Startstand

Quelle: Abschnitt „Schritt 15a“ und Entscheidungstabelle (Zeilen „Tankstellen-Anzeigename“, „Erster Verbrauch“) im Projektplan (Anwender-Rückmeldungen aus TestFlight vom 12.10.2026).

## Fachliche Anforderungen

### A. Tankstellen-Anzeigename
- A1 Überall, wo Tankstellen erscheinen (Startseite/Favoriten, Gruppenansicht, Suchergebnisse, Karte, Detailansicht, Tankbuch-Zuordnung), wird die Marke (Tankerkönig `brand`) prominent angezeigt, der Stammdatenname klein darunter. Fehlt die Marke, bleibt der Name.
- A2 In der Detailansicht kann der Nutzer einen eigenen Anzeigenamen vergeben, ändern und entfernen. Er wird lokal gespeichert (Migration erhält Daten), ist Teil der Sicherung und hat überall Vorrang.
- A3 Sicherung: Die neue Spalte wird gesichert und wiederhergestellt; Sicherungen älterer Versionen (ohne Spalte) bleiben einspielbar.

### B. Erster Verbrauch ab Startstand
- B1 Die erste Volltankung eines Fahrzeugs liefert einen Verbrauch ab dem Start-Kilometerstand (Annahme: Tank beim Start voll); Teilbetankungen davor zählen mit.
- B2 Der Wert ist als „ab Startstand“ gekennzeichnet; ein Hinweis erklärt die Annahme.
- B3 Auswertung (Schritt 14), Hilfe und ADR 0009 (und 0010) werden angepasst.

### C. Tests und Auslieferung
- C1 Tests: Markenanzeige mit/ohne Marke, eigener Anzeigename (inkl. Sicherung/Wiederherstellung und Migration), Verbrauch ab Startstand; FlaUI: Anzeigename vergeben + Anzeige in Favoriten, erster Verbrauch.
- C2 Windows-Zwischenstand 0.1.20 unter `review-versions/` (nicht committen).
- C3 Optionale Härtungen aus Schritt 15: Fehlerart bei Quellfehlern der Sicherung (`Failed` statt `InvalidFile`), Datenschutzhinweis zu Stationen/Preisständen in der Hilfe, App-Version des Zwischenstands angleichen.

## Randbedingungen
iOS: `MtouchInterpreter=all` und `TrimmerRootAssembly` unverändert; keine neue Abhängigkeit; Tests rufen nie Produktivendpunkte (Mock-Server).
