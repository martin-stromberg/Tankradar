#!/usr/bin/env python3
"""Conventional-Commits-Validierung für Tankradar.

Prüft alle Commits des aktuellen Branches seit der Divergenz von 'main'
(lokal oder 'origin/main', falls 'main' lokal nicht existiert) gegen das
Format 'type(scope): subject' bzw. 'type: subject' (Scope ist optional).

Erlaubte Types: feat, fix, docs, test, refactor, chore, perf, ci, build,
style, revert sowie die projekteigenen Types plan (Planungscommit) und merge
(Merge-Commit eines abgeschlossenen Entwicklungsschritts), die automatisiert
durch den projekteigenen /lifecycle-Workflow erzeugt werden. Ohne diese
beiden zusätzlichen Types würde dieser Check die eigenen, automatisiert
erzeugten Commits des Projekts dauerhaft blockieren.

Breaking-Change-Notation wird akzeptiert und nie blockiert: entweder als '!'
vor dem Doppelpunkt der ersten Zeile (z. B. 'feat!: ...' oder
'feat(scope)!: ...') oder als eigene 'BREAKING CHANGE:'-Zeile im Commit-Body.
Fehlt bei einem '!' der 'BREAKING CHANGE:'-Footer, wird nur ein Hinweis
ausgegeben (siehe check_breaking_change()).

Lässt sich eine per --base/--end übergebene Referenz nicht auflösen, wird
für --base mit Warnung auf stderr auf die main-Referenz zurückgefallen;
eine nicht auflösbare --end-Referenz führt zu Exit 1.

Auf eine Prüfung der Groß-/Kleinschreibung am Subject-Anfang wird bewusst
verzichtet: Die Projektsprache ist Deutsch, wo Substantive und damit auch
viele Satzanfänge grundsätzlich großgeschrieben werden — eine Regel "kein
Großbuchstabe am Anfang" ist für deutsche Commit-Nachrichten nicht sinnvoll
anwendbar und würde nahezu jeden bisherigen Commit dieses Projekts als
Verstoß melden.

Exit-Code: 0 (alle geprüften Commits valide), 1 (mindestens ein Verstoß).
"""
import argparse
import re
import sys

from _hook_common import repo_root, run

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

ALLOWED_TYPES = {
    'feat', 'fix', 'docs', 'test', 'refactor', 'chore', 'perf', 'plan', 'merge',
    'ci', 'build', 'style', 'revert',
}
MIN_SUBJECT_LENGTH = 10

COMMIT_RE = re.compile(
    r'^(?P<type>[a-zA-Z]+)(\((?P<scope>[\w.-]+)\))?(?P<breaking>!)?:\s*(?P<subject>.+)$'
)
BREAKING_CHANGE_BODY_RE = re.compile(r'^BREAKING CHANGE:', re.MULTILINE)


def resolve_base_ref():
    """Findet die main-Referenz (lokal oder origin/main) zur Divergenz-Bestimmung."""
    for candidate in ('main', 'origin/main'):
        res = run('git', 'rev-parse', '--verify', '--quiet', candidate)
        if res.returncode == 0:
            return candidate
    return None


def ref_exists(ref):
    return run('git', 'rev-parse', '--verify', '--quiet', f'{ref}^{{commit}}').returncode == 0


def commits_since_divergence(base_ref, end_ref='HEAD'):
    """Liefert (sha, subject, body) je Commit im Bereich base_ref..end_ref."""
    res = run('git', 'log', f'{base_ref}..{end_ref}', '--format=%H%x1f%s%x1f%b%x1e')
    if res.returncode != 0:
        return []
    commits = []
    for record in res.stdout.split('\x1e'):
        record = record.strip('\n')
        if not record:
            continue
        sha, _, rest = record.partition('\x1f')
        subject, _, body = rest.partition('\x1f')
        commits.append((sha, subject, body))
    return commits


def check_breaking_change(commit_body):
    """Returns True if the commit body contains a 'BREAKING CHANGE:' line."""
    return bool(BREAKING_CHANGE_BODY_RE.search(commit_body))


def has_breaking_notation(subject_line):
    """True, wenn die erste Zeile die '!'-Notation verwendet."""
    m = COMMIT_RE.match(subject_line)
    return bool(m and m.group('breaking'))


def validate_message(subject_line):
    """Returns a list of error strings for one commit subject line (empty = valid)."""
    errors = []
    m = COMMIT_RE.match(subject_line)
    if not m:
        errors.append("Format entspricht nicht 'type(scope): subject' bzw. 'type: subject'")
        return errors

    commit_type = m.group('type')
    subject = m.group('subject').strip()

    if commit_type not in ALLOWED_TYPES:
        errors.append(f"unbekannter Type '{commit_type}' (erlaubt: {', '.join(sorted(ALLOWED_TYPES))})")

    if len(subject) < MIN_SUBJECT_LENGTH:
        errors.append(f"Nachricht zu kurz ({len(subject)} Zeichen, mindestens {MIN_SUBJECT_LENGTH} erforderlich)")

    return errors


def parse_args():
    parser = argparse.ArgumentParser(description='Conventional-Commits-Format-Validierung')
    parser.add_argument(
        '--base', default=None,
        help='Basis-Referenz für die Divergenz-Bestimmung (Standard: main / origin/main)',
    )
    parser.add_argument(
        '--end', default=None,
        help='End-Referenz bis zu der Commits geprüft werden (Standard: HEAD). Wird im '
             'pre-push-Hook auf die tatsächlich gepushte lokale SHA je Ref gesetzt, damit '
             'nur der tatsächlich gepushte Commit-Bereich geprüft wird.',
    )
    return parser.parse_args()


def main():
    args = parse_args()
    root = repo_root()
    if root is None:
        return 1

    base_ref = args.base
    if base_ref is not None and not ref_exists(base_ref):
        print(f"WARNUNG: Basis-Referenz '{base_ref}' ist lokal nicht auflösbar - Fallback auf main-Referenz.",
              file=sys.stderr)
        base_ref = None
    base_ref = base_ref or resolve_base_ref()
    if base_ref is None:
        print('WARNUNG: Keine main-Referenz gefunden (weder lokal noch origin/main) - Commit-Format-Prüfung übersprungen.')
        return 0

    end_ref = args.end or 'HEAD'
    if not ref_exists(end_ref):
        print(f"ERROR: End-Referenz '{end_ref}' ist nicht auflösbar - Commit-Prüfung nicht möglich.", file=sys.stderr)
        return 1
    commits = commits_since_divergence(base_ref, end_ref)
    if not commits:
        print(f'OK: keine Commits im Bereich {base_ref}..{end_ref} gefunden.')
        return 0

    failed = False
    for sha, subject, body in commits:
        if has_breaking_notation(subject) and not check_breaking_change(body):
            print(f'HINWEIS: Commit {sha[:8]} "{subject}" nutzt "!", hat aber keinen "BREAKING CHANGE:"-Footer.')
        errors = validate_message(subject)
        if errors:
            failed = True
            print(f'ERROR: Commit {sha[:8]} "{subject}":')
            for e in errors:
                print(f'  -> {e}')

    if failed:
        print()
        print('Erwartetes Format: type(scope): subject   (scope optional)')
        print(f"Erlaubte Types: {', '.join(sorted(ALLOWED_TYPES))}")
        return 1

    print(f'OK: {len(commits)} Commit(s) im Bereich {base_ref}..{end_ref} entsprechen dem Conventional-Commits-Format.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
