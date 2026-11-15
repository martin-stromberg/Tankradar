# Offene Aufgaben

Erstellt am: 2026-11-14
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Task 10 — `workflow_dispatch`-Probelauf je Workflow auf GitHub (Issue + Dedup prüfen, Ergebnis dokumentieren). Plattformbedingt erst möglich, sobald die Workflows auf dem Default-Branch `main` liegen (nach Promotion `staging → main`); `gh workflow run` auf dem Task-Branch wurde mit HTTP 404 verifiziert.

## Code-Review-Befunde

Keine.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] `ReleaseCommandTests.test_workflows_in_repository_are_clean` — 2 preexisting `e2e-dumps`-Befunde in `pr-staging-ci.yml`/`staging-ci.yml` (bekannter Defekt aus Issue #22, bewusster Scope-Ausschluss; nicht durch diese Änderung verursacht).
- [ ] Reale End-to-End-Abnahme der Akzeptanzszenarien 1, 2, 3, 5 — `Nicht durchführbar` vor Promotion auf `main` (Plattformverhalten von `schedule`/`workflow_dispatch`); Szenario 4 (Inertheit vor Promotion) bestanden.
