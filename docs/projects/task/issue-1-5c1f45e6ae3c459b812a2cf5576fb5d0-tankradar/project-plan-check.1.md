# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan lückenhaft

Grundlage: `requirement.md` (Prüfbasis), `issue.md` (Originalanforderung), `inventory.md` und
`project-plan.md` (Stand 2026-09-28). Die dokumentierten Stakeholder-Entscheidungen vom 2026-09-28
gelten als bewusste Abweichungen und werden nicht als Lücken gewertet: keine Strompreise,
Ladestationen, Strom-Einstellungen und Strom-Filter in 1.0; Export und Route in 1.0; keine
Cloud-Synchronisation vor 1.0; Windows-Zwischenstände unter `review-versions/`, nicht versioniert.
Dasselbe gilt für die weiteren Entscheidungen in der Tabelle „Grobe Vorgehensentscheidungen“
(Tankerkönig, Nominatim, OSM-Karte, OpenRouteService, iOS 16, Data Protection statt
DB-Verschlüsselung, Preisalter-Grenze 60 Minuten, Preisniveau-Farben).

Der Plan deckt die fachlichen Kernanforderungen weitgehend vollständig und in sinnvoller
Reihenfolge ab. Offen sind vier Punkte aus `requirement.md`, die in keinem Schritt vorkommen und
auch nicht als bewusste Abweichung festgehalten sind, außerdem ein Verweis auf eine Funktion, die
erst in einem späteren Schritt entsteht (Details unten).

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| **Plattform und Architektur** | | |
| .NET MAUI, eine Codebasis für iOS (primär) und Windows (Entwicklung, Debugging, Tests) | 1 | Abgedeckt |
| MVVM-Struktur (XAML-Views, ViewModels mit Commands/Bindings, Services per DI) | 1 (Grundgerüst), 4–17 (fachliche Umsetzung) | Abgedeckt |
| Eine einzige SQLite-Datenbank für Nutzer- und Abrufdaten, keine getrennten Datenbanken | 4 (Vorgehensentscheidung „Datenhaltung“) | Abgedeckt |
| Datenbank-Initialisierung, Migrationen, Standardwerte beim ersten Start | 4 | Abgedeckt |
| Datenbankpfade je Plattform (5.3) | 4 | Abgedeckt |
| Designentwurf verbindlich (Navigation, Farben, Typografie, Hell/Dunkel, Komponenten), Abweichungen dokumentiert | 1, fortlaufend 4–17 | Abgedeckt |
| **Tankstellen und Preise** | | |
| Station mit Name, Adresse, Koordinaten, Entfernung, Öffnungszeiten, Zahlungsmöglichkeiten | 5, 8 | Abgedeckt |
| Provisorien („Preis unbestätigt“, „Automatentankstelle“) | 5 (Ableitung), 6, 8, 11 (Anzeige) | Abgedeckt |
| Offizielle Kraftstoffpreis-API, HTTPS, Timeout, Retry mit Backoff | 5 | Abgedeckt |
| Strompreis-API, Ladestationen, AC/DC/HPC | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| Preis-Cache mit Zeitstempel, Quelle, historische Preisstände | 5 | Abgedeckt |
| Aktualität ausschließlich über Zeitstempel; Anzeige „Daten veraltet“ | 5, 6, 8, 11 | Abgedeckt |
| API-Schlüssel nie im Code, Keychain bzw. Credential Locker | 5, 17 | Abgedeckt |
| **Suche** | | |
| Umkreissuche am Standort, Radius 5–50 km, Filter nach Spritsorten | 6 | Abgedeckt |
| Suche nach Adresse/Ort/PLZ mit Radius | 7 | Abgedeckt |
| Ergebnisliste sortierbar nach Preis, Entfernung, Name; Anzeige von Preisen, Entfernung, Provisorien | 6, 7 | Abgedeckt |
| Kartenansicht, Pins farbcodiert nach Preisniveau, Tippen öffnet Details | 9 | Abgedeckt |
| Umschalten Liste/Karte, Voreinstellung aus Einstellungen | 9 | Abgedeckt |
| Tankstellen entlang einer Route | 17 | Abgedeckt |
| Validierung von Adresse, Radius, Filter vor API-Aufruf | 6, 7, 17 | Abgedeckt |
| **Tankstellen-Details** | | |
| Vollständige Detailansicht (Name, Adresse, Entfernung, Preise, Provisorien, Öffnungszeiten, Zahlungsmöglichkeiten) | 8 | Abgedeckt |
| „Zu Favoriten hinzufügen“ mit Auswahl bestehender oder neuer Gruppe; „Aus Favoriten entfernen“ | 10 | Abgedeckt |
| **Favoriten und Gruppen** | | |
| Gruppen erstellen, umbenennen, löschen | 10 | Abgedeckt |
| Zuordnung ausschließlich über die Detailansicht; Mehrfachzuordnung | 10 | Abgedeckt |
| Gruppenansicht mit optionalen Notizen und Prioritäten | 10 | Abgedeckt |
| Optionale Gruppenbeschreibung (`FavoriteGroup.Description`) | – | Nicht erwähnt, siehe Hinweise |
| Cloud-Synchronisation | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| **Startseite** | | |
| Favoritengruppen mit Tankstellen, Name, Entfernung, Preisen der gewählten Sorten, Provisorien | 11 | Abgedeckt |
| Bereich „In der Nähe“, optional aktivierbar, filterbar, nur zur Information | 11 | Abgedeckt |
| Offline-first: zuerst lokale Daten anzeigen, Preise im Hintergrund aktualisieren (4.3) | 5, 11 (Aktualisierung beim Öffnen) | Abgedeckt |
| **Tankbuch** | | |
| Mehrere Fahrzeuge mit Name, Kraftstoffart, Start-Kilometerstand, optionalem Startverbrauch; anlegen, bearbeiten, löschen | 12 | Abgedeckt |
| Tank-/Ladevorgang: Datum, Uhrzeit, Fahrzeug, Tankstelle (Liste oder manuell), Liter/kWh, Preis pro Einheit, Gesamtbetrag, Kilometerstand, Notiz, Belegfoto | 13 | Abgedeckt |
| Vorgänge löschen | 13 | Abgedeckt |
| Verbrauch pro 100 km = (Menge / Strecke) × 100 | 13 | Abgedeckt |
| Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten, Verlaufsgrafiken | 14 | Abgedeckt |
| Export als CSV oder PDF | 16 | Abgedeckt |
| Tankbuch vollständig offline nutzbar | 12, 13, 14 | Abgedeckt |
| **Einstellungen** | | |
| Spritsorten: Auswahl und Reihenfolge | 4 | Abgedeckt |
| Strom: Aktivierung, Auswahl AC/DC/HPC | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| GPS-Nutzung (Immer / Nur bei Nutzung / Nie) | 4, angewendet in 6, 11, 17 | Abgedeckt |
| Standardansicht (Liste/Karte), Standardsortierung | 4, angewendet in 6, 9 | Abgedeckt |
| Lokale Sicherung erstellen und wiederherstellen | 15 | Abgedeckt |
| Sicherungsverlauf: Liste vorhandener Sicherungen und Löschen einzelner Sicherungen (`GetBackupHistoryAsync`, `DeleteBackupAsync`, `BackupSection` „History“) | – | Lücke |
| **Offline-Modus** | | |
| Zwischenspeicherung der letzten bekannten Preise, Offline-Hinweis | 5, 6, 8, 11 | Abgedeckt |
| Bei Wiederverbindung werden die Daten aktualisiert (2.2 „Synchronisation“) | – (nur Aktualisierung beim Öffnen und auf Nutzerwunsch, Schritte 8 und 11) | Lücke |
| **Backup und Restore (4.6)** | | |
| Sicherung enthält alle Nutzerdaten und alle Preisstände mit Zeitstempel | 15 | Abgedeckt |
| Nach Wiederherstellung: veraltete Preise erkennen, bei Bedarf über API aktualisieren, historische Daten erhalten | 15 | Abgedeckt |
| **Sicherheit und Datenschutz** | | |
| Nur HTTPS | 5, 7, 17 | Abgedeckt |
| Verschlüsselte lokale Speicherung sensibler Daten | 5, 17 (Zugangsdaten), 4, 13, 18 (Data Protection gemäß Entscheidung) | Abgedeckt |
| Standort nur bei Bedarf, keine GPS-Rohdaten gespeichert, iOS-Dialog mit klarem Zweck | 6, 11, 17, 18 | Abgedeckt |
| Keine Weitergabe von Fahrzeug- und Tankbuchdaten an Dritte | 12, 13, 15, 16 | Abgedeckt |
| Least Privilege, Secure by Default, Fail Secure | 4, 5, 18 | Abgedeckt |
| **Konfiguration** | | |
| AppSettings-Persistenz über `ISettingsService` und Einstellungsseite | 4 | Abgedeckt |
| API-Schlüssel und Secrets über nicht versionierte Konfiguration bzw. GitHub Secrets | 3, 5, 17, 18 | Abgedeckt |
| CI-Variablen für Apple-Signierung und Zertifikate | 3, 18 | Abgedeckt |
| Konfigurierbare Feature-Flags (5.4: u. a. `RouteSearchEnabled`, `OfflineModeEnforced`, `E2ETestMode`) samt `IFeatureFlagService` | – | Lücke |
| **Tests** | | |
| Unit-, Integrations- und FlaUI-E2E-Testprojekte | 1, danach je Schritt | Abgedeckt |
| E2E: Suche, Favoriten, Preisabruf, Tankbuch-Einträge, Verbrauchsberechnung, Statistik/Export, Backup/Restore | 6, 7, 10, 13, 14, 15, 16 | Abgedeckt |
| E2E-Szenario Offline-Modus: Daten anzeigen, obwohl die API nicht erreichbar ist (4.7, `OfflineModeScenarioTests`) | – (Offline-Rückfall nur in Unit/Integration, Schritt 5) | Lücke |
| Isolierte Testdatenbank, Mock-Server statt produktiver Endpunkte | 1, 5, 7, 9, 17 | Abgedeckt |
| **Projektrahmen** | | |
| Lokale Git-Hooks (Format, verbotene Muster/Dateien, Commit-Nachrichten, lokale Tests) | 2 | Abgedeckt |
| CI/CD mit Branch-Modell main/staging, Pre-Releases, Releases, Versionierung ab 0.1 | 3 | Abgedeckt |
| Lokaler Ersatzlauf bei Billing-Limit | 3 | Abgedeckt |
| Abhängigkeitsprüfung auf Sicherheitslücken, Secrets nur über GitHub Secrets | 3 | Abgedeckt |
| PR-Pflicht für staging und main | 2, 3 | Abgedeckt |
| iOS-Build, Signierung, TestFlight/App Store auf Basis `iOS-Deployment.ps1` | 18 | Abgedeckt |
| Windows-Installer (MSIX oder Setup.exe) laut Phase 7 der Anforderung | – (Schritt 1 legt „ohne Installation startbar“ fest, Schritt 3 liefert die Anwendung als Release-Artefakt, ohne dokumentierte Entscheidung gegen einen Installer) | Lücke |
| Ablage startfähiger Windows-Zwischenstände unter `review-versions/`, benannt, datiert, Changelog | 1 (Verfahren, 0.1.0), 4, 6–17 | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden. Alle Abhängigkeiten zeigen auf kleinere Schrittnummern und existieren. |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Mit einer Ausnahme ja. Schritt 12 (Fahrzeugverwaltung) verlangt, dass die Löschrückfrage „auf die Möglichkeit einer vorherigen Sicherung hin[weist]“. Die Sicherungsfunktion entsteht aber erst in Schritt 15. Bei der Umsetzung von Schritt 12 gibt es sie also noch nicht. Alle anderen inhaltlichen Bezüge sind durch direkte oder transitive Abhängigkeiten gedeckt, z. B. 11 → 4 über 5/6, 15 → 4/5/10 über 11 und 14, 17 → 5/8 über 7 und 9, 18 → 6 über 17. |

