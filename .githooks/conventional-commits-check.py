#!/usr/bin/env python3
"""Conventional-Commits-Validierung für Tankradar.

Prüft alle Commits des aktuellen Branches seit der Divergenz von 'main'
(lokal oder 'origin/main', falls 'main' lokal nicht existiert) gegen das
Format 'type(scope): subject' bzw. 'type: subject' (Scope ist optional).

Erlaubte Types: feat, fix, docs, test, refactor, chore, perf sowie die
projekteigenen Types plan (Planungscommit) und merge (Merge-Commit eines
abgeschlossenen Entwicklungsschritts), die automatisiert durch den
projekteigenen /lifecycle-Workflow erzeugt werden. Ohne diese beiden
zusätzlichen Types würde dieser Check die eigenen, automatisiert erzeugten
Commits des Projekts dauerhaft blockieren.

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

ALLOWED_TYPES = {'feat', 'fix', 'docs', 'test', 'refactor', 'chore', 'perf', 'plan', 'merge'}
MIN_SUBJECT_LENGTH = 10

COMMIT_RE = re.compile(r'^(?P<type>[a-zA-Z]+)(\((?P<scope>[\w.-]+)\))?:\s*(?P<subject>.+)$')


def resolve_base_ref():
    """Findet die main-Referenz (lokal oder origin/main) zur Divergenz-Bestimmung."""
    for candidate in ('main', 'origin/main'):
        res = run('git', 'rev-parse', '--verify', '--quiet', candidate)
        if res.returncode == 0:
            return candidate
    return None


def commits_since_divergence(base_ref):
    res = run('git', 'log', f'{base_ref}..HEAD', '--format=%H%x1f%s')
    if res.returncode != 0:
        return []
    commits = []
    for line in res.stdout.splitlines():
        if not line:
            continue
        sha, _, subject = line.partition('\x1f')
        commits.append((sha, subject))
    return commits


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
    return parser.parse_args()


def main():
    args = parse_args()
    root = repo_root()
    if root is None:
        return 1

    base_ref = args.base or resolve_base_ref()
    if base_ref is None:
        print('WARNUNG: Keine main-Referenz gefunden (weder lokal noch origin/main) - Commit-Format-Prüfung übersprungen.')
        return 0

    commits = commits_since_divergence(base_ref)
    if not commits:
        print(f'OK: keine Commits seit Divergenz von {base_ref} gefunden.')
        return 0

    failed = False
    for sha, subject in commits:
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

    print(f'OK: {len(commits)} Commit(s) seit Divergenz von {base_ref} entsprechen dem Conventional-Commits-Format.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
