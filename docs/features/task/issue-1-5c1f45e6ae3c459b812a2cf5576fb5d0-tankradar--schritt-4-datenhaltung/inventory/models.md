Es existieren keine Datenmodellklassen und keine Enums für Einstellungen oder Datenhaltung. `src/Tankradar.MAUI/Models/` enthält ausschließlich `.gitkeep`.

## `AppConfiguration`
Datei: `src/Tankradar.MAUI/Services/AppConfiguration.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `BundleId` | `string` | App-Kennung |
| `AppDisplayName` (const) | `string` | Anzeigename „Tankatlas" |
| `DefaultBundleId` (const) | `string` | Standard-Bundle-ID |

## `BaseViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/BaseViewModel.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Title` | `string` | Seitentitel |
| `IsBusy` / `IsNotBusy` | `bool` | Ladezustand |
