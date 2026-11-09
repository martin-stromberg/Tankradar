# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Die `requirement.md` enthält keinen explizit benannten Abschnitt „Akzeptanzkriterien"; die Kriterien wurden aus der fachlichen Zusammenfassung, dem fachlichen Scope und den Scope-Ausschlüssen abgeleitet.

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `BackupViewModel.Restored`-Ereignis entfällt vollständig (Deklaration `BackupViewModel.cs:53–56` inkl. XML-Kommentar und `Restored?.Invoke(this, EventArgs.Empty)` in Zeile 259); `NotifyRestored()` bleibt einziger Auslöser | Schritt 2 der Umsetzungsreihenfolge; Abschnitt „Änderungen an bestehenden Klassen → `BackupViewModel`"; verifiziert gegen `BackupViewModel.cs:53–56, 259–260` | Kein dedizierter Test nötig — Entfernung ist kompilierseitig erzwungen (alle 3 Abonnement-Stellen werden angepasst/entfernt); `Restore_Success_StillRaisesViewModelEvent` entfällt ersatzlos | Abgedeckt |
| `SettingsViewModel` abonniert `IRestoreNotifier.Restored` wie die übrigen Seiten | Schritt 1; Designentscheidung „Pflichtparameter nach `logger`, vor den optionalen Karten-Parametern"; verifiziert gegen `SettingsViewModel.cs:31–46` | `Restored_ReloadsSettings` (End-to-End über `ConfirmRestoreCommand` mit geteilter `RestoreNotifier`-Instanz) + neuer Test `Restored_ViaNotifier_ReloadsSettings` | Abgedeckt |
| Abonnement unbedingt — nicht mehr an `backup is not null` gekoppelt; `Backup = backup` bleibt | Designentscheidung „Unbedingtes `restoreNotifier.Restored += OnRestored`"; Schritt 1; Abschnitt „Geänderter Konstruktor" | Neuer Test `Restored_ViaNotifier_ReloadsSettings` prüft Neuladen ohne Karte „Sicherung" (`HasBackup` falsch) | Abgedeckt |
| `IRestoreNotifier` ist Pflichtparameter (Offene Frage 1 entlang der Empfehlung entschieden) | Designentscheidung mit Begründung (Konsistenz zu `TankbookViewModel`/`TankbookEntriesViewModel`, Fehler soll nicht still untergehen); Schritte 1 und 3 | Anpassung aller 17 `new SettingsViewModel(...)`-Aufrufstellen erzwingt Nachweis kompilierseitig | Abgedeckt |
| Handler bleibt Instanzmethode (schwache Referenz des `WeakEventManager`), ruft `_ = LoadAsync()`; Umbenennung `OnBackupRestored` → `OnRestored` | Designentscheidung „Handlername"; Schritt 1; Risiko „Schwache Referenz" benannt | Indirekt über `Restored_ReloadsSettings`/`Restored_ViaNotifier_ReloadsSettings` (Ladezähler/`WaitForAsync`, da kein `LastTask` vorhanden) | Abgedeckt |
| Nur `BackupResult.Ok` löst die Benachrichtigung aus; Auslösezeitpunkt und Fehlerpfade unverändert | Programmablauf Schritte 3 und 6; Scope-Klärung „Auslösende Ereignisse" | `Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored` (auf `Restore.Restored` umgestellt), `Restore_Failure_DoesNotNotify`, `Restore_Exception_ShowsFailure` | Abgedeckt |
| Sichtbares Verhalten unverändert: Optionen-, Tankbuch- und Vorgangsseiten laden sofort neu | Scope-Klärung „Erwartete Wirkung"; Akzeptanzszenario 1 | `Restored_ReloadsSettings`, `Restored_ReloadsVehicles`, `Restored_ReloadsEntriesAndStatistics`, `Restore_Success_NotifiesOnce`; E2E `TankbookEntriesE2ETests_RestoreReload` | Abgedeckt |
| Keine Änderung an `IRestoreNotifier`/`RestoreNotifier` (Interface, `WeakEventManager`, `Generation`) | „Neue Klassen: Keine"; kein Änderungsabschnitt für diese Klassen; `RestoreNotifierTests` bleiben unberührt | Bestehende `RestoreNotifierTests` laufen unverändert weiter | Abgedeckt |
| Keine neuen Abonnenten (z. B. `MapViewModel`, `FavoritesViewModel`) | Scope-Ausschluss #1, Status „bestätigt" mit Begründung | — (Ausschluss, nichts zu testen) | Abgedeckt |
| Einbettung der Karte „Sicherung" bleibt (`SettingsViewModel.Backup`, `OnAppearing`-Weiterleitung) | „`Backup = backup` bleibt bestehen"; `OnAppearing` unverändert | `WithoutBackup_HasBackupIsFalse`, `OnAppearing_LoadsBackupList` bleiben bestehen (nur um Pflichtparameter ergänzt) | Abgedeckt |
| `using Tankradar.MAUI.Services.Backup;` ergänzen (Produktivcode + 5 Testdateien) | Schritt 1 (ViewModel) und Schritt 3 (Testdateien-Liste); verifiziert: `ViewModelTests_PageTitles.cs` und `SettingsViewModelTests_Backup.cs` haben das using bereits, die übrigen 5 nicht | Kompilierung | Abgedeckt |
| Offene Frage 2 (Teststrategie) entschieden | Designentscheidung „Teststrategie `SettingsViewModel`-Neuladen": Kombination aus End-to-End-Pfad und direktem `NotifyRestored()`-Test; „Offene Punkte: Keine" | Beide geplanten Testvarianten konkret benannt | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Wiederherstellung über die Karte „Sicherung" → geöffnete Seiten laden neu (Happy Path, Szenario 1) | Kein neuer Test; bestehende Suite `TankbookEntriesE2ETests_RestoreReload` (3 Tests, Basis `BackupE2ETestBase`) fährt den realen Wiederherstellungspfad in der laufenden App | Nicht erforderlich mit Begründung — die Anforderung ändert keinen Benutzerfluss, sondern nur die interne Verdrahtung; sichtbares Verhalten ist per Definition unverändert. Die bestehende E2E-Suite deckt zusätzlich die DI-Auflösung des neuen `SettingsViewModel`-Pflichtparameters ab, da `BackupE2ETestBase` die Optionen/Karte „Sicherung" öffnet (Risiko „DI-Auflösung" im Plan benannt). Begründung ist nachvollziehbar im Abschnitt „E2E-Tests" dokumentiert. |
| Optionen-Seite lädt ohne Karte „Sicherung" neu (Szenario 2) | — | Nicht erforderlich — kein eigenständiger Benutzerfluss; im Produktivcode ist die Karte stets vorhanden (Risikoabschnitt). Unit-Test `Restored_ViaNotifier_ReloadsSettings` geplant. |
| Misslungene Wiederherstellung benachrichtigt niemanden (Szenario 3) | — | Nicht erforderlich — Fehlerpfad unverändert, bereits durch Unit-Tests abgedeckt; keine neue UI-Beobachtbarkeit. |

## Hinweise

- Stichproben-Verifikation gegen den Code bestätigt die Plan-Angaben: exakt 17 `new SettingsViewModel(...)`-Aufrufstellen in den 7 genannten Dateien; `ViewModel.Restored`-Abonnements nur an `BackupViewModelTests_Restore.cs:57, 84` und `BackupViewModelTests_RestoreNotification.cs:50` — alle drei abgedeckt; die übrigen `.Restored`-Fundstellen betreffen `BackupTexts.Restored` (Text) und `FakeBackupService.Restored` (Aufrufliste) und sind korrekt nicht im Plan.
- Bei den Aufrufstellen mit positionalen Optional-Argumenten (`SettingsViewModelTests_RouteSearch.cs:41, 66` mit `null, null, card`; `DataSourceViewModelTests_Check.cs:124` mit `CreateViewModel()`) verschiebt der neue dritte Pflichtparameter die Positionen — bei der Umsetzung darauf achten, dass der Notifier an Position 3 steht und die bisherigen Argumente entsprechend nachrücken.
- Die Testmethode `Restore_Confirm_RestoresSelectedFileAndRaisesRestored` behält nach der Umstellung auf `Restore.Restored` einen weiterhin zutreffenden Namen — Umbenennung optional, nicht erforderlich.
- Der vorbekannte, umsetzungsunabhängige Fehlschlag `ReleaseCommandTests.test_workflows_in_repository_are_clean` ist im Plan (Seiteneffekte/Risiken) korrekt benannt und darf die Verifikation nicht als Regression werten.
