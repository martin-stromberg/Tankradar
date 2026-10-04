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
    werden, existieren.

Wenn 'actionlint' im PATH liegt, wird es zusätzlich ausgeführt (tiefergehende Prüfung).
Exit-Code 0 = alles in Ordnung, 1 = Verstöße.
"""
import re
import shutil
import subprocess
import sys
from pathlib import Path

try:
    import yaml
except ImportError:  # pragma: no cover - Hinweis für Umgebungen ohne PyYAML
    print('FEHLER: PyYAML fehlt (pip install pyyaml).', file=sys.stderr)
    sys.exit(1)

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.stderr.reconfigure(encoding='utf-8', errors='replace')

ROOT = Path(__file__).resolve().parent.parent
SCRIPT_CALL_RE = re.compile(r'(?:node |\./)(scripts/[\w./-]+)')


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
