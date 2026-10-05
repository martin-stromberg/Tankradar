# Plan

1. App (nur Testmodus und nur bei `TANKATLAS_TEST_WINDOW=offscreen`): Fenster nach (-32000,-32000), `WS_EX_NOACTIVATE|WS_EX_TOOLWINDOW`, kein Vordergrund. Entscheidungslogik als testbare Klasse `TestWindowMode`.
2. E2E: Basis setzt `TANKATLAS_TEST_WINDOW=offscreen`; Rückfall per `TANKRADAR_E2E_WINDOW=foreground`. `NavigateToTab` ohne Mausklick (SelectionItem/Invoke). Screenshot direkt vom Fenster (PrintWindow).
3. Hooks: `test-execution-check.py` schließt E2E-Projekt standardmäßig aus (Unit+Integration); `PRE_PUSH_E2E=1` schaltet ein. Python-Tests.
4. CI: `pr-staging-ci.yml` und `staging-ci.yml` E2E blockierend. `local-ci.ps1` E2E blockierend. validate-workflows prüfen.
5. Stabilitätsnachweis: lokal mindestens fünf grüne Läufe Off-Screen; Entscheidung in Doku.
6. Doku (workflows.md Abweichung von der CI-Vorlage, lokaler Prüflauf, Hooks) und README.

## Offene Punkte

(keine)
