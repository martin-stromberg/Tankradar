#!/usr/bin/env python3
"""Code-Formatierungs-Check für Tankradar.

Ruft 'dotnet format <Solution> --verify-no-changes' auf und prüft dabei
immer die gesamte Solution (nicht nur gestagte Dateien). Pre-commit und
pre-push rufen den Check mit --strict auf; dann ist er blockierend und
liefert bei Formatabweichungen Exit-Code 1. Ohne --strict (manueller
Aufruf) werden Verstöße nur als Warnung gemeldet und der Exit-Code ist 0.
"""
import argparse
import sys

from _hook_common import find_solution, repo_root, run

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')


def parse_args():
    parser = argparse.ArgumentParser(description='Code-Formatierungs-Check (dotnet format --verify-no-changes)')
    parser.add_argument(
        '--strict', action='store_true',
        help='bei Verstößen Exit-Code 1 (blockierend); pre-commit und pre-push verwenden diesen Modus',
    )
    return parser.parse_args()


def main():
    args = parse_args()
    root = repo_root()
    if root is None:
        return 1

    solution = find_solution(root)
    if solution is None:
        print('Keine .sln-/.slnx-Datei gefunden - Formatierungs-Check übersprungen.')
        return 0

    if not run('dotnet', '--version').returncode == 0:
        print('WARNUNG: "dotnet" wurde nicht gefunden - Formatierungs-Check übersprungen.', file=sys.stderr)
        return 0

    result = run('dotnet', 'format', str(solution), '--verify-no-changes')

    if result.returncode != 0:
        print(f'WARNUNG: "dotnet format --verify-no-changes" meldet Formatierungsabweichungen in {solution.name}:')
        output = (result.stdout or '') + (result.stderr or '')
        for line in output.splitlines():
            if line.strip():
                print(f'  {line}')
        print('  -> Mit "dotnet format" lokal beheben.')
        if args.strict:
            return 1
        print('(Nur Warnung ohne --strict — blockiert nicht.)')
        return 0

    print(f'OK: {solution.name} entspricht den dotnet-format-Regeln.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
