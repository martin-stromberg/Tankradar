#!/usr/bin/env python3
"""Tests von validate-workflows.py: .ipa-Prüfungen (Verzeichnis-/Muster-Uploads), das Verbot mutierender
'gh release'-Aufrufe (Issue #64, ADR 0023) und Guards für den entfernten Release-Mechanismus bzw. den
verbindlichen TestFlight-Upload."""
import importlib.util
import sys
import unittest
from pathlib import Path

SPEC = importlib.util.spec_from_file_location('validate_workflows', Path(__file__).resolve().parent / 'validate-workflows.py')
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules['validate_workflows'] = MODULE
SPEC.loader.exec_module(MODULE)


def upload_errors(path, name='bericht'):
    errors = []
    MODULE.check_upload_artifact('wf', {'name': 'Upload', 'uses': 'actions/upload-artifact@v7', 'with': {'name': name, 'path': path}}, errors)
    return errors


def release_errors(run):
    errors = []
    MODULE.check_release_commands(run, 'wf', 'Release', errors)
    return errors


class UploadArtifactTests(unittest.TestCase):
    def test_known_safe_uploads_are_allowed(self):
        for path in ['coverage-report/', 'TestResults/*.trx', 'e2e-diagnostics/', 'vulnerable-packages.json', 'TestResults/**', 'artifacts/update.json']:
            self.assertEqual([], upload_errors(path), path)

    def test_explicit_ipa_is_rejected(self):
        self.assertTrue(upload_errors('release-ios.ipa'))
        self.assertTrue(upload_errors('coverage-report/', name='ios-ipa'))
        self.assertTrue(upload_errors('src/**/*.IPA'))

    def test_directories_that_could_contain_an_ipa_are_rejected(self):
        for path in ['artifacts/', 'artifacts', 'src/Tankradar.MAUI/bin/Release/', '.', './', '*', '**', 'artifacts/*', 'bin/**']:
            self.assertTrue(upload_errors(path), path)

    def test_patterns_and_unknown_extensions_are_rejected(self):
        for path in ['bin/*.app', 'publish/*', 'build/output.bin']:
            self.assertTrue(upload_errors(path), path)

    def test_multiline_paths_are_checked_individually(self):
        self.assertEqual([], upload_errors('coverage-report/\nTestResults/*.trx\n!TestResults/skip.trx'))
        self.assertEqual(1, len(upload_errors('coverage-report/\nartifacts/')))

    def test_unverifiable_and_escaping_paths_are_rejected(self):
        for path in ['${{ env.OUT }}', '$OUT/file.zip', '../outside.zip', '/abs/file.zip']:
            self.assertTrue(upload_errors(path), path)


