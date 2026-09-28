# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

Grundlage: `requirement.md` (Prüfbasis), `issue.md` (Originalanforderung), `inventory.md` und
`project-plan.md` (Stand 2026-09-28, nach Einarbeitung der Befunde aus `project-plan-check.1.md`).
Folgende Entscheidungen sind im Plan dokumentiert und gelten als bewusste Abweichungen, nicht als
Lücken:

- Stakeholder-Entscheidungen: keine Strompreise, Ladestationen, Strom-Einstellungen und
  Strom-Filter in 1.0; Export und Route gehören zu 1.0; keine Cloud-Synchronisation vor 1.0;
  Windows-Zwischenstände unter `review-versions/`, nicht versioniert
- Entscheidungen des Projektleiters: keine Feature-Flags, kein Windows-Installer
- weitere Vorgehensentscheidungen: Tankerkönig, Nominatim, OSM-Karte, OpenRouteService, iOS 16,
  Data Protection statt DB-Verschlüsselung, Preisalter-Grenze 60 Minuten, Preisniveau-Farben

Alle sechs Befunde der vorherigen Prüfung sind geschlossen. Die erneute vollständige Prüfung ergab
keine neuen Lücken und keine Reihenfolgefehler.

**Status der Befunde aus `project-plan-check.1.md`:**

