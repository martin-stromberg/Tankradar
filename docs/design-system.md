# Design-System: Tankradar (E-Mobility & Fuel Navigator)

Dokumentiert auf Basis: `design-draft/stitch_smart_fuel_charge_tracker.zip`

## Überblick

Das Design-System definiert eine hochmoderne, zuverlässige und datenzentrierte Mobilitätsplattform für Kraftstoff- und Ladepreise mit dem Titel **"E-Mobility & Fuel Navigator"**. Das System vereint deutsche Automobil-Präzision mit Benutzerfreundlichkeit für mobile Konsumenten.

**Leitbild:** Corporate / Modern High-Precision mit Fokus auf:
- Klarheit & Verlässlichkeit (sofortige Orientierung an Tankstelle/Ladesäule)
- Datenpräzision (Lesbarkeit von Preisen, Ladeleistungen, Belegung)
- Technologische Eleganz (subtile Tiefenwirkung, Glassmorphismus)

## Farbpalette

### Primärfarben

| Rolle | Farbe | RGB/Hex | Zweck |
|-------|-------|---------|-------|
| **Primary** | Teal | `#005c55` / `#0F766E` | Kernmarke, Stabilität, Energieeffizienz, primäre Aktionsflächen, Navigation |
| **Secondary** | Emerald Mint | `#006c49` / `#10B981` | Bestpreise, Sparpotenzial, freie Ladepunkte, erfolgreiche Ladevorgänge |
| **Tertiary** | Amber | `#734700` / `#F59E0B` | Warnfunktion, mittlere Preisniveaus, belegte Ladeplätze |
| **Error** | Koralla-Rot | `#ba1a1a` / `#EF4444` | Hohe Kraftstoffpreise, defekte Ladesäulen, Fehler |

### Neutral-Palette

| Rolle | Farbe | Zweck |
|-------|-------|-------|
| **Background** | `#faf8ff` | App-Canvas-Hintergrund |
| **Surface / Container** | `#ffffff` | Karten, Listenelemente, Content-Container |
| **Borders & Dividers** | `#E2E8F0` | Trennlinien, Konturen |
| **Text (Primary)** | `#0F172A` / `#131b2e` | Fließtext, Überschriften |
| **Text (Secondary)** | `#64748B` / `#3e4947` | Sekundärbeschriftungen, Hinweis |

### Spezielle Farbzustände

- **Erfolg (Grün):** `#10B981` (z.B. günstigste Station, freie Ladesäulen)
- **Teal/Standard:** `#0F766E` (z.B. Standardpreisniveau)
- **Rot/Teuer:** `#EF4444` (z.B. hohes Preisniveau)
- **Grau/Inaktiv:** `#94A3B8` (z.B. geschlossene Stationen)

## Typografie

### Schriftarten

- **Hauptschrift:** `Inter` (Überschriften, Fließtext, Metriken)
- **Technische Daten:** `JetBrains Mono` (Steckerbezeichnungen, Ladeleistung, Zählerstände)

### Typographische Stile

| Style | Font | Größe | Gewicht | Zeilenhöhe | Buchstabenabstand | Einsatz |
|-------|------|-------|---------|-----------|-------------------|----|
| **headline-xl** | Inter | 32px | 700 | 40px | -0.02em | Seiten-Haupttitel |
| **headline-lg** | Inter | 24px | 700 | 32px | -0.015em | Abschnitts-Titel |
| **headline-md** | Inter | 20px | 600 | 28px | -0.01em | Unterabschnitts-Titel |
| **title-sm** | Inter | 16px | 600 | 24px | 0 | Kartentitel |
| **body-lg** | Inter | 16px | 400 | 24px | 0 | Normaler Text |
| **body-md** | Inter | 14px | 400 | 20px | 0 | Sekundärer Text |
| **body-sm** | Inter | 12px | 400 | 16px | 0 | Hinweistext, Footer |
| **price-hero** | Inter | 28px | 800 | 32px | -0.03em | Preise (prominent) |
| **price-unit** | Inter | 13px | 600 | 16px | 0 | Einheiten (€/L, ct/kWh) |
| **label-code** | JetBrains Mono | 12px | 500 | 16px | 0.02em | Technische Labels |
| **label-pill** | Inter | 12px | 600 | 16px | 0 | Badge-Labels |

## Layout & Spacing

### Responsive Grid

| Viewport | Breite | Spalten | Gutter | Einsatz |
|----------|--------|---------|--------|---------|
| **Mobile** | < 640px | 1 Spalte | 1rem (16px) | Smartphone, einspaltig |
| **Tablet** | 640px – 1024px | 2 Spalten | 1.5rem (24px) | Split-View, 50% Karte + Liste |
| **Desktop** | > 1024px | 3 Spalten | 2–3rem | Navigation + Daten + Karte |

### Spacing-Skala

