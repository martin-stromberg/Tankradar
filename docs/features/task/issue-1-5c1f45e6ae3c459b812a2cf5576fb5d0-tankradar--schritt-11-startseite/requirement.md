# Anforderung: Schritt 11 – Startseite mit Favoritengruppen und Bereich „In der Nähe“

Quelle: `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/project-plan.md`, Schritt 11 (Beschreibung unverändert), ergänzt um den Zusatz des Projektleiters.

## Anforderung im Wortlaut

**Beschreibung:** Die Startseite (Bereich „Favoriten“) soll gemäß Designentwurf alle
Favoritengruppen mit ihren Tankstellen anzeigen. Zu jeder Tankstelle erscheinen:

- Name
- Entfernung, sofern ein Standort ermittelt werden darf
- die Preise der in den Einstellungen gewählten Spritsorten in der dort festgelegten Reihenfolge,
  mit Altersangabe „vor X Min.“ und Amber-Markierung ab 60 Minuten
- Zusatzhinweise wie „Preis unbestätigt“ oder „Automatentankstelle“

Beim Öffnen und auf Wunsch des Nutzers werden die Preise aktualisiert. Ohne Verbindung erscheinen
die zuletzt bekannten Preise mit Altersangabe und einem Offline-Hinweis in der Kopfzeile. Wird die
Netzverbindung wiederhergestellt, während die Startseite angezeigt wird, aktualisiert die App
veraltete Preise automatisch und entfernt den Offline-Hinweis. Tippt der
Nutzer eine Tankstelle an, öffnet sich ihre Detailansicht.

Zusätzlich gibt es einen optional aktivierbaren Bereich „In der Nähe“. Er erscheint nur, wenn er in
den Einstellungen aktiviert ist und die GPS-Nutzung es erlaubt. Er zeigt Tankstellen um den
aktuellen Standort, ist nach Spritsorten filterbar und dient ausschließlich der Information: Direkt
aus diesem Bereich gibt es keine Favoritenaktionen. Der Schalter zur Aktivierung dieses Bereichs
wird in den Einstellungen ergänzt. Standortdaten werden nicht gespeichert. Gibt es noch keine
Gruppen, zeigt die Startseite einen Hinweis, wie man Favoriten anlegt. Tests decken die Aufbereitung
der Gruppenübersicht, die automatische Aktualisierung nach Wiederverbindung und per FlaUI die Anzeige von Gruppen, Preisen und des Bereichs „In der Nähe“
mit Testdaten ab. Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

**Abhängigkeiten:** 5, 6, 10

**Betroffene Bereiche:** Startseite, Favoriten, Standort, Preisanzeige, Einstellungen

**Zusatz des Projektleiters:** Zusätzlich: (1) Aus der Gruppenansicht der Favoriten öffnet ein Eintrag die Detailansicht der Tankstelle (in ADR 0006 auf diesen Schritt verschoben); ADR 0006 entsprechend aktualisieren. (2) Kleine Restpunkte aus der Abnahme von Schritt 10: Kachel-Cache berücksichtigt den HTTP-Header `Age` bei der Frische-Berechnung, und eine `304`-Antwort ohne eigene Caching-Angaben übernimmt die zuvor gültige Lebensdauer statt pauschal 7 Tage; bei gleichzeitigem Zuordnen derselben Tankstelle zur selben Gruppe meldet der Favoritendienst „bereits zugeordnet“ statt „doppelter Name“ (mit Tests).

## Fachliche Zusammenfassung

Die Gruppenübersicht im Bereich „Favoriten“ (bisher `FavoritesPage`/`FavoritesViewModel`: Gruppenname, Beschreibung, Anzahl) wird zur Startseite ausgebaut. Sie zeigt je Favoritengruppe die zugeordneten Tankstellen mit Name, optionaler Entfernung, den Preiszeilen der gewählten Spritsorten (Reihenfolge und Auswahl aus `AppSettings.FuelTypes`, Preisalter „vor X Min.“, Amber ab 60 Minuten) und den Hinweisen „Preis unbestätigt“ bzw. „Automatentankstelle“. Die Preise werden beim Erscheinen der Seite und auf Wunsch aktualisiert, ohne Verbindung kommen die zuletzt bekannten Preise aus dem lokalen Preis-Cache mit Offline-Hinweis in der Kopfzeile; nach `IConnectionMonitor.ConnectionRestored` werden veraltete Preise automatisch neu geladen und der Hinweis entfällt. Ein optionaler, per neuer Einstellung aktivierbarer Bereich „In der Nähe“ listet nach Spritsorte filterbar die Tankstellen um den aktuellen Standort, nur informativ und ohne Favoritenaktionen.