class ReleaseCommandTests(unittest.TestCase):
    def test_mutating_release_commands_are_rejected(self):
        for run in [
            'gh release create "$RELEASE_TAG" artifacts/release-win-x64.zip artifacts/update.json --title "$RELEASE_TAG" --generate-notes',
            'gh release upload "$RELEASE_TAG" artifacts/release-win-x64.zip --clobber',
            'gh release create "$RC_TAG" a.zip \\\n  --target "$GITHUB_SHA" \\\n  --prerelease',
            'gh release edit v1 --draft=false',
            'gh release delete v1 --yes',
            'gh release upload v1 artifacts/update.json#Manifest',
        ]:
            self.assertTrue(release_errors(run), run)

    def test_ipa_release_assets_are_rejected(self):
        # Mutierende Aufrufe sind ohnehin verboten - die .ipa wird unabhängig vom Asset-Inhalt abgelehnt.
        self.assertTrue(release_errors('gh release upload v1 release-ios.ipa'))
        self.assertTrue(release_errors('gh release create v1 artifacts/a.zip \\\n  build/App.ipa --title t'))

    def test_line_continuations_do_not_bypass_the_ban(self):
        # Ein '\'-Umbruch zwischen 'gh', 'release' und dem Unterbefehl darf das Verbot nicht umgehen.
        for run in [
            'gh \\\n  release create "$TAG" a.zip',
            'gh release \\\n  create "$TAG" a.zip',
            'gh \\\n  release \\\n  upload "$TAG" a.zip --clobber',
        ]:
            self.assertTrue(release_errors(run), run)

    def test_gh_api_mutations_on_releases_are_rejected(self):
        for run in [
            'gh api repos/{owner}/{repo}/releases -X POST -f tag_name=v1',
            'gh api --method POST repos/o/r/releases -f tag_name=v1',
            'gh api repos/o/r/releases/123 -X PATCH -f draft=false',
            'gh api repos/o/r/releases/123 -X DELETE',
            'gh api repos/o/r/releases -f tag_name=v1',
            'gh api repos/o/r \\\n  /releases -X PUT -f tag_name=v1',
        ]:
            self.assertTrue(release_errors(run), run)

    def test_gh_api_reads_on_releases_are_allowed(self):
        for run in [
            'gh api repos/{owner}/{repo}/releases',
            'gh api repos/o/r/releases -X GET',
            'gh api repos/o/r/releases/latest',
            'gh api repos/o/r/issues -X POST -f title=x',
        ]:
            self.assertEqual([], release_errors(run), run)

    def test_label_suffix_and_other_commands_are_handled(self):
        # Nicht-mutierende Unterbefehle und lose Erwähnungen bleiben zulässig.
        self.assertEqual([], release_errors('gh release view v1'))
        self.assertEqual([], release_errors('gh release list --limit 10'))
        self.assertEqual([], release_errors('gh release view v1\necho gh release'))

    def test_workflows_in_repository_are_clean(self):
        errors = []
        for path in sorted((MODULE.ROOT / '.github' / 'workflows').glob('*.yml')):
            MODULE.check_workflow(path, errors)
        for path in sorted((MODULE.ROOT / '.github' / 'actions').glob('*/action.yml')):
            MODULE.check_action(path, errors)
        self.assertEqual([], errors)


class ReleaseMechanismRemovedTests(unittest.TestCase):
    def test_release_mechanism_files_removed(self):
        self.assertFalse((MODULE.ROOT / '.github' / 'actions' / 'build-and-package' / 'action.yml').exists())
        self.assertFalse((MODULE.ROOT / 'scripts' / 'package-windows.ps1').exists())

    def test_no_repo_workflow_uses_gh_release(self):
        for path in sorted((MODULE.ROOT / '.github' / 'workflows').glob('*.yml')):
            text = path.read_text(encoding='utf-8')
            self.assertFalse(MODULE.contains_release_mutation(text), path.name)


class RequireUploadTests(unittest.TestCase):
    """Guard gegen einen stillen Rückbau des verbindlichen TestFlight-Uploads (Annahme 5, Issue #64):
    Würde require-upload entfernt oder auf 'false' zurückgesetzt, entstünde wieder ein 'Release ohne
    Auslieferung' - dieser Test schlägt dann fehl."""

    def _package_ios_with(self, workflow_name, job_name):
        workflow = MODULE.load(MODULE.ROOT / '.github' / 'workflows' / workflow_name)
        job = workflow['jobs'][job_name]
        for step in job.get('steps') or []:
            if step.get('uses') == './.github/actions/package-ios':
                return step.get('with') or {}
        self.fail(f'{workflow_name}: Job "{job_name}" ruft ./.github/actions/package-ios nicht auf')

    def test_release_ios_requires_upload(self):
        self.assertEqual('true', self._package_ios_with('release.yml', 'ios').get('require-upload'))

    def test_staging_ios_prerelease_requires_upload(self):
        self.assertEqual('true', self._package_ios_with('staging-ci.yml', 'ios-prerelease').get('require-upload'))

    def test_package_ios_declares_require_upload_input(self):
        action = MODULE.load(MODULE.ROOT / '.github' / 'actions' / 'package-ios' / 'action.yml')
        self.assertIn('require-upload', action.get('inputs') or {})


if __name__ == '__main__':
    unittest.main()
