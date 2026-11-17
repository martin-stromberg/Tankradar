# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- Paketgröße iOS nicht lokal messbar (kein Mac/CI-Build in dieser Session);
  als nicht-machbar im ADR dokumentiert.
- Entscheidung: Konfiguration beibehalten (kein Compiled Model) – zulässig,
  da das Issue eine Evaluation verlangt; Begründung und Risiken in ADR 0021.

## Hinweise

- Diff gegen Basisbranch: nur `docs/adr/0021-ef-compiled-model-und-ios-aot.md`.
- Evaluation: `dotnet ef dbcontext optimize` erfolgreich erzeugt (12 Dateien,
  ~53 KB) und verworfen; Messung Modellaufbau ~296 ms / Migration ~201 ms;
  Kernpunkt: Compiled Model beseitigt nicht die Laufzeit-Reflection
  (LINQ/Migrationen) → Interpreter + TrimmerRoots bleiben nötig.
- Verifikation: Unit 2162/2162, Integration 311/311 (Migrationen/Restore/
  Integrität unverändert grün).
- Akzeptanzkriterien #71 erfüllt.
