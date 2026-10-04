# Bestandsaufnahme

- Vorlage: `drafts/iOS-Deployment.ps1`, `drafts/github/actions/package-ios/action.yml`, `drafts/github/workflows/*` (Projekt Reporter; nicht versioniert).
- Vorhanden: `.github/actions/build-ios` (signiert/unsigniert, Variablen-basiert), Aufrufe in `pr-staging-ci.yml` (`ios-build`), `staging-ci.yml` (`ios-prerelease`), `release.yml` (`ios`).
- `src/Tankradar.MAUI/Tankradar.MAUI.csproj`: TFMs android/ios/maccatalyst/windows, `ApplicationId` Platzhalter, `ApplicationVersion` 1. `Platforms/iOS/Info.plist` ohne CFBundleIdentifier, UIDeviceFamily iPhone+iPad, ohne Lokalisierungen, ohne PrivacyInfo.
- Doku `docs/help/ci-cd/{README,einrichtung,workflows,lokaler-pruefung}.md` nennt `build-ios` und die alten Variablen/Secrets.
- `scripts/validate-workflows.py` prüft Workflows und lokale Actions.
- Hook `forbidden-patterns-check.py` verbietet `.mobileprovision`/`.p8`/Logdateien im Commit.
