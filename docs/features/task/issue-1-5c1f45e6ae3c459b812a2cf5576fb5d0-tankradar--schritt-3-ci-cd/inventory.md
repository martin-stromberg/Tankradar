# Bestandsaufnahme

- Solution `Tankradar.sln`: `Tankradar.MAUI` (net10.0-android/ios/maccatalyst/windows10.0.19041.0, Version 0.1.0 fest in csproj), Tests Unit/Integration (net10.0-windows, xUnit, coverlet.collector), E2E (FlaUI, startet `bin\Debug\...\Tankradar.MAUI.exe` hartkodiert).
- Kein `.github/`, kein `package.json`, keine Coverage-Konfiguration. Lokal: .NET 10.0.401 mit Workloads android/ios/maccatalyst/maui-windows, Node 24, Python 3.13 (PyYAML), gh 2.92, reportgenerator.
- Git-Hooks (Schritt 2) unter `.githooks/` blockierend; Conventional-Commits-Typen inkl. `ci`, `merge`, `plan`.
- Gesamte Solution baut lokal (Release) erfolgreich, aber: mit `TreatWarningsAsErrors` scheitert sie an 2x CS1574 (cref) und CS0436 (doppelt eingebundene `TestDataPaths.cs` in Integration-Tests).
- Rohe Coverage 2,4 % (generierter Code, XAML, Platforms); ohne Ausschlüsse ist Schwelle 70 % unerreichbar.
- `scripts/create-review-version.ps1` zeigt funktionierendes Windows-Publish: `dotnet publish -c Release -f net10.0-windows10.0.19041.0 -p:Version=...`.
- Vorlage: semantic-release liefert als erste Version 1.0.0 und hebt bei Breaking Changes auf 1.0 an – widerspricht Projektvorgabe (0.1.0, keine automatische 1.0).
- `AppConfiguration`: Bundle-ID über `TANKRADAR_BUNDLE_ID`.
