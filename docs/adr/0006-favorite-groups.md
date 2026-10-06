# ADR 0006: Favoritengruppen

## Status

Angenommen (2026-10-06)

## Kontext

Schritt 10 ergänzt Favoritengruppen: Nutzer ordnen Tankstellen in der Detailansicht selbst benannten Gruppen zu
(eine Tankstelle darf mehreren Gruppen angehören), verwalten die Gruppen (anlegen, umbenennen, löschen nach
Rückfrage, Beschreibung) und pflegen je Eintrag Notiz und Priorität. Alles wird lokal gespeichert und steht offline
zur Verfügung. Designentwurf: Screens `tankstellen_details_favoriten` (Karte „Favoritengruppen“ mit zugeordneten
Gruppen, „Zu weiterer Gruppe hinzufügen / verwalten“ und „Aus Favoriten entfernen“) und
`startseite_favoriten_gruppen` (Startseite mit Gruppen und Preisen, gehört zu Schritt 11).

## Entscheidung

- **Datenmodell in der einzigen SQLite-Datenbank** (EF-Core-Migration `AddFavorites`, legt nur neue Tabellen an;
  bestehende Einstellungen, Tankstellen und Preise bleiben erhalten, geprüft durch einen Upgrade-Test auf einer
  echten Datei):
  `FavoriteGroups` (Id, Name, Beschreibung, Zeitpunkt des Anlegens) und `FavoriteEntries` (Id, Gruppe, Tankstelle,
  Notiz, Priorität, Zeitpunkt des Hinzufügens). Die Zuordnung ist je Gruppe und Tankstelle eindeutig (Mehrfachzuordnung
  über mehrere Gruppen ist erlaubt). Das Löschen einer Gruppe nimmt ihre Zuordnungen mit (Kaskade); die Tankstelle
  selbst (`Stations`) ist durch einen Fremdschlüssel mit `Restrict` gegen versehentliches Löschen geschützt und wird von
  der App nie gelöscht, damit Favoriten ihre Stammdaten (Name, Adresse) offline behalten.
- **Gruppennamen** sind getrimmt 1 bis 40 Zeichen lang und ohne Beachtung der Groß- und Kleinschreibung eindeutig
  (Prüfung im Dienst mit `OrdinalIgnoreCase`, zusätzlich eindeutiger Index mit `NOCASE` als Sicherung). Beschreibung
  höchstens 200, Notiz höchstens 500 Zeichen. Die Priorität (`Keine`, `Niedrig`, `Mittel`, `Hoch`) wird wie die übrigen
  Enums als Name gespeichert; die Liste einer Gruppe ist nach Priorität (hoch zuerst), dann nach Namen geordnet.
- **Zuordnung ausschließlich über die Detailansicht.** Die Karte „Favoritengruppen“ zeigt die Gruppen der Tankstelle als
  Chips, „Zu Favoriten hinzufügen“ bietet die Auswahl einer bestehenden Gruppe (nur Gruppen, denen die Tankstelle noch
  nicht angehört) oder das Anlegen einer neuen Gruppe in einem Schritt („Anlegen und hinzufügen“, entweder beides oder
  nichts). „Aus Favoriten entfernen“ erscheint nur bei vorhandener Zuordnung: bei einer Gruppe entfernt es sofort, bei
  mehreren erscheint eine Mehrfachauswahl mit „Entfernen“. Gruppenübersicht und Gruppenansicht können Tankstellen nicht
  zu- oder abordnen. Die Tankstelle muss lokal bekannt sein (jede Tankstelle der Suche wird gespeichert); sonst meldet die
  Karte, dass erneut gesucht werden soll.
- **Bedienung ohne Dialoge des Betriebssystems.** Auswahl, Namenseingabe, Rückfrage vor dem Löschen und das Bearbeiten
  von Notiz und Priorität erscheinen als Bereiche in der Seite selbst. Grund: Die Dialoge (`DisplayActionSheet`,
  `DisplayPromptAsync`) sind per UI Automation unter Windows nicht zuverlässig bedienbar, der Abnahmeablauf läuft als
  FlaUI-Test (Auslieferungs-Gate); die Bereiche sind zudem mit Tastatur und Sprachausgabe bedienbar (AutomationIds,
  Beschreibungen, Auswahlhinweis „Ausgewählt“).
