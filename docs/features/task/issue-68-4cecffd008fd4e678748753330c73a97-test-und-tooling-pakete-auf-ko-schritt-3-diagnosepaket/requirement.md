# Anforderung (Schritt 3, Issue #70)

Optionales, lokal erzeugtes Diagnosepaket, das der Anwender explizit freigibt.

## Akzeptanzkriterien (aus dem Issue)

- Diagnosedaten werden nur nach ausdrücklicher Benutzeraktion erzeugt/geteilt.
- Umfang und Zweck der Daten sind vor dem Teilen sichtbar.
- Redaktionsregeln sind durch Tests abgesichert.
- Keine unaufgeforderte Telemetrie oder Übertragung; Retention und Umfang begrenzt.

## Fachlicher Umfang

- In-Memory-Ringpuffer für Laufzeit-/Fehlerinformationen (nur Warnungen/Fehler,
  kapazitäts- und größenbegrenzt, kein persistenter Logspeicher).
- Paket (ZIP, lokal): Metadaten (App-/OS-Version, Gerätemodell, Laufzeitinfos)
  + redigierte Protokollauszüge + Inhaltsbeschreibung.
- Strikte Redaktion: konfigurierte API-Schlüssel (Tankerkönig, Routensuche),
  Schlüssel-/Token-Parameter, Koordinaten, Dateipfade, E-Mails,
  tokenartige Zeichenfolgen. Stationsnamen/Suchbegriffe/Belegfotos werden
  gar nicht erst erfasst (Architekturgarantie + Dokumentation).
- UI: Karte „Diagnose“ in den Optionen mit Erklärtext, Inhaltsvorschau und
  getrenntem Teilen-Schritt (System-Teilen/Speichern, kein Upload).
- Testmodus wie Sicherung: Ablage in `<Daten>/test-shared` statt Systemdialog.
