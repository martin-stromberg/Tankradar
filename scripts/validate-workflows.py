#!/usr/bin/env python3
"""Syntaktische und strukturelle Prüfung der GitHub-Actions-Dateien unter .github/.

Prüft ohne Netzwerkzugriff:
  - alle .yml-Dateien sind gültiges YAML,
  - jeder Workflow hat Trigger und Jobs; jeder Job hat runs-on + steps oder uses,
  - 'needs' verweist auf existierende Jobs,
  - lokale Composite-Actions ('uses: ./...') existieren und haben 'runs'/'using: composite',
  - der in staging-to-main-promotion.yml referenzierte workflow_run-Name entspricht dem Anzeigenamen
    von staging-ci.yml (Vorlage, Abschnitt 11.5),
  - lokale Skripte, die in 'run:'-Schritten per 'node scripts/...' oder './scripts/...' aufgerufen
    werden, existieren,
  - (öffentliches Repository) keine .ipa als Workflow-Artefakt oder GitHub-Release-Asset: auch Verzeichnis- und
    Muster-Uploads ('actions/upload-artifact') sowie 'gh release create/upload'-Aufrufe mit Verzeichnissen, Mustern
    oder Variablen, die eine .ipa enthalten könnten, gelten als Verstoß; erlaubt sind nur bekannte Verzeichnisse
    und Dateien mit unbedenklicher Endung.

Wenn 'actionlint' im PATH liegt, wird es zusätzlich ausgeführt (tiefergehende Prüfung).
Exit-Code 0 = alles in Ordnung, 1 = Verstöße.
"""
import re
import shlex
import shutil
import subprocess
import sys
from pathlib import Path, PurePosixPath

try:
    import yaml
except ImportError:  # pragma: no cover - Hinweis für Umgebungen ohne PyYAML
    print('FEHLER: PyYAML fehlt (pip install pyyaml).', file=sys.stderr)
    sys.exit(1)

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

ROOT = Path(__file__).resolve().parent.parent
SCRIPT_CALL_RE = re.compile(r'(?:node |\./)(scripts/[\w./-]+)')
IPA_RE = re.compile(r'\.ipa|ios-ipa', re.IGNORECASE)
# Verzeichnisse, die von Workflow-Schritten als Artefakt hochgeladen werden dürfen (enthalten nie eine .ipa).
SAFE_UPLOAD_DIRS = {'coverage-report', 'TestResults', 'e2e-diagnostics'}
# Dateiendungen, die als Artefakt bzw. Release-Asset unbedenklich sind.
SAFE_FILE_EXTENSIONS = {'.json', '.trx', '.xml', '.html', '.txt', '.md', '.log', '.zip', '.sha256'}
# Optionen von 'gh release create/upload', die einen Wert als eigenes Argument erwarten.
GH_VALUE_OPTIONS = {
    '--target', '-t', '--title', '-T', '--notes', '-n', '--notes-file', '-F', '--repo', '-R',
    '--discussion-category', '--notes-start-tag', '--verify-tag',
}


def load(path):
    with open(path, encoding='utf-8') as handle:
        return yaml.safe_load(handle)


def trigger_block(workflow):
    # PyYAML (YAML 1.1) liest den unquotierten Schlüssel 'on' als Boolean True.
    return workflow.get('on', workflow.get(True))


def check_run_steps(steps, label, errors):
    for step in steps or []:
        for match in SCRIPT_CALL_RE.finditer(str(step.get('run', ''))):
            if not (ROOT / match.group(1)).exists():
                errors.append(f'{label}: aufgerufenes Skript fehlt: {match.group(1)}')
        uses = step.get('uses')
        if isinstance(uses, str) and uses.startswith('./'):
            action_dir = ROOT / uses[2:]
            if not ((action_dir / 'action.yml').exists() or (action_dir / 'action.yaml').exists()):
                errors.append(f'{label}: lokale Action fehlt: {uses}')


