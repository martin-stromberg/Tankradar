# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Diagnose-Artefakte der E2E-Tests fehlen (Vorlage Abschnitt 11.4 und Checklisten-Punkt „If the project has Playwright/E2E tests: tracing is actually wired up in the test fixture, and an ‚Upload … traces‘ step exists in both CI workflows that run them“).** Die Anforderung erklärt die Vorlage einschließlich Troubleshooting-Hinweisen und Checkliste für verbindlich. Das Projekt hat E2E-Tests (FlaUI), die in `pr-staging-ci.yml` und `staging-ci.yml` laufen. Die Vorlage verlangt für diesen Fall, dass die Testbasis Diagnosematerial erzeugt und die Workflows es mit `if: always()` hochladen. Der Code setzt das nicht um: `src/Tankradar.Tests.E2E/E2ETestBase.cs` erfasst bei Testläufen weder Screenshots noch UI-Baum noch sonstige Diagnosedaten. Beide Workflows laden für E2E nur die `.trx`-Datei hoch. Genau diesen Fall beschreibt 11.4 als Fehlerbild („the only evidence is a bare pass/fail line in the trx logger output“). Die Abweichung ist auch nicht als bewusste Abweichung unter „Abweichungen von der Vorlage“ in `docs/help/ci-cd/workflows.md` begründet und dokumentiert. Das ist gerade bei den best-effort laufenden E2E-Tests auf gehosteten Runnern relevant, weil Fehlschläge dort sonst nicht nachvollziehbar sind.

## Hinweise

**Praktisch verifiziert (ohne Seiteneffekte):**

- `scripts/local-ci.ps1 -SkipE2E`: erfolgreich (Exit-Code 0). Bestanden haben Node-Tests, Workflow-Validierung, Restore, Formatprüfung, Sicherheitsprüfung (keine anfälligen Pakete) und die statische Analyse mit 0 Warnungen und 0 Fehlern. Dabei wurden alle Zielplattformen gebaut, auch `net10.0-ios` (iossimulator), MacCatalyst, Android und Windows. Unit-Tests (15/15) und Integrationstests (3/3) liefen grün, die Zeilenabdeckung beträgt 73,2 % bei einer Schwelle von 70 %.
- `npm test`: 24/24 Tests grün, darunter der Regressionstest zu 11.1, „erste Version ist 0.1.0“ und „niemals automatische Anhebung auf 1.0.0“.
- `node scripts/determine-next-version.mjs --rc` liefert auf dem aktuellen Stand `version=0.1.0` und `rc_tag=v0.1.0-rc.1`.
- `actionlint` ist lokal nicht verfügbar. Die strukturelle Prüfung erfolgte nur über `scripts/validate-workflows.py` (OK, 7 Workflows) und durch Lesen der Dateien.
- Der FlaUI-E2E-Lauf und das Windows-Paket (`-Package`) wurden in dieser Prüfung nicht ausgeführt.

**Erfüllte Anforderungsaspekte (im Code wiedergefunden):**

- Alle sieben Workflows und die gemeinsamen Bausteine `security-scan` und `build-and-package` sind vorhanden. Dazu kommt `build-ios`.
- Das Branch-Modell ist umgesetzt: `verify-pr-source.yml` lässt PRs nach `main` nur von `staging` zu.
- Das hybride Jobmodell `static checks` / `build & test` hat kein `needs` zwischen den beiden Jobs. Die Back-Merge-Erkennung ist vorhanden.
- Folgende Prüfungen laufen:
  - Formatprüfung
  - Sicherheitsprüfung (blockierend)
  - statische Analyse (`TreatWarningsAsErrors`)
  - Unit- und Integrationstests (blockierend)
  - FlaUI-E2E-Tests (best-effort, ohne Coverage)
  - Mindestabdeckung von 70 % in PR-CI und Pre-Release
- Ein Push auf `staging` erzeugt `vX.Y.Z-rc.N`. Dabei fließt `rc_version` korrekt in `build-and-package` (11.2). Ein Push auf `main` oder ein manueller Tag erzeugt das finale Release. Der Reparaturpfad schließt Pre-Releases aus (11.1). Releases entstehen einheitlich über die `gh` CLI (11.3).
- Release-Artefakt ist das self-contained Windows-ZIP `release-win-x64.zip` (unpackaged, `WindowsPackageType=None`) mit `update.json`. Ein Installer wird nicht erzeugt.
- iOS wird mit Mindestversion 16 gebaut, sowohl in PR-CI als auch in Pre-Release und Release. Ohne vollständige Signierungsdaten wird unsigniert (Simulator) gebaut und der Lauf scheitert nicht.
- Die Versionierung beginnt bei 0.1.0. Vor 1.0 heben Breaking Changes und `feat` nur die Minor-Version an. Der Ersatz von semantic-release ist begründet und dokumentiert.
- Geheimnisse und umgebungsspezifische Werte kommen ausschließlich aus `secrets.*` bzw. `vars.*`:
  - API-Schlüssel
  - Zertifikat und Provisioning-Profil
  - Bundle-ID
  - Team-ID
- Der lokale Prüflauf und seine Dokumentation (`docs/help/ci-cd/lokaler-pruefung.md`) sind vorhanden.
- Die Einrichtungs-Checkliste (`docs/help/ci-cd/einrichtung.md`) deckt ab:
  - Branches
  - Branch-Schutz
  - Labels
  - Secrets und Variablen
  - Kaltstart (11.5)
  - Merge-Methode
- Nacharbeiten zu Schritt 2:
  - `.githooks/install-hooks.sh` hat im Index Modus `100755`.
  - `checks.md`, der Docstring von `format-code-style-check.py` und der `changes.log`-Eintrag zu Schritt 2 stimmen mit dem tatsächlichen Hook-Verhalten überein. Die Formatprüfung läuft in pre-commit über die gesamte Solution und blockiert (`--strict`). Commit-Nachrichten werden nur in pre-push geprüft, und es gibt keinen `commit-msg`-Hook.

**Beobachtungen ohne Abweichungscharakter:**

- `sync-staging-with-main.yml` ermittelt `commits_behind`, nutzt den Wert aber nicht. Ist `main` ausnahmsweise nicht vor `staging`, scheitert `gh pr create` („No commits between …“). Die Vorlage verhält sich genauso, in der Praxis liegt `main` nach einer Promotion immer vorn.
- Pushes auf `staging` zwischen einem Release und dem Merge des Backmerge-PR sehen den neuen Release-Tag noch nicht. Sie erzeugen deshalb erneut RCs der bereits veröffentlichten Version. Das ist dokumentiert: Backmerge zeitnah mit „Create a merge commit“ mergen.
- Ob die Xcode-Version von `macos-latest` zur iOS-Workload von .NET 10 passt, lässt sich lokal nicht prüfen. Mit der Variable `IOS_XCODE_VERSION` gibt es eine Stellschraube dafür.
- `coverlet.runsettings` schließt UI-Glue-Code von der Abdeckung aus: `*.xaml.cs`, `MauiProgram.cs`, `Platforms/**` und `Views/TankradarContentPage.cs`. Das ist nachvollziehbar begründet, sollte aber bei künftigen Schritten nicht auf Fachlogik ausgeweitet werden.
