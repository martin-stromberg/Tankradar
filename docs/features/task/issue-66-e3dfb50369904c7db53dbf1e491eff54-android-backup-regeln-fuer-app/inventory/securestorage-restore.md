# SecureStorage und Wiederherstellung

## SecureStorage-Nutzung in der App

Einzige Umsetzung auf Basis von `SecureStorage` ist `SecureStorageApiKeyStore` (`src/Tankradar.MAUI/Services/Pricing/SecureStorageApiKeyStore.cs`) hinter der Schnittstelle `IApiKeyStore` (`Services/Pricing/ApiKeys.cs:10–30`). Unter Windows wird stattdessen `Platforms/Windows/CredentialLockerApiKeyStore` registriert (`MauiProgram.cs:319–327` bzw. `:244–256`); für Android und iOS gilt der `#else`-Pfad → `SecureStorage`.

Drei Schlüssel (`SecureStorageApiKeyStore.cs:11, 16, 21`):

- `tankatlas.price-api-key` — Tankerkönig-Preisschlüssel. Wird nicht nur vom Anwender gesetzt: `ApiKeyProvider.GetApiKeyAsync` (`ApiKeys.cs:92–131`) schreibt den beim Build mitgegebenen Schlüssel (`TankerkoenigApiKey`-Assembly-Metadatum) in die Ablage, sobald er vom gespeicherten abweicht (`:117–128`). Der Build-Schlüssel ist maßgeblich — nach einem Gerätewechsel ohne lesbare Ablage funktioniert die Preisabfrage also dennoch.
- `tankatlas.routing-api-key` und `tankatlas.routing-enabled` — anwenderseitiger OpenRouteService-Schlüssel und der Schalter „Routensuche aktivieren“, beide über `RouteSearchAccess` (`Services/Routing/RouteSearchAccess.cs`), Registrierung `MauiProgram.cs:241–257`.

Im Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt) werden die echten Ablagen nie erzeugt: `ApiKeyStoreSelector.Create` liefert `InMemoryApiKeyStore` (`ApiKeys.cs:166–181`), `RouteSearchAccess.Create` ebenfalls (`RouteSearchAccess.cs:99–123`).

## Fehlertoleranz bei unlesbaren Einträgen (relevant für „wiederhergestellt, aber unbrauchbar“)

Die Aufrufer tolerieren eine defekte Ablage bereits:

- `ApiKeyProvider.GetApiKeyAsync` fängt jede Ausnahme von `_store.GetAsync()` ab, protokolliert nur und fährt mit `stored = null` fort — anschließend greift der Build-Schlüssel oder „kein Schlüssel“ (`ApiKeys.cs:101–114`).
- `RouteSearchAccess.GetStoredKeyAsync` fängt Ausnahmen ab und liefert `null` (`RouteSearchAccess.cs:132–145`); `IsSwitchedOnAsync` fängt Ausnahmen ab und liefert `false` (`:227–237`). Folge: ein wiederhergestellter, unlesbarer `SecureStorage`-Eintrag wirkt wie „kein Schlüssel“ — die Routensuche bleibt aus, kein Absturz. Die Schnittstellen-Dokumentation sagt das bereits voraus: „…nach einer Wiederherstellung ohne Schlüssel ist die Routensuche daher aus“ (`RouteSearchAccess.cs:10–12`).
- `SecureStorageApiKeyStore` selbst fängt nichts ab (`:37, :43, :49`) — die Toleranz sitzt eine Ebene höher; `Remove`/`Set` bei korruptem Eintrag könnten je nach Plattformverhalten dennoch fehlschlagen und sind nur in `SetEnabledAsync`/`SaveKeyAsync`/`RemoveKeyAsync` nicht ausdrücklich abgefangen (Ausnahmen würden bis zur Oberfläche laufen).

## Plattformverhalten (Einordnung für die Entscheidung)

- **Android:** MAUI `SecureStorage` speichert AES-GCM-verschlüsselte Werte in einer SharedPreferences-Datei; der Verschlüsselungsschlüssel steckt im Android Keystore des Geräts. Bei Auto-Backup/Gerätetransfer wandert die Datei mit (in `shared_prefs/`, s. [datenablage.md](datenablage.md)), der Keystore-Schlüssel jedoch nicht — die Einträge sind auf dem neuen Gerät unentschlüsselbar. Das exakte Verhalten von `GetAsync` auf korrupte Einträge (Ausnahme vs. `null`) ist geräte-/versionsabhängig und nicht geräteerprobt; die o. g. try/catch-Ebenen fangen beide Varianten funktional ab.
- **iOS:** Einträge liegen in der Keychain mit Zugriffsklasse `AfterFirstUnlock`, nicht iCloud-synchronisiert, können aber in verschlüsselten Gerätesicherungen enthalten sein — dokumentiert in `docs/help/Datenschutz/beschreibung.md` (Abschnitt „Schlüssel in der Keychain“, Zeilen ~33–36) und ADR 0015 (`docs/adr/0015-ios-delivery-and-privacy.md:20`); `SecureStorageApiKeyStore.cs:19` spiegelt das im Kommentar zu `RoutingEnabledStorageKey`.
- **Windows:** Credential Locker, gerätegebunden, nicht Teil irgendeines Backups.

## Eigene Wiederherstellung (Restore) vs. System-Restore

Die app-eigene Wiederherstellung (`BackupService.RestoreAsync` → `RestoreSwap.Execute`) tauscht ausschließlich `tankatlas.db` (samt `-wal`/`-shm`) und `receipts/` im Datenverzeichnis aus (`Services/Backup/RestoreSwap.cs:84, 171`; Ablauf in `docs/help/Sicherung/ablauf-technisch.md:36–47`). `SecureStorage`/`Preferences` und `backups/` bleiben unberührt — die API-Schlüssel überleben eine Wiederherstellung, sie sind bewusst **nicht** Inhalt der Sicherung (ADR 0011, „Umfang: Was nicht gesichert wird“, `docs/adr/0011-local-backup-restore.md:38–45`; `docs/help/Sicherung/beschreibung.md` Abschnitt „Was eine Sicherung enthält“). Das Issue-Thema betrifft also nicht den eigenen Restore, sondern den Android-System-Restore (Auto-Backup/D2D-Transfer), bei dem die SecureStorage-Datei stumm mitkommen würde.

## Dokumentationsstand

- Dokumentiert ist das Verhalten für iOS (Keychain, Gerätesicherung) und Windows (Credential Locker): `docs/help/Datenschutz/beschreibung.md`, `docs/help/Datenschutz/ablauf-technisch.md:12`, ADR 0014/0015.
- **Android fehlt in dieser Dokumentation vollständig** — weder das Ablageverhalten (Keystore-verschlüsselte SharedPreferences) noch das Verhalten nach einem System-Restore ist beschrieben. Das Akzeptanzkriterium „Verhalten definiert und getestet oder dokumentiert“ wäre mit einer Dokumentationsergänzung (Datenschutz-Hilfe und/oder ADR, ggf. Kommentar in `SecureStorageApiKeyStore`) am ehesten erfüllbar; ein echter Gerätetest scheitert an der fehlenden Android-Test-Infrastruktur (kein Emulator-Job, kein Device-Farm-Anschluss, alle Testprojekte laufen auf Windows, s. [tests-und-doku.md](tests-und-doku.md)).
