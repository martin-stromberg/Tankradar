# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

Zweite Prüfrunde: Die drei Befunde aus `plan-check.1.md` (fehlender `require-upload`-Test-Guard, verwaiste Referenzen auf `package-windows.ps1`/`update.json`, fehlende In-flight-Run-Prüfung im Lösch-Runbook) sind vollständig und korrekt in `plan.md` eingearbeitet. Verbleibend ist eine kleinere Restlücke derselben Befundklasse: `docs/help/ci-cd/einrichtung.md` enthält weitere veraltete Aussagen außerhalb der im Plan genannten Zeilenbereiche.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Kein GitHub-Release/Asset nach einem Release-Lauf; `gh release create`/`upload` vollständig entfernt (`release.yml` Z. 116–142, `staging-ci.yml` Z. 405–414 — gegen Code verifiziert) | Entfernung der Schritte/Jobs, `finalize`-/`rc-tag`-Job per `git tag` + `git push`, Verbot aller mutierenden `gh release`-Aufrufe in `validate-workflows.py` (Umsetzungsschritte 3–6) | `ReleaseCommandTests`-Umbau (Verbot-Erwartungen), `ReleaseMechanismRemovedTests` (Dateien weg, Workflows ohne `gh release`), Szenarien 1, 4, 7 | Abgedeckt |
| Release-Tags `vX.Y.Z`/`vX.Y.Z-rc.N` werden weiter erzeugt (Versionsbasis von `determine-next-version.mjs`) | Neuer `finalize`-Job (`release.yml`, nur `release_kind == 'automatic'` + `released == 'true'`, `needs: [release, ios]` — Tag „verbraucht" die Version erst nach erfolgreichem Upload), `rc-tag`-Job (`staging-ci.yml`, spiegelt bisherige Parallel-Semantik); Korrektheitsannahme „GITHUB_TOKEN-Push triggert keinen Workflow" begründet und als Kommentar vorgesehen | Szenarien 1, 3, 4 (reale Workflow-Läufe); kein automatisierbarer Unit-Test möglich | Abgedeckt |
| Einziges Auslieferungsergebnis: signierte iOS-`.ipa` in App Store Connect (TestFlight), Upload verbindlich | Neuer Input `require-upload` an `package-ios` (Default `'false'`); `'true'` in `release.yml`-Job `ios` und `staging-ci.yml`-Job `ios-prerelease`; harte Fehlschlag-Pfade in „Determine signing mode" (Z. 84–117); `pr-staging-ci.yml` behält den secret-losen Compile-Check | **`RequireUploadTests` (neu, Z. 166/274):** YAML-geladene Workflows — `with['require-upload'] == 'true'` in beiden Jobs + Input-Deklaration in `package-ios/action.yml`; schlägt bei stillem Rückbau fehl | Abgedeckt (Runde-1-Lücke geschlossen) |
| „Already-released"-Erkennung auf Git-Tags umgestellt; Reparaturpfad (`upload-existing`, `EXPECTED_ASSETS`, `ghGetRelease`/`ghListReleases`) ersatzlos entfernt | `tagExists`-Injektion (`git tag --list <tag>`, Checkout liefert Tags via `fetch-tags: true`), vereinfachte `resolveReleaseVersion`-Logik, Outputs ohne `release_action`, `GH_TOKEN`-Env entfernt | Vier konkrete Skripttests in `resolve-release-version.test.mjs` (new-release, already-released, manual ohne `determineVersion`-Aufruf, no-releasable-commits) | Abgedeckt |
| `update.json` und Windows-Paket `release-win-x64.zip` verwerfen | Löschung von `.github/actions/build-and-package/` und `scripts/package-windows.ps1`; `local-ci.ps1`-Bereinigung (`-Package`, `-PackageVersion`, Schritt Z. 232–239, Kopfdoku Z. 26/33 — gegen Code verifiziert) | `ReleaseMechanismRemovedTests` (Nicht-Existenz der Dateien; alle Workflows ohne `gh release`) | Abgedeckt |
| 52 Bestands-Releases einmalig löschen, Git-Tags vollständig erhalten (kein `--cleanup-tag`) | Umsetzungsschritt 10 + eigener Programmablauf + Runbook-Abschnitt in ADR 0023; Sequenzierung strikt nach Promotion auf `main`; **In-flight-Prüfung** `gh run list --workflow release.yml|staging-ci.yml --status in_progress|queued` vor dem Löschen; ~60-Min-Beobachtungsfrist mit wiederholter Leer-Prüfung | Szenario 8 (reale Ausführung mit Nachweis `gh release list` leer — wiederholt — + `git ls-remote --tags origin` unverändert) | Abgedeckt (Runde-1-Lücke geschlossen) |
| Risikoentscheidung dokumentiert (Threat-Model, Rotationsweg, Betreiberhinweise) | ADR `docs/adr/0023-…` im bestehenden ADR-Format (nächste Nummer verifiziert: letzter ADR `0022-android-backup.md`); Verknüpfung in `api-schluessel.md` (vorhandener Sicherheitshinweis Z. 54–56 wird ergänzt statt dupliziert); Rotationsweg via `FUEL_PRICE_API_KEY` + `ApiKeyProvider`-Überschreibung (`ApiKeys.cs:117–128`) | Dokumentations-Schritte 7–8; kein Test nötig | Abgedeckt |
| Verwaiste Referenzen auf entfernte Mechanismen in Kommentaren/Dokumentation | `staging-ci.yml`-Kommentare Z. 372–375 (`update.json`/Paket) und Z. 416–417 (stiller Rückfall) sowie `release.yml`-Kommentar Z. 144–145 jetzt explizit erfasst; `api-schluessel.md` Z. 73 (Routing-Abschnitt → `package-windows.ps1`) jetzt erfasst — **aber:** `einrichtung.md` enthält weitere stale Aussagen außerhalb der genannten Zeilenbereiche | — | Lücke (siehe unten) |
| Berechtigungen geprüft: `contents: write` nur wo nötig | `release.yml` Workflow-Ebene bleibt (Tag-Push); `staging-ci.yml` Job-Level `contents: write` wandert zum `rc-tag`-Job, Workflow-Ebene bleibt `contents: read` | Durch `validate-workflows.py`-Gesamtlauf abgesichert | Abgedeckt |
| Scope-Ausschlüsse (Schlüsselmechanismus unverändert, keine Review-Automatisierung, kein Windows-Kanal, Versionslogik unverändert) | Alle vier Ausschlüsse mit Begründung und Status „bestätigt (2026-11-28)" dokumentiert | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine verbleibenden Lücken. Die in Runde 1 geforderte `require-upload`-Absicherung ist mit `RequireUploadTests` (YAML-basierter statischer Guard über `MODULE.load`, analog `WindowsPackageKeyPolicyTests`) vollständig geplant: Er prüft `with['require-upload'] == 'true'` in `release.yml` Job `ios` und `staging-ci.yml` Job `ios-prerelease` sowie die Input-Deklaration in `package-ios/action.yml`. Alle übrigen Akzeptanzkriterien haben konkrete Testnachweise (Skripttests, Validierungstests, reale Pipeline-Läufe als Abnahme).

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Gesamte Anforderung (CI-/Release-Ebene, keine App-UI-Änderung) | Keine E2E-Tests geplant | Nicht erforderlich — nachvollziehbar begründet im Plan: kein über die App-UI erreichbarer Benutzerfluss, kein Dialog, kein geändertes Laufzeitverhalten; Funktionsnachweis über Skripttests, Workflow-Validierung und erste reale Pipeline-Läufe auf `staging`/`main` |

## Fehlende oder unvollständige Planbestandteile

- [ ] **`einrichtung.md`: weitere veraltete Aussagen außerhalb der im Plan genannten Zeilenbereiche.** Der Plan nennt für `docs/help/ci-cd/einrichtung.md` nur Z. 85–91 („Verhalten der Pipeline") und Z. 110–113 (Prüf-Checkliste). Nicht erfasst sind:
  - **Z. 38** (Abschnitt 4, Apple-Vorbereitung): „Ohne sie baut die Pipeline iOS weiterhin unsigniert als Compile-Prüfung." — falsch ab `require-upload='true'`: Release- und Staging-Läufe schlagen dann fehl statt unsigniert zu bauen.
  - **Z. 72** (Absatz unter der Secrets-Tabelle): „… das Windows-Paket wird ohne Schlüssel gebaut …" — das Windows-Paket entfällt ersatzlos; die Aussage ist obsolet.
  - **Z. 81** (Variablen-Tabelle, `IOS_SIGNING_ENABLED`): „jeder andere Wert oder fehlende Variable = unsignierter Compile-Check" — widerspricht der `require-upload`-Semantik für Release-/Staging-Läufe (gilt nur noch für `pr-staging-ci.yml`).
  - **Z. 21–22** (Branch-Schutz-Checkliste): „nötig für Promotion-/Backmerge-PRs und Releases" — „Releases" im Sinne von GitHub-Releases entfällt (gemeint bleiben Tag-Pushes); redaktionell.
  - Ergänzend: `docs/help/ci-cd/workflows.md` Z. 46 (`package-ios`-Bausteinzeile: „optional TestFlight-Upload … kein Fehlschlag") sollte den neuen `require-upload`-Input erwähnen — die Beschreibung bleibt für den Default `'false'` korrekt, ist aber ohne den verbindlichen Modus unvollständig.

  Dieselbe Befundklasse wie Runde 1 (`api-schluessel.md:73`): ein mechanisch an den genannten Zeilenbereichen arbeitender Umsetzer würde widersprechende Alt-Aussagen stehen lassen. Die Änderungen sind im bestehenden Dokumentationsschritt 8 mitzuerfassen.

## Hinweise

- Die drei Runde-1-Befunde sind verifiziert geschlossen: (a) `RequireUploadTests` samt konkreter Prüfmethodik; (b) `api-schluessel.md:73` und `staging-ci.yml:372–375` sind jetzt benannt; (c) das Lösch-Runbook enthält die In-flight-/Queued-Prüfung beider Workflows plus ~60-Minuten-Beobachtungsfrist mit wiederholter `gh release list`-Leerprüfung — das deckt auch verspätet endende Alt-Läufe (Timeout `release.yml`/`ios` 60 min, `prerelease` 45 min) ab.
- Mengenabweichung zwischen Bestandsaufnahme und Plan: `inventory.md` nennt 48 Releases/50 Tags (Stand der Aufnahme), `requirement.md`/Plan nennen 52 Releases (Anwender-Entscheidung 2026-11-28). Unkritisch: das Runbook enumeriert zur Laufzeit per `gh release list` und erfasst den Tag-Soll-Bestand vorab per `git ls-remote --tags origin`.
- `staging-to-main-promotion.yml` referenziert den Anzeigenamen `Pre-Release` nicht mehr per `workflow_run` (Trigger ist `push` auf `staging`) — die Umbenennung des Jobs `prerelease` → `rc-tag` hat keine externen Abhängigkeiten. Verifiziert: kein `prerelease`-/`Pre-Release`-Bezug in der Promotion-Workflow-Datei.
- `finalize`-Design ist korrekt: Der Tag wird erst nach erfolgreichem iOS-Upload gepusht (`needs: [release, ios]`); ein iOS-Fehlschlag lässt die Version unverbraucht und per Re-Run wiederholbar (Buildnummer via `run_attempt`). Der manuelle Tag-Weg (`release_kind == 'manual'`) überspringt `finalize` korrekt, da der auslösende Tag bereits existiert.
- `test_validate_workflows.py:80` (`test_workflows_in_repository_are_clean`) bleibt gültig und muss nicht angepasst werden — es prüft alle Repo-Workflows gegen die jeweils aktiven Regeln.
- `docs/features/task/issue-64-…-tasks.md` (veraltete Einzeldatei) sowie die Inventare/Reviews anderer Aufgaben (`issue-58`, `issue-68`, `issue-90`) erwähnen den alten Paketpfad weiterhin — Zeitdokumente, kein Handlungsbedarf (entspricht der Plan-Festlegung).
