#!/usr/bin/env python3
"""Gemeinsame Hilfsfunktionen für die Tankradar Git-Hook-Prüfskripte.

Wird von allen Prüfskripten unter .githooks/ per
'from _hook_common import ...' importiert. Funktioniert ohne zusätzliche
sys.path-Anpassung, da Python das Verzeichnis des ausgeführten Skripts
automatisch in sys.path aufnimmt.
"""
import subprocess
import sys
from pathlib import Path

EXCLUDED_DIRS = {'.git', 'bin', 'obj', 'TestResults', 'node_modules', '.vs', '.idea', 'packages'}


def run(*args):
    return subprocess.run(args, capture_output=True, text=True, encoding='utf-8', errors='replace')


def repo_root():
    res = run('git', 'rev-parse', '--show-toplevel')
    if res.returncode != 0:
        print('ERROR: not inside a git repository', file=sys.stderr)
        return None
    return Path(res.stdout.strip())


def staged_files():
    res = run('git', 'diff', '--cached', '--name-only', '--diff-filter=ACM')
    if res.returncode != 0:
        return []
    return [p for p in res.stdout.splitlines() if p]


def find_solution(root):
    matches = sorted(list(root.glob('*.sln')) + list(root.glob('*.slnx')))
    return matches[0] if matches else None
