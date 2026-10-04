# Plan
1. `.gitignore`: `drafts/`, `.ios-deploy.user.json`, `.ios-deploy-*.rsp`, `logs/`.
2. `scripts/iOS-Deployment.ps1` aus der Vorlage: Projektpfad Tankradar.MAUI, `TANKRADAR_IOS_*`, `-BundleId` (`-p:ApplicationId`), Bundle-ID aus csproj statt Info.plist, App-Name `Tankradar.MAUI`; Store-Validierung ohne Reporter-spezifische Invarianten (iPhone-only, en+de, PrivacyInfo werden Warnungen bzw. entfallen); Lizenzzeile entfernt.
3. `.github/actions/package-ios` (ersetzt `build-ios`): gepinnte SDK-/Workload-Version, Signiermodus über `signing-enabled` + vollständige Eingaben, sonst unsignierter Simulator-Build (PR-Compile-Check); Buildnummer aus Commit-Anzahl; signierte .ipa, optional TestFlight-Upload; Keychain-Aufräumen. Abweichung: `dotnet publish -f` ohne separates `restore -r`, da das Projekt mehrere TFMs hat (Restore bleibt auf iOS beschränkt).
4. Workflows `pr-staging-ci.yml`, `staging-ci.yml`, `release.yml`: Aufrufe auf `package-ios` umstellen, `fetch-depth: 0`, Secrets `IOS_*`, Variable `IOS_SIGNING_ENABLED`; `APPLE_TEAM_ID`/`IOS_CODESIGN_KEY`/`IOS_PROVISIONING_PROFILE` als Variablen entfallen (jetzt Secrets).
5. `scripts/test-ios-deployment.ps1` (Parser-, Get-Help-, Abbruch-Tests unter Windows) und Aufruf in `scripts/local-ci.ps1` und im CI-Schritt „static checks".
6. Doku: `einrichtung.md`, `workflows.md`, `README.md`, `lokaler-pruefung.md` unter `docs/help/ci-cd/`, neue Seite `ios-deployment.md`; Hauptlinks.
7. Verifikation: `validate-workflows.py`, Testskript, `local-ci.ps1`.

## Offene Punkte
