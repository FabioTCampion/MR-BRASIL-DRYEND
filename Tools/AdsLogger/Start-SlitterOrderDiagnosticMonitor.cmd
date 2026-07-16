@echo off
setlocal
cd /d "%~dp0\..\.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Tools\AdsLogger\Start-SlitterOrderDiagnosticMonitor.ps1"
pause
