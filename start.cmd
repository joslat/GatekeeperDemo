@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0start.ps1" %*
if errorlevel 1 (
  echo.
  echo GatekeeperDemo could not be started. Review the error above.
  pause
  exit /b 1
)
endlocal
