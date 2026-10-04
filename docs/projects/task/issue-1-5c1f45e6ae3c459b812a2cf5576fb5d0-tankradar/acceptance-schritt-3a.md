# Abnahmeprüfung – Entwicklungsschritt 3a

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

Geprüft wurden `git diff task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar...HEAD` (31 Dateien) und
`grep -ri tankradar` über `src/`, `docs/help/`, `README.md`, `scripts/` und `.github/`. Außerdem wurden die
Prüfskripte ausgeführt und der abgelegte Windows-Zwischenstand gestartet (Prüfung am 2026-10-04).

**Für Anwender sichtbarer Name (umgestellt):**

- iOS-Anzeigename: In `Tankradar.MAUI.csproj` steht `<ApplicationTitle>Tankatlas</ApplicationTitle>`. MAUI
  erzeugt daraus `CFBundleDisplayName`. Die `Info.plist` setzt keinen eigenen `CFBundleDisplayName`/`CFBundleName`,
  der diesen Wert überschreiben würde.
- Windows-Fenstertitel: `App.CreateWindow` setzt `Title = AppConfiguration.AppDisplayName` (`"Tankatlas"`).
  Der Shell-Titel in `AppShell.xaml` nutzt dieselbe Konstante (`x:Static`). Damit ist der Name an einer Stelle
  zentral hinterlegt.
- Sichtbare Texte: In XAML und Code nennt kein Oberflächentext mehr „Tankradar“. Übrig sind nur
  Klassen-, Namespace- und Typnamen (`TankradarContentPage`, `clr-namespace:Tankradar.MAUI…`).
- Startbild: `Resources/Splash/splash.svg` ist eine reine Pfadgrafik ohne Schriftzug. Eine Änderung war
  nicht nötig.
- Anwenderdokumentation `docs/help/**`, README (Titel und Produktbeschreibung), `changes.log`, ADR 0001,
  Changelog-Überschrift der Review-Version und `releaseNotes` in `update.json` (`package-windows.ps1`) lauten
  jetzt auf „Tankatlas“.

**Technische Bezeichner (wie gefordert unverändert):**

- Bundle-ID `de.martinstromberg.tankradar`: unverändert in csproj, `AppConfiguration.DefaultBundleId` und
  `appsettings.json`.
- `RootNamespace`, Projekt-, Solution- und Assembly-Namen `Tankradar.*`: unverändert. Kein Rename im Diff,
  nur Änderungen an bestehenden Dateien.
- `TANKRADAR_*`-Variablen in Code, Skripten und Workflows: unverändert.
- Windows-Release-Artefakt `release-win-x64.zip`: unverändert und ohne Produktnamen. Release-Titel sind Tags.
- Verbliebene „Tankradar“-Treffer in `docs/help/` und README beziehen sich ausschließlich auf technische
  Bezeichner (`Tankradar.sln`, `Tankradar.MAUI.exe`, Pfade, Bundle-ID, Beispielpfad `Tankradar.ipa`). Dasselbe
  gilt für Entwicklerkommentare in `scripts/iOS-Deployment.ps1`, `scripts/local-ci.ps1`,
  `scripts/package-windows.ps1` und `scripts/test-ios-deployment.ps1`.

**Tests und praktische Verifikation:**

- Neuer E2E-Test `NavigationE2ETests.MainWindowShowsDisplayNameAsTitle` prüft `MainWindow.Title == "Tankatlas"`.
  Das Hauptfenster wird weiterhin über `Application.GetMainWindow` gefunden und hängt damit nicht am Titel.
  Neuer Unit-Test: `AppDisplayName_IsTankatlas_WhileBundleIdKeepsTechnicalName`.
- `scripts/local-ci.ps1` (mit E2E): alle Schritte OK. Unit 16/16, Integration 3/3, E2E 4/4 bestanden
  (laut TRX einschließlich `MainWindowShowsDisplayNameAsTitle`). Format, Sicherheitsprüfung,
  Build mit Warnungen als Fehler, Abdeckung ≥ 70 % und iOS-Compile-Prüfung OK.
- `scripts/test-ios-deployment.ps1`: erfolgreich, sowohl ohne als auch mit `IncludeIosTarget=false
  IncludeMacCatalystTarget=false IncludeAndroidTarget=false`. Die neuen Prüfungen für Anzeigename und
  unveränderte Bundle-ID sind OK.
- Windows-Zwischenstand `review-versions/0.1.2_2026-10-04/bin/Tankradar.MAUI.exe` startet. `MainWindowTitle`
  und der UI-Automation-Name des Hauptfensters lauten „Tankatlas“. Die App wurde danach wieder beendet.
  Der Changelog des Zwischenstands lautet „Changelog - Tankatlas 0.1.2 (2026-10-04)“. `review-versions/`
  ist nicht im Git-Status.
- Nach den Prüfläufen ist der Arbeitsbaum unverändert. Neben der vorbekannten untracked Datei unter
  `docs/features/` ist nur diese Abnahmedatei hinzugekommen.

**Beobachtungen (keine Abweichung im Sinne der Anforderung):**

- Die Windows-Versionsinformationen der `.exe` (`FileDescription`/`ProductName`) lauten `Tankradar.MAUI`.
  Sie werden vom Assembly-Namen abgeleitet und erscheinen außerhalb der App, z. B. im Task-Manager oder in
  den Dateieigenschaften. Auch der Startdateiname `Tankradar.MAUI.exe` im entpackten Release bleibt. Beides
  folgt aus dem bewusst unveränderten Assembly-Namen. Falls der Produktname auch dort erscheinen soll,
  wäre `<Product>Tankatlas</Product>` eine Option für einen späteren Schritt.
- Die `ProductVersion` der Review-`.exe` lautet `0.1.0+7ef55cc…`, die `FileVersion` dagegen `0.1.2.0`. Das ist
  ein Schönheitsfehler der Versionierung von Zwischenständen und gehört nicht zu Schritt 3a.
- Im README steht weiterhin „Dieses Repository befindet sich in Entwicklungsschritt 1“. Der Satz ist
  veraltet, gehört aber nicht zu Schritt 3a.
- Die Windows-`Package.appxmanifest` enthält `DisplayName` = `$placeholder$`. Für die ungepackte App ist das
  ohne Wirkung.
