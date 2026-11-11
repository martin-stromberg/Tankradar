# Offene Aufgaben

Erstellt am: 2026-11-11
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- (keine — Plan-Review: Vollständig umgesetzt)

## Code-Review-Befunde

- [ ] `AutoSearchOnceAsync` (`src/Tankradar.MAUI/ViewModels/MapViewModel.cs` ~Z. 530) prüft `GpsUsage` gegen den `_settings`-Cache (`GetSettingsOrReportAsync`), nicht gegen den im selben `OnAppearing` gestarteten frischen Lauf. Bei echt asynchronem `SettingsService.LoadAsync` ist `_settings` bei erneutem Erscheinen regelmäßig veraltet: WhileInUse→Never feuert dennoch eine Standort-/Preisabfrage (Verletzung des Still-Skip-Vertrags, privacy-relevant); Never→WhileInUse verstreicht eine Erscheinen-Runde beim Nachholen. Empfehlung: Vorlauf soll `LoadSettingsAsync()` direkt abwarten statt des `_settings`-Kurzschlusses; Test für den Wechsel mit asynchronem Laden ergänzen.

## Fehlgeschlagene Tests

- (keine — letzter Lauf: 2598/2598 bestanden; nach den Iteration-3-Fixes Unit 2095/2095, Integration 298/298)
