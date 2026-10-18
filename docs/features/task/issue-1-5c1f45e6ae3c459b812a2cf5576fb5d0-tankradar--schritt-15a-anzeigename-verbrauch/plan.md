# Umsetzungsplan Schritt 15a

## Entscheidungen

1. **Anzeigelogik zentral** in `StationNaming` (Models/Pricing): `Primary = CustomName ?? Brand ?? Name` (getrimmt, leere Werte zählen als fehlend); `Secondary = Name`, wenn er sich (ohne Groß-/Kleinschreibung) von `Primary` unterscheidet, sonst leer. Der eigene Anzeigename hat Vorrang; darunter steht klein der Stammdatenname (Marke bleibt unter eigenem Namen unberücksichtigt, um die Zeile kurz zu halten).
2. **Neue Spalte** `Stations.CustomName` (nullable, max. 60 Zeichen, `StationLimits.MaxCustomNameLength`), Migration `AddStationCustomName` rein additiv (`ADD COLUMN`), Daten bleiben erhalten. Der Upsert der API (`PriceRepository.SaveAsync`) berührt die Spalte nicht.
3. **Dienst** `IPriceRepository.SetCustomNameAsync(stationId, name)` → `StationNameResult` (`Ok`, `Invalid` (zu lang), `NotFound`); leerer/Leerraum-Name entfernt den eigenen Namen. `IFuelPriceService` reicht durch (`SetCustomNameAsync`), Cache der Detailabfrage wird invalidiert, Favoritenänderungs-Benachrichtigung nicht nötig (Seiten laden beim Erscheinen).
4. **Modelle**: `StationInfo.CustomName`; `StationListItem.Name` trägt künftig den Primärnamen, neues letztes optionales Feld `SubName` (+ `HasSubName`); `StationDetailItem.BrandText` wird durch `SubNameText` ersetzt, neu `CustomName` (für das Bearbeitungsfeld); `FavoriteEntry.StationName` trägt den Primärnamen, neues optionales Feld `SubName`; `KnownStation.Name` trägt den Primärnamen.
5. **Anzeige**: `StationTile`, `FavoriteGroupPage` (Name + kleiner Stammdatenname, AutomationIds `…SubName`), Detailseite (Name groß, Stammdatenname klein `Detail.SubName`, Karte „Eigener Name“ mit Eintragsfeld `Detail.CustomName.Entry`, Buttons `Detail.CustomName.Save` / `.Remove`; Chip `Detail.Brand` entfällt), Karte/Suche/Marker erhalten den Primärnamen über `StationListItem.Name`.
6. **Verbrauch ab Startstand**: `ConsumptionCalculator.Calculate(entries, startOdometerKm, startKind)`; für die Startart (Strom bei Elektro, sonst Kraftstoff) dient der Start-Kilometerstand als Basis des ersten Vollvorgangs; Teilbetankungen davor zählen mit. `TankbookEntryOverview.IsFromStart` kennzeichnet das Intervall. Für die andere Art eines Hybrids bleibt es bei „erster voller Vorgang“. Strecke ≤ 0 → `NoDistance` (Text nennt Startstand).
7. **Auswertung**: Reguläre Intervalle gehen vor; solange nur das Startintervall existiert, bilden Durchschnitt und Kosten pro 100 km dieses Intervall, gekennzeichnet (`TankbookAverageSource.FromStart`), und ersetzen den Startverbrauch (nur wenn kein berechnetes Intervall außer dem Startintervall). Sobald ein regulärer Vollvorgang-Intervall vorliegt, wird das Startintervall nicht mehr in Durchschnitt/Kosten eingerechnet (Annahme „Tank voll“ ist unsicher). Verbrauchsverlauf und Liste zeigen den Wert des ersten Vorgangs mit Kennzeichnung.
8. **Texte** (`TankbookEntryTexts`, `TankbookStatisticsTexts`, `DetailTexts`): „Verbrauch 6,50 L/100 km (ab Startstand)“, Hinweis „Berechnet ab dem Start-Kilometerstand; angenommen wird, dass der Tank beim Start voll war.“; Kachel „ab Startstand“.
9. **Sicherung**: Keine Codeänderung nötig (Preparer kopiert Spalten der Referenz auf dem Stand der Sicherung; neue Spalte in neuen Sicherungen; ältere Sicherungen ohne Spalte werden migriert). Nachweis per Tests (Roundtrip mit `CustomName`, Wiederherstellung einer Sicherung auf dem Vorgängerstand). `BackupTestEnvironment` seedet `CustomName`. Härtung aus Abnahme 15: Quellfehler beim Lesen/Entpacken → `Failed` statt `InvalidFile` soweit eindeutig; Hilfe ergänzt Datenschutzhinweis (Stationen/Preisstände).
10. **Mock/Testdaten**: Mock-Server liefert bereits `brand`; ergänzt wird eine Station mit Inhabernamen (Name „Klaus Heinrich“, Marke „Westfalen“) für E2E; sofern Eingriffe in bestehende Erwartungen (Anzahl Stationen) vermieden werden müssen, wird sie nur über den Parameter `stations` der Szenarien geladen.

## Schritte
1. Modell/Datenhaltung: `StationEntity.CustomName`, `TankradarDbContext` (MaxLength), Migration + Designer + Snapshot, `StationNaming`, `StationInfo`, `PriceRepository` (Map, `SetCustomNameAsync`), `IFuelPriceService`.
2. Builder und Dienste: `StationResultBuilder`, `StationDetailBuilder`, `FavoritesService`, `HomeOverviewBuilder`, `FavoriteGroupViewModel`, `TankbookEntryService.GetKnownStationsAsync`.
3. UI/ViewModel: `StationDetailViewModel` (Eigener Name: Entwurf, Speichern, Entfernen, Statusmeldung), XAML (Detail, Tile, Gruppenseite), Texte (`DetailTexts`).
4. Verbrauch: `ConsumptionCalculator`, `TankbookEntryOverview`, `TankbookEntryService.GetEntriesAsync` (Fahrzeug laden), `TankbookStatisticsCalculator`, Texte, ItemViewModel/XAML-Hinweis.
5. Tests: Unit (Naming, Builder, Favorites/Known, Calculator, Statistik, Texte, DetailViewModel), Integration (Repository, Migration-Upgrade mit Datenerhalt, Sicherung Roundtrip + Vorgängerstand), E2E (Anzeigename vergeben, Favoriten zeigt Marke/Eigenen Namen, erster Verbrauch ab Startstand).
6. Doku: ADR 0012 (Anzeigename) bzw. Ergänzung ADR 0009/0010/0011, Hilfe (Tankstellendetails, Favoriten, Startseite, Suche, Tankbuch, Sicherung), `changes.log`, README.
7. Auslieferung: lokale CI, iOS-Deployment-Test, Pre-Push-Prüfungen, Windows-Zwischenstand 0.1.20.

## Offene Punkte

(keine)
