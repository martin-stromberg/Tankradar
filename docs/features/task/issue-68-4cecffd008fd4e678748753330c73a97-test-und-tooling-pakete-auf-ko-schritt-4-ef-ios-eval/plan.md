# Plan – Schritt 4: EF-/iOS-Evaluation (#71)

1. Ist-Analyse: csproj (`MtouchInterpreter=all`, TrimmerRootAssembly-Liste, Kommentar zu "-all"-Variante), DbContext (8 DbSets, 9 Migrationen), DatabaseInitializer (`MigrateAsync`).
2. Compiled-Model-Versuch: `dotnet ef dbcontext optimize` gegen das Windows-TFM in ein Scratch-Verzeichnis; Umfang/Größe der erzeugten Dateien bewerten.
3. Messung: Zeit für den ersten `context.Model`-Aufbau (Runtime-Modell) als Proxy für den Startvorteil eines Compiled Models; in einem temporären Unit-Test messen, Ergebnis dokumentieren, Test wieder entfernen.
4. Guidance-Abgleich: EF-Core-Doku zu Compiled Models (Grenzen: Migrationen/Query-Kompilierung bleiben Laufzeit-Reflection) und MAUI-iOS-Interpreter-/Trimmer-Empfehlungen gegen die Ist-Konfiguration.
5. ADR 0021 schreiben (Entscheidung + Alternativen + Messwerte + Risiken).
6. Verifikation: Build, Unit-Tests, Integrations-Tests (Migrationen/Restore) – unverändert grün.
7. Reviews + Abnahme, Merge in den Projekt-Basisbranch.
