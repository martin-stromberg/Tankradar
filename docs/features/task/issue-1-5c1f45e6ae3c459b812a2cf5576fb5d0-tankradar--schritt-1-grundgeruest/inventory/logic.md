## `MauiProgram`
Datei: `src/Tankradar.MAUI/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp()` | `public static` | Baut `MauiApp` über `MauiApp.CreateBuilder()`; ruft `.UseMauiApp<App>()` und `.ConfigureFonts(...)` auf, registriert DI-Dienste (`AppConfiguration`, vier ViewModels, vier Pages) und aktiviert im `DEBUG`-Build `builder.Logging.AddDebug()`. |

Aktueller Zustand von `ConfigureFonts(...)` (Zeilen 22–29):
```csharp
.ConfigureFonts(fonts =>
{
    // Inter/JetBrains Mono sind als Ziel-Schriftarten vorgesehen; ohne verfügbare TTF-Dateien
    // (siehe README, Abschnitt "Bekannte Einschränkungen") wird auf die mitgelieferten
    // OpenSans-Schriften bzw. die Plattform-Systemschrift zurückgegriffen.
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
});
```
- Es werden aktuell genau zwei Fonts registriert (`OpenSansRegular`, `OpenSansSemibold`), beide aus `Resources/Fonts/OpenSans-*.ttf`.
- Es existieren **keine** `fonts.AddFont(...)`-Aufrufe für Inter oder JetBrains Mono.
- Der erklärende Kommentar direkt über dem Block beschreibt explizit den aktuellen Fallback-Zustand (fehlende TTF-Dateien, Rückgriff auf OpenSans/Systemschrift).
- `builder.Services` registriert aktuell **keinen** Datenverzeichnis-Service; es existiert kein Aufruf wie `AddSingleton<...>()` für einen Pfad-Provider.

Abonnierte Events: keine
Publizierte Events: keine

## `AppConfiguration` (Referenzmuster für Service-Registrierung)
Datei: `src/Tankradar.MAUI/Services/AppConfiguration.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AppConfiguration()` (Konstruktor) | `public` | Liest `TANKRADAR_BUNDLE_ID` aus `Environment.GetEnvironmentVariable`, sonst `DefaultBundleId`. |
| `LoadFromSettingsFileAsync()` | `public async Task` | Lädt `BundleId` optional aus `Resources/Raw/appsettings.json` nach (per `FileSystem.OpenAppPackageFileAsync`), sofern keine Umgebungsvariable gesetzt ist. |

- Dies ist der einzige bislang existierende Service im Ordner `src/Tankradar.MAUI/Services/` (aktuell nur diese eine Datei). Er zeigt das im Projekt etablierte Muster „Umgebungsvariable liest Konfiguration, Fallback auf Standardwert“ und wird in `MauiProgram.cs` per `builder.Services.AddSingleton<AppConfiguration>();` registriert.
- Kein Bezug zu Datenverzeichnissen; dient hier nur als Beleg für das bestehende Registrierungsmuster.

Abonnierte Events: keine
Publizierte Events: keine

## `TestDataPaths`
Datei: `src/TestSupport/TestDataPaths.cs`
Namespace: `Tankradar.TestSupport`

| Methode/Member | Sichtbarkeit | Kurzbeschreibung |
|-----------------|-------------|------------------|
| `TestDataPathEnvironmentVariable` | `public const string` | Wert `"TEST_DATA_PATH"`. Zentrale, bereits im Repository vorhandene Konstante für den Namen der Umgebungsvariable zur Testdaten-Isolation. |

- Diese Klasse enthält ausschließlich die Konstante, keine Logik.
- Wird bereits verwendet von:
  - `src/Tankradar.Tests.Integration/TestDataContext.cs` (`ResolveDataDirectory()`, liest die Variable, Fallback auf `%LocalAppData%\Tankradar.Tests`).
  - `src/Tankradar.Tests.E2E/E2ETestBase.cs` (setzt die Variable im `ProcessStartInfo.Environment` vor App-Start).
- Wird **nicht** von `src/Tankradar.MAUI` referenziert; im App-Projekt (`Tankradar.MAUI.csproj`) gibt es aktuell keine `Compile Include`-Verlinkung auf `TestSupport/TestDataPaths.cs` und keinen `ProjectReference` auf ein `TestSupport`-Projekt.

## Datenverzeichnis-Service (`AppDataPathProvider` / `IAppDataPathProvider`)

Im gesamten Repository (Suche über `src/**/*.cs`, außerhalb `bin`/`obj`) existiert **keine** Klasse oder Datei mit einem Namen wie `AppDataPathProvider` oder `IAppDataPathProvider`, weder im Namespace `Tankradar.MAUI.Services` noch anderswo. Der in der Anforderung beschriebene Service und die zugehörige Abstraktion sind vollständig neu zu erstellen; es gibt keinen Vorgänger-/Teilzustand, der erweitert werden könnte.
