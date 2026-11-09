# Umsetzungsplan: Benachrichtigung nach Wiederherstellung vereinheitlichen

## Übersicht

Die Benachrichtigung geöffneter Seiten nach einer Backup-Wiederherstellung wird auf den alleinigen Weg über `IRestoreNotifier` vereinheitlicht: `BackupViewModel.ConfirmRestoreAsync` ruft nur noch `NotifyRestored()` auf, das eigene Ereignis `BackupViewModel.Restored` entfällt. `SettingsViewModel` erhält `IRestoreNotifier` als Konstruktorparameter und abonniert `Restored` wie `TankbookViewModel` und `TankbookEntriesViewModel`. Betroffen sind die zwei ViewModels sowie die Tests, die das entfallende Ereignis abonnieren oder `SettingsViewModel` direkt instanziieren.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Die Benachrichtigung „Daten wurden aus einer Sicherung wiederhergestellt" — genau ein Auslöser (`IRestoreNotifier.NotifyRestored()` in `BackupViewModel.ConfirmRestoreAsync`) und genau ein Abonnementmechanismus (`IRestoreNotifier.Restored`) für alle Seiten |
| Auslösende Ereignisse | Bestätigung der Wiederherstellung in der Karte „Sicherung" der Optionen (`ConfirmRestoreAsync`), nur bei Ergebnis `BackupResult.Ok`; misslungene Wiederherstellungen lösen keine Benachrichtigung aus |
| Erwartete Wirkung | Alle geöffneten Seiten mit wiederherstellungsrelevanten Daten — Optionen (`SettingsViewModel`), Fahrzeugverwaltung (`TankbookViewModel`), Vorgangsseite (`TankbookEntriesViewModel`) — laden ihre Daten unmittelbar neu; sichtbares Verhalten unverändert |
| Umgang mit fehlenden Objekten | `IRestoreNotifier` ist als Singleton registriert (`MauiProgram.cs:85`) und steht zur Laufzeit immer bereit — nichts zu erzeugen. In Tests wird ein `RestoreNotifier` direkt instanziiert und dem `SettingsViewModel` (Pflichtparameter) übergeben; beim Neulade-Test wird eine gemeinsame Instanz zwischen `BackupViewModel` und `SettingsViewModel` geteilt |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Seiten, die heute nicht auf die Wiederherstellung reagieren (z. B. `MapViewModel`, `FavoritesViewModel`), werden nicht als Abonnenten ergänzt — sie laden nach einer Wiederherstellung weiterhin nicht automatisch neu | Anforderung beschränkt sich auf die Vereinheitlichung des Mechanismus, nicht auf neue Reaktionen | bestätigt (2026-11-09 — durch die Anforderung selbst: Akzeptanzkriterien nennen explizit nur Optionen-, Tankbuch- und Vorgangsseiten und fordern unverändertes sichtbares Verhalten) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `SettingsViewModel`-Konstruktor: `IRestoreNotifier` | Pflichtparameter, positioniert nach `logger` und vor den optionalen Karten-Parametern (`dataSource`, `backup`, `routeSearch`) | Konsistent zu `TankbookViewModel`/`TankbookEntriesViewModel`, wo `restoreNotifier` Pflicht ist; die Benachrichtigung hängt fachlich nicht an einer optionalen Karte, ein fehlender Notifier wäre ein Fehler und soll nicht still untergehen. Der Container versorgt den Parameter automatisch (Singleton bereits registriert). Preis: Anpassung der 17 Test-Aufrufstellen — bewusst in Kauf genommen. |
| Abonnement im `SettingsViewModel`-Konstruktor | Unbedingtes `restoreNotifier.Restored += OnRestored` — nicht mehr an `backup is not null` gekoppelt | Die Benachrichtigung ist vom Vorhandensein der eingebetteten Karte „Sicherung" unabhängig; so lädt die Optionen-Seite auch in Konstellationen ohne Karte neu |
| Handlername | `OnBackupRestored` wird zu `OnRestored` umbenannt, Instanzmethode mit `_ = LoadAsync()` | Einheitliches Benennungsmuster mit den beiden bestehenden Abonnenten; Instanzmethode ist Pflicht, weil `RestoreNotifier` Abonnenten schwach hält (`WeakEventManager`, `RestoreNotifier.cs:4–5`). `SettingsViewModel` besitzt kein `LastTask`, daher bleibt das Feuer-und-Vergiss-Muster `_ = LoadAsync()` |
| Teststrategie `SettingsViewModel`-Neuladen | Kombination: bestehender End-to-End-Pfad (`ConfirmRestoreCommand` über geteilte `RestoreNotifier`-Instanz) plus neuer direkter `NotifyRestored()`-Test ohne Karte „Sicherung" | Sichert beide Ebenen ab: die reale Auslösekette über `ConfirmRestoreAsync` und die entkoppelte Reaktion des `SettingsViewModel` auf den Notifier allein |

