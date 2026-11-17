# ADR 0021: EF-Core-Compiled-Model und iOS-AOT-/Trimming-Konfiguration

## Status

Angenommen (2026-11-17)

## Kontext

Issue #71 (Follow-up aus dem Architektur-/Sicherheitsreview #62) verlangt eine
Evaluation: Soll für `TankradarDbContext` ein EF-Core-Compiled-Model erzeugt
und die iOS-Konfiguration (`MtouchInterpreter=all`, TrimmerRootAssembly-Liste)
überarbeitet werden? Begründung des Status quo steht bereits im csproj: EF Core
kompiliert LINQ-Abfragen zur Laufzeit und findet Migrationen/Dienste per
Reflection; ohne Interpreter und ungekürzte Assemblys schlägt die
Datenbankinitialisierung fehl. Die Alternative `-all` (AOT + Interpreter als
Fallback) überschritt früher das 60-Minuten-Limit des CI-iOS-Builds.

## Evaluierung

### Generierbarkeit

`dotnet ef dbcontext optimize` (Tools 10.0.10, Runtime 10.0.15, Windows-TFM)
erzeugte ein lauffähiges Compiled Model: 12 Dateien, ~53 KB, ~1.010 Zeilen
(1 Modell/Builder, 8 EntityType-Klassen, Assembly-Attribute). Die Generierung
funktioniert also technisch.

### Messung (Windows, Release, SQLite-Datei, kalter Prozess)

| Schritt | Dauer |
|---------|-------|
| Erste Kontexterstellung | ~56 ms |
| Erster Modellaufbau (`context.Model`) | ~296 ms |
| `MigrateAsync` (9 Migrationen, neue DB) | ~201 ms |
| Zweite Kontexterstellung + Modellzugriff | ~12 ms |
| Erste LINQ-Abfrage (`CountAsync`) | ~109 ms |

Der Modellaufbau ist ein einmaliger Kostenblock pro Prozess (~0,3 s auf
Desktop-Hardware). Ein Compiled Model würde hauptsächlich diesen Anteil
senken; auf iOS ist er wegen des Interpreters höher, bleibt aber einmalig.

### Guidance-Abgleich

- Die EF-Core-Dokumentation empfiehlt Compiled Models primär für *große*
  Modelle; dieses Modell hat 8 Entitätstypen und 9 Migrationen.
- Ein Compiled Model beseitigt **nicht** die Laufzeit-Reflection von EF Core:
  LINQ-Abfragekompilierung und Migrationsauflösung bleiben interpretiert.
  `MtouchInterpreter` und die TrimmerRootAssembly-Ausnahmen wären weiterhin
  nötig – der iOS-Hauptkostenpunkt bliebe bestehen.
- Compiled Models müssen bei jeder Modelländerung neu generiert werden
  (`Run this command again when the model is modified`). Ein vergessenes
  `dotnet ef dbcontext optimize` erzeugt ein veraltetes Modell mit
  schwer diagnostizierbaren Laufzeitfehlern; dieses Risiko wächst mit jeder
  Migration.

## Entscheidung

**Die bestehende Konfiguration wird beibehalten; kein Compiled Model wird
eingeführt.** Konkret:

- `MtouchInterpreter=all` für das iOS-Ziel bleibt (Interpreter ist für EF
  unverzichtbar, unabhängig vom Compiled Model).
- Die `TrimmerRootAssembly`-Ausnahmen für `Tankradar.MAUI`,
  `Microsoft.EntityFrameworkCore[.Relational|.Sqlite]` und
  `Microsoft.Data.Sqlite` bleiben.
- Das Modell wird weiterhin zur Laufzeit gebaut; Migrationen laufen über
  `MigrateAsync` im `DatabaseInitializer`.

## Alternativen

- **Compiled Model einführen:** Abgelehnt. Nutzen (~einmalig 0,3 s Desktop,
  auf iOS höher) steht gegen das Regenerations-Risiko bei jeder Migration,
  ~1.000 Zeilen generierter Code und unverändert nötigem Interpreter.
- **`MtouchInterpreter=-all` (AOT + Interpreter-Fallback):** Abgelehnt. Früher
  CI-Timeout (60 Min); ein Compiled Model ändert daran nichts.
- **TrimmerRoots entfernen:** Abgelehnt. Der Linker würde per Reflection
  genutzte EF-/Entity-Typen entfernen; Datenbankinitialisierung bricht.

## Risikoabschätzung (Beibehaltung)

- **Startzeit:** Der einmalige Modellaufbau kostet auf iOS mehr als die
  ~0,3 s der Desktop-Messung, ist aber durch die Messung als kleiner,
  einmaliger Anteil eingeordnet; die App zeigt den Start ohnehin über den
  Splash. Akzeptabel.
- **Paketgröße:** Unverändert – die ausgenommenen Assemblys werden nicht
  gekürzt (bewusste Größen-Robustheits-Abwägung, bereits dokumentiert).
- **Korrektheit:** Kein Drift-Risiko zwischen generiertem und Laufzeit-Modell.
- **Wiederanlauf:** Evaluation erneut prüfen, wenn das Modell deutlich wächst
  (Faustregel >50 Entitätstypen), Cold-Start-Profile den EF-Anteil als
  dominant zeigen oder EF-/MAUI-Guidance sich ändert.

## Verifikation

Migrationen, Restore und Datenbankintegrität sind unverändert durch die
bestehende Unit-/Integrations-Testsuite abgedeckt (kein Konfigurations-
delta → keine neuen Risiken).
