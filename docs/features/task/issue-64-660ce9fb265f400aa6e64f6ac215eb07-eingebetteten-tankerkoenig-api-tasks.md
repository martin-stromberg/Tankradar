# Tasks: Ablage der Releases aus GitHub entfernen, Auslieferung nur über den App Store (Issue #64)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Skripte | `scripts/resolve-release-version.mjs` vereinfachen: `EXPECTED_ASSETS`, `releaseHasExpectedAsset`, `incompleteReleases`, `gh*`/`ghGetRelease`/`ghListReleases`, Reparaturpfad und `release_action` entfernen; `tagExists`-Injektion (`git tag --list`) einführen | Offen | — |
| 2 | Skripte | `scripts/resolve-release-version.test.mjs` umschreiben: `tagExists`-Stubs, Szenarien new-release/already-released/no-releasable-commits/manual | Offen | — |
| 3 | iOS-Action | `.github/actions/package-ios/action.yml`: Input `require-upload` (Default `'false'`) deklarieren; „Determine signing mode" bei `'true'` auf harte Fehlschläge (`::error::` + Exit 1) statt unsigned-Rückfall/Notice umstellen; Beschreibung ergänzen | Offen | — |
| 4 | Workflow | `release.yml`: Schritte „Check out release tag for asset repair", „Build and package", „Create GitHub release", „Upload asset to existing GitHub release" entfernen; `GH_TOKEN`-Env am Resolve-Schritt streichen | Offen | — |
| 5 | Workflow | `release.yml`: Job-Outputs bereinigen (`release_action` raus, `release_kind` neu exponieren); alle `release_action == 'create'`-Bedingungen auf `released == 'true'` vereinfachen | Offen | — |
| 6 | Workflow | `release.yml`: Job `finalize` (ubuntu-latest, `needs: [release, ios]`, `if: released=='true' && release_kind=='automatic'`) mit `git tag` + `git push origin <tag>` anlegen | Offen | — |
| 7 | Workflow | `release.yml`: Job `ios` — `if` auf `released == 'true'` vereinfachen, `package-ios` mit `require-upload: 'true'` aufrufen | Offen | — |
| 8 | Workflow | `staging-ci.yml`: Job `prerelease` durch `rc-tag` ersetzen (ubuntu-latest, `permissions: contents: write`, `git tag` + `git push origin <rc_tag>`); Windows-Paketbau und `gh release create --prerelease` entfernen | Offen | — |
| 9 | Workflow | `staging-ci.yml`: Job `ios-prerelease` — `package-ios` mit `require-upload: 'true'` aufrufen | Offen | — |
| 10 | Entfernung | Verzeichnis `.github/actions/build-and-package/` löschen | Offen | — |
| 11 | Entfernung | `scripts/package-windows.ps1` löschen | Offen | — |
| 12 | Entfernung | `scripts/local-ci.ps1`: Parameter `-Package`/`-PackageVersion`, Paket-Schritt und Kopfdoku-Erwähnungen entfernen | Offen | — |
| 13 | Validierung | `scripts/validate-workflows.py`: `check_release_commands` zum harten Verbot von `gh release create/upload/edit/delete` umbauen; `gh_release_assets` und `GH_VALUE_OPTIONS` entfernen; Docstring anpassen | Offen | — |
| 14 | Validierung | `scripts/test_validate_workflows.py`: `ReleaseCommandTests` auf Verbot-Erwartungen umbauen; `WindowsPackageKeyPolicyTests` entfernen; `ReleaseMechanismRemovedTests` neu (Dateien weg, Workflows ohne `gh release`) | Offen | — |
| 15 | Dokumentation | ADR `docs/adr/0023-release-verteilung-und-tankerkoenig-schluessel.md` anlegen (Entscheidung, Threat-Model, Rotationsweg, Betreiberhinweise) | Offen | — |
| 16 | Dokumentation | `docs/help/ci-cd/workflows.md` aktualisieren (Workflow-/Baustein-/Skript-Tabellen, Abweichung „gh CLI") | Offen | — |
| 17 | Dokumentation | `docs/help/ci-cd/README.md` aktualisieren (Ablauf-Schritte 2/4, Tabelle „Release-Artefakte", Secret-Verhalten) | Offen | — |
| 18 | Dokumentation | `docs/help/ci-cd/einrichtung.md` aktualisieren (Prüf-Checkliste, Verhalten der Pipeline mit `require-upload`) | Offen | — |
| 19 | Dokumentation | `docs/help/ci-cd/versionierung.md` aktualisieren (kein `--generate-notes`, Tags per Workflow-Push, RC ohne `update.json`) | Offen | — |
| 20 | Dokumentation | `docs/help/ci-cd/lokaler-pruefung.md` aktualisieren (`-Package`-Beispiel/-Zeile entfernen) | Offen | — |
| 21 | Dokumentation | `docs/help/Preisdaten/api-schluessel.md` aktualisieren (Artefakt-Tabelle ohne Windows-Paket, Verweis auf ADR 0023) | Offen | — |
| 22 | Dokumentation | `docs/RELEASE_NOTES.md` (EN+DE) und `changes.log` um den Verteilungswechsel ergänzen; `README.md`-Docs-Verweis ggf. redaktionell anpassen | Offen | — |
| 23 | Tests | Gesamtprüfung: `npm test`, `python scripts/test_validate_workflows.py`, `python scripts/validate-workflows.py`, `pwsh scripts/test-ios-deployment.ps1` | Offen | — |
| 24 | Betrieb | Bestands-GitHub-Releases löschen (einmalig, **nach** Merge/Promotion der Workflow-Änderungen auf `main`): `git ls-remote --tags origin` (Soll erfassen) → `gh release list` → Schleife `gh release delete <tag> --yes` **ohne** `--cleanup-tag` (Tags bleiben); Nachweis `gh release list` leer + Tag-Anzahl unverändert. Ausführender: Betreiber/Agent mit `repo`-Scope | Offen | — |