Zusätzlich: Ein Eintrag der Gruppenansicht (`FavoriteGroupPage`) öffnet die Detailansicht; drei kleine Restpunkte aus der Abnahme von Schritt 10 (`Age`-Header und `304` im Kachel-Cache, „bereits zugeordnet“ statt „doppelter Name“ bei gleichzeitigem Zuordnen) werden behoben.

## Fachpunkte im Einzelnen

1. **Gruppen mit Tankstellen auf der Startseite.** Alle Gruppen (nach Namen geordnet, wie bisher), je Gruppe die Tankstellen in der bisherigen Reihenfolge (Priorität hoch zuerst, dann Name). Tankstellen mit mehrfacher Gruppenzugehörigkeit erscheinen in jeder Gruppe. Die Gruppenverwaltung (Anlegen, Öffnen der Gruppenansicht mit Umbenennen, Löschen, Notiz und Priorität) bleibt erreichbar.
2. **Tankstellenzeile.** Name; Entfernung nur, wenn ein Standort ermittelt werden darf (Einstellung `GpsUsage` ≠ `Never`, Berechtigung, Position verfügbar); Preiszeilen nur für gewählte Spritsorten in der Einstellungs-Reihenfolge; je Preis Alter „vor X Min.“, Amber-Markierung ab 60 Minuten (`PriceFreshness`); Hinweise „Preis unbestätigt“ (`StationHints.HasUnconfirmedPrice`) und „Automatentankstelle“ (`StationHints.IsAutomatedStation`). Nur was die Quelle liefert wird angezeigt (Entscheidung „Fehlende Quelldaten“).
3. **Aktualisierung.** Beim Erscheinen der Seite (sofort lokale Daten anzeigen, dann aktualisieren, wenn online und der letzte Abruf älter als die Cache-Frist ist) und auf Wunsch (Schaltfläche, erzwingt Abruf). Aktualisiert werden die Preise der Favoriten-Tankstellen (Abruf nach Kennungen, nicht per Umkreis).
4. **Offline und Wiederverbindung.** Ohne Verbindung bzw. bei nicht erreichbarem Preisdienst: zuletzt bekannte Preise mit Alter, Offline-Hinweis in der Kopfzeile (`SearchTexts.OfflineBanner`). Bei `ConnectionRestored` während die Seite angezeigt wird: Aktualisierung veralteter Preise (Offline-Stand oder Preise ab 60 Minuten) automatisch, Offline-Hinweis entfällt. Gleiches Muster wie `StationDetailViewModel`.
5. **Auswahl-/Identifikationsanforderung.** Antippen einer Tankstelle (Startseite, Gruppenansicht) öffnet ihre Detailansicht über `IStationNavigator`. Die Tankstelle wird dabei aus der lokalen Datenbank identifiziert (Kennung), nicht über eine Eingabe.
6. **Bereich „In der Nähe“.** Sichtbar nur, wenn die neue Einstellung aktiv ist und `GpsUsage.AllowsLocation()` gilt. Zeigt Tankstellen um den aktuellen Standort (Radius Standard 5 km, aus der bestehenden Umkreissuche), nach Spritsorte filterbar (Alle + gewählte Sorten), ohne Favoritenaktionen. Standort wird nicht gespeichert und nicht protokolliert (nur im Speicher je Aktualisierung).
7. **Einstellung.** Neuer Schalter in „Optionen“ zum Aktivieren von „In der Nähe“ (Standard: aus), persistent in der lokalen Datenbank; Schemaänderung mit Migration, die bestehende Daten erhält.
8. **Leerer Zustand.** Ohne Gruppen: Hinweis, wie man Favoriten anlegt (Tankstelle in der Detailansicht über „Zu Favoriten hinzufügen“ einer Gruppe zuordnen, oder Gruppe anlegen).
9. **Gruppenansicht → Detailansicht** (Zusatz 1) und **ADR 0006 aktualisieren**.
10. **Restpunkte Schritt 10** (Zusatz 2): `Age`-Header, `304` ohne Caching-Angaben, „bereits zugeordnet“ statt „doppelter Name“ – je mit Tests.

## Betroffene Klassen und Komponenten

