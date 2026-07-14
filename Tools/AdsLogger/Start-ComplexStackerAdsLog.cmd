@echo off
REM EN: Starts the read-only complex stacker ADS logger.
REM PT: Inicia o logger ADS somente leitura do stacker complexo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-ComplexStackerAdsLog.ps1" %*