def classify_upload_path(entry):
    """Prüft einen Pfad (Zeile von 'path' bzw. Asset von 'gh release') auf eine mögliche .ipa. Liefert einen Grund oder None."""
    entry = entry.strip().split('#', 1)[0]
    if not entry or entry.startswith('!'):
        return None
    if IPA_RE.search(entry):
        return 'enthält eine .ipa'
    if '${{' in entry or '$' in entry or '`' in entry:
        return 'ist nicht statisch prüfbar (Variable oder Ausdruck)'
    normalized = entry.replace('\\', '/')
    if normalized.startswith('/') or '..' in normalized.split('/'):
        return 'zeigt außerhalb des Arbeitsbereichs'
    if normalized.split('/')[0] in SAFE_UPLOAD_DIRS:
        return None
    last = normalized.rstrip('/').rsplit('/', 1)[-1]
    suffix = PurePosixPath(last).suffix.lower()
    if normalized.endswith('/') or not suffix or last in ('.', '*', '**'):
        return 'lädt ein Verzeichnis hoch, das eine .ipa enthalten könnte'
    if suffix not in SAFE_FILE_EXTENSIONS:
        return 'hat eine Dateiendung, die nicht als unbedenklich gilt (oder ein Muster, das eine .ipa treffen könnte)'
    return None


def gh_release_assets(command_line):
    """Liefert die Asset-Argumente eines 'gh release create/upload'-Aufrufs (ohne Tag und Optionen); None, wenn die Zeile keiner ist."""
    try:
        tokens = shlex.split(command_line, comments=True, posix=True)
    except ValueError:
        return None
    for index in range(len(tokens) - 2):
        if tokens[index] == 'gh' and tokens[index + 1] == 'release' and tokens[index + 2] in ('create', 'upload'):
            rest = tokens[index + 3:]
            break
    else:
        return None
    assets = []
    positional = 0
    skip_next = False
    for token in rest:
        if skip_next:
            skip_next = False
            continue
        if token in GH_VALUE_OPTIONS:
            skip_next = True
            continue
        if token.startswith('-') or re.fullmatch(r'\$\{?\w+\[@\]\}?', token):
            continue
        positional += 1
        if positional > 1:  # das erste Argument ist der Tag
            assets.append(token)
    return assets


def check_release_commands(run_text, label, step_name, errors):
    """'gh release create/upload' darf nur eindeutig benannte, unbedenkliche Dateien als Asset veröffentlichen (kein .ipa, kein Verzeichnis, kein Muster, keine Variable)."""
    text = re.sub(r'\\\r?\n', ' ', str(run_text))
    for line in text.splitlines():
        assets = gh_release_assets(line)
        if assets is None:
            continue
        if IPA_RE.search(line):
            errors.append(f'{label}: Schritt "{step_name}" veröffentlicht eine .ipa als Release-Asset (nur der TestFlight-Upload ist erlaubt)')
            continue
        for asset in assets:
            reason = classify_upload_path(asset)
            if reason:
                errors.append(f'{label}: Schritt "{step_name}" veröffentlicht per gh release das Asset "{asset}", das {reason}')


def check_upload_artifact(label, step, errors):
    with_block = step.get('with') or {}
    name = step.get('name', '?')
    if IPA_RE.search(str(with_block.get('name', ''))):
        errors.append(f'{label}: Schritt "{name}" lädt eine .ipa als Workflow-Artefakt hoch (nur der TestFlight-Upload ist erlaubt)')
    for entry in str(with_block.get('path', '')).splitlines():
        reason = classify_upload_path(entry)
        if reason:
            errors.append(f'{label}: Schritt "{name}" lädt "{entry.strip()}" als Workflow-Artefakt hoch, das {reason}')


def check_key_policy(label, steps, errors):
    """Der Repository ist öffentlich: Weder erhält ein Windows-Paketschritt den Tankerkönig-Schlüssel, noch wird eine .ipa als Artefakt hochgeladen."""
    for step in steps or []:
        uses = str(step.get('uses', ''))
        with_block = step.get('with') or {}
        name = step.get('name', '?')
        if uses.startswith('./.github/actions/build-and-package'):
            for key, value in with_block.items():
                if re.search(r'fuel|price|api[-_]?key', f'{key} {value}', re.IGNORECASE) and 'routing' not in str(key).lower():
                    errors.append(f'{label}: Windows-Paketschritt "{name}" darf keinen Schlüssel erhalten ({key})')
        if uses.startswith('actions/upload-artifact'):
            check_upload_artifact(label, step, errors)
        if step.get('run'):
            check_release_commands(step['run'], label, name, errors)


