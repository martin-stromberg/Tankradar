# Anforderung (übersetzt)

## Quelle

- Issue #90 „Nachricht von Apple" (https://github.com/martin-stromberg/Tankradar/issues/90)
- Auslösender Kommentar des Repository-Eigentümers:
  „@softwarewerkstatt Erstell bitte einen Maintenance-Branch für die .NET10-Version und dann erledige diese Aufgabe"

## Eingangsmeldung (App Store Connect / Apple Developer Relations)

> ITMS-90725: SDK version issue — This app was built with the iOS 26.5 SDK.
> Starting April 2027, iOS and iPadOS apps must be built with the iOS 27 SDK or
> later, included in Xcode 27 or later, in order to be uploaded to App Store
> Connect or submitted for distribution.
> (Tankatlas, Version 0.8.0, Build 386)

## Übersetzte Anforderung

1. **Maintenance-Branch für .NET 10 anlegen:** Vor der Umstellung soll ein
   eigener Branch den aktuellen .NET-10-Stand konservieren, damit die
   .NET-10-Linie bis zur Apple-Frist (April 2027) weiterhin gepflegt werden
   kann. → `maintenance/net10` von `staging` abgezweigt und gepusht.
2. **App auf .NET 11 / iOS-27-SDK umstellen:** Die iOS-Builds müssen künftig mit
   dem iOS-27-SDK (Xcode 27) erstellt werden, damit App-Store-Uploads ab
   April 2027 nicht abgelehnt werden. Da die Hauptlinie von .NET 10 auf
   .NET 11 wechselt (dort liegt die iOS-27-Workload), ist das zugleich das
   .NET-11-Upgrade der gesamten App:
   - `global.json` → .NET SDK 11.0.100
   - Alle Zielframeworks `net10.0-*` → `net11.0-*`
   - NuGet-Pakete auf die .NET-11-Linie (MAUI 11.0.10, EF Core/Logging/
     Data.Sqlite 11.0.0)
   - CI: .NET-Setup auf 11.0.1xx, iOS-Workload-Pin auf Manifest
     `27.2.11039` (iOS 27.2 SDK), macOS-Runner mit Xcode 27 (`xcode-27`-Image),
     `IOS_XCODE_VERSION=27.2`
   - Lokale Skripte und Dokumentation auf die neuen Zielframeworks.

## Akzeptanzkriterien

- `maintenance/net10` existiert auf dem Remote und zeigt auf den letzten
  .NET-10-Stand von `staging`.
- iOS-Jobs der CI laufen auf einem Runner mit Xcode 27 und bauen mit der
  iOS-27.2-SDK-Workload (`Microsoft.iOS.Sdk.net11.0_27.2` 27.2.11039).
- Die Lösung baut und testet unter .NET SDK 11.0.100 (Windows-Ziel,
  Unit- und Integrationstests grün).
- PR gegen `staging` mit Verknüpfung `Closes #90`.

## Nicht-Ziele / Scope-Ausschlüsse

- Kein sofortiger Re-Upload nach App Store Connect (Apple-Frist ist
  April 2027; der nächste reguläre Release liefert das neue Binary).
- Keine Anhebung der iOS-Mindestversion (bleibt iOS 16 gemäß
  Stakeholder-Entscheidung).
- Keine Schemaänderung / keine EF-Migration (reines Plattform-Upgrade).
