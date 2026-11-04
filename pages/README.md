# GitHub Pages – Tankatlas-Präsentation

Statische Single-Page-Präsentation der App (Issue #34). Wird bei jedem Merge in
`main` über `.github/workflows/pages.yml` auf GitHub Pages veröffentlicht.

## Inhalt

- `index.html` – Hero plus Info-Panels in der Reihenfolge des Nutzungsablaufs
  (Favoriten → Karte/Suche → Details → Tankbuch → Sicherung)
- `styles.css` – Design-System-Farben (siehe `docs/design-system.md`), fester
  Tankstellen-Hintergrund über `assets/bg-scene.svg`
- `app.js` – Einblendung der Panels beim Scrollen (IntersectionObserver,
  `prefers-reduced-motion` beachtet)
- `assets/appicon.svg` – Kopie des App-Icons (`src/Tankradar.MAUI/Resources/AppIcon/appiconfg.svg`)
- `assets/screenshots/*.png` – echte Screenshots der Windows-App im Testmodus

## Screenshots erzeugen

Die PNG-Dateien sind echte Aufnahmen der Windows-App und werden mit dem Werkzeug
`src/PagesScreenshots` erzeugt. Es startet die App im Testmodus (isoliertes
Datenverzeichnis, Mock-Dienste für Preise, Ortssuche und Routing, echte
OpenStreetMap-Kacheln), legt über die Oberfläche Favoriten und ein Fahrzeug an,
schreibt Tankbuch-Vorgänge in die Testdatenbank und fotografiert die Fenster
per `PrintWindow` — außer den Kartenkacheln wird kein produktiver Endpunkt angesprochen.

```powershell
dotnet run --project src/PagesScreenshots
```

Voraussetzung: ein Debug-Build der Windows-App
(`dotnet build -f net10.0-windows10.0.19041.0` in `src/Tankradar.MAUI`) und
Netzwerkzugriff auf `tile.openstreetmap.org` für die Kartenkacheln.
Optionales Argument: ein abweichendes Zielverzeichnis für die PNG-Dateien.

## Einmalige Einrichtung

Im Repository muss einmalig **Settings → Pages → Source: GitHub Actions**
aktiviert sein, sonst schlägt `actions/deploy-pages` fehl.
