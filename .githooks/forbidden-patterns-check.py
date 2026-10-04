#!/usr/bin/env python3
"""Forbidden-Patterns-Check für Tankradar.

Blockiert versehentlich committete Secrets, Zertifikate, Datenbank-Dumps,
iOS-Signierungsdateien und Logdateien:

  - API-Schlüssel (z. B. für Tankerkönig/Kraftstoffpreise, OpenRouteService),
    sowohl in Gleichheitszeichen-Notation (Umgebungsvariablen-Stil) als auch
    in JSON-Notation (Feldnamen mit api-key/secret/token/password o. ä.)
  - Zertifikate/Signierungsdaten (.pfx, .p12, .keystore, .jks, .pem, .cer,
    sowie PEM-Klartext-Inhalte und konkrete Werte in <SigningKey>/
    <CertificateThumbprint> in .csproj-Dateien)
  - Datenbank-Dumps (.db, .sqlite, .sqlite3, .sql)
  - iOS-Signierungsdateien (.mobileprovision, .p8)
  - Logdateien jeder Größe (Ausnahme: das versionierte changes.log)

Run as a pre-commit hook (default: only staged files, warn-only, exit 0) or
with --all to scan the entire repository. Pass --strict (as used in
pre-push) to turn findings into a hard failure (exit 1).
"""
import argparse
import re
import sys
from pathlib import Path

from _hook_common import EXCLUDED_DIRS, repo_root, run, staged_files

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

# JSON-Notation, z. B. ein Tankerkoenig- oder Routing-Dienst-API-Schluessel-Feld mit konkretem
# Wert. Der Feldname muss einen typischen Geheimnis-Begriff enthalten (api/access/private/
# routing/ors/openrouteservice + key, secret, token, password), damit harmlose Felder wie
# keyboardType, PrimaryKey, sortKey oder KeyVaultName nicht anschlagen.
JSON_KEY_VALUE_RE = re.compile(
    r'"(?P<name>[^"]*?(?:(?:api|access|private|routing|ors|openrouteservice)[_-]?key|secret|token|password)[^"]*)"'
    r'\s*:\s*"(?P<value>[^"]*)"',
    re.IGNORECASE,
)
# Feldnamen, die zwar einen Treffer-Begriff enthalten, aber keine Geheimnisse sind.
JSON_NAME_EXCLUDE_RE = re.compile(r'publickeytoken', re.IGNORECASE)
JSON_MIN_VALUE_LENGTH = 8
URL_VALUE_RE = re.compile(r'^https?://', re.IGNORECASE)

CERT_EXTENSIONS = {'.pfx', '.p12', '.keystore', '.jks', '.pem', '.cer'}
CERT_CONTENT_RE = re.compile(r'-----BEGIN (?:RSA PRIVATE KEY|CERTIFICATE|PRIVATE KEY)-----')

DB_DUMP_EXTENSIONS = {'.db', '.sqlite', '.sqlite3', '.sql'}

IOS_SIGNING_EXTENSIONS = {'.mobileprovision', '.p8'}

# Logdateien werden unabhängig von ihrer Größe blockiert (kein Größenlimit mehr); einzige
# Ausnahme ist das versionierte, projekteigene Änderungsprotokoll changes.log im Repo-Root
# (vollständiger relativer Pfad, keine Whitelist für gleichnamige Dateien in Unterordnern).
ALLOWED_LOG_FILES = {'changes.log'}

SIGNING_TAG_RE = re.compile(r'<(SigningKey|CertificateThumbprint)>([^<]*)</\1>')
PLACEHOLDER_RE = re.compile(r'^\s*(\$\(|%|\{\{|PLACEHOLDER|CHANGE_ME|X{3,}|x{3,})', re.IGNORECASE)


def all_files(root):
    """
    Listet alle Dateien des Repositorys für den --all-Modus: getrackte Dateien plus
    nicht-getrackte, aber nicht von .gitignore ausgeschlossene Dateien. Bewusst nicht per
    reinem Dateisystem-Scan (root.rglob), da sonst gezielt via .gitignore ausgeschlossene
    Dateien (z. B. lokale *.log-Arbeitsdateien, siehe .gitignore) fälschlich als Fund
    gemeldet würden, obwohl sie nie committet werden können.
    """
    res = run('git', '-C', str(root), 'ls-files', '--cached', '--others', '--exclude-standard', '-z')
    if res.returncode != 0:
        print('WARNUNG: git ls-files fehlgeschlagen - Fallback auf Dateisystem-Scan '
              '(.gitignore wird nicht beachtet).', file=sys.stderr)
        files = []
        for p in root.rglob('*'):
            if not p.is_file():
                continue
            if any(part in EXCLUDED_DIRS for part in p.parts):
                continue
            files.append(str(p.relative_to(root).as_posix()))
        return files

    files = []
    for rel in res.stdout.split('\x00'):
        if not rel:
            continue
        if any(part in EXCLUDED_DIRS for part in Path(rel).parts):
            continue
        files.append(rel)
    return files


def is_concrete_value(value, min_length=1, allow_whitespace=True):
    """True, wenn der Wert wie ein konkretes Klartext-Secret aussieht (kein Platzhalter/Env-Verweis)."""
    if len(value) < min_length:
        return False
    if not allow_whitespace and re.search(r'\s', value):
        return False
    return not (PLACEHOLDER_RE.match(value) or SAFE_VALUE_RE.match(value))


def check_api_keys(rel, content):
    issues = []
    for lineno, line in enumerate(content.splitlines(), 1):
        for pattern in API_KEY_RES:
            for m in pattern.finditer(line):
                if SAFE_VALUE_RE.match(m.group('value')):
                    continue
                issues.append(f'  {rel}:{lineno}: möglicher API-Schlüssel gefunden ({m.group(0)[:60]})')
        for m in JSON_KEY_VALUE_RE.finditer(line):
            value = m.group('value')
            if JSON_NAME_EXCLUDE_RE.search(m.group('name')) or URL_VALUE_RE.match(value):
                continue
            if not is_concrete_value(value, JSON_MIN_VALUE_LENGTH, allow_whitespace=False):
                continue
            issues.append(f'  {rel}:{lineno}: möglicher API-Schlüssel in JSON-Notation gefunden ({m.group(0)[:60]})')
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

    if suffix in IOS_SIGNING_EXTENSIONS:
        issues.append(f'  {rel}: verbotene iOS-Signierungsdatei ({suffix})')

    if suffix == '.log' and rel not in ALLOWED_LOG_FILES:
        try:
            size = path.stat().st_size
        except OSError:
            size = 0
        issues.append(f'  {rel}: Logdatei nicht erlaubt ({size} Bytes) - Logdateien dürfen nicht committet werden (Ausnahme: {", ".join(sorted(ALLOWED_LOG_FILES))})')

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
        description='Forbidden-Patterns-Check (Secrets, Zertifikate, DB-Dumps, iOS-Signierungsdateien, Logdateien)'
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
        print(f'{level}: verbotene Muster gefunden (Secrets, Zertifikate, DB-Dumps, iOS-Signierungsdateien oder Logdateien):')
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
