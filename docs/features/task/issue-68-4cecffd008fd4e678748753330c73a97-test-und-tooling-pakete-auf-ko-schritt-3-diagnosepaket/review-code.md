# Code-Review – Schritt 3: Opt-in-Diagnosepaket (#70)

Selbstreview (kein Subagent-Mechanismus verfügbar – dokumentierte Abweichung).

## Geprüfte Änderungen

- `Services/Diagnostics/*`: Puffer, Logger-Provider, Redaktor, Geheimnis-Lieferant, Paketdienst, Weitergabe (Plattform/Test), Selektor.
- `ViewModels/DiagnosticsViewModel.cs`, `SettingsViewModel.cs` (optionale Karte), `Views/SettingsPage.xaml` (Karte „Diagnose“), `Resources/Texts/DiagnosticsTexts.cs`.
- `MauiProgram.cs`: DI-Registrierungen + `ILoggerProvider`.
- Tests: 8 neue Unit-Testdateien + 1 E2E-Test.

## Befunde

### Behoben während des Reviews

1. **`SecretParameterPattern` ließ den Geheimniswert stehen.** Das Muster
   `\b(name)\s*=` ersetzte nur `name=` durch `name=[***]`, der Wert blieb im Text.
   Korrigiert zu `\b(name)\s*=\s*[^\s;&"']+`, sodass der Wert mit erfasst wird.
   Test `Redact_SecretParameter_Removed` beweist die Entfernung.
2. **`ZipArchiveMode.Write` existiert nicht** → `ZipArchiveMode.Create` korrigiert.

### Geprüft und für gut befunden

- **Opt-in:** `DiagnosticsViewModel` erzeugt kein Paket im Konstruktor;
  `ShareCommand` ist erst nach der Vorschau aktiv (`IsNotBusy && HasPreview`).
  `CreatePreviewTextAsync` schreibt keine Datei (Test belegt: Verzeichnis
  wird nicht angelegt).
- **Kein automatischer Versand:** Die App ruft nur `Share.Default.RequestAsync`
  (Systemweitergabe, Nutzerwahl des Ziels) oder im Testmodus eine lokale Kopie.
  Es gibt keinen Netzwerk-Upload.
- **Redaktion:** wörtliche Geheimnisse (Preis-/Routenschlüssel aus den
  bestehenden Ablagen), name=wert-Geheimnisse, E-Mails, Koordinaten
  (Parameter + Paare), Windows-/UNC-/Unix-Pfade, lange Token. Mindestlänge 8
  für wörtliche Werte verhindert Flächenredaktion.
- **Begrenzung:** Puffer 200 Einträge (Ringpuffer), Paket max. 200 Einträge,
  500 Zeichen/Zeile, 128 KiB Protokoll; Retention: nur neuestes Paket bleibt.
- **Flüchtigkeit:** Puffer nur im Speicher des laufenden Starts; Paket im
  Cache-Verzeichnis (`GetCacheDirectory`), wird bei jeder Erzeugung ersetzt.
- **DI:** `ILoggerProvider` als Singleton ergänzt (Standard-Logging unverändert,
  Debug-Provider weiterhin aktiv). Selektor spiegelt das bewährte
  `BackupSharerSelector`-Muster.
- **Tests:** Klassen klein und thematisch getrennt (Redaktor-Geheimnisse,
  Redaktor-Orte, Puffer, Provider, Paketdienst, Geheimnis-Lieferant,
  Weitergabe-Selektor, ViewModel) – entspricht test-class-structure.
- **XAML:** kompilierte Bindungen (`x:DataType`), AutomationIds konsistent
  (`Settings.Diagnostics.*`), `SemanticProperties.HeadingLevel`, ScrollView
  mit Höhenbegrenzung für die Vorschau.

## Ergebnis

**Keine offenen Befunde.**