Bestehend (zu erweitern):
- Datenmodell: `AppSettings` (neue Eigenschaft), `UserSettingsEntity` (neue Spalte) samt EF-Migration, `TileMetadata` (Lebensdauer), `StationInfo` (nur Nutzung).
- Logik/Services: `IFavoritesService`/`FavoritesService` (Zuordnung, Fehlerabbildung), `IFuelPriceService`/`FuelPriceService`, `IPriceRepository`/`PriceRepository`, `ITankerkoenigClient`/`TankerkoenigClient` (Preisabruf nach Kennungen), `ISettingsService`/`SettingsService`, `TileCachePolicy`, `HttpTileSource`, `StationResultBuilder` (Wiederverwendung).
- ViewModels/Views: `FavoritesViewModel`/`FavoritesPage` (Startseite), `FavoriteGroupViewModel`/`FavoriteEntryItemViewModel`/`FavoriteGroupPage` (Antippen → Detail), `SettingsViewModel`/`SettingsPage`, `MauiProgram` (DI).
- Texte: `FavoritesTexts`, `SettingsTexts`.
- Testunterstützung: `MockTankerkoenigServer` (Preisabruf nach Kennungen), `FakeFuelPriceService`, `StubTankerkoenigClient`, `FakeLocationService`.

Neu (voraussichtlich):
- Aufbereitung: `HomeOverviewBuilder` (Gruppen + Tankstellen + Preise → Anzeigeelemente), Modelle `HomeGroup`, `HomeStationItem`.
- Dienste: Preisaktualisierung nach Kennungen in `IFuelPriceService` (`RefreshStationsAsync`), Ergebnis `StationRefreshResult`.
- ViewModels: `HomeGroupViewModel`, `HomeStationViewModel`, `NearbySectionViewModel`.
- Tests: Unit, Integration (Migration, Preisabruf gegen Mock-Server), FlaUI-E2E.
- Dokumentation: `docs/help/Startseite/`, ADR 0006 (Update), ADR 0007 (Designabweichungen Startseite).

## Implementierungsansatz

- Vorhandene Bausteine werden wiederverwendet: Preis-Cache und Offline-Fallback (`FuelPriceService`, `PriceRepository`), Verbindungsüberwachung (`IConnectionMonitor`), Anzeigeaufbereitung (`StationResultBuilder`/`StationListItem` inkl. `PriceFreshness`, `StationHints`), Standortdienst (`ILocationService`), Navigation (`IStationNavigator`, `StationListItem` als Parameter der Detailansicht), Änderungsbenachrichtigung (`IFavoritesService.Changed`).
- Für Favoriten gibt es bisher nur Umkreissuche und Einzel-Detailabruf. Ein Abruf nach Kennungen (Tankerkönig `prices.php`, bis zu 10 Kennungen je Anfrage) vermeidet je Favorit eine Anfrage; Annahme, dass dieser Endpunkt wie die übrigen im Mock-Server nachgebildet wird.
- Entfernungen werden lokal aus Standort und gespeicherter Tankstellenposition berechnet (`PriceRepository.DistanceKm`).
- Restpunkt `Age`: Verbleibende Lebensdauer = Lebensdauer aus `Cache-Control: max-age`/`Expires` abzüglich `Age`. Restpunkt `304`: Die bei der Speicherung gültige Lebensdauer wird in den Metadaten mitgeführt und bei einer `304` ohne eigene Angaben wiederverwendet. Restpunkt Zuordnung: `FavoritesService.SaveAsync` bildet die Verletzung des eindeutigen Index je Aufrufer ab (Zuordnung → `AlreadyMember`, Name → `DuplicateName`).

## Konfiguration

- Neue Benutzereinstellung `ShowNearby` (Standard aus) in `AppSettings`/`UserSettingsEntity`, Umschalter in „Optionen“. Keine weitere Konfiguration; Radius und Anzahl der Einträge von „In der Nähe“ sind feste Werte.

## Annahmen und Abgrenzung

- Elemente des Designentwurfs ohne Entsprechung in der Anforderung (Tages-Empfehlung, Routen-Schaltflächen, Tiefstpreis-/„Nah“-Kennzeichen, Sortieren, Strom-/Ladestationen) entfallen; Strom wegen ADR 0001, übriges als bewusste Abweichung (ADR 0007).
- Die Spritsorten-Filter gelten nur im Bereich „In der Nähe“, nicht für die Favoritengruppen.
- Favoritenaktionen (Zuordnen/Entfernen) bleiben ausschließlich in der Detailansicht (ADR 0006).
- Tests laufen ausschließlich gegen Mock-Server und Testdaten, nie gegen produktive Endpunkte; der API-Schlüssel wird nie ausgelesen oder protokolliert.

## Offene Fragen

Keine, die vor der Umsetzung geklärt werden müssten; folgende Entscheidungen wurden eigenständig und begründet getroffen (Details im Plan): Abruf nach Kennungen statt Einzelabrufen; „In der Nähe“ mit festem Radius 5 km und höchstens 10 Einträgen nach Entfernung; Einträge von „In der Nähe“ öffnen die Detailansicht (Information, keine Favoritenaktion); Gruppen auf der Startseite einklappbar (Designentwurf).
