# Plan

1. `test-ios-deployment.ps1`: alle msbuild-`-getProperty`-Aufrufe setzen alle vier `Include*Target` explizit.
2. Workflows (pr-staging-ci, staging-ci, release, security-scan): `IncludeAndroidTarget: 'false'` in allen Windows-Job-Umgebungen.
3. `local-ci.ps1`: Pruefskript ohne und mit Windows-Job-Umgebung als Kindprozess; restliche Schritte mit Windows-Job-Umgebung.
4. `package-ios`: Abweichung beim Pack-Cleanup per Kommentar begruendet.

## Offene Punkte
Keine.
