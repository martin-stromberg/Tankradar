import assert from "node:assert/strict";
import { test } from "node:test";
import { meetsThreshold, parseLineCoverage } from "./check-coverage.mjs";

test("parseLineCoverage liest Punkt und Komma als Dezimaltrenner", () => {
  assert.equal(parseLineCoverage("  Line coverage: 73.2%\n  Covered lines: 41"), 73.2);
  assert.equal(parseLineCoverage("  Line coverage: 73,2%"), 73.2);
  assert.equal(parseLineCoverage("Line coverage: 100%"), 100);
});

test("parseLineCoverage meldet fehlende Angabe", () => {
  assert.throws(() => parseLineCoverage("Summary without value"), /Could not determine line coverage/);
});

test("meetsThreshold vergleicht inklusive Schwelle", () => {
  assert.equal(meetsThreshold(70, 70), true);
  assert.equal(meetsThreshold(69.9, 70), false);
});
