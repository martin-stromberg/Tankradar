# Übersetzte Anforderung: Favoritengruppen und Zuordnung von Tankstellen (Schritt 10)

## Ziel
Nutzer verwalten favorisierte Tankstellen in selbst benannten Gruppen (z. B. „Arbeitsweg“, „Heimat“, „Urlaub“). Zusätzlich werden vier Restpunkte der Kartenkacheln aus Schritt 9 erledigt.

## Anforderungen (wörtliche Beschreibung des Projektplans)
Nutzer sollen favorisierte Tankstellen in selbst benannten Gruppen verwalten
(z. B. „Arbeitsweg", „Heimat", „Urlaub"). Eine Tankstelle darf mehreren Gruppen angehören. In der
Tankstellen-Detailansicht gibt es die Schaltfläche „Zu Favoriten hinzufügen". Sie bietet die
Auswahl einer bestehenden Gruppe oder das Anlegen einer neuen an. Ist die Tankstelle bereits einer
oder mehreren Gruppen zugeordnet, gibt es zusätzlich „Aus Favoriten entfernen"; bei mehreren Gruppen
wählt der Nutzer dort, aus welcher Gruppe oder welchen Gruppen sie entfernt wird. Tankstellen werden
ausschließlich über die Detailansicht Gruppen zugeordnet.

Gruppen können erstellt, umbenannt und (nach Rückfrage) gelöscht werden. Zu jeder Gruppe lässt
sich optional eine kurze Beschreibung hinterlegen und ändern. Die Gruppenansicht listet
die Tankstellen einer Gruppe. Zu jedem Eintrag lassen sich optional eine Notiz und eine Priorität
pflegen; nach der Priorität ist die Liste geordnet. Favoriten und Gruppen werden lokal gespeichert
und stehen offline zur Verfügung. Tests decken die Gruppenoperationen, die Mehrfachzuordnung, das
Hinzufügen und Entfernen sowie per FlaUI den Ablauf „Tankstelle suchen – Details öffnen – zu neuer
Gruppe hinzufügen – Gruppe umbenennen – entfernen" ab. Nach Abschluss wird ein startfähiger
Windows-Zwischenstand unter `review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt
(nicht committen).

Zusatz des Projektleiters (Restpunkte Kartenkacheln):
1. Kachel-Cache beachtet die HTTP-Caching-Header des Kachelservers (`Cache-Control`/`Expires`, bedingte Anfragen per `ETag`/`If-None-Match` bzw. `Last-Modified`), pauschal 7 Tage nur als Rückfall; ADR 0005 präzisieren.
2. Kachel-Cache unter iOS in `FileSystem.CacheDirectory` statt `AppDataDirectory` (nicht ins Backup).
3. User-Agent mit tatsächlicher App-Version und Kontaktangabe (Projekt-URL `https://github.com/martin-stromberg/Tankradar`); Quellenangabe „© OpenStreetMap-Mitwirkende“ als Link auf https://www.openstreetmap.org/copyright.
4. Unter Windows: Karte per Maus ziehen verschiebt die Karte, Mausrad zoomt.

## Rahmen
Eine SQLite-Datenbank, Schemaänderung erhält bestehende Daten (EF-Core-Migration); Designentwurf `tankstellen_details_favoriten` (Karte „Favoritengruppen“); Tests nie gegen produktive Endpunkte; Oberflächentests als Gate; Anzeigename „Tankatlas“.
