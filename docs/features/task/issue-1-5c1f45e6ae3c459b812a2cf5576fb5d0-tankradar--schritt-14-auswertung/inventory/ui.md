# UI: Seite, Texte, Stile, AutomationIds, Grafik

## `TankbookEntriesPage`
Dateien: `src/Tankradar.MAUI/Views/TankbookEntriesPage.xaml` (510 Zeilen), `TankbookEntriesPage.xaml.cs` (19 Zeilen, nur `InitializeComponent` und `BindingContext = viewModel`)

- Basis `TankradarContentPage`, `x:DataType="vm:TankbookEntriesViewModel"`, Titel `TankbookEntryTexts.PageTitle`, Hintergrund per `AppThemeBinding` (`ColorBackgroundLight`/`ColorBackgroundDark`), mischt `resources:TankbookStyles` ein.
- Aufbau: ein `ScrollView` mit `VerticalStackLayout` (Padding `space-md`, Spacing `space-md`) in dieser Reihenfolge: Kopf (Überschrift `TankbookEntriesPage.Headline`, Fahrzeugname, `VehicleDetails`, Intro) -> `StatusMessage` -> Schaltfläche „Vorgang erfassen“ (`IsVisible="{Binding Form.IsClosed}"`) -> Formular-`Border` (`BindingContext="{Binding Form}"`, `IsVisible="{Binding IsOpen}"`) -> Überschrift „Vorgänge, neueste zuerst“ -> Leerhinweis -> Liste (`BindableLayout` über `Entries`, Karte je `TankbookEntryItemViewModel`).
- Es gibt keinen Bereich für Auswertung/Statistik und keine zweite Seite dafür. Shell-Route: `tankbookentries` (nur diese Seite, `AppShell.xaml.cs`). Die Seite wird über „Tank- und Ladevorgänge“ auf der Fahrzeugkarte (`Tankbook.Vehicle.Entries`) geöffnet, Rückkehr mit dem Zurück-Pfeil (`NavigationViewBackButton`).
- Registrierung: `TankbookEntriesPage` und `TankbookEntriesViewModel` transient in `MauiProgram.cs` (Zeilen 83, 98); `ITankbookEntryService`/`IVehicleService` Singleton.
- Code-Behind-Dateien (`*.xaml.cs`) sind aus der Coverage ausgenommen (`coverlet.runsettings`).

## Stile und Design-System
- `src/Tankradar.MAUI/Resources/DesignSystem.xaml`: Farben `ColorPrimaryTeal #0F766E`, `ColorSecondaryEmerald #10B981`, `ColorTertiaryAmber #F59E0B`, `ColorErrorRed #EF4444`, `ColorNeutralGray #94A3B8`, `ColorBackgroundLight #FAF8FF`/`ColorBackgroundDark #131B2E`, `ColorSurfaceLight #FFFFFF`/`ColorSurfaceDark #283044`, `ColorBorderLight/Dark`, `ColorTextPrimaryLight/Dark`, `ColorTextSecondaryLight/Dark`; Abstände `space-xs/sm/md/lg/xl` (4/8/16/24/40), `rounded-*`, `MinimumTouchTarget` (44), Schatten `ShadowLevel0..3`, Label-Stile `headline-xl/lg/md`, `body-lg/md/sm`, `price-hero`, `label-code`.
- `src/Tankradar.MAUI/Resources/TankbookStyles.xaml` (+ `.xaml.cs`): `TankbookCard`, `TankbookButton`, `TankbookOutlineButton`, `TankbookDangerButton`, `FuelKindChip`, `UnitFieldBorder`, `FieldLabel`, `FieldError`, `UnitLabel`.
- `src/Tankradar.MAUI/Resources/MapPalette.cs`: Farbpalette der Karte (`Cheapest #006C49`, `Normal #0F766E`, `Expensive #BA1A1A`, `Closed #64748B`, `For(PriceLevel)`).
- Icon: `Resources/Images/icon_tankbook.svg`.

## Grafik-Nutzung (MAUI Graphics)
`Microsoft.Maui.Graphics`, `GraphicsView` und `IDrawable` werden im gesamten `src/Tankradar.MAUI` nicht verwendet (Suche ohne Treffer in `.cs` und `.xaml`; nur `Color.FromArgb` in `MapPalette`). Es gibt keine Diagramm-Komponente und kein zusätzliches Diagramm-/Charting-Paket: Pakete der App-Projektdatei sind `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug`, `Microsoft.EntityFrameworkCore.Sqlite`, `SQLitePCLRaw.bundle_e_sqlite3`, `Microsoft.EntityFrameworkCore.Design`. Karten-Darstellung der Startseite/Suche ist davon getrennt (Kacheln, siehe `Services/Map`).

