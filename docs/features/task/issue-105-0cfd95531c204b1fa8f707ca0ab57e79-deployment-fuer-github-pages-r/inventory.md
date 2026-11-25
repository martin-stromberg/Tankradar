# Bestandsaufnahme: Deployment für GitHub Pages reparieren (Issue #105)

Analysiert wurde der Workflow `.github/workflows/pages.yml` („GitHub
Pages") und sein CI/CD-Umfeld bezogen auf die Anforderung, den
fehlschlagenden Schritt `actions/deploy-pages@v5` zu reparieren
(„No artifacts named github-pages"). Kein Anwendungscode ist betroffen.

## Zusammenfassung

- **`pages.yml` besteht aus einem einzigen Job `deploy`**, in dem
  `actions/upload-pages-artifact@v5` (erzeugt das Artifact
  `github-pages` aus `pages/`) und `actions/deploy-pages@v5`
  (`id: deployment`) unmittelbar aufeinander laufen — ohne `needs`,
  Warte- oder Retry-Schritt. Trigger: Push auf `main` (Path-Filter
  `pages/**` + Workflow-Datei) und `workflow_dispatch`; Environment
  `github-pages` mit `steps.deployment.outputs.page_url`;
  `concurrency`-Gruppe `github-pages` ohne Abbruch.
- **Fehllauf `45541447133` (2026-11-25, `main`, `9af51e7`):** Das
  Artifact `github-pages` wurde um 03:39:21,99 UTC finalisiert
  (ID `14989192493`, 371 130 Bytes, im Run weiterhin vorhanden); der
  run-scoped Metadata-Lookup von `deploy-pages` um 03:39:22,46 UTC —
  ~0,48 s später — meldete „Found 0 artifact(s)". Im Erfolgslauf
  `44993397486` (2026-11-22) lag der gleiche Abstand bei ~0,31 s und der
  Lookup fand das Artifact („Found 1"). Das Fehlerbild ist damit
  nicht-deterministisch/intermittierend.
- **Einzige Änderung seit dem letzten Erfolgslauf:** Dependabot-Bump
  `actions/configure-pages` `v5 → v6` (Commit `630b915`, Merge `00ec4e4`,
  PR #102). `configure-pages` hat keinen Bezug zur Artifact-Erstellung;
  ein Kausalzusammenhang ist nicht belegt.
- **Upstream deckt das Bild exakt ab:** `actions/deploy-pages#451`
  (offen) beschreibt genau diesen intermittierenden Same-Job-Fehler; der
  Fix-PR `#455` (Retry im Artifact-Lookup) ist offen und unmerged.
  `deploy-pages@v5` läuft bereits auf dem neuesten Release `v5.0.1`
  (SHA-identisch) — ein Versionspin auf etwas Neueres ist faktisch
  nicht möglich. Die im Log sichtbare `DEP0040 punycode`-Warnung ist
  bekannt (`#413`) und kosmetisch.
- **Einrichtung ist korrekt:** Die Pages-API meldet
  `build_type: workflow` (Source „GitHub Actions"), URL
  `https://martin-stromberg.github.io/Tankradar/`; Berechtigungen
  `pages: write` + `id-token: write` stehen im Workflow. Es ist der
  **erste Fehlschlag auf `main`** seit Inbetriebnahme (Issue #34); die
  beiden älteren Fehlschläge vom 2026-11-04 stammen aus der
  Einrichtungsphase auf `staging`.
- **Ausrollweg vorgegeben:** `verify-pr-source.yml` lässt nur PRs von
  `staging` nach `main` zu; `staging-to-main-promotion.yml` erzeugt den
  Promotion-Draft-PR. Da `pages.yml` im eigenen Path-Filter steht,
  startet der Workflow beim Erreichen von `main` unmittelbar ein
  Verifikationsdeployment.
- **Struktur-Validierung vorhanden:** `scripts/validate-workflows.py`
  (mit `check_workflow`, u. a. `needs`-Referenz-Prüfung) und
  `scripts/test_validate_workflows.py` prüfen die Workflow-Dateien —
  bei einem Umbau auf ein Zwei-Job-Layout greifen beide automatisch.

Test-Ausgangszustand: **Alle ausgeführten Suiten grün** — `npm test`
32/32, `test_validate_workflows.py` 14/14, `validate-workflows.py`
Exit 0, iOS-Deployment-Skriptprüfung OK, .NET Unit 2155/2155,
Integration 199/199, E2E (Category!=OsInterface) 2/2. Kein nachgewiesener
bestehender Testfehler. Nicht ausgeführt: die best-effort
OS-Interface-Lane (Category=OsInterface, CI-seitig nicht-blockierend),
Format-/Security-Checks; `actionlint` fehlt lokal. Details und Nachweise:
[Tests](inventory/tests.md).

## Details

- [Workflow `pages.yml` und Workflow-Kontext](inventory/workflow.md) —
  Dateiaufbau, Schritte, Dateihistorie, verwandte Workflows,
  Dependabot-Konfiguration, Dokumentationsstellen
- [Fehlgeschlagener Lauf und Laufhistorie](inventory/failed-run.md) —
  Zeitlinie des Fehllaufs `45541447133` mit Artifact-Nachweis,
  Vergleich mit dem Erfolgslauf `44993397486`, komplette Laufhistorie,
  aufgelöste Action-SHAs
- [Umgebung, Action-Versionen und Upstream](inventory/upstream.md) —
  Pages-Einstellungen, neueste Action-Releases, einschlägige offene
  Upstream-Issues/PRs, lokale Werkzeug- und Git-Fakten
- [Tests und Test-Ausgangszustand](inventory/tests.md) — alle Testläufe
  mit Exit-Codes, Zählungen und Nachweisen unter
  `inventory/test-results/`
