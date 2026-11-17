# Enums: Sicherung

## `BackupResult`

Datei: `src/Tankradar.MAUI/Models/Backup/BackupResult.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` | Die Operation wurde ausgeführt. |
| `NotFound` | Die Sicherung wurde nicht gefunden (oder der Dateiname ist nicht zulässig). |
| `InvalidFile` | Die Datei ist keine gültige Sicherung oder beschädigt. |
| `TooNew` | Die Sicherung stammt aus einer neueren Version der App. |
| `Busy` | Es läuft bereits eine andere Sicherungsoperation. |
| `Failed` | Die Operation ist aus einem anderen Grund fehlgeschlagen. |

Abbildung auf UI-Texte: `BackupTexts.GetMessage` (`src/Tankradar.MAUI/Resources/Texts/BackupTexts.cs:147`). **Es existieren keine Werte für „Passwort erforderlich“ oder „falsches Passwort“/Entschlüsselungsfehler** — entsprechende Ergebnisse würden derzeit als `InvalidFile` behandelt.

## `BackupTamper` (Testhilfe, kein Produktivcode)

Datei: `src/TestSupport/BackupTestTampering.cs`

| Wert | Bedeutung |
|------|-----------|
| `Trigger` | Zusätzlicher Trigger, der beim Anlegen eines Fahrzeugs alle Vorgänge löscht. |
| `PrefixedTrigger` | Wie `Trigger`, aber mit reserviertem Präfix `sqlite_`. |
| `View` | Zusätzliche View. |
| `ExtraCheck` | Zusätzliche `CHECK`-Einschränkung an `Vehicles`. |
| `ManipulatedSequence` | Autoinkrement-Zähler von `Vehicles` auf Höchstwert. |
| `MigrationLockRow` | Zeile in `__EFMigrationsLock`. |
| `RemovedCollation` | `NOCASE`-Kollation aus `Vehicles` entfernt. |
| `ExtraColumn` | Zusätzliche Spalte an einer App-Tabelle. |
| `ExtraTable` | Zusätzliche fremde Tabelle. |
| `ExtraIndex` | Zusätzlicher Index auf einer App-Tabelle. |
| `MissingTable` | App-Tabelle fehlt. |
| `MissingColumn` | Spalte einer App-Tabelle fehlt. |
| `InvalidDate` | Ungültiger Datumswert in `Vehicles.CreatedUtc`. |
| `UnknownFuelKind` | Unbekannte Kraftstoffart eines Fahrzeugs. |
| `DuplicateName` | Fahrzeugname doppelt (eindeutiger Index entfernt). |
| `ViewAsTable` | App-Tabelle durch View gleichen Namens ersetzt. |
| `OrphanEntry` | Vorgang verweist auf fehlendes Fahrzeug. |
| `OversizedCustomName` | `CustomName` mit 61 Zeichen. |
| `HugeCustomName`, `HugeStationName`, `HugeBrand`, `HugeGroupName`, `HugeEntryNote`, `HugeEntryStationName` | Textfelder mit 200.000 Zeichen. |
| `HugeBlobCustomName` | `CustomName` als BLOB mit 200.000 Bytes. |

Zugehörige Helfer: `BackupTestTampering.GetSql`, `IsRejected` (welche Varianten abgelehnt werden müssen), `CreateTamperedCopy` (neue gültige Sicherungsdatei mit manipulierter Datenbank, gültigem Manifest und Prüfsummen).
