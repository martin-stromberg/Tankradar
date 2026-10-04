# Plan
1. `E2EDiagnostics` (Capture, DumpTree, ResolveDirectory `e2e-diagnostics/`) und `E2ETestBase.RunWithDiagnostics` + Startfehler-Erfassung.
2. Bestehenden Navigationstest in `RunWithDiagnostics` kapseln; neue Tests `DiagnosticsCaptureE2ETests`; Testparallelitaet deaktivieren.
3. Workflows `pr-staging-ci.yml`/`staging-ci.yml`: Schritt "Upload E2E diagnostics" (`if: always()`).
4. `scripts/local-ci.ps1`: Hinweis auf Diagnoseverzeichnis, Bereinigung vor Lauf; `.gitignore`.
5. Doku `docs/help/ci-cd/` (README, lokaler-pruefung, workflows).

## Offene Punkte
