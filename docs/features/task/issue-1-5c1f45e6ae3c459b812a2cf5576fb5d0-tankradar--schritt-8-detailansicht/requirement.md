# Anforderung (Schritt 8: Tankstellen-Detailansicht)

Quelle: `docs/projects/.../project-plan.md`, Abschnitt „Schritt 8“, plus Zusatz des Projektleiters.

1. Detailansicht aus der Ergebnisliste gemäß Designentwurf: Name, Adresse, Entfernung, Preise der aktivierten Sorten mit „vor X Min.“ und Amber ab 60 Minuten, Hinweise („Preis unbestätigt“, „Automatentankstelle“), Öffnungszeiten, Zahlungsmöglichkeiten nur falls geliefert (Tankerkönig liefert keine). Fehlende Angaben weglassen. Keine Strom-/Ladeinformationen.
2. Offline: zuletzt bekannte Daten mit Alter und Offline-Hinweis in der Kopfzeile; mit Verbindung Aktualisierung; Wiederverbindung bei geöffneter Ansicht aktualisiert veraltete Preise automatisch und entfernt den Offline-Hinweis.
3. Tests: Aufbereitung (inkl. ausgeblendeter Angaben), automatische Aktualisierung, FlaUI-Öffnen aus der Liste. Windows-Zwischenstand 0.1.11 unter `review-versions/`.
4. Zusatz 1: Altersgrenze für Detailangaben (Öffnungszeiten/Automatentankstelle) in Detailansicht und Suchergebnissen.
5. Zusatz 2: Adresseingabe akzeptiert typografische Zeichen der iOS-Tastatur (Normalisierung, Tests „Up’n Kamp“).
6. Zusatz 3: Build-Schlüssel zusätzlich aus `FUEL_PRICE_API_KEY` (Reihenfolge TANKRADAR_FUEL_PRICE_API_KEY, FUEL_PRICE_API_KEY, local.props); Doku.
7. Zusatz 4: `docs/help/ci-cd/workflows.md` hält CI-Vordergrundbetrieb fest.
8. Zusatz 5 (CI-Fehler PR #5): Nominatim-Drosselung mit Sicherheitsabstand (mind. 1100 ms, monotone Uhr), E2E prüft Abstand am Mock >= 1000 ms, Unit-Test.
