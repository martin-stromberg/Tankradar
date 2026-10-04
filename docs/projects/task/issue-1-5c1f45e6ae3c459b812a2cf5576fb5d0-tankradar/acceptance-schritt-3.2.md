# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Abweichungen gefunden

## Abweichungen

- [ ] **Der iOS-Build in `package-ios` scheitert auf dem macOS-Runner voraussichtlich an fehlenden Workloads für die übrigen Zielframeworks.** Die Vorlage ist auf diesen Fall vorbereitet. Sie installiert nur `maui-ios ios` und blendet die Nicht-iOS-Zielframeworks ausdrücklich aus: `IncludeAndroidTarget: false` bei Workload-Installation, Restore und Publish, `IncludeIosTarget: false` in den übrigen Jobs. Tankradar übernimmt die reine iOS-Workload-Installation (`dotnet workload install maui-ios ios --skip-manifest-update`), streicht aber den Ausblendungsmechanismus. Die `Tankradar.MAUI.csproj` hat auf macOS weiterhin die Zielframeworks `net10.0-android;net10.0-ios;net10.0-maccatalyst`, unbedingt und ohne Schalter.

  Der Kommentar im Baustein lautet: „`dotnet publish/build -f net10.0-ios` stellt über TargetFramework sicher, dass nur das iOS-Framework wiederhergestellt wird“. Das trifft nicht zu. Der implizite Restore wertet alle Zielframeworks aus und bricht bei fehlender Workload mit `NETSDK1147` ab. Praktisch nachgestellt im Scratchpad mit SDK 10.0.401: ein Projekt mit `net10.0-tvos;net10.0` ohne tvOS-Workload, gebaut mit `dotnet build -f net10.0` bzw. `dotnet publish -f net10.0`. Ergebnis: `error NETSDK1147 … tvos`.

  Die Vorgängerfassung (`build-ios`) umging das Problem mit `dotnet workload restore Tankradar.sln`. Betroffen ist jeder iOS-Lauf:
  - der unsignierte Pflicht-Check `ios build` in `pr-staging-ci.yml`, der laut Checkliste Pflicht-Statusprüfung für `staging` ist,
  - `ios prerelease` in `staging-ci.yml`,
  - `ios` in `release.yml`.

  Damit würde das in der Vorlage funktionierende Deployment für Tankradar voraussichtlich brechen, und auch die Vorgabe „ohne Signierungsdaten unsigniert bauen und nicht fehlschlagen“ wäre verletzt. Abhilfe in Richtung Vorlage wäre:
  - entweder die Nicht-iOS-Zielframeworks in der csproj an eine Eigenschaft koppeln und diese im Baustein setzen,
  - oder zusätzlich `android`/`maccatalyst` (bzw. `maui`) im gepinnten Band installieren.

- [ ] **Die Store-Prüfung auf `PrivacyInfo.xcprivacy` wurde mit sachlich falscher Begründung zur Warnung abgeschwächt.** Die Vorlage behandelt ein fehlendes Privacy-Manifest im Bundle-Root als Packaging-Fehler (`FEHLER … exit 1`). `scripts/iOS-Deployment.ps1` (Funktion `Invoke-IpaValidation`, ca. Z. 731–739) gibt stattdessen nur eine Warnung aus. Die Begründung im Code lautet „Tankradar liefert derzeit kein Privacy-Manifest mit“, in `docs/help/ci-cd/ios-deployment.md` steht, die „Pflicht-`PrivacyInfo.xcprivacy`“ gelte „für Tankradar nicht“. Beides ist falsch:
  - `src/Tankradar.MAUI/Platforms/iOS/Resources/PrivacyInfo.xcprivacy` ist seit Schritt 1 (Commit `c9979c5`) versioniert.
  - Die Datei liegt im Standard-Ressourcenordner, den MAUI ins Bundle-Root übernimmt.
  - Sie enthält das für .NET-MAUI-Apps nötige Mindest-Manifest (FileTimestamp, SystemBootTime, DiskSpace). Laut Kommentar in der Datei ist es wegen der Required-Reason-APIs von .NET/MAUI selbst erforderlich.

  Die Vorlagen-Invariante gilt also auch für Tankradar. Durch die Abschwächung bleibt eine Packaging-Regression, etwa wenn die Datei verloren geht, vor dem Upload unentdeckt und fällt erst bei App Store Connect auf (ITMS-91053). Die Prüfung muss wie in der Vorlage ein Fehler bleiben, und die Dokumentation muss entsprechend korrigiert werden.

