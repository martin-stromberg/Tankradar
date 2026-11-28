# Umsetzungsplan: Ablage der Releases aus GitHub entfernen, Auslieferung nur über den App Store (Issue #64)

## Übersicht

Die GitHub-Release-Ablage wird vollständig entfernt: `gh release create`/`gh release upload` entfallen aus `release.yml` und `staging-ci.yml`, ebenso der Windows-Paketpfad (`build-and-package`, `package-windows.ps1`, `update.json`). Die Versions-Tags (`vX.Y.Z` und `vX.Y.Z-rc.N`) werden stattdessen explizit per `git tag` + `git push` erzeugt, damit `determine-next-version.mjs` unverändert funktioniert. Einziges Auslieferungsergebnis ist die signierte iOS-`.ipa` in App Store Connect (TestFlight); der Upload wird für Release- und RC-Läufe verbindlich. Die Risikoentscheidung zum eingebetteten Tankerkönig-Schlüssel wird als ADR 0023 dokumentiert. Zusätzlich werden die bestehenden GitHub-Releases (52 Stück — stabile Releases und RC-Pre-Releases, Stand 2026-11-28) per Anwender-Entscheidung einmalig gelöscht; die Git-Tags bleiben dabei vollständig erhalten.

Betroffene Bereiche: ausschließlich CI-/Release-Ebene (Workflows, Composite Actions, CI-Hilfsskripte, Workflow-Validierung) plus Dokumentation und ein einmaliger Betriebsschritt (Bestands-Löschung). Keine Anwendungsklassen.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein „Release-Lauf" je Release-Ereignis (stabiler Lauf auf `main`/manuellem Tag; RC-Lauf auf `staging`), jeweils bestehend aus Versionsermittlung, Build, TestGate und Auslieferung. Zusätzlich die einmalige Entscheidungs-/Risikodokumentation (ADR) und die einmalige Löschung der Bestands-Releases (Betriebsschritt nach Merge). |
| Auslösende Ereignisse | Push auf `main`; manuell gepushter Tag `v*.*.*` (`.github/workflows/release.yml`); Push auf `staging` mit freigabefähigen Commits (`.github/workflows/staging-ci.yml`). Trigger bleiben unverändert. Die Bestands-Löschung wird manuell durch den Betreiber/Automatisierungs-Agenten ausgelöst — nicht durch einen Workflow. |
| Erwartete Wirkung | Nach einem Lauf existiert kein GitHub-Release und kein Release-Asset; nach dem Abschluss-Betriebsschritt ist die Release-Liste vollständig leer (`gh release list` ohne Einträge). Sichtbare Ergebnisse: signierte iOS-`.ipa` in App Store Connect (TestFlight), Git-Tags `vX.Y.Z`/`vX.Y.Z-rc.N` auf `origin` (vollständig erhalten, Anzahl unverändert), ADR 0023 mit Risikoentscheidung (Threat-Model, Rotationsweg, Betreiberhinweise). |
| Umgang mit fehlenden Objekten | **Release-Tag `v*.*.*`:** bisher implizit durch `gh release create` → künftig explizit **erzeugen** (`git tag` + `git push` im `finalize`-Job des Release-Workflows; nur `release_kind == 'automatic'`). **RC-Tag `v*.*.*-rc.N`:** ebenfalls explizit **erzeugen** (`rc-tag`-Job in `staging-ci.yml`). **Release-Existenz-/Reparaturprüfung:** GitHub-Release-Abfrage wird **verworfen**; die „already-released"-Erkennung wird auf Git-Tag-Existenz **wiederverwendet** (andere Datenquelle). **`update.json`:** **verwerfen** (kein Consumer, Download-URL ohne Ziel). **Windows-Paket `release-win-x64.zip`:** **verwerfen** (kein Verteilungskanal mehr). **Bestehende GitHub-Releases (52 Stück):** **verwerfen** — einmalige Löschung per `gh release delete <tag>` ohne `--cleanup-tag`, sequenziert nach dem Merge (siehe Programmablauf „Einmalige Löschung der Bestands-Releases"). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein Umbau der Schlüsselversorgung: `TankerkoenigApiKey.targets` und `ApiKeys.cs` bleiben unverändert; der gemeinsame Schlüssel bleibt per `AssemblyMetadata("TankerkoenigApiKey", …)` im iOS-Binary. Kein benutzergebundener Schlüssel, kein Backend/Proxy. | Bewusste Risikoentscheidung des Projektinhabers (Option 3 aus Issue #64); wird über ADR 0023 dokumentiert statt beseitigt. | bestätigt (2026-11-28) |
| 2 | Keine Automatisierung der App-Store-Einreichung/des Reviews: Der Upload endet in App Store Connect (TestFlight via iTMSTransporter wie bisher); Einreichung und Freigabe bleiben manuell. | „In den App Store laden" wird als bestehender TestFlight-Upload gelesen; Review-Automatisierung (fastlane/ASC-API) wäre ein neuer Mechanismus mit deutlich mehr Aufwand. | bestätigt (2026-11-28) |
| 3 | Kein neuer Verteilungskanal für die Windows-Variante; der Windows-Paketbau entfällt ersatzlos. | Ohne GitHub-Release existiert kein Kanal; Windows ist Entwicklungs-/Testplattform, `create-review-version.ps1` deckt lokale Zwischenstände. | bestätigt (2026-11-28) |
| 4 | Keine Änderung an der Versionslogik: `scripts/determine-next-version.mjs` und `scripts/versioning.mjs` bleiben unverändert (tagbasierte 0.x-Regel). | Scope-Vorgabe der Anforderung; die Tag-Abhängigkeit wird durch explizite Tag-Pushes weiter bedient. | bestätigt (2026-11-28) |

Der frühere Ausschluss „Keine Löschung bereits existierender GitHub-Releases" entfällt per Anwender-Entscheidung vom 2026-11-28: Die Bestands-Releases **sollen gelöscht** werden und sind jetzt verbindlicher Scope (siehe Programmablauf „Einmalige Löschung der Bestands-Releases").

### Getroffene Annahmen (statt offener Punkte)

Die sechs offenen Fragen der `requirement.md` wurden zunächst als minimalinvasive Annahmen festgelegt und vom Anwender am 2026-11-28 bestätigt — mit einer Änderung: Annahme 4 wurde durch die Entscheidung „Bestands-Releases löschen" ersetzt.

