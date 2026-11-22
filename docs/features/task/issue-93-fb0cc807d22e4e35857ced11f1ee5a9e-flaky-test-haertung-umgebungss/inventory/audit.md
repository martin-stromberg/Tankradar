# Audit: umgebungssensitive Tests, Deadlines, Cleanup-Stellen

Heuristik der Anforderung: `Process.Start`/`ProcessStartInfo`, FlaUI/UI-Automation,
`HttpListener`/Loopback-Ports, echte Dateisystemszenarien, Kindprozesse mit eigenen
Zeitbudgets, Clipboard. Befundlage **vor** jeder Markierung (keine Trait-Nutzung im Repo).

## Kandidaten nach Heuristik

### A. FlaUI/UI-Automation + echter App-Prozess — gesamtes E2E-Projekt

- `src/Tankradar.Tests.E2E`: ~111 Dateien, 184 ausgeführte Tests (TRX). Jede `E2ETestBase`-Instanz
  startet `Tankradar.MAUI.exe` per `Application.Launch(ProcessStartInfo)` und bedient sie
  über UIA3. → Per Heuristik **alle** Klassen außer `StartFailureCleanupE2ETests`
  (kein App-Start, Meta-Test) OS-sensitiv.
- Zusätzlich OS-Bezug innerhalb der E2E-Basen: Loopback-Mock-Server
  (`SearchE2ETestBase`, `RouteE2ETestBase`, `PriceServiceE2ETestBase` u. a.) und
  `TcpListener`-Portprobe (`PriceServiceE2ETests_Unreachable`, `HomeE2ETests_Offline`,
  `OfflineE2ETests_Tankbook` via `ClosedPortUrl()`).
- Vorkommende FlaUI/OS-APIs: `UIA3Automation`, `Application.Launch`, `Process.GetProcessById`,
  `WaitForInputIdle`, `PrintWindow`/`GetWindowRect`/`IsIconic` (user32 P/Invoke),
  `System.Drawing`-Screenshot. Clipboard: **kein** Vorkommen in `src/`.

### B. Kindprozess mit eigenem Zeitbudget — `ApiKeyBuildTests_*` (Integration)

- `ApiKeyBuildTests_Log` (3 Facts), `ApiKeyBuildTests_Sources` (3 Facts) über
  `ApiKeyBuildRunner.Build` → `Process.Start("dotnet", "build …")`, `WaitForExit(5 min)`.
  Klassischer `OsInterface`-Kandidat (prozessübergreifend, SDK-abhängig, laufzeitstark).

### C. Echte Loopback-Portbindung — Integration und Unit

- `LoopbackHttpListenerTests_Retry` (3 Facts): bewusste Port-Kollisionen + Parallelstarts.
- Alle `*MockServerTests_*`/`SearchMockServerTestBase`-Nachfahren der Integration
  (~13 Klassen): 3–4 `HttpListener` je Testklasse. Bereits entschärft:
  `LoopbackHttpListener` wählt freien Port + 10× Retry bei `HttpListenerException`.
- **Unit-Projekt**: `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`,
  `OpenRouteServiceKeyCheckerTests` (echter `HttpListener`).
- Einordnung laut Anforderung offen (Offene Frage 2): isoliert+Retry vs. trotzdem
  Port-Kollisions-/Firewall-anfällig.

### D. Reales Dateisystem (Temp-Verzeichnisse, Dateisperren)

- `TestDataContext`/`TestDatabase` (Integration): reale Verzeichnisse unter
  `%TEMP%`/LocalApplicationData; `Cleanup` ohne Fehlerbehandlung.
- `BackupTestEnvironment` + 14 `BackupServiceTests_*`-Klassen (Integration, serialisiert
  über `BackupCollection`) — echte SQLite-Dateien, WAL, Datei-Moves.
- Unit: `TempDirectory`-Nutzer, `BackupCryptoTests`, `HttpTileSourceTests_*` (4 Dateien),
  `ReceiptPhotoStoreTests_*` (2 Dateien) — Temp-Verzeichnisse + `Directory.Delete` im Dispose.
- `E2ETestBase`/`DiagnosticsCaptureE2ETests` (E2E) — Temp-Verzeichnisse, Dateisperren
  durch den App-Prozess (derzeit nur `IOException`-Catch bzw. kein Catch).
- NTFS-Junctions/Reparse-Points: kein Vorkommen.

## Deadline-Matrix (Host-Poll vs. Kind-/Phasen-Budget)

