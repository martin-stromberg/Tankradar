# Planprüfung – Issue #90 (.NET 11 / iOS 27 SDK)

**Status: Plan vollständig**

Geprüft gegen `requirement.md` und `inventory.md` (Ausführung direkt durch
den Hauptagenten, da keine Unteragenten verfügbar):

- ✅ Maintenance-Branch gefordert → `maintenance/net10` existiert bereits
  auf origin (b778efd, letzter .NET-10-`staging`-Stand) — im Plan als
  erledigte Vorbedingung verankert.
- ✅ ITMS-90725-Adressierung: iOS-27-SDK ist nur mit Xcode 27 baubar;
  `macos-latest` (= macos-26) enthält kein Xcode 27 → Runner-Wechsel auf
  `xcode-27` + `IOS_XCODE_VERSION=27.2` deckt das ab.
- ✅ Workload-Konsistenz: Manifest `27.2.11039` ↔ Pack
  `Microsoft.iOS.Sdk.net11.0_27.2` 27.2.11039 ↔ `Xcode_27.2.app`
  (iOS-27.2-SDK) — Versionen verifiziert gegen NuGet und Runner-Image-Doku.
- ✅ Vollständigkeit der TFM-Umstellung: alle 5 Projektdateien plus alle
  TFM-Strings in Skripten/CI erfasst (Grep-Inventur in `inventory.md`).
- ✅ Paketlinien konsistent: MAUI/EF/Logging/Sqlite auf 11.0.x; kein
  Mix mit 10.0.x-Laufzeitpaketen.
- ✅ Testbedarf abgedeckt: lokaler Build/Tests + `test-ios-deployment.ps1`
  + Workflow-Validator; iOS-Realpfad via PR-Check `ios build`.
- ✅ Kein UI-Impact → Usability-Review entfällt fachlich (Marker-Datei
  wird trotzdem für den Ablauf angelegt).
- ⚠️ Restrisiko (im Plan vermerkt): iOS-16-Mindestversion unter Xcode 27;
  erst im CI-Lauf verifizierbar — akzeptables Restrisiko mit definiertem
  Reaktionsweg.
