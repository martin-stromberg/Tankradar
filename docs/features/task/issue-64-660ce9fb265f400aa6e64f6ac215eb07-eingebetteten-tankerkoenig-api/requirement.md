# Übersetzte Anforderung – Issue #64: Ablage der Releases aus GitHub entfernen, Auslieferung nur über den App Store

## Fachliche Zusammenfassung

Der Repository-Eigentümer entscheidet zu Issue #64 das Verteilungsmodell neu: Build-Artefakte werden nicht mehr als GitHub-Releases abgelegt; die App wird ausschließlich über App Store Connect (TestFlight/App Store) ausgeliefert. Der gemeinsame Tankerkönig-API-Schlüssel bleibt dabei bewusst im iOS-Binary eingebettet (Risikoentscheidung statt Entfernung) — die Entscheidung ist mit Threat-Model, Rotationsweg und Betreiberhinweisen zu dokumentieren. Der bisherige GitHub-Release-Pfad (`gh release create`/`gh release upload` mit `release-win-x64.zip` und `update.json` in `release.yml` und `staging-ci.yml`) entfällt vollständig.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein „Release-Lauf" je Release-Ereignis — bestehend aus Versionsermittlung, Build und Auslieferung. Zusätzlich die einmalige Entscheidungs- und Risikodokumentation als verbindlicher Teil des Scopes (Akzeptanzkriterium von Issue #64).
- **Auslösende Ereignisse:** Die bestehenden Trigger bleiben: Push auf `main` (automatische Version) und manueller Tag `v*.*.*` in `.github/workflows/release.yml`; Push auf `staging` mit freigabefähigen Commits für den RC-Lauf in `.github/workflows/staging-ci.yml`.
- **Erwartete Wirkung:** Nach einem Release-Lauf existiert kein GitHub-Release und kein GitHub-Release-Asset mehr; die bereits existierenden GitHub-Releases werden einmalig gelöscht (Anwender-Entscheidung 2026-11-28, siehe Nachtrag), die Git-Tags bleiben als Versionsbasis vollständig erhalten. Als einziges sichtbares Auslieferungsergebnis liegt die signierte iOS-`.ipa` in App Store Connect (TestFlight). Die Risikoentscheidung (gemeinsamer Schlüssel im Binary, Vertrieb nur über den Apple-Kanal) ist nachvollziehbar dokumentiert.
- **Umgang mit fehlenden Objekten:**
  - **Release-Tag `v*.*.*`:** Wurde bisher implizit von `gh release create` erzeugt. Muss **ersetzt/erzeugt** werden (`git tag` + Push oder `gh api`), sonst findet `scripts/determine-next-version.mjs` (`git tag --merged HEAD --list "v*"`) keinen letzten Tag und die Versionsermittlung versucht bei jedem Lauf dieselbe Version erneut.
  - **Release-Existenz-/Reparaturprüfung:** `scripts/resolve-release-version.mjs` fragt GitHub-Releases ab (`ghGetRelease`, `ghListReleases`, `EXPECTED_ASSETS`, `release_action: "upload-existing"`). Die Asset-Reparatur entfällt ersatzlos (**verwerfen**); die „already-released"-Erkennung ist auf Git-Tags umzustellen (**wiederverwenden**, andere Datenquelle).
  - **`update.json`:** Die Download-URL zeigt auf `https://github.com/.../releases/download/...`; ohne Release-Ablage hat das Manifest kein gültiges Ziel. Es existiert kein Consumer im App-Code (kein Auto-Updater). → **verwerfen**.
  - **Windows-Paket `release-win-x64.zip`:** Ohne GitHub-Release existiert kein Verteilungskanal. → **verwerfen** oder nur noch als nicht veröffentlichtes Build-Ergebnis erzeugen (offene Frage 2).
- **Scope-Ausschlüsse:**
  - Keine Änderung am Schlüsselmechanismus: `src/Tankradar.MAUI/MSBuild/TankerkoenigApiKey.targets` und `src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs` bleiben unverändert; der gemeinsame Schlüssel wird weiterhin per `AssemblyMetadata("TankerkoenigApiKey", ...)` in die iOS-App kompiliert. Kein benutzergebundener Schlüssel, kein Backend/Proxy.
  - Keine Automatisierung der App-Store-Einreichung/des App-Reviews: Der Upload endet in App Store Connect (TestFlight-Upload per iTMSTransporter, wie bisher); die Einreichung bleibt manuell (Annahme, siehe offene Frage 1).
  - ~~Keine Löschung bereits existierender GitHub-Releases~~ — entfallen per Anwender-Entscheidung (2026-11-28): Die Bestands-Releases **sollen gelöscht** werden (`gh release delete`, Git-Tags bleiben erhalten); siehe Nachtrag unten.
  - Kein neuer Verteilungskanal für die Windows-Variante.
  - Keine Änderung an der Versionslogik selbst (`scripts/determine-next-version.mjs`, `scripts/versioning.mjs`): die tagbasierte 0.x-Regel bleibt.

