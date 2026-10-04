# Git Hooks

This folder contains Hooks for Git that can be used to automate tasks and enforce policies in your repository. Git hooks are scripts that run automatically at certain points in the Git workflow, such as before committing changes or after pushing to a remote repository.

## Instructions

Copy the folder githooks to your local repository's `.git/hooks` directory. 

Alternatively, you can create a symbolic link to the `githooks` folder in your repository's `.git/hooks` directory. This way, you can keep the hooks in version control and easily update them across multiple repositories.

Alternatively, you can also use the `git config` command to set the `core.hooksPath` configuration variable to point to the `githooks` folder in your repository. This will tell Git to look for hooks in that directory instead of the default `.git/hooks` directory.
Use the installation scripts provided in the `githooks` folder to set up the hooks in your repository. These scripts will set the configuration variable.

## Structure

The `githooks` folder contains the following files:

- `pre-commit`: This hook runs before a commit is made. It can be used to check for code formatting, run tests, or enforce other policies before allowing a commit to proceed.


- `translation-check.py`: This script checks for missing translations in the codebase. It is used in conjunction with the `pre-commit` hook to ensure that all necessary translations are present before committing changes.


- `csproj-xmldoc-check.py`: Ensures XML documentation is actually enforced and complete for staged (or, with `--all`, all) `.cs`/`.csproj` files: `GenerateDocumentationFile`/`CS1591` configuration in `.csproj`, no `#pragma warning disable` for XML-doc warning codes, and complete `<param>`/`<typeparam>`/`<returns>`/`<response>` tags on documented members.


- `razor-l10n-check.py`: Flags hardcoded, natural-language UI strings in staged (or, with `--all`, all) `.razor` files (localizable attributes like `title`/`placeholder`/`alt`/`aria-label`/`label`/`tooltip`, and multi-word text nodes) that should go through `@L["Key"]` localization instead.


- `razor-usage-check.py`: Flags orphaned `.razor` components that are not referenced anywhere in their project. In `pre-commit` it only warns (runs whenever a `.razor` file is staged); in `pre-push` it runs with `--all --strict`, auditing the whole repository and blocking the push if any orphans remain.


- `no-notimplemented-check.py`: Flags `NotImplementedException` usage and members whose body is just a single `throw` statement (placeholder stubs) in `.cs` files. In `pre-commit` it only warns about staged files, so work in progress is allowed; in `pre-push` it runs with `--all --strict`, auditing the whole repository and blocking the push if any stubs remain — so unfinished implementations can't leave the machine.


- `enum-coverage-check.py`: Flags public/internal C# enums whose values are not covered by any test file (test projects are recognized by a `Test`/`Tests` directory name). In `pre-commit` it only warns about the current solution/repo state whenever a `.cs` file is staged; in `pre-push` it runs with `--all --strict`, auditing the whole repository and blocking the push if any enum coverage gaps remain.


- `pre-push`: Blocks direct pushes to `main`/`staging`, then runs `no-notimplemented-check.py`, `razor-usage-check.py`, and `enum-coverage-check.py` in strict, whole-repo mode.