| Token | Wert | Einsatz |
|-------|------|---------|
| **space-xs** | 0.25rem (4px) | Minimale Abstände |
| **space-sm** | 0.5rem (8px) | Small Gaps |
| **space-md** | 1rem (16px) | Standard Margin/Padding |
| **space-lg** | 1.5rem (24px) | Tablet Gutter |
| **space-xl** | 2.5rem (40px) | Large Sections |

### Touch Targets

- **Mindestgröße:** 44 × 44 px (iOS/Android-Standard)
- Alle anklickbaren Elemente müssen diese Größe erfüllen

## Elevation & Depth

### Schatten-Ebenen

| Ebene | Einsatz | Schatten | Beispiele |
|-------|---------|---------|-----------|
| **0 (Hintergrund)** | Basis | keine | Canvas (`#F8FAFC`) |
| **1 (Base)** | Standard | `0 1px 3px rgba(15,23,42,0.04)` | Kartenlisten, Formularelemente |
| **2 (Floating)** | Schwebend | `0 4px 12px rgba(15,23,42,0.08)` | FABs, Map Pins |
| **3 (Modal)** | Überlagert | `0 12px 32px rgba(15,23,42,0.16)` | Bottom Sheets, Modals |

### Speziale Effekte

- **Glassmorphismus:** Auf schwebenden Suchleisten und Floating Headers über Karten
  - `backdrop-filter: blur(12px)`
  - Hintergrund: `rgba(255, 255, 255, 0.88)`

## Border-Radius

| Token | Wert | Einsatz |
|-------|------|---------|
| **rounded-sm** | 0.25rem (4px) | Minimale Abrundung |
| **rounded** | 0.5rem (8px) | Standard (Buttons, Inputs) |
| **rounded-md** | 0.75rem (12px) | Leichte Abrundung |
| **rounded-lg** | 1rem (16px) | Container, Karten |
| **rounded-xl** | 1.5rem (24px) | Große Container |
| **rounded-full** | 9999px (Pill) | Badges, Toggles, Pills |

## Komponenten

### 1. Buttons & Schnellschalter

#### Primary Action Button
- **Hintergrund:** `#0F766E` (Primary Teal)
- **Text:** `#FFFFFF`
- **Border-Radius:** 8px
- **Font-Weight:** 600
- **Zustände:**
  - Normal: `#0F766E`
  - Hover: `#0D5D56` (dunkler)
  - Pressed: `#0A4D46` (noch dunkler)
  - Disabled: `#D2D9F4` (surface-dim)

#### Segmented Control / Fuel/EV Toggle
- **Kompaktes Dual-Segment zur Umschaltung zwischen Kraftstoff und Laden**
- **Hintergrund:** `#F1F5F9` (neutral)
- **Selektiertes Segment:** `#FFFFFF` mit Schatten (`0 2px 4px rgba(0,0,0,0.06)`)
- **Border-Radius:** pill (9999px)

### 2. Pill-Badges (Kraftstoff & Ladestecker)

#### Kraftstoffarten (Super E5, E10, Diesel)
- **Standard (unausgewählt):**
  - Hintergrund: `#F1F5F9`
  - Text: `#334155`
  - Border: `#E2E8F0`
- **Ausgewählt:**
  - Hintergrund: `#0F766E`
  - Text: `#FFFFFF`

#### Ladeinfrastruktur (HPC 300kW, DC 150kW, AC 22kW)
- **Format:** Blitz-Icon + Leistungsangabe in `JetBrains Mono`
- **HPC-Schnelllader (Akzent):**
  - Hintergrund: `#ECFDF5` (grün)
  - Rand: `#A7F3D0`
  - Text: `#065F46`

### 3. Stationskarten (Feed- & Detailkarten)

#### Aufbau
- **Linksteil:** Markenlogo (Aral, Shell, Ionity, EnBW), Distanzangabe
- **Rechtsteil:** Prominente Preisanzeige mit Trendindikator
- **Footer:** Belegungsampel oder Gültigkeitsdauer ("Vor 8 Min. aktualisiert")

#### Design-Details
- **Border-Radius:** 16px (lg)
- **Hintergrund:** `#FFFFFF`
- **Border:** `1px solid #E2E8F0`
- **Schatten:** `0 1px 3px rgba(15,23,42,0.04)` (Ebene 1)

### 4. Karten-Pins (Map Markers)

#### Struktur
- **Vertikal:** Nadel mit pillenförmigem Preiskopf
- **Farbcodierung nach Preisniveau:**
  - 🟢 **Grün (`#10B981`):** Günstigste Station im Umkreis
  - 🔷 **Teal (`#0F766E`):** Standardpreisniveau
  - 🔴 **Rot (`#EF4444`):** Hohes Preisniveau
  - ⚪ **Grau (`#94A3B8`):** Station geschlossen / Ladesäule außer Betrieb

### 5. Eingabefelder & Filter

