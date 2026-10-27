# Tasks: Tankbuch öffnet mit Tank- und Ladevorgängen (Issue #20)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Texte | Neue Schlüssel in `TankbookTexts` anlegen (`VehiclesPageTitle`, `ManageVehicles`, `AddVehicle`, `SelectVehicleLabel`, `FormatSelectVehicleHint`, `ChooseVehicleIntro`) | Offen | — |
| 2 | Konfiguration/DI | `IPreferences` als `Preferences.Default` in `MauiProgram` registrieren | Offen | — |
| 3 | Speicherung | `IVehicleSelectionStore` anlegen | Offen | — |
| 4 | Speicherung | `PreferencesVehicleSelectionStore` anlegen und registrieren | Offen | — |
| 5 | Logik | `VehicleSelectionResolver.Resolve` anlegen | Offen | — |
| 6 | Tests | `FakePreferences` in `Unit/Support` anlegen | Offen | — |
| 7 | Tests | Unit-Tests `VehicleSelectionResolverTests` (5 Fälle) schreiben | Offen | — |
| 8 | Tests | Unit-Tests `PreferencesVehicleSelectionStoreTests` (3 Fälle) schreiben | Offen | — |
| 9 | Navigation | `ITankbookNavigator` umbauen (`OpenEntriesAsync`, `CloseAsync` entfernen; `OpenVehicleManagementAsync(bool)` ergänzen) | Offen | — |
| 10 | Navigation | `ShellTankbookNavigator` umbauen (`VehiclesRoute` statt `EntriesRoute`, `SelectPages` entfernen) | Offen | — |
| 11 | Navigation | `AppShell.xaml.cs`: Route `tankbookentries` entfernen, `tankbookvehicles` → `TankbookPage` registrieren | Offen | — |
| 12 | Navigation | `AppShell.xaml`: Tab `tankbook` auf `TankbookEntriesPage` umstellen | Offen | — |
| 13 | ViewModel | `VehicleItemViewModel`: Parameter `openEntries` und `OpenEntriesCommand` entfernen; Konstruktionsaufrufe anpassen | Offen | — |
| 14 | ViewModel | `TankbookViewModel`: `IQueryAttributable` (`startCreate`) ergänzen, `OpenEntries` entfernen | Offen | — |
| 15 | ViewModel | `TankbookEntriesViewModel`: Konstruktor um `IVehicleSelectionStore` erweitern, `IQueryAttributable` und `Show`/`CloseAsync` entfernen | Offen | — |
| 16 | ViewModel | `TankbookEntriesViewModel.LoadAsync` auf Resolver umstellen, Fallback und Speicherung der Kennung | Offen | — |
| 17 | ViewModel | `TankbookEntriesViewModel.SelectVehicleAsync` und Chip-Liste (`VehicleOptions`, `ToggleVehiclePickerCommand`) | Offen | — |
| 18 | ViewModel | `TankbookEntriesViewModel`: `ManageVehiclesCommand`, `AddVehicleCommand`, Leerzustand-Eigenschaften | Offen | — |
| 19 | ViewModel | `TankbookEntriesViewModel`: Ansage bei `OnAppearing` und nach Wechsel; `OnRestored` mit `Clear()` | Offen | — |
| 20 | View | `TankbookEntriesPage.xaml`: Kopf mit Auswahl, Chip-Liste, Verwaltungslink, Leerzustand, Sichtbarkeit an `HasVehicle`, Titel | Offen | — |
| 21 | View | `TankbookPage.xaml`: Titel „Fahrzeuge“, Schaltfläche „Tank- und Ladevorgänge“ entfernen, Einleitungstext | Offen | — |
| 22 | Texte | Veraltete Texte `TankbookTexts.Entries` und `TankbookEntryTexts.PageTitle` entfernen (nach Prüfung) | Offen | — |
| 23 | Tests | Bestehende Unit-Tests anpassen (Navigation, Load, Restore, Konstruktion, Titel, Textetests) | Offen | — |
| 24 | Tests | `ShellTankbookNavigatorTests_SelectPages` entfernen oder reduzieren | Offen | — |
| 25 | Tests | Neue Unit-Tests `TankbookEntriesViewModelTests_Selection` schreiben (Laden, Fallback, Leerzustand, Wechsel, Ansage) | Offen | — |
| 26 | Tests | Unit-Test `TankbookEntriesViewModelTests_Restore` für Auswahl-Reset ergänzen | Offen | — |
| 27 | Tests | Unit-Test `TankbookViewModelTests_Navigation` für `startCreate` ergänzen | Offen | — |
| 28 | E2E-Hilfsmethoden | `TankbookE2EDriver`: `OpenEntries` (Tab und Auswahl), `ManageVehicles`, `SelectVehicle`, `AddVehicleFromEmptyState`; `LeaveEntries` entfernen | Offen | — |
| 29 | E2E-Tests | Bestehende E2E-Tests an neuen Einstieg anpassen (`TankbookEntriesE2ETests_Flow`, `TankbookE2ETests_*`, `NavigationE2ETests`, Aufrufer von `OpenEntries`) | Offen | — |
| 30 | E2E-Tests | Neuer E2E-Test: Tab zeigt Vorgänge direkt (Pflicht) | Offen | — |
| 31 | E2E-Tests | Neuer E2E-Test: Fahrzeugwechsel über Auswahl im Kopf (Pflicht) | Offen | — |
| 32 | E2E-Tests | Neuer E2E-Test: Leerzustand und Anlegen über „Fahrzeug hinzufügen“ (Pflicht) | Offen | — |
| 33 | E2E-Tests | Neuer E2E-Test: Auswahl bleibt nach Neustart erhalten (Pflicht) | Offen | — |
| 34 | E2E-Tests | Neuer E2E-Test: Löschen des gewählten Fahrzeugs führt zum nächsten Fahrzeug bzw. Leerzustand (Pflicht) | Offen | — |
| 35 | E2E-Tests | Neuer E2E-Test: Verwaltung erreichbar und Zurück zum Tab (Pflicht) | Offen | — |
| 36 | E2E-Tests | `TankbookEntriesE2ETests_RestoreReload` um Auswahl-Reset ergänzen (Soll) | Offen | — |
| 37 | Dokumentation | ADR `docs/adr/0018-tankbook-start-view.md` anlegen; Verweis in ADR 0009 ergänzen | Offen | — |
| 38 | Dokumentation | `docs/help/Tankbuch/beschreibung.md`, `ablauf-anwender.md`, `ablauf-technisch.md` aktualisieren; README auf Tankbuch-Einstieg prüfen | Offen | — |
