# Anforderung (übersetzt)

## Fachliche Zusammenfassung

Der Workflow „GitHub Pages" (`.github/workflows/pages.yml`), der die statische
Präsentationsseite unter `pages/` auf GitHub Pages veröffentlicht, schlägt im
Schritt `actions/deploy-pages@v5` fehl: Die Metadaten-Abfrage findet das im
selben Job zuvor erfolgreich von `actions/upload-pages-artifact@v5`
hochgeladene Artifact `github-pages` nicht („No artifacts named
`github-pages` were found for this workflow run"). Der Workflow ist so zu
korrigieren, dass das Deployment wieder zuverlässig durchläuft; die Korrektur
ist über den bestehenden Branching-Workflow (Task-Branch → PR gegen `staging`
→ Promotion `staging` → `main`) mit Admin-Merges bis auf `main` zu bringen,
damit die Page auf den aktuellen Stand geht.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein Lauf des Workflows „GitHub Pages"
  (`.github/workflows/pages.yml`), konkret das Zusammenspiel der Schritte
  `actions/upload-pages-artifact@v5` (erzeugt das Artifact `github-pages`)
  und `actions/deploy-pages@v5` (schlägt fehl, `id: deployment`) im einzigen
  Job `deploy`.
- **Auslösende Ereignisse:** Push auf `main` mit Änderungen an `pages/**`
  oder `.github/workflows/pages.yml` (Path-Filter); manuell per
  `workflow_dispatch`. Die Korrektur selbst ändert
  `.github/workflows/pages.yml` und löst damit beim Erreichen von `main`
  unmittelbar einen verifizierenden Lauf aus.
- **Erwartete Wirkung:** Der Job `deploy` läuft grün durch,
  `actions/deploy-pages` findet das Artifact `github-pages` und publiziert
  die Seite; die GitHub Page ist auf dem Stand des `main`-Branchs
  (Environment `github-pages` mit `steps.deployment.outputs.page_url`).
- **Umgang mit fehlenden Objekten:** Das Deployment-Artifact
  `github-pages` muss zum Zeitpunkt der run-scoped Metadaten-Abfrage von
  `deploy-pages` auffindbar sein. Das vom Upload-Schritt erzeugte Artifact
  ist wiederzuverwenden — es ist sicherzustellen (zeitlich oder per
  Wiederholung), dass der Lookup das eben finalisierte Artifact sieht. Kein
  neues fachliches Objekt ist zu erzeugen; ein Re-Run nur des fehlgeschlagenen
  Jobs ist zu vermeiden, weil dabei ein zweites `github-pages`-Artifact
  entsteht und der Lookup mit „Multiple artifacts" fehlschlägt
  (Upstream-Issue `actions/deploy-pages#338`).
- **Scope-Ausschlüsse:**
  - Keine inhaltliche oder gestalterische Änderung der Seite unter `pages/`
    — ausschließlich die Bereitstellung ist betroffen.
  - Keine Änderung am Branching-/Promotionsprozess selbst
    (`staging-to-main-promotion.yml` und Required Checks bleiben wie sie
    sind); Admin-Merges sind die vom Auftraggeber vorgegebene operative
    Vorgehensweise, kein Feature-Eingriff.
  - Kein Umbau der Pages-Infrastruktur; Voraussetzung bleibt
    Settings → Pages → Source „GitHub Actions" (siehe `pages/README.md`,
    bereits eingerichtet — frühere Läufe waren erfolgreich).
  - Kein nachträgliches Re-Deploy verpasster Zwischenstände — Ziel ist
    allein der aktuelle `main`-Stand.

## Betroffene Klassen und Komponenten

Kein C#-Code und keine App-Komponenten sind betroffen; der Eingriff liegt
ausschließlich in der CI/CD-Definition:

- `.github/workflows/pages.yml` — primär betroffenes Artefakt:
  Job-Struktur `deploy`, die Schritte `actions/checkout@v7`,
  `actions/configure-pages@v6`, `actions/upload-pages-artifact@v5`,
  `actions/deploy-pages@v5` sowie ggf. `permissions`, `concurrency` und
  `environment`.
- `pages/` — ausgelieferter Inhalt, fachlich unverändert.
- `pages/README.md` — ggf. Dokumentationsanpassung, falls sich die
  Workflow-Struktur (z. B. Zwei-Job-Layout) ändert.
