# Plan (Schritt 3)

## Akzeptanzszenarien

1. **Happy Path:** Anwender öffnet Optionen → Karte „Diagnose“ zeigt Zweck und
   Umfang → „Inhalt anzeigen“ erzeugt die Vorschau (Metadaten + redigierte
   Protokollauszüge) → „Paket erstellen und teilen“ schreibt
   `tankatlas-diagnose-*.zip` und öffnet die Systemweitergabe (Testmodus:
   Kopie nach `test-shared`).
2. **Ohne vorhandene Einträge:** Puffer leer → Paket enthält Metadaten und den
   Vermerk „keine Protokolleinträge“; Teilen bleibt möglich.
3. **Fehlerfall:** Paket kann nicht erzeugt/geteilt werden → Statusmeldung in
   der Karte, keine Datei.
4. **Redaktion:** Eine Protokollzeile mit Schlüssel, Koordinaten, Pfad und
   E-Mail erscheint im Paket ausschließlich redigiert (Unit-Tests).

## Umsetzung

1. `Services/Diagnostics/`:
   - `DiagnosticLogEntry` (Zeitpunkt, Stufe, Kategorie, Meldung,
     Ausnahmetypname).
   - `IDiagnosticsLogBuffer`/`DiagnosticsLogBuffer`: Ringpuffer
     (Kapazität 200, Mindeststufe Warning, Thread-sicher, TimeProvider).
   - `DiagnosticsLoggerProvider` + `DiagnosticsLogger`: `ILoggerProvider`/
     `ILogger`, füttern den Puffer (alle Kategorien, ≥ Warning).
   - `DiagnosticsRedactor`: statische Redaktion (konfigurierte Schlüsselwerte,
     Schlüssel-/Token-Parameter, E-Mails, Koordinaten, Windows-/UNC-/Unix-
     Pfade, tokenartige Hexfolgen).
   - `ISensitiveValueProvider`/`SensitiveValueProvider`: sammelt aktive
     Schlüssel aus `IApiKeyProvider` und `IRouteSearchAccess`.
   - `IDiagnosticsPackageService`/`DiagnosticsPackageService`: Vorschautext
     + ZIP-Erzeugung im Cache-Verzeichnis (`IAppDataPathProvider.
     GetCacheDirectory()`/Unterordner `diagnostics`); Retention: ältere
     Diagnosepakete werden gelöscht, nur das neueste bleibt; Größenbegrenzung
     (200 Einträge, 500 Zeichen je Eintrag, 128 KiB Gesamtkappe).
   - `IDiagnosticsSharer` + `MauiDiagnosticsSharer` (Share.Default) +
     `TestDiagnosticsSharer` + `DiagnosticsSharerSelector` (Muster der
     Sicherungs-Weitergabe).
   - `DiagnosticsPackageContent` für Vorschau/Paketaufbau.
2. `Resources/Texts/DiagnosticsTexts.cs` mit allen UI-Texten inkl. erklärender
   Inhaltsbeschreibung (was enthalten ist, was nie enthalten ist).
3. `ViewModels/DiagnosticsViewModel`: Karten-VM mit `PreviewCommand`,
   `ShareCommand` (nur nach erzeugter Vorschau aktiv), `StatusMessage`;
   Weitergabe über `IDiagnosticsSharer`.
4. `SettingsViewModel`: optionaler Parameter `DiagnosticsViewModel?` +
   `Diagnostics`/`HasDiagnostics`.
5. `SettingsPage.xaml`: Karte „Diagnose“ nach der Sicherungs-Karte
   (Heading, Hint, Inhaltsvorschau, Buttons, Status; AutomationIds
   `Settings.Diagnostics.*`).
6. `MauiProgram`: Registrierungen (Buffer, `ILoggerProvider`, Paketdienst mit
   Cache-Verzeichnis, Sharer-Selektor, `ISensitiveValueProvider`, VM).
7. Tests:
   - Unit: `DiagnosticsRedactorTests` (Schlüssel, Params, E-Mail, Koordinaten,
     Pfade, Tokens, Name-Freiheit), `DiagnosticsLogBufferTests` (Kapazität,
     Stufenfilter, Reihenfolge, Thread-Sicherheit),
     `DiagnosticsPackageServiceTests` (ZIP-Inhalte, Redaktion angewendet,
     Retention löscht alte Pakete, Größenkappe).
   - E2E `SettingsDiagnosticsE2ETests`: Optionen öffnen → Karte sichtbar →
     Vorschau → Teilen → Datei in `test-shared` liegt vor und Oberfläche bleibt
     bedienbar; Zusatz: Vorschau zeigt keinen gespeicherten Schlüssel.
8. Doku: `docs/help/Einstellungen/` (Diagnose-Abschnitt oder eigene Seite),
   Release Notes EN+DE.

## Scope-Klärung

- **Verarbeitungseinheit:** lokale ZIP-Datei im App-Cache; Auslöser ist
  ausschließlich die Benutzeraktion „Paket erstellen und teilen“.
- **Wirkung:** Datei entsteht lokal; Versand erfolgt nur über den vom Anwender
  bestätigten Systemdialog — die App überträgt nichts.
- **Scope-Ausschlüsse (bestätigt durch Issue-Text):** kein persistenter
  Logspeicher über App-Neustarts, kein Upload/Telemetrie-Endpunkt, keine
  Screenshots/Belegfotos, keine Stations-/Suchdaten im Paket.
- **Bekannte Restriktion:** Stationsnamen/Suchbegriffe können technisch nicht
  musterbasiert erkannt werden; Schutz entsteht durch das Erfassungsdesign
  (nur Warnungs-/Fehlerprotokolle ohne Standort-/Stationsdaten) plus
  Musterredaktion — dokumentiert in `DiagnosticsTexts` und `docs/help`.

## Offene Punkte

Keine.
