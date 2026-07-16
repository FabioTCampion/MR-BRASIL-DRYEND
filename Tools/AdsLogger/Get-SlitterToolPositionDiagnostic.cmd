@echo off
setlocal
cd /d "%~dp0\..\.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Tools\AdsLogger\Get-SlitterToolPositionDiagnostic.ps1"
echo.
pause
endlocal