| Vorheriger Befund | Jetzt | Fundstelle im Plan |
|-------------------|-------|--------------------|
| Aktualisierung bei Wiederverbindung | Geschlossen | Schritt 5 (Verbindungserkennung), 8 und 11 (automatische Aktualisierung, Offline-Hinweis wird entfernt, Tests) |
| Sicherungsverlauf (Liste, Löschen) | Geschlossen | Schritt 15 (Liste mit Datum und Größe, Wiederherstellung aus der Liste, Löschen nach Rückfrage, Tests und FlaUI) |
| Feature-Flags | Geschlossen durch Entscheidung | Vorgehensentscheidung „Feature-Flags“ (Route fest in 1.0, Offline aus echter Verbindung, E2E-Modus über Test-Konfiguration) |
| E2E-Szenario Offline-Modus | Geschlossen | Schritt 5 (Test-Konfiguration mit nicht erreichbarem Mock), Schritt 13 (FlaUI-Offline-Szenario für Startseite, Detailansicht und Tankbuch) |
| Windows-Installer | Geschlossen durch Entscheidung | Vorgehensentscheidung „Windows-Auslieferung“, Schritt 3 (gezipptes Build, kein Installer) |
| Reihenfolge Schritt 12 und 15 (Sicherungshinweis) | Geschlossen | Schritt 12 ohne Sicherungshinweis; Schritt 15 ergänzt den Hinweis in der Löschrückfrage |

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| **Plattform und Architektur** | | |
| .NET MAUI, eine Codebasis für iOS (primär, iOS 16) und Windows (Entwicklung, Debugging, Tests) | 1 | Abgedeckt |
| MVVM-Struktur (XAML-Views, ViewModels mit Commands/Bindings, Services per DI) | 1 (Grundgerüst), 4–17 (fachlich) | Abgedeckt |
| Eine einzige SQLite-Datenbank für Nutzer- und Abrufdaten | 4 (Vorgehensentscheidung „Datenhaltung“) | Abgedeckt |
| Datenbank-Initialisierung, Migrationen ohne Datenverlust, Standardwerte beim ersten Start | 4 | Abgedeckt |
| Designentwurf verbindlich (Navigation, Farben, Typografie, Hell/Dunkel), Abweichungen dokumentiert | 1, fortlaufend 4–17 | Abgedeckt |
| Feature-Flags (requirement.md 5.4) | – | Bewusst ausgeschlossen (Entscheidung Projektleiter) |
| **Tankstellen und Preise** | | |
| Station mit Name, Adresse, Koordinaten, Entfernung, Öffnungszeiten, Zahlungsmöglichkeiten (soweit geliefert) | 5, 8 | Abgedeckt |
| Provisorien („Preis unbestätigt“, „Automatentankstelle“) | 5 (Ableitung), 6, 8, 11 (Anzeige) | Abgedeckt |
| Offizielle Kraftstoffpreis-API, HTTPS, Zeitlimit, Retry mit Backoff, Drosselung | 5 | Abgedeckt |
| Strompreis-API, Ladestationen, AC/DC/HPC | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| Preis-Cache mit Zeitstempel, historische Preisstände | 5 | Abgedeckt |
| Aktualität ausschließlich über Zeitstempel, Anzeige „veraltet“ | 5, 6, 8, 11 | Abgedeckt |
| API-Schlüssel nie im Code, Keychain bzw. Credential Locker | 5, 17 | Abgedeckt |
| **Suche** | | |
| Umkreissuche am Standort, Radius 5–50 km, Filter nach Spritsorten | 6 | Abgedeckt |
| Suche nach Adresse/Ort/PLZ mit Radius | 7 | Abgedeckt |
| Ergebnisliste sortierbar nach Preis, Entfernung, Name; Preise, Entfernung, Provisorien | 6, 7 | Abgedeckt |
| Kartenansicht mit Pins farbcodiert nach Preisniveau, Tippen öffnet Details | 9 | Abgedeckt |
| Kartenfunktionen: eigener Standort, Zoom und Verschieben | 9 | Abgedeckt |
| Umschalten Liste/Karte, Voreinstellung aus Einstellungen | 9 | Abgedeckt |
| Tankstellen entlang einer Route | 17 | Abgedeckt |
| Validierung von Adresse, Radius und Filter vor dem API-Aufruf | 6, 7, 17 | Abgedeckt |
| **Tankstellen-Details** | | |
| Vollständige Detailansicht (Name, Adresse, Entfernung, Preise, Provisorien, Öffnungszeiten, Zahlungsmöglichkeiten) | 8 | Abgedeckt |
| „Zu Favoriten hinzufügen“ mit Auswahl einer bestehenden oder neuen Gruppe; „Aus Favoriten entfernen“ | 10 | Abgedeckt |
| **Favoriten und Gruppen** | | |
| Gruppen erstellen, umbenennen, löschen; optionale Beschreibung | 10 | Abgedeckt |
| Zuordnung ausschließlich über die Detailansicht; Mehrfachzuordnung | 10 | Abgedeckt |
| Gruppenansicht mit optionalen Notizen und Prioritäten | 10 | Abgedeckt |
| Cloud-Synchronisation | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| **Startseite** | | |
| Favoritengruppen mit Tankstellen: Name, Entfernung, Preise der gewählten Sorten, Provisorien | 11 | Abgedeckt |
| Bereich „In der Nähe“, optional aktivierbar, filterbar, nur zur Information | 11 | Abgedeckt |
| Offline-first: zuerst lokale Daten, dann Aktualisierung | 5, 11 | Abgedeckt |
| **Tankbuch** | | |
| Mehrere Fahrzeuge (Name, Kraftstoffart, Start-Kilometerstand, optionaler Startverbrauch) anlegen, bearbeiten, löschen | 12 | Abgedeckt |
| Tank- und Ladevorgang: Datum, Uhrzeit, Fahrzeug, Tankstelle (Liste oder manuell), Liter/kWh, Preis pro Einheit, Gesamtbetrag, Kilometerstand, Notiz, Belegfoto | 13 | Abgedeckt |
| Vorgänge löschen | 13 | Abgedeckt |
| Verbrauch pro 100 km = (Menge / Strecke) × 100 | 13 | Abgedeckt |
| Durchschnittsverbrauch, Kosten pro 100 km, Monatskosten, Verlaufsgrafiken | 14 | Abgedeckt |
| Export als CSV oder PDF | 16 | Abgedeckt |
| Tankbuch vollständig offline nutzbar | 12, 13, 14 | Abgedeckt |
| **Einstellungen** | | |
| Spritsorten: Auswahl und Reihenfolge | 4 | Abgedeckt |
| Strom: Aktivierung, Auswahl AC/DC/HPC | – | Bewusst ausgeschlossen (Stakeholder-Entscheidung) |
| GPS-Nutzung (Immer / Nur bei Nutzung / Nie) | 4, angewendet in 6, 9, 11, 17 | Abgedeckt |
| Standardansicht (Liste/Karte), Standardsortierung | 4, angewendet in 6, 9 | Abgedeckt |
| Lokale Sicherung erstellen und wiederherstellen | 15 | Abgedeckt |
| Sicherungsverlauf: Liste und Löschen einzelner Sicherungen | 15 | Abgedeckt |
| **Offline-Modus** | | |
| Letzte bekannte Preise zwischenspeichern, Offline-Hinweis | 5, 6, 8, 11 | Abgedeckt |
| Aktualisierung bei Wiederverbindung | 5 (Verbindungserkennung), 8, 11 | Abgedeckt |
| **Backup und Restore (4.6)** | | |
| Sicherung enthält alle Nutzerdaten und alle Preisstände mit Zeitstempel | 15 | Abgedeckt |
| Nach Wiederherstellung veraltete Preise erkennen, bei Bedarf aktualisieren, historische Daten erhalten | 15 | Abgedeckt |
| **Sicherheit und Datenschutz** | | |
| Nur HTTPS | 5, 7, 17 | Abgedeckt |
| Verschlüsselte lokale Speicherung sensibler Daten | 5, 17 (Zugangsdaten), 4, 13, 18 (Data Protection gemäß Entscheidung) | Abgedeckt |
| Standort nur bei Bedarf, keine GPS-Rohdaten gespeichert, iOS-Dialog mit klarem Zweck | 6, 9, 11, 17, 18 | Abgedeckt |
| Keine Weitergabe von Fahrzeug- und Tankbuchdaten an Dritte | 12, 13, 15, 16, 18 | Abgedeckt |
| Least Privilege, Secure by Default, Fail Secure | 4, 5, 15, 18 | Abgedeckt |
| **Konfiguration** | | |
| AppSettings-Persistenz über Einstellungsdienst und Einstellungsseite | 4, 11 (Schalter „In der Nähe“) | Abgedeckt |
| API-Schlüssel und Secrets über nicht versionierte Konfiguration bzw. GitHub Secrets | 3, 5, 17, 18 | Abgedeckt |
| CI-Variablen für Apple-Signierung und Zertifikate | 3, 18 | Abgedeckt |
| **Tests** | | |
| Unit-, Integrations- und FlaUI-E2E-Testprojekte | 1, danach in jedem Schritt | Abgedeckt |
| E2E: Suche, Favoriten, Preisabruf, Tankbuch-Einträge, Verbrauchsberechnung, Statistik, Export, Backup/Restore | 6, 7, 8, 10, 11, 13, 14, 15, 16 | Abgedeckt |
| E2E-Szenario Offline-Modus | 5 (Test-Konfiguration), 13 | Abgedeckt |
| Isolierte Testdatenbank, Mock-Server statt produktiver Endpunkte | 1, 5, 7, 9, 17 | Abgedeckt |
| **Projektrahmen** | | |
| Lokale Git-Hooks (Format, verbotene Muster/Dateien, Commit-Nachrichten, lokale Tests) | 2 | Abgedeckt |
| CI/CD mit Branch-Modell main/staging, Pre-Releases, Releases, Versionierung ab 0.1 | 3 | Abgedeckt |
| Lokaler Ersatzlauf bei Billing-Limit | 3 | Abgedeckt |
| Prüfung der Abhängigkeiten auf Sicherheitslücken, Secrets nur über GitHub Secrets | 3 | Abgedeckt |
| PR-Pflicht für staging und main | 2, 3 | Abgedeckt |
| iOS-Build, Signierung, TestFlight/App Store auf Basis von `iOS-Deployment.ps1` | 18 | Abgedeckt |
| Windows-Installer (MSIX oder Setup.exe) | – | Bewusst ausgeschlossen (Entscheidung Projektleiter, Schritt 3) |
| Startfähige Windows-Zwischenstände unter `review-versions/`, benannt, datiert, mit Changelog | 1 (Verfahren, 0.1.0), 4–17 | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden. Alle Abhängigkeiten zeigen auf existierende Schritte mit kleinerer Nummer. |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Ja. Alle inhaltlichen Bezüge sind direkt oder transitiv abgedeckt oder liegen in der Nummernfolge vorher, zum Beispiel: 13 → 8 (Offline-Szenario Detailansicht) über 10; 15 → 12 (Löschrückfrage Fahrzeug) über 14 → 13; 15 → 10 (Favoriten in der Sicherung) über 11; 17 → 4 und 5 über 7 → 6; 18 → 6 (Standortdialog) über 17. Der Sicherungshinweis für die Fahrzeug-Löschrückfrage steht jetzt erst in Schritt 15. |

## Fehlende oder unvollständige Punkte

Keine.

## Hinweise

- **Schritt 9 und Adresssuche:** Schritt 9 zeigt auch die Ergebnisse der Adresssuche auf der Karte und
  markiert die gesuchte Position. Als Abhängigkeiten nennt er aber nur 6 und 8, nicht 7. Bei
  sequenzieller Abarbeitung ist das unproblematisch, weil Schritt 7 vorher umgesetzt wird. Nur bei
  paralleler oder umgestellter Abarbeitung wäre die Adresssuche in Schritt 9 noch nicht vorhanden.
  Optional kann Schritt 7 als Abhängigkeit von Schritt 9 nachgetragen werden. Auf den Status hat das
  keinen Einfluss.
- **`RouteButton` in der Detailansicht** (requirement.md 3.4, als optional markiert): Er ist weiterhin
  nicht geplant. Die Routensuche steht in Schritt 17 zur Verfügung. Das ist keine Lücke.
- `inventory.md` nennt in der Phasierung noch einen Windows-Installer. Das ist der Stand der
  Übersetzung vor der Entscheidung des Projektleiters und kein Widerspruch zum Plan.
- `.gitignore` enthält den Eintrag `review-versions/`, wie im Plan vorausgesetzt.
