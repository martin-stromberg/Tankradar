# Testergebnisse – Schritt 3: Opt-in-Diagnosepaket (#70)

## Build

- `dotnet build src/Tankradar.MAUI -c Release` (Windows-Ziel,
  TreatWarningsAsErrors): **0 Warnungen, 0 Fehler**.
- `dotnet build src/Tankradar.Tests.E2E -c Release`: **0 Fehler**.

## Unit-Tests

`dotnet test src/Tankradar.Tests.Unit -c Release` (Windows-Targets):

- **2162/2162 bestanden, 0 Fehler, 0 übersprungen** (davon 43 neue
  Diagnose-Tests).

Neue Testabdeckung (Issue #70):

- `DiagnosticsRedactorTests_Secrets` / `_Locations`: wörtliche Geheimnisse,
  name=wert-Parameter, E-Mails, Koordinaten (Parameter + Paare),
  Windows-/Unix-Pfade, lange Tokens, Mindestlänge, Unverändertheit
  unverfänglicher Texte.
- `DiagnosticsLogBufferTests`: Mindeststufe, Ringpuffer-Kapazität,
  Chronologie, Parameterprüfung.
- `DiagnosticsLoggerProviderTests`: Stufen-Filter, Kategorie, Ausnahmetyp.
- `DiagnosticsPackageServiceTests`: ZIP-Einträge, Manifest, Redaktion im
  Paket, Eintrags-/Zeilen-/Größenbegrenzung, Retention (nur neuestes Paket),
  Vorschau ohne Dateischreiben, Leer-Puffer-Hinweis.
- `SensitiveValueProviderTests`: beide Schlüsselquellen, Leerwerte,
  Deduplizierung.
- `DiagnosticsSharerSelectorTests_TestMode`: Testmodus kopiert nach
  `test-shared`, Plattformweitergabe wird im Testmodus nie erzeugt,
  Produktivmodus nutzt Plattform, fehlende Datei → Fehler.
- `DiagnosticsViewModelTests`: kein automatisches Erzeugen/Teilen, Teilen erst
  nach Vorschau aktiv, Ablauf Vorschau→Teilen, Fehlerstatus.

## E2E-Tests

`DiagnosticsE2ETests_Flow` (neu): Karte sichtbar, Teilen vor Vorschau
gesperrt, Vorschau zeigt Paketinhalt, Teilen legt gültiges ZIP mit den drei
Einträgen in `test-shared` ab (kein Systemdialog), Statusmeldung erscheint.

Lokale Ausführung in dieser Session nicht möglich (App-Launch benötigt
interaktive Windows-Sitzung); der Test läuft mit der übrigen E2E-Suite in
der PR-CI auf Windows. Verifikation erfolgt dort (wie in Schritt 1/2).

## Ergebnis

**Alle ausführbaren Tests bestanden.**
