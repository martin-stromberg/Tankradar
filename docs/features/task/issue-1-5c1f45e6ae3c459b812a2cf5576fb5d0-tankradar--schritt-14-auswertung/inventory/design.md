# Designentwurf: Statistik-Darstellung (nur gelesen)

## Fundort
- Ordner `design-draft/` im Repository-Root enthält nur das Archiv `stitch_smart_fuel_charge_tracker.zip` (nicht entpackt im Repository). Zum Lesen wurde es in das Scratchpad entpackt (`.../scratchpad/dd/stitch_smart_fuel_charge_tracker/`), im Repository wurde nichts abgelegt.
- Inhalt des Archivs: `e_mobility_fuel_navigator/DESIGN.md` (Design-System hell), `e_mobility_fuel_navigator_dark_edition/DESIGN.md` (dunkel), je Bildschirm `code.html` + `screen.png` in den Ordnern `startseite_favoriten_gruppen`, `suche_kartenansicht`, `tankstellen_details_favoriten`, `tankbuch_verbrauchsauswertung`, jeweils auch `_dark_mode`, sowie `tank_lade_navigator_logo`.
- Relevant: `tankbuch_verbrauchsauswertung/code.html` (hell) und `tankbuch_verbrauchsauswertung_dark_mode/code.html`. **Alle `screen.png`-Dateien sind keine Bilder**, sondern enthalten den Text `<FIFE Image failed to fetch>` (nicht lesbar). Als Quelle dient daher nur das HTML (Tailwind, Beispieldaten „Mai 2024“, VW Golf 2.0 TDI und Tesla Model 3).

## Aufbau des Bildschirms „Tankbuch – Verbrauchsauswertung“
1. **Fahrzeugkarte „Aktives Fahrzeug“** mit Auswahlliste (Dropdown, Kraftstoffart und Kennzeichen), darunter eine Zeile mit Gesamt-Kilometerstand („84.350 km“) und **Gesamt-Ø** („Gesamt-Ø: 5,4 L/100km“).
2. **Schnellaktion** „+ Neuen Tankvorgang erfassen“ (Vollbreite-Schaltfläche, 48 px hoch).
3. **Statistik-Abschnitt „Monatsanalyse Mai 2024“** mit Plakette „Effizient“ (grünes Pill-Label) und:
   - **Kachel „Ø Verbrauch“** (zwei Kacheln im 2-Spalten-Raster): großer Wert „5,42“ (`price-hero`, 28 px/800), Einheit „L/100km“ (`price-unit`), Symbol Tankstelle; darunter Trendzeile mit Pfeil („trending_down“, „-0,3 L ggü. Vormonat“, grün).
   - **Kachel „Kosten / 100 km“**: „8,83 €“; Trendzeile neutral („horizontal_rule“, „Stabil (-0,02 €)“).
   - **Karte „Gesamtkosten Mai“**: „164,20 €“ (`price-hero`) mit Plakette „98,50 L getankt“, Symbol Kalender.
   - **Mini-Verlaufsdiagramm** „Verlauf (L/100km letzte 6 Betankungen)“ in der Karte: Inline-SVG (Viewbox 300x70, `preserveAspectRatio="none"`), **6 Balken** (22 px breit, Radius 3, je Balken Wert oberhalb: 5.7, 5.6, 5.5, 5.8, 5.45, 5.41), gestrichelte **Referenzlinie** (Benchmark, Strichmuster 3 3, Farbe `#bdc9c6`) und Beschriftung „Ziel: 5,50 L“ in Primärfarbe; Hervorhebung: Balken 1 bis 4 `#dae2fd` (surface-container-highest), vorletzter Balken `#6cf8bb` (secondary-container) mit fettem grünem Wert, letzter Balken `#005c55` (primary) mit fettem Wert; x-Beschriftung „Mär / Apr / 03. Mai / 18. Mai“ (`label-code`, 10 px). Keine y-Achse, keine Gitterlinien, Werte direkt am Balken (Punkt als Dezimaltrennzeichen im SVG, im restlichen Text Komma).
