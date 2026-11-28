# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Kein GitHub-Release/Asset nach einem Release-Lauf; `gh release create`/`upload` vollständig entfernt (`release.yml` Z. 116–142, `staging-ci.yml` Z. 405–414) | Entfernung der Schritte/Jobs, `finalize`-/`rc-tag`-Job per `git tag` + `git push`, Verbot aller mutierenden `gh release`-Aufrufe in `validate-workflows.py` (`plan.md` Schritte 3–6) | `ReleaseCommandTests`-Umbau (Verbot-Erwartungen), `ReleaseMechanismRemovedTests` (Dateien weg, Workflows ohne `gh release`), Szenarien 1, 4, 7 | Abgedeckt |
| Release-Tags `vX.Y.Z`/`vX.Y.Z-rc.N` werden weiter erzeugt (Versionsbasis von `determine-next-version.mjs`) | Neuer `finalize`-Job (`release.yml`, nur `release_kind == 'automatic'` + `released == 'true'`), `rc-tag`-Job (`staging-ci.yml`); Korrektheitsannahme „GITHUB_TOKEN-Push triggert keinen Workflow" begründet | Szenarien 1, 3, 4 (reale Workflow-Läufe); kein automatisierbarer Unit-Test möglich | Abgedeckt |
| Einziges Auslieferungsergebnis: signierte iOS-`.ipa` in App Store Connect (TestFlight), Upload verbindlich | Neuer Input `require-upload` an `package-ios`; `'true'` in `release.yml`/`staging-ci.yml`; harte Fehlschlag-Pfade in „Determine signing mode" | Szenario 5 nur „statisch prüfbar; Workflow-Log" — **kein automatisierter Regressionstest**, dass beide Workflows `require-upload: 'true'` setzen | Lücke (Testnachweis) |
| „Already-released"-Erkennung auf Git-Tags umgestellt; Reparaturpfad (`upload-existing`, `EXPECTED_ASSETS`, `ghGetRelease`/`ghListReleases`) ersatzlos entfernt | `tagExists`-Injektion (`git tag --list`), vereinfachte `resolveReleaseVersion`-Logik, Outputs ohne `release_action` | Vier konkrete Skripttests in `resolve-release-version.test.mjs` (new-release, already-released, manual, no-releasable-commits) | Abgedeckt |
| `update.json` und Windows-Paket `release-win-x64.zip` verwerfen | Löschung von `.github/actions/build-and-package/` und `scripts/package-windows.ps1`; `local-ci.ps1`-Bereinigung (`-Package`, `-PackageVersion`, Schritt, Kopfdoku) | `ReleaseMechanismRemovedTests` (Nicht-Existenz der Dateien) | Abgedeckt |
| 52 Bestands-Releases einmalig löschen, Git-Tags vollständig erhalten (kein `--cleanup-tag`) | Umsetzungsschritt 10 + Programmablauf „Einmalige Löschung" + Runbook-Abschnitt in ADR 0023; Sequenzierung strikt nach Promotion auf `main` | Szenario 8 (reale Ausführung mit Nachweis `gh release list` leer + `git ls-remote --tags origin` unverändert) | Abgedeckt, mit Restlücke: keine Prüfung auf in-flight Workflow-Läufe (siehe unten) |
| Risikoentscheidung dokumentiert (Threat-Model, Rotationsweg, Betreiberhinweise) | ADR `docs/adr/0023-…` im bestehenden ADR-Format + Verknüpfung in `api-schluessel.md` | Dokumentations-Schritte 7–8; kein Test nötig | Abgedeckt |
| Berechtigungen geprüft: `contents: write` nur wo nötig | `release.yml` Workflow-Ebene bleibt (Tag-Push); `staging-ci.yml` Job-Level `contents: write` wandert zum `rc-tag`-Job, Workflow-Ebene bleibt `contents: read` | Durch `validate-workflows.py`-Gesamtlauf abgesichert | Abgedeckt |
| Scope-Ausschlüsse (Schlüsselmechanismus unverändert, keine Review-Automatisierung, kein Windows-Kanal, Versionslogik unverändert) | Alle vier Ausschlüsse mit Begründung und Status „bestätigt (2026-11-28)" dokumentiert | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] **Kein automatisierter Guard für `require-upload: 'true'`:** Es gibt keinen Test, der sicherstellt, dass `release.yml` (Job `ios`) und `staging-ci.yml` (Job `ios-prerelease`) `package-ios` dauerhaft mit `require-upload: 'true'` aufrufen — und dass der verbindliche Modus bei fehlenden `IOS_SIGNING_ENABLED`-/Signierungs-/API-Key-Secrets hart fehlschlägt. Würde der Input später entfernt oder auf `'false'` zurückgesetzt, entstünde wieder ein „Release ohne Auslieferung" — genau der Zustand, den Annahme 5 (bestätigt) verhindern soll. Szenario 5 ist nur „statisch prüfbar; Workflow-Log". Naheliegend: ein Test in `test_validate_workflows.py` (analog `ReleaseMechanismRemovedTests`), der die beiden `uses: ./.github/actions/package-ios`-Aufrufe auf `require-upload: 'true'` prüft. Bezug: Erwartete Wirkung „einziges sichtbares Auslieferungsergebnis: signierte `.ipa` in App Store Connect" + Annahme 5.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Gesamte Anforderung (CI-/Release-Ebene, keine App-UI-Änderung) | Keine E2E-Tests geplant | Nicht erforderlich — nachvollziehbar begründet im Plan: kein über die App-UI erreichbarer Benutzerfluss, kein Dialog, kein Laufzeitverhalten geändert; Funktionsnachweis über Skripttests, Workflow-Validierung und erste reale Pipeline-Läufe |

