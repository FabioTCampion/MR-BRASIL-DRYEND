[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Update-ProcessPath {
    $machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$machinePath;$userPath"
}

function Get-CommandVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $resolvedCommand = Get-Command $Command -ErrorAction SilentlyContinue
    if ($null -eq $resolvedCommand) {
        return 'NOT INSTALLED'
    }

    try {
        $output = & $resolvedCommand.Source @Arguments 2>$null
        if ($LASTEXITCODE -ne 0 -or $null -eq $output) {
            return 'INSTALLED - VERSION UNAVAILABLE'
        }

        return (($output | Select-Object -First 1) -as [string]).Trim()
    }
    catch {
        return 'INSTALLED - VERSION UNAVAILABLE'
    }
}

function Get-VisualStudioCodeVersion {
    $candidatePaths = @(
        "$env:ProgramFiles\Microsoft VS Code\Code.exe",
        "$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -LiteralPath $candidatePath) {
            return (Get-Item -LiteralPath $candidatePath).VersionInfo.ProductVersion
        }
    }

    return 'NOT INSTALLED'
}

Update-ProcessPath

$windowsCurrentVersion = Get-ItemProperty `
    -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' `
    -ErrorAction SilentlyContinue

$twinCatService = Get-Service -Name 'TcSysSrv' -ErrorAction SilentlyContinue
$adsDllPath = 'C:\TwinCAT\AdsApi\.NET\v4.0.30319\TwinCAT.Ads.dll'

$status = [ordered]@{
    Windows              = if ($windowsCurrentVersion) { $windowsCurrentVersion.ProductName } else { 'UNKNOWN' }
    WindowsBuild         = if ($windowsCurrentVersion) { $windowsCurrentVersion.CurrentBuild } else { 'UNKNOWN' }
    Architecture         = $env:PROCESSOR_ARCHITECTURE
    WindowsPowerShell    = $PSVersionTable.PSVersion.ToString()
    Winget               = Get-CommandVersion -Command 'winget' -Arguments @('--version')
    Git                  = Get-CommandVersion -Command 'git' -Arguments @('--version')
    DotnetSdk            = Get-CommandVersion -Command 'dotnet' -Arguments @('--version')
    Node                 = Get-CommandVersion -Command 'node' -Arguments @('--version')
    Npm                  = Get-CommandVersion -Command 'npm.cmd' -Arguments @('--version')
    VisualStudioCode     = Get-VisualStudioCodeVersion
    TwinCatSystemService = if ($twinCatService) { $twinCatService.Status.ToString() } else { 'NOT INSTALLED' }
    TwinCatAdsDll        = if (Test-Path -LiteralPath $adsDllPath) { $adsDllPath } else { 'NOT FOUND' }
}

Write-Host ''
Write-Host 'DryEnd.Next environment status' -ForegroundColor Cyan
Write-Host 'Estado do ambiente DryEnd.Next' -ForegroundColor Cyan
Write-Host ''

[pscustomobject]$status | Format-List

Write-Host 'Development requirements / Requisitos de desenvolvimento:' -ForegroundColor Yellow
Write-Host '  - Visual Studio Code'
Write-Host '  - .NET 10 SDK x64'
Write-Host '  - Node.js 24 LTS x64 and npm'
Write-Host '  - Git for Windows'
Write-Host '  - TwinCAT ADS route/runtime for online tests'
Write-Host ''
Write-Host 'Server requirements / Requisitos do servidor:' -ForegroundColor Yellow
Write-Host '  - ASP.NET Core Runtime 10 x64, unless published self-contained'
Write-Host '  - TwinCAT ADS Router/runtime and configured ADS route'
Write-Host '  - SQL Server network access'
Write-Host '  - No Node.js, VS Code, Git, or IIS required'
