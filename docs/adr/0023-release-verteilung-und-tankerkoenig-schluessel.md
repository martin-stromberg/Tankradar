# ADR 0023: Release-Verteilung nur über den App Store — gemeinsamer Tankerkönig-Schlüssel bleibt im iOS-Binary

## Status

Angenommen (2026-11-24)

## Kontext

Issue #64 (Befund aus dem Architektur- und Sicherheitsreview #62) hat das Verteilungsmodell neu entschieden:
Bisher legten die Workflows `release.yml` und `staging-ci.yml` jede Version als GitHub-Release ab —
`release-win-x64.zip` (Windows-Paket, bewusst ohne Schlüssel gebaut) plus `update.json` (Update-Manifest
ohne Consumer im App-Code, dessen Download-URL auf `releases/download/…` zeigte). Zugleich war der
gemeinsame Tankerkönig-API-Schlüssel im iOS-Binary eingebettet
(`AssemblyMetadata("TankerkoenigApiKey", …)` via `TankerkoenigApiKey.targets`, Secret
`FUEL_PRICE_API_KEY`), und die frei herunterladbaren GitHub-Artefakte machten jedes Release-Paket zu einer
potenziellen Schlüsselquelle.

Der Repository-Eigentümer hat entschieden (Kommentar zu Issue #64, bestätigt 2026-11-28): Die
GitHub-Release-Ablage entfällt vollständig; einziges Auslieferungsergebnis ist die signierte iOS-`.ipa`
in App Store Connect (TestFlight). Der gemeinsame Schlüssel bleibt bewusst im iOS-Binary — eine
Risikoentscheidung (Option 3 aus Issue #64), die dieses Dokument festhält, statt den Schlüsselmechanismus
umzubauen. Die 52 bestehenden GitHub-Releases werden einmalig gelöscht, die Git-Tags bleiben als
Versionswahrheit von `scripts/determine-next-version.mjs` (`git tag --merged`) vollständig erhalten.

## Entscheidungen

### Verteilung

- **Keine GitHub-Releases und keine Release-Assets mehr.** `gh release create`/`upload` entfallen aus
  `release.yml` und `staging-ci.yml`; `validate-workflows.py` erzwingt das Verbot mutierender
  `gh release`-Aufrufe (`create`/`upload`/`edit`/`delete`) als harten CI-Verstoß.
- **Tags entstehen weiter — explizit per `git push`.** Der `finalize`-Job in `release.yml` pusht den
  stabilen Tag `vX.Y.Z` erst nach erfolgreichem TestFlight-Upload (nur `release_kind == 'automatic'`);
  der `rc-tag`-Job in `staging-ci.yml` pusht `vX.Y.Z-rc.N`. Pushes mit den `GITHUB_TOKEN`-Credentials von
  `actions/checkout` lösen keinen Folge-Workflow-Lauf aus — der `v*.*.*`-Trigger kann sich nicht selbst
  re-triggern.
- **Der TestFlight-Upload ist verbindlich.** `package-ios` erhält den Input `require-upload`; mit
  `'true'` (gesetzt in `release.yml` und `staging-ci.yml`) führen fehlende `IOS_SIGNING_ENABLED`-,
  Signierungs- oder `IOS_API_*`-Secrets zum Job-Fehlschlag statt zum stillen Rückfall auf den
  unsignierten Compile-Check. Nur die PR-CI (`pr-staging-ci.yml`) baut weiterhin ohne Secrets unsigniert.
- **Der Windows-Paketpfad entfällt ersatzlos:** `.github/actions/build-and-package/`,
  `scripts/package-windows.ps1` und `local-ci.ps1 -Package` sind entfernt; `update.json` wird nicht mehr
  erzeugt. Lokale Windows-Zwischenstände deckt `create-review-version.ps1`; die Windows-Build-Absicherung
  bleibt über `dotnet build`/`dotnet test` in den regulären Jobs erhalten.
- **„Already-released" prüft den Git-Tag.** `resolve-release-version.mjs` nutzt `git tag --list <tag>`
  (injiziert als `tagExists`) statt der GitHub-Releases-API; der `upload-existing`-Reparaturpfad und
  `EXPECTED_ASSETS` entfallen — es gibt kein Artefakt mehr, das repariert werden könnte.

### Schlüsselrisiko

Der gemeinsame Tankerkönig-Schlüssel bleibt per `AssemblyMetadata` im iOS-Binary. Die verbleibenden
Annahmen und Regeln:

- **Threat-Model:** Eine statische Extraktion des Schlüssels aus der Store-`.ipa` ist prinzipiell möglich;
  ein Entnehmer könnte das gemeinsame Tankerkönig-Kontingent (Rate-Limit des geteilten Schlüssels)
  missbrauchen und damit die Preisabfrage aller Anwender beeinträchtigen.
- **Begrenzende Faktoren:** Es existiert kein frei herunterladbares Build-Artefakt mehr; die Auslieferung
  erfolgt ausschließlich Apple-signiert und -verschlüsselt über den App-Store-Kanal. Der Schlüssel steckt
  nur im iOS-Binary — das entfallene Windows-Paket war ohnehin schlüsselfrei gebaut.
- **Rotationsweg:** Bei Missbrauch oder Sperrung wird das Secret `FUEL_PRICE_API_KEY` getauscht und ein
  neuer signierter Build ausgeliefert; `ApiKeyProvider` überschreibt die Geräteablage automatisch, wenn
  der Build-Schlüssel vom gespeicherten abweicht (`ApiKeys.cs:117–128`) — Anwender müssen nichts
  nachtragen.
- **Betreiberhinweise:** Missbrauch zeigt sich in der Tankerkönig-Nutzungsstatistik (Kontingent-Abrufe
  ohne eigene Nutzung). Empfohlen: regelmäßige Sichtprüfung der Statistik und ein Rotations-Turnus für
  `FUEL_PRICE_API_KEY`; bei Sperrung durch Tankerkönig neuen Schlüssel beantragen, Secret tauschen,
  Release-Lauf auslösen.

### Bestands-Releases (einmaliger Betriebsschritt)

Die vorhandenen GitHub-Releases (stabile Releases und RC-Pre-Releases) werden **einmalig** gelöscht —
nachgelagert zum Merge, siehe Runbook unten. Die Git-Tags bleiben ausdrücklich erhalten (kein
`--cleanup-tag`), sonst bräche die tagbasierte Versionsermittlung.

## Runbook: Löschung der Bestands-Releases (einmalig, nicht wiederholen)

Voraussetzung: Die Workflow-Änderungen dieses ADR sind nach `main` promoviert — erst dann existiert kein
Workflow-Pfad mehr, der Releases wieder anlegen würde. Authentifizierung: `gh`-CLI mit `repo`-Scope.

1. **Keine Alt-Läufe aktiv oder in der Warteschlange:**
   `gh run list --workflow release.yml --status in_progress` und `--status queued`, analog
   `gh run list --workflow staging-ci.yml --status in_progress|queued`. Läuft noch ein Lauf des alten
   Stands (Timeout bis ~60 Min), erst abwarten und erneut prüfen — er könnte ein Release wieder anlegen.
2. **Tag-Soll-Bestand erfassen:** `git ls-remote --tags origin` — Anzahl der `v*`-Tags notieren.
3. **Löschen:** `gh release list` auflisten, je Release-Tag `gh release delete <tag> --yes` —
   **ohne** `--cleanup-tag`.
4. **Nachweis:** `gh release list` ist leer; `git ls-remote --tags origin` erneut — die Tag-Anzahl muss
   identisch zu Schritt 2 sein. Bei Tag-Differenz: Abbruch, fehlenden Tag per `git push origin <tag>`
   wiederherstellen.
5. **Abschluss-Verifikation:** Die Leer-Prüfung nach einer Beobachtungsfrist von mindestens ~60 Minuten
   (längstes Alt-Workflow-Timeout) wiederholen — ein verspätet fertig werdender Alt-Lauf könnte sonst
   ein Release anlegen. Danach bleibt `gh release list` dauerhaft leer; das Verbot mutierender
   `gh release`-Aufrufe ist zusätzlich CI-erzwungen.

## Alternativen

- **Benutzergebundener Schlüssel bzw. Backend-Proxy** (andere Optionen aus Issue #64) wurde vom
  Eigentümer zugunsten der dokumentierten Risikoentscheidung verworfen — der Umbau der
  Schlüsselversorgung wäre deutlich aufwendiger, und ohne frei herunterladbare Artefakte sinkt das
  praktische Extraktionsrisiko.
- **Windows-Paket weiterbauen ohne Veröffentlichung** wurde verworfen: Ohne Verteilungskanal hätte das
  Paket keinen Consumer; `update.json` mit `releases/download`-URL hätte kein gültiges Ziel.
- **Release-Anlage per `gh api` statt `git tag`** wurde nicht gewählt: Ein REST-Aufruf würde wieder eine
  GitHub-Release-Abhängigkeit einführen; der reine Git-Tag genügt der Versionsermittlung.

## Folgen

- **Einziges Auslieferungsergebnis** ist die signierte iOS-`.ipa` in App Store Connect (TestFlight);
  Einreichung und Review-Freigabe bleiben manuell.
- **Kein Windows-Artefakt** mehr — weder als Download noch als Build-Ergebnis; Windows bleibt
  Entwicklungs-/Testplattform.
- **`--generate-notes` entfällt;** Release-Kommunikation läuft über `docs/RELEASE_NOTES.md` und
  `changes.log` (redaktionell, wie bisher gepflegt).
- **Version „verbraucht" erst bei Erfolg:** Da der stabile Tag erst nach dem TestFlight-Upload gepusht
  wird, kann ein iOS-Fehlschlag per Workflow-Re-Run wiederholt werden (Buildnummer via `run_attempt`
  eindeutig). Ein RC-Tag ohne TestFlight-Build bleibt möglich (parallele Jobs, wie bisher).
- **Fehlende iOS-Konfiguration blockiert sichtbar:** Release- und Staging-Läufe schlagen fehl, solange
  Signierungs-/Upload-Secrets fehlen — gewollt (sichtbar statt still).
- **Einmaliger Betriebsverlust:** Die gelöschten Bestands-Releases samt Assets
  (`release-win-x64.zip`/`update.json`) sind irreversibel weg — fachlich gewollt; die Assets waren
  schlüsselfrei, es entsteht kein neuer Sicherungs- oder Sicherheitsbefund.
- **Regressionsschutz:** `validate-workflows.py` (Verbot mutierender `gh release`-Aufrufe),
  `ReleaseMechanismRemovedTests` und `RequireUploadTests` verhindern eine stille Wiedereinführung des
  Release-Pfads bzw. des optionalen Uploads.