| Host-Warteort | Host-Frist | Kind-/Gegen-Budget | Befund |
|---------------|-----------|--------------------|--------|
| `E2ETestBase.WaitForTab` (Z. 143) | 30 s | UIA-Timeout je Call: 60 s CI / 15 s lokal | **Host < Kind in CI** — Referenzmuster |
| `E2ETestBase.NavigateToTab` (Z. 140) | 5×5 s ≈ 25 s | UIA-Timeout 60 s CI | **Host < Kind in CI** |
| `E2ETestBase.WaitForPageTitle` (Z. 196) | 30 s | UIA-Timeout 60 s CI | **Host < Kind in CI** |
| `E2ETestBase.SkipSetup` (Z. 538, 560) | 30 s / 6 s je Versuch | UIA-Timeout 60 s CI | **Host < Kind in CI** |
| `SettingsE2ETestBase.DefaultTimeout`/`WaitUntil` (Z. 18) | 20 s | UIA-Timeout 60 s CI | **Host < Kind in CI** |
| `E2ETestBase.StartAndConnect` → `GetMainWindow` (Z. 467–476) | `max(uiaTimeout,30 s)` je Call; `totalLimit` = max(60 s, 2×UIA) | UIA-Timeout 60 s CI | konsistent (Host ≥ Kind) |
| `E2ETestBase.CloseApplicationAndWait` (Z. 364–368) | `WaitForExit` 15 s, Kill + 10 s | – | feste Fristen, kein Kind-Budget |
| `ApiKeyBuildRunner.Build` (Z. 115) | 5 min Kind-Budget | CI-Schritt ohne eigenes Timeout; Job 45 min | kein unmittelbarer Konflikt; Schritt-Kill würde Prozess ohne Prozessbaum-Kill zurücklassen |
| `TransientRetry` (Diverses) | `totalLimit`/`maxPause` parametrisiert | – | bereits Budget-bewusst |
| `TransientWait.Until` | Frist per Aufruf | – | siehe oben |
| `SearchE2ETests_LargeResult` | 5 s–120 s diverse | Messzeit minus UIA-Wartezeit (`timeoutTime`-Überladung) | bereits gehärtet |
| Mock-Server `Dispose` | `_loop.Wait(2 s)` | – | kurze, unkritische Frist |
| CI-Job `build-and-test` | `timeout-minutes: 45` | Summe aller drei Suiten | der einzige Job-Timeout |

## Cleanup-Stellen ohne Retry/Backoff (`Directory.Delete`)

| Stelle | Fehlerbehandlung heute |
|--------|------------------------|
| `E2ETestBase.Dispose` (Z. 295–305) | fängt nur `IOException`, kein Retry |
| `TestDataContext.Cleanup` (Z. 37–43) | **keine** — Fehler kann Assert überdecken |
| `ApiKeyBuildRunner.Dispose` (Z. 160–174) | `IOException`+`UnauthorizedAccessException`, kein Retry |
| `TempDirectory.Dispose` (Unit, Z. 33–43) | nur `IOException`, kein Retry |
| `DiagnosticsCaptureE2ETests` (Z. 38–41) | **keine** (`finally`-Löschung) |
| `TileSourceMockServerTests_Flow` (Z. 30) | Kontext zu prüfen — `Directory.Delete` vorhanden |
| `BackupCryptoTests` (Z. 23), `HttpTileSourceTests_*` (4×), `ReceiptPhotoStoreTests_*` (2×) | jeweils `Directory.Delete` im Dispose (Try-Catch variiert) |
| Prozess-Kill: `CloseApplicationAndWait`, `DiscardFailedStart`, `ApiKeyBuildRunner` | `Kill(entireProcessTree: true)` bereits vorhanden |

Invariante der Anforderung: Cleanup-Fehler darf Assert nie überdecken — verletzt
potenziell bei `TestDataContext.Cleanup`, `DiagnosticsCaptureE2ETests` und allen
Dispose-Pfaden ohne Catch.

## Trait-/Category-Nutzung und Retry-/Quarantäne-Konvention

- **Keine** xUnit-Traits im gesamten Repository (kein `[Trait]`, keine Kategorie-Filter,
  kein `--filter` in Workflows/Skripten/Hooks).
- Kein `Flaky`-/Retry-Konzept auf Testfallebene; existierende Retries sind
  infrastrukturell (`TransientRetry` UIA, `LoopbackHttpListener` Port, `E2EStartupPolicy`
  Neustart, `NavigateToTab`/`SkipSetup` Versuche).
- Kein Meta-Test für Trait-Konventionen; als Muster geeignet: `E2EInputPolicyTests_NoFocus`
  (Quelltext-Konvention), `FlakyConventionTests` der Blaupause (Reflexion).
- Kein `Skip`-/`Trait`-gesteuertes Deaktivieren von Tests; `DisableTestParallelization`
  nur im E2E-Projekt; Sammlungen: `EnvironmentCollection` (Unit), `BackupCollection` (Integration).
