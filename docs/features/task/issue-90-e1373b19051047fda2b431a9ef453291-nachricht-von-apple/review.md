# Plan-Review – Issue #90

**Status: Vollständig umgesetzt**

Gegenprobe des Diff (38 Dateien) gegen `plan.md`:

- ✅ `global.json` → 11.0.100; alle 5 Projekte auf `net11.0-*`
- ✅ Pakete: MAUI 11.0.10, EF/Logging/Data.Sqlite 11.0.0;
  `SQLitePCLRaw.bundle_e_sqlite3` musste zusätzlich auf 3.0.5
  (transitives Minimum von EF Core 11, NU1605) — planergänzende,
  notwendige Folgeänderung.
- ✅ CI: `dotnet-version` überall 11.0.1xx bzw. 11.0.x;
  iOS-Jobs auf `xcode-27`; `package-ios` pinnt 11.0.100-Band und
  Workload 27.2.11039; Pack-Glob `net11.0_*`; Build `-f net11.0-ios`.
- ✅ `IOS_XCODE_VERSION=27.2` als Repo-Variable gesetzt (gh).
- ✅ Skripte und Doku (README, pages, docs/help, RELEASE_NOTES,
  changes.log) aktualisiert.
- ✅ `maintenance/net10` auf origin (b778efd = letzter .NET-10-Stand).
- ➕ Planergänzungen während der Umsetzung:
  - CsWinRT1032: 11 Collection-Expressions in der App auf konkrete
    Array-Typen gegossen (Analyzer-Vorgabe), in den Testprojekten
    Unit/Integration per `NoWarn` unterdrückt (kein WinRT-Versand).
  - `BackupTestTampering.RemovedCollation`: EF Core 11 erzeugt
    `COLLATE "NOCASE"` (statt `COLLATE NOCASE`) — Tamper deckt beide
    Schreibweisen ab.
- ⚠️ Offen bis CI: iOS-Build auf `xcode-27` (iOS-16-Mindestversion,
  Workload-Pin) erst im PR-Check verifizierbar — erwarteter Pfad.