## Betroffene Klassen und Komponenten

Es werden keine Anwendungsklassen (Datenmodell, Services, Interfaces, Enums, UI) angelegt oder geändert — die Umsetzung liegt vollständig auf CI-/Release-Ebene plus Dokumentation:

- **`.github/workflows/release.yml`:** Schritte „Create GitHub release" und „Upload asset to existing GitHub release" entfernen; Tag-Erzeugung durch `git tag`/`git push` (oder `gh api`) ersetzen; Windows-Build über `./.github/actions/build-and-package` entfernen oder umwidmen; `permissions: contents: write` ist für den Tag-Push weiterhin nötig.
- **`.github/workflows/staging-ci.yml`:** Schritt „Create GitHub pre-release" entfernen; RC-Tag ggf. ebenfalls per `git tag` setzen, damit `determine-next-version.mjs --rc` weiter zählen kann.
- **`scripts/resolve-release-version.mjs` + `scripts/resolve-release-version.test.mjs`:** `ghGetRelease`/`ghListReleases`/`EXPECTED_ASSETS` und der `upload-existing`-Reparaturpfad entfernen; „already-released" anhand der Git-Tag-Existenz prüfen.
- **`.github/actions/build-and-package/action.yml` und `scripts/package-windows.ps1`:** entfallen ganz oder werden auf eine reine Build-Prüfung ohne Veröffentlichung reduziert; `update.json` mit `releases/download`-URL entfällt. Betroffen ist auch `scripts/local-ci.ps1` (`-Package`) und `scripts/check-coverage.mjs`-nahe Artefaktlisten (Erwähnung `artifacts/update.json`).
- **`scripts/validate-workflows.py` + `scripts/test_validate_workflows.py`:** Die Regeln für `gh release create/upload`-Assets (IPA-Verbot, Positivliste) anpassen — nach Entfernung der `gh release`-Aufrufe bleibt das IPA-Verbot als Schutz sinnvoll oder wird obsolet.
- **`.github/actions/package-ios/action.yml`:** Bleibt der einzige Auslieferungspfad (signierter Build → IPA-Validierung → iTMSTransporter-Upload). Ggf. TestFlight-Upload von optional auf verbindlich umstellen (offene Frage 5).
- **Dokumentation:** `docs/help/ci-cd/workflows.md`, `docs/help/ci-cd/README.md` (Tabelle „Release-Artefakte"), `docs/help/ci-cd/einrichtung.md`, `docs/help/ci-cd/versionierung.md`, `docs/RELEASE_NOTES.md`, `README.md`; **neuer ADR** unter `docs/adr/` für die Verteilungs-/Risikoentscheidung inkl. Threat-Model, Rotationsweg (`FUEL_PRICE_API_KEY`-Secret-Rotation + neuer Release) und Betreiberhinweisen (Missbrauchsbeobachtung beim Tankerkönig-Dienst).
- **Tests:** Keine Anwendungstests nötig; Verifikation über `scripts/validate-workflows.py` und die Node-Skripttests (`*.test.mjs`).

## Implementierungsansatz

- Alle `gh release create`-/`gh release upload`-Schritte aus `release.yml` und `staging-ci.yml` entfernen; den Release-Tag stattdessen explizit setzen und pushen (`git tag` + `git push origin <tag>`), damit die tagbasierte Versionsermittlung unverändert funktioniert.
- `scripts/resolve-release-version.mjs` vereinfachen: Existenzprüfung über Git-Tags statt GitHub-Releases; `release_action`/`upload-existing`-Reparatur und `EXPECTED_ASSETS` entfallen (kein Artefakt mehr, das repariert werden könnte).
- Der iOS-Job (`package-ios`) bleibt der einzige Produktivpfad: signierter Build bei `IOS_SIGNING_ENABLED='true'`, IPA-Validierung, Upload per iTMSTransporter nach App Store Connect mit `IOS_API_KEY_ID`/`IOS_API_ISSUER_ID`/`IOS_API_KEY_P8`.
- Entscheidungsdokumentation als ADR (Fortsetzung der ADR-Nummerierung nach `0022-android-backup.md`): verbleibendes Risiko (Extraktion des Schlüssels aus dem Store-Binary), begrenzende Faktoren (signierte, verschlüsselte Store-Auslieferung statt frei herunterladbarer Artefakte), Rotationsweg und Betreiberhinweise gemäß den Akzeptanzkriterien von Issue #64.

## Konfiguration

- Secrets/Repository-Variablen unverändert: `FUEL_PRICE_API_KEY` (iOS-Build), `IOS_*`-Signierungs- und App-Store-Connect-Secrets, `APP_BUNDLE_ID`, `IOS_XCODE_VERSION`, `IOS_SIGNING_ENABLED`.
- `permissions: contents: write` in `release.yml` bleibt für den Tag-Push erforderlich; in `staging-ci.yml` (Job `prerelease`) prüfen, ob die Berechtigung nach Entfernen des Release-Schritts reduziert werden kann (bzw. für einen RC-Tag-Push weiter nötig ist).

## Offene Fragen

1. **Bedeutung von „App Store":** Ist der bestehende TestFlight-Upload (iTMSTransporter → App Store Connect) gemeint, oder soll zusätzlich die Einreichung zum App-Store-Review/die Store-Auslieferung automatisiert werden? Letzteres erfordert einen neuen Mechanismus (App-Store-Connect-API/fastlane) und ist deutlich mehr Aufwand.
2. **Windows-Variante:** Entfällt die Windows-Auslieferung vollständig (Build, `release-win-x64.zip`, `update.json` und der Job in `release.yml`/`staging-ci.yml` entfallen), oder wird das Paket weiterhin gebaut — etwa als Workflow-Artefakt oder reine Build-Prüfung — ohne es zu veröffentlichen?
3. **RC-Pre-Releases:** Gilt die Entfernung auch für `staging-ci.yml` (dort wird aktuell je RC ein GitHub-Pre-Release mit dem Windows-Paket erzeugt)? Ohne Ablage bliebe als RC-Ergebnis nur der iOS-/TestFlight-Build.
4. **Bestehende GitHub-Releases:** Sollen die bereits vorhandenen Releases im öffentlichen Repository gelöscht werden (`gh release delete`, Tags dabei ausdrücklich **behalten**, da sie die Versionsbasis bilden) oder unverändert stehen bleiben? → **Beantwortet (Anwender, 2026-11-28):** Ja — die Bestands-Releases werden gelöscht (`gh release delete` ohne `--cleanup-tag`, Tags bleiben); siehe Nachtrag.
5. **Verbindlichkeit des TestFlight-Uploads:** Soll der Upload künftig Pflicht sein (Job schlägt fehl, wenn `IOS_SIGNING_ENABLED`/Secrets fehlen), statt wie bisher still auf einen unsignierten Compile-Check zurückzufallen? Ohne GitHub-Release gäbe es sonst Releases ohne jede Auslieferung.
6. **Ort der Risikodokumentation:** Reicht ein neuer ADR unter `docs/adr/` für Entscheidung, Threat-Model, Rotationsweg und Betreiberhinweise, oder ist zusätzlich ein Abschnitt in `docs/help/` (z. B. `docs/help/ci-cd/`) gewünscht?

## Nachtrag — Anwender-Entscheidungen (2026-11-28)

Der Repository-Eigentümer hat den Umsetzungsplan im Issue-Kommentar vom 2026-11-28 bewertet („3. Die bestehenden GitHub-Releases sollen gelöscht werden. Ansonsten passt es so."):

- **(a) Löschung der Bestands-Releases ist gewünscht:** Damit ist offene Frage 4 beantwortet. Die bestehenden GitHub-Releases (aktuell 52 Stück — stabile Releases und RC-Pre-Releases, Stand 2026-11-28) sollen einmalig gelöscht werden (`gh release delete <tag>` **ohne** `--cleanup-tag` — die Git-Tags `vX.Y.Z`/`vX.Y.Z-rc.N` bleiben als Versionsbasis von `scripts/determine-next-version.mjs` erhalten). Ausführung als einmaliger Betriebsschritt, sequenziert **nach** dem Merge der Workflow-Änderungen auf `main` (vorher würden die alten Workflows fehlende Releases wieder anlegen); Nachweis: `gh release list` leer + `git ls-remote --tags origin` unverändert.
- **(b) Alle übrigen Scope-Festlegungen und Annahmen bestätigt:** Die verbleibenden Scope-Ausschlüsse (Schlüsselmechanismus unverändert, keine Review-Automatisierung, kein Windows-Verteilungskanal, Versionslogik unverändert) sowie die im Plan getroffenen Annahmen 1, 2, 3, 5 und 6 sind vom Anwender bestätigt.
