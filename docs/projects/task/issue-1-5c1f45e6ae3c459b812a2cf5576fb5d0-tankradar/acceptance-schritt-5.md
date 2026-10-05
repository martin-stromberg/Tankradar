# Abnahmeprüfung – Entwicklungsschritt 5

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Abweichung aus `acceptance-schritt-5.1.md` (rotierter Build-Schlüssel) – behoben:**
- `ApiKeyProvider.GetApiKeyAsync()` (`src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs`) macht den Build-Schlüssel maßgeblich. Weicht er vom gespeicherten Schlüssel ab oder fehlt dort einer, schreibt die App ihn in die sichere Ablage (Credential Locker bzw. Keychain) und verwendet ihn. Ein Update mit neuem oder rotiertem Schlüssel kommt dadurch ohne Zutun des Anwenders an. Enthält der Build keinen Schlüssel, nutzt die App weiter den gespeicherten. Ist die Ablage nicht lesbar oder nicht beschreibbar, verwendet sie trotzdem den Build-Schlüssel.
- Tests in `ApiKeyProviderTests_Resolution`: `DifferentBuildKey_ReplacesStoredKey` (genau ein Schreibvorgang, danach stabil), `SameKey_IsNotRewritten`, `StoredKeyWithoutBuildKey_IsUsed`, `UnreadableStore_BuildKeyIsWritten`, `StoreFailures_FallBackToBuildKey` und `ReadFailureWithoutBuildKey_ReturnsNull`. Die Logik ist korrekt und ausreichend getestet.
- Die Hilfe (`docs/help/Preisdaten/api-schluessel.md`) beschreibt das neue Verhalten. Eine kleine Ungenauigkeit gibt es: Dort steht „Beim Start vergleicht die App …“. Tatsächlich vergleicht sie beim ersten Bedarf des Schlüssels, also beim Öffnen der Optionen oder beim ersten Abruf. Fachlich ändert das nichts.

**Hinweise aus `acceptance-schritt-5.1.md` – behoben:**
- Fail Secure im Testmodus: Fehlt `TANKRADAR_PRICE_API_URL` oder ist die Adresse nicht auswertbar, setzt `PriceApiOptions.FromEnvironment` das Kennzeichen `EndpointNotConfigured`. `TankerkoenigClient.GetAsync` bricht dann vor der Schlüsselermittlung und vor jeder HTTP-Anfrage mit `PriceFailure.EndpointNotConfigured` ab. `FuelPriceService` behandelt das wie jeden anderen `PriceApiException`-Fehler: Suche und Details liefern die zuletzt bekannten Preise, die Verbindungsprüfung meldet den Grund. Abgedeckt ist das durch `PriceApiOptionsTests_Validation` (Testmodus ohne URL setzt das Kennzeichen, Normalbetrieb nicht) und `TankerkoenigClientTests_EndpointNotConfigured` (Suche und Details ohne Anfrage).
- Isolierte Ablage: `ApiKeyStoreSelector.Create` gibt im Testmodus einen `InMemoryApiKeyStore` zurück. Die echte Ablage wird dabei nicht einmal erzeugt. `MauiProgram` ist für Windows und iOS entsprechend verdrahtet. Außerdem ignoriert `ApiKeyProvider` im Testmodus den Build-Schlüssel ganz und gibt nur `TANKRADAR_PRICE_API_KEY` zurück. Damit besteht ein doppelter Schutz. Abgedeckt ist das durch `ApiKeyStoreSelectorTests_TestMode` sowie `TestModeWithoutEnvironmentKey_IgnoresBuildKey` und `TestMode_UsesEnvironmentKeyWithoutStoring`.
- Regressionen: keine gefunden. Normalbetrieb ohne Testmodus: produktiver HTTPS-Endpunkt, Umgebungsschlüssel wird ignoriert (`EnvironmentKeyOutsideTestMode_IsIgnored`), Ablage ist die des Betriebssystems.

**Erkennung des Testmodus (`TEST_DATA_PATH`):**
- Testmodus gilt, wenn die Prozess-Umgebungsvariable `TEST_DATA_PATH` nicht leer ist. Alle drei Stellen prüfen das mit `IsNullOrWhiteSpace`: `AppDataPathProvider`, `PriceApiOptions` und `ApiKeys`. Datenbankpfad, Endpunkt und Schlüsselablage schalten also immer gemeinsam um. Die Mechanik für das Datenverzeichnis besteht schon seit den früheren Schritten.
- iOS: Eine reguläre Installation aus TestFlight oder dem App Store kann nicht versehentlich in den Testmodus geraten. Apps übernehmen dort keine Umgebungsvariablen des Benutzers. Setzen lassen sie sich nur über Xcode bzw. `simctl`.
- Windows: Eine reguläre Installation gerät nur in den Testmodus, wenn `TEST_DATA_PATH` dauerhaft als Benutzer- oder Systemvariable gesetzt ist. Weil der Name sehr allgemein ist, könnte das auf Entwicklerrechnern auch ein fremdes Werkzeug tun. Folgen: Die App verwendet die Datenbank aus diesem Verzeichnis, lädt keine Preise und nutzt den Build-Schlüssel nicht. Unsicher wird sie dadurch nicht, weil sie den Abruf verweigert, statt etwas Falsches anzusprechen. Die Optionen zeigen außerdem eine klare Meldung mit Variablennamen. Für Endanwender ist das Risiko gering. Ich bewerte es deshalb nicht als Abweichung. Als Verbesserung bietet sich ein produktspezifischer Name an (z. B. `TANKATLAS_TEST_DATA_PATH`) oder ein Testmodus nur in Debug-Builds.