## Fehlende oder unvollständige Punkte

- [ ] **Aktualisierung bei Wiederverbindung** (requirement.md 2.2 „Offline Mode – Synchronisation: Bei Wiederverbindung werden Daten aktualisiert“, 4.3 Offline-first mit Hintergrund-Synchronisation): Kein Schritt sieht vor, dass die App nach wiederhergestellter Verbindung die Preise selbst aktualisiert und den Offline-Hinweis entfernt. Geplant ist nur die Aktualisierung beim Öffnen und auf Nutzerwunsch (Schritte 8, 11). Der Punkt sollte in Schritt 5, 6 oder 11 ergänzt werden.
- [ ] **Sicherungsverlauf** (requirement.md 3.2 `IBackupService.GetBackupHistoryAsync`/`DeleteBackupAsync`, 3.4 `BackupSection (Create, Restore, History)`): Schritt 15 sieht nur Erstellen (über die Teilen-/Speichern-Funktion) und Wiederherstellen vor. Eine Übersicht vorhandener Sicherungen und das Löschen einzelner Sicherungen fehlen. Entweder in Schritt 15 aufnehmen oder als bewusste Abweichung in die Vorgehensentscheidungen eintragen, z. B. weil Sicherungen außerhalb der App abgelegt werden.
- [ ] **Feature-Flags** (requirement.md 5.4, konfigurierbare Feature-Flags mit `IFeatureFlagService`): Sie kommen im Plan nicht vor. `CloudSyncEnabled` und `ElectricityChargingEnabled` entfallen durch die Stakeholder-Entscheidungen. Für `RouteSearchEnabled`, `OfflineModeEnforced` und `E2ETestMode` fehlt aber eine Zuordnung zu einem Schritt oder eine dokumentierte Entscheidung dagegen.
- [ ] **E2E-Szenario Offline-Modus** (requirement.md 3.6 `OfflineModeScenarioTests`, 4.7 „Offline-Modus: Daten anzeigen, ohne dass API verfügbar ist“): Kein Schritt verlangt einen FlaUI-Test, der die Anzeige zwischengespeicherter Preise samt Offline-Hinweis bei nicht erreichbarer API prüft. Der Offline-Rückfall wird nur in Unit- und Integrationstests abgedeckt (Schritt 5). Passend wäre eine Ergänzung in Schritt 6, 8 oder 11.
- [ ] **Windows-Installer** (requirement.md 7, Phase 7 „Windows Installer (MSIX oder Setup.exe)“): Der Plan liefert die Windows-App ohne Installation und als Release-Artefakt aus, begründet aber nicht, warum der Installer entfällt. Entweder einen Schritt bzw. Schrittbestandteil ergänzen oder die Entscheidung in den Vorgehensentscheidungen festhalten.
- [ ] **Reihenfolge Schritt 12 und Schritt 15**: Der Hinweis auf eine vorherige Sicherung in der Löschrückfrage von Schritt 12 setzt die Funktion aus Schritt 15 voraus. Möglich sind: den Hinweis erst in Schritt 15 ergänzen, ihn in Schritt 12 so formulieren, dass er nicht von der App-Funktion abhängt, oder die Reihenfolge anpassen.

