#!/usr/bin/env python3
"""Forbidden-Patterns-Check für Tankradar.

Blockiert versehentlich committete Secrets, Zertifikate, Datenbank-Dumps und
übergroße Logdateien:

  - API-Schlüssel (z. B. für Tankerkönig/Kraftstoffpreise, OpenRouteService)
  - Zertifikate/Signierungsdaten (.pfx, .p12, .keystore, .jks, .pem, .cer,
    sowie PEM-Klartext-Inhalte und konkrete Werte in <SigningKey>/
    <CertificateThumbprint> in .csproj-Dateien)
  - Datenbank-Dumps (.db, .sqlite, .sqlite3)
  - Logdateien über 1 MB

Run as a pre-commit hook (default: only staged files, warn-only, exit 0) or
with --all to scan the entire repository. Pass --strict (as used in
pre-push) to turn findings into a hard failure (exit 1).
"""
import argparse
import re
import sys
from pathlib import Path

from _hook_common import EXCLUDED_DIRS, repo_root, staged_files

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

TEXT_SCAN_EXTENSIONS = {
    '.cs', '.csproj', '.json', '.config', '.xml', '.xaml', '.txt',
    '.env', '.yml', '.yaml', '.ps1', '.sh', '.py',
}
# .md-Dateien (Dokumentation, inklusive dieser eigenen Anforderungs-, Plan- und
# Check-Dokumente, die Muster-Beispiele zu Illustrationszwecken enthalten) werden bewusst
# nicht auf API-Schluessel-Inhalte durchsucht, um Fehlalarme durch Beispieltexte zu
# vermeiden. Datei-/Zertifikats-/DB-Dump-/Logdatei-Pruefungen erfassen .md nicht ohnehin.

# Werte, die auf eine sichere Referenz (Umgebungsvariable) statt eines Klartext-Secrets
# hindeuten, gelten nicht als Fund.
SAFE_VALUE_RE = re.compile(
    r'^(\$env:|\$\{|%|os\.environ|Environment\.GetEnvironmentVariable|getenv\(|process\.env)',
    re.IGNORECASE,
)

API_KEY_RES = [
    re.compile(r'(FUEL_API_KEY|ROUTING_KEY|MAPBOX_TOKEN|NOMINATIM_TOKEN)\s*=\s*(?P<value>\S+)', re.IGNORECASE),
    re.compile(r'(?:fuel|routing|api)[_-]?(?:key|token|secret)\s*=\s*(?P<value>\S+)', re.IGNORECASE),
]

CERT_EXTENSIONS = {'.pfx', '.p12', '.keystore', '.jks', '.pem', '.cer'}
CERT_CONTENT_RE = re.compile(r'-----BEGIN (?:RSA PRIVATE KEY|CERTIFICATE|PRIVATE KEY)-----')

DB_DUMP_EXTENSIONS = {'.db', '.sqlite', '.sqlite3'}

LOG_SIZE_LIMIT_BYTES = 1024 * 1024  # 1 MB

SIGNING_TAG_RE = re.compile(r'<(SigningKey|CertificateThumbprint)>([^<]*)</\1>')
PLACEHOLDER_RE = re.compile(r'^\s*(\$\(|%|\{\{|PLACEHOLDER|CHANGE_ME|X{3,}|x{3,})', re.IGNORECASE)


def all_files(root):
    files = []
    for p in root.rglob('*'):
        if not p.is_file():
            continue
        if any(part in EXCLUDED_DIRS for part in p.parts):
            continue
        files.append(str(p.relative_to(root).as_posix()))
    return files


def check_api_keys(rel, content):
    issues = []
    for lineno, line in enumerate(content.splitlines(), 1):
        for pattern in API_KEY_RES:
            m = pattern.search(line)
            if not m:
                continue
            if SAFE_VALUE_RE.match(m.group('value')):
                continue
            issues.append(f'  {rel}:{lineno}: möglicher API-Schlüssel gefunden ({m.group(0)[:60]})')
    return issues


def check_certificate_content(rel, content):
    issues = []
    for lineno, line in enumerate(content.splitlines(), 1):
        if CERT_CONTENT_RE.search(line):
            issues.append(f'  {rel}:{lineno}: Zertifikats-/Schlüsseldaten im Klartext gefunden')
    return issues


def check_signing_data(rel, content):
    issues = []
    for lineno, line in enumerate(content.splitlines(), 1):
        for m in SIGNING_TAG_RE.finditer(line):
            tag, value = m.group(1), m.group(2).strip()
            if value and not PLACEHOLDER_RE.match(value):
                issues.append(f'  {rel}:{lineno}: <{tag}> enthält einen konkreten Wert ("{value[:40]}")')
    return issues


def check_file(root, rel):
    """Returns a list of issue strings for one file (by name and, where relevant, content)."""
    path = root / rel
    issues = []
    suffix = path.suffix.lower()

    if suffix in CERT_EXTENSIONS:
        issues.append(f'  {rel}: verbotene Zertifikats-/Schlüsseldatei ({suffix})')

    if suffix in DB_DUMP_EXTENSIONS:
        issues.append(f'  {rel}: verbotener Datenbank-Dump ({suffix})')

    if suffix == '.log':
        try:
            size = path.stat().st_size
        except OSError:
            size = 0
        if size > LOG_SIZE_LIMIT_BYTES:
            issues.append(f'  {rel}: Logdatei zu groß ({size / (1024 * 1024):.1f} MB > 1 MB)')

    if suffix in TEXT_SCAN_EXTENSIONS:
        try:
            content = path.read_text(encoding='utf-8', errors='replace')
        except OSError:
            return issues

        issues.extend(check_api_keys(rel, content))
        issues.extend(check_certificate_content(rel, content))
        if suffix == '.csproj':
            issues.extend(check_signing_data(rel, content))

    return issues


def parse_args():
    parser = argparse.ArgumentParser(
        description='Forbidden-Patterns-Check (Secrets, Zertifikate, DB-Dumps, Logdateien)'
    )
    parser.add_argument('--all', action='store_true', help='ganzes Repository prüfen statt nur gestaffelte Dateien')
    parser.add_argument('--strict', action='store_true', help='bei Fund Exit-Code 1 statt nur Warnung')
    return parser.parse_args()


def main():
    args = parse_args()
    root = repo_root()
    if root is None:
        return 1

    if args.all:
        files = all_files(root)
        scan_mode = 'all'
    else:
        files = staged_files()
        scan_mode = 'staged'

    checked = 0
    all_issues = []
    for rel in files:
        path = root / rel
        if not path.exists() or not path.is_file():
            continue
        checked += 1
        all_issues.extend(check_file(root, rel))

    if all_issues:
        level = 'ERROR' if args.strict else 'WARNING'
        print(f'{level}: verbotene Muster gefunden (Secrets, Zertifikate, DB-Dumps oder übergroße Logdateien):')
        print('\n'.join(all_issues))
        print('  -> Diese Dateien/Inhalte dürfen nicht committet werden. Bitte aus dem Commit entfernen')
        print('     und ggf. in .gitignore aufnehmen.')
        if args.strict:
            return 1
        print('(Nur Warnung beim Commit — muss vor dem Push entfernt werden.)')
        return 0

    print(f'OK: {checked} {scan_mode} file(s) checked, no forbidden patterns found.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
