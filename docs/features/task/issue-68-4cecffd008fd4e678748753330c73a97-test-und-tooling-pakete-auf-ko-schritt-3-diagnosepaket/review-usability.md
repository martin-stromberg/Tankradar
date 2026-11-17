# Usability-Review – Schritt 3: Opt-in-Diagnosepaket (#70)

## Änderung der Oberfläche

Neue Karte „Diagnose“ in den Optionen (vor der Karte „Datenquelle“):
Überschrift, Erklärtext, Datenschutzhinweis (was **nicht** enthalten ist),
Schaltfläche „Inhalt anzeigen“ (Vorschau), Vorschau-Bereich mit
Höhenbegrenzung, Schaltfläche „Paket erstellen und teilen“ (erst nach
Vorschau aktiv), Statusmeldung.

## Bewertung

- **Transparenz vor Aktion:** Der Anwender sieht Zweck, Inhalt und die
  ausgeschlossenen Daten, bevor überhaupt eine Datei entsteht. Die
  Teilen-Schaltfläche ist bis zur Vorschau deaktiviert – der Opt-in-Gedanke
  ist erzwungen, nicht nur beschrieben.
- **Konsistenz:** Gleiche Karten-/Abstands-/Textstile wie die übrigen
  Optionen-Karten (`SettingsCard`, `space-sm`, `body-sm`/`headline-md`),
  Outline-Schaltfläche für die Vorschau, gefüllte Schaltfläche für die
  Hauptaktion – wie „Sicherung erstellen“.
- **Barrierefreiheit:** Überschrift mit `HeadingLevel="Level2"`,
  alle Elemente mit AutomationIds; Statusmeldung als eigenes Label.
  Der Vorschautext ist scrollbar und höhenbegrenzt (240), damit die Karte
  die Seite nicht sprengt.
- **Verständlichkeit:** Deutsche, unverfängliche Formulierungen; das Paket
  wird als „kleine Datei mit technischen Hinweisen“ beschrieben; klar, dass
  die App selbst nichts überträgt.
- **Fehlerfall:** Fehlgeschlagene Erzeugung/Weitergabe zeigt eine
  verständliche Statusmeldung statt einer Ausnahme.

## Befunde

Keine.

## Ergebnis

**Keine Befunde.**
