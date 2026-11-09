# Übersetzte Anforderung: Benachrichtigung nach Wiederherstellung vereinheitlichen

## Fachliche Zusammenfassung

Nach einer erfolgreichen Wiederherstellung aus einer Sicherung werden geöffnete Seiten heute über zwei parallele Mechanismen benachrichtigt: `BackupViewModel.ConfirmRestoreAsync` löst sowohl das seiteneigene Ereignis `BackupViewModel.Restored` aus (einziger Abonnent: `SettingsViewModel`, `SettingsViewModel.cs:45`) als auch `IRestoreNotifier.NotifyRestored()` (Abonnenten: `TankbookViewModel`, `TankbookEntriesViewModel`). Die Benachrichtigung wird auf den alleinigen Weg über `IRestoreNotifier` vereinheitlicht: Das Ereignis `BackupViewModel.Restored` entfällt, und `SettingsViewModel` abonniert `IRestoreNotifier.Restored` wie die übrigen Seiten. Das sichtbare Verhalten (Optionen-, Tankbuch- und Vorgangsseiten laden sofort neu) bleibt unverändert; es ist eine reine Vereinheitlichung der internen Verdrahtung.

## Fachlicher Scope

- **Verarbeitungseinheit:** Die Benachrichtigung „Daten wurden aus einer Sicherung wiederhergestellt" — genau ein Auslöser (`IRestoreNotifier.NotifyRestored()` in `BackupViewModel.ConfirmRestoreAsync`) und genau ein Abonnementmechanismus (`IRestoreNotifier.Restored`) für alle Seiten.
- **Auslösende Ereignisse:** Bestätigung der Wiederherstellung durch den Anwender in der Karte „Sicherung" der Optionen (`ConfirmRestoreAsync`), nur bei Ergebnis `BackupResult.Ok`. Misslungene Wiederherstellungen lösen weiterhin keine Benachrichtigung aus.
- **Erwartete Wirkung:** Alle geöffneten Seiten mit wiederherstellungsrelevanten Daten — Optionen (`SettingsViewModel`), Fahrzeugverwaltung (`TankbookViewModel`) und Vorgangsseite (`TankbookEntriesViewModel`) — laden ihre Daten nach der Wiederherstellung unmittelbar neu. Für den Anwender ändert sich nichts; die Wirkung ist, dass künftige Seiten nur noch einen Benachrichtigungsmechanismus kennen müssen.
- **Umgang mit fehlenden Objekten:** `IRestoreNotifier` ist als Singleton registriert (`MauiProgram.cs:85`) und steht zur Laufzeit immer bereit — es ist nichts zu erzeugen. In Unit-Tests, die `SettingsViewModel` ohne Benachrichtigungsdienst aufbauen, kann die Abhängigkeit entfallen (optionaler Konstruktorparameter) oder ein `RestoreNotifier` direkt instanziiert werden — Entscheidung siehe „Offene Fragen".
- **Scope-Ausschlüsse:**
  - Keine Änderung an `IRestoreNotifier`/`RestoreNotifier` (Schnittstelle, `WeakEventManager`-Umsetzung, `Generation`-Zähler bleiben unverändert).
  - Keine Änderung am Auslösezeitpunkt oder an den Fehlerpfaden von `ConfirmRestoreAsync` (Statusmeldungen, `LoadAsync` der Sicherungsliste, `IsBusy`-Verwaltung bleiben).
  - Keine neuen Abonnenten: Seiten, die heute nicht auf die Wiederherstellung reagieren (z. B. `MapViewModel`, `FavoritesViewModel`), werden nicht ergänzt.
  - Die Einbettung der Karte „Sicherung" in die Optionen (`SettingsViewModel.Backup` inkl. `OnAppearing`-Weiterleitung) bleibt bestehen; entfernt wird nur das Ereignis-Abonnement.

## Betroffene Klassen und Komponenten

- `src/Tankradar.MAUI/ViewModels/BackupViewModel.cs`
  - Entfernen des öffentlichen Ereignisses `Restored` (Zeilen 53–56) und des Aufrufs `Restored?.Invoke(this, EventArgs.Empty)` (Zeile 259); `NotifyRestored()` (Zeile 260) bleibt der einzige Auslöser.
- `src/Tankradar.MAUI/ViewModels/SettingsViewModel.cs`
  - Neue Abhängigkeit `IRestoreNotifier` (Konstruktorparameter); Abonnement `restoreNotifier.Restored += OnRestored` statt `backup.Restored += OnBackupRestored` (Zeile 45); Umbenennung/Anpassung des Handlers `OnBackupRestored` (Zeilen 276–279) — der Handler muss eine Instanzmethode bleiben, da `RestoreNotifier` Abonnenten schwach hält (`WeakEventManager`, vgl. Hinweis in `RestoreNotifier.cs:4–6`).
  - `using Tankradar.MAUI.Services.Backup;` ergänzen.
- `src/Tankradar.MAUI/MauiProgram.cs`
  - Keine Änderung erwartet: `IRestoreNotifier` ist bereits registriert (`MauiProgram.cs:85`) und `SettingsViewModel` wird per `AddTransient<SettingsViewModel>()` (Zeile 126) aufgelöst — eine injizierte Abhängigkeit wird automatisch versorgt, sofern sie als Konstruktorparameter gefordert wird.