- **Gruppenübersicht im Bereich „Favoriten“, Gruppenansicht als eigene Seite** (Route `favoritegroup`, Parameter
  `groupId`). Die Übersicht listet Name, Beschreibung und Zahl der Tankstellen und legt neue Gruppen an. Die Startseite
  mit Preisen, Entfernungen und „In der Nähe“ (Schritt 11) ersetzt später die Darstellung der Übersicht; Datenmodell
  und Dienst bleiben. Aus der Gruppenansicht lässt sich die Detailansicht einer Tankstelle noch nicht öffnen (Schritt 11:
  „Tippt der Nutzer eine Tankstelle an, öffnet sich ihre Detailansicht“); die Tankstelle wird weiterhin über die Suche
  geöffnet.
- **Änderungsbenachrichtigung.** Der Dienst löst nach jeder gespeicherten Änderung `Changed` aus. Geöffnete Ansichten
  (Detailansicht im Hintergrund eines anderen Reiters, Gruppenliste) laden sich neu; die Anmeldung referenziert die
  Ansicht nur schwach (`FavoritesChangeSubscription`), damit der Singleton-Dienst keine Seiten am Leben hält. Grund: Die
  Shell meldet beim Reiterwechsel keine erneute Anzeige für Seiten, die auf dem Navigationsstapel eines Reiters liegen
  (im FlaUI-Test beobachtet: nach dem Umbenennen einer Gruppe zeigte die Detailansicht sonst den alten Namen).
- **Datenschutz.** Namen, Beschreibungen, Notizen und Tankstellenkennungen werden nie protokolliert (nur Ausnahmetypen);
  die Daten verlassen das Gerät nicht und sind nicht Teil der Sicherheitsprüfung von Paketen oder Artefakten.

## Abweichungen vom Designentwurf

- **Kein Stern in der Kopfkarte der Detailansicht.** Der Entwurf zeigt einen Favoritenstern, der die Tankstelle sofort
  favorisiert. Die Anforderung verlangt die Gruppenzuordnung über „Zu Favoriten hinzufügen“; ein Stern ohne Gruppe
  gäbe einen zweiten, uneindeutigen Weg. Die Karte „Favoritengruppen“ am Seitenende trägt die Bedienung.
- **Schaltflächen „Zu Favoriten hinzufügen“ und „Aus Favoriten entfernen“** statt „Zu weiterer Gruppe hinzufügen /
  verwalten“ und eines Kreuzes am Gruppen-Chip, wie in der Anforderung benannt; die Chips der zugeordneten Gruppen sind
  rein anzeigend. „Aktiv synchronisiert“ entfällt (es gibt keine Synchronisierung, nur lokale Speicherung).
- **Gruppenansicht und Gruppenübersicht** sind im Entwurf nur als Startseite mit Preisen vorhanden (Schritt 11); sie
  zeigen hier Name, Beschreibung, Anzahl, Adresse, Notiz und Priorität in den Kartenstilen der Detailansicht. Preise,
  Entfernung und „In der Nähe“ folgen in Schritt 11.
- **Prioritäten** als vier benannte Stufen (keine, niedrig, mittel, hoch) statt einer freien Zahl: eindeutig zu bedienen
  und zu sortieren, ohne Eingabefehler.

## Folgen

- Schritt 11 baut die Startseite auf `IFavoritesService` auf (Gruppen mit Tankstellen) und ergänzt Preise, Entfernung
  und das Öffnen der Detailansicht aus der Gruppe.
- Neue Spalten oder Tabellen für Favoriten erfordern weiterhin Migrationen, die bestehende Daten erhalten
  (Skill `entityframework-database`).
