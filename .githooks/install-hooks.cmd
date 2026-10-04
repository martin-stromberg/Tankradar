@echo off
setlocal

where git >nul 2>nul
if errorlevel 1 (
  echo FEHLER: Git wurde nicht gefunden. Bitte Git installieren und sicherstellen, dass es im PATH liegt.
  exit /b 1
)

git config --local core.hooksPath .githooks
if errorlevel 1 (
  echo FEHLER: "git config --local core.hooksPath .githooks" ist fehlgeschlagen.
  exit /b 1
)

where python3 >nul 2>nul
if errorlevel 1 (
  where python >nul 2>nul
  if errorlevel 1 (
    echo WARNUNG: Weder "python3" noch "python" wurden im PATH gefunden. Die Git-Hooks benoetigen Python 3.x.
  )
)

echo.
echo Git-Hooks wurden fuer dieses Repository aktiviert (core.hooksPath=.githooks).
echo Details: docs\help\git-hooks\
exit /b 0