def check_workflow(path, errors):
    label = path.relative_to(ROOT).as_posix()
    workflow = load(path)
    if not isinstance(workflow, dict):
        errors.append(f'{label}: kein gültiges Workflow-Objekt')
        return None
    if not trigger_block(workflow):
        errors.append(f'{label}: Trigger ("on") fehlt')
    jobs = workflow.get('jobs')
    if not isinstance(jobs, dict) or not jobs:
        errors.append(f'{label}: keine Jobs definiert')
        return workflow
    for job_id, job in jobs.items():
        job_label = f'{label} Job "{job_id}"'
        if 'uses' not in job and not ('runs-on' in job and job.get('steps')):
            errors.append(f'{job_label}: runs-on/steps fehlen')
        needs = job.get('needs', [])
        for need in [needs] if isinstance(needs, str) else needs:
            if need not in jobs:
                errors.append(f'{job_label}: needs verweist auf unbekannten Job "{need}"')
        check_run_steps(job.get('steps'), job_label, errors)
        check_key_policy(job_label, job.get('steps'), errors)
    return workflow


def check_action(path, errors):
    label = path.relative_to(ROOT).as_posix()
    action = load(path)
    runs = (action or {}).get('runs')
    if not runs or runs.get('using') != 'composite':
        errors.append(f'{label}: erwartet runs.using == composite')
        return
    for step in runs.get('steps', []):
        if 'run' in step and 'shell' not in step:
            errors.append(f'{label}: run-Schritt "{step.get("name", "?")}" ohne shell')
    check_run_steps(runs.get('steps'), label, errors)
    if path.parent.name == 'build-and-package':
        for name in (action.get('inputs') or {}):
            if re.search(r'fuel|price', str(name), re.IGNORECASE):
                errors.append(f'{label}: die Windows-Paket-Action darf keinen Schlüssel-Input haben ({name})')
        for step in runs.get('steps', []):
            for key, value in (step.get('env') or {}).items():
                if re.search(r'FUEL_PRICE', str(key)) and str(value).strip() not in ('', "''"):
                    errors.append(f'{label}: Schritt "{step.get("name", "?")}" setzt {key} nicht leer')
    check_key_policy(label, runs.get('steps'), errors)


def main():
    errors = []
    workflows = {}
    for path in sorted((ROOT / '.github' / 'workflows').glob('*.yml')):
        workflows[path.name] = check_workflow(path, errors)
    for path in sorted((ROOT / '.github' / 'actions').glob('*/action.yml')):
        check_action(path, errors)

    staging = workflows.get('staging-ci.yml')
    promotion = workflows.get('staging-to-main-promotion.yml')
    if staging and promotion:
        referenced = (trigger_block(promotion) or {}).get('workflow_run', {}).get('workflows', [])
        if staging.get('name') not in referenced:
            errors.append(
                'staging-to-main-promotion.yml: workflow_run referenziert nicht den Anzeigenamen '
                f'von staging-ci.yml ({staging.get("name")!r})'
            )

    actionlint = shutil.which('actionlint')
    if actionlint:
        result = subprocess.run([actionlint, '-no-color'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8')
        if result.returncode != 0:
            errors.append('actionlint meldet Befunde:\n' + (result.stdout + result.stderr).rstrip())
    else:
        print('Hinweis: actionlint nicht im PATH - nur die eingebauten Prüfungen wurden ausgeführt.')

    if errors:
        print('FEHLER: Workflow-Prüfung fehlgeschlagen:')
        for error in errors:
            print(f'  - {error}')
        return 1
    print(f'OK: {len(workflows)} Workflows und alle lokalen Actions sind syntaktisch und strukturell gültig.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
