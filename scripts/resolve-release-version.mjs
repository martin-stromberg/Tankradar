// Löst für den Release-Workflow auf, welche Version veröffentlicht werden muss. Die GitHub-Release-
// Ablage ist entfernt (Issue #64, ADR 0023): die "already-released"-Erkennung prüft die Existenz des
// Git-Tags statt der GitHub-Releases-API - der Tag ist die einzige verbliebene Versionswahrheit.
// Struktur nach ci-workflows-instructions.md, Abschnitt 8.2; die Versionsermittlung stammt aus
// versioning.mjs (0.x-Regeln statt semantic-release).
import { execFileSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { determine } from "./determine-next-version.mjs";

const VERSION_PATTERN = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$/;
const AUTOMATIC_RELEASE_BRANCHES = ["main"];

export function parseManualTag(tagName) {
  if (!tagName?.startsWith("v")) {
    throw new Error(`Expected a vX.Y.Z tag, received '${tagName ?? ""}'.`);
  }
  const version = tagName.slice(1);
  if (!VERSION_PATTERN.test(version)) {
    throw new Error(`Tag '${tagName}' is not a valid vX.Y.Z release tag.`);
  }
  return version;
}

export function classifyWorkflowRef({ refType, refName }) {
  if (refType === "tag") {
    return { kind: "manual", version: parseManualTag(refName), tag: refName };
  }
  if (refType === "branch" && AUTOMATIC_RELEASE_BRANCHES.includes(refName)) {
    return { kind: "automatic" };
  }
  throw new Error(`Unsupported release ref '${refType ?? ""}:${refName ?? ""}'.`);
}

/**
 * Kernlogik; alle Abhängigkeiten sind injiziert.
 * @param {object} input
 * @param {{refType: string, refName: string}} input.ref
 * @param {() => {changed: string, version: string}} input.determineVersion
 * @param {(tag: string) => Promise<boolean>|boolean} input.tagExists
 */
export async function resolveReleaseVersion({ ref, determineVersion, tagExists }) {
  const classification = classifyWorkflowRef({ refType: ref.refType, refName: ref.refName });

  if (classification.kind === "manual") {
    // Der gepushte Tag ist selbst der Auslöser - keine Existenzprüfung nötig.
    return {
      released: "true",
      reason: "new-release",
      version: classification.version,
      tag: classification.tag,
      release_kind: "manual"
    };
  }

  const next = determineVersion();
  if (next.changed !== "true") {
    return { released: "false", reason: "no-releasable-commits", version: "", tag: "", release_kind: "automatic" };
  }

  const tag = `v${next.version}`;
  if (await tagExists(tag)) {
    // Restzustand: der Ziel-Tag existiert bereits, ohne in HEAD gemergt zu sein (sonst hätte
    // determine ihn als letzten Tag gefunden) - kein erneutes Ausliefern.
    return { released: "false", reason: "already-released", version: next.version, tag, release_kind: "automatic" };
  }
  return { released: "true", reason: "new-release", version: next.version, tag, release_kind: "automatic" };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const outputs = await resolveReleaseVersion({
    ref: { refType: (process.env.GITHUB_REF_TYPE ?? "").toLowerCase(), refName: process.env.GITHUB_REF_NAME },
    determineVersion: () => determine(),
    // Der Checkout läuft mit fetch-tags: true - alle Tags liegen lokal vor.
    tagExists: (tag) => execFileSync("git", ["tag", "--list", tag], { encoding: "utf8" }).trim() !== ""
  });
  const lines = Object.entries(outputs).map(([key, value]) => `${key}=${value}`);
  console.log(lines.join("\n"));
  if (process.env.GITHUB_OUTPUT) {
    appendFileSync(process.env.GITHUB_OUTPUT, `${lines.join("\n")}\n`);
  }
}