4. **„Letzte Tankvorgänge“** (Eintragskarten mit Tankstelle, Datum, Gesamtbetrag, Preis pro Einheit, Detailzeile Menge / Distanz / „Berechnet“ Verbrauch, Notiz, „Beleg“-Marke); „Alle 42 anzeigen“. Diese Karten sind in Schritt 13 umgesetzt (ADR 0009).
5. **Export** („CSV-Export“, „PDF-Bericht“; Schritt 16) und **Offline-/Sync-Banner** („Offline erfasst • Wird automatisch synchronisiert“; laut ADR 0009 entfällt, keine Cloud-Synchronisation in Version 1.0).

## Kennzahlen im Entwurf
| Kennzahl | Darstellung | Hinweis |
|----------|-------------|---------|
| Gesamt-Ø Verbrauch | Textzeile in der Fahrzeugkarte | ein Wert über alle Vorgänge |
| Ø Verbrauch (Monat) | Kachel, 2 Nachkommastellen, Einheit L/100km | mit Vergleich zum Vormonat in Litern („-0,3 L“) |
| Kosten / 100 km | Kachel, 2 Nachkommastellen, Euro | Trend als Differenz in Euro („-0,02 €“), Bezug im Entwurf nur der Monat |
| Gesamtkosten Monat | große Zahl + Plakette mit getankter Menge im Monat | Monat im Titel („Gesamtkosten Mai“) |
| Verlauf Verbrauch | 6 Balken, letzte 6 Betankungen | Datumsmarken nur an vier Stellen |

## Schwellen und Hinweise im Entwurf
- Plakette „Effizient“ neben der Monatsüberschrift; im Entwurf keine Regel angegeben (der Monatswert 5,42 liegt unter der Zielmarke 5,50).
- Referenz „Ziel: 5,50 L“ als gestrichelte Linie im Diagramm; ein Zielwert ist im aktuellen Datenmodell nicht vorhanden (weder in `Vehicle` noch in den Einstellungen).
- Trendzeilen: Richtung per Symbol (sinkend = grün, gleichbleibend = neutral); Schwelle für „Stabil“ nicht definiert (Beispiel -0,02 €).
- **Nicht im Entwurf:** Zustand mit zu wenigen Vorgängen, Startverbrauch-Fallback, Hybrid (getrennte Werte für Kraftstoff und Strom), Strom-Einheit kWh, Monate ohne Vorgänge, leeres Diagramm, Fehler- oder Ladezustand, Barrierefreiheits-Texte für das Diagramm, Dark-Mode-spezifische Diagrammfarben außer den unten genannten.

## Farben und Typografie (aus `DESIGN.md` und `code.html`)
- Hell: `primary #005c55`, `primary-container #0f766e`, `secondary #006c49`, `secondary-container #6cf8bb`, `surface #faf8ff`, `surface-container-lowest #ffffff`, `surface-container-low #f2f3ff`, `surface-container-highest #dae2fd`, `on-surface #131b2e`, `on-surface-variant #3e4947`, `outline-variant #bdc9c6`, `tertiary #734700`, `error #ba1a1a`.
- Dunkel (`..._dark_mode/code.html`, im SVG): Balkenfarben `#21262d` (Standard), Hervorhebungen `#10b981` und `#04b4a2`, Referenzlinie `#30363d`; Textinhalt sonst identisch (Monatsanalyse Mai 2024, Effizient, Ziel: 5,50 L).
- Schriften: Inter (Titel/Text/Zahlen), JetBrains Mono (`label-code`); `price-hero` 28/32 px, 800; `price-unit` 13 px, 600; `label-code` 12 px; `title-sm` 16 px, 600; `label-pill` 12 px, 600. Radien: Karten `xl` (12 px im Entwurf, in der App `rounded-lg` 16), Abstände `space-xs..xl` (4/8/16/24/40 px) entsprechen `DesignSystem.xaml`.
- Symbole: Material Symbols (Web-Schrift); in der App sind keine Material-Symbols eingebunden (vgl. `ui.md`).

## Abweichungen, die bereits entschieden sind (ADR 0009)
Keine Karte „Aktives Fahrzeug“ mit Auswahlliste; Fahrzeug wird über die Fahrzeugkarte gewählt; Monatsanalyse, Verlauf und Statistik sind für Schritt 14 vorgesehen; Export folgt in Schritt 16; Sync-Banner entfällt; „Alle 42 anzeigen“ entfällt.
