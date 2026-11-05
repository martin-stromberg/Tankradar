// Prüft die Zeilenabdeckung aus der ReportGenerator-TextSummary gegen eine Mindestschwelle.
// Aufruf: node scripts/check-coverage.mjs <Summary.txt> [Schwelle in Prozent, Standard 70]
import { readFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

/** Liest den Wert hinter "Line coverage:" (Punkt oder Komma als Dezimaltrenner). */
export function parseLineCoverage(summaryText) {
  const match = /Line coverage:\s*([0-9]+(?:[.,][0-9]+)?)/.exec(summaryText);
  if (!match) {
    throw new Error("Could not determine line coverage from the generated report.");
  }
  return Number(match[1].replace(",", "."));
}

export function meetsThreshold(coverage, threshold) {
  return coverage >= threshold;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const [summaryPath, thresholdArg] = process.argv.slice(2);
  if (!summaryPath) {
    console.error("Usage: node scripts/check-coverage.mjs <Summary.txt> [threshold]");
    process.exit(2);
  }
  const threshold = Number(thresholdArg ?? process.env.COVERAGE_THRESHOLD ?? 70);
  const coverage = parseLineCoverage(readFileSync(summaryPath, "utf8"));
  console.log(`Line coverage: ${coverage}% (threshold: ${threshold}%)`);
  process.exit(meetsThreshold(coverage, threshold) ? 0 : 1);
}
