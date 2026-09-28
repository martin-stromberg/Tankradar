# UI-Ressourcen und Design-System (Ist-Zustand)

Dieser Bereich passt nicht in die Kategorien Datenmodell/Logik/Enum/Interface und wird deshalb
gesondert dokumentiert, da er den Großteil der in der Anforderung genannten Dateien betrifft.

## `src/Tankradar.MAUI/AppShell.xaml`

Wurzelelement `<Shell>` (Zeilen 2–8) setzt aktuell **nur**:
- `x:Class`, XML-Namespaces, `Title="Tankradar"`, `FlyoutBehavior="Disabled"`.

Es sind **keine** Shell-Farbattribute gesetzt (kein `Shell.TabBarBackgroundColor`,
`Shell.TabBarForegroundColor`, `Shell.TabBarTitleColor`, `Shell.TabBarUnselectedColor`,
`Shell.TabBarDisabledColor` o. ä. auf dem `<Shell>`-Root oder anderswo in der Datei). Die
`<TabBar>` enthält vier `<ShellContent>`-Einträge (Favoriten/`favorites`, Karte/`map`,
Tankbuch/`tankbook`, Optionen/`settings`), jeweils mit `Title`, `Icon` (`icon_favorites`,
`icon_map`, `icon_tankbook`, `icon_settings`) und `ContentTemplate`. Ohne explizite Farbattribute
zeigt die Shell den Plattform-Standard-Look (Windows: Standard-NavigationView-Farben).

`src/Tankradar.MAUI/AppShell.xaml.cs`: `AppShell()`-Konstruktor abonniert `Navigated += OnNavigated`,
das bei Navigation `Title = currentPage.Title` setzt. Kein Bezug zu Farben/Styling.

## `src/Tankradar.MAUI/Resources/DesignSystem.xaml`

Vollständige Farb-Token (Zeilen 6–24), unverändert relevant für die Anforderung:

| Token | Wert |
|-------|------|
| `ColorPrimaryTeal` | `#0F766E` |
| `ColorSecondaryEmerald` | `#10B981` |
| `ColorTertiaryAmber` | `#F59E0B` |
| `ColorErrorRed` | `#EF4444` |
| `ColorNeutralGray` | `#94A3B8` |
| `ColorBackgroundLight` / `Dark` | `#FAF8FF` / `#131B2E` |
| `ColorSurfaceLight` / `Dark` | `#FFFFFF` / `#283044` |
| `ColorBorderLight` / `Dark` | `#E2E8F0` / `#3E4947` |
| `ColorTextPrimaryLight` / `Dark` | `#0F172A` / `#EEF0FF` |
| `ColorTextSecondaryLight` / `Dark` | `#64748B` / `#94A3B8` |

Es gibt **keinen** Token mit dem Namen `on-surface-variant` oder ähnlich (Suche im gesamten
`DesignSystem.xaml` ergebnislos). `ColorTextSecondary*` ist der einzige Text-Token, der als
Entsprechung für gedämpfte/inaktive UI-Elemente infrage kommt.

Typografie-Styles (Zeilen 51–95), `FontFamily`-Setter-Status je Style:

| Style-Key | FontSize | FontAttributes | `FontFamily`-Setter vorhanden? |
|-----------|----------|-----------------|-------------------------------|
| `headline-xl` | 32 | Bold | Nein |
| `headline-lg` | 24 | Bold | Nein |
| `headline-md` | 20 | Bold | Nein |
| `body-lg` | 16 | – | Nein |
| `body-md` | 14 | – | Nein |
| `body-sm` | 12 | – | Nein |
| `price-hero` | 28 | Bold | Nein |
| `label-code` | 12 | – | Ja — über `OnPlatform` (`WinUI`→`Consolas`, `iOS`→`Menlo-Regular`), **kein** Fallback für `Android`/`MacCatalyst` in der `OnPlatform`-Liste |

Keiner der sieben `headline-*`/`body-*`/`price-hero`-Styles referenziert derzeit `OpenSansRegular`,
`OpenSansSemibold` oder einen anderen registrierten Font-Alias; sie fallen auf die
Plattform-Systemschrift zurück. `label-code` referenziert keinen registrierten Alias, sondern feste
Plattform-Schriftnamen (`Consolas`/`Menlo-Regular`).

## `src/Tankradar.MAUI/Resources/Fonts/`

Aktueller Inhalt (nur 2 Dateien):
- `OpenSans-Regular.ttf`
- `OpenSans-Semibold.ttf`

Keine Inter-, keine JetBrains-Mono-Dateien, keine Lizenzdateien (`OFL-*.txt` o. ä.) vorhanden.

## `src/Tankradar.MAUI/Tankradar.MAUI.csproj`

Relevante `<ItemGroup>`-Einträge (Zeilen 53–69):
```xml
<MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
<MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />
<MauiImage Include="Resources\Images\*" />
<MauiImage Update="Resources\Images\dotnet_bot.png" Resize="True" BaseSize="300,185" />
<MauiFont Include="Resources\Fonts\*" />
```
- `Color="#512BD4"` ist die MAUI-Standardvorlagenfarbe (Violett), sowohl bei `MauiIcon` als auch bei
  `MauiSplashScreen`; **nicht** die Teal-Primärfarbe `#0F766E`.
- Die Zeile `<MauiImage Update="Resources\Images\dotnet_bot.png" ... />` ist vorhanden und referenziert
  die Vorlagendatei.
- `<MauiFont Include="Resources\Fonts\*" />` erfasst bereits pauschal alle Dateien im Fonts-Ordner
  (keine Einzelauflistung je Datei) — neue TTF-Dateien im Ordner würden ohne `.csproj`-Änderung erfasst.

