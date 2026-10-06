← [Zurück zur Übersicht](index.md)

# Tankstellen-Detailansicht — Technischer Ablauf

| Komponente | Aufgabe |
|------------|---------|
| `MapViewModel.OpenStationCommand` | übergibt die `StationListItem` an `IStationNavigator`; Doppelauslösung (Karte und Schaltfläche) wird ignoriert |
| `IStationNavigator` / `ShellStationNavigator` | Shell-Route `stationdetail` mit Navigationsparameter `station`; die Rückkehr übernimmt die Kopfleiste der Shell |
| `StationDetailViewModel` | `IQueryAttributable`; zeigt zunächst die Listendaten, ruft `IFuelPriceService.GetStationDetailAsync` ab, baut mit `StationDetailBuilder` die Anzeige; abonniert `IConnectionMonitor.ConnectionChanged` (Offline-Hinweis) und `ConnectionRestored` (Neuabruf, wenn der letzte Stand `OfflineFallback` war oder ein Preis veraltet ist) |
| `StationDetailBuilder` | Preiszeilen der aktivierten Sorten in Einstellungsreihenfolge, Hinweise, Öffnungszeiten; blendet fehlende Angaben aus |
| `DetailFreshness` | Altersgrenze `MaxAge` 24 h für Detailangaben; `StationInfo.WithoutStaleDetails` entfernt Öffnungszeiten und `WholeDay` darüber (auch in `StationResultBuilder` und `FuelPriceService.WithKnownDetailsAsync`) |
| `StationDetailPage` | XAML-Seite (AutomationIds `Detail.*`), im DI als Transient registiert; Offline-Hinweis in einer eigenen Zeile über dem `ScrollView`; Preis aus `PriceMainText`, `PriceFractionText` (hochgestellt) und `PriceUnitText`, vorgelesen wird `PriceSpokenText`; „Preise aktualisieren“ ist über `CanRefresh` (online und nicht beschäftigt) gesteuert |
| `StationDetailItem` | `PriceStatusText` („Live-Preise“ bzw. „Preise: vor X Min.“ des ältesten Preises), `HasInfoBox`, `IsAutomatedStation` (Chip „Automat 24/7“) |

Die Entfernung stammt aus der Suche (die Detailabfrage der Quelle liefert sie nicht). Von Ausnahmen wird nur der Typ protokolliert.
