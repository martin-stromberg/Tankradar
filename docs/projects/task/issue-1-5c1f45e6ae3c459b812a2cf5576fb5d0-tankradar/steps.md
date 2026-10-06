# Entwicklungsschritte – Projekt Tankradar

Branch: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar`

Hinweis: Schritt-Branches verwenden das Trennzeichen `--` statt `/`, da Git keinen Branch
`task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/…` anlegen kann, solange der Basisbranch `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar` existiert (Ref-Pfadkonflikt).

| # | Titel | Abhängigkeiten | Branch | Status |
|---|-------|-----------------|--------|--------|
| 1 | App-Grundgerüst, Navigation und Design-System | Keine | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-1-grundgeruest` | Fertig |
| 2 | Lokale Git-Hooks zur Qualitätssicherung | 1 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks` | Fertig |
| 3 | CI/CD-Pipeline mit Pre-Releases und Releases | 1, 2 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-3-ci-cd` | Fertig |
| 3a | App-Anzeigename „Tankatlas“ | 1, 3 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-3a-anzeigename-tankatlas` | Fertig |
| 4 | Lokale Datenhaltung und Einstellungen | 1 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-4-datenhaltung` | Fertig |
| 5 | Anbindung der Kraftstoffpreis-API und Preis-Cache | 4 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-5-preis-api` | Fertig |
| 6 | Umkreissuche nach aktuellem Standort mit Ergebnisliste | 4, 5 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-6-umkreissuche` | Fertig |
| 6a | Oberflächentests als Auslieferungs-Gate | 3, 6 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-6a-e2e-gate` | Fertig |
| 7 | Suche nach Adresse, Ort oder PLZ | 6 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-7-adresssuche` | Fertig |
| 8 | Tankstellen-Detailansicht | 6 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-8-detailansicht` | Fertig |
| 9 | Kartenansicht der Suchergebnisse | 6, 7, 8 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-9-kartenansicht` | In Arbeit |
| 10 | Favoritengruppen und Zuordnung von Tankstellen | 8 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-10-favoriten` | Offen |
| 11 | Startseite mit Favoritengruppen und Bereich „In der Nähe" | 5, 6, 10 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-11-startseite` | Offen |
| 12 | Fahrzeugverwaltung im Tankbuch | 4 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-12-fahrzeuge` | Offen |
| 13 | Tank- und Ladevorgänge erfassen | 10, 11, 12 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-13-tankvorgaenge` | Offen |
| 14 | Verbrauchs- und Kostenauswertung | 13 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-14-auswertung` | Offen |
| 15 | Lokale Sicherung und Wiederherstellung | 11, 14 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15-sicherung` | Offen |
| 16 | Export des Tankbuchs als CSV oder PDF | 14 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-16-export` | Offen |
| 17 | Tankstellen entlang einer Route | 7, 9 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-17-route` | Offen |
| 18 | iOS-Build, Signierung und TestFlight-Auslieferung | 3, 13, 17 | `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-18-ios-auslieferung` | Offen |
