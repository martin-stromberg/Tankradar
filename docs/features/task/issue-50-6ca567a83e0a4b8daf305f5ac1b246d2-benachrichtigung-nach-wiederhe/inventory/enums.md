# Enums

## `BackupResult`
Datei: `src/Tankradar.MAUI/Models/Backup/BackupResult.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` | Die Operation wurde ausgeführt. Nur dieser Wert löst in `BackupViewModel.ConfirmRestoreAsync` die Benachrichtigung aus (eigenes `Restored`-Ereignis + `NotifyRestored()`, `BackupViewModel.cs:255–261`). |
| `NotFound` | Die Sicherung wurde nicht gefunden (oder der Dateiname ist nicht zulässig); führt zum Neuladen der Liste, aber zu keiner Benachrichtigung. |
| `InvalidFile` | Die Datei ist keine gültige Sicherung oder beschädigt. |
| `TooNew` | Die Sicherung stammt aus einer neueren Version der App. |
| `Busy` | Es läuft bereits eine andere Sicherungsoperation. |
| `Failed` | Die Operation ist aus einem anderen Grund fehlgeschlagen. |