## Programmabläufe

### Wiederherstellung bestätigen und Seiten benachrichtigen

1. Anwender drückt „Wiederherstellen" in einem Eintrag der Karte „Sicherung" → `BackupItemViewModel.RestoreCommand` → `BackupViewModel.BeginRestoreAsync` → Rückfrage wird angezeigt (unverändert).
2. Anwender bestätigt → `ConfirmRestoreCommand` → `BackupViewModel.ConfirmRestoreAsync` → `_service.RestoreAsync(item.FileName)`.
3. Bei `BackupResult.Ok`: `SetStatus(BackupTexts.Restored)`, `LoadAsync()` der Sicherungsliste, dann **nur noch** `_restoreNotifier.NotifyRestored()` (der Aufruf `Restored?.Invoke(...)` entfällt).
4. `RestoreNotifier.NotifyRestored` erhöht `Generation` und meldet das Ereignis `Restored` an alle schwach gehaltenen Abonnenten.
5. `SettingsViewModel.OnRestored`, `TankbookViewModel.OnRestored`, `TankbookEntriesViewModel.OnRestored` laden ihre Daten neu.
6. Bei jedem anderen `BackupResult` und bei Ausnahmen: wie bisher nur Statusmeldung (bzw. `LoadAsync` bei `NotFound`), keine Benachrichtigung.

Beteiligte Klassen/Komponenten: `BackupViewModel`, `BackupItemViewModel`, `IBackupService`, `IRestoreNotifier`/`RestoreNotifier`, `SettingsViewModel`, `TankbookViewModel`, `TankbookEntriesViewModel`

### Optionen-Seite reagiert auf Wiederherstellung (neue Verdrahtung)

1. Der Container erzeugt `SettingsViewModel` (transient) und injiziert `IRestoreNotifier` (Singleton).
2. Der Konstruktor abonniert `restoreNotifier.Restored += OnRestored` — unabhängig davon, ob die Karte „Sicherung" (`backup`) übergeben wurde.
3. Bei `NotifyRestored()` ruft `OnRestored` `_ = LoadAsync()` auf; `LoadAsync` lädt die Einstellungen semaphor-geschützt (`_gate`) neu und setzt `IsLoaded`.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `IRestoreNotifier`/`RestoreNotifier`, `ISettingsService`

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `BackupViewModel` (ViewModel)

- **Entfernte Events:** `Restored` (`event EventHandler?`, `BackupViewModel.cs:53–56`) — entfällt vollständig; einziger Auslöser bleibt `IRestoreNotifier.NotifyRestored()`
- **Geänderte Methoden:** `ConfirmRestoreAsync` — Zeile 259 `Restored?.Invoke(this, EventArgs.Empty)` entfällt; Zeile 260 `_restoreNotifier.NotifyRestored()` bleibt der einzige Benachrichtigungsweg; Auslösezeitpunkt (nach `SetStatus` und `LoadAsync`, nur bei `BackupResult.Ok`) unverändert

### `SettingsViewModel` (ViewModel)