## Fehlende oder unvollständige Planbestandteile

- [ ] **Verwaiste Referenz auf gelöschtes Skript in `docs/help/Preisdaten/api-schluessel.md:73`:** Der Routing-Abschnitt („CI und Windows-Paket") nennt `scripts/package-windows.ps1` — eine Datei, die der Plan löscht. Der Plan nennt für diese Datei nur die Zeilen 40–44 (Artefakt-Tabelle) und 54–56 (Sicherheitshinweis); Zeile 73 würde nach Umsetzung auf ein nicht existierendes Skript verweisen. Ebenso bleibt der Kommentar in `staging-ci.yml:372–375` („sonst melden `update.json` und das Paket …") nach Entfernung des Paketpfads veraltet. (Historische Erwähnungen in `changes.log` und `docs/adr/0014-route-search.md:83` sind als Zeitdokumente akzeptabel.)
- [ ] **Runbook-Lücke: in-flight Workflow-Läufe vor der Löschung:** Der Betriebsschritt (Schritt 10, Szenario 8) sequenziert die Löschung korrekt nach der Promotion auf `main` — danach kann kein *neuer* Lauf mehr ein Release anlegen. Ein vor dem Merge **gestarteter** `release`-/`prerelease`-Lauf des alten Stands (Timeout bis 60 Min) könnte jedoch während/nach der Löschung fertig werden und ein Release wieder anlegen; die Verifikation `gh release list` leer direkt im Anschluss würde das nicht erfassen. Im Runbook ergänzen: vor der Löschung `gh run list` auf `in_progress`/`queued`-Läufe der Workflows `Release`/`Pre-Release` prüfen (bzw. verzögert nachverifizieren).

## Hinweise

- Die Sequenzierung der Löschung ist im Kern korrekt durchdacht: Nach der Promotion haben `main` (neues `release.yml`) und `staging` (bereits durch den Feature-Merge) keinen `gh release`-Pfad mehr; der Reparaturpfad, der fehlende Releases wieder angelegt hätte, existiert dann nicht mehr. Zwischenzeitlich auf `main` erzeugte Releases werden über die ausführungszeitnahe Enumeration per `gh release list` miterfasst. `--cleanup-tag` ist korrekt ausgeschlossen, da die Tags die Versionswahrheit von `determine-next-version.mjs` bilden.
- Alle im Codebestand vorhandenen `gh release`-/`update.json`-/`release-win-x64`-/`build-and-package`-/`package-windows`-Referenzen (`.github/`, `scripts/`, `docs/help/`, `docs/RELEASE_NOTES.md`, `changes.log`, `README.md`) sind bis auf die oben genannten zwei Reststellen im Plan erfasst; `src/` enthält verifiziert keinerlei Release-Bezug (kein Auto-Updater).
- `test_validate_workflows.py:28` (`artifacts/update.json` als erlaubter generischer `.json`-Upload-Pfad) darf bewusst bleiben — der Plan begründet das korrekt.
- `docs/adr/0014-route-search.md:83` und die historischen `changes.log`-Einträge sind Zeitdokumente und müssen nicht angepasst werden.
