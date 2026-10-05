# Anforderung (Schritt 7: Suche nach Adresse, Ort oder PLZ)

## Wörtliche Beschreibung

Neben der Suche am aktuellen Standort soll die Tankstellensuche eine Suche nach
einer eingegebenen Adresse, einem Ort oder einer Postleitzahl bieten. Der Nutzer gibt den
Suchbegriff ein und legt den Radius fest (5–50 km). Die App wandelt die Eingabe über
OpenStreetMap-Nominatim in eine Position um, auf iOS und Windows gleichermaßen. Die Tankstellen im
Umkreis erscheinen in derselben Ergebnisliste wie bei der Standortsuche: Preise der gewählten
Spritsorten mit Altersangabe, Entfernung zur gesuchten Adresse, Zusatzhinweise, Sortierung nach
Preis, Entfernung oder Name sowie Spritsortenfilter.

Die Nutzungsrichtlinie von Nominatim wird eingehalten: Eine Anfrage geht nur bei ausdrücklichem
Absenden der Suche hinaus (keine Autovervollständigung), höchstens eine pro Sekunde, mit einer
Kennung, die die App identifiziert. Die Quellenangabe für OpenStreetMap wird angezeigt. Eingaben
werden validiert, bevor sie an den externen Dienst gehen. Nicht auffindbare Adressen und fehlende
Verbindung werden verständlich gemeldet. Eingegebene Adressen und daraus ermittelte Positionen
werden nicht dauerhaft gespeichert. Die Adresssuche braucht kein GPS und funktioniert auch bei der
Einstellung „GPS-Nutzung: Nie". Tests decken Validierung, Adressauflösung gegen einen
Test-/Mock-Dienst, Einhaltung der Anfragebegrenzung und per FlaUI den Ablauf der Adresssuche ab.
Nach Abschluss wird ein startfähiger Windows-Zwischenstand unter
`review-versions/<Version>_<JJJJ-MM-TT>/` im Repository-Root abgelegt (nicht committen).

## Verbindliche Projektvorgaben (haben Vorrang bei Widerspruch)

- Suchradius 1–25 km über Chips 1/2/5/10/15/25 (Projektplan-Entscheidung, Quelle Tankerkönig max. 25 km);
  die Angabe „5–50 km“ in der Beschreibung wird dadurch ersetzt (die Radiuschips der Standortsuche gelten auch hier).
- Designentwurf verbindlich (Suchfeld „Adresse, PLZ oder Ort“ mit Löschen-Schaltfläche).
- Tests nie gegen produktive Endpunkte: Mock für Nominatim.
- Keine Roh-Koordinaten und keine Adresseingaben persistieren oder protokollieren.

## Anforderungen

1. Suchmodus „Aktueller Standort“ / „Adresse“ auf der Seite „Karte“.
2. Adressmodus: Eingabefeld, Löschen, Absenden per Schaltfläche „Suchen“ oder Enter; keine Autovervollständigung.
3. Eingabevalidierung vor dem Aufruf (Länge, Zeichen).
4. Nominatim-Client: HTTPS, identifizierender User-Agent, Drosselung 1 Anfrage/s, ein Treffer, Zeitlimit, keine Wiederholung.
5. Quellenangabe „© OpenStreetMap-Mitwirkende“ sichtbar im Adressmodus.
6. Verständliche Meldungen: nicht gefunden, offline, Dienst nicht erreichbar/abgelehnt, ungültige Eingabe.
7. Ergebnisliste/Filter/Sortierung identisch zur Standortsuche; Entfernung zur gesuchten Adresse.
8. Unabhängig von der GPS-Einstellung „Nie“.
9. Testmodus: Nominatim-Endpunkt nur über Umgebungsvariable (Mock); ohne Angabe im Testmodus wird verweigert.
10. Tests: Unit, Integration (Mock), E2E (FlaUI); Doku, README, Zwischenstand 0.1.10.