- Unverändert: `IRestoreNotifier`, `RestoreNotifier`, `TankbookViewModel`, `TankbookEntriesViewModel` und deren `OnRestored`-Handler.
- Tests:
  - `src/Tankradar.Tests.Unit/Unit/BackupViewModelTests_Restore.cs` — Abonnements `ViewModel.Restored` (Zeilen 57, 84) auf den Notifier umstellen (z. B. `Restore.Restored` abonnieren oder `Restore.Generation` prüfen).
  - `src/Tankradar.Tests.Unit/Unit/BackupViewModelTests_RestoreNotification.cs` — der Test `Restore_Success_StillRaisesViewModelEvent` (Zeilen 44–56) entfällt ersatzlos bzw. wird entfernt; die übrigen Prüfungen über `Restore.Generation` bleiben.
  - `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Backup.cs` — `Restored_ReloadsSettings` (Zeilen 54–73) so umbauen, dass `SettingsViewModel` denselben `RestoreNotifier` erhält wie das `BackupViewModel` (Hilfsmethode `CreateBackup` erzeugt den Notifier derzeit intern, Zeile 18), oder die Benachrichtigung direkt per `NotifyRestored()` auslösen.
  - `src/Tankradar.Tests.Unit/Unit/BackupViewModelTestBase.cs` — die Eigenschaft `Restore` (Zeile 48) steht den `BackupViewModel`-Tests bereits zur Verfügung; ggf. bei `SettingsViewModel`-Tests eine gemeinsame Instanz teilen.
  - Übrige `SettingsViewModel`-Tests (17 Aufrufstellen `new SettingsViewModel(...)`) sind nur betroffen, wenn der Notifier zum Pflichtparameter wird.

## Implementierungsansatz

- `SettingsViewModel` erhält `IRestoreNotifier` als Konstruktorparameter und abonniert `Restored` im Konstruktor, analog zum Muster in `TankbookViewModel.cs:51` und `TankbookEntriesViewModel.cs:74`. Der Handler ruft weiterhin `LoadAsync()` auf (`_ = LoadAsync()` bzw. Zuweisung an ein verfolgbares Task-Feld, wie es die übrigen Handler mit `LastTask = LoadAsync()` tun — Annahme: `SettingsViewModel` besitzt kein `LastTask`, das Feuer-und-Vergiss-Muster `_ = LoadAsync()` bleibt daher naheliegend).
- Erweiterungspunkt ist das bestehende `IRestoreNotifier.Restored`-Ereignis; es wird weder eine neue Klasse noch ein neues Interface benötigt.
- Abhängigkeit: `SettingsViewModel` → `IRestoreNotifier` (Singleton, `MauiProgram.cs:85`). Die Registrierung `AddTransient<SettingsViewModel>()` löst den Parameter automatisch auf, sofern er nicht optional ist; bei optionalem Parameter (`IRestoreNotifier? restoreNotifier = null`, passend zu den übrigen optionalen Karten-Parametern) ist ebenfalls keine Änderung an `MauiProgram.cs` nötig, da der Container registrierte Typen auch für optionale Parameter einsetzt.
- Zum Muster gehört, dass Ereignishandler Instanzmethoden sind — keine Lambdas — damit die schwache Referenz des `WeakEventManager` den Abonnenten nicht vorzeitig freigibt (`RestoreNotifier.cs:4–6`).
- Wichtig: `SettingsViewModel` darf das Abonnement nicht mehr an das Vorhandensein der Sicherungskarte (`backup is not null`) koppeln, sondern an den Notifier selbst — die Benachrichtigung ist von der eingebetteten Karte unabhängig.

## Konfiguration

Keine. Das Verhalten ist nicht konfigurierbar und benötigt keine Anwendungseinstellung.

## Offene Fragen

1. Soll `IRestoreNotifier` im `SettingsViewModel`-Konstruktor ein Pflichtparameter oder — konsistent zu `dataSource`, `backup`, `routeSearch` — ein optionaler Parameter (`= null`) sein? Pflicht wäre die klarere Forderung (die Benachrichtigung hängt fachlich nicht an der Sicherungskarte), erfordert aber die Anpassung aller 17 Test-Aufrufstellen; optional bleibt abwärtskompatibel, lässt aber einen stillen Pfad ohne Neuladen zu, wenn kein Notifier übergeben wird. Empfehlung: Pflichtparameter, da der Notifier im Container immer verfügbar ist und ein fehlender Notifier ein Fehler wäre.
2. Soll der bestehende `SettingsViewModel`-Test auf das Neuladen ausschließlich über direkten Aufruf von `NotifyRestored()` prüfen (entkoppelt vom `BackupViewModel`) oder weiterhin den End-to-End-Pfad über `ConfirmRestoreCommand` fahren? Beides ist aus der Anforderung ableitbar („ggf. einen `SettingsViewModel`-Test auf das Neuladen nach `NotifyRestored` ergänzen"); eine Kombination — direkter Notifier-Test plus bestehender Pfad — sichert beide Ebenen ab.
