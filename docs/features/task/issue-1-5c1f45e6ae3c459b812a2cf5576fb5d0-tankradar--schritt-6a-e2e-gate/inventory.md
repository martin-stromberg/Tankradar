# Bestandsaufnahme

- `src/Tankradar.Tests.E2E/E2ETestBase.cs`: startet App, `NavigateToTab` (SelectionItem/Click im Wechsel), Diagnose bei Fehlschlag.
- `src/Tankradar.Tests.E2E/E2EDiagnostics.cs`: Screenshot via `Capture.Element` (Bildschirmausschnitt, offscreen unbrauchbar).
- Tests bedienen die App bereits über Invoke/Toggle/SelectionItem; einziger Mausklick: Fallback in `NavigateToTab`.
- App: `Platforms/Windows/App.xaml.cs`, `MauiProgram.cs`; Testmodus über `TANKATLAS_TEST_DATA_PATH` (`TestSupport/TestDataPaths.cs`).
- `.githooks/pre-push` ruft `test-execution-check.py` (build + `dotnet test --no-build` der ganzen Solution).
- `scripts/local-ci.ps1` Schritt FlaUI-E2E best-effort; `pr-staging-ci.yml` und `staging-ci.yml` E2E `continue-on-error`.
- Doku: `docs/help/ci-cd/workflows.md`, `lokaler-pruefung.md`, `git-hooks/checks.md`.
