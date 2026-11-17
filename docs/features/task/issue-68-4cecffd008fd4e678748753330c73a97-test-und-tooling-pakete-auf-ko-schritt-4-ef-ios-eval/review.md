# Review – Schritt 4: EF-/iOS-Evaluation (#71)

## Gesamtergebnis

**Anforderung erfüllt, keine offenen Befunde.**

Details: [review-code.md](review-code.md), [review-usability.md](review-usability.md), [test-results.md](test-results.md).

## Akzeptanzkriterien #71

| Kriterium | Erfüllt durch |
|-----------|---------------|
| Compiled Model evaluiert | `dotnet ef dbcontext optimize` real ausgeführt (generierbar, 12 Dateien) und verworfen |
| Aktuelle Guidance für iOS AOT/Trimming/Reflection geprüft | ADR 0021, Abschnitt „Guidance-Abgleich“ |
| Messung wo machbar | Modellaufbau ~296 ms / Migration ~201 ms (Windows Release); Paketgröße iOS als nicht lokal messbar dokumentiert |
| ADR mit Entscheidung + Risiken | `docs/adr/0021-ef-compiled-model-und-ios-aot.md` |
| Migrationen/Restore/Integrität getestet | Integrations-Suite 311/311 grün, unverändert |
