// Ermittelt die nächste Version aus der Git-Historie (Conventional Commits) und schreibt die Ergebnisse
// in GITHUB_OUTPUT (falls gesetzt) bzw. auf stdout. Ersetzt `semantic-release --dry-run` der Vorlage,
// weil semantic-release die 0.x-Vorgaben (erste Version 0.1.0, keine automatische 1.0) nicht abbildet.
//
// Aufruf: node scripts/determine-next-version.mjs [--rc]
//   --rc   hängt zusätzlich die RC-Nummer an (rc_tag, rc_version, für den Push auf staging).
import { execFileSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { computeNextVersion, nextRcNumber, STABLE_TAG_RE } from "./versioning.mjs";

function git(...args) {
  return execFileSync("git", args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
}

/** Neuester stabiler Tag (vX.Y.Z), der von HEAD aus erreichbar ist, oder null. */
export function findLatestStableTag() {
  const tags = git("tag", "--merged", "HEAD", "--list", "v*", "--sort=-v:refname")
    .split("\n")
    .map((tag) => tag.trim())
    .filter((tag) => STABLE_TAG_RE.test(tag));
  return tags[0] ?? null;
}

/** Commits seit dem Tag (bzw. alle erreichbaren Commits ohne Merge-Commits). */
export function readCommitsSince(tag) {
  const range = tag ? [`${tag}..HEAD`] : ["HEAD"];
  const raw = git("log", ...range, "--no-merges", "--format=%s%x1f%b%x1e");
  return raw
    .split("\x1e")
    .map((record) => record.replace(/^\n/, ""))
    .filter((record) => record.trim() !== "")
    .map((record) => {
      const [subject, body = ""] = record.split("\x1f");
      return { subject, body };
    });
}

export function determine({ rc = false } = {}) {
  const latestStableTag = findLatestStableTag();
  const result = computeNextVersion({ latestStableTag, commits: readCommitsSince(latestStableTag) });
  const outputs = { changed: String(result.changed), version: result.version, reason: result.reason };
  if (rc && result.changed) {
    const number = nextRcNumber(result.version, git("tag", "--list", `v${result.version}-rc.*`).split("\n"));
    outputs.rc_number = String(number);
    outputs.rc_tag = `v${result.version}-rc.${number}`;
    outputs.rc_version = `${result.version}-rc.${number}`;
  }
  return outputs;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const outputs = determine({ rc: process.argv.includes("--rc") });
  const lines = Object.entries(outputs).map(([key, value]) => `${key}=${value}`);
  console.log(lines.join("\n"));
  if (process.env.GITHUB_OUTPUT) {
    appendFileSync(process.env.GITHUB_OUTPUT, `${lines.join("\n")}\n`);
  }
}