**Nicht umgesetzte Review-Hinweise:**
- Kein Test für den Meldungstext zu `EndpointNotConfigured`: Die Anforderung verlangt nur, dass Fehlerzustände nicht zu unsicherem Verhalten führen. Die Verweigerung selbst ist auf Client-Ebene getestet. Den Text habe ich in der App praktisch bestätigt (siehe unten). Die Anforderung ist davon nicht berührt.
- Kein Test der Verdrahtung in `MauiProgram.cs`: Die Auswahllogik ist als eigene Einheit (`ApiKeyStoreSelector`) getestet, die Verdrahtung ist nur eine Zeile je Plattform. Am Windows-Zwischenstand habe ich praktisch bestätigt, dass kein Eintrag in den Credential Locker geschrieben wird. Auch das berührt die Anforderung nicht. Ein Rauchtest, der den DI-Container im Testmodus auflöst, wäre trotzdem nützlich.

**Technische Beobachtungen (kein Abnahmehindernis):**
- Der Test `ReadBuildTimeKey_WithoutBuildSetting_ReturnsNull` prüft nur `value is null || value.Length > 0`. Sein Name verspricht mehr, als er prüft.
- Der Testmodus samt Endpunkt-Überschreibung ist auch in Release-Builds enthalten. Ein anderer Host als Loopback ist nur über HTTPS erlaubt. Wer die Umgebung des Prozesses steuern kann, kann die App aber auf einen eigenen HTTPS-Dienst lenken. Den echten Schlüssel bekommt dieser Dienst nicht, weil im Testmodus nur der Umgebungsschlüssel verwendet wird.
- Die unter `acceptance-schritt-5.1.md` positiv bewerteten Punkte gelten unverändert. Dazu gehören der Weg des API-Schlüssels, HTTPS, Zeitlimit, Wiederholung, Drosselung, Cache, Speicherung mit Zeitstempel, Aktualität (60 Minuten), abgeleitete Hinweise, Offline-Rückfall, Verbindungserkennung, Quellenangabe, Migration und `TransientRetry` für die E2E-Robustheit. Der Diff seit dem Basis-Branch bestätigt das.

**Praktische Verifikation:**
- `scripts/local-ci.ps1` (mit E2E): Exit-Code 0, alle Schritte OK. Unit-Tests 204/204, Integrationstests 33/33, Zeilenabdeckung 94,1 %, FlaUI-E2E 12/12, iOS-Compile-Prüfung OK.
- `scripts/test-ios-deployment.ps1`: „iOS-Deployment-Pruefung erfolgreich.“ (Exit-Code 0).
- E2E-Projekt 3-mal hintereinander: jeweils 12/12 bestanden, keine UIA-Timeouts.
- Windows-Zwischenstand `review-versions/0.1.6_2026-10-05/` (`CHANGELOG.md` vorhanden, Build-Schlüssel-Metadatum leer). Gestartet mit `TEST_DATA_PATH` auf ein temporäres Verzeichnis unter `%TEMP%`, ohne `TANKRADAR_PRICE_API_URL` und ohne `TANKRADAR_PRICE_API_KEY`. Optionen → „Datenquelle“ zeigt „Daten: Tankerkönig / MTS-K“, den Lizenztext (CC BY 4.0) und „API-Schlüssel: nicht hinterlegt“. „Verbindung zum Preisdienst prüfen“ meldet: „Testmodus ohne Preisdienst-Adresse (TANKRADAR_PRICE_API_URL): Der Abruf wird nicht durchgeführt.“ Der Prozess hatte keine TCP-Verbindungen außer Loopback, es ging also keine Anfrage an den echten Dienst. Die Datenbank lag ausschließlich im Testverzeichnis. Danach habe ich die App per Prozessname `Tankradar.MAUI` beendet und das Verzeichnis gelöscht. Es läuft kein `Tankradar.MAUI` mehr.
- Windows Credential Locker: Vor und nach allen Läufen (CI, 3 E2E-Läufe, Zwischenstand) gab es keinen Eintrag mit der Ressource `Tankatlas/PriceApi`. Geprüft habe ich das nur lesend über `PasswordVault.FindAllByResource`, ohne einen Inhalt auszugeben.
- Kein echter Tankerkönig-Endpunkt wurde aufgerufen.