- **Neue Abhängigkeit:** Konstruktorparameter `IRestoreNotifier restoreNotifier` (Pflicht, dritter Parameter nach `settingsService` und `logger`, vor den optionalen Karten-Parametern); `using Tankradar.MAUI.Services.Backup;` ergänzen; Dokumentationskommentar des Parameters anpassen
- **Neue Event-Handler:** `OnRestored` (umbenannt aus `OnBackupRestored`, `SettingsViewModel.cs:276–279`) — reagiert auf `IRestoreNotifier.Restored`, ruft weiterhin `_ = LoadAsync()`; bleibt private Instanzmethode (`WeakEventManager`)
- **Geänderter Konstruktor:** `restoreNotifier.Restored += OnRestored` statt `backup.Restored += OnBackupRestored` (`SettingsViewModel.cs:43–46`); das `if (backup is not null)` um das Abonnement entfällt; `Backup = backup` bleibt bestehen

### `MauiProgram` (DI-Registrierung)

- Keine Änderung: `AddSingleton<IRestoreNotifier, RestoreNotifier>()` (`MauiProgram.cs:85`) und `AddTransient<SettingsViewModel>()` (`MauiProgram.cs:126`) sind vorhanden; der neue Pflichtparameter wird vom Container automatisch aufgelöst

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Test-Kompilierung:** Alle 17 `new SettingsViewModel(...)`-Aufrufstellen in 7 Testdateien brechen kompilierseitig, sobald `IRestoreNotifier` Pflichtparameter wird — vollständige Anpassung ist Teil der Umsetzung (Liste in `inventory/tests.md`, Abschnitt „Übrige `new SettingsViewModel(...)`-Aufrufstellen").
- **Verhalten ohne Karte „Sicherung":** `SettingsViewModel` lädt nach `NotifyRestored()` künftig auch dann neu, wenn keine `backup`-Karte übergeben wurde — gewollte Entkopplung; im Produktivcode ist die Karte stets vorhanden, daher kein sichtbarer Unterschied.
- **Schwache Referenz:** Der Handler muss eine Instanzmethode bleiben; kein Lambda — sonst gibt `WeakEventManager` den Abonnenten vorzeitig frei (`RestoreNotifier.cs:4–5`).
- **DI-Auflösung:** `SettingsPage` erhält `SettingsViewModel` per Konstruktorinjektion; der neue Pflichtparameter wird automatisch versorgt. Scheiterte die Auflösung, würde die App beim Öffnen der Optionen fehlschlagen — durch bestehende E2E-Tests, die die Optionen/Karte „Sicherung" öffnen (`BackupE2ETestBase`), abgesichert.
- **Vorbekannter Testfehlschlag (unabhängig):** `ReleaseCommandTests.test_workflows_in_repository_are_clean` schlägt bereits auf unverändertem Stand fehl (Workflow-Validator, `e2e-dumps/`-Artefakt-Upload) — nicht Teil dieser Umsetzung.

## Umsetzungsreihenfolge

1. **`SettingsViewModel` auf `IRestoreNotifier` umstellen**
   - Voraussetzungen: `IRestoreNotifier`/`RestoreNotifier` vorhanden (`src/Tankradar.MAUI/Services/Backup/`), Singleton-Registrierung vorhanden (`MauiProgram.cs:85`)
   - Beschreibung: `using Tankradar.MAUI.Services.Backup;` ergänzen; Pflichtparameter `IRestoreNotifier restoreNotifier` als dritten Konstruktorparameter einführen; unbedingtes Abonnement `restoreNotifier.Restored += OnRestored`; Handler `OnBackupRestored` in `OnRestored` umbenennen; Parameterdokumentation anpassen (Hinweis aus dem `backup`-Parameter „Nach einer Wiederherstellung werden die Einstellungen neu geladen" auf den neuen Parameter bzw. die Klassenbeschreibung übertragen)
2. **`BackupViewModel` bereinigen**
   - Voraussetzungen: Schritt 1 (damit kein Abonnent des entfallenden Ereignisses zurückbleibt)
   - Beschreibung: Ereignisdeklaration `Restored` inkl. XML-Kommentar (Zeilen 53–56) und `Restored?.Invoke(this, EventArgs.Empty)` (Zeile 259) entfernen; `NotifyRestored()` bleibt
3. **Testaufrufstellen `new SettingsViewModel(...)` anpassen**
   - Voraussetzungen: Schritt 1
   - Beschreibung: Alle 17 Aufrufstellen in 7 Dateien erhalten als drittes Argument einen `IRestoreNotifier` (je `new RestoreNotifier()`, bei `SettingsViewModelTests_Backup` die geteilte Instanz); fehlendes `using Tankradar.MAUI.Services.Backup;` in `SettingsViewModelTests_Loading.cs`, `SettingsViewModelTests_Changes.cs`, `SettingsViewModelTests_Nearby.cs`, `SettingsViewModelTests_RouteSearch.cs` und `DataSourceViewModelTests_Check.cs` ergänzen (`ViewModelTests_PageTitles.cs` und `SettingsViewModelTests_Backup.cs` haben es bereits)
4. **Restore-bezogene `BackupViewModel`-Tests auf den Notifier umstellen**
   - Voraussetzungen: Schritt 2
   - Beschreibung: `BackupViewModelTests_Restore` Zeilen 57 und 84 von `ViewModel.Restored` auf `Restore.Restored` umstellen; `BackupViewModelTests_RestoreNotification.Restore_Success_StillRaisesViewModelEvent` ersatzlos entfernen
5. **`SettingsViewModelTests_Backup` umbauen und direkten Notifier-Test ergänzen**
   - Voraussetzungen: Schritte 1, 3
   - Beschreibung: Geteilte `RestoreNotifier`-Instanz als Feld; `CreateBackup()` nutzt sie; `Restored_ReloadsSettings` bleibt auf dem End-to-End-Pfad (`ConfirmRestoreCommand`); neuen Test für direktes `NotifyRestored()` ohne Karte ergänzen
6. **Build und Tests verifizieren**
   - Voraussetzungen: Schritte 1–5
   - Beschreibung: `dotnet build Tankradar.sln -c Release` (Umgebung wie Bestandsaufnahme: `IncludeWindowsTarget=true`, übrige Targets `false`), danach `dotnet test src/Tankradar.Tests.Unit` sowie die E2E-Suite `TankbookEntriesE2ETests_RestoreReload` als Nachweis der DI-Verdrahtung

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Restored_ViaNotifier_ReloadsSettings` | `SettingsViewModelTests_Backup` | Direkter Aufruf `notifier.NotifyRestored()` auf der geteilten Instanz — `SettingsViewModel` ohne eingebettete Karte lädt die Einstellungen neu (`_settings.LoadCount` erhöht, geänderte Werte sichtbar); belegt die Entkopplung von der Karte „Sicherung" |
| Geteilte `RestoreNotifier`-Instanz (Feld) | `SettingsViewModelTests_Backup` | Eine Instanz für `CreateBackup()` und alle `new SettingsViewModel(...)`-Aufrufe der Datei, damit der End-to-End-Pfad über `ConfirmRestoreCommand` weiterläuft |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `BackupViewModelTests_Restore.Restore_Confirm_RestoresSelectedFileAndRaisesRestored` | Abonnement `ViewModel.Restored` (Zeile 57) entfällt; stattdessen `Restore.Restored` abonnieren (`Restore` = `RestoreNotifier` aus `BackupViewModelTestBase`) |
| `BackupViewModelTests_Restore.Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored` | Abonnement `ViewModel.Restored` (Zeile 84) auf `Restore.Restored` umstellen |
| `BackupViewModelTests_RestoreNotification.Restore_Success_StillRaisesViewModelEvent` | Entfällt ersatzlos — prüft das entfernte ViewModel-Ereignis |
| `SettingsViewModelTests_Backup.Restored_ReloadsSettings` | `CreateBackup()` erzeugt den Notifier heute intern (Zeile 18); auf geteilte Instanz umstellen, damit `ConfirmRestoreAsync` über denselben `RestoreNotifier` benachrichtigt, den das `SettingsViewModel` abonniert |
| `SettingsViewModelTests_Backup.WithoutBackup_HasBackupIsFalse`, `.OnAppearing_LoadsBackupList` | Neuer Pflichtparameter — Aufrufstellen Zeilen 27, 40 (und 58) erhalten die geteilte Instanz |
| Übrige 14 `new SettingsViewModel(...)`-Aufrufstellen in `SettingsViewModelTests_Loading` (5), `SettingsViewModelTests_RouteSearch` (3), `SettingsViewModelTests_Changes` (2), `DataSourceViewModelTests_Check` (2), `SettingsViewModelTests_Nearby` (1), `ViewModelTests_PageTitles` (1) | Neuer Pflichtparameter — je `new RestoreNotifier()` als drittes Argument; ggf. `using Tankradar.MAUI.Services.Backup;` ergänzen |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Die Anforderung ändert keinen Benutzerfluss — das sichtbare Verhalten ist per Definition unverändert, es wird nur die interne Verdrahtung vereinheitlicht. Es gibt keine neue oder geänderte Benutzerinteraktion, deren Happy Path einen E2E-Nachweis bräuchte. Zusätzlich fahren die bestehenden E2E-Tests `TankbookEntriesE2ETests_RestoreReload` den realen Wiederherstellungspfad in der laufenden App (inkl. DI-Auflösung von `SettingsViewModel` beim Öffnen der Optionen/Karte „Sicherung" über `BackupE2ETestBase`), sodass die neue Konstruktorabhängigkeit in der echten Anwendung mit abgedeckt ist.

Anzupassende bestehende E2E-Tests: Keine.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Wiederherstellung benachrichtigt alle geöffneten Seiten | Anwender bestätigt die Wiederherstellung in der Karte „Sicherung" | App läuft; eine lesbare Sicherung liegt vor; seit der Sicherung geänderte Daten (Einstellungen, Fahrzeuge, Vorgänge); Optionen-, Fahrzeug- und Vorgangsseite geöffnet bzw. instanziiert | `RestoreCommand` → Rückfrage → `ConfirmRestoreCommand` → `RestoreAsync` liefert `Ok` → `NotifyRestored()` → `OnRestored` in den drei ViewModels | Optionen zeigen wiederhergestellte Einstellungen, Fahrzeugliste und Vorgänge zeigen den gesicherten Stand; Meldung „wiederhergestellt" auf der Karte | Unit: `Restored_ReloadsSettings`, `Restore_Success_NotifiesOnce`, `Restored_ReloadsVehicles`, `Restored_ReloadsEntriesAndStatistics`; E2E: `TankbookEntriesE2ETests_RestoreReload` |
| 2 | Optionen laden neu, ohne dass eine Karte „Sicherung" eingebettet ist | `NotifyRestored()` des Singleton-Notifiers | `SettingsViewModel` ohne `backup`-Parameter instanziiert (`HasBackup` falsch) | `NotifyRestored()` → `OnRestored` → `LoadAsync()` | Einstellungen werden neu geladen und angezeigt | Neuer Unit-Test `Restored_ViaNotifier_ReloadsSettings` |
| 3 | Misslungene Wiederherstellung benachrichtigt niemanden | Bestätigung, aber `RestoreAsync` liefert `NotFound`/`InvalidFile`/`TooNew`/`Busy`/`Failed` oder wirft | wie Szenario 1 | `ConfirmRestoreCommand` → Ergebnis ≠ `Ok` → keine `NotifyRestored()`-Ausführung | Nur Status-/Fehlermeldung auf der Karte; keine Seite lädt neu; `Generation` bleibt | Unit: `Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored`, `Restore_Failure_DoesNotNotify`, `Restore_Exception_ShowsFailure` |

## Offene Punkte

Keine. Die beiden in der Anforderung gestellten Fragen (Pflicht- vs. optionaler Parameter; Teststrategie des `SettingsViewModel`-Neuladens) sind in den Designentscheidungen entschieden — jeweils entlang der in der Anforderung gegebenen Empfehlung.
