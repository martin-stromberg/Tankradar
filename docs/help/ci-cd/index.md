← [Zurück zur Dokumentation](../index.md)

# CI/CD-Pipeline mit Pre-Releases und Releases

Tankatlas nutzt GitHub Actions, um Build, Prüfungen und Veröffentlichung zu automatisieren: Jeder
Pull Request wird geprüft, jeder Push auf `staging` erzeugt automatisch einen RC-Tag
(`vX.Y.Z-rc.N`) und jeder Push auf `main` den Release-Tag `vX.Y.Z` — jeweils per `git push`.
Ausgeliefert wird ausschließlich über App Store Connect (TestFlight): Der signierte iOS-Build wird
verbindlich hochgeladen; es gibt keine GitHub-Releases und kein Windows-ZIP mehr (Issue #64,
ADR 0023).

## Inhalt

- [Übersicht, Branch-Modell und Ablauf](README.md)
- [Workflows und gemeinsame Bausteine im Detail](workflows.md)
- [Versionierung (0.1.0, keine automatische 1.0)](versionierung.md)
- [Lokaler Prüflauf (Ersatz für GitHub Actions)](lokaler-pruefung.md)
- [Einmalige Einrichtung auf GitHub und bei Apple (Checkliste, Secrets, Variablen)](einrichtung.md)
- [iOS-Deployment-Skript (Pair to Mac, TestFlight)](ios-deployment.md)
