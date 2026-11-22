# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- Platform upgrade to .NET 11 / MAUI 11: the app now targets .NET 11 on all platforms and iOS builds are produced with the iOS 27.2 SDK (Xcode 27.2), satisfying Apple's App Store requirement for iOS 27 SDK builds starting April 2027. The previous .NET 10 codebase remains available on the `maintenance/net10` branch.
- Optional password protection for local backups: when creating a backup you can set a password; the file is then saved as an encrypted container (AES-256-GCM, key derivation PBKDF2-HMAC-SHA256, ADR 0019). Restore and import detect encrypted files automatically and ask for the password. Unencrypted backups remain the default and stay fully importable; the password is not stored anywhere and cannot be recovered.
- Security update November 2026: .NET 10 packages (Entity Framework Core, Data.Sqlite, Logging.Debug) updated to 10.0.15, Microsoft.Maui.Controls to 10.0.130, SQLitePCLRaw to 2.1.14; includes security fixes from the .NET 10.0.15 patch release. Maintenance/security update with no new features.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Plattform-Upgrade auf .NET 11 / MAUI 11: Die App zielt auf allen Plattformen auf .NET 11; iOS-Builds entstehen mit dem iOS-27.2-SDK (Xcode 27.2) und erfüllen damit Apples App-Store-Anforderung für iOS-27-SDK-Builds ab April 2027. Der bisherige .NET-10-Stand bleibt auf dem Branch `maintenance/net10` verfügbar.
- Optionaler Passwortschutz für lokale Sicherungen: Beim Erstellen einer Sicherung kann ein Passwort vergeben werden; die Datei wird dann als verschlüsselter Container gespeichert (AES-256-GCM, Schlüsselableitung PBKDF2-HMAC-SHA256, ADR 0019). Wiederherstellen und Einspielen erkennen verschlüsselte Dateien automatisch und fragen das Passwort ab. Unverschlüsselte Sicherungen bleiben Standard und weiterhin einspielbar; das Passwort wird nirgendwo gespeichert und kann nicht wiederhergestellt werden.
- Sicherheitsupdate November 2026: .NET-10-Pakete (Entity Framework Core, Data.Sqlite, Logging.Debug) auf 10.0.15, Microsoft.Maui.Controls auf 10.0.130, SQLitePCLRaw auf 2.1.14 aktualisiert; enthält Sicherheitsfixes aus dem .NET-10.0.15-Patch-Release. Wartungs-/Sicherheitsupdate ohne neue Funktionen.
