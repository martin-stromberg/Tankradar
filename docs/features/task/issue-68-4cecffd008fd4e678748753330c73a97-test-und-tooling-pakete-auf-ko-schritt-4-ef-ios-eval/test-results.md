# Testergebnisse – Schritt 4: EF-/iOS-Evaluation (#71)

## Evaluation

- `dotnet ef dbcontext optimize` (Windows-TFM): **erfolgreich** – 12 Dateien,
  ~53 KB generiertes Compiled Model in ein Scratch-Verzeichnis; danach
  verworfen (Entscheidung: nicht übernehmen, siehe ADR 0021).
- Temporäre Messung (einmalig ausgeführt, Datei entfernt):
  `CTOR=56ms MODEL=296ms MIGRATE=201ms SECOND_CTX=12ms FIRST_QUERY=109ms`,
  8 Entitätstypen, 9 Migrationen.

## Tests

- Unit-Tests: **2162/2162 bestanden** (unverändert – kein Code-Delta).
- Integrations-Tests (Migrationen, Restore, Datenbankintegrität):
  **311/311 bestanden**.
- E2E: kein E2E-Delta in diesem Schritt (keine UI-/Persistenzänderung);
  die Suite läuft wie in Schritt 1–3 über die PR-CI.

## Ergebnis

**Alle Tests bestanden; Migrationen/Restore/Integrität unverändert grün.**
