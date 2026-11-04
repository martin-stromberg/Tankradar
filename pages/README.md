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
- `assets/screenshots/*.svg` – stilisierte Platzhalter der App-Ansichten

## Screenshots austauschen

Die Dateien in `assets/screenshots/` sind gestaltete Platzhalter, solange keine
echten App-Screenshots vorliegen. Für echte Screenshots (oder ein GIF, wenn
mehrere Ansichten zum Verständnis nötig sind):

1. Datei unter demselben Namen als `.svg`, `.png` oder `.webp` in
   `assets/screenshots/` ablegen.
2. Bei anderem Namen oder Format das `src`-Attribut des `<img>` in `index.html`
   anpassen und `alt`-Text beibehalten.

Empfehlung: Hochformat im Verhältnis 1:2 (z. B. 360×720), wie die Platzhalter.

## Einmalige Einrichtung

Im Repository muss einmalig **Settings → Pages → Source: GitHub Actions**
aktiviert sein, sonst schlägt `actions/deploy-pages` fehl.
