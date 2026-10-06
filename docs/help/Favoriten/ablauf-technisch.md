← [Zurück zur Übersicht](index.md)

# Favoritengruppen — Technischer Ablauf

## Daten

Migration `AddFavorites` legt `FavoriteGroups` (Name mit `NOCASE`-Index, eindeutig) und `FavoriteEntries` (Gruppe mit Kaskade, Tankstelle mit `Restrict`, Notiz, Priorität als Enum-Name, eindeutig je Gruppe und Tankstelle) an; bestehende Daten bleiben erhalten (`FavoritesMigrationTests_Upgrade`).

## Dienst

`IFavoritesService` / `FavoritesService` (Singleton, `IDbContextFactory`, `IDatabaseInitializer`) kapselt alle Operationen und liefert `FavoriteResult` (`Ok`, `InvalidName`, `InvalidText`, `DuplicateName`, `GroupNotFound`, `StationUnknown`, `AlreadyMember`, `NotMember`). „Anlegen und hinzufügen“ läuft in einem `SaveChanges`. Nach jeder gespeicherten Änderung wird `Changed` ausgelöst; ViewModels melden sich über `FavoritesChangeSubscription` schwach an und laden neu.

## Oberfläche

- `StationFavoritesViewModel` (Karte in `StationDetailPage`, über `StationDetailViewModel.Favorites`): Chips, Hinzufügen-/Entfernen-Bereiche mit `GroupOptionViewModel`.
- `FavoritesViewModel` / `FavoritesPage`: Gruppenübersicht; `IFavoriteGroupNavigator` öffnet `FavoriteGroupPage` (Route `favoritegroup`, Parameter `groupId`).
- `FavoriteGroupViewModel` mit `FavoriteEntryItemViewModel`: Bearbeiten, Löschen mit Rückfrage, Notiz und Priorität.
- Texte: `FavoritesTexts`. Bedienung ohne Betriebssystem-Dialoge, mit AutomationIds `Detail.Favorites.*`, `Favorites.*`, `FavoriteGroup.*`.

## Tests

Unit (Dienst, ViewModels, Änderungsbenachrichtigung), Integration (Migration, Neustart, Datenbankregeln), FlaUI (`FavoritesE2ETests_Flow`: suchen – Details – neue Gruppe – umbenennen – entfernen; `FavoritesE2ETests_Groups`).