## Hinweise

**Behebung der Abweichung aus `acceptance-schritt-3.1.md` (E2E-Diagnose-Artefakte): behoben.**

- Die Diagnose ist in der Testbasis verdrahtet: `E2ETestBase.RunWithDiagnostics` und `E2EDiagnostics` erzeugen pro fehlgeschlagenem Test eine `.png`, eine `.uitree.txt` und eine `.error.txt`.
- `NavigationE2ETests` nutzt den Mechanismus.
- `DiagnosticsCaptureE2ETests` belegt, dass die Dateien entstehen bzw. bei Erfolg ausbleiben.
- Beide Workflows (`pr-staging-ci.yml`, `staging-ci.yml`) haben den Schritt `Upload E2E diagnostics` mit `if: always()`.
- `e2e-diagnostics/` ist in `.gitignore`, und `docs/help/ci-cd/workflows.md` beschreibt den Ablauf.

**Praktisch verifiziert (ohne Seiteneffekte):**

- `scripts/local-ci.ps1` lief vollständig **mit** E2E und endete mit Exit-Code 0 („Lokaler Prüflauf erfolgreich“). Ergebnisse:
  - Node-Tests 24/24
  - Workflow-Validierung OK
  - iOS-Deployment-Skriptprüfung OK
  - Restore, Formatprüfung, Sicherheitsprüfung OK
  - statische Analyse mit Warnungen als Fehler OK
  - Unit 15/15, Integration 3/3
  - Zeilenabdeckung 73,2 % bei einer Schwelle von 70 %
  - FlaUI-E2E 3/3
- Separat ausgeführt:
  - `npm test`: 24/24 grün
  - `python scripts/validate-workflows.py`: „OK: 7 Workflows …“. `actionlint` ist lokal nicht vorhanden, daher nur die eingebauten Prüfungen.
  - `scripts/test-ios-deployment.ps1`: alle Prüfungen OK, darunter Syntax, Hilfe, Bundle-ID an drei Stellen, sauberer Abbruch ohne Mac für alle Aktionen und die Hilfsfunktionen.
- Nicht ausgeführt: das Windows-Paket (`-Package`) sowie alle macOS-/GitHub-gebundenen Teile, also Workload-Pinning, Keychain, Signierung, iTMSTransporter und Upload.

**Übernahme der Vorlage `package-ios` (im Übrigen sachgerecht):**

- Übernommen sind:
  - SDK-Band-Pinning per `global.json` (nur im CI-Workspace)
  - Workload-Pinning per Rollback-File mit `--skip-manifest-update`
  - Entfernen fremder iOS-Packs
  - Keychain-Import mit Zufallspasswort, `set-key-partition-list` und Aufräumen mit `if: always()` (zusätzlich `.p8` und Profil)
  - Buildnummer aus Commit-Anzahl + `run_attempt` − 1
  - Display-Version ohne Pre-Release-Suffix (`${RELEASE_VERSION%%-*}`)
  - `MauiXamlInflator=XamlC`
  - `.ipa`-Suche mit Deduplizierung
  - iTMSTransporter-Suche mit derselben Kandidatenliste und Standalone-Fallback (zusätzlich mit Prüfung der Apple-Signatur)
  - Gate `IOS_SIGNING_ENABLED` mit allen `IOS_*`-Secrets
- Abweichung beim Entfernen fremder Packs: Tankradar löscht nur das Versionsverzeichnis (`rm -rf "$d"`), die Vorlage das übergeordnete Pack-Verzeichnis (`rm -rf "$(dirname "$d")"`). Die Abweichung ist nicht kommentiert. Nach den Workload-Manifesten erfolgt die Auflösung über Pack-Version und -Pfad, ein leer zurückbleibendes `Microsoft.iOS.Sdk.net10.0_<TPV>`-Verzeichnis dürfte also unschädlich sein. Ein Beleg auf macOS steht aber aus.
- Bewusste und nachvollziehbare Erweiterungen gegenüber der Vorlage:
  - Das Gate entscheidet innerhalb des Bausteins. Ohne Variable bzw. Secrets wird unsigniert für den Simulator gebaut, statt den Job zu überspringen. Das ist durch die Projektvorgabe „iOS wird mitgebaut, ohne Signierung kein Fehlschlag“ begründet.
  - Optionaler Parameter `xcode-version`.
  - `APP_BUNDLE_ID` als Override.
