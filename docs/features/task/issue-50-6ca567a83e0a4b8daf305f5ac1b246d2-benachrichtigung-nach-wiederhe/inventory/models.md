# Datenmodelle

## `BackupFileInfo`
Datei: `src/Tankradar.MAUI/Models/Backup/BackupFileInfo.cs`

`sealed record`; ein Eintrag der Liste der Sicherungen. Relevant für den Wiederherstellungspfad: `BackupViewModel.ConfirmRestoreAsync` ruft `_service.RestoreAsync(item.FileName)` auf; `IsReadable` entscheidet in `ShowRestoreQuestionAsync`, ob die Rückfrage erscheint (`BackupViewModel.cs:431`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FileName` | `string` | Der Dateiname im Sicherungsverzeichnis. |
| `CreatedUtc` | `DateTime` | Erstellungszeitpunkt (UTC) laut Manifest; bei nicht lesbaren Dateien das Änderungsdatum. |
| `SizeBytes` | `long` | Die Dateigröße in Bytes. |
| `AppVersion` | `string` | Die App-Version laut Manifest; leer bei nicht lesbaren Dateien. |
| `IsReadable` | `bool` | Gibt an, ob das Manifest gelesen werden konnte. |
