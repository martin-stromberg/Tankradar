← [Zurück zur Übersicht](index.md)

# Favoritengruppen — Beschreibung

## Zweck

Favorisierte Tankstellen werden in Gruppen verwaltet. Eine Tankstelle darf mehreren Gruppen angehören, je Gruppe aber nur einmal. Entscheidungen und Abweichungen vom Designentwurf: [ADR 0006](../../adr/0006-favorite-groups.md).

## Zuordnen nur über die Detailansicht

Die Karte **Favoritengruppen** am Ende der Detailansicht zeigt die Gruppen der Tankstelle und bietet:

- **Zu Favoriten hinzufügen:** wählen Sie eine bestehende Gruppe (nur solche, denen die Tankstelle noch nicht angehört) oder geben Sie den Namen einer neuen Gruppe ein und tippen **Anlegen und hinzufügen**.
- **Aus Favoriten entfernen** (nur bei vorhandener Zuordnung): bei einer Gruppe wird sofort entfernt, bei mehreren wählen Sie die Gruppen aus und tippen **Entfernen**.

In der Gruppenübersicht und in der Gruppenansicht lassen sich Tankstellen weder zu- noch abordnen.

## Gruppen verwalten (Bereich „Favoriten“)

- Die Übersicht zeigt je Gruppe Name, Beschreibung und Zahl der Tankstellen; **Neue Gruppe** legt eine Gruppe mit optionaler Beschreibung an.
- Die Gruppenansicht (**Gruppe öffnen**) bietet **Gruppe bearbeiten** (umbenennen, Beschreibung ändern) und **Gruppe löschen** (nach Rückfrage; die Tankstellen bleiben bekannt und in anderen Gruppen erhalten).
- Zu jeder Tankstelle der Gruppe lassen sich über **Notiz und Priorität** eine Notiz und eine Priorität (Keine, Niedrig, Mittel, Hoch) hinterlegen; die Liste ist nach Priorität geordnet (hoch zuerst), dann nach Namen.

## Regeln

- Gruppennamen: 1 bis 40 Zeichen, ohne Beachtung der Groß- und Kleinschreibung eindeutig. Beschreibung höchstens 200, Notiz höchstens 500 Zeichen.
- Eine Tankstelle muss lokal bekannt sein (jede Tankstelle der Suche ist es); sonst bittet die App, erneut zu suchen.
- Die Startseite mit Preisen und Entfernungen folgt in einem späteren Schritt; aus der Gruppenansicht lässt sich die Detailansicht noch nicht öffnen.