- Unterschied zur Vorlage beim Release: Die signierte `.ipa` ist nur ein Workflow-Artefakt (14 Tage) und kein GitHub-Release-Asset. Für iOS-Assets gibt es auch keinen Reparaturpfad (`checkout-release-tag`). Die Vorlage hängt die `.ipa` per `release-assets.mjs` an das Release. Das ist in `docs/help/ci-cd/README.md` beschrieben, als Release-Artefakt fordert die Anforderung nur das Windows-ZIP, und die Auslieferung läuft über TestFlight. Deshalb ist das hier nicht als Abweichung gewertet.

**`scripts/iOS-Deployment.ps1` im Vergleich zu `drafts/iOS-Deployment.ps1`:**

- Angepasst wurden Projektpfad, `Tankradar.MAUI.app`, das Präfix `TANKRADAR_IOS_*`, `-BundleId` / `TANKRADAR_IOS_BUNDLE_ID` und das Lesen der Bundle-ID aus `<ApplicationId>`.
- Die Umbenennung `$isWindows`/`$isMacOS` → `$onWindows`/`$onMacOS` ist begründet und dokumentiert.
- Alle Aktionen sind erhalten: build, simulator, device, store, upload, list, menu.
- Die Abschwächungen bei Gerätefamilie (universal statt iPhone-only), Lokalisierungen und `ITSAppUsesNonExemptEncryption` passen zur tatsächlichen `Info.plist` von Tankradar und sind dokumentiert. Empfehlung: `ITSAppUsesNonExemptEncryption=false` in die `Info.plist` aufnehmen, damit App Store Connect die Exportkonformität nicht bei jedem Build abfragt.

**Einrichtungs-Checkliste:**

- Die Checkliste nennt alle geforderten Apple- und GitHub-Schritte:
  - App-ID/Bundle-ID
  - App-Eintrag in App Store Connect
  - Distribution-Zertifikat als `.p12`
  - Provisioning-Profil
  - App-Store-Connect-API-Key
  - Secrets und Variablen
- Die Namen stimmen exakt mit den Workflows überein:
  - neun Secrets (`IOS_*`, `FUEL_PRICE_API_KEY`, `ROUTING_API_KEY`)
  - drei Variablen (`IOS_SIGNING_ENABLED`, `APP_BUNDLE_ID`, `IOS_XCODE_VERSION`)
- Redaktioneller Fehler: In `docs/help/ci-cd/einrichtung.md`, Abschnitt 8, stehen der letzte Checklistenpunkt („Nach dem Setzen von `IOS_SIGNING_ENABLED=true` …“) und der Absatz „Was **nur** auf GitHub/macOS geprüft werden kann …“ doppelt (Z. 110–117 und 118–125).

**Bundle-ID und Ablage der Vorlage:**

- Die Bundle-ID `de.martinstromberg.tankradar` steht einheitlich an allen relevanten Stellen:
  - csproj `ApplicationId`
  - `AppConfiguration.DefaultBundleId`
  - `Resources/Raw/appsettings.json`
  - README
  - `einrichtung.md`, `ios-deployment.md`
  - `test-ios-deployment.ps1`
  - Unit-Test `AppConfigurationTests_BundleId`
- Der alte Platzhalter findet sich nur noch in historischen Prüfberichten und im ignorierten Build-Rest `artifacts/publish-win-x64/`.
- Veraltet ist der Satz „bis dahin gilt der bestehende Platzhalter“ in `project-plan.md` (Schritt 3, Ergänzung). Dasselbe gilt für die Aussage in Schritt 18 und in der Tabelle „Grobe Vorgehensentscheidungen“, die Bundle-ID liege nie im Repository. Beides steht jetzt im Widerspruch zur Stakeholder-Freigabe der Bundle-ID und sollte bei Gelegenheit nachgezogen werden.
- `drafts/` ist in `.gitignore` (Z. 471), nicht im Index und in keinem Commit der Historie enthalten (`git log --all -- drafts` ist leer).
- Aus `drafts/github/` nicht übernommen wurden `deploy-pages.yml`, `pull_request_template.md` und `checkout-release-tag`. Für Tankradar besteht dafür kein Bedarf (keine Pages, kein iOS-Reparaturpfad). Eine Begründung ist nicht dokumentiert.
