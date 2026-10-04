# Anforderung: Anbindung der Kraftstoffpreis-API und Preis-Cache (Schritt 5)

## Ziel
Aktuelle Kraftstoffpreise und Tankstellendaten über die Tankerkönig-API abrufen (Umkreissuche nach Spritsorten, Details einer Tankstelle), lokal mit Zeitstempel speichern und offline weiter bereitstellen.

## Funktionale Anforderungen
1. Abruf: Tankstellen im Umkreis einer Position (gefiltert nach Spritsorten) und Details einer einzelnen Tankstelle (Name, Adresse, Preise je Sorte, Öffnungszeiten, weitere Angaben der Quelle).
2. Nicht gelieferte Angaben (z. B. Zahlungsmöglichkeiten) werden nicht erfunden. „Automatentankstelle“ wird, wo möglich, aus durchgehenden Öffnungszeiten abgeleitet.
3. Nur HTTPS, Zeitlimit, Wiederholung mit zunehmender Wartezeit, Drosselung gemäß Nutzungsbedingungen, lokaler Cache statt erneutem Abruf.
4. API-Schlüssel nie im Quellcode: Build aus Secret bzw. nicht versionierter lokaler Konfiguration, zur Laufzeit in Keychain (iOS) bzw. Credential Locker (Windows).
5. Quellenangabe „Daten: Tankerkönig / MTS-K“ (CC BY 4.0) sichtbar im Bereich „Optionen“.
6. Jeder abgerufene Preis wird mit Zeitstempel gespeichert; ältere Stände bleiben erhalten (bestehende Installationen behalten ihre Daten).
7. Aktualität ausschließlich aus dem Zeitstempel: ab 60 Minuten veraltet; daraus folgt „Preis unbestätigt“.
8. Offline: zuletzt bekannte Preise samt Alter statt eines Fehlers. Erkennung von Netzverbindung und deren Wiederherstellung.
9. Fehlerzustände führen nicht zu unsicherem Verhalten.
10. Mock-Server für Integrations- und E2E-Tests; E2E per Testkonfiguration mit unerreichbarem Dienst startbar. Keine Tests gegen produktive Endpunkte.
11. Tests: Abruf, Wiederholung, Zeitlimit, Speicherung mit Zeitstempel, Aktualität (Grenze 60 Min.), abgeleitete Hinweise, Offline-Rückfall, Wiederverbindung.
12. Windows-Zwischenstand 0.1.5 unter `review-versions/` (nicht committen). Dokumentation, wie der Anwender den Schlüssel lokal bzw. als GitHub-Secret hinterlegt.

## Zusatz des Projektleiters
Der FlaUI-Test `SettingsE2ETests_Defaults.FirstStart_ShowsDefaultValues` scheiterte auf dem GitHub-Windows-Runner einmal mit `System.TimeoutException: UIA Timeout` (COMException 0x80131505) beim Aufbau der UI-Automation. Die E2E-Testbasis soll den Aufbau der UI-Automation und das erste Abfragen des Hauptfensters gegen solche Timeouts robust machen (begrenzte Wiederholung mit klarer Meldung), ohne echte Fehler zu verdecken.

## Abhängigkeit
Schritt 4 (Datenhaltung, Einstellungen).
