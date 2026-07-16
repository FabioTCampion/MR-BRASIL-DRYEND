@echo off
setlocal
cd /d "%~dp0\..\.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\Tools\AdsLogger\Get-SlitterToolVariationReport.ps1"
pause
