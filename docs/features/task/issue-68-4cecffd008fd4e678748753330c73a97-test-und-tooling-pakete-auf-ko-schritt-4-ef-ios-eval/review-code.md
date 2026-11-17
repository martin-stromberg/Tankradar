# Code-Review – Schritt 4: EF-/iOS-Evaluation (#71)

Selbstreview (kein Subagent-Mechanismus verfügbar – dokumentierte Abweichung).

## Geprüfte Änderungen

- `docs/adr/0021-ef-compiled-model-und-ios-aot.md` (einzige dauerhafte Änderung).
- Kein Produktivcode geändert – die Evaluation ergab „Konfiguration beibehalten“.

## Befunde

### Geprüft und für gut befunden

- **Echte Evaluation statt Dokumentations-Selbstzweck:** `dotnet ef dbcontext
  optimize` wurde tatsächlich ausgeführt (12 Dateien, ~53 KB generiert) und
  danach wieder verworfen – die Generierbarkeit ist nachgewiesen, kein
  bloßer Verweis auf die Dokumentation.
- **Messwerte real erhoben:** Modellaufbau ~296 ms, Migration ~201 ms,
  Folgekontext ~12 ms (Windows, Release); temporärer Messtest wurde nach
  Aufzeichnung gelöscht und ist nicht im Commit.
- **Kernargument stichhaltig:** Ein Compiled Model eliminiert nicht die
  Laufzeit-Reflection von EF (LINQ-Kompilierung, Migrationsauflösung);
  `MtouchInterpreter` und TrimmerRoots blieben nötig. Der Nutzen beschränkt
  sich auf den einmaligen Modellaufbau.
- **ADR-Form** entspricht den übrigen ADRs (Status, Kontext, Entscheidung,
  Alternativen, Risiken, Wiederanlauf-Kriterien).
- **Risiko des Regenerations-Workflows** ist zutreffend benannt (veraltetes
  generiertes Modell bei vergessenem `dbcontext optimize`).

## Ergebnis

**Keine offenen Befunde.**
