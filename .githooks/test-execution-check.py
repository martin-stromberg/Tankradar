#!/usr/bin/env python3
"""Test-Ausführungs-Validierung vor Push für Tankradar.

Baut die Solution einmal ('dotnet build') und führt danach 'dotnet test --no-build'
aus, damit kein Build parallel zu laufenden E2E-Tests stattfindet (Dateisperre auf
der App-EXE). Blockiert den Push, wenn Build oder Tests fehlschlagen oder das
Gesamt-Zeitlimit (Standard: 10 Minuten) überschritten wird.

Notfall-Fallback: HOOK_SKIP_TESTS=1 überspringt die Testausführung (mit
Warnung auf stderr). TEST_TIMEOUT_SECONDS überschreibt das Zeitlimit.

Exit-Code: 0 (Tests bestanden oder übersprungen), 1 (fehlgeschlagen/Timeout).
"""
import os
import subprocess
import sys
import time

from _hook_common import find_solution, repo_root

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

DEFAULT_TIMEOUT_SECONDS = 10 * 60


def timeout_seconds():
    raw = os.environ.get('TEST_TIMEOUT_SECONDS', '')
    if raw.strip():
        try:
            return int(raw)
        except ValueError:
            print(f'WARNUNG: TEST_TIMEOUT_SECONDS="{raw}" ist keine gültige Zahl, verwende Standard.', file=sys.stderr)
    return DEFAULT_TIMEOUT_SECONDS


def main():
    if os.environ.get('HOOK_SKIP_TESTS') == '1':
        print('WARNUNG: HOOK_SKIP_TESTS=1 gesetzt - Testausführung vor Push wird übersprungen.', file=sys.stderr)
        return 0

    root = repo_root()
    if root is None:
        return 1

    solution = find_solution(root)
    if solution is None:
        print('WARNUNG: keine .sln-/.slnx-Datei gefunden - Testausführung übersprungen.', file=sys.stderr)
        return 0

    timeout = timeout_seconds()
    print(f'Führe "dotnet build" und "dotnet test --no-build" für {solution.name} aus (Timeout gesamt: {timeout}s)...')

    start = time.monotonic()
    commands = [
        ['dotnet', 'build', str(solution), '--nologo', '--verbosity', 'quiet'],
        ['dotnet', 'test', str(solution), '--no-build', '--nologo', '--verbosity', 'quiet'],
    ]
    result = None
    for command in commands:
        remaining = max(1, timeout - (time.monotonic() - start))
        try:
            result = subprocess.run(command, cwd=str(root), timeout=remaining)
        except subprocess.TimeoutExpired:
            elapsed = time.monotonic() - start
            print(f'FEHLER: Testlauf nach {elapsed:.0f}s abgebrochen (Timeout: {timeout}s).', file=sys.stderr)
            print('Hinweis: HOOK_SKIP_TESTS=1 kann als Notfall-Fallback gesetzt werden, um die Testausführung zu überspringen.', file=sys.stderr)
            return 1
        except FileNotFoundError:
            print('FEHLER: "dotnet" wurde nicht gefunden. Ist das .NET SDK installiert?', file=sys.stderr)
            return 1
        if result.returncode != 0:
            break

    elapsed = time.monotonic() - start
    if result.returncode != 0:
        print(f'FEHLER: Build oder Tests fehlgeschlagen (Exit-Code {result.returncode}, nach {elapsed:.0f}s).', file=sys.stderr)
        return 1

    print(f'OK: alle Tests bestanden (nach {elapsed:.0f}s).')
    return 0


if __name__ == '__main__':
    sys.exit(main())
