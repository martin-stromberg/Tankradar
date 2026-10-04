← [Zurück zur Dokumentation](../index.md)

# CI/CD-Pipeline mit Pre-Releases und Releases

Tankradar nutzt GitHub Actions, um Build, Prüfungen und Veröffentlichung zu automatisieren: Jeder
Pull Request wird geprüft, jeder Push auf `staging` erzeugt automatisch eine Pre-Release-Version und
jeder Push auf `main` ein finales Release. Die Windows-App wird als ZIP veröffentlicht, das nach dem
Entpacken ohne Installation startet.

## Inhalt

- [Übersicht, Branch-Modell und Ablauf](README.md)
- [Workflows und gemeinsame Bausteine im Detail](workflows.md)
- [Versionierung (0.1.0, keine automatische 1.0)](versionierung.md)
- [Lokaler Prüflauf (Ersatz für GitHub Actions)](lokaler-pruefung.md)
- [Einmalige Einrichtung auf GitHub und bei Apple (Checkliste, Secrets, Variablen)](einrichtung.md)
- [iOS-Deployment-Skript (Pair to Mac, TestFlight)](ios-deployment.md)