#### Suchfeld
- **Größflächig angelegt**
- **Integriert:** GPS-Fokus-Button, Lupe, Schnellfilter für favorisierte Marken
- **Border-Radius:** 8px (rounded)
- **Fokus-Zustand:** Ring `2px solid #0F766E`

#### Tankbuch-Formulare
- **Input-Elemente mit Maßeinheiten-Suffixen:** `L`, `kWh`, `€`, `km`
- **Neutraler Zustand:** `#E2E8F0`
- **Fokus:** Ring `2px solid #0F766E`

## Verfügbare Screens (aus Design-Draft)

Die folgenden Screens sind im ZIP-File `stitch_smart_fuel_charge_tracker.zip` dokumentiert:

| Screen-Ordner | Beschreibung | Light Mode | Dark Mode |
|---------------|-------------|-----------|-----------|
| **e_mobility_fuel_navigator** | Logo & Branding Guide | ✓ | ✓ |
| **startseite_favoriten_gruppen** | HomePage mit Favorit-Gruppen | ✓ | ✓ |
| **suche_kartenansicht** | SearchPage mit Kartenansicht | ✓ | ✓ |
| **tankbuch_verbrauchsauswertung** | LogbookPage mit Statistiken & Charts | ✓ | ✓ |
| **tankstellen_details_favoriten** | StationDetailPage | ✓ | ✓ |
| **tank_lade_navigator_logo** | Logo Assets | ✓ | – |

Jeder Screen enthält:
- `screen.png` – Visueller Mockup
- `code.html` – HTML-Implementierungsdetails (optional)

## Dark Mode

Das Design-System unterstützt vollständig Dark Mode mit konsistenten Farbmappings:

- **Background (Dark):** `#131b2e` / `#0f172a`
- **Surface (Dark):** `#283044`
- **Text (Dark):** `#eef0ff` / `#f2f3ff`
- **Borders (Dark):** Hellere Konturen auf dunklem Hintergrund

Alle beschriebenen Farben und Komponenten haben Dark-Mode-Entsprechungen in der Design-Palette.

## Implementierungshinweise für MAUI

### XAML ResourceDictionary-Struktur

Das Design-System lässt sich in MAUI wie folgt abbilden:

```xaml
<ResourceDictionary>
  <!-- Colors -->
  <Color x:Key="ColorPrimary">#0F766E</Color>
  <Color x:Key="ColorSecondary">#10B981</Color>
  <Color x:Key="ColorTertiary">#F59E0B</Color>
  <Color x:Key="ColorError">#EF4444</Color>
  <Color x:Key="ColorSurface">#FFFFFF</Color>
  <Color x:Key="ColorBackground">#FAF8FF</Color>
  
  <!-- Typography -->
  <x:Double x:Key="FontSizeHeadlineXL">32</x:Double>
  <x:Double x:Key="FontSizeBodyMD">14</x:Double>
  
  <!-- Spacing -->
  <x:Double x:Key="SpacingSM">8</x:Double>
  <x:Double x:Key="SpacingMD">16</x:Double>
  <x:Double x:Key="SpacingLG">24</x:Double>
  
  <!-- Border Radius -->
  <CornerRadius x:Key="BorderRadiusDefault">8</CornerRadius>
  <CornerRadius x:Key="BorderRadiusLarge">16</CornerRadius>
</ResourceDictionary>
```

### Weitere Implementierungsüberlegungen

- **Font-Installation:** Inter und JetBrains Mono müssen als App-Ressourcen installiert werden (iOS: Font.otf in Info.plist, Windows: Font-Registry)
- **Elevation/Shadows:** MAUI `Shadow` Element mit entsprechenden Blur-, Offset- und Color-Properties
- **Touch Target Validation:** Sicherstellen, dass Buttons und interaktive Elemente mind. 44×44 px sind
- **Responsive Behavior:** Conditional XAML oder StackLayout-Orientierungswechsel bei Viewport-Größen-Änderungen

## Gestalt-Prinzipien

Das Design folgt diesen visuellen Prinzipien:

1. **Klare Hierarchie:** Großes Preis-Hero an erster Stelle, gefolgt von Nebeninformationen
2. **Scanbarkeit:** Oben: Sucheingabe, Mitte: Ergebnisse/Karte, Unten: FAB oder Action-Bar
3. **Konsistente Abstände:** Alle Margins/Paddings folgen der 4px/8px-Skala
4. **Funktionale Farbcodierung:** Farbe signalisiert Funktion (rot=teuer, grün=günstig)
5. **Minimal aber informativ:** Keine unnötigen Dekoration, aber ausreichend Whitespace

---

**Designentwurf dokumentiert:** 2026-09-28  
**Quelle:** `design-draft/stitch_smart_fuel_charge_tracker.zip`  
**Status:** Verbindliche Grundlage für MAUI UI-Implementierung
