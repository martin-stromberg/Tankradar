# Anforderung (Schritt 3, Ergänzung iOS-Deployment nach Stakeholder-Vorlage)

Quelle: Absatz „Ergänzung durch Stakeholder-Entscheidung vom 2026-10-04" in Schritt 3 von `project-plan.md`.

Stakeholder: „Schau dir mal drafts\iOS-Deployment.ps1 an. Dieses Skript für das Deployment stammt aus einem anderen Projekt, in welchem das Deployment für iOS gut funktioniert. Pass dein Skript entsprechend an und am besten übernimm den Namen. Auch findest du in dem drafts-Verzeichnis einen Unterordner github mit der CI-Pipeline dieses anderen Projekts. Darin ist das Deployment mit den GitHub Actions implementiert. So kannst du die Workflows auch für dieses Projekt einrichten."

## Anforderungen
1. `scripts/iOS-Deployment.ps1` aus der Vorlage übernehmen und anpassen (Projekt `src/Tankradar.MAUI/Tankradar.MAUI.csproj`, Env-Präfix `TANKRADAR_IOS_*`, Aktionen build/simulator/device/store/upload/list/menu inkl. Pair-to-Mac und TestFlight-Upload). Keine PolyForm-Lizenzzeile.
2. Baustein `package-ios` aus der Vorlage übernehmen (Keychain-Import, Workload-/SDK-Pinning, Buildnummer = Commit-Anzahl + run_attempt − 1, signierte .ipa, TestFlight-Upload per iTMSTransporter, Keychain-Aufräumen); `build-ios` ersetzen (kein doppelter iOS-Pfad).
3. Einbindung in PR-, Pre-Release- und Release-Workflow; Steuerung über Variable `IOS_SIGNING_ENABLED` und Secrets `IOS_*`; ohne Variable/Secrets kein Fehlschlag. Android entfällt.
4. `docs/help/ci-cd/einrichtung.md` auf Secrets/Variable und Apple-Schritte ausrichten; lokale Nutzung des Skripts dokumentieren.
5. `.gitignore`: `drafts/`, `.ios-deploy.user.json`, Deploy-Logs.
6. Bestehende Pipeline-Teile bleiben erhalten; `scripts/local-ci.ps1` prüft iOS unter Windows nur als Build und läuft grün (inkl. E2E).
7. Verifikation unter Windows (Parser, Get-Help, sauberer Abbruch, Workflow-Validierung); Doku nennt, was erst auf GitHub/macOS prüfbar ist.