## Hinweise

- **Kleinere Punkte ohne Einfluss auf den Status (bei der Nachplanung mit prüfen):**
  - Optionale Gruppenbeschreibung (`FavoriteGroup.Description`, requirement.md 2.1/3.1): Schritt 10 nennt nur den Gruppennamen.
  - Standortmarkierung des Nutzers sowie Zoom/Verschieben der Karte (3.4 `MapView`): Schritt 9 nennt beides nicht ausdrücklich.
  - Optionaler `RouteButton` in der Detailansicht (3.4): nicht geplant. Er ist in der Anforderung als optional markiert.
  - Nach der Planregel „Zwischenstand nach jedem Schritt mit sichtbarer Änderung“ fehlt in Schritt 5 die Ablage eines Windows-Zwischenstands, obwohl dort die Quellenangabe im Bereich „Optionen“ sichtbar wird.
- Die Tabelle der Vorgehensentscheidungen eignet sich gut, um die Punkte 2, 3 und 5 der Lückenliste zu schließen: Falls sie bewusst nicht umgesetzt werden sollen, genügt jeweils eine dokumentierte Entscheidung mit Begründung.
- `inventory.md` beschreibt den Initialzustand (kein Code, Toolchain bereit, externe Vorgaben unter `inventory/external-sources/`). Der Plan verweist in den Schritten 2, 3 und 18 korrekt auf diese Vorlagen. Aus der Bestandsaufnahme ergeben sich keine weiteren Lücken.