- Referenzlauf: Run-ID 45541447133 (2026-11-25, Push auf `main`,
  nur `deploy-pages` rot; Upload des Artifacts erfolgreich,
  Artifact-ID 14989192493, 371130 Bytes).
- Tests: kein Unit-/Integrationstest-Bedarf. Validierung erfolgt über den
  tatsächlichen Workflow-Lauf, der durch den Push der geänderten
  `pages.yml` auf `main` ausgelöst wird.

## Implementierungsansatz

- **Analysestand (Annahme, in der Planung zu verifizieren):** Der Fehler
  entspricht dem bekannten, als intermittierend dokumentierten Race in
  `actions/deploy-pages` (Upstream-Issue `actions/deploy-pages#451`):
  Laufen `upload-pages-artifact` und `deploy-pages` im selben Job, kann die
  run-scoped `listArtifacts`-Abfrage das Bruchteile einer Sekunde zuvor
  finalisierte Artifact noch nicht sehen. Der `configure-pages`-Bump
  v5 → v6 (Dependabot PR #102) ist die einzige Änderung seit dem letzten
  Erfolgslauf (Run-ID 44993397486, 2026-11-22), enthält laut Release Notes
  aber nur Runtime- und Dependency-Upgrades (Node 24) — ein
  Kausalzusammenhang ist nicht belegt, der Race die wahrscheinlichere
  Erklärung.
- **Lösungsoptionen (Entscheidung in der Planung):**
  1. Zwei-Job-Layout nach dem offiziellen Starter-Workflow: `build`-Job
     mit `upload-pages-artifact`, separater `deploy`-Job (`needs: build`)
     mit `deploy-pages` — vergrößert den Abstand zwischen Finalisierung
     und Lookup strukturell; im Upstream-Issue als wirksamer Fix bestätigt.
  2. Retry-Härtung im selben Job: nach einem fehlgeschlagenen
     `deploy-pages`-Schritt (`steps.deployment.outcome == 'failure'`)
     eine kurze Wartezeit (z. B. `sleep 30`) und ein zweiter
     `deploy-pages`-Schritt; ein Step-Retry lädt nicht erneut hoch und
     erzeugt daher kein Duplikat-Artifact.
  3. Versionspin: neuere `deploy-pages`-Version mit Retry im Lookup bzw.
     `artifact_id`-Input (Upstream-Issue `actions/deploy-pages#399`),
     sofern verfügbar; `configure-pages` nur dann auf v5 zurücksetzen,
     wenn die Verifikation einen Kausalzusammenhang ergibt.
- **Ausrollpfad:** Änderung auf dem Task-Branch, PR gegen `staging` mit
  `Closes #105`, anschließend Promotion `staging` → `main` und Admin-Merge
  im Benutzerkontext des Repository-Eigentümers. Da `pages.yml` im
  Path-Filter des Push-Triggers steht, startet der Workflow auf `main`
  unmittelbar und dient als Abnahmenachweis.
- **Abhängigkeiten:** Keine Code-Abhängigkeiten. Zu beachten ist, dass ein
  „Re-run failed jobs" des betroffenen Laufs wegen des Duplikat-Artifacts
  nicht als Behebung taugt; Abhilfe nur über neue Workflow-Läufe (neuer
  Push oder `workflow_dispatch`).

## Konfiguration

Keine anwendungs- oder benutzerseitige Konfiguration; das Verhalten wird
vollständig im Workflow-YAML fixiert. Optionale Härtungsparameter
(Wartezeit/Anzahl der Wiederholungen, Job-Timeouts) werden als Konstanten
in `.github/workflows/pages.yml` gepflegt.

## Offene Fragen

- Soll strukturell auf das Zwei-Job-Layout umgestellt werden
  (empfohlener, vom Upstream bestätigter Fix) oder genügt die
  Retry-Härtung als minimaler Eingriff?
- Bleibt `actions/configure-pages@v6` stehen, oder soll der Dependabot-Bump
  vorsorglich auf v5 zurückgesetzt werden (kein belegter
  Kausalzusammenhang)?
- Wer führt die Admin-Merges (Benutzerkontextwechsel) durch, und sind
  dabei Branch-Protection-Regeln bzw. Required Checks auf `staging` und
  `main` zu beachten?
- Soll — unabhängig vom automatisch ausgelösten Push-Lauf — ein
  zusätzlicher `workflow_dispatch`-Lauf als negative/positive Kontrolle
  angestoßen werden?