## Texte (`src/Tankradar.MAUI/Resources/Texts/`)
Alle UI-Texte sind deutsche `const string` bzw. `static`-Formatierer in Klassen je Bereich (`ChoiceTexts`, `DetailTexts`, `FavoritesTexts`, `MapTexts`, `PriceTexts`, `SearchTexts`, `SettingsTexts`, `TankbookEntryTexts`, `TankbookTexts`); sie werden in XAML per `{x:Static texts:...}` eingebunden, Formatierung mit deutscher Kultur (`German`).

`TankbookEntryTexts` (Auszug der relevanten Elemente): `PageTitle` „Tank- und Ladevorgänge“, `Intro`, `EntriesHeading` „Vorgänge, neueste zuerst“, `NoEntries`, `AddEntry`, `PartialLabel`, `PartialBadge` „Teilbetankung“, `LoadFailed`, `VehicleNotFound`; Formatierer `FormatAmount` („74,36 €“), `FormatUnitPrice`, `FormatQuantity`, `FormatKilometers` („10.500 km“), `FormatConsumption` („6,00 L/100 km“), `GetConsumptionUnit(TankbookEntryKind)`, `FormatDateTime` („dd.MM.yyyy, HH:mm“), `GetConsumptionLine(TankbookEntryOverview)` mit den Zuständen:
- `Calculated`: „Verbrauch 6,00 L/100 km“
- `Partial`: „Teilbetankung: zählt beim nächsten vollen Vorgang mit“
- `NoPreviousFullEntry`: „Noch kein Verbrauch: erster voller Vorgang“
- `NoDistance`: „Kein Verbrauch: keine Strecke seit dem letzten vollen Vorgang“

`TankbookTexts`: `PageTitle` „Tankbuch“, `KilometerUnit` „km“, `LiterConsumptionUnit` „L/100 km“, `ElectricConsumptionUnit` „kWh/100 km“, `ConsumptionLabel` „Startverbrauch, optional“, `SaveFailed`, `Entries` „Tank- und Ladevorgänge“, `FormatVehicleDetails(Vehicle)` (z. B. „Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km“), `GetConsumptionUnit(VehicleFuelKind)`, `FormatConsumptionInput`.

Es gibt keine Texte für Auswertung (Kennzahlen, Monat, Hinweise bei zu wenigen Vorgängen). Tests der Texte: `TankbookEntryTextsTests_Format`, `TankbookEntryTextsTests_Messages`, `TankbookTextsTests_Labels`, `TankbookTextsTests_Messages`.

## AutomationId-Konventionen
- Schema `<Bereich>.<Element>` bzw. `<Bereich>.<Unterbereich>.<Element>`, im Code punktgetrennt in PascalCase; je Listeneintrag identische Id (die Tests suchen alle und ordnen sie von oben nach unten, `TankbookE2EDriver.FindAllOrdered`); Auswahl-Chips: `AutomationId="{Binding AutomationKey, StringFormat='...{0}'}"`.
- Fahrzeugseite `TankbookPage`: `TankbookPage.Headline`, `Tankbook.Status`, `Tankbook.Empty`, `Tankbook.AddVehicle`, `Tankbook.Form.{Title,Name,NameError,FuelKind.<Key>,Odometer,OdometerUnit,OdometerError,Consumption,ConsumptionUnit,ConsumptionError,Save,Cancel}`, `Tankbook.Vehicle.{Name,Details,Entries,Edit,Delete,DeleteQuestion,DeleteConfirm,DeleteCancel}`.
- Vorgangsseite `TankbookEntriesPage`: Kopf `TankbookEntriesPage.Headline`, `TankbookEntries.{Vehicle,VehicleDetails,Status,Add,Empty}`; Formular `TankbookEntries.Form.{Title,Date,Time,DateError,Station,StationError,KnownStation.<Key>,KindText,Kind.<Key>,Quantity,QuantityUnit,QuantityError,UnitPrice,UnitPriceUnit,UnitPriceError,TotalAmount,TotalAmountUnit,TotalAmountError,ComputedHint,Odometer,OdometerUnit,OdometerError,Partial,Note,NoteError,PhotoPreview,PickPhoto,CapturePhoto,RemovePhoto,PhotoError,Status,Save,Cancel}`; Listeneintrag (Singular-Präfix) `TankbookEntry.Item.{Station,Total,Date,UnitPrice,Partial,Kind,Quantity,Distance,Consumption,Note,ShowReceipt,Receipt,ReceiptMissing,Edit,Delete,DeleteQuestion,DeleteConfirm,DeleteCancel}`.
- Barrierefreiheit: sichtbarer Text ist der Name der Schaltfläche, Kontext in `SemanticProperties.Hint` (WCAG 2.5.3, ADR 0009 Zusatz 12c); Überschriften mit `SemanticProperties.HeadingLevel`; Chips zeigen „Ausgewählt“ im Hinweis (`HelpText` in FlaUI).
- Es gibt keine AutomationIds für eine Auswertung.
