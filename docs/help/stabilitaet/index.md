← [Zurück zur Dokumentation](../index.md)

# Teststabilität

Konventionen für Tests, die echte Betriebssystem-Schnittstellen berühren (UI-Automation, echte
Prozessstarts, Loopback-Listener): Klassifizierung als `Category=OsInterface`, getrennte
best-effort-Lane, `Flaky`-Markierung mit einmaligem automatischem Retry, Flake-Statistik und
Quarantäne-Prozess.

## Inhalt

- [OS-Schnittstellen-Tests](os-interface-tests.md) — Klassifizierungskriterium, Kategorien
  `OsInterface`/`Flaky`, Retry-/Rerun-/Quarantäne-Konvention, lokale Befehle, bekannte
  Fehlerbilder und Audit-Heuristik.
