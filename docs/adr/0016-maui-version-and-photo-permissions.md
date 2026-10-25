# ADR 0016: MAUI-Version festlegen und Fotoberechtigungen auf iOS

## Status

Angenommen (2026-10-25). Entscheidet den offenen Punkt aus [ADR 0015](0015-ios-delivery-and-privacy.md) (Notwendigkeit von `NSPhotoLibraryUsageDescription`).

## Kontext

TestFlight rc.12: „Foto aufnehmen“ im Tankbuch-Formular endete auf dem iPhone nach der Kamera-Freigabe jedes Mal mit „Das Foto konnte nicht übernommen werden.“. Die App verwendete `Microsoft.Maui.*` 10.0.20 (Standardwert aus der installierten Workload). Dort ruft `MediaPicker.PhotoAsync` für die Aufnahme zusätzlich `Permissions.EnsureGrantedAsync<Permissions.PhotosAddOnly>()` auf; ohne `NSPhotoLibraryAddUsageDescription` wirft das eine `PermissionException`. Upstream entfernt den Aufruf mit dotnet/maui#34287; im Quelltext der Tags 10.0.90 und 10.0.120 ist er weg. Die Anwenderentscheidung lautet: MAUI aktualisieren, die Berechtigung nicht eintragen.

## Entscheidungen

- **MAUI 10.0.120:** Neueste stabile 10.0.x-Version (Release 2026-10-21), enthält #34287. `MauiVersion` ist in `Tankradar.MAUI.csproj` ausdrücklich gesetzt; Controls, Core, Essentials und Resizetizer folgen daraus (sonst gilt `MauiWorkloadVersion` der installierten Workload, also je Rechner/Runner anders). Die Pakete kommen aus NuGet, eine Workload-Aktualisierung ist dafür nicht nötig: Restore und Windows-Build sind mit der lokalen Workload 10.0.20 erfolgreich, `net10.0-ios` kompiliert. Die CI bleibt unverändert (`dotnet workload restore Tankradar.sln`, gepinnte iOS-Workload im Baustein `package-ios`); der iOS-Build wird von der CI geprüft.
- **Release-Notes 10.0.20 bis 10.0.120:** Keine als „Breaking“ ausgewiesene Änderung betrifft die App. Für `MediaPicker` relevant: #34287 (kein `PhotosAddOnly` mehr), Korrekturen zu `PickPhotosAsync` (Dateiname/Inhaltstyp, Mehrfachauswahl, `UIKitThreadAccessException` bei 2+ Elementen, vollständiger Pfad für PHPicker-Ergebnisse), unter Windows einmalige EXIF-Drehung. Die App nutzt `PickPhotosAsync` mit `SelectionLimit = 1` und `CapturePhotoAsync` und liest das Ergebnis über `OpenReadAsync`; Fotos werden unverändert übernommen.
- **Berechtigungen laut Quelltext 10.0.120:** `CapturePhotoAsync` verlangt nur `Permissions.Camera` (Mikrofon nur für Video); `PickPhotosAsync` verwendet ab iOS 14 den PHPicker ohne jede `EnsureGrantedAsync`-Abfrage. Die App-Mindestversion ist iOS 16.
- **`NSPhotoLibraryUsageDescription` entfällt** in der iOS-`Info.plist` (Gerätetest: Fotoauswahl öffnet ohne Abfrage). Die iOS-`Info.plist` enthält nur noch Kamera und Standort (Test `Info_plist_DeclaresOnlyCameraAndLocationWhenInUse`). MacCatalyst ist nicht Teil der Auslieferung und nicht geräteerprobt und bleibt unverändert.
- **Fehlermeldungen:** `MauiReceiptPhotoPicker` kapselt `PermissionException` und `FeatureNotSupportedException` in `ReceiptPhotoAccessException` (Grund `PermissionDenied`/`NotSupported`), damit das ViewModel nicht von MAUI abhängt und testbar bleibt. Das Formular zeigt bei verweigerter Freigabe „Kein Zugriff auf die Kamera. Erlaube den Zugriff in den Einstellungen unter Tankatlas.“ (Auswahl: „…auf die Fotos…“), bei fehlender Unterstützung einen eigenen Hinweis, sonst weiter „Das Foto konnte nicht übernommen werden.“. Die Meldung wird angesagt; das Protokoll enthält nur Ausnahmetyp und Grund.

## Folgen

- Das Aufnehmen eines Belegfotos fragt auf iOS nur nach der Kamera.
- Wer die Kamera ablehnt, erhält einen verständlichen Hinweis statt der allgemeinen Fehlermeldung.
- Die MAUI-Version ist an einer Stelle festgelegt und bei Updates bewusst zu ändern; die Workload-Version kann davon abweichen (Build-Werkzeuge der Workload 10.0.20, Bibliotheken 10.0.120).
- Offen (Gerät): Aufnahme und Auswahl mit 10.0.120 auf dem iPhone bestätigen, auch nach abgelehnter Kamera-Freigabe.
