# Release Notes

## Important Notes Before Update

- Distribution change (Issue #64): releases are no longer published on GitHub — the app is delivered exclusively via the App Store / TestFlight (App Store Connect). Existing GitHub releases have been removed; the Git tags remain intact. The Windows ZIP package is discontinued.
- CI change (Issue #93): environment-sensitive tests (FlaUI/UI automation, real process starts, loopback listeners) now run in a separate non-blocking `os-interface-tests` job (`continue-on-error`); E2E tests no longer block PR/staging merges. Deterministic tests (`Category!=OsInterface`) still block; `Flaky`-marked failures get exactly one automated re-run and stay visible via a TRX-based flake report.

## What's New

- Price service hardened (Issue #65): Tankerkönig responses are now size-limited (1 MiB) and read as a stream; oversized replies fall back to the last known prices with the existing "unexpected response" status, and interrupted downloads are retried like connection errors.
- Platform upgrade to .NET 11 / MAUI 11: the app now targets .NET 11 on all platforms and iOS builds are produced with the iOS 27.2 SDK (Xcode 27.2), satisfying Apple's App Store requirement for iOS 27 SDK builds starting April 2027. The previous .NET 10 codebase remains available on the `maintenance/net10` branch.
- Optional password protection for local backups: when creating a backup you can set a password; the file is then saved as an encrypted container (AES-256-GCM, key derivation PBKDF2-HMAC-SHA256, ADR 0019). Restore and import detect encrypted files automatically and ask for the password. Unencrypted backups remain the default and stay fully importable; the password is not stored anywhere and cannot be recovered.
- Security update November 2026: .NET 10 packages (Entity Framework Core, Data.Sqlite, Logging.Debug) updated to 10.0.15, Microsoft.Maui.Controls to 10.0.130, SQLitePCLRaw to 2.1.14; includes security fixes from the .NET 10.0.15 patch release. Maintenance/security update with no new features.

## Wichtige Hinweise vor dem Update

- Verteilungsänderung (Issue #64): Releases werden nicht mehr auf GitHub abgelegt — die App wird ausschließlich über den App Store / TestFlight (App Store Connect) verteilt. Die bestehenden GitHub-Releases wurden entfernt; die Git-Tags bleiben erhalten. Das Windows-ZIP-Paket entfällt.
- CI-Änderung (Issue #93): Umgebungssensitive Tests (FlaUI/UI-Automation, echte Prozessstarts, Loopback-Listener) laufen in einem eigenen, nicht blockierenden `os-interface-tests`-Job (`continue-on-error`); E2E-Tests blockieren PR-/Staging-Merges nicht mehr. Deterministische Tests (`Category!=OsInterface`) blockieren weiterhin; `Flaky`-markierte Fehlschläge erhalten genau einen automatisierten Re-Run und bleiben über eine TRX-basierte Flake-Statistik sichtbar.

## Neuerungen

- Preisdienst gehärtet (Issue #65): Tankerkönig-Antworten werden jetzt größenbegrenzt (1 MiB) und als Datenstrom gelesen; übergroße Antworten fallen mit dem vorhandenen Statustext „unerwartete Antwort“ auf die letzten bekannten Preise zurück, und abgebrochene Übertragungen werden wie Verbindungsfehler wiederholt.
- Plattform-Upgrade auf .NET 11 / MAUI 11: Die App zielt auf allen Plattformen auf .NET 11; iOS-Builds entstehen mit dem iOS-27.2-SDK (Xcode 27.2) und erfüllen damit Apples App-Store-Anforderung für iOS-27-SDK-Builds ab April 2027. Der bisherige .NET-10-Stand bleibt auf dem Branch `maintenance/net10` verfügbar.
- Optionaler Passwortschutz für lokale Sicherungen: Beim Erstellen einer Sicherung kann ein Passwort vergeben werden; die Datei wird dann als verschlüsselter Container gespeichert (AES-256-GCM, Schlüsselableitung PBKDF2-HMAC-SHA256, ADR 0019). Wiederherstellen und Einspielen erkennen verschlüsselte Dateien automatisch und fragen das Passwort ab. Unverschlüsselte Sicherungen bleiben Standard und weiterhin einspielbar; das Passwort wird nirgendwo gespeichert und kann nicht wiederhergestellt werden.
- Sicherheitsupdate November 2026: .NET-10-Pakete (Entity Framework Core, Data.Sqlite, Logging.Debug) auf 10.0.15, Microsoft.Maui.Controls auf 10.0.130, SQLitePCLRaw auf 2.1.14 aktualisiert; enthält Sicherheitsfixes aus dem .NET-10.0.15-Patch-Release. Wartungs-/Sicherheitsupdate ohne neue Funktionen.
