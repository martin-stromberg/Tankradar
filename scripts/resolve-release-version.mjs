// Löst für den Release-Workflow auf, welche Version veröffentlicht werden muss und ob ein bereits
// vorhandenes Release repariert (fehlende Assets nachgeladen) werden muss.
// Struktur nach ci-workflows-instructions.md, Abschnitt 8.2; die Versionsermittlung stammt aus
// versioning.mjs (0.x-Regeln statt semantic-release).
import { execFileSync } from "node:child_process";
import { appendFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { determine } from "./determine-next-version.mjs";

const VERSION_PATTERN = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$/;
const AUTOMATIC_RELEASE_BRANCHES = ["main"];
export const EXPECTED_ASSETS = ["release-win-x64.zip", "update.json"];

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

export function releaseHasExpectedAsset(release) {
  const assets = release.assets ?? [];
  return EXPECTED_ASSETS.every((name) =>
    assets.some((asset) => asset.name === name && asset.state === "uploaded" && asset.size > 0)
  );
}

// CRITICAL GUARD (Vorlage, Abschnitt 11.1): Pre-Releases werden nie repariert. Sonst würde ein alter,
// unvollständiger RC ausgecheckt, in dessen Commit die aktuellen Workflow-Dateien noch nicht existieren.
export function incompleteReleases(releases) {
  return releases.filter((release) => {
    if (release.prerelease) {
      return false;
    }
    return !releaseHasExpectedAsset(release);
  });
}

/**
 * Kernlogik; alle Abhängigkeiten sind injiziert.
 * @param {object} input
 * @param {{refType: string, refName: string}} input.ref
 * @param {() => {changed: string, version: string}} input.determineVersion
 * @param {(tag: string) => Promise<object|null>|object|null} input.getRelease
 * @param {() => Promise<object[]>|object[]} input.listReleases
 */
export async function resolveReleaseVersion({ ref, determineVersion, getRelease, listReleases }) {
  const classification = classifyWorkflowRef({ refType: ref.refType, refName: ref.refName });

  let version = classification.version ?? "";
  let tag = classification.tag ?? "";
  if (classification.kind === "automatic") {
    const next = determineVersion();
    if (next.changed === "true") {
      version = next.version;
      tag = `v${version}`;
    }
  }

  if (tag) {
    const existing = await getRelease(tag);
    const base = { version, tag, release_kind: classification.kind };
    if (!existing) {
      return { released: "true", reason: "new-release", ...base, release_action: "create" };
    }
    if (!releaseHasExpectedAsset(existing)) {
      return { released: "true", reason: "repair-missing-assets", ...base, release_action: "upload-existing" };
    }
    return { released: "false", reason: "already-released", ...base, release_action: "" };
  }

  // Automatischer Lauf ohne freigabefähige Commits: unvollständiges (nicht Pre-) Release reparieren.
  const incomplete = incompleteReleases(await listReleases());
  if (incomplete.length > 0) {
    const oldest = [...incomplete].sort((a, b) => String(a.created_at).localeCompare(String(b.created_at)))[0];
    return {
      released: "true",
      reason: "repair-missing-assets",
      version: oldest.tag_name.slice(1),
      tag: oldest.tag_name,
      release_kind: "automatic",
      release_action: "upload-existing"
    };
  }
  return { released: "false", reason: "no-releasable-commits", version: "", tag: "", release_kind: "automatic", release_action: "" };
}

function gh(args) {
  return execFileSync("gh", args, { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
}

export function ghGetRelease(repository, tag) {
  try {
    return JSON.parse(gh(["api", `repos/${repository}/releases/tags/${tag}`]));
  } catch (error) {
    if (String(error.stderr ?? error.message).includes("404")) {
      return null;
    }
    throw error;
  }
}

export function ghListReleases(repository) {
  const pages = gh(["api", "--paginate", "--slurp", `repos/${repository}/releases?per_page=100`]);
  return JSON.parse(pages).flat();
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const repository = process.env.GITHUB_REPOSITORY;
  const outputs = await resolveReleaseVersion({
    ref: { refType: (process.env.GITHUB_REF_TYPE ?? "").toLowerCase(), refName: process.env.GITHUB_REF_NAME },
    determineVersion: () => determine(),
    getRelease: (tag) => ghGetRelease(repository, tag),
    listReleases: () => ghListReleases(repository)
  });
  const lines = Object.entries(outputs).map(([key, value]) => `${key}=${value}`);
  console.log(lines.join("\n"));
  if (process.env.GITHUB_OUTPUT) {
    appendFileSync(process.env.GITHUB_OUTPUT, `${lines.join("\n")}\n`);
  }
}
