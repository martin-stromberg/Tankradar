# Projektplan – UI im Tankbuch optimieren

## Übersicht

Projekttitel: UI im Tankbuch optimieren

Gesamtziel: Der Tab „Tankbuch“ reagiert beim Navigieren sofort, die Monatskostenliste zeigt nur Umsatzmonate, Vorgangsaktionen werden kompakt als Symbolschaltflächen dargestellt und der Exportbereich ist standardmäßig zugeklappt.

Betroffene Bereiche: Tankbuch-Tab (Navigation/Laden), Auswertung (Monatsliste), Vorgangsliste (Aktionen), Exportbereich, Unit- und E2E-Tests, Dokumentation.

## Grobe Vorgehensentscheidungen

1. **Navigation/Laden entkoppeln statt Datenmodell ändern:** Der erste Datenbankzugriff des Tankbuch-ViewModels wird hinter einen UI-Yield gesetzt. Dadurch kann die Navigationsverarbeitung zuerst fortgesetzt werden; bestehende Lade- und Fehlerlogik bleibt erhalten.
2. **Sichtbare Monatsliste filtern, nicht die Statistikbasis verändern:** Monate ohne Vorgang werden im Kind-Statistik-ViewModel aus der `Months`-Liste ausgeblendet. Monatskarte und Kostenverlauf behalten die bisherige kontinuierliche Monatsreihe.
3. **Vorhandene Symbolbutton-Konventionen nutzen:** Bearbeiten/Löschen werden als kompakte Buttons mit Glyphen und `SemanticProperties.Description`/`Hint` dargestellt; AutomationIds bleiben unverändert.
4. **Exportkarte mit Zustand im bestehenden ViewModel klappbar machen:** Ein `IsExpanded`-Zustand steuert Sichtbarkeit des Inhalts und Glyphen/Beschreibung der Symbolschaltfläche. Standard ist zugeklappt.
5. **Tests und Dokumentation in den jeweiligen Schritten mitführen:** Schritt 1 deckt den asynchronen Start ab; Schritt 2 passt Statistik-, Listen- und Export-UI samt Tests an.

## Entwicklungsschritte

### Schritt 1: Tankbuch-Navigation sofort ausliefern

**Fachliche Anforderung:** Der Tab „Tankbuch“ soll beim Navigieren nicht spürbar auf das Laden des gewählten Fahrzeugs warten. Die Seite/Navigation darf zuerst sichtbar reagieren; Fahrzeug-, Vorgangs- und Auswertungsdaten laden danach wie bisher weiter.

**Akzeptanzkriterien:**

- `TankbookEntriesViewModel.OnAppearing` ruft den Fahrzeugdienst nicht mehr synchron vor dem ersten UI-Yield auf.
- Das bestehende Laden von Fahrzeugen, Auswahl, Vorgängen, bekannten Tankstellen und Statistik bleibt funktional unverändert.
- Ein Unit-Test belegt, dass `GetVehiclesAsync` beim synchronen `OnAppearing`-Aufruf noch nicht ausgeführt wird und nach Ablauf von `LastTask` ausgeführt wurde.
- Relevante bestehende Tankbuch-Lade- und Auswahltests bleiben grün.

**Abhängigkeiten:** Keine.

**Betroffene fachliche Bereiche:** Tankbuch-Tab, asynchrones Laden, UI-Reaktionsfähigkeit.

### Schritt 2: Tankbuch-Übersicht und Exportbereich verschlanken

**Fachliche Anforderung:** Die Monatskostenliste zeigt keine Monate ohne Umsatz; Bearbeiten/Löschen in der Vorgangsliste sind Symbolschaltflächen; der Exportbereich kann auf- und zugeklappt werden und startet zugeklappt.

**Akzeptanzkriterien:**

- `TankbookKindStatisticsViewModel.Months` enthält nur Monatszeilen mit `EntryCount > 0`; Monatskarte und Kostenverlauf bleiben wie bisher aus der ungekürzten Statistikreihe gebildet.
- Bearbeiten und Löschen erscheinen als Symbolschaltflächen mit den bisherigen AutomationIds `TankbookEntry.Item.Edit` und `TankbookEntry.Item.Delete`, mindestens `MinimumTouchTarget` und barrierefreien Beschreibungen.
- Die Lösch-Rückfrage samt Bestätigen/Abbrechen bleibt unverändert.
- `TankbookExportViewModel` besitzt einen öffentlichen Auf-/Zuklappzustand mit Standard `false`, passenden Glyph-/Beschreibungstexten und einem Toggle-Command.
- In der XAML-Oberfläche sind alle bisherigen Exportbedienelemente bei geschlossenem Bereich ausgeblendet; nach dem Öffnen funktionieren Format-, Zeitraum-, Datums- und Exportfunktionen wie bisher.
- Unit- und E2E-Tests decken gefilterte Monatszeilen, Symbolschaltflächen/Barrierefreiheit und den zugeklappten Export-Startzustand ab; Export-E2E-Hilfen klappen den Bereich bei Bedarf auf.

**Abhängigkeiten:** Schritt 1.

**Betroffene fachliche Bereiche:** Tankbuch-Statistik, Vorgangsliste, Export-UI, Barrierefreiheit, Tests.

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|---------------------|
| 1 | Tankbuch-Navigation sofort ausliefern | Keine | Navigation, asynchrones Laden |
| 2 | Tankbuch-Übersicht und Exportbereich verschlanken | 1 | Statistik, Vorgangsliste, Export, Tests |

## Offene Punkte

Keine.
