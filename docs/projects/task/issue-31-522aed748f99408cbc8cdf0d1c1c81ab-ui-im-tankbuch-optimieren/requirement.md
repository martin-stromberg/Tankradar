# Anforderung – UI im Tankbuch optimieren

## Quelle

- GitHub-Issue: `martin-stromberg/Tankradar#31`
- Kommentar: `@softwarewerkstatt erledige bitte diese Aufgabe`
- Basisbranch für den späteren Pull Request: `staging`

## Ziel

Der Tab „Tankbuch“ soll beim Aufruf unmittelbar reagieren und die Übersicht sowie Bedienung der Tank- und Ladevorgänge verbessern.

## Fachliche Anforderungen

1. Der Aufruf des Tankbuchs soll nicht spürbar auf den Datenladevorgang des aktuell gewählten Fahrzeugs warten. Das Navigationsereignis soll die Seite unverzüglich anzeigen; Fahrzeug-, Vorgangs- und Auswertungsdaten dürfen danach weiter asynchron nachgeladen werden.
2. In der Auflistung der Monatskosten sollen Monate ohne Umsatz nicht ausgegeben werden.
3. In der Liste der erfassten Tank- und Ladevorgänge sollen die Schaltflächen „Bearbeiten“ und „Löschen“ durch Symbolschaltflächen ersetzt werden. Barrierefreiheit und bisherige AutomationIds bleiben erhalten.
4. Der Exportbereich soll auf- und zuklappbar sein. Standardmäßig ist er zugeklappt.

## Betroffene Bereiche

- `src/Tankradar.MAUI/Views/TankbookEntriesPage.xaml`
- `src/Tankradar.MAUI/ViewModels/TankbookEntriesViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/TankbookKindStatisticsViewModel.cs`
- `src/Tankradar.MAUI/ViewModels/TankbookExportViewModel.cs`
- zentrale Texte unter `src/Tankradar.MAUI/Resources/Texts/`
- Unit- und FlaUI-E2E-Tests der Tankbuch-, Statistik- und Exportbereiche
- Anwenderdokumentation `docs/help/Tankbuch/` sowie `README.md` und `changes.log`

## Abläufe

- Der Tab `tankbook` zeigt `TankbookEntriesPage`; `TankradarContentPage.OnAppearing` ruft `TankbookEntriesViewModel.OnAppearing`.
- `TankbookEntriesViewModel.LoadAsync` lädt Fahrzeuge, Auswahl, Vorgänge, bekannte Tankstellen und Statistik.
- Die Auswertung erzeugt aktuell eine lückenlose Monatsreihe inklusive Monaten ohne Vorgang; die sichtbare Monatsliste wird im `TankbookKindStatisticsViewModel` erzeugt.
- Der Exportbereich ist aktuell immer ausgeklappt und enthält Format-, Zeitraum-, Datums- und Statusbedienelemente.

## Konfiguration und Daten

- Keine Schemaänderung und keine Migration.
- Keine neue externe Abhängigkeit.
- Keine Änderung an Berechtigungen, API-Schlüsseln oder Netzwerkzugriffen.

## Akzeptanzkriterien

- Beim Wechsel in den Tab „Tankbuch“ startet der Datenladevorgang erst nach Abgabe des UI-Threads; `OnAppearing` blockiert nicht auf den Fahrzeugdienst.
- Die sichtbare Monatsliste enthält nur Monate mit mindestens einem Vorgang bzw. Umsatz; Monatskarte und Diagrammverhalten bleiben fachlich unverändert, soweit sie nicht ausdrücklich Teil der Auflistung sind.
- Bearbeiten und Löschen erscheinen als kompakte Symbolschaltflächen mit mindestens der bestehenden Touch-Zielgröße; Hilfstexte nennen weiterhin Aktion und Vorgang.
- Der Exportbereich ist geschlossen standardmäßig, lässt sich über Kopfbereich/Symbolschaltfläche öffnen und zeigt dann alle bisherigen Exportfunktionen.
- Bestehende Bearbeiten-/Löschen- und Export-AutomationIds bleiben für E2E-Tests stabil; für den Export-Umschalter und die Symbolzustände gibt es Testabdeckung.
- Unit-Tests und relevante E2E-Szenarien laufen erfolgreich oder werden nachvollziehbar angepasst.

## Explizite Nicht-Ziele

- Keine Änderung der Exportformate, Exportdateiinhalte oder Zeitraumlogik.
- Keine Änderung der Datenbank, Datenschutzlogik oder Fahrzeugverwaltung.
- Keine Performance-Optimierung jenseits der Entkopplung des Tankbuch-Navigationsereignisses vom initialen Datenladevorgang.

## Offene Fragen

Keine. Symbolschaltflächen werden passend zum vorhandenen Design mit Text-Glyphen und barrierefreien Beschreibungen umgesetzt.