## `src/Tankradar.MAUI/Resources/Images/`

Enthält aktuell: `dotnet_bot.png` (92.532 Bytes, MAUI-Vorlagenbild) sowie die vier SVG-Tab-Icons
`icon_favorites.svg`, `icon_map.svg`, `icon_settings.svg`, `icon_tankbook.svg`. Keine Referenz auf
`dotnet_bot.png` wurde außerhalb der `.csproj` (`<MauiImage Update=...>`) gefunden — insbesondere
nicht in den vier View-XAML-Dateien.

## Views: `AutomationId` je Headline-`Label`

| Datei | Headline-`Label`-Text | `AutomationId` gesetzt? |
|-------|------------------------|--------------------------|
| `src/Tankradar.MAUI/Views/FavoritesPage.xaml` | „Favoriten“ | Nein |
| `src/Tankradar.MAUI/Views/MapPage.xaml` | „Karte“ | Nein |
| `src/Tankradar.MAUI/Views/TankbookPage.xaml` | „Tankbuch“ | Nein |
| `src/Tankradar.MAUI/Views/SettingsPage.xaml` | „Optionen“ | Nein |

Alle vier Views erben von `views:TankradarContentPage` (Basisklasse in
`src/Tankradar.MAUI/Views/TankradarContentPage.cs`, leitet `OnAppearing()` an das ViewModel weiter,
keine AutomationId-bezogene Logik). Jede Seite enthält genau ein `VerticalStackLayout` mit zwei
`Label`s (Headline im Style `headline-lg`, Beschreibungstext im Style `body-md`); kein Element in
den vier Dateien trägt derzeit ein `AutomationId`-Attribut.

## `README.md`, Abschnitt „Bekannte Einschränkungen“

Datei: `README.md`, Zeilen 115–131. Erster Punkt (Zeilen 117–124) beschreibt wörtlich, dass Inter
und JetBrains Mono fehlen, aktuell OpenSans/Systemschrift als Fallback verwendet wird und für
`label-code` plattformabhängig Consolas (Windows) bzw. Menlo (iOS) genutzt wird, inklusive eines
expliziten `**TODO:**`-Absatzes zum Nachziehen der Schriften. Die zwei weiteren Punkte
(iOS-Build ohne Mac/Xcode, Android/MacCatalyst-Zusatzziele) sind von dieser Anforderung nicht
betroffen und bleiben unverändert.

## Designentwurf (`design-draft/stitch_smart_fuel_charge_tracker.zip`)

Kein entpackter `design-draft/`-Ordner vorhanden, nur die ZIP-Datei (752.832 Bytes). Für diese
Bestandsaufnahme wurde `startseite_favoriten_gruppen/code.html` (Light) und
`startseite_favoriten_gruppen_dark_mode/code.html` (Dark) aus dem Archiv gelesen (nicht entpackt im
Repository abgelegt).

**Bottom-Navigation, Light-Mode** (`<nav>`-Element):
- Hintergrund: `bg-surface-container-lowest/90` (Tailwind-Token `surface-container-lowest` =
  `#ffffff` im Light-Theme dieser Datei) mit Blur und Schatten
  (`shadow-[0_-2px_12px_rgba(0,0,0,0.05)]`).
- Aktiver Eintrag (Favoriten, `aria-current="page"`): Klassen `text-primary font-semibold`.
  Tailwind-Token `primary` ist in dieser Datei `#005c55` definiert; `primary-container` ist
  `#0f766e` (identisch mit `ColorPrimaryTeal` aus `DesignSystem.xaml`). Welcher der beiden Werte
  tatsächlich dem im Screenshot sichtbaren Teal-Akzent entspricht, wurde hier nicht separat
  verifiziert (Faktenlage: zwei unterschiedliche Grünwerte im selben Tailwind-Config-Block).
- Inaktive Einträge: Klasse `text-on-surface-variant`. Tailwind-Token `on-surface-variant` = `#3e4947`
  in dieser Light-Datei.
- Vier Einträge in Reihenfolge: Favoriten (`local_gas_station`-Icon), Karte (`map`-Icon), Tankbuch
  (`analytics`-Icon), Optionen (`tune`-Icon) — Reihenfolge und Beschriftung stimmen mit
  `AppShell.xaml` überein.

**Bottom-Navigation, Dark-Mode** (separate Datei, eigener Tailwind-Config-Block):
- Hintergrund: `bg-[#0d1117]/95` (Literalwert, kein benannter Token in dieser Datei) mit
  `border-t border-[#30363d]/60`.
- Aktiver Eintrag: Klassen `text-emerald-400` (Tailwind-Default-Farbe, nicht `ColorSecondaryEmerald`)
  plus `drop-shadow-[0_0_8px_rgba(16,185,129,0.35)]` (Glow-Effekt).
- Inaktive Einträge: Klassen `text-text-secondary hover:text-text-primary` (in dieser Dark-Datei
  benannte, aber im gelesenen Ausschnitt nicht aufgelöste Custom-Farben).
- Zum Vergleich: `on-surface-variant` ist in dieser Dark-Datei mit `#9ca3af` definiert, `primary`
  mit `#10b981` (identisch mit `ColorSecondaryEmerald`).

Diese Werte sind reine Fakten aus dem HTML-Designentwurf und keine Vorgabe für konkrete
`Shell.*`-Bindable-Property-Werte; die Anforderung selbst verweist auf diese Unsicherheit
(„Offene Fragen" 1 und 2).
