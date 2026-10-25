# ADR 0015: iOS-Auslieferung, Datenschutzangaben und Härtungen

## Status

Angenommen (2026-10-25), Teil A (Auslieferung, Datenschutz, Icons). Befunde und Entscheidungen zu Routensuche-Härtungen, Absturz 0xC0000005 und Sicherungstest werden in Teil B ergänzt.

## Kontext

Schritt 18 prüft und schließt das vorhandene Auslieferungsgerüst (Skript `scripts/iOS-Deployment.ps1`, Baustein `package-ios`, Privacy-Manifest, Dateischutz) für die fertige App. Es gibt keinen Mac im Projekt: Signierung, Upload und der Inhalt der gebauten IPA sind nur über CI und Laufzeitprüfungen belegbar.

## Entscheidungen

- **Mindestversion iOS 16:** Quelle ist `SupportedOSPlatformVersion` in der csproj (unverändert). Geprüft wird an drei Stellen: `test-ios-deployment.ps1` (csproj-Wert), `Invoke-IpaValidation` (`MinimumOSVersion` der IPA, Fehler bei kleinerer oder fehlender Angabe) und der CI-Schritt *Validate iOS package*. Die Prüfung im Paket ist die einzige Stelle, an der Abweichungen sichtbar werden.
- **Fehlende Werte:** `Get-MissingDeployValues` liefert je Aktion die fehlenden Werte mit Parameter, Umgebungsvariable und Herkunftshinweis; alle werden gesammelt gemeldet. Jede Aktion prüft nur ihre eigenen Werte (`upload`: nur App-Store-Connect-Zugang, keine Signierung; `device`: Zertifikat und Profil; `build`/`simulator`: nichts). Die Team-ID wird als Hinweis aus dem Zertifikatsnamen „(TEAMID)“ erklärt, nicht als neue Pflicht.
- **CI-Prüfung:** Der Baustein enthält einen Bash-Schritt statt eines Aufrufs des PowerShell-Skripts (das Skript ist auf Pair-to-Mac/SSH ausgelegt). Die Fehlertexte sind identisch; `test-ios-deployment.ps1` vergleicht sie. Zusätzlich prüft der Schritt die Größe des Marketing-Icons (1024 x 1024) mit `assetutil`; ist sie nicht lesbar, gibt es nur eine Warnung.
- **Berechtigungen:** Standorttext nennt zusätzlich den Routenstart (die Routensuche verwendet den Standort als Startpunkt). Kamera und Fotos bleiben auf Belegfotos begrenzt. `NSPhotoLibraryUsageDescription` bleibt erhalten: Er kann ohne Gerät nicht als ungenutzt belegt werden, ein Entfernen birgt den Absturz bei fehlendem Schlüssel, das Behalten höchstens eine Rückfrage der App-Prüfung. Gerätetest und Entscheidung bleiben offen. Weitere Schlüssel sind per Test ausgeschlossen.
- **Export-Compliance:** `ITSAppUsesNonExemptEncryption` = false bleibt (nur Standard-Verschlüsselung des Betriebssystems).
- **Privacy-Manifest:** `UserDefaults` (CA92.1) wird deklariert (MAUI Essentials kann es verwenden; fehlende Deklaration löst ITMS-91053 aus), `FileTimestamp` erhält zusätzlich `3B52.1`. Die erfasste Datenart bleibt der genaue Standort (App-Funktionalität). Adress- und Ortseingaben werden an Dritte übermittelt, aber nicht vom Entwickler gespeichert; sie sind als „an Drittanbieter übermittelt, nicht gespeichert“ im Datenschutztext benannt, nicht als eigene Datenart im Manifest.
- **Data Protection:** Klasse `CompleteUntilFirstUserAuthentication` bleibt (Hintergrundzugriffe). Neu: auch fertige Exportdateien erhalten `ProtectFile` (Sicherungen und Belegfotos taten es bereits).
- **Keychain-Zugriffsklasse:** `SecureStorage` legt Einträge mit AfterFirstUnlock an. Eine Änderung auf „ThisDeviceOnly“ ist ohne Eigenbau (direkter Keychain-Zugriff) nicht möglich und nicht gefordert. Stattdessen wird die Formulierung präzisiert: In der Keychain des Geräts bzw. im Credential Locker; nicht in der Sicherung der App, nicht im Build; nicht mit iCloud synchronisiert; **in verschlüsselten Gerätesicherungen können** iOS-Einträge enthalten sein. „Gerätelokal“ entfällt in Kommentaren, Hilfe, README und ADR 0014.
- **Datenschutzdokumentation:** Neuer Bereich `docs/help/Datenschutz/` mit den Angaben für App Store Connect. Die Datenschutzerklärung (öffentliche URL) liegt beim Inhaber und ist noch offen.
- **Windows-Icon mit mehreren Größen:** Der Resizetizer schreibt nur ein ICO mit 64 px. Ein MSBuild-Ziel (`MSBuild/WindowsAppIcon.targets`, nur Windows, nach `ResizetizeImages`, `RoslynCodeTaskFactory`, keine neue Abhängigkeit) ersetzt `resizetizer/r/appicon.ico` durch ein ICO mit den Teilbildern 16, 24, 32, 48 und 256 px (PNG-Einträge). Auf diese Datei zeigt `ApplicationIcon`; sie wird in die Build-Ausgabe kopiert. Geprüft: Das ICO ist vollständig im erzeugten Programm eingebettet. Die Größe 64 px entfällt, weil der Resizetizer dafür keine Teildatei liefert.
- **Icon-Absicherung:** `create-review-version.ps1` leert den Publish-Ordner vor dem Build und prüft anschließend Zeitstempel (nicht älter als der Build und die SVG-Quellen) und ICO-Größen (16, 32, 48, 256). Unit-Tests werten die SVG-Quellen und die Einbindung per XML aus. Das 1024-px-Marketing-Icon entsteht aus der quadratischen, vollflächig gefüllten Quelle ohne Transparenz; ein vorhandenes, vom Resizetizer erzeugtes Bild war 1024 x 1024 und an den geprüften Pixeln deckend. Die Windows-App wird unverpackt ausgeliefert: Es gibt keinen Startmenü- oder Kacheleintrag; die Kachelbilder des Resizetizers bleiben ungenutzt (Korrektur in `changes.log`).

## Folgen

- Fehlende Pflichtwerte werden vor dem ersten Mac-Zugriff gesammelt gemeldet.
- Ein Paket mit `MinimumOSVersion` unter 16, ohne Manifest oder mit falscher Verschlüsselungsangabe erreicht TestFlight nicht.
- Offen und nur mit Mac/Gerät klärbar: Verhalten der Berechtigungsdialoge, Notwendigkeit von `NSPhotoLibraryUsageDescription`, tatsächlicher Inhalt der IPA, Annahme durch Apple.
