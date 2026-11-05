// Reine Versionierungslogik der CI/CD-Pipeline (keine I/O, vollständig testbar).
//
// Regeln (Projektvorgabe, abweichend von semantic-release):
//  - Die erste Version ist 0.1.0.
//  - Solange die Hauptversion 0 ist, führt ein Breaking Change nur zu einer Minor-Anhebung;
//    eine automatische Anhebung auf 1.0.0 findet nie statt (1.0.0 setzt der Projektleiter
//    bewusst per manuellem Tag v1.0.0).
//  - Ab Hauptversion >= 1 gilt klassisches SemVer (Breaking -> major, feat -> minor, fix/perf -> patch).
//  - Commits ohne feat/fix/perf/Breaking (docs, chore, ci, test, ...) lösen keine Version aus.

const COMMIT_RE = /^(?<type>[a-zA-Z]+)(\((?<scope>[\w.-]+)\))?(?<breaking>!)?:\s*(?<subject>.+)$/;
const BREAKING_FOOTER_RE = /^BREAKING[ -]CHANGE:/m;
export const STABLE_TAG_RE = /^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/;
export const FIRST_VERSION = "0.1.0";

/**
 * Zerlegt eine Commit-Nachricht in Typ und Breaking-Kennzeichen.
 * @param {string} subject Erste Zeile der Commit-Nachricht.
 * @param {string} [body] Rumpf der Commit-Nachricht.
 * @returns {{type: string|null, breaking: boolean}}
 */
export function parseCommit(subject, body = "") {
  const match = COMMIT_RE.exec(subject ?? "");
  const breaking = Boolean(match?.groups?.breaking) || BREAKING_FOOTER_RE.test(body ?? "");
  return { type: match ? match.groups.type.toLowerCase() : null, breaking };
}

/**
 * Ermittelt die höchste erforderliche Anhebung für eine Commit-Liste.
 * @param {Array<{subject: string, body?: string}>} commits
 * @returns {"major"|"minor"|"patch"|null}
 */
export function requiredBump(commits) {
  let bump = null;
  for (const commit of commits) {
    const { type, breaking } = parseCommit(commit.subject, commit.body);
    if (breaking) {
      return "major";
    }
    if (type === "feat") {
      bump = "minor";
    } else if ((type === "fix" || type === "perf") && bump === null) {
      bump = "patch";
    }
  }
  return bump;
}

/**
 * Parst "X.Y.Z" (ohne führendes v) in ein Objekt.
 * @param {string} version
 */
export function parseVersion(version) {
  const match = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.exec(version ?? "");
  if (!match) {
    throw new Error(`'${version}' ist keine gültige Version X.Y.Z.`);
  }
  return { major: Number(match[1]), minor: Number(match[2]), patch: Number(match[3]) };
}

/**
 * Wendet eine Anhebung an und beachtet dabei die 0.x-Regel (nie automatisch auf 1.0.0).
 * @param {string} current Aktuelle stabile Version ohne v.
 * @param {"major"|"minor"|"patch"} bump
 * @returns {string}
 */
export function applyBump(current, bump) {
  const { major, minor, patch } = parseVersion(current);
  if (major === 0) {
    // Vor 1.0: Breaking Changes und Features erhöhen die Minor-Version, Fixes die Patch-Version.
    return bump === "patch" ? `0.${minor}.${patch + 1}` : `0.${minor + 1}.0`;
  }
  if (bump === "major") {
    return `${major + 1}.0.0`;
  }
  return bump === "minor" ? `${major}.${minor + 1}.0` : `${major}.${minor}.${patch + 1}`;
}

/**
 * Berechnet die nächste Version.
 * @param {{latestStableTag: string|null, commits: Array<{subject: string, body?: string}>}} input
 *   latestStableTag: neuester stabiler Tag (vX.Y.Z) im Verlauf oder null, wenn noch keiner existiert.
 *   commits: Commits seit diesem Tag (bzw. alle, wenn kein Tag existiert).
 * @returns {{changed: boolean, version: string, reason: string}}
 */
export function computeNextVersion({ latestStableTag, commits }) {
  if (!latestStableTag) {
    if (commits.length === 0) {
      return { changed: false, version: "", reason: "no-commits" };
    }
    return { changed: true, version: FIRST_VERSION, reason: "first-release" };
  }
  if (!STABLE_TAG_RE.test(latestStableTag)) {
    throw new Error(`'${latestStableTag}' ist kein stabiler Release-Tag (vX.Y.Z).`);
  }
  const bump = requiredBump(commits);
  if (bump === null) {
    return { changed: false, version: "", reason: "no-releasable-commits" };
  }
  return { changed: true, version: applyBump(latestStableTag.slice(1), bump), reason: bump };
}

/**
 * Bestimmt die nächste RC-Nummer aus den vorhandenen Tags für genau diese Version.
 * @param {string} version Version ohne v.
 * @param {string[]} existingTags Alle vorhandenen Tags.
 * @returns {number}
 */
export function nextRcNumber(version, existingTags) {
  const prefix = `v${version}-rc.`;
  const used = existingTags
    .filter((tag) => tag.startsWith(prefix))
    .map((tag) => Number(tag.slice(prefix.length)))
    .filter((n) => Number.isInteger(n) && n > 0);
  return used.length === 0 ? 1 : Math.max(...used) + 1;
}

/**
 * Leitet die rein numerische Anzeigeversion (für ApplicationDisplayVersion) aus einer ggf. mit
 * Pre-Release-Suffix versehenen Version ab.
 * @param {string} version z. B. "0.2.0-rc.3"
 * @returns {string} z. B. "0.2.0"
 */
export function displayVersion(version) {
  return version.split("-")[0];
}
