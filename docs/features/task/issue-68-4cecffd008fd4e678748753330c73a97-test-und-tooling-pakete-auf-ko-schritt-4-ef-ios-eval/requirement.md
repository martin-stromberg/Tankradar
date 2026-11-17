# Anforderung – Schritt 4: EF-Compiled-Model- und iOS-AOT-/Trimming-Evaluation (#71)

Quelle: Issue #71 (Follow-up aus Architektur-/Security-Review #62).

## Aufgabe

- EF-Core-Compiled-Model für `TankradarDbContext` evaluieren (nicht blind anwenden).
- Aktuelle EF-Core-/.NET-MAUI-Empfehlungen für iOS AOT, Trimming und Reflection prüfen.
- Wo machbar messen: Startzeit-Anteil des Modellaufbaus, Paketgröße, relevante Datenbankpfade.
- Entscheidung in einem ADR dokumentieren, inkl. Risikoabschätzung beim Behalten der bestehenden Konfiguration.
- Migrationen, Restore und Datenbankintegrität müssen unverändert getestet bleiben.

## Akzeptanzkriterien

1. Evaluierter Nachweis (generiertes Modell oder begründeter Verzicht auf Generierung).
2. ADR mit Entscheidung, Alternativen, Messwerten (soweit machbar) und Risiken.
3. Keine Verschlechterung: Migrationen/Restore/Integritätstests bleiben grün.