| # | Frage | Getroffene Annahme |
|---|-------|--------------------|
| 1 | Bedeutung von „App Store" | Der bestehende TestFlight-Upload per iTMSTransporter nach App Store Connect ist gemeint; Einreichung/Review bleibt manuell. *(bestätigt 2026-11-28)* |
| 2 | Windows-Variante | Entfällt vollständig: `build-and-package`-Action, `package-windows.ps1`, `update.json`-Erzeugung und `local-ci.ps1 -Package` werden entfernt (nicht nur „nicht veröffentlicht"). *(bestätigt 2026-11-28)* |
| 3 | RC-Pre-Releases | Die Entfernung gilt auch für `staging-ci.yml`; RC-Ergebnis ist künftig nur noch Git-Tag plus TestFlight-Build. *(bestätigt 2026-11-28)* |
| 4 | Bestehende GitHub-Releases | **Geändert per Anwender-Entscheidung 2026-11-28:** Die 52 Bestands-Releases (stabile + RC-Pre-Releases) werden **gelöscht** — einmalig per `gh release delete <tag>` ohne `--cleanup-tag` (Git-Tags bleiben als Versionswahrheit erhalten); Ausführung als Betriebsschritt nach dem Merge der Workflow-Änderungen (siehe Programmablauf unten). |
| 5 | Verbindlichkeit des TestFlight-Uploads | Upload wird für `release.yml` und `staging-ci.yml` verbindlich (neuer `require-upload`-Input an `package-ios`, Job schlägt fehl); `pr-staging-ci.yml` behält den unsignierten Compile-Check. *(bestätigt 2026-11-28)* |
| 6 | Ort der Risikodokumentation | Neuer ADR `docs/adr/0023-…` (Threat-Model, Rotationsweg, Betreiberhinweise) plus Verknüpfung in `docs/help/Preisdaten/api-schluessel.md`; keine zusätzliche neue Hilfeseite. *(bestätigt 2026-11-28)* |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| „Already-released"-Erkennung (`resolve-release-version.mjs`) | Existenzprüfung über Git-Tags (`git tag --list <tag>`, injiziert als `tagExists`) statt über die GitHub-Releases-API; Reparaturpfad (`upload-existing`, `EXPECTED_ASSETS`, `ghGetRelease`/`ghListReleases`) entfällt ersatzlos. | Es gibt kein Artefakt mehr, das „repariert" werden könnte; der Git-Tag ist die einzige verbliebene Versionswahrheit. |
| Tag-Erzeugung | Explizites `git tag` + `git push origin <tag>` in eigenen Job-Schritten mit den Standard-Credentials von `actions/checkout`. | `gh release create` entfällt; Pushes mit `GITHUB_TOKEN` lösen **keinen** neuen Workflow-Lauf aus — der `v*.*.*`-Tag-Trigger kann den Workflow damit nicht selbst re-triggern (wichtige Korrektheitsannahme). |
| Zeitpunkt des Release-Tag-Push | Neuer `finalize`-Job in `release.yml` mit `needs: [release, ios]`, nur bei `release_kind == 'automatic'` und `released == 'true'`. | Der Tag markiert „ausgeliefert": Scheitert der TestFlight-Upload, entsteht kein Tag und ein erneuter Lauf kann dieselbe Version wiederholen (es gibt keinen Reparaturpfad mehr). Ein Push vor dem iOS-Job würde die Version endgültig „verbrauchen". |
| RC-Tag-Push (`staging-ci.yml`) | Eigener leichtgewichtiger Job `rc-tag` (ubuntu-latest, `permissions: contents: write`), weiterhin parallel zu `ios-prerelease` (nur `needs: [detect-backmerge, version]`). | Spiegelt die bisherige Semantik (Tag/Pre-Release unabhängig vom iOS-Ergebnis); minimalinvasiv; RC-Tags dienen primär der Nummernzählung von `determine-next-version.mjs --rc`. |
| Verbindlicher TestFlight-Upload | Neuer Input `require-upload` an `.github/actions/package-ios` (Standard `'false'`); `'true'` in `release.yml`-Job `ios` und `staging-ci.yml`-Job `ios-prerelease`. Bei `'true'` führen fehlende `IOS_SIGNING_ENABLED`, fehlende Signierungs- oder API-Key-Secrets zum Fehlschlag statt zum stillen Rückfall auf einen unsignierten Build. | Ohne GitHub-Release gäbe ein unsignierter Compile-Check ein „Release" ohne jedes Auslieferungsergebnis — widerspricht der Weisung. PR-CI (`pr-staging-ci.yml`) braucht weiterhin den secret-losen Compile-Check. |
| Windows-Paketpfad | Vollständige Entfernung: `.github/actions/build-and-package/`, `scripts/package-windows.ps1`, `local-ci.ps1`-Parameter `-Package`/`-PackageVersion` samt Schritt. | Kein Consumer für ZIP/`update.json`; die `releases/download`-URL des Manifests hätte kein gültiges Ziel; lokale Windows-Zwischenstände deckt `create-review-version.ps1`; die Windows-Build-Absicherung bleibt über `dotnet build`/`dotnet test` in den regulären Jobs erhalten. |
| `validate-workflows.py` | `gh release create`/`upload`/`edit`/`delete` in `run:`-Schritten wird zu einem harten Verstoß (Policy „keine GitHub-Release-Ablage"); das IPA-Verbot für `actions/upload-artifact` bleibt unverändert. `gh_release_assets`/`GH_VALUE_OPTIONS` entfallen (unerreichbar). | Macht die Entfernung CI-erzwungen statt konventionell; verhindert stille Wiedereinführung des Release-Pfads. Die `build-and-package`-Schlüsselregeln bleiben als datengetriebene Schutzregeln (feuern bei Wiedereinführung). |
| Risikodokumentation | ADR `docs/adr/0023-release-verteilung-und-tankerkoenig-schluessel.md` im bestehenden ADR-Format; `api-schluessel.md` verweist darauf. | Folgt der ADR-Konvention (nächste Nummer 0023); das Akzeptanzkriterium (Threat-Model, Rotationsweg, Betreiberhinweise) ist dort vollständig abbildbar; `api-schluessel.md:54–56` enthält bereits Teile und wird ergänzt/verlinkt statt dupliziert. |
| Löschung der Bestands-Releases | Einmalige Remote-Operation durch den Betreiber/Automatisierungs-Agenten: `gh release list` + Schleife `gh release delete <tag> --yes` — **ohne** `--cleanup-tag`. Sequenzierung: strikt **nach** dem Merge/Promotion der Workflow-Änderungen nach `main`, mit abschließender Verifikation (`gh release list` leer, `git ls-remote --tags origin` unverändert). Kein CI-Job und kein Repo-Skript-Zwang; dokumentiert als als einmalig gekennzeichneter Runbook-Abschnitt in ADR 0023. | Die alten Workflows (Stand vor dem Merge) würden fehlende Releases über den Reparaturpfad (`resolve-release-version.mjs`/`release_action`) bzw. `gh release create` wieder anlegen — erst nach der Promotion existiert dieser Mechanismus nicht mehr. `--cleanup-tag` ist ausgeschlossen, weil die Tags `vX.Y.Z`/`vX.Y.Z-rc.N` die Versionswahrheit von `determine-next-version.mjs` (`git tag --merged`) bilden. Ein CI-Job wäre für eine einmalige destruktive Remote-Operation unverhältnismäßig und würde das Löschrecht dauerhaft in die Pipeline holen. |

## Programmabläufe

### Stabiler Release-Lauf (`release.yml`)

1. Trigger: Push auf `main` oder manueller Tag `v*.*.*`.
2. Job `release` (windows-latest): Checkout (`fetch-depth: 0`, `fetch-tags: true`) → `node scripts/resolve-release-version.mjs` (ohne `GH_TOKEN`, siehe unten) → nur bei `released == 'true'`: `setup-dotnet`, `dotnet workload restore`, Release-Gate (blockierende Unit-/Integrationstests, OS-Interface-Tests best-effort, TRX-Upload `test-results-release`).
3. Job `ios` (`needs: release`, `if: released == 'true'`): `./.github/actions/package-ios` mit allen Signierungs- und App-Store-Connect-Secrets, `fuel-price-api-key` und neu `require-upload: 'true'` → signierter Build, `.ipa`-Validierung, **verpflichtender** iTMSTransporter-Upload.
4. Job `finalize` (`needs: [release, ios]`, `if: release_kind == 'automatic' && released == 'true'`, ubuntu-latest): Checkout → `git tag "<tag>"` auf `GITHUB_SHA` → `git push origin "<tag>"`.
5. Manueller Weg (`release_kind == 'manual'`): `finalize` wird übersprungen — der Tag existiert bereits als Auslöser.

Beteiligte Komponenten: `release.yml`, `scripts/resolve-release-version.mjs`, `scripts/determine-next-version.mjs`, `.github/actions/package-ios`.

### RC-Lauf (`staging-ci.yml`)

1. Trigger: Push auf `staging` (außer Back-Merge, unverändert).
2. Jobs `detect-backmerge`, `static-checks`, `build-and-test`, `os-interface-tests`, `version` unverändert (`version` liefert `changed`, `rc_tag`, `rc_version`).
3. Job `rc-tag` (ersetzt `prerelease`; `if: is_backmerge != 'true' && changed == 'true'`; ubuntu-latest; `permissions: contents: write`): Checkout → `git tag "<rc_tag>"` → `git push origin "<rc_tag>"`. Kein Windows-Build, kein `gh release`.
4. Job `ios-prerelease` unverändert konditioniert, `package-ios` zusätzlich mit `require-upload: 'true'`.

Beteiligte Komponenten: `staging-ci.yml`, `scripts/determine-next-version.mjs`, `.github/actions/package-ios`.

### Versionsauflösung (`resolve-release-version.mjs`, vereinfacht)

1. `classifyWorkflowRef` klassifiziert den Ref wie bisher (`tag` → `manual` mit `parseManualTag`; `branch: main` → `automatic`).
2. `manual` → `released: "true"`, `reason: "new-release"`, Version/Tag aus dem gepushten Tag (kein Existenzcheck — der Tag-Push ist der Auslöser selbst).
3. `automatic` → `determine()` aus `determine-next-version.mjs`; `changed == 'false'` → `released: "false"`, `reason: "no-releasable-commits"`; `changed == 'true'` → `tagExists(tag)` prüfen: Tag vorhanden → `released: "false"`, `reason: "already-released"` (Tag existiert, ist aber nicht in `HEAD` gemergt — Restzustand); sonst `released: "true"`, `reason: "new-release"`.
4. Outputs: `released`, `reason`, `version`, `tag`, `release_kind`. Entfallen: `release_action`, alle GitHub-API-Abhängigkeiten.

Beteiligte Komponenten: `scripts/resolve-release-version.mjs`, `scripts/determine-next-version.mjs`, `scripts/versioning.mjs`.

### Einmalige Löschung der Bestands-Releases (Betriebsschritt nach Merge)

1. **Voraussetzung prüfen:** Die Workflow-Änderungen dieses Branches sind auf `main` promoviert (PR → `staging` → Promotion nach `main`). Ab dann existiert kein Workflow-Pfad mehr, der GitHub-Releases anlegt; die alten Workflows (Stand vor dem Merge) hätten fehlende Releases über den Reparaturpfad bzw. `gh release create` wieder angelegt.
2. **Keine Alt-Läufe in-flight/queued:** Vor dem Löschen prüfen, dass keine Workflow-Läufe des alten Stands mehr laufen oder in der Warteschlange sind: `gh run list --workflow release.yml --status in_progress` und `--status queued`, analog `gh run list --workflow staging-ci.yml --status in_progress|queued`. Ein vor dem Merge gestarteter Lauf des alten Workflows (Timeout bis ~60 Min) könnte sonst während/nach der Löschung fertig werden und ein Release wieder anlegen. Erst löschen, wenn keine Läufe mit Startzeitpunkt vor dem Merge-Commit mehr aktiv/queued sind; ab dem Merge-Commit gestartete Läufe nutzen bereits den neuen Workflow und sind unschädlich. Bei laufenden Alt-Läufen: abwarten und erneut prüfen.
3. **Tag-Bestand als Soll erfassen:** `git ls-remote --tags origin` — Anzahl der `v*`-Tags notieren (Soll-Bestand aus Bestandsaufnahme: 50 Tags `v*`).
4. **Release-Bestand auflisten:** `gh release list --repo martin-stromberg/Tankradar` (52 Einträge: stabile Releases + RC-Pre-Releases, Stand 2026-11-28).
5. **Löschen:** Je Release-Tag `gh release delete <tag> --yes` — **ohne** `--cleanup-tag` (die Git-Tags bleiben ausdrücklich erhalten). Authentifizierung: `gh`-CLI des ausführenden Betreibers/Agenten mit `repo`-Scope. Es handelt sich um eine einmalige Remote-Operation — kein CI-Job, kein dauerhaftes Repo-Skript erforderlich (Runbook-Abschnitt in ADR 0023).
6. **Nachweis:** `gh release list` ist leer; `git ls-remote --tags origin` erneut ausführen — die Tag-Anzahl muss identisch zu Schritt 3 sein. Bei Tag-Differenz: Abbruch/Eskalation, fehlenden Tag manuell wiederherstellen (`git push origin <tag>`).
7. **Abschluss-Verifikation gegen Wiederanlage:** Die Leer-Prüfung `gh release list` wird nach einer kurzen Beobachtungsfrist (mindestens das längste Alt-Workflow-Timeout, ~60 Min) wiederholt — eine sofortige einmalige Prüfung könnte ein Release übersehen, das ein noch laufender Alt-Lauf verspätet anlegt. Die wiederholte Prüfung gilt als abschließender Check nach allen Merges (Feature → `staging`, Promotion `staging` → `main`). Danach bleibt `gh release list` dauerhaft leer, da der Release-Anlegemechanismus mit dem Merge entfernt wurde (zusätzlich durch `validate-workflows.py` CI-erzwungen).

Beteiligte Komponenten: `gh`-CLI (Betreiber, `repo`-Scope), ADR 0023 (einmaliger Runbook-Abschnitt), `scripts/determine-next-version.mjs` (Abhängigkeit: Git-Tags müssen erhalten bleiben).

## Neue Klassen

Keine — die Umsetzung liegt vollständig auf CI-/Release-Ebene (YAML, Node-/Python-/PowerShell-Skripte) plus Dokumentation. Neue Dateien: `docs/adr/0023-*.md` (Dokument, keine Klasse).

## Änderungen an bestehenden Klassen

### `.github/workflows/release.yml` (Workflow)

- **Entfernte Schritte (Job `release`):** „Check out release tag for asset repair" (Z. 53–63), „Build and package" (Z. 109–114), „Create GitHub release" (Z. 116–133), „Upload asset to existing GitHub release" (Z. 135–142).
- **Geänderter Schritt „Resolve release version" (Z. 47–51):** `env: GH_TOKEN` entfällt (keine `gh`-Aufrufe mehr).
- **Job-Outputs (Z. 30–34):** `release_action` entfällt; `release_kind` wird zusätzlich als Job-Output exposiert (für die `finalize`-Bedingung).
- **Gate-Bedingungen:** alle `release_action == 'create'`-Bedingungen (Z. 79, 84, 90, 96, 102, 149) werden zu `released == 'true'` (Reparaturpfad entfällt — „create" ist der einzige verbleibende Fall).
- **Job `ios`:** `if` vereinfacht zu `needs.release.outputs.released == 'true'`; `package-ios` erhält `require-upload: 'true'`. Der Kommentar über dem Job (Z. 144–145: „optional nach TestFlight hochgeladen … sonst unsigniert (Compile-Prüfung) - ohne Fehlschlag") beschreibt das alte Rückfallverhalten und wird auf den verbindlichen Upload umgeschrieben (Fehlschlag bei fehlender Signierung/fehlenden Secrets).
- **Neuer Job `finalize` (ubuntu-latest):** `needs: [release, ios]`, `if: needs.release.outputs.released == 'true' && needs.release.outputs.release_kind == 'automatic'`; Schritte Checkout → `git tag "$RELEASE_TAG"` → `git push origin "$RELEASE_TAG"` (Env `RELEASE_TAG` aus `needs.release.outputs.tag`).
- **Permissions:** `contents: write` auf Workflow-Ebene bleibt (für den Tag-Push erforderlich).

### `.github/workflows/staging-ci.yml` (Workflow)

- **Job `prerelease` (Z. 379–414) wird durch `rc-tag` ersetzt:** ubuntu-latest statt windows-latest; `needs: [detect-backmerge, version]` und `if`-Bedingung unverändert; Job-Level `permissions: contents: write` bleibt; Schritte: Checkout (`fetch-depth: 0`) → `git tag "$RC_TAG"` → `git push origin "$RC_TAG"`. Der Windows-Paketbau und `gh release create --prerelease` entfallen.
- **Job `ios-prerelease` (Z. 418–446):** `package-ios` erhält `require-upload: 'true'`; Bedingungen unverändert.
- **Veraltete Kommentare entfernen/anpassen:** (a) Der Kommentar über „Determine next version and RC number" (Z. 372–375: „sonst melden `update.json` und das Paket die stabile statt der RC-Version") verweist auf gelöschte Artefakte und wird auf die verbleibende Begründung umformuliert („… sonst melden die Build-Jobs/TestFlight-Builds die stabile statt der RC-Version"). (b) Der Kommentar über `ios-prerelease` (Z. 416–417: „optional nach TestFlight hochgeladen … sonst unsigniert (Compile-Prüfung) - ohne Fehlschlag") wird wie in `release.yml` auf den verbindlichen Upload umgeschrieben.
- **Workflow-Permissions** (`contents: read`, `checks: write`, Z. 14–16) bleiben; die Job-Level-`contents: write` wandert mit dem `rc-tag`-Job.

### `scripts/resolve-release-version.mjs` (Node-Skript)

- **Entfernt:** `EXPECTED_ASSETS` (Z. 12), `releaseHasExpectedAsset` (Z. 35–40), `incompleteReleases` (Z. 44–51) inkl. Pre-Release-Guard, `gh`/`ghGetRelease`/`ghListReleases` (Z. 102–120), der Reparaturpfad (Z. 86–98), `getRelease`/`listReleases`-Injektionen, Output `release_action`, `GITHUB_REPOSITORY`-Auswertung.
- **Neu:** `tagExists(tag)` als injizierte Abhängigkeit; Implementierung im Main-Block über `git tag --list <tag>` (nicht-leere Ausgabe = vorhanden; Checkout liefert alle Tags via `fetch-tags: true`).
- **Geändert:** `resolveReleaseVersion` auf die oben beschriebene vereinfachte Logik; Outputs ohne `release_action`.
- **Unverändert:** `parseManualTag`, `classifyWorkflowRef`, `AUTOMATIC_RELEASE_BRANCHES`.

### `scripts/resolve-release-version.test.mjs` (Node-Tests)

- **Entfernt:** Tests zu `releaseHasExpectedAsset`, `incompleteReleases` (Regression 11.1), Reparatur-/`upload-existing`-Szenarien, GitHub-Release-Stubs (`complete`, `asset`, `noReleases` mit `getRelease`/`listReleases`).
- **Neu/angepasst:** `resolveReleaseVersion`-Szenarien gegen `tagExists`-Stubs: manuell immer released; automatisch neu (Tag fehlt); automatisch already-released (Tag vorhanden); automatisch no-releasable-commits.
- **Unverändert:** `parseManualTag`-, `classifyWorkflowRef`-Tests.

### `.github/actions/package-ios/action.yml` (Composite Action)

- **Neuer Input:** `require-upload` (String, Default `'false'`) — „'true' macht signierten Build + TestFlight-Upload verbindlich; der Job schlägt fehl, wenn Signierung oder API-Key-Secrets fehlen."
- **Geänderter Schritt „Determine signing mode" (Z. 84–117):** Bei `REQUIRE_UPLOAD == 'true'`: `ENABLED != 'true'` → `::error::` + Exit 1 statt `signed=false`; fehlende Signierungs-Secrets → `::error::` + Exit 1 statt unsigniertem Rückfall; fehlende `IOS_API_KEY_*`-Secrets → `::error::` + Exit 1 statt Notice.
- **Schritt „Upload to TestFlight" (Z. 301–350):** unveränderte Bedingung (bei `require-upload='true'` kann sie dank der vorgeschalteten Härte nicht mehr „leise überspringen"); Verhalten bei `'false'` bleibt exakt wie bisher.
- **Beschreibung (Z. 3–7):** um den verbindlichen Modus ergänzen.

### `.github/actions/build-and-package/action.yml` (Composite Action)

- **Wird gelöscht** (gesamtes Verzeichnis `.github/actions/build-and-package/`). Referenzen in `release.yml`/`staging-ci.yml` entfallen mit den Workflow-Änderungen.

### `scripts/package-windows.ps1` (PowerShell-Skript)

- **Wird gelöscht.** Einzige verbliebene Rufer waren `build-and-package` und `local-ci.ps1 -Package`.

### `scripts/local-ci.ps1` (PowerShell-Skript)

- **Entfernte Parameter:** `-Package` (Z. 47–48), `-PackageVersion` (Z. 50–51).
- **Entfernter Schritt:** Paketbau (Z. 232–239).
- **Kopfdokumentation (Z. 26, 33):** `update.json`-/`Package`-Erwähnungen entfernen.

### `scripts/validate-workflows.py` (Python-Validierung)

- **`check_release_commands` (Z. 126–139):** Umgebaut zum Verbot — jede Zeile mit `gh release create|upload|edit|delete` erzeugt einen Fehler („GitHub-Release-Ablage entfernt, Issue #64/ADR 0023"); `gh release view`/`list` bleiben zulässig.
- **Entfernt:** `GH_VALUE_OPTIONS` (Z. 45–48), `gh_release_assets` (Z. 96–123) — ohne erlaubte `gh release`-Aufrufe unerreichbar.
- **Unverändert:** `classify_upload_path`, `check_upload_artifact` (IPA-Verbot für Workflow-Artefakte bleibt Schutz), `check_key_policy`/`check_action`-Sonderregeln für `build-and-package` (als datengetriebene Schutzregeln belassen), Docstring-Anpassung.

### `scripts/test_validate_workflows.py` (Python-Tests)

- **`ReleaseCommandTests`:** umgebaut zu „alle mutierenden `gh release`-Aufrufe sind Fehler"; `test_label_suffix_and_other_commands_are_handled` angepasst (`gh release view` weiterhin sauber).
- **`WindowsPackageKeyPolicyTests` (Z. 89–106):** entfernt (die geprüften Dateien existieren nicht mehr).
- **Neu `ReleaseMechanismRemovedTests`:** `.github/actions/build-and-package/action.yml` und `scripts/package-windows.ps1` existieren nicht; kein Repo-Workflow enthält `gh release create|upload`.
- **Neu `RequireUploadTests`:** statischer Guard gegen einen stillen Rückbau des verbindlichen Uploads (Annahme 5). Stil wie `WindowsPackageKeyPolicyTests` — YAML-Parsing über `MODULE.load`, kein Zeilen-/Muster-Matching: `release.yml` laden → Job `ios` → den Schritt mit `uses: ./.github/actions/package-ios` finden → `with['require-upload'] == 'true'` (String-Vergleich) muss gelten; analog `staging-ci.yml` → Job `ios-prerelease`. Zusätzlich wird geprüft, dass `.github/actions/package-ios/action.yml` den Input `require-upload` deklariert. Würde der Input entfernt oder auf `'false'` zurückgesetzt, entstünde wieder ein „Release ohne Auslieferung" — der Test schlägt dann fehl.
- **`UploadArtifactTests`:** unverändert (`artifacts/update.json` bleibt als generischer `.json`-Pfad ein zulässiges Testmuster).

### Dokumentation

- **Neu `docs/adr/0023-release-verteilung-und-tankerkoenig-schluessel.md`:** Status „Angenommen (2026-11-24)"; Kontext Issue #64/Review #62/Owner-Entscheidung; Entscheidungen: (a) Release-Ablage aus GitHub entfernt, Vertrieb ausschließlich über App Store Connect/TestFlight, Tags per `git push`; (b) gemeinsamer Schlüssel bleibt im iOS-Binary — Threat-Model (statische Extraktion aus der Store-`.ipa` → Missbrauch des gemeinsamen Kontingents), begrenzende Faktoren (kein frei herunterladbares Artefakt mehr, Apple-signierte/-verschlüsselte Auslieferung, Windows-Paket schlüsselfrei), Rotationsweg (`FUEL_PRICE_API_KEY` tauschen + neuer signierter Build; `ApiKeyProvider` überschreibt die Geräteablage automatisch, `ApiKeys.cs:117–128`), Betreiberhinweise (Missbrauchsbeobachtung über Tankerkönig-Nutzungsstatistik, Rotations-Turnus, Vorgehen bei Sperrung); Folgen (kein Windows-Artefakt, `--generate-notes` entfällt, `RELEASE_NOTES.md`/`changes.log` sind die Release-Doku); zusätzlich ein als **einmalig** gekennzeichneter Runbook-Abschnitt „Löschung der Bestands-Releases" (In-flight-Prüfung `gh run list --workflow release.yml --status in_progress|queued` und `--workflow staging-ci.yml`, dann `gh release list`/`gh release delete <tag> --yes` ohne `--cleanup-tag`, Sequenzierung nach Merge, Nachweis `gh release list` leer — nach kurzer Beobachtungsfrist wiederholt — + `git ls-remote --tags origin` unverändert).
- **`docs/help/ci-cd/workflows.md`:** Workflow-Tabelle (Z. 14, 17), Baustein-Tabelle (`build-and-package`-Zeile entfernen, Z. 44; `package-ios`-Zeile (Z. 46) um den neuen `require-upload`-Input ergänzen: `'true'` in `release.yml`/`staging-ci.yml` macht signierten Build + TestFlight-Upload verbindlich — fehlende `IOS_SIGNING_ENABLED`/Signierungs-/API-Key-Secrets führen zum Job-Fehlschlag statt zum stillen Rückfall auf den unsignierten Simulator-Build als Compile-Prüfung; `'false'`/PR-CI behält das bisherige Rückfallverhalten), Skript-Tabelle (`package-windows.ps1` entfernen, `resolve-release-version.mjs` und `validate-workflows.py` beschreiben neu, Z. 54–57), Abweichung „Release-Erstellung per gh CLI" (Z. 81) → „Tag-Push per git, keine GitHub-Releases mehr".
- **`docs/help/ci-cd/README.md`:** Ablauf-Schritte 2 und 4 (Z. 21–28) → RC-Tag + TestFlight statt GitHub-Pre-Release; Tabelle „Release-Artefakte" (Z. 38–44) → nur noch iOS-`.ipa`/TestFlight, Hinweis auf entfernte Windows-Artefakte; Verhalten bei fehlenden Secrets (Z. 50–52) → `require-upload`-Semantik.
- **`docs/help/ci-cd/einrichtung.md`:** Checklistenpunkt „Actions-Berechtigung" im Abschnitt „Branch-Schutz" (Z. 21–22) — Begründung „…nötig für Promotion-/Backmerge-PRs und Releases" an die neue Verteilung anpassen (keine GitHub-Releases mehr; Schreibrecht dient u. a. dem Tag-Push der Workflows); Einleitung von Abschnitt 4 „Apple-seitige Vorbereitung" (Z. 38) — Aussage „baut die Pipeline iOS weiterhin unsigniert als Compile-Prüfung" durch die `require-upload`-Fehlschlag-Semantik ersetzen (Release-/Staging-Läufe schlagen fehl, nur PR-CI baut weiter unsigniert); Absatz unter der Secret-Tabelle zu `FUEL_PRICE_API_KEY` (Z. 72) — Teilaussage „das Windows-Paket wird ohne Schlüssel gebaut" entfernen/anpassen (Windows-Paketpfad entfällt vollständig); Tabellenzeile `IOS_SIGNING_ENABLED` im Abschnitt „Repository-Variablen" (Z. 81) — „jeder andere Wert oder fehlende Variable = unsignierter Compile-Check" ergänzen/ändern zu „bei `require-upload='true'` (Release-/Staging-Läufe) = Job-Fehlschlag"; Abschnitt „Verhalten der Pipeline" (Z. 85–91) → Fehlschlag statt stiller Compile-Check in Release-/Staging-Läufen; Prüf-Checkliste (Z. 110–113) → „RC-Tag `v0.1.0-rc.1` gepusht, iOS-Build in TestFlight".
- **`docs/help/ci-cd/versionierung.md`:** Z. 6–7 (`--generate-notes` entfällt — Release-Notes = `RELEASE_NOTES.md`/`changes.log`), Z. 25–28 (RC-Version ohne `update.json`), Hinweis dass Tags vom Workflow gepusht werden.
- **`docs/help/ci-cd/lokaler-pruefung.md`:** `-Package`-Aufrufbeispiel (Z. 23) und Tabellenzeile (Z. 37) entfernen.
- **`docs/help/ci-cd/index.md`:** Einleitungsabsatz (Z. 6–9) anpassen — Aussagen „jeder Push auf `staging` erzeugt eine Pre-Release-Version"/„Push auf `main` ein finales Release" und „Die Windows-App wird als ZIP veröffentlicht" sind veraltet → RC-/Release-Tag per `git push`, Auslieferung ausschließlich über TestFlight, kein Windows-ZIP mehr.
- **`docs/help/Preisdaten/api-schluessel.md`:** Artefakt-Tabelle (Z. 40–44) → Windows-Zeile durch Hinweis „entfällt seit Issue #64" ersetzen; Secret-Absatz (Z. 48) → Satz zum Windows-Paketieren (`build-and-package`) entfernen bzw. umformulieren (die Action existiert nicht mehr); Sicherheitshinweis (Z. 54–56) um Verweis auf ADR 0023 ergänzen; Routing-Abschnitt (Z. 73, „CI und Windows-Paket") → Verweis auf `scripts/package-windows.ps1` entfernen (z. B. „Weder die Workflows noch das CI kennen einen Routing-Schlüssel; das Secret `ROUTING_API_KEY` wird nicht benötigt").
- **`docs/RELEASE_NOTES.md`:** Hinweis (EN+DE) „Releases werden nicht mehr auf GitHub abgelegt; die App wird über TestFlight/App Store verteilt; bestehende GitHub-Releases wurden entfernt (Tags bleiben erhalten)".
- **`changes.log`:** neuer Eintrag `## 2026-11-24 — GitHub-Release-Ablage entfernt (Issue #64)` inkl. Vermerk der einmaligen Löschung der Bestands-Releases.
- **`README.md`:** Z. 113–115 — Wort „Releases" im Verweis ggf. anpassen (nur redaktionell, kein Download-Bezug vorhanden); `pages/` unberührt.
- **Historische Zeitdokumente bleiben unverändert:** Alt-Einträge in `changes.log`, `docs/adr/0014-route-search.md:83` und die Bestandsaufnahmen anderer Aufgaben unter `docs/projects/task/` erwähnen den alten Paketpfad weiterhin — bewusst als Zeitdokumente (Befund der eigenen Referenz-Suche; kein Handlungsbedarf).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Workflow-`run:`-Schritte | Kein `gh release create`/`upload`/`edit`/`delete` (Policy: keine GitHub-Release-Ablage) | `validate-workflows.py` meldet Verstoß, Exit 1 |
| `require-upload='true'` (package-ios) | `IOS_SIGNING_ENABLED='true'` + alle Signierungs-Secrets + alle `IOS_API_*`-Secrets vorhanden | Job-Fehlschlag mit `::error::` und Namen der fehlenden Secrets |
| Manueller Release-Tag | Format `vX.Y.Z` (optionales Suffix) via `parseManualTag` | Skriptfehler (bestehende Regel, unverändert) |
| Automatischer Lauf | Ziel-Tag darf nicht bereits existieren (`tagExists`) | `released=false`, `reason=already-released` (kein Fehler) |
| Upload-Pfade/`upload-artifact` | Keine `.ipa`, keine Verzeichnisse/Muster/Variablen | Bestehende `validate-workflows.py`-Regeln (unverändert) |
| Bestands-Löschung (einmaliger Betriebsschritt) | Vorher: keine `in_progress`/`queued`-Läufe von `release.yml`/`staging-ci.yml` des alten Stands (`gh run list`); `gh release delete <tag>` ausschließlich **ohne** `--cleanup-tag`; Nachweis `gh release list` leer (nach ~60-Min-Beobachtungsfrist wiederholt) und `git ls-remote --tags origin`-Anzahl unverändert | Alt-Lauf noch aktiv/queued → abwarten, nicht löschen; Tag-Differenz vorher/nachher → Abbruch, Tag manuell wiederherstellen |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `require-upload` (Input `package-ios`) | String `'true'`/`'false'` | `'false'` | Verbindlicher TestFlight-Upload in `release.yml`/`staging-ci.yml`; `'false'` = bisheriges Rückfallverhalten (PR-CI) |
| `permissions: contents: write` (`release.yml`, Workflow-Ebene) | bestehend | — | Bleibt: für `git push` des Release-Tags nötig |
| `permissions: contents: write` (`staging-ci.yml`, Job `rc-tag`) | bestehend (vom `prerelease`-Job übernommen) | — | Für `git push` des RC-Tags nötig |
| `GH_TOKEN` env (Schritt „Resolve release version", `release.yml`) | entfernt | — | Keine `gh`-Aufrufe mehr im Skript |

## Seiteneffekte und Risiken

- **Tag-Push triggert nicht rekursiv:** Pushes mit `GITHUB_TOKEN`-Credentials (Standard von `actions/checkout`) erzeugen keinen neuen Workflow-Lauf — der `v*.*.*`-Trigger feuert nicht auf den workflow-eigenen Tag-Push. Diese Korrektheitsannahme wird im `finalize`/`rc-tag`-Schritt kommentiert.
- **Version „verbraucht" erst bei Erfolg:** Da der stabile Tag erst nach erfolgreichem iOS-Upload gepusht wird, kann ein iOS-Fehlschlag durch Workflow-Re-Run wiederholt werden; die TestFlight-Buildnummer steigt über `run_attempt` (Kollision ausgeschlossen).
- **RC-Tag ohne iOS-Ergebnis möglich** (parallele Jobs, wie bisher): ein fehlgeschlagener `ios-prerelease` hinterlässt einen RC-Tag ohne TestFlight-Build — bestehende Semantik, bewusst beibehalten.
- **Kein `gh release`-Ersatz für Release-Notes:** `--generate-notes` entfällt; Release-Kommunikation läuft über `docs/RELEASE_NOTES.md` und `changes.log` (redaktionell, wie bisher gepflegt).
- **Manuelle Tag-Pushes durch Maintainer** lösen den Release-Workflow weiter aus (menschlicher Push ≠ GITHUB_TOKEN) — der bisherige „manuelle Weg" bleibt funktionsfähig.
- **`ios-prerelease`/`ios` schlagen künftig fehl**, wenn Secrets fehlen: In der Einrichtungsphase vor Secret-Hinterlegung würden Staging-Pushes blockieren — gewollt (sichtbar statt still), in `einrichtung.md` dokumentiert.
- **`test-ios-deployment.ps1`/`versioning.test.mjs`/Anwendungstests:** unberührt; die Schlüssel-Härtungstests (`ApiKeyBuildTests_*`, `ApiKeyProviderTests_Resolution`) bleiben gültig, da der Schlüsselmechanismus unverändert bleibt.
- **`check-coverage.mjs`:** betroffene Artefaktliste existiert nicht (Inventory-Korrektur); keine Anpassung nötig.
- **Bestehende 52 GitHub-Releases werden gelöscht** (Anwender-Entscheidung 2026-11-28): irreversibler Verlust der Assets `release-win-x64.zip`/`update.json` — fachlich gewollt; die alten Assets waren schlüsselfrei, es entsteht dadurch kein neuer Sicherheitsbefund. Die Git-Tags bleiben ausdrücklich erhalten (kein `--cleanup-tag`), sonst bräche die Versionsermittlung `determine-next-version.mjs`.
- **Wiederanlage-Risiko bei zu früher Löschung:** Die alten Workflows (Stand vor dem Merge) würden fehlende Releases über den Reparaturpfad (`resolve-release-version.mjs`/`release_action`) bzw. `gh release create` beim nächsten Push auf `main`/`staging` neu anlegen. Daher strikte Sequenzierung: Löschung erst nach Promotion der Workflow-Änderungen nach `main`, danach Verifikation `gh release list` leer.

## Umsetzungsreihenfolge

1. **`scripts/resolve-release-version.mjs` vereinfachen + Tests anpassen**
   - Voraussetzungen: keine (Node 24, `node --test` vorhanden)
   - Beschreibung: GitHub-Release-Abhängigkeiten entfernen, `tagExists`-Injektion einführen, `resolve-release-version.test.mjs` umschreiben; `npm test` grün.

2. **`package-ios` um `require-upload` erweitern**
   - Voraussetzungen: keine
   - Beschreibung: Input deklarieren, „Determine signing mode" um harte Fehlschlag-Pfade ergänzen, Beschreibung aktualisieren.

3. **`release.yml` umbauen**
   - Voraussetzungen: Schritte 1–2 (Skript-Outputs `released`/`release_kind` ohne `release_action`; `require-upload`-Input existiert)
   - Beschreibung: Release-/Reparatur-/Paket-Schritte entfernen, Bedingungen auf `released` vereinfachen, `release_kind`-Output exponieren, `finalize`-Job (Tag-Push) anfügen, `ios`-Job mit `require-upload: 'true'`.

4. **`staging-ci.yml` umbauen**
   - Voraussetzungen: Schritt 2
   - Beschreibung: `prerelease` → `rc-tag` (Tag-Push), `ios-prerelease` mit `require-upload: 'true'`.

5. **Windows-Paketpfad entfernen**
   - Voraussetzungen: Schritte 3–4 (keine Workflow-Referenzen mehr auf `build-and-package`)
   - Beschreibung: `.github/actions/build-and-package/` und `scripts/package-windows.ps1` löschen; `local-ci.ps1` bereinigen (Parameter, Schritt, Kopfdoku).

6. **`validate-workflows.py` verschärfen + Tests anpassen**
   - Voraussetzungen: Schritte 3–5 (Workflows ohne `gh release`, Dateien entfernt — sonst schlagen die neuen Guards fehl)
   - Beschreibung: `gh release`-Verbot implementieren, toten Parsing-Code entfernen, `test_validate_workflows.py` umbauen (Verbot-Tests, `ReleaseMechanismRemovedTests`, `RequireUploadTests` als Guard für `require-upload: 'true'`, `WindowsPackageKeyPolicyTests` entfernen).

7. **ADR 0023 schreiben**
   - Voraussetzungen: keine
   - Beschreibung: Entscheidung, Threat-Model, Rotationsweg, Betreiberhinweise im ADR-Format.

8. **Dokumentation aktualisieren**
   - Voraussetzungen: Schritte 1–7 (Beschreibung soll den Endzustand abbilden)
   - Beschreibung: `docs/help/ci-cd/{workflows,README,einrichtung,versionierung,lokaler-pruefung}.md`, `docs/help/Preisdaten/api-schluessel.md`, `docs/RELEASE_NOTES.md`, `changes.log`, ggf. `README.md`.

9. **Gesamtprüfung**
   - Voraussetzungen: Schritte 1–8
   - Beschreibung: `npm test`, `python scripts/test_validate_workflows.py`, `python scripts/validate-workflows.py`, `pwsh scripts/test-ios-deployment.ps1`; optional `scripts/local-ci.ps1`.

10. **Bestands-Releases löschen (einmaliger Betriebsschritt, nach Merge)**
    - Voraussetzungen: Schritte 1–9 gemergt und nach `main` promoviert (PR → `staging` → Promotion nach `main`) — erst dann existiert kein Workflow-Pfad mehr, der Releases wieder anlegen würde; `gh`-CLI des Ausführenden mit `repo`-Scope
    - Beschreibung: In-flight-Prüfung `gh run list --workflow release.yml --status in_progress|queued` und `--workflow staging-ci.yml` (keine Alt-Läufe vor dem Merge-Commit mehr aktiv; ggf. abwarten) → `git ls-remote --tags origin` (Soll-Tag-Anzahl erfassen) → `gh release list` → Schleife `gh release delete <tag> --yes` **ohne** `--cleanup-tag` → Verifikation: `gh release list` leer (nach ~60-Min-Beobachtungsfrist wiederholen, abschließender Check nach allen Merges), `git ls-remote --tags origin`-Anzahl unverändert. Einmalige Remote-Operation, kein CI-Job; Runbook-Abschnitt in ADR 0023.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `resolveReleaseVersion` automatisch: Tag fehlt → `new-release` | `scripts/resolve-release-version.test.mjs` | `tagExists`-Stub `false` → `released="true"` |
| `resolveReleaseVersion` automatisch: Tag vorhanden → `already-released` | `scripts/resolve-release-version.test.mjs` | `tagExists`-Stub `true` → `released="false"` |
| `resolveReleaseVersion` manuell: Tag-Push → released ohne Existenzprüfung | `scripts/resolve-release-version.test.mjs` | `determineVersion` darf nicht aufgerufen werden |
| `resolveReleaseVersion` automatisch: `changed=false` → `no-releasable-commits` | `scripts/resolve-release-version.test.mjs` | keine Tag-Prüfung nötig |
| `test_gh_release_commands_are_rejected` | `scripts/test_validate_workflows.py` (`ReleaseCommandTests`) | `gh release create/upload/edit/delete` in `run:` → Fehler; `gh release view` → sauber |
| `test_release_mechanism_files_removed` | `scripts/test_validate_workflows.py` (`ReleaseMechanismRemovedTests`, neu) | `build-and-package/action.yml` und `package-windows.ps1` existieren nicht; alle Repo-Workflows ohne `gh release` |
| `test_release_and_staging_require_upload` | `scripts/test_validate_workflows.py` (`RequireUploadTests`, neu) | YAML-geladene Workflows: `release.yml` Job `ios` und `staging-ci.yml` Job `ios-prerelease` rufen `package-ios` mit `with.require-upload == 'true'` auf; `package-ios/action.yml` deklariert den Input — schlägt bei stillem Rückbau des verbindlichen Uploads fehl |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `scripts/resolve-release-version.test.mjs` (gesamte Datei) | Injektions-API ändert sich (`tagExists` statt `getRelease`/`listReleases`); Reparatur-/Asset-Tests entfallen |
| `ReleaseCommandTests.test_current_release_commands_are_allowed` (`test_validate_workflows.py`) | Die referenzierten `gh release`-Aufrufe existieren nicht mehr und sind künftig verboten |
| `ReleaseCommandTests.test_ipa_asset_is_rejected`, `test_directory_pattern_and_variable_assets_are_rejected`, `test_label_suffix_and_other_commands_are_handled` | Erwartungen ändern sich: `gh release`-Mutationen sind immer Fehler |
| `WindowsPackageKeyPolicyTests` (`test_validate_workflows.py`) | Geprüfte Dateien werden gelöscht → Klasse entfällt |

### E2E-Tests (primärer Funktionsnachweis)

Es werden keine E2E-Tests geplant. Die Anforderung berührt keinen über die App-UI erreichbaren Benutzerfluss: Alle Änderungen liegen in GitHub-Actions-Workflows, CI-Skripten und Dokumentation. Es gibt keine neue oder geänderte Benutzerinteraktion, keinen Dialog und kein Anwendungsverhalten zur Laufzeit — die App selbst bleibt unverändert (der Schlüsselmechanismus ist explizit aus dem Scope ausgeschlossen). Der Funktionsnachweis erfolgt über die Skripttests (`npm test`, `test_validate_workflows.py`), die Workflow-Validierung und den ersten realen Pipeline-Lauf auf `staging`/`main` (siehe Akzeptanzszenarien).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path stabiler Release | Push auf `main` mit freigabefähigem Commit | `staging` in `main` gemergt; letzter stabiler Tag `v0.10.0`; Secrets/Variablen vollständig (`IOS_SIGNING_ENABLED='true'`) | `release` ermittelt `v0.11.0` (`tagExists` → false) → Release-Gate-Tests → `ios` baut signiert, validiert `.ipa`, lädt per iTMSTransporter hoch → `finalize` pusht Tag `v0.11.0` | Git-Tag `v0.11.0` auf `origin`; **kein** GitHub-Release/Asset; neuer Build in App Store Connect/TestFlight | Realer Workflow-Lauf nach Merge nach `staging`→`main`; vorab `npm test` + `validate-workflows.py` |
| 2 | Push ohne freigabefähige Commits | Push auf `main` ohne `feat:`/`fix:`-Commits | — | `resolve` → `changed=false` | `released=false`; Jobs `ios`/`finalize` übersprungen; kein Tag, kein Release | Skripttest + Workflow-Log |
| 3 | Manueller Release-Tag | Maintainer pusht Tag `v1.0.0` | Tag existiert auf `origin` | `resolve` → `manual` → released; Tests, iOS-Build+Upload; `finalize` übersprungen | TestFlight-Build `1.0.0`; kein GitHub-Release; kein doppelter Tag-Push | Skripttest (manual-Pfad) + Workflow-Log |
| 4 | RC-Lauf | Push auf `staging` mit freigabefähigen Commits | `changed=true`, `rc_tag=v0.11.0-rc.2` | `rc-tag` pusht Git-Tag; `ios-prerelease` lädt nach TestFlight | Git-Tag `v0.11.0-rc.2`; kein GitHub-Pre-Release; TestFlight-RC-Build | Realer Lauf auf `staging` |
| 5 | Fehlende iOS-Konfiguration beim Release | Push auf `main`, aber `IOS_SIGNING_ENABLED` oder Secrets fehlen | freigabefähiger Commit | `package-ios` mit `require-upload='true'` schlägt im Schritt „Determine signing mode" fehl | Job/Workflow rot mit `::error::` und Secret-Namen; **kein** Tag gepusht (finalize nicht erreicht) — die Blockade ist im Workflow-Lauf eindeutig sichtbar | `RequireUploadTests` (`test_validate_workflows.py`) + Workflow-Log |
| 6 | Wiederaufnahme nach iOS-Fehlschlag | Re-Run des Release-Workflows (oder nächster `main`-Push) | iOS-Job zuvor fehlgeschlagen; Tag `v0.11.0` existiert noch nicht | `resolve` → `tagExists=false` → `released=true`; voller Lauf inkl. erneutem TestFlight-Upload (Buildnummer über `run_attempt` eindeutig); danach Tag-Push | Version wird nicht „verbrannt"; erfolgreiche Wiederholung liefert Tag + TestFlight-Build | Workflow-Log; Skripttest already-released vs. new-release |
| 7 | Regressionsschutz | Jemand fügt `gh release create` in einem Workflow ein oder entfernt/setzt `require-upload: 'true'` zurück | PR mit `gh release`-Aufruf bzw. ohne verbindlichen Upload-Input | `python scripts/validate-workflows.py` bzw. `RequireUploadTests` | Exit 1 mit Verstoßmeldung; `test_workflows_in_repository_are_clean`/`RequireUploadTests` rot — „Release ohne Auslieferung" kann nicht still zurückkehren | `test_validate_workflows.py` |
| 8 | Bestands-Löschung der Releases | Manueller Betriebsschritt nach Merge/Promotion auf `main` | 52 Releases (stabil + RC-Pre) vorhanden; Workflow-Änderungen auf `main` aktiv; `gh` mit `repo`-Scope authentifiziert | `gh run list` auf `in_progress`/`queued`-Läufe von `release.yml`/`staging-ci.yml` prüfen (keine Alt-Läufe; ggf. abwarten) → `git ls-remote --tags origin` (Soll-Anzahl erfassen) → `gh release list` → Schleife `gh release delete <tag> --yes` ohne `--cleanup-tag` → Verifikation (nach ~60-Min-Beobachtungsfrist wiederholt) | `gh release list` leer (auch nach Beobachtungsfrist); alle Git-Tags `v*` vollständig erhalten (Anzahl identisch); kein anschließender Workflow-Lauf legt ein Release an | Reale Ausführung + Nachweis `gh release list` (leer, wiederholt) und `git ls-remote --tags origin` (unverändert) |

## Offene Punkte

Keine. Die sechs in der Anforderung genannten offenen Fragen wurden als getroffene Annahmen festgelegt und vom Anwender am 2026-11-28 bestätigt (Annahme 4 dabei zur Löschung der Bestands-Releases geändert) — alle Punkte sind in den Plan eingearbeitet.
