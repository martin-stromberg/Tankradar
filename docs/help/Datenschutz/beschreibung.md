← [Zurück zur Übersicht](index.md)

# Datenschutz — Beschreibung und Angaben für den App Store

## Zweck

Dieses Dokument bereitet die Datenschutzangaben vor, die App Store Connect für Tankatlas verlangt (App-Datenschutz, „Privacy Nutrition Labels“, Export-Compliance, Berechtigungstexte). Es beschreibt den tatsächlichen Datenfluss der App. Die endgültigen Eingaben in App Store Connect nimmt der Inhaber vor und gleicht sie vor der Einreichung mit den dann geltenden Vorgaben von Apple ab; die Formulierungen hier sind eine Vorlage, kein Ersatz dafür.

## Datenfluss: Was geht wohin?

| Zweck | Empfänger | Übermittelte Daten | Gespeichert vom Entwickler? |
|-------|-----------|--------------------|-----------------------------|
| Tankstellen und Preise in der Nähe, Routensuche (Tankstellen entlang der Route), Preise der Favoriten, Tankstellendetails | Tankerkönig (Daten: MTS-K) | Koordinaten des Suchmittelpunkts bzw. der Routenabschnitte und Radius; bei Favoritenpreisen und Detailansicht die Kennungen der betreffenden Tankstellen (ohne Bezug zum Anwender); jeweils zusammen mit dem in die App eingebauten Zugangsschlüssel; technisch bedingt die IP-Adresse | Nein (kein eigener Server) |
| Adress-, Orts- und PLZ-Suche, Start und Ziel einer Route | Nominatim (OpenStreetMap) | eingegebener Suchtext, technisch bedingt die IP-Adresse | Nein |
| Routenberechnung | OpenRouteService (HeiGIT), nur wenn die Routensuche aktiviert ist | Start- und Zielkoordinaten, mit dem **eigenen** Schlüssel des Anwenders | Nein |
| Kartendarstellung | OpenStreetMap-Kachelserver | Kachelkoordinaten des sichtbaren Kartenausschnitts, technisch bedingt die IP-Adresse | Nein |

**Nicht** übertragen werden: Fahrzeuge, Tank- und Ladevorgänge, Belegfotos, die Favoritenliste als solche (übertragen werden nur die Tankstellenkennungen für die Preisabfrage, siehe oben), Notizen, Einstellungen, Sicherungen und Exporte. Es gibt keine Analyse-, Absturzberichts- oder Werbe-Bausteine und keine Verfolgung über Apps oder Websites hinweg.

## Standort

- Berechtigung: nur „Beim Verwenden der App“ (`NSLocationWhenInUseUsageDescription`). Kein Hintergrundstandort.
- Verwendung: Suche „In der Nähe“ und – wenn der Anwender es wählt – aktueller Standort als Startpunkt einer Route.
- Der Standort bleibt im Arbeitsspeicher, wird nicht gespeichert und nicht protokolliert. Lehnt der Anwender ab, bleibt die Adresssuche nutzbar.
- Text im Berechtigungsdialog: „Zur Ermittlung von Tankstellen in deiner Nähe und – wenn du es wählst – als Startpunkt einer Route.“

## Kamera und Fotos

Kamera und Fotoauswahl dienen ausschließlich dem Belegfoto eines Tank- oder Ladevorgangs. Die Fotos liegen im App-Datenverzeichnis, mit Dateischutz, und gehören auf Wunsch zur lokalen Sicherung. Der Schlüssel `NSPhotoLibraryUsageDescription` bleibt gesetzt, weil die Foto-Auswahl des Betriebssystems ihn beim Speichern bzw. Auswählen verlangen kann und ein fehlender Schlüssel die App beendet; ob er auf iOS 16 und neuer entbehrlich ist, ist auf einem Gerät zu prüfen (offen, siehe ADR 0015).

## Schlüssel in der Keychain – korrekte Formulierung

Der Routing-Schlüssel des Anwenders (und der Tankerkönig-Schlüssel nach dem ersten Start) liegt in der **Keychain des Geräts** (iOS) bzw. im **Credential Locker** (Windows). Richtig ist:

- Die Einträge sind **nicht in der Sicherung der App** (ZIP) und **nicht im Build** enthalten.
- Die App legt sie über die MAUI-Abstraktion `SecureStorage` mit der Zugriffsklasse **AfterFirstUnlock** an. Sie sind **nicht** mit iCloud synchronisiert, **können** aber in **verschlüsselten Gerätesicherungen** (Finder/iTunes) enthalten sein und mit einer solchen Sicherung auf ein anderes Gerät wandern.
- Die Bezeichnung „gerätelokal“ ist deshalb zu ungenau und wird nicht mehr verwendet. Die Zugriffsklasse wurde bewusst nicht auf „ThisDeviceOnly“ geändert (siehe ADR 0015).

## Vorlage für die Angaben in App Store Connect

**App-Datenschutz (Datenarten):**

| Datenart | Angabe | Verwendungszweck | Mit Identität verknüpft | Zum Tracking verwendet |
|----------|--------|------------------|--------------------------|------------------------|
| Genauer Standort | gesammelt (an Drittanbieter-Dienste übermittelt, vom Entwickler nicht gespeichert) | App-Funktionalität | Nein | Nein |
| Suchtexte (Adressen, Orte, Routenziele) | an Nominatim bzw. OpenRouteService übermittelt, vom Entwickler nicht gespeichert | App-Funktionalität | Nein | Nein |
| alle übrigen Datenarten (Kontakt, Gesundheit, Finanzen, Nutzerinhalte, Kennungen, Nutzungsdaten, Diagnose) | nicht gesammelt | — | — | — |

Hinweis zur Einordnung: Nach Apples Definition gilt ein Abruf nur dann als „Sammeln“, wenn die Daten länger als zur Bearbeitung der Anfrage zugänglich bleiben. Die Dienste Dritter (Tankerkönig, Nominatim, OpenRouteService) gelten als Partner der App; die Angabe „genauer Standort, App-Funktionalität, nicht verknüpft, kein Tracking“ stimmt mit dem Privacy-Manifest überein und ist die vorsichtigere Variante. Bei der Eingabe in App Store Connect ist die jeweils aktuelle Fassung der Apple-Hinweise zu beachten.

**Tracking:** Nein (`NSPrivacyTracking` = false, keine Tracking-Domains).

**Export-Compliance:** `ITSAppUsesNonExemptEncryption` = false in der Info.plist. Die App nutzt ausschließlich die Standard-Verschlüsselung des Betriebssystems (HTTPS/TLS, Keychain, Dateischutz) und keine eigene oder nicht freigestellte Verschlüsselung. Die Frage in App Store Connect entfällt damit.

**Datenschutzerklärung (URL):** App Store Connect verlangt eine öffentlich erreichbare Datenschutzerklärung. Sie ist noch zu erstellen und zu veröffentlichen (Verantwortlicher ist der Inhaber, nicht eine Marke); der Inhalt der Abschnitte oben ist die Grundlage. Offen: Der Inhaber nennt die URL.

## Dateischutz

Datenbank, Belegfotos, Sicherungen und Exportdateien erhalten unter iOS den Dateischutz `CompleteUntilFirstUserAuthentication`: Sie sind nach dem ersten Entsperren nach dem Start lesbar (damit Hintergrundzugriffe wie ein App-Update nicht scheitern), vorher verschlüsselt.

← [Zurück zur Übersicht](index.md)
